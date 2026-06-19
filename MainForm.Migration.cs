using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    public partial class MainForm
    {
        // ── État BitLocker d'un lecteur ───────────────────────────────────────────────
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
        /// Appel direct à kernel32 : retourne le type du point de montage MÊME si le
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
                        // On confirme via PowerShell si disponible ; sinon on suppose Locked.
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
                    catch { /* volume démontage entre-temps */ }

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

        // ── Helpers PowerShell BitLocker ───────────────────────────────────────────────
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
                    "OFF"     => BitLockerState.NotEncrypted,
                    "LOCKED"  => BitLockerState.Locked,
                    "UNLOCKED" => BitLockerState.Unlocked,
                    _         => BitLockerState.Unknown
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
        /// Priorité : Panneau de config classique (fonctionne sur toutes les éditions Pro/Enterprise).
        /// Fallback : ms-settings:deviceencryption (Windows 11 Home).
        /// </summary>
        private static void OpenBitLockerControlPanel()
        {
            // Option 1 : Panneau de configuration → Chiffrement de lecteur BitLocker
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
            catch { /* control.exe non disponible — très rare */ }

            // Option 2 : Paramètres Windows 10/11 → chiffrement de l'appareil
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "ms-settings:deviceencryption",
                    UseShellExecute = true
                });
                return;
            }
            catch { /* ms-settings non disponible */ }

            // Option 3 : message manuel
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

        // ── Bouton Vérifier BitLocker ──────────────────────────────────────────────────
        private async void BtnBitLocker_Click(object? sender, EventArgs e)
        {
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo selectedDrive)
            {
                MessageBox.Show(
                    "Sélectionnez d'abord un lecteur dans la liste.\n" +
                    "Si aucun disque n'apparaît, cliquez sur \U0001f504 pour actualiser.",
                    "Aucun lecteur sélectionné",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string driveLetter = selectedDrive.Letter.TrimEnd('\\', ':') + ":";

            btnBitLocker.Enabled = false;
            lblBitLockerStatus.Text = "Analyse en cours…";
            LogTitle(rtbMigrationLog, $"BitLocker — {driveLetter}");

            try
            {
                var state = await Task.Run(() => GetBitLockerStatePowerShell(driveLetter + "\\"));

                selectedDrive.BitLocker = state;
                var idx = cmbUSBDrives.SelectedIndex;
                cmbUSBDrives.Items[idx] = selectedDrive;
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
                        await HandleBitLockerUnlockAsync(driveLetter);
                        break;

                    case BitLockerState.Unlocked:
                        // Le lecteur est chiffré mais déverrouillé : propose d'ouvrir la gestion BitLocker
                        LogWarning(rtbMigrationLog, $"{driveLetter} est chiffré (BitLocker actif, déverrouillé).");
                        var openManage = MessageBox.Show(
                            $"{driveLetter} est chiffré par BitLocker mais déverrouillé.\n" +
                            "Vous pouvez migrer les données normalement.\n\n" +
                            "Souhaitez-vous ouvrir la gestion BitLocker\n" +
                            "(désactiver le chiffrement, changer le mot de passe…) ?",
                            "BitLocker actif — Déverrouillé",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);
                        if (openManage == DialogResult.Yes)
                            OpenBitLockerControlPanel();
                        break;

                    default:
                        // État inconnu (module BitLocker absent ou PowerShell indisponible)
                        LogWarning(rtbMigrationLog, $"{driveLetter} — État BitLocker indéterminé (module absent ?).");
                        var openUnknown = MessageBox.Show(
                            $"L'état BitLocker de {driveLetter} n'a pas pu être déterminé.\n\n" +
                            "Cela peut indiquer que le module BitLocker est absent\n" +
                            "ou que PowerShell n'est pas accessible.\n\n" +
                            "Souhaitez-vous ouvrir le Panneau de configuration BitLocker\n" +
                            "pour vérifier manuellement l'état du lecteur ?",
                            "BitLocker — État inconnu",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        if (openUnknown == DialogResult.Yes)
                            OpenBitLockerControlPanel();
                        break;
                }
            }
            catch (Exception ex)
            {
                lblBitLockerStatus.Text = "\u274c Erreur lors de la vérification.";
                LogError(rtbMigrationLog, $"BitLocker : {ex.Message}");
            }
            finally
            {
                btnBitLocker.Enabled = true;
            }
        }

        // ── Déverrouillage BitLocker ────────────────────────────────────────────────
        private async Task HandleBitLockerUnlockAsync(string driveLetter)
        {
            var letter = driveLetter.TrimEnd('\\', ':').ToUpperInvariant() + ":";

            var answer = MessageBox.Show(
                $"Le lecteur {letter} est verrouillé par BitLocker.\n\n" +
                "Le Panneau de configuration BitLocker va s'ouvrir.\n" +
                "Déverrouillez le lecteur depuis cette fenêtre,\n" +
                "puis cliquez OK ici pour rafraîchir l'état du lecteur.",
                "BitLocker — Déverrouillage",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (answer != DialogResult.OK) return;

            // Ouvre le Panneau de configuration → Chiffrement de lecteur BitLocker
            OpenBitLockerControlPanel();

            // L'utilisateur déverrouille dans le Panneau de config, puis confirme ici
            MessageBox.Show(
                $"Cliquez OK une fois que vous avez déverrouillé {letter} dans le Panneau de configuration.\n\n" +
                "La liste des lecteurs sera automatiquement actualisée.",
                "Attente déverrouillage",
                MessageBoxButtons.OK,
                MessageBoxIcon.Question);

            // Pause courte pour laisser Windows monter le volume
            lblBitLockerStatus.Text = "Actualisation…";
            await Task.Delay(800);

            // Réévaluer l'état BitLocker du lecteur dans la combobox
            for (int i = 0; i < cmbUSBDrives.Items.Count; i++)
            {
                if (cmbUSBDrives.Items[i] is not USBDriveInfo d) continue;
                if (!d.Letter.TrimEnd('\\').Equals(letter, StringComparison.OrdinalIgnoreCase)) continue;

                var root = letter + "\\";
                var newState = await Task.Run(() => GetBitLockerStatePowerShell(root));

                // Si PowerShell ne répond pas, on infère d'après l'accessibilité
                if (newState == BitLockerState.Unknown)
                    newState = IsVolumeAccessible(root) ? BitLockerState.Unlocked : BitLockerState.Locked;

                d.BitLocker = newState;
                cmbUSBDrives.Items[i] = d;

                if (cmbUSBDrives.SelectedIndex == i)
                {
                    cmbUSBDrives.SelectedIndex = i;
                    UpdateBitLockerLabel(d);
                }

                if (newState == BitLockerState.Unlocked || newState == BitLockerState.NotEncrypted)
                {
                    LogSuccess(rtbMigrationLog, $"{letter} déverrouillé avec succès.");
                    // Recharger les profils si disponibles
                    if (d.HasUsers)
                        LoadProfiles(d);
                }
                else
                {
                    LogWarning(rtbMigrationLog, $"{letter} toujours verrouillé après tentative.");
                }
                break;
            }
        }
    }
}
