using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    public partial class MainForm
    {
        // ── État BitLocker d'un lecteur ────────────────────────────────────────────
        private enum BitLockerState
        {
            Unknown,
            NotEncrypted,
            Unlocked,
            Locked,
        }

        private sealed class USBDriveInfo
        {
            public string Letter { get; init; } = "";
            public string Label { get; init; } = "";
            public long Size { get; init; }
            public bool HasUsers { get; init; }
            public bool HasWindows { get; init; }
            public string UsersPath { get; init; } = "";
            public BitLockerState BitLocker { get; set; } = BitLockerState.Unknown;

            public override string ToString()
            {
                var sizeStr = Size > 0 ? FileService.FormatSize(Size) : "Inconnu";
                var bde = BitLocker switch
                {
                    BitLockerState.Locked => " \U0001f512 BitLocker verrouillé",
                    BitLockerState.Unlocked => " \U0001f513 BitLocker actif (déverrouillé)",
                    _ => ""
                };
                return $"{Letter} — {Label} ({sizeStr}){bde}";
            }
        }

        private sealed class UserProfileItem
        {
            public string Name { get; init; } = " ";
            public string Path { get; init; } = " ";
            public bool IsMatch { get; init; }

            public override string ToString()
                => IsMatch ? $"★ {Name} (correspond à l'utilisateur actuel)" : Name;
        }

        // ───────────────────────────────────────────────────────────────────
        //  DÉTECTION DES LECTEURS
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Appel direct à kernel32 : retourne le type du point de montage même si le
        /// volume est inaccessible (BitLocker verrouillé, disque non prêt…).
        /// Valeurs : 0=Unknown, 1=NoRootDir (lettre libre), 2=Removable,
        ///           3=Fixed, 4=Network, 5=CDROM, 6=RAMDisk
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern uint GetDriveType(string lpRootPathName);

        private static uint GetDriveTypeWin32(string root)
        {
            try { return GetDriveType(root); }
            catch { return 0; }
        }

        /// <summary>
        /// Teste si le volume est accessible en lecture.
        /// Un volume BitLocker verrouillé lève UnauthorizedAccessException ou IOException.
        /// </summary>
        private static bool IsVolumeAccessible(string root)
        {
            try { Directory.GetDirectories(root); return true; }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }
            catch { return false; }
        }

        private void LoadUSBDrives()
        {
            cmbUSBDrives.Items.Clear();
            lstProfiles.Items.Clear();
            lblBitLockerStatus.Text = "";

            try
            {
                var currentRoot = Path.GetPathRoot(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                    ?.TrimEnd(Path.DirectorySeparatorChar)
                    .ToUpperInvariant();

                var result = new List<USBDriveInfo>();
                var seenLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // ── Étape 1 : scan A–Z via GetDriveType (fonctionne même si IsReady = false)
                foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                {
                    if (c == 'A' || c == 'B') continue;   // disquettes

                    var letter = $"{c}:";
                    var root = letter + "\\";

                    if (letter.Equals(currentRoot, StringComparison.OrdinalIgnoreCase)) continue;

                    uint driveType = GetDriveTypeWin32(root);
                    if (driveType <= 1) continue;          // 0=inconnu, 1=lettre libre
                    if (driveType == 5 || driveType == 6) continue; // CD/DVD, RAMDisk

                    bool accessible = IsVolumeAccessible(root);

                    if (!accessible)
                    {
                        // Volume monté mais inaccessible → très probablement BitLocker verrouillé.
                        var bdeState = GetBitLockerStatePowerShell(root);
                        var finalState = bdeState == BitLockerState.Unknown
                            ? BitLockerState.Locked
                            : bdeState;

                        if (finalState == BitLockerState.Locked)
                        {
                            result.Add(new USBDriveInfo
                            {
                                Letter = letter,
                                Label = "Volume verrouillé",
                                BitLocker = BitLockerState.Locked
                            });
                            seenLetters.Add(letter);
                        }
                        continue;
                    }

                    // Volume accessible : ne retenir que ceux qui ont un dossier Users
                    var usersPath = Path.Combine(root, "Users");
                    var windowsPath = Path.Combine(root, "Windows");
                    if (!Directory.Exists(usersPath)) continue;

                    string volLabel = "Sans nom";
                    long volSize = 0;
                    try
                    {
                        var di = new DriveInfo(letter);
                        if (!string.IsNullOrEmpty(di.VolumeLabel)) volLabel = di.VolumeLabel;
                        volSize = di.TotalSize;
                    }
                    catch { /* volume démonté entre-temps */ }

                    var state = GetBitLockerStatePowerShell(root);

                    result.Add(new USBDriveInfo
                    {
                        Letter = letter,
                        Label = volLabel,
                        Size = volSize,
                        HasUsers = true,
                        HasWindows = Directory.Exists(windowsPath),
                        UsersPath = usersPath,
                        BitLocker = state
                    });
                    seenLetters.Add(letter);
                }

                // ── Étape 2 : fallback PowerShell global — volumes BitLocker que
                //    GetDriveType ne voit pas du tout (cas rares, volumes non montés)
                foreach (var ltr in GetLockedBitLockerLetters(currentRoot ?? "C:"))
                {
                    if (seenLetters.Contains(ltr)) continue;
                    result.Add(new USBDriveInfo
                    {
                        Letter = ltr,
                        Label = "Volume verrouillé",
                        BitLocker = BitLockerState.Locked
                    });
                }

                foreach (var d in result.OrderBy(d => d.Letter))
                    cmbUSBDrives.Items.Add(d);

                if (cmbUSBDrives.Items.Count > 0)
                    cmbUSBDrives.SelectedIndex = 0;
                else
                    lblMigrationInfo.Text =
                        "Aucun disque externe avec un dossier Users n'a été détecté.\n" +
                        "Branchez le disque puis cliquez sur \U0001f504.";
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur détection disque : {ex.Message}");
            }
        }

        // ── Helpers PowerShell BitLocker ────────────────────────────────────────────
        private static BitLockerState GetBitLockerStatePowerShell(string drivePath)
        {
            try
            {
                var letter = drivePath.Replace("/", "\\").TrimEnd('\\').ToUpperInvariant();
                if (!letter.EndsWith(':')) letter += ":";

                var script =
                    $"$v = Get-BitLockerVolume -MountPoint '{letter}' -ErrorAction SilentlyContinue; " +
                    "if ($v -eq $null) { 'UNKNOWN' } " +
                    "elseif ($v.ProtectionStatus -eq 'Off') { 'OFF' } " +
                    "elseif ($v.LockStatus -eq 'Locked') { 'LOCKED' } " +
                    "else { 'UNLOCKED' }";

                return RunPowerShellInline(script).Trim() switch
                {
                    "OFF"      => BitLockerState.NotEncrypted,
                    "LOCKED"   => BitLockerState.Locked,
                    "UNLOCKED" => BitLockerState.Unlocked,
                    _          => BitLockerState.Unknown
                };
            }
            catch { return BitLockerState.Unknown; }
        }

        /// <summary>
        /// Retourne les lettres de lecteur verrouillés par BitLocker (fallback global).
        /// </summary>
        private static List<string> GetLockedBitLockerLetters(string excludeRoot)
        {
            var letters = new List<string>();
            try
            {
                var script =
                    "Get-BitLockerVolume -ErrorAction SilentlyContinue | " +
                    "Where-Object { $_.LockStatus -eq 'Locked' } | " +
                    "Select-Object -ExpandProperty MountPoint";

                foreach (var line in RunPowerShellInline(script).Split('\n'))
                {
                    var mp = line.Trim().TrimEnd('\\');
                    if (string.IsNullOrWhiteSpace(mp)) continue;
                    var root = mp.ToUpperInvariant();
                    if (root.Equals(excludeRoot, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!root.EndsWith(':')) continue;
                    letters.Add(root);
                }
            }
            catch { /* module BitLocker absent — silencieux */ }
            return letters;
        }

        /// <summary>
        /// Ouvre la page de gestion BitLocker du Panneau de configuration.
        /// </summary>
        private static void OpenBitLockerControlPanel()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "control.exe",
                    Arguments       = "/name Microsoft.BitLockerDriveEncryption",
                    UseShellExecute = true
                });
                return;
            }
            catch { }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "ms-settings:deviceencryption",
                    UseShellExecute = true
                });
                return;
            }
            catch { }

            MessageBox.Show(
                "Impossible d'ouvrir automatiquement la gestion BitLocker.\n\n" +
                "Ouvrez manuellement :\n" +
                "Panneau de configuration → Système et sécurité → Chiffrement de lecteur BitLocker",
                "BitLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static string RunPowerShellInline(string script)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "powershell.exe",
                Arguments              = $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)
                ?? throw new InvalidOperationException("Impossible de démarrer PowerShell.");
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }

        // ───────────────────────────────────────────────────────────────────
        //  ÉVÉNEMENTS UI
        // ───────────────────────────────────────────────────────────────────
        private void BtnRefreshUSB_Click(object? sender, EventArgs e)
        {
            LoadUSBDrives();
            Log(rtbMigrationLog, "Liste des lecteurs actualisée.");
        }

        private void CmbUSBDrives_SelectedIndexChanged(object? sender, EventArgs e)
        {
            lstProfiles.Items.Clear();
            lblBitLockerStatus.Text = "";

            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive) return;

            UpdateBitLockerLabel(drive);

            if (drive.BitLocker == BitLockerState.Locked)
            {
                lblMigrationInfo.Text =
                    $"\u26a0\ufe0f {drive.Letter} est verrouillé par BitLocker.\n" +
                    "Cliquez sur \U0001f512 Vérifier BitLocker pour déverrouiller.";
                return;
            }

            if (!drive.HasUsers)
            {
                lblMigrationInfo.Text =
                    "Ce lecteur ne contient pas de dossier Users.\n" +
                    "Vérifiez que c'est bien le bon disque.";
                return;
            }

            lblMigrationInfo.Text = "Chargement des profils...";
            LoadProfiles(drive);
        }

        private void UpdateBitLockerLabel(USBDriveInfo drive)
        {
            (lblBitLockerStatus.Text, lblBitLockerStatus.ForeColor) = drive.BitLocker switch
            {
                BitLockerState.Locked       => ($"\U0001f512 {drive.Letter} — BitLocker VERROUILLÉ",             Color.OrangeRed),
                BitLockerState.Unlocked     => ($"\U0001f513 {drive.Letter} — BitLocker actif (déverrouillé)",   Color.DarkOrange),
                BitLockerState.NotEncrypted => ($"\u2705 {drive.Letter} — Pas de chiffrement",                   Color.SeaGreen),
                _                           => ($"\u2139\ufe0f {drive.Letter} — État BitLocker inconnu",         SystemColors.GrayText)
            };
        }

        private void LoadProfiles(USBDriveInfo drive)
        {
            lstProfiles.Items.Clear();
            try
            {
                var excluded = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };
                var currentUsername = Environment.UserName;

                if (!Directory.Exists(drive.UsersPath)) return;

                var profiles = Directory.GetDirectories(drive.UsersPath)
                    .Select(p => new DirectoryInfo(p))
                    .Where(d => !excluded.Contains(d.Name, StringComparer.OrdinalIgnoreCase) && !d.Name.StartsWith('.'))
                    .Where(d => Directory.Exists(Path.Combine(d.FullName, "Documents"))
                             || Directory.Exists(Path.Combine(d.FullName, "Desktop")))
                    .Select(d =>
                    {
                        var base_ = d.Name.Contains('.') ? d.Name[..d.Name.LastIndexOf('.')] : d.Name;
                        var isMatch =
                            d.Name.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) ||
                            base_.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) ||
                            d.Name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase);
                        return new UserProfileItem { Name = d.Name, Path = d.FullName, IsMatch = isMatch };
                    })
                    .OrderByDescending(p => p.IsMatch)
                    .ThenBy(p => p.Name)
                    .ToList();

                foreach (var p in profiles) lstProfiles.Items.Add(p);

                // Sélection automatique : préférer le profil NOM (sans domaine) s'il existe,
                // sinon le premier correspondant.
                var currentUsername2 = Environment.UserName;
                var exactMatch = profiles.FirstOrDefault(p =>
                    p.Name.Equals(currentUsername2, StringComparison.OrdinalIgnoreCase));
                var domainMatch = profiles.FirstOrDefault(p =>
                    p.Name.StartsWith(currentUsername2 + ".", StringComparison.OrdinalIgnoreCase));

                // Si les deux existent, on sélectionne NOM (sera migré en second, après NOM.DOMAINE)
                if (exactMatch != null)
                    lstProfiles.SelectedItem = exactMatch;
                else if (domainMatch != null)
                    lstProfiles.SelectedItem = domainMatch;
                else
                {
                    var firstMatch = profiles.FirstOrDefault(p => p.IsMatch);
                    if (firstMatch != null) lstProfiles.SelectedItem = firstMatch;
                    else if (lstProfiles.Items.Count > 0) lstProfiles.SelectedIndex = 0;
                }

                // Indication visuelle si double profil détecté
                if (exactMatch != null && domainMatch != null && chkOldProfile.Checked)
                    lblMigrationInfo.Text =
                        $"{profiles.Count} profil(s) trouvé(s).\n" +
                        $"⚠️ Double profil détecté ({domainMatch.Name} + {exactMatch.Name}).\n" +
                        "La migration copiera d'abord l'ancien profil domaine, puis le profil actuel.";
                else
                    lblMigrationInfo.Text = $"{profiles.Count} profil(s) trouvé(s). Sélectionnez le profil à migrer.";
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur chargement profils : {ex.Message}");
                lblMigrationInfo.Text = "Erreur lors du chargement des profils.";
            }
        }

        // ───────────────────────────────────────────────────────────────────
        //  CONSTRUCTION DE LA LISTE DES ÉTAPES DE MIGRATION
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Construit les étapes de migration pour un profil source donné.
        /// </summary>
        private List<(string Name, Func<Task> Action)> BuildMigrationSteps(
            string sourceProfile,
            string destProfile,
            Progress<int> progress,
            List<string> errorList,
            CancellationToken ct,
            USBDriveInfo selectedDrive,
            bool includePublic)
        {
            var steps = new List<(string Name, Func<Task> Action)>();

            if (chkMigrateDocuments.Checked) steps.Add(("Documents", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Documents"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Documents", progress, errorList, ct)));

            if (chkMigrateDesktop.Checked) steps.Add(("Bureau", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Desktop"),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Bureau", progress, errorList, ct)));

            if (chkMigrateDownloads.Checked) steps.Add(("Téléchargements", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Downloads"),
                Path.Combine(destProfile, "Downloads"),
                "Téléchargements", progress, errorList, ct)));

            if (chkMigratePictures.Checked) steps.Add(("Images", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Pictures"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Images", progress, errorList, ct)));

            if (chkMigrateMusic.Checked) steps.Add(("Musique", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Music"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                "Musique", progress, errorList, ct)));

            if (chkMigrateVideos.Checked) steps.Add(("Vidéos", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Videos"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "Vidéos", progress, errorList, ct)));

            if (chkMigrateSignatures.Checked) steps.Add(("Signatures Outlook", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Signatures"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),
                "Signatures Outlook", progress, errorList, ct)));

            if (chkMigrateTemplates.Checked) steps.Add(("Modèles Office", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Templates"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                "Modèles Office", progress, errorList, ct)));

            if (chkMigrateExcelMacros.Checked) steps.Add(("Macros Excel (XLSTART)", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Excel", "XLSTART"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                "Macros Excel (XLSTART)", progress, errorList, ct)));

            if (chkMigrateSap.Checked) steps.Add(("SAP GUI", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "SAP"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                "SAP GUI", progress, errorList, ct)));

            if (chkMigrateEdgeProfile.Checked) steps.Add(("Profil Edge", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Local", "Microsoft", "Edge", "User Data", "Default"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default"),
                "Profil Edge", progress, errorList, ct)));

            if (chkMigrateStickyNotes.Checked) steps.Add(("Sticky Notes",
                () => MigrateStickyNotesAsync(sourceProfile, ct)));

            if (chkMigrateOutlook.Checked) steps.Add(("Données Outlook (PST)",
                () => MigrateOutlookAsync(sourceProfile, ct)));

            if (chkMigrateWallpaper.Checked) steps.Add(("Fond d'écran",
                () => MigrateWallpaperAsync(sourceProfile)));

            if (chkMigrateNetworkDrives.Checked) steps.Add(("Lecteurs réseau",
                () => MigrateNetworkDrivesAsync(sourceProfile)));

            if (includePublic && chkMigratePublic.Checked) steps.Add(("Dossier Public", () => MigrateFolderStep(
                Path.Combine(selectedDrive.Letter + "\\", "Users", "Public"),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                "Dossier Public", progress, errorList, ct)));

            return steps;
        }

        // ── Bouton Démarrer la migration ──────────────────────────────────────────────
        private async void BtnStartMigration_Click(object? sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is not UserProfileItem selectedProfile)
            {
                MessageBox.Show(
                    "Veuillez sélectionner un profil à migrer dans la liste.",
                    "Aucun profil sélectionné",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (cmbUSBDrives.SelectedItem is not USBDriveInfo selectedDrive)
            {
                MessageBox.Show(
                    "Veuillez sélectionner un disque source dans la liste.",
                    "Aucun disque sélectionné",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (selectedDrive.BitLocker == BitLockerState.Locked)
            {
                MessageBox.Show(
                    $"Le lecteur {selectedDrive.Letter} est verrouillé par BitLocker.\n" +
                    "Déverrouillez-le avant de démarrer la migration.",
                    "BitLocker verrouillé",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // ── Détection double profil NOM.DOMAINE + NOM ─────────────────────────────
            // Si "Détecter Ancien profil" est coché et que les deux variantes existent,
            // on effectue d'abord la copie du profil NOM.DOMAINE, puis celle du profil NOM.
            var currentUsername = Environment.UserName;
            UserProfileItem? domainProfile = null;
            UserProfileItem? cleanProfile  = null;

            if (chkOldProfile.Checked && selectedDrive.HasUsers)
            {
                var allProfiles = lstProfiles.Items.Cast<UserProfileItem>().ToList();

                domainProfile = allProfiles.FirstOrDefault(p =>
                    p.Name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase));

                cleanProfile = allProfiles.FirstOrDefault(p =>
                    p.Name.Equals(currentUsername, StringComparison.OrdinalIgnoreCase));
            }

            bool doubleMigration = domainProfile != null && cleanProfile != null;

            // Message de confirmation adapté
            string confirmMsg = doubleMigration
                ? $"Double profil détecté :\n" +
                  $"  1. {domainProfile!.Name}  (ancien profil domaine)\n" +
                  $"  2. {cleanProfile!.Name}  (profil actuel)\n\n" +
                  $"La migration copiera d'abord « {domainProfile.Name} » puis « {cleanProfile.Name} ».\n" +
                  $"Les données du profil actuel écraseront celles de l'ancien profil en cas de conflit.\n\n" +
                  $"Source : {selectedDrive.Letter}\nConfirmer ?"
                : $"Démarrer la migration du profil « {selectedProfile.Name} » depuis {selectedDrive.Letter} ?\n\n" +
                  "Les fichiers plus récents sur le disque source écraseront ceux de la destination.";

            var confirm = MessageBox.Show(
                confirmMsg,
                "Confirmer la migration",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            rtbMigrationLog.Clear();
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var destProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var progress    = new Progress<int>(UpdateProgress);

                LogTitle(rtbMigrationLog, "Démarrage de la migration");
                LogInfo(rtbMigrationLog, $"Dest    : {destProfile}");
                LogInfo(rtbMigrationLog, $"Utilisateur actuel : {currentUsername}");

                if (doubleMigration)
                {
                    // ── Passe 1 : NOM.DOMAINE ────────────────────────────────────────
                    LogTitle(rtbMigrationLog, $"Passe 1 — Ancien profil domaine : {domainProfile!.Name}");
                    LogInfo(rtbMigrationLog, $"Source  : {domainProfile.Path}");

                    var steps1 = BuildMigrationSteps(
                        domainProfile.Path, destProfile, progress, errorList, ct,
                        selectedDrive, includePublic: false);

                    int total1 = steps1.Count, step1 = 0;
                    foreach (var (name, action) in steps1)
                    {
                        ct.ThrowIfCancellationRequested();
                        step1++;
                        UpdateStatus($"[Passe 1/{2}] Migration {name} ({step1}/{total1})");
                        await action();
                    }

                    LogSuccess(rtbMigrationLog, $"Passe 1 terminée — profil « {domainProfile.Name} » copié.");

                    // ── Passe 2 : NOM ────────────────────────────────────────────────
                    LogTitle(rtbMigrationLog, $"Passe 2 — Profil actuel : {cleanProfile!.Name}");
                    LogInfo(rtbMigrationLog, $"Source  : {cleanProfile.Path}");

                    var steps2 = BuildMigrationSteps(
                        cleanProfile.Path, destProfile, progress, errorList, ct,
                        selectedDrive, includePublic: true);

                    int total2 = steps2.Count, step2 = 0;
                    foreach (var (name, action) in steps2)
                    {
                        ct.ThrowIfCancellationRequested();
                        step2++;
                        UpdateStatus($"[Passe 2/{2}] Migration {name} ({step2}/{total2})");
                        await action();
                    }

                    LogSuccess(rtbMigrationLog, $"Passe 2 terminée — profil « {cleanProfile.Name} » copié.");
                }
                else
                {
                    // ── Migration simple (un seul profil) ────────────────────────────
                    LogInfo(rtbMigrationLog, $"Source  : {selectedProfile.Path}");

                    var steps = BuildMigrationSteps(
                        selectedProfile.Path, destProfile, progress, errorList, ct,
                        selectedDrive, includePublic: true);

                    int totalSteps = steps.Count, currentStep = 0;
                    foreach (var (name, action) in steps)
                    {
                        ct.ThrowIfCancellationRequested();
                        currentStep++;
                        UpdateStatus($"Migration {name} ({currentStep}/{totalSteps})");
                        await action();
                    }
                }

                LogTitle(rtbMigrationLog, "Migration terminée");
                if (doubleMigration)
                    LogSuccess(rtbMigrationLog,
                        $"Profils « {domainProfile!.Name} » puis « {cleanProfile!.Name} » migrés avec succès.");
                else
                    LogSuccess(rtbMigrationLog, $"Profil « {selectedProfile.Name} » migré avec succès.");

                if (errorList.Count > 0)
                {
                    LogTitle(rtbMigrationLog, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList)
                        LogWarning(rtbMigrationLog, err);
                }

                UpdateStatus("Migration terminée avec succès");
                ToastService.Show(this, "Migration terminée avec succès !", ToastKind.Success);

                MessageBox.Show(
                    "Migration terminée avec succès !\n\n" +
                    (doubleMigration
                        ? $"Profils migrés :\n  1. {domainProfile!.Name}\n  2. {cleanProfile!.Name}"
                        : $"Profil migré : {selectedProfile.Name}") +
                    $"\nSource : {selectedDrive.Letter}",
                    "Migration réussie",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                LogWarning(rtbMigrationLog, "Migration annulée par l'utilisateur.");
                UpdateStatus("Migration annulée");
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur migration : {ex.Message}");
                UpdateStatus("Erreur lors de la migration");

                MessageBox.Show(
                    $"Une erreur est survenue :\n{ex.Message}",
                    "Erreur de migration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        // ── Bouton Vérifier BitLocker ──────────────────────────────────────────────────────
        private async void BtnBitLocker_Click(object? sender, EventArgs e)
        {
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo selectedDrive)
            {
                MessageBox.Show(
                    "Veuillez d'abord sélectionner un lecteur dans la liste.",
                    "Aucun lecteur sélectionné",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (selectedDrive.BitLocker == BitLockerState.Locked)
            {
                OpenBitLockerControlPanel();
                await HandleBitLockerUnlockAsync(selectedDrive);
            }
            else
            {
                var root = selectedDrive.Letter + "\\";
                selectedDrive.BitLocker = GetBitLockerStatePowerShell(root);
                UpdateBitLockerLabel(selectedDrive);

                if (selectedDrive.BitLocker != BitLockerState.Locked && selectedDrive.HasUsers)
                    LoadProfiles(selectedDrive);
            }
        }

        /// <summary>
        /// Attend que le disque soit déverrouillé (polling toutes les 2 s, max 5 min)
        /// puis recharge automatiquement la liste des profils.
        /// </summary>
        private async Task HandleBitLockerUnlockAsync(USBDriveInfo drive)
        {
            const int pollIntervalMs = 2000;
            const int maxAttempts    = 150; // 5 minutes

            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(pollIntervalMs);

                var newState = GetBitLockerStatePowerShell(drive.Letter + "\\");

                if (newState != BitLockerState.Locked)
                {
                    drive.BitLocker = IsVolumeAccessible(drive.Letter + "\\")
                        ? newState
                        : BitLockerState.Locked;

                    if (drive.BitLocker != BitLockerState.Locked)
                    {
                        LoadUSBDrives();

                        for (int idx = 0; idx < cmbUSBDrives.Items.Count; idx++)
                        {
                            if (cmbUSBDrives.Items[idx] is USBDriveInfo d &&
                                d.Letter.Equals(drive.Letter, StringComparison.OrdinalIgnoreCase))
                            {
                                cmbUSBDrives.SelectedIndex = idx;
                                break;
                            }
                        }

                        Log(rtbMigrationLog, $"\U0001f513 {drive.Letter} déverrouillé — profils rechargés.");
                        return;
                    }
                }
            }

            Log(rtbMigrationLog, $"\u26a0\ufe0f Délai dépassé — {drive.Letter} toujours verrouillé.");
        }
    }
}
