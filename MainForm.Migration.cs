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
        private sealed class USBDriveInfo
        {
            public string Letter { get; init; } = "";
            public string Label { get; init; } = "";
            public long Size { get; init; }
            public bool HasWindows { get; init; }
            public string UsersPath { get; init; } = "";

            public override string ToString()
            {
                var sizeStr = Size > 0 ? FileService.FormatSize(Size) : "Inconnu";
                var status = HasWindows ? "✓ Windows détecté" : "✗ Pas de Windows";
                return $"{Letter} — {Label} ({sizeStr}) — {status}";
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

        private void LoadUSBDrives()
        {
            cmbUSBDrives.Items.Clear();
            lstProfiles.Items.Clear();

            try
            {
                var usbDrives = new List<USBDriveInfo>();
                var allDrives = DriveInfo.GetDrives()
                    .Where(d => (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed) && d.IsReady)
                    .ToList();

                var currentWindows = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                    .TrimEnd(Path.DirectorySeparatorChar);
                var currentRoot = Path.GetPathRoot(currentWindows)?.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();

                foreach (var drive in allDrives)
                {
                    var windowsPath = Path.Combine(drive.Name, "Windows");
                    var usersPath = Path.Combine(drive.Name, "Users");
                    var hasWindows = Directory.Exists(windowsPath) && Directory.Exists(usersPath);

                    var driveRoot = drive.Name.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
                    if (driveRoot == currentRoot)
                        continue;

                    if (!hasWindows)
                        continue;

                    usbDrives.Add(new USBDriveInfo
                    {
                        Letter = drive.Name.TrimEnd('\\'),
                        Label = string.IsNullOrEmpty(drive.VolumeLabel) ? "Sans nom" : drive.VolumeLabel,
                        Size = drive.TotalSize,
                        HasWindows = hasWindows,
                        UsersPath = usersPath
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

            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive)
                return;

            if (!drive.HasWindows)
            {
                lblMigrationInfo.Text = "Ce lecteur ne contient pas d'installation Windows valide.\nVeuillez sélectionner un autre lecteur.";
                return;
            }

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
                            var hasDesktop = Directory.Exists(Path.Combine(d.FullName, "Desktop"));
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
                                Name = d.Name,
                                Path = d.FullName,
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
            if (cmbUSBDrives.SelectedItem is not USBDriveInfo drive || !drive.HasWindows)
            {
                MessageBox.Show("Veuillez sélectionner un lecteur contenant une installation Windows.",
                    "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                var steps = new List<(string Name, Func<Task> Action)>();

                if (chkMigrateDocuments.Checked) steps.Add(("Documents", () => MigrateStep(
                    Path.Combine(profile.Path, "Documents"), Path.Combine(userProfile, "Documents"),
                    "Documents", progress, ct, errorList)));

                if (chkMigrateDesktop.Checked) steps.Add(("Bureau", () => MigrateStep(
                    Path.Combine(profile.Path, "Desktop"), Path.Combine(userProfile, "Desktop"),
                    "Bureau", progress, ct, errorList)));

                if (chkMigrateDownloads.Checked) steps.Add(("Téléchargements", () => MigrateStep(
                    Path.Combine(profile.Path, "Downloads"), Path.Combine(userProfile, "Downloads"),
                    "Téléchargements", progress, ct, errorList)));

                if (chkMigratePictures.Checked) steps.Add(("Images", () => MigrateStep(
                    Path.Combine(profile.Path, "Pictures"), Path.Combine(userProfile, "Pictures"),
                    "Images", progress, ct, errorList)));

                if (chkMigrateMusic.Checked) steps.Add(("Musique", () => MigrateStep(
                    Path.Combine(profile.Path, "Music"), Path.Combine(userProfile, "Music"),
                    "Musique", progress, ct, errorList)));

                if (chkMigrateVideos.Checked) steps.Add(("Vidéos", () => MigrateStep(
                    Path.Combine(profile.Path, "Videos"), Path.Combine(userProfile, "Videos"),
                    "Vidéos", progress, ct, errorList)));

                if (chkMigrateSignatures.Checked) steps.Add(("Signatures Outlook", () => MigrateStep(
                    Path.Combine(profile.Path, "AppData", "Roaming", "Microsoft", "Signatures"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),
                    "Signatures Outlook", progress, ct, errorList)));

                if (chkMigrateExcelMacros.Checked) steps.Add(("Macros Excel (XLSTART)", () => MigrateStep(
                    Path.Combine(profile.Path, "AppData", "Roaming", "Microsoft", "Excel", "XLSTART"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                    "Macros Excel (XLSTART)", progress, ct, errorList)));

                if (chkMigrateTemplates.Checked) steps.Add(("Modèles Office", () => MigrateStep(
                    Path.Combine(profile.Path, "AppData", "Roaming", "Microsoft", "Templates"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                    "Modèles Office", progress, ct, errorList)));

                if (chkMigrateSap.Checked) steps.Add(("SAP GUI", () => MigrateStep(
                    Path.Combine(profile.Path, "AppData", "Roaming", "SAP"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                    "SAP GUI", progress, ct, errorList)));

                if (chkMigratePublic.Checked) steps.Add(("Dossier Public", () => MigrateStep(
                    Path.Combine(Path.GetPathRoot(profile.Path) ?? "", "Users", "Public"),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    "Dossier Public", progress, ct, errorList)));

                if (chkMigrateOutlook.Checked) steps.Add(("Données Outlook", () => MigrateOutlookDataAsync(profile.Path, rtbMigrationLog, ct)));
                if (chkMigrateStickyNotes.Checked) steps.Add(("Sticky Notes", () => MigrateStickyNotesAsync(profile.Path, rtbMigrationLog, ct)));
                if (chkMigrateEdgeProfile.Checked) steps.Add(("Profil Edge", () => MigrateEdgeProfileAsync(profile.Path, rtbMigrationLog, progress, ct, errorList)));
                if (chkMigrateWallpaper.Checked) steps.Add(("Fond d'écran", () => MigrateWallpaperAsync(profile.Path, rtbMigrationLog)));
                if (chkMigrateNetworkDrives.Checked) steps.Add(("Lecteurs réseau", () => MigrateNetworkDrivesInfoAsync(profile.Path, rtbMigrationLog)));
                if (chkMigrateOneNote.Checked) steps.Add(("OneNote (registre)", () => MigrateOneNoteAsync(profile.Path, rtbMigrationLog)));

                // IP Desktop Softphone — réservé, non fonctionnel pour l'instant
                // if (chkMigrateIpDesktopSoftphone.Checked) steps.Add(("IP Softphone", () => MigrateIpDesktopSoftphoneAsync(profile.Path, rtbMigrationLog, progress, ct, errorList)));

                int totalSteps = steps.Count;
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
                        catch (Exception ex)
                        {
                            LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}");
                        }
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

            // Cache d'autocomplétion
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