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

        private void BtnCancelBackup_Click(object? sender, EventArgs e)
            => CancelCurrentOperation(BackupLogBox);

        private async void BtnStartBackup_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBackupPath.Text))
            {
                MessageBox.Show("Veuillez sélectionner un dossier de destination.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            BackupLogBox.Clear();
            _ctsBackup = new CancellationTokenSource();
            var ct = _ctsBackup.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var backupRoot = txtBackupPath.Text;
                Directory.CreateDirectory(backupRoot);
                lock (_logLock) { _logFilePath = Path.Combine(backupRoot, "Sauvegarde.log"); }

                LogTitle(BackupLogBox, "Démarrage de la sauvegarde");
                LogInfo(BackupLogBox, $"Utilisateur : {Environment.UserName}");
                LogInfo(BackupLogBox, $"Poste : {Environment.MachineName}");
                LogInfo(BackupLogBox, $"Destination : {backupRoot}");
                if (estimatedSize > 0)
                    LogInfo(BackupLogBox, $"Taille estimée : {FileService.FormatSize(estimatedSize)}");

                UpdateOldProfileOptionState();
                if (chkPanelBackup.IsChecked("OldProfile"))
                    DetectAndLogOldProfiles(BackupLogBox);

                var currentUsername   = Environment.UserName;
                string? domainProfilePath = null;
                string? cleanProfilePath  = null;

                if (chkPanelBackup.IsChecked("OldProfile"))
                {
                    var usersDir = Path.GetDirectoryName(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                    if (usersDir != null)
                    {
                        domainProfilePath = Directory.GetDirectories(usersDir)
                            .FirstOrDefault(d =>
                            {
                                var name = Path.GetFileName(d);
                                return name != null
                                    && name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase)
                                    && !ExcludedProfiles.Contains(name, StringComparer.OrdinalIgnoreCase);
                            });

                        var exactDir = Path.Combine(usersDir, currentUsername);
                        if (Directory.Exists(exactDir)) cleanProfilePath = exactDir;
                    }
                }

                bool doubleBackup = domainProfilePath != null && cleanProfilePath != null;

                if (doubleBackup)
                {
                    LogInfo(BackupLogBox, $"\u26a0\ufe0f Double profil détecté : « {Path.GetFileName(domainProfilePath)} » + « {currentUsername} »");
                    LogInfo(BackupLogBox, "La sauvegarde copiera d'abord l'ancien profil domaine, puis le profil actuel.");
                }

                var progress = new Progress<int>(UpdateProgress);

                if (doubleBackup)
                {
                    LogTitle(BackupLogBox, $"Passe 1 — Ancien profil domaine : {Path.GetFileName(domainProfilePath)}");
                    var steps1 = BuildBackupSteps(backupRoot, domainProfilePath!, progress, errorList,
                        includePublic: false, includeAppData: false, ct);

                    int total1 = steps1.Count, idx1 = 0;
                    foreach (var (name, action) in steps1)
                    {
                        ct.ThrowIfCancellationRequested();
                        idx1++;
                        UpdateStatus($"[Passe 1/2] Sauvegarde {name} ({idx1}/{total1})");
                        await action();
                    }
                    LogSuccess(BackupLogBox, $"Passe 1 terminée — profil « {Path.GetFileName(domainProfilePath)} » sauvegardé.");
                }

                if (doubleBackup)
                    LogTitle(BackupLogBox, $"Passe 2 — Profil actuel : {currentUsername}");

                var steps = BuildBackupSteps(backupRoot, cleanProfilePath ?? string.Empty,
                    progress, errorList, includePublic: true, includeAppData: true, ct);

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
                    LogSuccess(BackupLogBox, $"Passe 2 terminée — profil « {currentUsername} » sauvegardé.");

                LogTitle(BackupLogBox, "Récapitulatif final");
                UpdateStatus("Calcul de la taille finale...");
                var totalSize = await Task.Run(() => FileService.GetDirectorySize(backupRoot), CancellationToken.None);
                LogInfo(BackupLogBox, $"Taille totale de la sauvegarde : {FileService.FormatSize(totalSize)}");

                if (errorList.Count > 0)
                {
                    LogTitle(BackupLogBox, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList) LogWarning(BackupLogBox, err);
                }

                LogTitle(BackupLogBox, "Sauvegarde terminée");
                LogSuccess(BackupLogBox, $"Fichiers sauvegardés dans : {backupRoot}");
                UpdateStatus("Sauvegarde terminée avec succès");
                ToastService.Show(this, "Sauvegarde terminée avec succès !", ToastKind.Success);

                MessageBox.Show($"Sauvegarde terminée avec succès !\n\nDossier : {backupRoot}\nTaille : {FileService.FormatSize(totalSize)}",
                    "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                LogWarning(BackupLogBox, "Sauvegarde annulée par l'utilisateur.");
                UpdateStatus("Sauvegarde annulée");
            }
            catch (Exception ex)
            {
                LogError(BackupLogBox, $"Erreur : {ex.Message}");
                UpdateStatus("Erreur lors de la sauvegarde");
                MessageBox.Show($"Erreur lors de la sauvegarde :\n{ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                HideProgress();
                lock (_logLock) { _logFilePath = null; }
                _ctsBackup?.Dispose();
                _ctsBackup = null;
            }
        }

        private long EstimateBackupSize()
        {
            var userProfile    = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDataLocal   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            long total = 0;

            static long SizeOf(string? path)
            {
                if (path == null || !Directory.Exists(path)) return 0;
                try
                {
                    return new DirectoryInfo(path)
                        .EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => { try { return f.Length; } catch { return 0L; } });
                }
                catch { return 0; }
            }

            if (chkPanelBackup.IsChecked("Documents"))       total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (chkPanelBackup.IsChecked("Desktop"))         total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (chkPanelBackup.IsChecked("Downloads"))       total += SizeOf(Path.Combine(userProfile, "Downloads"));
            if (chkPanelBackup.IsChecked("Pictures"))        total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            if (chkPanelBackup.IsChecked("Music"))           total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            if (chkPanelBackup.IsChecked("Videos"))          total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            if (chkPanelBackup.IsChecked("Signatures"))      total += SizeOf(Path.Combine(appDataRoaming, "Microsoft", "Signatures"));
            if (chkPanelBackup.IsChecked("OfficeTemplates")) total += SizeOf(Path.Combine(appDataRoaming, "Microsoft", "Templates"));
            if (chkPanelBackup.IsChecked("ExcelMacros"))     total += SizeOf(Path.Combine(appDataRoaming, "Microsoft", "Excel", "XLSTART"));
            if (chkPanelBackup.IsChecked("Sap"))             total += SizeOf(Path.Combine(appDataRoaming, "SAP"));
            if (chkPanelBackup.IsChecked("StickyNotes"))     total += SizeOf(Path.Combine(appDataLocal, "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState"));
            if (chkPanelBackup.IsChecked("Public"))          total += SizeOf(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));

            // Navigateurs — utilise ProfilePathFactory pour la taille réelle
            foreach (var browser in BrowserService.All)
            {
                if (chkPanelBackup.IsChecked(browser.Key))
                {
                    try { total += SizeOf(browser.ProfilePathFactory()); }
                    catch { }
                }
            }

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
            bool includePublic,
            bool includeAppData,
            CancellationToken ct)
        {
            var steps = new List<(string Name, Func<Task> Action)>();

            bool useOverride = !string.IsNullOrEmpty(sourceProfileOverride)
                               && Directory.Exists(sourceProfileOverride);

            string Src(string relativePath) =>
                useOverride ? Path.Combine(sourceProfileOverride, relativePath) : relativePath;

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (chkPanelBackup.IsChecked("Documents")) steps.Add(("Documents", () => CopyStep(
                useOverride ? Src("Documents") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(backupRoot, "Documents"), "Documents", BackupLogBox, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Desktop")) steps.Add(("Bureau", () => CopyStep(
                useOverride ? Src("Desktop") : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Path.Combine(backupRoot, "Desktop"), "Bureau", BackupLogBox, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Downloads")) steps.Add(("Téléchargements", () => CopyStep(
                useOverride ? Src("Downloads") : Path.Combine(userProfile, "Downloads"),
                Path.Combine(backupRoot, "Downloads"), "Téléchargements", BackupLogBox, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Pictures")) steps.Add(("Images", () => CopyStep(
                useOverride ? Src("Pictures") : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Path.Combine(backupRoot, "Pictures"), "Images", BackupLogBox, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Music")) steps.Add(("Musique", () => CopyStep(
                useOverride ? Src("Music") : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Path.Combine(backupRoot, "Music"), "Musique", BackupLogBox, progress, errorList, ct)));

            if (chkPanelBackup.IsChecked("Videos")) steps.Add(("Vidéos", () => CopyStep(
                useOverride ? Src("Videos") : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Path.Combine(backupRoot, "Videos"), "Vidéos", BackupLogBox, progress, errorList, ct)));

            if (includeAppData)
            {
                var appDataRoaming = useOverride
                    ? Path.Combine(sourceProfileOverride, "AppData", "Roaming")
                    : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (chkPanelBackup.IsChecked("Signatures"))      steps.Add(("Signatures Outlook",    () => BackupSignaturesAsync(backupRoot, BackupLogBox, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("OfficeTemplates")) steps.Add(("Modèles Office",        () => CopyStep(Path.Combine(appDataRoaming, "Microsoft", "Templates"),      Path.Combine(backupRoot, "Templates"),        "Modèles Office",        BackupLogBox, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("ExcelMacros"))     steps.Add(("Macros Excel (XLSTART)",() => CopyStep(Path.Combine(appDataRoaming, "Microsoft", "Excel", "XLSTART"),Path.Combine(backupRoot, "Excel", "XLSTART"), "Macros Excel (XLSTART)", BackupLogBox, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("Sap"))             steps.Add(("SAP GUI",               () => CopyStep(Path.Combine(appDataRoaming, "SAP"),                           Path.Combine(backupRoot, "SAP"),              "SAP GUI",                BackupLogBox, progress, errorList, ct)));
                if (chkPanelBackup.IsChecked("Outlook"))         steps.Add(("Données Outlook",       () => BackupOutlookDataAsync(backupRoot, BackupLogBox, ct)));
                if (chkPanelBackup.IsChecked("OneNote"))         steps.Add(("OneNote (registre)",    () => BackupOneNoteAsync(backupRoot, BackupLogBox)));
                if (chkPanelBackup.IsChecked("StickyNotes"))     steps.Add(("Sticky Notes",          () => BackupStickyNotesAsync(backupRoot, BackupLogBox, ct)));
                if (chkPanelBackup.IsChecked("Wallpaper"))       steps.Add(("Fond d'écran",          () => BackupWallpaperAsync(backupRoot, BackupLogBox)));
                if (chkPanelBackup.IsChecked("NetworkDrives"))   steps.Add(("Lecteurs réseau",       () => BackupNetworkDrivesAsync(backupRoot, BackupLogBox)));
                if (chkPanelBackup.IsChecked("IpSoftphone"))     steps.Add(("IP Desktop Softphone",  () => BackupIpDesktopSoftphoneAsync(backupRoot, BackupLogBox, progress, errorList, ct)));

                // Navigateurs — boucle générique sur BrowserService.All
                foreach (var browser in BrowserService.All)
                {
                    if (chkPanelBackup.IsChecked(browser.Key))
                    {
                        var b = browser; // capture locale
                        steps.Add(($"{b.DisplayName}",
                            () => BackupBrowserAsync(b, backupRoot, BackupLogBox, progress, errorList, ct)));
                    }
                }
            }

            if (includePublic && chkPanelBackup.IsChecked("Public")) steps.Add(("Dossier Public", () => CopyStep(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                Path.Combine(backupRoot, "Public"), "Dossier Public", BackupLogBox, progress, errorList, ct)));

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
                foreach (var mb in sharedMailboxes) Log(rtb, $"   \u2192 {mb}", Color.FromArgb(139, 233, 253));
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
                RegistryService.BackupOpenNotebookKey(backupRoot, msg => LogInfo(rtb, msg));
            });
        }

        private async Task BackupStickyNotesAsync(string backupRoot, RichTextBox rtb, CancellationToken ct)
        {
            var localState = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState");

            var stickyPath = Path.Combine(localState, "plum.sqlite");

            if (!File.Exists(stickyPath))
            {
                LogInfo(rtb, "Aucune donnée Sticky Notes trouvée.");
                return;
            }

            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var src  = Path.Combine(localState, "plum.sqlite" + suffix);
                var dest = Path.Combine(backupRoot,  "StickyNotes.sqlite" + suffix);
                if (File.Exists(src))
                    await Task.Run(() => File.Copy(src, dest, overwrite: true), ct);
            }

            LogSuccess(rtb, "Sticky Notes sauvegardés (sqlite + WAL/SHM)");
        }

        /// <summary>
        /// Sauvegarde générique du profil d'un navigateur :
        ///   1. Tue le processus si actif
        ///   2. Copie le dossier de profil
        ///   3. Relance le navigateur s'il était ouvert
        /// </summary>
        private async Task BackupBrowserAsync(
            BrowserDef browser,
            string backupRoot,
            RichTextBox rtb,
            IProgress<int> progress,
            List<string> errorList,
            CancellationToken ct)
        {
            var profilePath = browser.ProfilePathFactory();
            if (profilePath == null || !Directory.Exists(profilePath))
            {
                LogInfo(rtb, $"{browser.DisplayName} : aucun profil trouvé, ignoré.");
                return;
            }

            // ── Fermeture du navigateur si actif ────────────────────────
            var procs = System.Diagnostics.Process.GetProcessesByName(browser.ProcessName);
            bool wasRunning = procs.Length > 0;

            if (wasRunning)
            {
                LogInfo(rtb, $"{browser.DisplayName} : {procs.Length} processus détecté(s) — fermeture avant copie...");
                await Task.Run(() =>
                {
                    foreach (var proc in procs)
                    {
                        try { proc.Kill(entireProcessTree: true); } catch { }
                        finally { proc.Dispose(); }
                    }
                }, ct);

                // Attente extinction (max 6 s)
                var deadline = DateTime.UtcNow.AddSeconds(6);
                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(300, ct);
                    var remaining = System.Diagnostics.Process.GetProcessesByName(browser.ProcessName);
                    bool done = remaining.Length == 0;
                    foreach (var p in remaining) p.Dispose();
                    if (done) break;
                }
                LogInfo(rtb, $"{browser.DisplayName} fermé — démarrage de la copie.");
            }
            else
            {
                foreach (var p in procs) p.Dispose();
            }

            // ── Copie du profil ─────────────────────────────────────────
            var dest = Path.Combine(backupRoot, browser.BackupSubFolder);
            await CopyStep(profilePath, dest, $"Profil {browser.DisplayName}", rtb, progress, errorList, ct);

            // ── Relance si nécessaire ───────────────────────────────────
            if (wasRunning)
            {
                var exe = browser.ExecutableCandidates.FirstOrDefault(File.Exists);
                if (exe != null)
                {
                    await Task.Run(() =>
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo { FileName = exe, UseShellExecute = true }),
                        CancellationToken.None);
                    LogInfo(rtb, $"{browser.DisplayName} relancé.");
                }
                else
                {
                    LogWarning(rtb, $"{browser.DisplayName} : impossible de relancer — exécutable introuvable.");
                }
            }
        }

        private async Task BackupWallpaperAsync(string backupRoot, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                try
                {
                    string? wallpaperPath = null;
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                        wallpaperPath = key?.GetValue("Wallpaper") as string;

                    if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                    {
                        var ext  = Path.GetExtension(wallpaperPath);
                        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                        var dest = Path.Combine(backupRoot, "Wallpaper" + ext);
                        File.Copy(wallpaperPath, dest, true);
                        LogSuccess(rtb, $"Fond d'écran sauvegardé ({FileService.FormatSize(new FileInfo(dest).Length)})");
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
                    else LogInfo(rtb, "Aucun fond d'écran personnalisé trouvé.");
                }
                catch (Exception ex) { LogError(rtb, $"Erreur fond d'écran : {ex.Message}"); }
            });
        }

        private async Task BackupNetworkDrivesAsync(string backupRoot, RichTextBox rtb)
        {
            await Task.Run(() =>
            {
                var lines = new List<string>();
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Network");
                    if (key == null)
                    {
                        LogInfo(rtb, "Aucun lecteur réseau trouvé.");
                        return;
                    }
                    foreach (var driveName in key.GetSubKeyNames())
                    {
                        using var sub = key.OpenSubKey(driveName);
                        var remote  = sub?.GetValue("RemotePath") as string ?? "";
                        var label   = sub?.GetValue("UserName")   as string ?? "";
                        lines.Add($"{driveName}|{remote}|{label}");
                    }
                }
                catch (Exception ex) { LogError(rtb, $"Erreur lecteurs réseau : {ex.Message}"); }

                if (lines.Count > 0)
                {
                    File.WriteAllLines(Path.Combine(backupRoot, "NetworkDrives.txt"), lines);
                    LogSuccess(rtb, $"{lines.Count} lecteur(s) réseau sauvegardé(s)");
                }
                else
                {
                    LogInfo(rtb, "Aucun lecteur réseau à sauvegarder.");
                }
            });
        }

        private async Task BackupIpDesktopSoftphoneAsync(string backupRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var vendorCandidates = new[]
            {
                "Alcatel-Lucent Enterprise",
                "ALE International",
                "Unify",
                "Mitel Networks",
            };

            bool found = false;
            foreach (var vendor in vendorCandidates)
            {
                var vendorSrc = Path.Combine(appDataRoaming, vendor, "IP Desktop Softphone");
                if (!Directory.Exists(vendorSrc)) continue;

                found = true;
                var vendorDest = Path.Combine(backupRoot, "IpDesktopSoftphone", vendor);
                await CopyStep(vendorSrc, vendorDest, $"IP Desktop Softphone ({vendor})",
                    rtb, progress, errorList, ct);
            }

            if (!found)
                LogInfo(rtb, "IP Desktop Softphone : aucun profil trouvé.");
        }
    }
}
