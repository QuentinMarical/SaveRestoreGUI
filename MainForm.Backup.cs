using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Logique de sauvegarde — parité fonctionnelle complète avec Sauvegarde.ps1 :
    /// dossiers utilisateurs, Outlook (PST + autocomplete + profils + règles + boîtes partagées),
    /// signatures + clés MailSettings, OneNote (RecentNotebooks/User MRU/OpenNotebook),
    /// Templates, SAP, Sticky Notes, Edge (profil complet), fond d'écran, lecteurs réseau,
    /// dossier Public, IP Desktop Softphone, récapitulatif final.
    /// </summary>
    public partial class MainForm
    {
        private void BtnBrowseBackup_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choisissez le dossier où stocker la sauvegarde",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtBackupPath.Text = Path.Combine(dialog.SelectedPath, Environment.UserName);
            }
        }

        private async void BtnStartBackup_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBackupPath.Text))
            {
                MessageBox.Show("Veuillez sélectionner un dossier de destination.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbBackupLog.Clear();
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var backupRoot = txtBackupPath.Text;
                Directory.CreateDirectory(backupRoot);
                _logFilePath = Path.Combine(backupRoot, "Sauvegarde.log");

                LogTitle(rtbBackupLog, "Démarrage de la sauvegarde");
                LogInfo(rtbBackupLog, $"Utilisateur : {Environment.UserName}");
                LogInfo(rtbBackupLog, $"Poste : {Environment.MachineName}");
                LogInfo(rtbBackupLog, $"Destination : {backupRoot}");

                UpdateOldProfileOptionState();
                if (chkOldProfile.Checked)
                    DetectAndLogOldProfiles(rtbBackupLog);

                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var progress = new Progress<int>(UpdateProgress);

                var steps = new List<(string Name, Func<Task> Action)>();

                if (chkDocuments.Checked) steps.Add(("Documents", () => CopyStep(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Path.Combine(backupRoot, "Documents"), "Documents", rtbBackupLog, progress, errorList, ct)));

                if (chkDesktop.Checked) steps.Add(("Bureau", () => CopyStep(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Path.Combine(backupRoot, "Desktop"), "Bureau", rtbBackupLog, progress, errorList, ct)));

                if (chkDownloads.Checked) steps.Add(("Téléchargements", () => CopyStep(
                    Path.Combine(userProfile, "Downloads"),
                    Path.Combine(backupRoot, "Downloads"), "Téléchargements", rtbBackupLog, progress, errorList, ct)));

                if (chkPictures.Checked) steps.Add(("Images", () => CopyStep(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Path.Combine(backupRoot, "Pictures"), "Images", rtbBackupLog, progress, errorList, ct)));

                if (chkMusic.Checked) steps.Add(("Musique", () => CopyStep(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Path.Combine(backupRoot, "Music"), "Musique", rtbBackupLog, progress, errorList, ct)));

                if (chkVideos.Checked) steps.Add(("Vidéos", () => CopyStep(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    Path.Combine(backupRoot, "Videos"), "Vidéos", rtbBackupLog, progress, errorList, ct)));

                if (chkSignatures.Checked) steps.Add(("Signatures Outlook", () => BackupSignaturesAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
                if (chkTemplates.Checked) steps.Add(("Modèles Office", () => CopyStep(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                    Path.Combine(backupRoot, "Templates"), "Modèles Office", rtbBackupLog, progress, errorList, ct)));
                if (chkExcelMacros.Checked) steps.Add(("Macros Excel (XLSTART)", () => CopyStep(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                    Path.Combine(backupRoot, "Excel", "XLSTART"), "Macros Excel (XLSTART)", rtbBackupLog, progress, errorList, ct)));
                if (chkSap.Checked) steps.Add(("SAP GUI", () => CopyStep(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                    Path.Combine(backupRoot, "SAP"), "SAP GUI", rtbBackupLog, progress, errorList, ct)));

                if (chkPublic.Checked) steps.Add(("Dossier Public", () => CopyStep(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    Path.Combine(backupRoot, "Public"), "Dossier Public", rtbBackupLog, progress, errorList, ct)));

                if (chkOutlook.Checked) steps.Add(("Données Outlook", () => BackupOutlookDataAsync(backupRoot, rtbBackupLog, ct)));
                if (chkOneNote.Checked) steps.Add(("OneNote (registre)", () => BackupOneNoteAsync(backupRoot, rtbBackupLog)));
                if (chkStickyNotes.Checked) steps.Add(("Sticky Notes", () => BackupStickyNotesAsync(backupRoot, rtbBackupLog, ct)));
                if (chkEdgeProfile.Checked) steps.Add(("Profil Edge", () => BackupEdgeProfileAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
                if (chkWallpaper.Checked) steps.Add(("Fond d'écran", () => BackupWallpaperAsync(backupRoot, rtbBackupLog)));
                if (chkNetworkDrives.Checked) steps.Add(("Lecteurs réseau", () => BackupNetworkDrivesAsync(backupRoot, rtbBackupLog)));

                int totalSteps = steps.Count;
                int currentStep = 0;

                foreach (var (name, action) in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    currentStep++;
                    UpdateStatus($"Sauvegarde {name} ({currentStep}/{totalSteps})");
                    await action();
                }

                LogTitle(rtbBackupLog, "Récapitulatif final");
                UpdateStatus("Calcul de la taille finale...");
                var totalSize = await Task.Run(() => FileService.GetDirectorySize(backupRoot), CancellationToken.None);
                LogInfo(rtbBackupLog, $"Taille totale de la sauvegarde : {FileService.FormatSize(totalSize)}");

                if (errorList.Count > 0)
                {
                    LogTitle(rtbBackupLog, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList)
                        LogWarning(rtbBackupLog, err);
                }

                LogTitle(rtbBackupLog, "Sauvegarde terminée");
                LogSuccess(rtbBackupLog, $"Fichiers sauvegardés dans : {backupRoot}");
                UpdateStatus("Sauvegarde terminée avec succès");
                ToastService.Show(this, "Sauvegarde terminée avec succès !", ToastKind.Success);

                MessageBox.Show($"Sauvegarde terminée avec succès !\n\nDossier : {backupRoot}\nTaille : {FileService.FormatSize(totalSize)}",
                    "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                LogWarning(rtbBackupLog, "Sauvegarde annulée par l'utilisateur.");
                UpdateStatus("Sauvegarde annulée");
            }
            catch (Exception ex)
            {
                LogError(rtbBackupLog, $"Erreur : {ex.Message}");
                UpdateStatus("Erreur lors de la sauvegarde");
                MessageBox.Show($"Erreur lors de la sauvegarde :\n{ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                HideProgress();
                _logFilePath = null;
            }
        }

        private async Task CopyStep(string source, string destination, string name,
            RichTextBox rtb, IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            if (!Directory.Exists(source))
            {
                LogWarning(rtb, $"{name} : source introuvable.");
                return;
            }

            Log(rtb, $"Copie de {name}...");
            var result = await FileService.CopyFolderAsync(source, destination, progress, null, ct);

            if (result.Copied == 0 && result.Skipped == 0)
            {
                LogWarning(rtb, $"{name} : source vide.");
                return;
            }

            foreach (var err in result.Errors)
            {
                LogError(rtb, $"Erreur copie {err}");
                errorList.Add($"{name} : {err}");
            }

            LogSuccess(rtb, $"{name} : {result.Copied} fichiers copiés, {result.Skipped} ignorés — {FileService.FormatSize(result.TotalBytes)}");
        }

        private async Task BackupSignaturesAsync(string backupRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var signaturesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures");
            await CopyStep(signaturesPath, Path.Combine(backupRoot, "Signatures"), "Signatures Outlook",
                rtb, progress, errorList, ct);

            await Task.Run(() => RegistryService.BackupOutlookSignatureSettings(backupRoot,
                msg => LogInfo(rtb, msg)), ct);
        }

        private async Task BackupOutlookDataAsync(string backupRoot, RichTextBox rtb, CancellationToken ct)
        {
            var outlookDir = Path.Combine(backupRoot, "OutlookData");
            Directory.CreateDirectory(outlookDir);

            Log(rtb, "Recherche des archives Outlook (PST)...");
            var pstFiles = await Task.Run(OutlookService.FindPstFiles, ct);

            if (pstFiles.Count > 0)
            {
                Log(rtb, $"{pstFiles.Count} fichier(s) PST détecté(s)");
                var pstPaths = new List<string>();

                foreach (var pst in pstFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    var destPath = Path.Combine(outlookDir, Path.GetFileName(pst));
                    try
                    {
                        var size = new FileInfo(pst).Length;
                        Log(rtb, $"Copie de {Path.GetFileName(pst)} ({FileService.FormatSize(size)})...");
                        await Task.Run(() => File.Copy(pst, destPath, true), ct);
                        pstPaths.Add(pst);
                        LogSuccess(rtb, $"PST sauvegardé : {Path.GetFileName(pst)}");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message} (fichier peut-être ouvert dans Outlook)");
                    }
                }

                await File.WriteAllLinesAsync(Path.Combine(outlookDir, "PST_Paths.txt"), pstPaths, ct);
                LogInfo(rtb, "Liste des chemins PST sauvegardée dans PST_Paths.txt");
            }
            else
            {
                LogInfo(rtb, "Aucun fichier PST détecté.");
            }

            var roamCachePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Outlook", "RoamCache");
            if (Directory.Exists(roamCachePath))
            {
                var autocompleteFiles = Directory.GetFiles(roamCachePath, "Stream_Autocomplete_*.dat");
                if (autocompleteFiles.Length > 0)
                {
                    var destDir = Path.Combine(outlookDir, "RoamCache");
                    Directory.CreateDirectory(destDir);
                    foreach (var file in autocompleteFiles)
                    {
                        ct.ThrowIfCancellationRequested();
                        await Task.Run(() => File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true), ct);
                    }
                    LogSuccess(rtb, $"Cache d'autocomplétion sauvegardé ({autocompleteFiles.Length} fichiers)");
                }
            }

            await Task.Run(() => RegistryService.BackupOutlookProfiles(outlookDir, msg => LogInfo(rtb, msg)), ct);

            var rulesFiles = OutlookService.FindRulesFiles();
            foreach (var rule in rulesFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await Task.Run(() => File.Copy(rule, Path.Combine(outlookDir, Path.GetFileName(rule)), true), ct);
                    LogSuccess(rtb, $"Fichier de règles sauvegardé : {Path.GetFileName(rule)}");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogError(rtb, $"Erreur règles {Path.GetFileName(rule)} : {ex.Message}");
                }
            }

            Log(rtb, "Recherche des boîtes aux lettres partagées...");
            var sharedMailboxes = await Task.Run(OutlookService.FindSharedMailboxes, ct);
            if (sharedMailboxes.Count > 0)
            {
                LogInfo(rtb, $"{sharedMailboxes.Count} boîte(s) partagée(s) détectée(s) :");
                foreach (var mb in sharedMailboxes)
                    Log(rtb, $"   → {mb}");
                await File.WriteAllLinesAsync(Path.Combine(outlookDir, "SharedMailboxes.txt"), sharedMailboxes, ct);
                LogSuccess(rtb, "Liste sauvegardée dans SharedMailboxes.txt");
            }
            else
            {
                LogInfo(rtb, "Aucun boîte aux lettres partagée détectée.");
            }
        }

        private async Task BackupOneNoteAsync(string backupRoot, RichTextBox rtb)
        {
            Log(rtb, "Export des clés de registre OneNote...");
            await Task.Run(() =>
            {
                RegistryService.BackupOneNoteKeys(backupRoot, msg => LogInfo(rtb, msg));
                RegistryService.BackupOpenNotebookKey(backupRoot, msg => LogInfo(rtb, msg));
            });
        }

        private async Task BackupStickyNotesAsync(string backupRoot, RichTextBox rtb, CancellationToken ct)
        {
            var stickyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState", "plum.sqlite");

            if (File.Exists(stickyPath))
            {
                var destPath = Path.Combine(backupRoot, "StickyNotes.sqlite");
                await Task.Run(() => File.Copy(stickyPath, destPath, true), ct);
                LogSuccess(rtb, "Sticky Notes sauvegardés");
            }
            else
            {
                LogInfo(rtb, "Aucune donnée Sticky Notes trouvée.");
            }
        }

        private async Task BackupEdgeProfileAsync(string backupRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var edgeDefault = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default");

            await CopyStep(edgeDefault,
                Path.Combine(backupRoot, "EdgeProfile"),
                "Profil Edge",
                rtb, progress, errorList, ct);
        }

        private async Task BackupWallpaperAsync(string backupRoot, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                try
                {
                    string? wallpaperPath = null;
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    {
                        wallpaperPath = key?.GetValue("Wallpaper") as string;
                    }

                    if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                    {
                        var ext = Path.GetExtension(wallpaperPath);
                        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                        var dest = Path.Combine(backupRoot, "Wallpaper" + ext);
                        File.Copy(wallpaperPath, dest, true);
                        var size = new FileInfo(dest).Length;
                        LogSuccess(rtb, $"Fond d'écran sauvegardé ({FileService.FormatSize(size)})");
                        return;
                    }

                    var transcoded = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Microsoft", "Windows", "Themes", "TranscodedWallpaper");
                    if (File.Exists(transcoded))
                    {
                        File.Copy(transcoded, Path.Combine(backupRoot, "Wallpaper.jpg"), true);
                        LogSuccess(rtb, "Fond d'écran sauvegardé (TranscodedWallpaper)");
                    }
                    else
                    {
                        LogInfo(rtb, "Aucun fond d'écran personnalisé trouvé.");
                    }
                }
                catch (Exception ex)
                {
                    LogError(rtb, $"Erreur fond d'écran : {ex.Message}");
                }
            });
        }

        private async Task BackupNetworkDrivesAsync(string backupRoot, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                var lines = new List<string>();
                try
                {
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT * FROM Win32_MappedLogicalDisk");
                    foreach (var drive in searcher.Get().Cast<System.Management.ManagementObject>())
                    {
                        var driveLetter   = drive["DeviceID"]?.ToString();
                        var providerName  = drive["ProviderName"]?.ToString();
                        if (!string.IsNullOrEmpty(driveLetter) && !string.IsNullOrEmpty(providerName))
                        {
                            lines.Add($"Lettre: {driveLetter} → Chemin: {providerName}");
                            Log(rtb, $"Lecteur réseau détecté : {driveLetter} → {providerName}");
                        }
                    }

                    if (lines.Count > 0)
                    {
                        File.WriteAllLines(Path.Combine(backupRoot, "NetworkDrives.txt"), lines);
                        LogSuccess(rtb, $"Lecteurs réseau sauvegardés ({lines.Count})");
                    }
                    else
                    {
                        LogInfo(rtb, "Aucun lecteur réseau mappé.");
                    }
                }
                catch (Exception ex)
                {
                    LogError(rtb, $"Erreur lecteurs réseau : {ex.Message}");
                }
            });
        }

        private void DetectAndLogOldProfiles(RichTextBox rtb)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var usersDir = Path.GetDirectoryName(userProfile);
            var currentUser = Environment.UserName;
            if (usersDir == null) return;

            var excluded = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };
            var oldProfiles = Directory.GetDirectories(usersDir)
                .Where(d =>
                {
                    var name = Path.GetFileName(d);
                    return !name!.Equals(currentUser, StringComparison.OrdinalIgnoreCase)
                           && !excluded.Contains(name, StringComparer.OrdinalIgnoreCase)
                           && name.StartsWith(currentUser + ".", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (oldProfiles.Count > 0)
            {
                foreach (var profile in oldProfiles)
                    LogInfo(rtb, $"Ancien profil détecté : {Path.GetFileName(profile)}");
            }
            else
            {
                LogInfo(rtb, "Aucun ancien profil utilisateur détecté.");
            }
        }
    }
}
