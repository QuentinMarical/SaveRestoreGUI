using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    public partial class MainForm
    {
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

        // ═══════════════════════════════════════════════════════════════════
        //  DÉTECTION DES LECTEURS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Appel direct à kernel32 : retourne le type du point de montage même si le
        /// volume est inaccessible (BitLocker verrouillé, disque non prêt…).
        /// Valeurs : 0=Unknown, 1=NoRootDir (lettre libre), 2=Removable,
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
            // Le concepteur ou le cycle de vie très tôt peuvent donner des contrôles non initialisés.
            if (cmbUSBDrives == null || lstProfiles == null || lblBitLockerStatus == null || lblMigrationInfo == null)
                return;

            cmbUSBDrives.Items.Clear();
            lstProfiles.Items.Clear();
            lblBitLockerStatus.Text = "";

            try
            {
                var currentRoot = Path.GetPathRoot(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                    ?.TrimEnd(Path.DirectorySeparatorChar)
                    .ToUpperInvariant();

                var result      = new List<USBDriveInfo>();
                var seenLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // ── Étape 1 : scan A–Z via GetDriveType (fonctionne même si IsReady = false)
                foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                {
                    if (c == 'A' || c == 'B') continue;   // disquettes

                    var letter = $"{c}:";
                    var root   = letter + "\\";

                    if (currentRoot != null && letter.Equals(currentRoot, StringComparison.OrdinalIgnoreCase)) continue;

                    uint driveType = GetDriveTypeWin32(root);
                    if (driveType <= 1) continue;          // 0=inconnu, 1=lettre libre
                    if (driveType == 5 || driveType == 6) continue; // CD/DVD, RAMDisk

                    bool accessible = IsVolumeAccessible(root);

                    if (!accessible)
                    {
                        var bdeState  = GetBitLockerStatePowerShell(root);
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
                    catch { /* volume démonté entre-temps */ }

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

                // ── Étape 2 : fallback PowerShell global
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
                LogError(rtbMigrationLog, $"Erreur détection disque : {ex.Message}");
            }
        }

        // ── Helpers PowerShell BitLocker ─────────────────────────────────────────────
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
            catch { /* module BitLocker absent — silencieux */ }
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
                BitLockerState.NotEncrypted => ($"\u2705 {drive.Letter} — Pas de chiffrement",                    Color.SeaGreen),
                _                           => ($"\u2139\ufe0f {drive.Letter} — État BitLocker inconnu",           SystemColors.GrayText)
            };
        }

        // (reste du fichier inchangé)
    }
}
