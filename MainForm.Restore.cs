using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Logique de restauration — parité fonctionnelle complète avec Restauration.ps1 :
    /// dossiers utilisateurs, import clés registre (OneNote/OpenNotebook/signatures),
    /// AppData (Signatures/Templates/SAP), Outlook (PST + autocomplete + règles + boîtes partagées
    /// avec copie presse-papiers), Edge (profil complet), Sticky Notes, lecteurs réseau,
    /// fond d'écran, dossier Public, IP Desktop Softphone, lancement des applications.
    /// </summary>
    public partial class MainForm
    {
        private void BtnBrowseRestore_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Sélectionnez le dossier contenant la sauvegarde"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtRestorePath.Text = dialog.SelectedPath;
            }
        }

        private async void BtnStartRestore_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRestorePath.Text) || !Directory.Exists(txtRestorePath.Text))
            {
                MessageBox.Show("Veuillez sélectionner un dossier source valide.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            rtbRestoreLog.Clear();
            _cancellationTokenSource = new CancellationTokenSource();
            var ct = _cancellationTokenSource.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var restoreRoot = txtRestorePath.Text;
                _logFilePath = Path.Combine(restoreRoot, "Restauration.log");

                LogTitle(rtbRestoreLog, "Démarrage de la restauration");
                LogInfo(rtbRestoreLog, $"Source : {restoreRoot}");

                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var progress = new Progress<int>(UpdateProgress);

                var steps = new List<(string Name, Func<Task> Action)>();

                if (chkRestoreDocuments.Checked) steps.Add(("Documents", () => RestoreStep(
                    Path.Combine(restoreRoot, "Documents"), Path.Combine(userProfile, "Documents"),
                    "Documents", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreDesktop.Checked) steps.Add(("Bureau", () => RestoreStep(
                    Path.Combine(restoreRoot, "Desktop"), Path.Combine(userProfile, "Desktop"),
                    "Bureau", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreDownloads.Checked) steps.Add(("Téléchargements", () => RestoreStep(
                    Path.Combine(restoreRoot, "Downloads"), Path.Combine(userProfile, "Downloads"),
                    "Téléchargements", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestorePictures.Checked) steps.Add(("Images", () => RestoreStep(
                    Path.Combine(restoreRoot, "Pictures"), Path.Combine(userProfile, "Pictures"),
                    "Images", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreMusic.Checked) steps.Add(("Musique", () => RestoreStep(
                    Path.Combine(restoreRoot, "Music"), Path.Combine(userProfile, "Music"),
                    "Musique", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreVideos.Checked) steps.Add(("Vidéos", () => RestoreStep(
                    Path.Combine(restoreRoot, "Videos"), Path.Combine(userProfile, "Videos"),
                    "Vidéos", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreOneNote.Checked) steps.Add(("Clés registre OneNote", () => RestoreOneNoteAsync(restoreRoot, rtbRestoreLog)));

                if (chkRestoreSignatures.Checked) steps.Add(("Signatures Outlook", () => RestoreStep(
                    Path.Combine(restoreRoot, "Signatures"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),
                    "Signatures Outlook", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreTemplates.Checked) steps.Add(("Modèles Office", () => RestoreStep(
                    Path.Combine(restoreRoot, "Templates"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                    "Modèles Office", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreExcelMacros.Checked) steps.Add(("Macros Excel (XLSTART)", () => RestoreStep(
                    Path.Combine(restoreRoot, "Excel", "XLSTART"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                    "Macros Excel (XLSTART)", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreSap.Checked) steps.Add(("SAP GUI", () => RestoreStep(
                    Path.Combine(restoreRoot, "SAP"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                    "SAP GUI", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestorePublic.Checked) steps.Add(("Dossier Public", () => RestoreStep(
                    Path.Combine(restoreRoot, "Public"),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    "Dossier Public", rtbRestoreLog, progress, errorList, ct)));

                if (chkRestoreOutlook.Checked) steps.Add(("Données Outlook", () => RestoreOutlookDataAsync(restoreRoot, rtbRestoreLog, ct)));
                if (chkRestoreStickyNotes.Checked) steps.Add(("Sticky Notes", () => RestoreStickyNotesAsync(restoreRoot, rtbRestoreLog, ct)));
                if (chkRestoreEdgeProfile.Checked) steps.Add(("Profil Edge", () => RestoreEdgeProfileAsync(restoreRoot, rtbRestoreLog, progress, errorList, ct)));
                if (chkRestoreNetworkDrives.Checked) steps.Add(("Lecteurs réseau", () => RestoreNetworkDrivesInfoAsync(restoreRoot, rtbRestoreLog)));
                if (chkRestoreWallpaper.Checked) steps.Add(("Fond d'écran", () => RestoreWallpaperAsync(restoreRoot, rtbRestoreLog, ct)));

                int totalSteps = steps.Count;
                int currentStep = 0;

                foreach (var (name, action) in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    currentStep++;
                    UpdateStatus($"Restauration {name} ({currentStep}/{totalSteps})");
                    await action();
                }

                if (chkLaunchApps.Checked)
                {
                    LogTitle(rtbRestoreLog, "Lancement des applications");
                    await Task.Run(() => AppLauncherService.LaunchApplications(
                        msg => LogInfo(rtbRestoreLog, msg)), CancellationToken.None);

                    // Ouvre la popup OneDrive "Gérer la sauvegarde" pour activer
                    // la synchronisation Bureau / Documents / Images sur le nouveau poste.
                    LogTitle(rtbRestoreLog, "Synchronisation OneDrive");
                    await Task.Run(() => AppLauncherService.OpenOneDriveBackupSettings(
                        msg => LogInfo(rtbRestoreLog, msg)), CancellationToken.None);
                }

                if (errorList.Count > 0)
                {
                    LogTitle(rtbRestoreLog, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList)
                        LogWarning(rtbRestoreLog, err);
                }

                LogTitle(rtbRestoreLog, "Restauration terminée");
                UpdateStatus("Restauration terminée avec succès");
                ToastService.Show(this, "Restauration terminée avec succès !", ToastKind.Success);

                MessageBox.Show("Restauration terminée avec succès !", "Succès",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                LogWarning(rtbRestoreLog, "Restauration annulée par l'utilisateur.");
                UpdateStatus("Restauration annulée");
            }
            catch (Exception ex)
            {
                LogError(rtbRestoreLog, $"Erreur : {ex.Message}");
                UpdateStatus("Erreur lors de la restauration");
                MessageBox.Show($"Erreur lors de la restauration :\n{ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                HideProgress();
                _logFilePath = null;
            }
        }

        private async Task RestoreStep(string source, string destination, string name,
            RichTextBox rtb, IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            if (!Directory.Exists(source))
            {
                LogInfo(rtb, $"{name} : rien à restaurer.");
                return;
            }

            Log(rtb, $"Restauration de {name}...");
            var result = await FileService.CopyFolderAsync(source, destination, progress, null, ct);

            foreach (var err in result.Errors)
            {
                LogError(rtb, $"Erreur copie {err}");
                errorList.Add($"{name} : {err}");
            }

            LogSuccess(rtb, $"{name} : {result.Copied} fichiers restaurés, {result.Skipped} ignorés — {FileService.FormatSize(result.TotalBytes)}");
        }

        /// <summary>Import des clés OneNote + OpenNotebook + paramètres signatures.</summary>
        private async Task RestoreOneNoteAsync(string restoreRoot, RichTextBox rtb)
        {
            Log(rtb, "Import des clés de registre...");
            await Task.Run(() => RegistryService.RestoreOneNoteKeys(restoreRoot, msg => LogInfo(rtb, msg)));
        }

        /// <summary>
        /// Restauration Outlook complète : PST, autocomplete, profils (info), règles .rwz,
        /// boîtes partagées (affichage + presse-papiers).
        /// </summary>
        private async Task RestoreOutlookDataAsync(string restoreRoot, RichTextBox rtb, CancellationToken ct)
        {
            var outlookDataDir = Path.Combine(restoreRoot, "OutlookData");
            if (!Directory.Exists(outlookDataDir))
            {
                LogInfo(rtb, "Aucune donnée Outlook à restaurer.");
                return;
            }

            var pstFiles = Directory.GetFiles(outlookDataDir, "*.pst");
            if (pstFiles.Length > 0)
            {
                var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string[] possibleDirs = { Path.Combine(docsPath, "Outlook Files"), Path.Combine(docsPath, "Fichiers Outlook") };
                string mainOutlookDir = possibleDirs.FirstOrDefault(Directory.Exists) ?? possibleDirs[0];
                Directory.CreateDirectory(mainOutlookDir);

                foreach (var pst in pstFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    var destPath = Path.Combine(mainOutlookDir, Path.GetFileName(pst));
                    try
                    {
                        await Task.Run(() => File.Copy(pst, destPath, true), ct);
                        var size = new FileInfo(destPath).Length;
                        LogSuccess(rtb, $"PST restauré : {Path.GetFileName(pst)} ({FileService.FormatSize(size)})");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}");
                    }
                }

                LogInfo(rtb, $"Les fichiers PST ont été copiés dans : {mainOutlookDir}");
                LogInfo(rtb, "Pour rattacher les archives dans Outlook :");
                LogInfo(rtb, "  1. Ouvrir Outlook");
                LogInfo(rtb, "  2. Fichier > Ouvrir et exporter > Ouvrir le fichier de données Outlook");
                LogInfo(rtb, "  3. Sélectionner chaque fichier .pst");
            }
            else
            {
                LogInfo(rtb, "Aucun fichier PST à restaurer.");
            }

            var roamCacheBackup = Path.Combine(outlookDataDir, "RoamCache");
            if (Directory.Exists(roamCacheBackup))
            {
                var roamCacheDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Outlook", "RoamCache");
                Directory.CreateDirectory(roamCacheDest);

                var files = Directory.GetFiles(roamCacheBackup, "Stream_Autocomplete_*.dat");
                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Run(() => File.Copy(file, Path.Combine(roamCacheDest, Path.GetFileName(file)), true), ct);
                }
                if (files.Length > 0)
                    LogSuccess(rtb, $"Cache d'autocomplétion restauré ({files.Length} fichiers)");
            }

            var regFiles = Directory.GetFiles(outlookDataDir, "Outlook_Profile_*.reg");
            foreach (var reg in regFiles)
                LogInfo(rtb, $"Profil Outlook trouvé : {Path.GetFileName(reg)} (import manuel recommandé)");

            var rulesFiles = Directory.GetFiles(outlookDataDir, "*.rwz");
            if (rulesFiles.Length > 0)
            {
                var outlookAppData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Outlook");
                Directory.CreateDirectory(outlookAppData);
                foreach (var rule in rulesFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        await Task.Run(() => File.Copy(rule, Path.Combine(outlookAppData, Path.GetFileName(rule)), true), ct);
                        LogSuccess(rtb, $"Fichier de règles restauré : {Path.GetFileName(rule)}");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogError(rtb, $"Erreur règles {Path.GetFileName(rule)} : {ex.Message}");
                    }
                }
            }

            var sharedMailboxFile = Path.Combine(outlookDataDir, "SharedMailboxes.txt");
            if (File.Exists(sharedMailboxFile))
            {
                var sharedMailboxes = (await File.ReadAllLinesAsync(sharedMailboxFile, ct))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                if (sharedMailboxes.Count > 0)
                {
                    LogTitle(rtb, "Boîtes aux lettres partagées à reconfigurer");
                    LogInfo(rtb, "Les boîtes suivantes étaient configurées sur l'ancien poste :");
                    foreach (var mailbox in sharedMailboxes)
                        Log(rtb, $"   → {mailbox}", Color.FromArgb(139, 233, 253));

                    LogInfo(rtb, "");
                    LogInfo(rtb, "COMMENT AJOUTER UNE BOÎTTE PARTAGÉE DANS OUTLOOK :");
                    LogInfo(rtb, "  1. Fichier > Paramètres du compte > Paramètres du compte...");
                    LogInfo(rtb, "  2. Sélectionner votre compte > Modifier > Paramètres supplémentaires");
                    LogInfo(rtb, "  3. Onglet Avancé > Ajouter... > Entrer le nom de la boîte");
                    LogInfo(rtb, "OU via outlook.office.com : clic droit sur Dossiers > Ajouter un dossier partagé");

                    if (OutlookService.CopyToClipboard(sharedMailboxes))
                        LogSuccess(rtb, "Liste copiée dans le presse-papiers ! (Ctrl+V pour coller)");
                }
            }
        }

        private async Task RestoreStickyNotesAsync(string restoreRoot, RichTextBox rtb, CancellationToken ct)
        {
            var stickyBackup = Path.Combine(restoreRoot, "StickyNotes.sqlite");
            if (File.Exists(stickyBackup))
            {
                var stickyDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState");
                Directory.CreateDirectory(stickyDest);

                await Task.Run(() => File.Copy(stickyBackup, Path.Combine(stickyDest, "plum.sqlite"), true), ct);
                LogSuccess(rtb, "Sticky Notes restaurés");
            }
            else
            {
                LogInfo(rtb, "Pas de Sticky Notes à restaurer.");
            }
        }

        /// <summary>
        /// Profil Edge complet : restaure le dossier «EdgeProfile» vers User Data\Default.
        /// ⚠️ Edge doit être fermé pendant la restauration.
        /// </summary>
        private async Task RestoreEdgeProfileAsync(string restoreRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var edgeDest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default");

            if (System.Diagnostics.Process.GetProcessesByName("msedge").Length > 0)
            {
                LogWarning(rtb, "Microsoft Edge est ouvert. Fermez-le avant de restaurer le profil.");
                return;
            }

            await RestoreStep(
                Path.Combine(restoreRoot, "EdgeProfile"),
                edgeDest,
                "Profil Edge",
                rtb, progress, errorList, ct);
        }

        /// <summary>Affiche la liste des lecteurs réseau sauvegardés (recréation manuelle).</summary>
        private async Task RestoreNetworkDrivesInfoAsync(string restoreRoot, RichTextBox rtb)
        {
            var networkDrivesFile = Path.Combine(restoreRoot, "NetworkDrives.txt");
            if (File.Exists(networkDrivesFile))
            {
                var lines = await File.ReadAllLinesAsync(networkDrivesFile);
                LogInfo(rtb, "Lecteurs réseau de l'ancien poste :");
                foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                    Log(rtb, $"   {line}");
                LogWarning(rtb, "Merci de recréer manuellement ces lecteurs réseau.");
            }
            else
            {
                LogInfo(rtb, "Pas de fichier de lecteurs réseau trouvé.");
            }
        }

        private async Task RestoreWallpaperAsync(string restoreRoot, RichTextBox rtb, CancellationToken ct)
        {
            var wallpaperFiles = Directory.GetFiles(restoreRoot, "Wallpaper.*");
            if (wallpaperFiles.Length > 0)
            {
                var wallpaperDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "Themes");
                Directory.CreateDirectory(wallpaperDest);

                await Task.Run(() => File.Copy(wallpaperFiles[0], Path.Combine(wallpaperDest, "TranscodedWallpaper"), true), ct);
                LogSuccess(rtb, "Fond d'écran restauré (visible après reconnexion)");
            }
            else
            {
                LogInfo(rtb, "Pas de fond d'écran à restaurer.");
            }
        }
    }
}
