using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SaveRestoreGUI
{
    public partial class MainForm
    {
        [GeneratedRegex(@"^[A-Z]:$", RegexOptions.CultureInvariant)]
        private static partial Regex DriveLetterRegex();

        // ── État BitLocker d'un lecteur ──────────────────────────────────────────
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
                    BitLockerState.Locked   => " \U0001f512 BitLocker verrouillé",
                    BitLockerState.Unlocked => " \U0001f513 BitLocker actif (déverrouillé)",
                    _                       => ""
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

        private UserProfileItem? _autoSelectedProfile;
        private List<UserProfileItem> _allLoadedProfiles = [];

        // ═══════════════════════════════════════════════════════════════════
        //  DÉTECTION DES LECTEURS
        // ═══════════════════════════════════════════════════════════════════

        [LibraryImport("kernel32.dll", EntryPoint = "GetDriveTypeW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint GetDriveType(string lpRootPathName);

        private static uint GetDriveTypeWin32(string root)
        {
            try { return GetDriveType(root); }
            catch { return 0; }
        }

        private static bool IsVolumeAccessible(string root)
        {
            try { Directory.GetDirectories(root); return true; }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }
            catch { return false; }
        }

        private void LoadUSBDrives()
        {
            if (cmbUSBDrives == null || lblBitLockerStatus == null || lblMigrationInfo == null || lblSelectedProfile == null)
                return;

            cmbUSBDrives.Items.Clear();
            _allLoadedProfiles.Clear();
            _autoSelectedProfile = null;
            lblBitLockerStatus.Text = "";

            try
            {
                var currentRoot = Path.GetPathRoot(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                    ?.TrimEnd(Path.DirectorySeparatorChar)
                    .ToUpperInvariant();

                var result      = new List<USBDriveInfo>();
                var seenLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                {
                    if (c == 'A' || c == 'B') continue;

                    var letter = $"{c}:";
                    var root   = letter + "\\";

                    if (currentRoot != null && letter.Equals(currentRoot, StringComparison.OrdinalIgnoreCase)) continue;

                    uint driveType = GetDriveTypeWin32(root);
                    if (driveType <= 1) continue;
                    if (driveType == 5 || driveType == 6) continue;

                    bool accessible = IsVolumeAccessible(root);

                    if (!accessible)
                    {
                        var bdeState   = GetBitLockerStatePowerShell(root);
                        var finalState = bdeState == BitLockerState.Unknown
                            ? BitLockerState.Locked
                            : bdeState;

                        if (finalState == BitLockerState.Locked)
                        {
                            result.Add(new USBDriveInfo
                            {
                                Letter    = letter,
                                Label     = "Volume verrouillé",
                                BitLocker = BitLockerState.Locked
                            });
                            seenLetters.Add(letter);
                        }
                        continue;
                    }

                    var usersPath   = Path.Combine(root, "Users");
                    var windowsPath = Path.Combine(root, "Windows");
                    if (!Directory.Exists(usersPath)) continue;

                    string volLabel = "Sans nom";
                    long   volSize  = 0;
                    try
                    {
                        var di = new DriveInfo(letter);
                        if (!string.IsNullOrEmpty(di.VolumeLabel)) volLabel = di.VolumeLabel;
                        volSize = di.TotalSize;
                    }
                    catch { }

                    var state = GetBitLockerStatePowerShell(root);

                    result.Add(new USBDriveInfo
                    {
                        Letter     = letter,
                        Label      = volLabel,
                        Size       = volSize,
                        HasUsers   = true,
                        HasWindows = Directory.Exists(windowsPath),
                        UsersPath  = usersPath,
                        BitLocker  = state
                    });
                    seenLetters.Add(letter);
                }

                foreach (var ltr in GetLockedBitLockerLetters(currentRoot ?? "C:"))
                {
                    if (seenLetters.Contains(ltr)) continue;
                    result.Add(new USBDriveInfo
                    {
                        Letter    = ltr,
                        Label     = "Volume verrouillé",
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
                LogError(MigrationLogBox, $"Erreur détection disque : {ex.Message}");
            }
        }

        // ── Helpers PowerShell BitLocker ─────────────────────────────────────────────
        private static BitLockerState GetBitLockerStatePowerShell(string drivePath)
        {
            try
            {
                var letter = drivePath.Replace("/", "\\").TrimEnd('\\').ToUpperInvariant();
                if (!letter.EndsWith(':')) letter += ":";

                if (!DriveLetterRegex().IsMatch(letter))
                    return BitLockerState.Unknown;

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
                    var mp   = line.Trim().TrimEnd('\\');
                    if (string.IsNullOrWhiteSpace(mp)) continue;
                    var root = mp.ToUpperInvariant();
                    if (root.Equals(excludeRoot, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!root.EndsWith(':')) continue;
                    letters.Add(root);
                }
            }
            catch { }
            return letters;
        }

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

        // ═══════════════════════════════════════════════════════════════════
        //  ÉVÉNEMENTS UI
        // ═══════════════════════════════════════════════════════════════════
        private void BtnRefreshUSB_Click(object? sender, EventArgs e)
        {
            LoadUSBDrives();
            Log(MigrationLogBox, "Liste des lecteurs actualisée.");
        }

        private void BtnCancelMigration_Click(object? sender, EventArgs e)
            => CancelCurrentOperation(MigrationLogBox);

        private void CmbUSBDrives_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _allLoadedProfiles.Clear();
            _autoSelectedProfile = null;
            lblSelectedProfile.Text = "";
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
            var p = ThemeManager.Palette;
            (lblBitLockerStatus.Text, lblBitLockerStatus.ForeColor) = drive.BitLocker switch
            {
                BitLockerState.Locked       => ($"\U0001f512 {drive.Letter} — BitLocker VERROUILLÉ",             p.Danger),
                BitLockerState.Unlocked     => ($"\U0001f513 {drive.Letter} — BitLocker actif (déverrouillé)",   p.Warning),
                BitLockerState.NotEncrypted => ($"\u2705 {drive.Letter} — Pas de chiffrement",                    p.Success),
                _                           => ($"\u2139\ufe0f {drive.Letter} — État BitLocker inconnu",           p.TextSecondary)
            };
        }

        private void LoadProfiles(USBDriveInfo drive)
        {
            _allLoadedProfiles.Clear();
            _autoSelectedProfile = null;
            try
            {
                var currentUsername = Environment.UserName;

                if (!Directory.Exists(drive.UsersPath))
                {
                    lblSelectedProfile.Text = "❌ Dossier Users introuvable sur ce lecteur.";
                    return;
                }

                var profiles = Directory.GetDirectories(drive.UsersPath)
                    .Select(p => new DirectoryInfo(p))
                    .Where(d => !ExcludedProfiles.Contains(d.Name, StringComparer.OrdinalIgnoreCase) && !d.Name.StartsWith('.'))
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

                _allLoadedProfiles.AddRange(profiles);

                var exactMatch  = profiles.FirstOrDefault(p =>
                    p.Name.Equals(currentUsername, StringComparison.OrdinalIgnoreCase));
                var domainMatch = profiles.FirstOrDefault(p =>
                    p.Name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase));

                _autoSelectedProfile = exactMatch ?? domainMatch ?? profiles.FirstOrDefault(p => p.IsMatch);

                if (_autoSelectedProfile != null)
                    lblSelectedProfile.Text = $"✅ Profil sélectionné : {_autoSelectedProfile.Name}\n{_autoSelectedProfile.Path}";
                else if (profiles.Count > 0)
                    lblSelectedProfile.Text = $"⚠️ Aucune correspondance pour « {currentUsername} ».\n" +
                        $"{profiles.Count} profil(s) disponible(s) : {string.Join(", ", profiles.Select(p => p.Name))}";
                else
                    lblSelectedProfile.Text = $"❌ Aucun profil utilisateur trouvé sur {drive.Letter}.";

                if (exactMatch != null && domainMatch != null && chkPanelMigration.IsChecked("OldProfile"))
                    lblMigrationInfo.Text =
                        $"⚠️ Double profil détecté ({domainMatch.Name} + {exactMatch.Name}).\n" +
                        "La migration copiera d'abord l'ancien profil domaine, puis le profil actuel.";
                else
                    lblMigrationInfo.Text = $"{profiles.Count} profil(s) trouvé(s) sur ce lecteur.";
            }
            catch (Exception ex)
            {
                LogError(MigrationLogBox, $"Erreur chargement profils : {ex.Message}");
                lblSelectedProfile.Text = "Erreur lors du chargement des profils.";
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CONSTRUCTION DE LA LISTE DES ÉTAPES DE MIGRATION
        // ═══════════════════════════════════════════════════════════════════

        private List<(string Name, Func<Task> Action)> BuildMigrationSteps(
            string sourceProfile,
            string destProfile,
            Progress<int> progress,
            List<string> errorList,
            USBDriveInfo selectedDrive,
            bool includePublic,
            CancellationToken ct)
        {
            var steps = new List<(string Name, Func<Task> Action)>();

            if (chkPanelMigration.IsChecked("Documents")) steps.Add(("Documents", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Documents"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Documents", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Desktop")) steps.Add(("Bureau", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Desktop"),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Bureau", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Downloads")) steps.Add(("Téléchargements", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Downloads"),
                Path.Combine(destProfile, "Downloads"),
                "Téléchargements", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Pictures")) steps.Add(("Images", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Pictures"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Images", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Music")) steps.Add(("Musique", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Music"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                "Musique", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Videos")) steps.Add(("Vidéos", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "Videos"),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "Vidéos", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Signatures")) steps.Add(("Signatures Outlook", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Signatures"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),
                "Signatures Outlook", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("OfficeTemplates")) steps.Add(("Modèles Office", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Templates"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                "Modèles Office", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("ExcelMacros")) steps.Add(("Macros Excel (XLSTART)", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Excel", "XLSTART"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                "Macros Excel (XLSTART)", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("Sap")) steps.Add(("SAP GUI", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Roaming", "SAP"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                "SAP GUI", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("BrowserEdge")) steps.Add(("Profil Edge", () => MigrateFolderStep(
                Path.Combine(sourceProfile, "AppData", "Local", "Microsoft", "Edge", "User Data", "Default"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data", "Default"),
                "Profil Edge", progress, errorList, ct)));

            if (chkPanelMigration.IsChecked("StickyNotes")) steps.Add(("Sticky Notes",
                () => MigrateStickyNotesAsync(sourceProfile, ct)));

            if (chkPanelMigration.IsChecked("Outlook")) steps.Add(("Données Outlook (PST)",
                () => MigrateOutlookAsync(sourceProfile, ct)));

            if (chkPanelMigration.IsChecked("OneNote")) steps.Add(("Clés registre OneNote",
                () => MigrateOneNoteAsync(sourceProfile)));

            if (chkPanelMigration.IsChecked("Wallpaper")) steps.Add(("Fond d'écran",
                () => MigrateWallpaperAsync(sourceProfile)));

            if (chkPanelMigration.IsChecked("NetworkDrives")) steps.Add(("Lecteurs réseau",
                () => MigrateNetworkDrivesAsync(sourceProfile)));

            if (chkPanelMigration.IsChecked("IpSoftphone")) steps.Add(("IP Desktop Softphone",
                () => MigrateIpDesktopSoftphoneAsync(sourceProfile, progress, errorList, ct)));

            if (includePublic && chkPanelMigration.IsChecked("Public")) steps.Add(("Dossier Public", () => MigrateFolderStep(
                Path.Combine(selectedDrive.Letter + "\\", "Users", "Public"),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                "Dossier Public", progress, errorList, ct)));

            return steps;
        }

        // ── Bouton Démarrer la migration ─────────────────────────────────────────────────
        private async void BtnStartMigration_Click(object? sender, EventArgs e)
        {
            if (_autoSelectedProfile is not UserProfileItem selectedProfile)
            {
                MessageBox.Show(
                    "Aucun profil correspondant à l'utilisateur actuel n'a été détecté.\n" +
                    "Vérifiez que le disque source contient bien un dossier Users.",
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

            var currentUsername = Environment.UserName;
            UserProfileItem? domainProfile = null;
            UserProfileItem? cleanProfile  = null;

            if (chkPanelMigration.IsChecked("OldProfile") && selectedDrive.HasUsers)
            {
                var allProfiles = _allLoadedProfiles;

                domainProfile = allProfiles.FirstOrDefault(p =>
                    p.Name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase));

                cleanProfile = allProfiles.FirstOrDefault(p =>
                    p.Name.Equals(currentUsername, StringComparison.OrdinalIgnoreCase));
            }

            bool doubleMigration = domainProfile != null && cleanProfile != null;

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

            MigrationLogBox.Clear();
            _ctsMigration = new CancellationTokenSource();
            var ct        = _ctsMigration.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var destProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var progress    = new Progress<int>(UpdateProgress);

                LogTitle(MigrationLogBox, "Démarrage de la migration");
                LogInfo(MigrationLogBox, $"Dest    : {destProfile}");
                LogInfo(MigrationLogBox, $"Utilisateur actuel : {currentUsername}");

                if (doubleMigration)
                {
                    // ── Passe 1 : NOM.DOMAINE ──────────────────────────────────────────
                    LogTitle(MigrationLogBox, $"Passe 1 — Ancien profil domaine : {domainProfile!.Name}");
                    LogInfo(MigrationLogBox, $"Source  : {domainProfile.Path}");

                    var steps1 = BuildMigrationSteps(
                        domainProfile.Path, destProfile, progress, errorList,
                        selectedDrive, includePublic: false, ct);

                    int total1 = steps1.Count, step1 = 0;
                    foreach (var (name, action) in steps1)
                    {
                        ct.ThrowIfCancellationRequested();
                        step1++;
                        UpdateStatus($"[Passe 1/2] Migration {name} ({step1}/{total1})");
                        await action();
                    }

                    LogSuccess(MigrationLogBox, $"Passe 1 terminée — profil « {domainProfile.Name} » copié.");

                    // ── Passe 2 : NOM ────────────────────────────────────────────────
                    LogTitle(MigrationLogBox, $"Passe 2 — Profil actuel : {cleanProfile!.Name}");
                    LogInfo(MigrationLogBox, $"Source  : {cleanProfile.Path}");

                    var steps2 = BuildMigrationSteps(
                        cleanProfile.Path, destProfile, progress, errorList,
                        selectedDrive, includePublic: true, ct);

                    int total2 = steps2.Count, step2 = 0;
                    foreach (var (name, action) in steps2)
                    {
                        ct.ThrowIfCancellationRequested();
                        step2++;
                        UpdateStatus($"[Passe 2/2] Migration {name} ({step2}/{total2})");
                        await action();
                    }

                    LogSuccess(MigrationLogBox, $"Passe 2 terminée — profil « {cleanProfile.Name} » copié.");
                }
                else
                {
                    // ── Migration simple (un seul profil) ─────────────────────────────
                    LogInfo(MigrationLogBox, $"Source  : {selectedProfile.Path}");

                    var steps = BuildMigrationSteps(
                        selectedProfile.Path, destProfile, progress, errorList,
                        selectedDrive, includePublic: true, ct);

                    int totalSteps = steps.Count, currentStep = 0;
                    foreach (var (name, action) in steps)
                    {
                        ct.ThrowIfCancellationRequested();
                        currentStep++;
                        UpdateStatus($"Migration {name} ({currentStep}/{totalSteps})");
                        await action();
                    }
                }

                LogTitle(MigrationLogBox, "Migration terminée");
                if (doubleMigration)
                    LogSuccess(MigrationLogBox,
                        $"Profils « {domainProfile!.Name} » puis « {cleanProfile!.Name} » migrés avec succès.");
                else
                    LogSuccess(MigrationLogBox, $"Profil « {selectedProfile.Name} » migré avec succès.");

                if (errorList.Count > 0)
                {
                    LogTitle(MigrationLogBox, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList)
                        LogWarning(MigrationLogBox, err);
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
                LogWarning(MigrationLogBox, "Migration annulée par l'utilisateur.");
                UpdateStatus("Migration annulée");
            }
            catch (Exception ex)
            {
                LogError(MigrationLogBox, $"Erreur migration : {ex.Message}");
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
                HideProgress();
                lock (_logLock) { _logFilePath = null; }
                _ctsMigration?.Dispose();
                _ctsMigration = null;
            }
        }

        // ── Bouton Vérifier BitLocker ──────────────────────────────────────────────────────────────
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

                        Log(MigrationLogBox, $"\U0001f513 {drive.Letter} déverrouillé — profils rechargés.");
                        return;
                    }
                }
            }

            Log(MigrationLogBox, $"\u26a0\ufe0f Délai dépassé — {drive.Letter} toujours verrouillé.");
        }
    }
}
