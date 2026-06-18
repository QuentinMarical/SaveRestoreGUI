using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;
namespace SaveRestoreGUI
{
    /// <summary>
    /// Logique de migration USB — détection des disques contenant Windows,
    /// énumération des profils utilisateurs, migration en mode fusion.
    /// </summary>
    public partial class MainForm
    {
        // ── État BitLocker d'un lecteur ─────────────────────────────────────────────
        private enum BitLockerState
        {
            Unknown,
            NotEncrypted,   // pas de chiffrement BitLocker
            Unlocked,       // chiffré MAIS accessible (déverrouillé)
            Locked,         // chiffré ET verrouillé (inaccessible)
        }

        private sealed class USBDriveInfo
        {
            public string Letter     { get; init; } = "";
            public string Label      { get; init; } = "";
            public long   Size       { get; init; }
            public bool   HasUsers   { get; init; }
            public bool   HasWindows { get; init; }
            public string UsersPath  { get; init; } = "";
            public BitLockerState BitLocker { get; set; } = BitLockerState.Unknown;

            public override string ToString()
            {
                var sizeStr = Size > 0 ? FileService.FormatSize(Size) : "Inconnu";
                var bde = BitLocker switch
                {
                    BitLockerState.Locked       => " 🔒 BitLocker verrouillé",
                    BitLockerState.Unlocked     => " 🔓 BitLocker actif (déverrouillé)",
                    _                           => ""
                };
                return $"{Letter} — {Label} ({sizeStr}){bde}";
            }
        }

        private sealed class UserProfileItem
        {
            public string Name    { get; init; } = " ";
            public string Path    { get; init; } = " ";
            public bool   IsMatch { get; init; }

            public override string ToString()
                => IsMatch ? $"★ {Name} (correspond à l'utilisateur actuel)" : Name;
        }

        // ───────────────────────────────────────────────────────────────────────────
        //  DÉTECTION DES LECTEURS
        //  Règle : tous les lecteurs NON-système qui possèdent un dossier Users
        //  à la racine. Les lecteurs BitLocker verrouillés sont aussi listés.
        // ───────────────────────────────────────────────────────────────────────────
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

                // 1. Lecteurs montés et accessibles
                foreach (var drive in DriveInfo.GetDrives()
                    .Where(d => d.DriveType is DriveType.Removable or DriveType.Fixed))
                {
                    var root = drive.Name.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
                    if (root == currentRoot) continue;
                    if (!drive.IsReady) continue;

                    var usersPath   = Path.Combine(drive.Name, "Users");
                    var windowsPath = Path.Combine(drive.Name, "Windows");
                    if (!Directory.Exists(usersPath)) continue;

                    var bdeState = GetBitLockerStatePowerShell(root + "\\");

                    result.Add(new USBDriveInfo
                    {
                        Letter     = root,
                        Label      = string.IsNullOrEmpty(drive.VolumeLabel) ? "Sans nom" : drive.VolumeLabel,
                        Size       = drive.TotalSize,
                        HasUsers   = true,
                        HasWindows = Directory.Exists(windowsPath),
                        UsersPath  = usersPath,
                        BitLocker  = bdeState
                    });
                }

                // 2. Lecteurs BitLocker VERROUILLÉS (non montés)
                foreach (var letter in GetLockedBitLockerLetters(currentRoot ?? "C:"))
                {
                    if (result.Any(d => d.Letter.Equals(letter, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    result.Add(new USBDriveInfo
                    {
                        Letter    = letter,
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
                        "Branchez le disque puis cliquez sur 🔄.";
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur détection disque : {ex.Message}");
            }
        }

        // ── Helpers PowerShell BitLocker (sans droits admin) ───────────────────────

        private static BitLockerState GetBitLockerStatePowerShell(string drivePath)
        {
            try
            {
                var letter = drivePath.TrimEnd('\\', '/').TrimEnd(Path.DirectorySeparatorChar);
                if (!letter.EndsWith(':')) letter = letter.TrimEnd(Path.DirectorySeparatorChar);

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
                    var mp = line.Trim().TrimEnd('\\');
                    if (string.IsNullOrWhiteSpace(mp)) continue;
                    var root = mp.ToUpperInvariant();
                    if (root.Equals(excludeRoot, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!root.EndsWith(':')) continue;
                    letters.Add(root);
                }
            }
            catch { /* module BitLocker absent */ }
            return letters;
        }

        private static (bool Success, string Message) UnlockBitLockerWithRecoveryKey(
            string driveLetter, string recoveryKey)
        {
            try
            {
                var letter = driveLetter.TrimEnd('\\');
                if (!letter.EndsWith(':')) letter += ":";

                var script =
                    $"$key = '{recoveryKey.Replace("'", "''")}'; " +
                    $"Unlock-BitLocker -MountPoint '{letter}' -RecoveryPassword $key -ErrorAction Stop; " +
                    "'OK'";

                var output = RunPowerShellInline(script);
                return output.Trim() == "OK"
                    ? (true, "Déverrouillage réussi.")
                    : (false, $"Réponse inattendue : {output.Trim()}");
            }
            catch (Exception ex) { return (false, ex.Message); }
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

        // ───────────────────────────────────────────────────────────────────────────

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
                    $"⚠️ {drive.Letter} est verrouillé par BitLocker.\n" +
                    "Cliquez sur 🔒 Vérifier BitLocker pour déverrouiller.";
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
                BitLockerState.Locked       => ($"🔒 {drive.Letter} — BitLocker VERROUILLÉ",  Color.OrangeRed),
                BitLockerState.Unlocked     => ($"🔓 {drive.Letter} — BitLocker actif (déverrouillé)", Color.DarkOrange),
                BitLockerState.NotEncrypted => ($"✅ {drive.Letter} — Pas de chiffrement",    Color.SeaGreen),
                _                           => ($"ℹ️ {drive.Letter} — État BitLocker inconnu", SystemColors.GrayText)
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

                var matching = profiles.FirstOrDefault(p => p.IsMatch);
                if (matching != null) lstProfiles.SelectedItem = matching;
                else if (lstProfiles.Items.Count > 0) lstProfiles.SelectedIndex = 0;

                lblMigrationInfo.Text = $"{profiles.Count} profil(s) trouvé(s). Sélectionnez le profil à migrer.";
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur chargement profils : {ex.Message}");
                lblMigrationInfo.Text = "Erreur lors du chargement des profils.";
            }
        }

        // ── Bouton BitLocker ────────────────────────────────────────────────────────
        private async void BtnBitLocker_Click(object? sender, EventArgs e)
        {
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo selectedDrive)
            {
                MessageBox.Show("Sélectionnez d'abord un lecteur dans la liste.",
                    "BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var driveLetter = selectedDrive.Letter.TrimEnd('\\');
            if (!driveLetter.EndsWith(':')) driveLetter += ":";

            btnBitLocker.Enabled    = false;
            lblBitLockerStatus.Text = "Vérification en cours…";
        // ─── Bouton BitLocker ───────────────────────────────────────────────────────
        /// <summary>
        /// Vérifie l'état BitLocker du disque sélectionné dans cmbUSBDrives
        /// (ou du disque système si aucun disque externe n'est sélectionné)
        /// via manage-bde -status, et affiche le résultat dans le log et dans
        /// lblBitLockerStatus.
        /// </summary>
        private async void BtnBitLocker_Click(object? sender, EventArgs e)
        {
            // Déterminer la lettre de lecteur à analyser
            string driveLetter;
            if (cmbUSBDrives.SelectedItem is USBDriveInfo selectedDrive)
            {
                driveLetter = selectedDrive.Letter.TrimEnd('\\', ':') + ":";
            }
            else
            {
                // Aucun disque externe : on analyse le disque système
                driveLetter = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                              ?.TrimEnd('\\') ?? "C:";
            }

            btnBitLocker.Enabled = false;
            lblBitLockerStatus.Text = "Analyse en cours…";
            LogTitle(rtbMigrationLog, $"BitLocker — {driveLetter}");

            try
            {
                var state = await Task.Run(() => GetBitLockerStatePowerShell(driveLetter + "\\"));
                selectedDrive.BitLocker = state;

                var idx = cmbUSBDrives.SelectedIndex;
                cmbUSBDrives.Items[idx]    = selectedDrive;
                cmbUSBDrives.SelectedIndex = idx;
                UpdateBitLockerLabel(selectedDrive);

                switch (state)
                {
                    case BitLockerState.NotEncrypted:
                        LogSuccess(rtbMigrationLog, $"{driveLetter} — Pas de chiffrement BitLocker actif.");
                        MessageBox.Show($"{driveLetter} n'est pas chiffré par BitLocker.",
                            "BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    case BitLockerState.Locked:
                        LogWarning(rtbMigrationLog, $"{driveLetter} est verrouillé par BitLocker.");
                        await HandleBitLockerUnlockAsync(selectedDrive, driveLetter);
                        break;

                    case BitLockerState.Unlocked:
                        LogWarning(rtbMigrationLog, $"{driveLetter} est chiffré (BitLocker actif, déverrouillé).");
                        MessageBox.Show(
                            $"{driveLetter} est chiffré mais déverrouillé.\n" +
                            "Vous pouvez migrer les données normalement.",
                            "BitLocker actif", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;

                    default:
                        LogWarning(rtbMigrationLog, $"{driveLetter} — état BitLocker indéterminé (module absent ?).");
                        break;
                var (output, error) = await Task.Run(() => RunManageBde(driveLetter));

                if (!string.IsNullOrWhiteSpace(error))
                {
                    LogWarning(rtbMigrationLog, $"manage-bde : {error.Trim()}");
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    lblBitLockerStatus.Text = "⚠️ Aucune réponse de manage-bde.";
                    LogWarning(rtbMigrationLog, "Aucune sortie de manage-bde. Vérifiez les droits administrateur.");
                    return;
                }

                // Afficher la sortie brute dans le log
                foreach (var line in output.Split('\n'))
                {
                    var l = line.TrimEnd('\r');
                    if (!string.IsNullOrWhiteSpace(l))
                        Log(rtbMigrationLog, $"  {l}");
                }

                // Extraire l'état de protection pour le label résumé
                var statusLine = output
                    .Split('\n')
                    .FirstOrDefault(l =>
                        l.Contains("Protection Status", StringComparison.OrdinalIgnoreCase) ||
                        l.Contains("État de la protection", StringComparison.OrdinalIgnoreCase) ||
                        l.Contains("Statut de la protection", StringComparison.OrdinalIgnoreCase));

                if (statusLine != null)
                {
                    var isProtected =
                        statusLine.Contains("Protection On", StringComparison.OrdinalIgnoreCase) ||
                        statusLine.Contains("Active", StringComparison.OrdinalIgnoreCase) ||
                        statusLine.Contains("Activé", StringComparison.OrdinalIgnoreCase);

                    lblBitLockerStatus.Text = isProtected
                        ? $"🔒 {driveLetter} — BitLocker ACTIVÉ"
                        : $"🔓 {driveLetter} — BitLocker désactivé";

                    if (isProtected)
                        LogWarning(rtbMigrationLog,
                            $"{driveLetter} est chiffré — déchiffrez le disque avant la migration.");
                    else
                        LogSuccess(rtbMigrationLog,
                            $"{driveLetter} — pas de chiffrement BitLocker actif.");
                }
                else
                {
                    lblBitLockerStatus.Text = $"ℹ️ {driveLetter} — état indéterminé (voir log)";
                }
            }
            catch (Exception ex)
            {
                lblBitLockerStatus.Text = "❌ Erreur lors de la vérification.";
                LogError(rtbMigrationLog, $"BitLocker : {ex.Message}");
            }
            finally
            {
                btnBitLocker.Enabled = true;
            }
        }

        private async Task HandleBitLockerUnlockAsync(USBDriveInfo drive, string driveLetter)
        {
            var answer = MessageBox.Show(
                $"Le lecteur {driveLetter} est verrouillé par BitLocker.\n\n" +
                "Voulez-vous le déverrouiller avec une clé de récupération (48 chiffres) ?",
                "BitLocker verrouillé",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes) return;

            using var dlg = new BitLockerKeyDialog(driveLetter);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var recoveryKey = dlg.RecoveryKey;
            if (string.IsNullOrWhiteSpace(recoveryKey)) return;

            btnBitLocker.Enabled    = false;
            lblBitLockerStatus.Text = $"🔓 Déverrouillage de {driveLetter}…";
            Log(rtbMigrationLog, $"Tentative de déverrouillage BitLocker de {driveLetter}…");

            var (success, message) = await Task.Run(() =>
                UnlockBitLockerWithRecoveryKey(driveLetter, recoveryKey));

            if (success)
            {
                LogSuccess(rtbMigrationLog, $"{driveLetter} — Déverrouillé avec succès !");
                MessageBox.Show(
                    $"{driveLetter} a été déverrouillé avec succès.\nLes profils vont être rechargés.",
                    "Déverrouillage réussi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadUSBDrives();
            }
            else
            {
                LogError(rtbMigrationLog, $"Échec du déverrouillage : {message}");
                MessageBox.Show(
                    $"Impossible de déverrouiller {driveLetter}.\n\nErreur : {message}\n\n" +
                    "Vérifiez que la clé est correcte (48 chiffres, groupes de 6 séparés par des tirets).",
                    "Échec du déverrouillage", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblBitLockerStatus.Text = $"🔒 {driveLetter} — Toujours verrouillé";
            }
        }

        // ── Migration ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Exécute manage-bde -status sur la lettre de lecteur indiquée
        /// et retourne (stdout, stderr).
        /// Nécessite des droits administrateur.
        /// </summary>
        private static (string Output, string Error) RunManageBde(string driveLetter)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "manage-bde.exe",
                Arguments              = $"-status {driveLetter}",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };

            using var proc = System.Diagnostics.Process.Start(psi)
                             ?? throw new InvalidOperationException("Impossible de démarrer manage-bde.");

            var output = proc.StandardOutput.ReadToEnd();
            var error  = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return (output, error);
        }

        // ─── Migration ──────────────────────────────────────────────────────────────

        private async void BtnStartMigration_Click(object? sender, EventArgs e)
        {
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive || !drive.HasUsers)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur contenant un dossier Users.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (drive.BitLocker == BitLockerState.Locked)
            {
                MessageBox.Show(
                    $"Le lecteur {drive.Letter} est verrouillé par BitLocker.\n" +
                    "Déverrouillez-le d'abord via le bouton 🔒 Vérifier BitLocker.",
                    "BitLocker verrouillé", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lstProfiles.SelectedItem is not UserProfileItem profile)
            {
                MessageBox.Show("Veuillez sélectionner un profil utilisateur à migrer.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Voulez-vous migrer les données du profil « {profile.Name} » vers le profil actuel ?\n\n" +
                $"Source : {profile.Path}\n" +
                $"Destination : {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\n\n" +
                "Mode fusion : les fichiers locaux plus récents seront conservés.",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            rtbMigrationLog.Clear();
            _cancellationTokenSource = new CancellationTokenSource();
            var ct        = _cancellationTokenSource.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _logFilePath = Path.Combine(userProfile, $"Migration_USB_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                var progress = new Progress<int>(UpdateProgress);

                LogTitle(rtbMigrationLog, "Migration depuis USB");
                LogInfo(rtbMigrationLog, $"Source : {profile.Path}");
                LogInfo(rtbMigrationLog, $"Destination : {userProfile}");

                var steps = new List<(string Name, Func<Task> Action)>();

                if (chkMigrateDocuments.Checked)      steps.Add(("Documents",             () => MigrateStep(Path.Combine(profile.Path, "Documents"),  Path.Combine(userProfile, "Documents"),  "Documents",             progress, ct, errorList)));
                if (chkMigrateDesktop.Checked)         steps.Add(("Bureau",                () => MigrateStep(Path.Combine(profile.Path, "Desktop"),   Path.Combine(userProfile, "Desktop"),   "Bureau",                progress, ct, errorList)));
                if (chkMigrateDownloads.Checked)       steps.Add(("Téléchargements",       () => MigrateStep(Path.Combine(profile.Path, "Downloads"), Path.Combine(userProfile, "Downloads"), "Téléchargements",       progress, ct, errorList)));
                if (chkMigratePictures.Checked)        steps.Add(("Images",                () => MigrateStep(Path.Combine(profile.Path, "Pictures"),  Path.Combine(userProfile, "Pictures"),  "Images",                progress, ct, errorList)));
                if (chkMigrateMusic.Checked)           steps.Add(("Musique",               () => MigrateStep(Path.Combine(profile.Path, "Music"),     Path.Combine(userProfile, "Music"),     "Musique",               progress, ct, errorList)));
                if (chkMigrateVideos.Checked)          steps.Add(("Vidéos",                () => MigrateStep(Path.Combine(profile.Path, "Videos"),    Path.Combine(userProfile, "Videos"),    "Vidéos",                progress, ct, errorList)));
                if (chkMigrateSignatures.Checked)      steps.Add(("Signatures Outlook",    () => MigrateStep(Path.Combine(profile.Path, "AppData", "Roaming", "Microsoft", "Signatures"),   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),             "Signatures Outlook",    progress, ct, errorList)));
                if (chkMigrateExcelMacros.Checked)     steps.Add(("Macros Excel",          () => MigrateStep(Path.Combine(profile.Path, "AppData", "Roaming", "Microsoft", "Excel", "XLSTART"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"), "Macros Excel",          progress, ct, errorList)));
                if (chkMigrateTemplates.Checked)       steps.Add(("Modèles Office",        () => MigrateStep(Path.Combine(profile.Path, "AppData", "Roaming", "Microsoft", "Templates"),   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),             "Modèles Office",        progress, ct, errorList)));
                if (chkMigrateSap.Checked)             steps.Add(("SAP GUI",               () => MigrateStep(Path.Combine(profile.Path, "AppData", "Roaming", "SAP"),                      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),                               "SAP GUI",               progress, ct, errorList)));
                if (chkMigratePublic.Checked)          steps.Add(("Dossier Public",        () => MigrateStep(Path.Combine(Path.GetPathRoot(profile.Path) ?? "", "Users", "Public"),        Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),                                                        "Dossier Public",        progress, ct, errorList)));
                if (chkMigrateOutlook.Checked)         steps.Add(("Données Outlook",       () => MigrateOutlookDataAsync(profile.Path, rtbMigrationLog, ct)));
                if (chkMigrateStickyNotes.Checked)     steps.Add(("Sticky Notes",          () => MigrateStickyNotesAsync(profile.Path, rtbMigrationLog, ct)));
                if (chkMigrateEdgeProfile.Checked)     steps.Add(("Profil Edge",           () => MigrateEdgeProfileAsync(profile.Path, rtbMigrationLog, progress, ct, errorList)));
                if (chkMigrateWallpaper.Checked)       steps.Add(("Fond d'écran",          () => MigrateWallpaperAsync(profile.Path, rtbMigrationLog)));
                if (chkMigrateNetworkDrives.Checked)   steps.Add(("Lecteurs réseau",       () => MigrateNetworkDrivesInfoAsync(profile.Path, rtbMigrationLog)));
                if (chkMigrateOneNote.Checked)         steps.Add(("OneNote (registre)",    () => MigrateOneNoteAsync(profile.Path, rtbMigrationLog)));

                int totalSteps  = steps.Count;
                int currentStep = 0;

                foreach (var (name, action) in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    currentStep++;
                    UpdateStatus($"Migration {name} ({currentStep}/{totalSteps})");
                    await action();
                }

                if (errorList.Count > 0)
                {
                    LogTitle(rtbMigrationLog, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList) LogWarning(rtbMigrationLog, err);
                }

                LogTitle(rtbMigrationLog, "Migration terminée");
                UpdateStatus("Migration terminée avec succès");
                ToastService.Show(this, "Migration terminée avec succès !", ToastKind.Success);
                MessageBox.Show("Migration terminée avec succès !", "Succès",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                LogWarning(rtbMigrationLog, "Migration annulée par l'utilisateur.");
                UpdateStatus("Migration annulée");
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur : {ex.Message}");
                UpdateStatus("Erreur lors de la migration");
                MessageBox.Show($"Erreur lors de la migration :\n{ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                HideProgress();
                _logFilePath = null;
            }
        }

        private async Task MigrateStep(string source, string destination, string name,
            IProgress<int> progress, CancellationToken ct, List<string> errorList)
        {
            if (!Directory.Exists(source)) { LogWarning(rtbMigrationLog, $"{name} : source introuvable."); return; }
            Log(rtbMigrationLog, $"Migration de {name}...");
            var r = await FileService.CopyFolderAsync(source, destination, progress, null, ct, mergeMode: true);
            foreach (var err in r.Errors) { LogError(rtbMigrationLog, $"Erreur copie {err}"); errorList.Add($"{name} : {err}"); }
            LogSuccess(rtbMigrationLog, $"{name} : {r.Copied} fichiers migrés, {r.Skipped} ignorés — {FileService.FormatSize(r.TotalBytes)}");
        }

        private async Task MigrateOutlookDataAsync(string sourceProfilePath, RichTextBox rtb, CancellationToken ct)
        {
            var pstLocations = new[]
            {
                Path.Combine(sourceProfilePath, "Documents", "Outlook Files"),
                Path.Combine(sourceProfilePath, "Documents", "Fichiers Outlook"),
                Path.Combine(sourceProfilePath, "AppData", "Local", "Microsoft", "Outlook")
            };

            var pstFiles = pstLocations
                .Where(Directory.Exists)
                .SelectMany(loc => Directory.GetFiles(loc, "*.pst"))
                .ToList();

            if (pstFiles.Count > 0)
            {
                var pstDest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Outlook Files");
                Directory.CreateDirectory(pstDest);
                Log(rtb, $"Migration de {pstFiles.Count} fichier(s) PST...");
                foreach (var pst in pstFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    var dest = Path.Combine(pstDest, Path.GetFileName(pst));
                    if (!File.Exists(dest))
                    {
                        try
                        {
                            var size = new FileInfo(pst).Length;
                            await Task.Run(() => File.Copy(pst, dest), ct);
                            LogSuccess(rtb, $"PST migré : {Path.GetFileName(pst)} ({FileService.FormatSize(size)})");
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}"); }
                    }
                    else LogInfo(rtb, $"PST déjà présent : {Path.GetFileName(pst)}");
                }
            }
            else LogInfo(rtb, "Aucun fichier PST trouvé.");

            var roamCacheSrc = Path.Combine(sourceProfilePath, "AppData", "Local", "Microsoft", "Outlook", "RoamCache");
            if (Directory.Exists(roamCacheSrc))
            {
                var roamCacheDst = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Outlook", "RoamCache");
                Directory.CreateDirectory(roamCacheDst);
                var files = Directory.GetFiles(roamCacheSrc, "Stream_Autocomplete_*.dat");
                foreach (var f in files)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Run(() => File.Copy(f, Path.Combine(roamCacheDst, Path.GetFileName(f)), true), ct);
                }
                if (files.Length > 0) LogSuccess(rtb, $"Cache d'autocomplétion migré ({files.Length} fichiers)");
            }
        }

        private async Task MigrateStickyNotesAsync(string sourceProfilePath, RichTextBox rtb, CancellationToken ct)
        {
            var src = Path.Combine(sourceProfilePath, "AppData", "Local", "Packages",
                "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState", "plum.sqlite");
            if (File.Exists(src))
            {
                var dst = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState");
                Directory.CreateDirectory(dst);
                await Task.Run(() => File.Copy(src, Path.Combine(dst, "plum.sqlite"), true), ct);
                LogSuccess(rtb, "Sticky Notes migrés");
            }
            else LogInfo(rtb, "Pas de Sticky Notes à migrer.");
        }

        private async Task MigrateEdgeProfileAsync(string sourceProfilePath, RichTextBox rtb,
            IProgress<int> progress, CancellationToken ct, List<string> errorList)
        {
            if (System.Diagnostics.Process.GetProcessesByName("msedge").Length > 0)
            { LogWarning(rtb, "Microsoft Edge est ouvert. Fermez-le avant de migrer le profil."); return; }

            var src = Path.Combine(sourceProfilePath, "AppData", "Local", "Microsoft", "Edge", "User Data", "Default");
            var dst = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default");
            await MigrateStep(src, dst, "Profil Edge", progress, ct, errorList);
        }

        private async Task MigrateWallpaperAsync(string sourceProfilePath, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                try
                {
                    var src = Path.Combine(sourceProfilePath, "AppData", "Roaming",
                        "Microsoft", "Windows", "Themes", "TranscodedWallpaper");
                    if (File.Exists(src))
                    {
                        var dst = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Microsoft", "Windows", "Themes");
                        Directory.CreateDirectory(dst);
                        File.Copy(src, Path.Combine(dst, "TranscodedWallpaper"), true);
                        LogSuccess(rtb, "Fond d'écran migré (visible après reconnexion)");
                    }
                    else LogInfo(rtb, "Pas de fond d'écran à migrer.");
                }
                catch (Exception ex) { LogError(rtb, $"Erreur fond d'écran : {ex.Message}"); }
            });
        }

        private async Task MigrateNetworkDrivesInfoAsync(string sourceProfilePath, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                var f = Path.Combine(sourceProfilePath, "NetworkDrives.txt");
                if (File.Exists(f))
                {
                    try
                    {
                        LogInfo(rtb, "Lecteurs réseau de l'ancien poste :");
                        foreach (var line in File.ReadAllLines(f).Where(l => !string.IsNullOrWhiteSpace(l)))
                            Log(rtb, $"   {line}");
                        LogWarning(rtb, "Merci de recréer manuellement ces lecteurs réseau.");
                    }
                    catch (Exception ex) { LogError(rtb, $"Erreur lecteurs réseau : {ex.Message}"); }
                }
                else LogInfo(rtb, "Pas de fichier de lecteurs réseau trouvé sur le profil source.");
            });
        }

        private async Task MigrateOneNoteAsync(string sourceProfilePath, RichTextBox rtb)
        {
            Log(rtb, "Migration des clés de registre OneNote...");
            await Task.Run(() =>
            {
                try   { RegistryService.RestoreOneNoteKeys(sourceProfilePath, msg => LogInfo(rtb, msg)); }
                catch (Exception ex) { LogError(rtb, $"Erreur OneNote : {ex.Message}"); }
            });
        }
    }
}
