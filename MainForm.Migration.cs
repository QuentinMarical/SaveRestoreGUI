using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Logique de migration USB — détection des disques contenant Windows,
    /// énumération des profils utilisateurs, migration en mode fusion.
    /// Supporte le déverrouillage BitLocker par clé de récupération.
    /// Le profil .ZEPRODBUR est toujours migré en premier (avant le profil courant).
    /// </summary>
    public partial class MainForm
    {
        // ── État BitLocker du disque sélectionné ──────────────────────────────
        private bool _isDriveBitLockerLocked = false;

        private sealed class USBDriveInfo
        {
            public string Letter { get; init; } = "";
            public string Label  { get; init; } = "";
            public long   Size   { get; init; }
            public bool   HasWindows { get; init; }
            public string UsersPath  { get; init; } = "";

            public override string ToString()
            {
                var sizeStr = Size > 0 ? FileService.FormatSize(Size) : "Inconnu";
                var status  = HasWindows ? "✓ Windows détecté" : "✗ Pas de Windows";
                return $"{Letter} — {Label} ({sizeStr}) — {status}";
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

        // ══════════════════════════════════════════════════════════════════════
        //  DÉTECTION DISQUES
        // ══════════════════════════════════════════════════════════════════════

        private void LoadUSBDrives()
        {
            cmbUSBDrives.Items.Clear();
            lstProfiles.Items.Clear();
            SetBitLockerButtonState(false);

            try
            {
                var usbDrives  = new List<USBDriveInfo>();
                var allDrives  = DriveInfo.GetDrives()
                    .Where(d => (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed) && d.IsReady)
                    .ToList();

                var currentWindows = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                    .TrimEnd(Path.DirectorySeparatorChar);
                var currentRoot = Path.GetPathRoot(currentWindows)?.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();

                // Chercher aussi les disques verrouillés par BitLocker (non montés)
                var lockedLetters = GetBitLockerLockedDrives();

                // Disques accessibles avec Windows
                foreach (var drive in allDrives)
                {
                    var windowsPath = Path.Combine(drive.Name, "Windows");
                    var usersPath   = Path.Combine(drive.Name, "Users");
                    var hasWindows  = Directory.Exists(windowsPath) && Directory.Exists(usersPath);

                    var driveRoot = drive.Name.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
                    if (driveRoot == currentRoot)
                        continue;

                    if (!hasWindows)
                        continue;

                    usbDrives.Add(new USBDriveInfo
                    {
                        Letter     = drive.Name.TrimEnd('\\'),
                        Label      = string.IsNullOrEmpty(drive.VolumeLabel) ? "Sans nom" : drive.VolumeLabel,
                        Size       = drive.TotalSize,
                        HasWindows = hasWindows,
                        UsersPath  = usersPath
                    });
                }

                // Disques verrouillés BitLocker (montés mais non lisibles)
                foreach (var letter in lockedLetters)
                {
                    var root = letter.TrimEnd('\\') + "\\";
                    if (usbDrives.Any(d => d.Letter.Equals(letter.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                        continue;
                    // Vérifier que ce n'est pas le disque courant
                    var driveRoot = letter.TrimEnd('\\', Path.DirectorySeparatorChar).ToUpperInvariant();
                    if (driveRoot == currentRoot)
                        continue;

                    usbDrives.Add(new USBDriveInfo
                    {
                        Letter     = letter.TrimEnd('\\'),
                        Label      = $"🔒 BitLocker verrouillé",
                        Size       = 0,
                        HasWindows = false,   // inconnu tant que verrouillé
                        UsersPath  = ""
                    });
                }

                foreach (var drive in usbDrives)
                    cmbUSBDrives.Items.Add(drive);

                if (cmbUSBDrives.Items.Count > 0)
                {
                    cmbUSBDrives.SelectedIndex = 0;
                }
                else
                {
                    lblMigrationInfo.Text = "Aucun disque externe contenant Windows n'a été détecté.\nBranchez le disque puis cliquez sur 🔄.";
                }
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur détection disque : {ex.Message}");
            }
        }

        private void BtnRefreshUSB_Click(object? sender, EventArgs e)
        {
            LoadUSBDrives();
            Log(rtbMigrationLog, "Liste des lecteurs actualisée.");
        }

        private void CmbUSBDrives_SelectedIndexChanged(object? sender, EventArgs e)
        {
            lstProfiles.Items.Clear();
            SetBitLockerButtonState(false);

            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive)
                return;

            // Vérifier l'état BitLocker du disque sélectionné
            var bitlockerStatus = GetDriveBitLockerStatus(drive.Letter);
            _isDriveBitLockerLocked = bitlockerStatus == BitLockerStatus.Locked;

            if (_isDriveBitLockerLocked)
            {
                SetBitLockerButtonState(true);  // fait clignoter le bouton
                lblMigrationInfo.Text = "🔒 Ce disque est verrouillé par BitLocker.\nCliquez sur le bouton 🔒 pour le déverrouiller.";
                return;
            }

            if (!drive.HasWindows)
            {
                lblMigrationInfo.Text = "Ce lecteur ne contient pas d'installation Windows valide.\nVeuillez sélectionner un autre lecteur.";
                return;
            }

            LoadProfilesForDrive(drive);
        }

        private void LoadProfilesForDrive(USBDriveInfo drive)
        {
            lstProfiles.Items.Clear();
            lblMigrationInfo.Text = "Chargement des profils...";

            try
            {
                var excludedFolders = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };
                var currentUsername = Environment.UserName;

                if (Directory.Exists(drive.UsersPath))
                {
                    var profiles = Directory.GetDirectories(drive.UsersPath)
                        .Select(p => new DirectoryInfo(p))
                        .Where(d => !excludedFolders.Contains(d.Name, StringComparer.OrdinalIgnoreCase)
                                    && !d.Name.StartsWith('.'))
                        .Where(d =>
                        {
                            var hasDocuments = Directory.Exists(Path.Combine(d.FullName, "Documents"));
                            var hasDesktop   = Directory.Exists(Path.Combine(d.FullName, "Desktop"));
                            return hasDocuments || hasDesktop;
                        })
                        .Select(d =>
                        {
                            var baseUsername = d.Name.Contains('.') ? d.Name[..d.Name.LastIndexOf('.')] : d.Name;
                            var isMatch = d.Name.Equals(currentUsername, StringComparison.OrdinalIgnoreCase)
                                          || baseUsername.Equals(currentUsername, StringComparison.OrdinalIgnoreCase)
                                          || d.Name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase);

                            return new UserProfileItem
                            {
                                Name    = d.Name,
                                Path    = d.FullName,
                                IsMatch = isMatch
                            };
                        })
                        .OrderByDescending(p => p.IsMatch)
                        .ThenBy(p => p.Name)
                        .ToList();

                    foreach (var profile in profiles)
                        lstProfiles.Items.Add(profile);

                    var matching = profiles.FirstOrDefault(p => p.IsMatch);
                    if (matching != null)
                        lstProfiles.SelectedItem = matching;
                    else if (lstProfiles.Items.Count > 0)
                        lstProfiles.SelectedIndex = 0;

                    lblMigrationInfo.Text = $"{profiles.Count} profil(s) utilisateur trouvé(s). Sélectionnez le profil à migrer.";
                }
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"Erreur chargement profils : {ex.Message}");
                lblMigrationInfo.Text = "Erreur lors du chargement des profils.";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BITLOCKER — Détection, déverrouillage, bouton clignotant
        // ══════════════════════════════════════════════════════════════════════

        private enum BitLockerStatus { NotEncrypted, Unlocked, Locked, Unknown }

        /// <summary>Retourne la liste des lettres de lecteurs verrouillés par BitLocker.</summary>
        private static List<string> GetBitLockerLockedDrives()
        {
            var locked = new List<string>();
            try
            {
                var result = RunManageBde("-status", "");
                // Chercher les volumes verrouillés dans la sortie globale
                var lines = result.Split('\n');
                string? currentVolume = null;
                bool isLocked = false;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    // Ligne de volume : "Volume C:" ou "BitLocker Drive Encryption: Volume C:"
                    var volMatch = Regex.Match(trimmed, @"Volume\s+([A-Z]:)", RegexOptions.IgnoreCase);
                    if (volMatch.Success)
                    {
                        if (currentVolume != null && isLocked)
                            locked.Add(currentVolume);
                        currentVolume = volMatch.Groups[1].Value;
                        isLocked = false;
                    }
                    if (trimmed.Contains("Locked", StringComparison.OrdinalIgnoreCase) &&
                        trimmed.Contains("Protection", StringComparison.OrdinalIgnoreCase) == false)
                        isLocked = true;
                    // Ligne status : "    Lock Status:  Locked"
                    if (Regex.IsMatch(trimmed, @"Lock Status.*Locked", RegexOptions.IgnoreCase))
                        isLocked = true;
                }
                if (currentVolume != null && isLocked)
                    locked.Add(currentVolume);
            }
            catch { /* manage-bde peut ne pas être disponible */ }
            return locked;
        }

        /// <summary>Retourne le statut BitLocker d'un lecteur spécifique.</summary>
        private static BitLockerStatus GetDriveBitLockerStatus(string driveLetter)
        {
            try
            {
                var letter = driveLetter.TrimEnd('\\');
                if (!letter.EndsWith(':'))
                    letter += ':';

                var output = RunManageBde($"-status {letter}", "");
                if (string.IsNullOrWhiteSpace(output))
                    return BitLockerStatus.NotEncrypted;

                // Si le disque n'est pas chiffré
                if (output.Contains("not recognized", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("No BitLocker", StringComparison.OrdinalIgnoreCase))
                    return BitLockerStatus.NotEncrypted;

                // Verrouillé
                if (Regex.IsMatch(output, @"Lock Status[^\n]*Locked", RegexOptions.IgnoreCase))
                    return BitLockerStatus.Locked;

                // Déverrouillé/actif
                if (Regex.IsMatch(output, @"Lock Status[^\n]*Unlocked", RegexOptions.IgnoreCase) ||
                    output.Contains("Percentage Encrypted", StringComparison.OrdinalIgnoreCase))
                    return BitLockerStatus.Unlocked;

                return BitLockerStatus.Unknown;
            }
            catch
            {
                return BitLockerStatus.Unknown;
            }
        }

        /// <summary>Récupère l'ID BitLocker (Key ID) du disque pour l'afficher à l'utilisateur.</summary>
        private static string GetBitLockerKeyId(string driveLetter)
        {
            try
            {
                var letter = driveLetter.TrimEnd('\\');
                if (!letter.EndsWith(':')) letter += ':';
                var output = RunManageBde($"-status {letter}", "");
                // "Key Identifier: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
                var match = Regex.Match(output, @"Key Identifier[:\s]+([0-9A-Fa-f\-]{36})");
                if (match.Success)
                    return match.Groups[1].Value;
                // Format FR : "Identificateur de clé :"
                match = Regex.Match(output, @"Identificateur de cl[eé][^:]*:[\s]+([0-9A-Fa-f\-]{36})", RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }
            catch { }
            return "(ID non disponible)";
        }

        /// <summary>Exécute manage-bde et retourne la sortie standard.</summary>
        private static string RunManageBde(string args, string input)
        {
            try
            {
                using var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName               = "manage-bde.exe",
                        Arguments              = args,
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        CreateNoWindow         = true,
                        Verb                   = "runas"   // nécessite élevation
                    }
                };
                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(10_000);
                return output;
            }
            catch
            {
                return "";
            }
        }

        // ── Bouton clignotant ──────────────────────────────────────────────────

        private void SetBitLockerButtonState(bool blinking)
        {
            if (blinking)
            {
                btnUnlockBitLocker.Visible = true;
                btnUnlockBitLocker.Enabled = true;
                _bitlockerBlinkTimer.Start();
            }
            else
            {
                _bitlockerBlinkTimer.Stop();
                btnUnlockBitLocker.Visible = false;
                btnUnlockBitLocker.BackColor = Color.Empty;
            }
        }

        private void BitLockerBlinkTimer_Tick(object? sender, EventArgs e)
        {
            // Alterne entre orange vif et transparent pour attirer l'attention
            btnUnlockBitLocker.BackColor = btnUnlockBitLocker.BackColor == Color.OrangeRed
                ? Color.Transparent
                : Color.OrangeRed;
        }

        // ── Clic déverrouillage ────────────────────────────────────────────────

        private async void BtnUnlockBitLocker_Click(object? sender, EventArgs e)
        {
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive)
                return;

            var driveLetter = drive.Letter.TrimEnd('\\');
            if (!driveLetter.EndsWith(':')) driveLetter += ':';

            // Récupérer l'ID BitLocker pour l'afficher à l'utilisateur
            var keyId = await Task.Run(() => GetBitLockerKeyId(driveLetter));

            // Afficher le prompt avec l'ID BitLocker
            using var dlg = new BitLockerUnlockDialog(driveLetter, keyId);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var recoveryKey = dlg.RecoveryKey.Trim();
            if (string.IsNullOrWhiteSpace(recoveryKey))
            {
                MessageBox.Show("Clé de récupération vide.", "Annulé",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblMigrationInfo.Text = $"Déverrouillage de {driveLetter} en cours…";
            btnUnlockBitLocker.Enabled = false;

            try
            {
                var unlocked = await Task.Run(() => UnlockBitLockerDrive(driveLetter, recoveryKey));

                if (unlocked)
                {
                    SetBitLockerButtonState(false);
                    _isDriveBitLockerLocked = false;
                    LogSuccess(rtbMigrationLog, $"BitLocker : disque {driveLetter} déverrouillé avec succès.");

                    // Recharger la liste des disques pour mettre à jour l'état
                    LoadUSBDrives();

                    // Essayer de re-sélectionner le disque déverrouillé
                    for (int i = 0; i < cmbUSBDrives.Items.Count; i++)
                    {
                        if (cmbUSBDrives.Items[i] is USBDriveInfo d &&
                            d.Letter.Equals(drive.Letter, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbUSBDrives.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    LogError(rtbMigrationLog, $"BitLocker : échec du déverrouillage — clé de récupération incorrecte ou erreur.");
                    lblMigrationInfo.Text = "Échec du déverrouillage. Vérifiez la clé de récupération.";
                    btnUnlockBitLocker.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                LogError(rtbMigrationLog, $"BitLocker : erreur — {ex.Message}");
                btnUnlockBitLocker.Enabled = true;
            }
        }

        /// <summary>Tente de déverrouiller le disque avec la clé de récupération.</summary>
        private static bool UnlockBitLockerDrive(string driveLetter, string recoveryKey)
        {
            try
            {
                using var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName               = "manage-bde.exe",
                        Arguments              = $"-unlock {driveLetter} -RecoveryPassword {recoveryKey}",
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        CreateNoWindow         = true
                    }
                };
                proc.Start();
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(30_000);
                return proc.ExitCode == 0 ||
                       output.Contains("successfully", StringComparison.OrdinalIgnoreCase) ||
                       output.Contains("réussi", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MIGRATION PRINCIPALE
        // ══════════════════════════════════════════════════════════════════════

        private async void BtnStartMigration_Click(object? sender, EventArgs e)
        {
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive || !drive.HasWindows)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur contenant une installation Windows.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isDriveBitLockerLocked)
            {
                MessageBox.Show("Le disque est verrouillé par BitLocker. Déverrouillez-le avant de démarrer la migration.",
                    "BitLocker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            if (result != DialogResult.Yes)
                return;

            rtbMigrationLog.Clear();
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;
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

                // ── Construire la liste ordonnée des profils à migrer ──────────
                // Règle : d'abord le profil .ZEPRODBUR (ou tout profil avec un suffixe de domaine),
                // puis le profil sélectionné.
                var profilesToMigrate = BuildOrderedProfileList(drive.UsersPath, profile, Environment.UserName);

                foreach (var sourceProfile in profilesToMigrate)
                {
                    LogTitle(rtbMigrationLog, $"=== Profil source : {sourceProfile.Name} ===");

                    var steps = BuildMigrationSteps(sourceProfile.Path, userProfile, progress, ct, errorList);

                    int totalSteps  = steps.Count;
                    int currentStep = 0;

                    foreach (var (name, action) in steps)
                    {
                        ct.ThrowIfCancellationRequested();
                        currentStep++;
                        UpdateStatus($"Migration {name} ({currentStep}/{totalSteps}) — {sourceProfile.Name}");
                        await action();
                    }
                }

                if (errorList.Count > 0)
                {
                    LogTitle(rtbMigrationLog, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList)
                        LogWarning(rtbMigrationLog, err);
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

        // ══════════════════════════════════════════════════════════════════════
        //  ORDRE DES PROFILS : .ZEPRODBUR d'abord, puis profil courant
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Construit la liste ordonnée des profils à migrer :
        ///   1. Profil .ZEPRODBUR (ou *.DOMAINE) correspondant à l'utilisateur courant — migration en premier
        ///   2. Profil principal sélectionné
        /// </summary>
        private static List<UserProfileItem> BuildOrderedProfileList(
            string usersPath, UserProfileItem selectedProfile, string currentUsername)
        {
            var ordered = new List<UserProfileItem>();

            try
            {
                if (!Directory.Exists(usersPath))
                    return new List<UserProfileItem> { selectedProfile };

                var excluded = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };

                // Chercher un profil domaine (ex: dupont.ZEPRODBUR) qui correspond à l'utilisateur courant
                // et qui est DIFFÉRENT du profil sélectionné
                var domainProfiles = Directory.GetDirectories(usersPath)
                    .Select(p => new DirectoryInfo(p))
                    .Where(d => !excluded.Contains(d.Name, StringComparer.OrdinalIgnoreCase)
                                && !d.Name.StartsWith('.')
                                && d.Name.Contains('.'))   // contient un suffixe domaine
                    .Where(d =>
                    {
                        // Le préfixe (avant le point) correspond-il au nom courant ?
                        var baseName = d.Name[..d.Name.IndexOf('.')];
                        return baseName.Equals(currentUsername, StringComparison.OrdinalIgnoreCase);
                    })
                    .Where(d => !d.FullName.Equals(selectedProfile.Path, StringComparison.OrdinalIgnoreCase))
                    .Where(d => Directory.Exists(Path.Combine(d.FullName, "Documents"))
                             || Directory.Exists(Path.Combine(d.FullName, "Desktop")))
                    .Select(d => new UserProfileItem
                    {
                        Name    = d.Name,
                        Path    = d.FullName,
                        IsMatch = false
                    })
                    .ToList();

                // Ajouter d'abord les profils domaine (ex: .ZEPRODBUR)
                ordered.AddRange(domainProfiles);
            }
            catch { /* Ignorer les erreurs d'accès */ }

            // Toujours ajouter le profil sélectionné en dernier
            // (ses données écrasent/complètent le profil domaine)
            if (!ordered.Any(p => p.Path.Equals(selectedProfile.Path, StringComparison.OrdinalIgnoreCase)))
                ordered.Add(selectedProfile);

            return ordered;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CONSTRUCTION DES ÉTAPES DE MIGRATION
        // ══════════════════════════════════════════════════════════════════════

        private List<(string Name, Func<Task> Action)> BuildMigrationSteps(
            string sourceProfilePath,
            string userProfile,
            IProgress<int> progress,
            CancellationToken ct,
            List<string> errorList)
        {
            var steps = new List<(string Name, Func<Task> Action)>();

            if (chkMigrateDocuments.Checked) steps.Add(("Documents", () => MigrateStep(
                Path.Combine(sourceProfilePath, "Documents"), Path.Combine(userProfile, "Documents"),
                "Documents", progress, ct, errorList)));

            if (chkMigrateDesktop.Checked) steps.Add(("Bureau", () => MigrateStep(
                Path.Combine(sourceProfilePath, "Desktop"), Path.Combine(userProfile, "Desktop"),
                "Bureau", progress, ct, errorList)));

            if (chkMigrateDownloads.Checked) steps.Add(("Téléchargements", () => MigrateStep(
                Path.Combine(sourceProfilePath, "Downloads"), Path.Combine(userProfile, "Downloads"),
                "Téléchargements", progress, ct, errorList)));

            if (chkMigratePictures.Checked) steps.Add(("Images", () => MigrateStep(
                Path.Combine(sourceProfilePath, "Pictures"), Path.Combine(userProfile, "Pictures"),
                "Images", progress, ct, errorList)));

            if (chkMigrateMusic.Checked) steps.Add(("Musique", () => MigrateStep(
                Path.Combine(sourceProfilePath, "Music"), Path.Combine(userProfile, "Music"),
                "Musique", progress, ct, errorList)));

            if (chkMigrateVideos.Checked) steps.Add(("Vidéos", () => MigrateStep(
                Path.Combine(sourceProfilePath, "Videos"), Path.Combine(userProfile, "Videos"),
                "Vidéos", progress, ct, errorList)));

            if (chkMigrateSignatures.Checked) steps.Add(("Signatures Outlook", () => MigrateStep(
                Path.Combine(sourceProfilePath, "AppData", "Roaming", "Microsoft", "Signatures"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),
                "Signatures Outlook", progress, ct, errorList)));

            if (chkMigrateExcelMacros.Checked) steps.Add(("Macros Excel (XLSTART)", () => MigrateStep(
                Path.Combine(sourceProfilePath, "AppData", "Roaming", "Microsoft", "Excel", "XLSTART"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                "Macros Excel (XLSTART)", progress, ct, errorList)));

            if (chkMigrateTemplates.Checked) steps.Add(("Modèles Office", () => MigrateStep(
                Path.Combine(sourceProfilePath, "AppData", "Roaming", "Microsoft", "Templates"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                "Modèles Office", progress, ct, errorList)));

            if (chkMigrateSap.Checked) steps.Add(("SAP GUI", () => MigrateStep(
                Path.Combine(sourceProfilePath, "AppData", "Roaming", "SAP"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                "SAP GUI", progress, ct, errorList)));

            if (chkMigratePublic.Checked) steps.Add(("Dossier Public", () => MigrateStep(
                Path.Combine(Path.GetPathRoot(sourceProfilePath) ?? "", "Users", "Public"),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                "Dossier Public", progress, ct, errorList)));

            if (chkMigrateOutlook.Checked)      steps.Add(("Données Outlook",  () => MigrateOutlookDataAsync(sourceProfilePath, rtbMigrationLog, ct)));
            if (chkMigrateStickyNotes.Checked)  steps.Add(("Sticky Notes",      () => MigrateStickyNotesAsync(sourceProfilePath, rtbMigrationLog, ct)));
            if (chkMigrateEdgeProfile.Checked)  steps.Add(("Profil Edge",       () => MigrateEdgeProfileAsync(sourceProfilePath, rtbMigrationLog, progress, ct, errorList)));
            if (chkMigrateWallpaper.Checked)    steps.Add(("Fond d'écran",      () => MigrateWallpaperAsync(sourceProfilePath, rtbMigrationLog)));
            if (chkMigrateNetworkDrives.Checked) steps.Add(("Lecteurs réseau",  () => MigrateNetworkDrivesInfoAsync(sourceProfilePath, rtbMigrationLog)));
            if (chkMigrateOneNote.Checked)      steps.Add(("OneNote (registre)",() => MigrateOneNoteAsync(sourceProfilePath, rtbMigrationLog)));

            // IP Desktop Softphone — réservé, non fonctionnel pour l'instant
            // if (chkMigrateIpDesktopSoftphone.Checked) steps.Add(("IP Softphone", () => MigrateIpDesktopSoftphoneAsync(sourceProfilePath, rtbMigrationLog, progress, ct, errorList)));

            return steps;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ÉTAPES ATOMIQUES
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Étape de migration en mode fusion (les fichiers destination plus récents sont conservés).</summary>
        private async Task MigrateStep(string source, string destination, string name,
            IProgress<int> progress, CancellationToken ct, List<string> errorList)
        {
            if (!Directory.Exists(source))
            {
                LogWarning(rtbMigrationLog, $"{name} : source introuvable.");
                return;
            }

            Log(rtbMigrationLog, $"Migration de {name}...");
            var result = await FileService.CopyFolderAsync(source, destination, progress, null, ct, mergeMode: true);

            foreach (var err in result.Errors)
            {
                LogError(rtbMigrationLog, $"Erreur copie {err}");
                errorList.Add($"{name} : {err}");
            }

            LogSuccess(rtbMigrationLog, $"{name} : {result.Copied} fichiers migrés, {result.Skipped} ignorés — {FileService.FormatSize(result.TotalBytes)}");
        }

        /// <summary>Migration des données Outlook depuis le profil source (PST + autocomplete).</summary>
        private async Task MigrateOutlookDataAsync(string sourceProfilePath, RichTextBox rtb, CancellationToken ct)
        {
            var pstLocations = new List<string>
            {
                Path.Combine(sourceProfilePath, "Documents", "Outlook Files"),
                Path.Combine(sourceProfilePath, "Documents", "Fichiers Outlook"),
                Path.Combine(sourceProfilePath, "AppData", "Local", "Microsoft", "Outlook")
            };

            var pstFiles = new List<string>();
            foreach (var loc in pstLocations)
            {
                if (Directory.Exists(loc))
                    pstFiles.AddRange(Directory.GetFiles(loc, "*.pst"));
            }

            if (pstFiles.Count > 0)
            {
                var pstDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Outlook Files");
                Directory.CreateDirectory(pstDest);

                Log(rtb, $"Migration de {pstFiles.Count} fichier(s) PST...");
                foreach (var pst in pstFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    var destPath = Path.Combine(pstDest, Path.GetFileName(pst));
                    if (!File.Exists(destPath))
                    {
                        try
                        {
                            var size = new FileInfo(pst).Length;
                            await Task.Run(() => File.Copy(pst, destPath), ct);
                            LogSuccess(rtb, $"PST migré : {Path.GetFileName(pst)} ({FileService.FormatSize(size)})");
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}"); }
                    }
                    else
                    {
                        LogInfo(rtb, $"PST déjà présent : {Path.GetFileName(pst)}");
                    }
                }
            }
            else
            {
                LogInfo(rtb, "Aucun fichier PST trouvé.");
            }

            var roamCacheSource = Path.Combine(sourceProfilePath, "AppData", "Local", "Microsoft", "Outlook", "RoamCache");
            if (Directory.Exists(roamCacheSource))
            {
                var roamCacheDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Outlook", "RoamCache");
                Directory.CreateDirectory(roamCacheDest);

                var autocompleteFiles = Directory.GetFiles(roamCacheSource, "Stream_Autocomplete_*.dat");
                foreach (var file in autocompleteFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Run(() => File.Copy(file, Path.Combine(roamCacheDest, Path.GetFileName(file)), true), ct);
                }
                if (autocompleteFiles.Length > 0)
                    LogSuccess(rtb, $"Cache d'autocomplétion migré ({autocompleteFiles.Length} fichiers)");
            }
        }

        /// <summary>Migration des Sticky Notes (plum.sqlite).</summary>
        private async Task MigrateStickyNotesAsync(string sourceProfilePath, RichTextBox rtb, CancellationToken ct)
        {
            var stickySource = Path.Combine(sourceProfilePath, "AppData", "Local", "Packages",
                "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState", "plum.sqlite");

            if (File.Exists(stickySource))
            {
                var stickyDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState");
                Directory.CreateDirectory(stickyDest);

                await Task.Run(() => File.Copy(stickySource, Path.Combine(stickyDest, "plum.sqlite"), true), ct);
                LogSuccess(rtb, "Sticky Notes migrés");
            }
            else
            {
                LogInfo(rtb, "Pas de Sticky Notes à migrer.");
            }
        }

        /// <summary>
        /// Profil Edge complet : copie le dossier «Default» du profil source
        /// vers User Data\Default du profil courant, en mode fusion.
        /// ⚠️ Edge doit être fermé pendant la migration.
        /// </summary>
        private async Task MigrateEdgeProfileAsync(string sourceProfilePath, RichTextBox rtb,
            IProgress<int> progress, CancellationToken ct, List<string> errorList)
        {
            if (System.Diagnostics.Process.GetProcessesByName("msedge").Length > 0)
            {
                LogWarning(rtb, "Microsoft Edge est ouvert. Fermez-le avant de migrer le profil.");
                return;
            }

            var edgeSource = Path.Combine(sourceProfilePath, "AppData", "Local", "Microsoft", "Edge", "User Data", "Default");
            var edgeDest   = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default");

            await MigrateStep(edgeSource, edgeDest, "Profil Edge", progress, ct, errorList);
        }

        /// <summary>Migration du fond d'écran.</summary>
        private async Task MigrateWallpaperAsync(string sourceProfilePath, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                try
                {
                    var wallpaperPath = Path.Combine(sourceProfilePath, "AppData", "Roaming", "Microsoft", "Windows", "Themes", "TranscodedWallpaper");

                    if (File.Exists(wallpaperPath))
                    {
                        var wallpaperDest = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Microsoft", "Windows", "Themes");
                        Directory.CreateDirectory(wallpaperDest);

                        File.Copy(wallpaperPath, Path.Combine(wallpaperDest, "TranscodedWallpaper"), true);
                        LogSuccess(rtb, "Fond d'écran migré (visible après reconnexion)");
                    }
                    else
                    {
                        LogInfo(rtb, "Pas de fond d'écran à migrer.");
                    }
                }
                catch (Exception ex)
                {
                    LogError(rtb, $"Erreur fond d'écran : {ex.Message}");
                }
            });
        }

        /// <summary>Affiche la liste des lecteurs réseau sauvegardés (recréation manuelle).</summary>
        private async Task MigrateNetworkDrivesInfoAsync(string sourceProfilePath, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                var networkDrivesFile = Path.Combine(sourceProfilePath, "NetworkDrives.txt");
                if (File.Exists(networkDrivesFile))
                {
                    try
                    {
                        var lines = File.ReadAllLines(networkDrivesFile);
                        LogInfo(rtb, "Lecteurs réseau de l'ancien poste :");
                        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                            Log(rtb, $"   {line}");
                        LogWarning(rtb, "Merci de recréer manuellement ces lecteurs réseau.");
                    }
                    catch (Exception ex)
                    {
                        LogError(rtb, $"Erreur lecteurs réseau : {ex.Message}");
                    }
                }
                else
                {
                    LogInfo(rtb, "Pas de fichier de lecteurs réseau trouvé sur le profil source.");
                }
            });
        }

        /// <summary>Migration des clés de registre OneNote.</summary>
        private async Task MigrateOneNoteAsync(string sourceProfilePath, RichTextBox rtb)
        {
            Log(rtb, "Migration des clés de registre OneNote...");
            await Task.Run(() =>
            {
                try
                {
                    RegistryService.RestoreOneNoteKeys(sourceProfilePath, msg => LogInfo(rtb, msg));
                }
                catch (Exception ex)
                {
                    LogError(rtb, $"Erreur OneNote : {ex.Message}");
                }
            });
        }
    }
}
