using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Logique de sauvegarde — parité fonctionnelle complète avec Sauvegarde.ps1.
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
                txtBackupPath.Text = Path.Combine(dialog.SelectedPath, Environment.UserName);
        }

        private async void BtnStartBackup_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBackupPath.Text))
            {
                MessageBox.Show("Veuillez sélectionner un dossier de destination.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // #5 — Calcul de taille estimée avant de démarrer
            UpdateStatus("Calcul de la taille estimée...");
            long estimatedSize = await Task.Run(() => EstimateBackupSize());

            if (estimatedSize > 0)
            {
                var confirm = MessageBox.Show(
                    $"Taille estimée de la sauvegarde : {FileService.FormatSize(estimatedSize)}\n\nContinuer ?",
                    "Confirmer la sauvegarde",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    UpdateStatus("Prêt");
                    return;
                }
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
                lock (_logLock) { _logFilePath = Path.Combine(backupRoot, "Sauvegarde.log"); }

                LogTitle(rtbBackupLog, "Démarrage de la sauvegarde");
                LogInfo(rtbBackupLog, $"Utilisateur : {Environment.UserName}");
                LogInfo(rtbBackupLog, $"Poste : {Environment.MachineName}");
                LogInfo(rtbBackupLog, $"Destination : {backupRoot}");
                if (estimatedSize > 0)
                    LogInfo(rtbBackupLog, $"Taille estimée : {FileService.FormatSize(estimatedSize)}");

                UpdateOldProfileOptionState();
                if (chkPanelBackup.IsChecked("OldProfile"))
                    DetectAndLogOldProfiles(rtbBackupLog);

                var currentUsername   = Environment.UserName;
                string? domainProfilePath = null;
                string? cleanProfilePath  = null;

                if (chkPanelBackup.IsChecked("OldProfile"))
                {
                    var usersDir = Path.GetDirectoryName(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                    if (usersDir != null)
                    {
                        var excluded = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };

                        domainProfilePath = Directory.GetDirectories(usersDir)
                            .FirstOrDefault(d =>
                            {
                                var name = Path.GetFileName(d);
                                return name != null
                                    && name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase)
                                    && !excluded.Contains(name, StringComparer.OrdinalIgnoreCase);
                            });

                        var exactDir = Path.Combine(usersDir, currentUsername);
                        if (Directory.Exists(exactDir)) cleanProfilePath = exactDir;
                    }
                }

                bool doubleBackup = domainProfilePath != null && cleanProfilePath != null;

                if (doubleBackup)
                {
                    LogInfo(rtbBackupLog, $"\u26a0\ufe0f Double profil détecté : « {Path.GetFileName(domainProfilePath)} » + « {currentUsername} »");
                    LogInfo(rtbBackupLog, "La sauvegarde copiera d'abord l'ancien profil domaine, puis le profil actuel.");
                }

                var progress = new Progress<int>(UpdateProgress);

                if (doubleBackup)
                {
                    LogTitle(rtbBackupLog, $"Passe 1 — Ancien profil domaine : {Path.GetFileName(domainProfilePath)}");
                    var steps1 = BuildBackupSteps(backupRoot, domainProfilePath!, progress, errorList, ct,
                        includePublic: false, includeAppData: false);

                    int total1 = steps1.Count, idx1 = 0;
                    foreach (var (name, action) in steps1)
                    {
                        ct.ThrowIfCancellationRequested();
                        idx1++;
                        UpdateStatus($"[Passe 1/2] Sauvegarde {name} ({idx1}/{total1})");
                        await action();
                    }
                    LogSuccess(rtbBackupLog, $"Passe 1 terminée — profil « {Path.GetFileName(domainProfilePath)} » sauvegardé.");
                }

                if (doubleBackup)
                    LogTitle(rtbBackupLog, $"Passe 2 — Profil actuel : {currentUsername}");

                var steps = BuildBackupSteps(backupRoot, cleanProfilePath ?? string.Empty,
                    progress, errorList, ct, includePublic: true, includeAppData: true);

                int totalSteps = steps.Count, currentStep = 0;
                foreach (var (name, action) in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    currentStep++;
                    UpdateStatus(doubleBackup
                        ? $"[Passe 2/2] Sauvegarde {name} ({currentStep}/{totalSteps})"
                        : $"Sauvegarde {name} ({currentStep}/{totalSteps})");
                    await action();
                }

                if (doubleBackup)
                    LogSuccess(rtbBackupLog, $"Passe 2 terminée — profil « {currentUsername} » sauvegardé.");

                LogTitle(rtbBackupLog, "Récapitulatif final");
                UpdateStatus("Calcul de la taille finale...");
                var totalSize = await Task.Run(() => FileService.GetDirectorySize(backupRoot), CancellationToken.None);
                LogInfo(rtbBackupLog, $"Taille totale de la sauvegarde : {FileService.FormatSize(totalSize)}");

                if (errorList.Count > 0)
                {
                    LogTitle(rtbBackupLog, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList) LogWarning(rtbBackupLog, err);
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
                lock (_logLock) { _logFilePath = null; }
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// #5 — Calcule la taille estimée en additionnant les dossiers sélectionnés.
        /// Exécuté sur le thread pool pour ne pas bloquer l'UI.
        /// </summary>
        private long EstimateBackupSize()
        {
            var userProfile    = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDataLocal   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            long total = 0;

            long SizeOf(string path)
            {
                if (!Directory.Exists(path)) return 0;
                try
                {
                    return new DirectoryInfo(path)
                        .EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => { try { return f.Length; } catch { return 0L; } });
                }
                catch { return 0; }
            }

            if (chkPanelBackup.IsChecked("Documents"))    total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (chkPanelBackup.IsChecked("Desktop"))      total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (chkPanelBackup.IsChecked("Downloads"))    total += SizeOf(Path.Combine(userProfile, "Downloads"));
            if (chkPanelBackup.IsChecked("Pictures"))     total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            if (chkPanelBackup.IsChecked("Music"))        total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            if (chkPanelBackup.IsChecked("Videos"))       total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            if (chkPanelBackup.IsChecked("Signatures"))   total += SizeOf(Path.Combine(appDataRoaming, "Microsoft", "Signatures"));
            if (chkPanelBackup.IsChecked("OfficeTemplates"))    total += SizeOf(Path.Combine(appDataRoaming, "Microsoft", "Templates"));
            if (chkPanelBackup.IsChecked("ExcelMacros"))  total += SizeOf(Path.Combine(appDataRoaming, "Microsoft", "Excel", "XLSTART"));
            if (chkPanelBackup.IsChecked("Sap"))          total += SizeOf(Path.Combine(appDataRoaming, "SAP"));
            if (btnBrowserPickerBackup.IsSelected("Microsoft Edge"))  total += SizeOf(Path.Combine(appDataLocal,   "Microsoft", "Edge", "User Data", "Default"));
            if (chkPanelBackup.IsChecked("StickyNotes"))  total += SizeOf(Path.Combine(appDataLocal,   "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState"));
            if (chkPanelBackup.IsChecked("Public"))       total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));

            return total;
        }

        // ───────────────────────────────────────────────────────────────────────────────
        //  Construction des étapes de sauvegarde
        // ───────────────────────────────────────────────────────────────────────────────

        private List<(string Name, Func<Task> Action)> BuildBackupSteps(
            string backupRoot,
            string sourceProfileOverride,
            IProgress<int> progress,
            List<string> errorList,
            CancellationToken ct,
            bool includePublic,
            bool includeAppData)
        {
            var steps = new List<(string Name, Func<Task> Action)>();

            bool useOverride = !string.IsNullOrEmpty(sourceProfileOverride)
                               && Directory.Exists(sourceProfileOverride);

            string Src(string relativePath) =>
                useOverride ? Path.Combine(sourceProfileOverride, relativePath) : relativePath;

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (chkPanelBackup.IsChecked("Documents")) steps.Add(("Documents", () => CopyStep(
                useOverride ? Src("Documents") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(backupRoot, "Documents"), "Documents", rtbBackupLog, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Desktop")) steps.Add(("Bureau", () => CopyStep(
                useOverride ? Src("Desktop") : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Path.Combine(backupRoot, "Desktop"), "Bureau", rtbBackupLog, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Downloads")) steps.Add(("Téléchargements", () => CopyStep(
                useOverride ? Src("Downloads") : Path.Combine(userProfile, "Downloads"),
                Path.Combine(backupRoot, "Downloads"), "Téléchargements", rtbBackupLog, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Pictures")) steps.Add(("Images", () => CopyStep(
                useOverride ? Src("Pictures") : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Path.Combine(backupRoot, "Pictures"), "Images", rtbBackupLog, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Music")) steps.Add(("Musique", () => CopyStep(
                useOverride ? Src("Music") : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Path.Combine(backupRoot, "Music"), "Musique", rtbBackupLog, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Videos")) steps.Add(("Vidéos", () => CopyStep(
                useOverride ? Src("Videos") : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Path.Combine(backupRoot, "Videos"), "Vidéos", rtbBackupLog, progress, errorList, ct)));

            if (includeAppData)
            {
                var appDataRoaming = useOverride
                    ? Path.Combine(sourceProfileOverride, "AppData", "Roaming")
                    : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (chkPanelBackup.IsChecked("Signatures"))  steps.Add(("Signatures Outlook",  () => BackupSignaturesAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("OfficeTemplates"))   steps.Add(("Modèles Office",       () => CopyStep(Path.Combine(appDataRoaming, "Microsoft", "Templates"),     Path.Combine(backupRoot, "Templates"),               "Modèles Office",       rtbBackupLog, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("ExcelMacros")) steps.Add(("Macros Excel (XLSTART)",() => CopyStep(Path.Combine(appDataRoaming, "Microsoft", "Excel", "XLSTART"), Path.Combine(backupRoot, "Excel", "XLSTART"),        "Macros Excel (XLSTART)",rtbBackupLog, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("Sap"))         steps.Add(("SAP GUI",              () => CopyStep(Path.Combine(appDataRoaming, "SAP"),                          Path.Combine(backupRoot, "SAP"),                     "SAP GUI",               rtbBackupLog, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("Outlook"))     steps.Add(("Données Outlook",      () => BackupOutlookDataAsync(backupRoot, rtbBackupLog, ct)));
                if (chkPanelBackup.IsChecked("OneNote"))     steps.Add(("OneNote (registre)",   () => BackupOneNoteAsync(backupRoot, rtbBackupLog)));
                if (chkPanelBackup.IsChecked("StickyNotes")) steps.Add(("Sticky Notes",         () => BackupStickyNotesAsync(backupRoot, rtbBackupLog, ct)));
                if (btnBrowserPickerBackup.IsSelected("Microsoft Edge")) steps.Add(("Profil Edge",          () => BackupEdgeProfileAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("Wallpaper"))   steps.Add(("Fond d'écran",          () => BackupWallpaperAsync(backupRoot, rtbBackupLog)));
                if (chkPanelBackup.IsChecked("NetworkDrives"))steps.Add(("Lecteurs réseau",     () => BackupNetworkDrivesAsync(backupRoot, rtbBackupLog)));
                if (chkPanelBackup.IsChecked("IpSoftphone")) steps.Add(("IP Desktop Softphone", () => BackupIpDesktopSoftphoneAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
            }

            if (includePublic && chkPanelBackup.IsChecked("Public")) steps.Add(("Dossier Public", () => CopyStep(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                Path.Combine(backupRoot, "Public"), "Dossier Public", rtbBackupLog, progress, errorList, ct)));

            return steps;
        }

        // ───────────────────────────────────────────────────────────────────────────────
        //  Méthodes de copie / backup individuelles
        // ───────────────────────────────────────────────────────────────────────────────

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
                        LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}");
                    }
                }

                await File.WriteAllLinesAsync(Path.Combine(outlookDir, "PST_Paths.txt"), pstPaths, ct);
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
                    LogSuccess(rtb, $"Règles sauvegardées : {Path.GetFileName(rule)}");
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { LogError(rtb, $"Erreur règles {Path.GetFileName(rule)} : {ex.Message}"); }
            }

            Log(rtb, "Recherche des boîtes aux lettres partagées...");
            var sharedMailboxes = await Task.Run(OutlookService.FindSharedMailboxes, ct);
            if (sharedMailboxes.Count > 0)
            {
                LogInfo(rtb, $"{sharedMailboxes.Count} boîte(s) partagée(s) détectée(s)");
                foreach (var mb in sharedMailboxes) Log(rtb, $"   \u2192 {mb}");
                await File.WriteAllLinesAsync(Path.Combine(outlookDir, "SharedMailboxes.txt"), sharedMailboxes, ct);
                LogSuccess(rtb, "Liste sauvegardée dans SharedMailboxes.txt");
            }
            else
            {
                LogInfo(rtb, "Aucune boîte aux lettres partagée détectée.");
            }
        }

        private async Task BackupOneNoteAsync(string backupRoot, RichTextBox rtb)
        {
            Log(rtb, "Export des clés de registre OneNote...");
            await Task.Run(() =>
            {
                RegistryService.BackupOneNoteKeys(backupRoot, msg => LogInfo(rtb, msg));