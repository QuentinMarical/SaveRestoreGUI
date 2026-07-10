using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Logique de restauration — parité fonctionnelle complète avec Restauration.ps1.
    /// </summary>
    public partial class MainForm
    {
        private void BtnBrowseRestore_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Sélectionnez le dossier contenant la sauvegarde"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var selected = dialog.SelectedPath;

            if (!BackupValidator.IsValidBackupFolder(selected))
            {
                var warn = MessageBox.Show(
                    $"Le dossier sélectionné ne semble pas contenir une sauvegarde SaveRestoreGUI :\n{selected}\n\nContinuer quand même ?",
                    "Dossier douteux",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (warn != DialogResult.Yes) return;
            }

            txtRestorePath.Text = selected;
        }

        private void BtnCancelRestore_Click(object? sender, EventArgs e)
            => CancelCurrentOperation(RestoreLogBox);

        private async void BtnStartRestore_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRestorePath.Text) || !Directory.Exists(txtRestorePath.Text))
            {
                MessageBox.Show("Veuillez sélectionner un dossier source valide.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RestoreLogBox.Clear();
            _ctsRestore = new CancellationTokenSource();
            var ct = _ctsRestore.Token;
            var errorList = new List<string>();

            SetControlsEnabled(false);

            try
            {
                var restoreRoot = txtRestorePath.Text;
                lock (_logLock) { _logFilePath = Path.Combine(restoreRoot, "Restauration.log"); }

                LogTitle(RestoreLogBox, "Démarrage de la restauration");
                LogInfo(RestoreLogBox, $"Source : {restoreRoot}");

                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var progress = new Progress<int>(UpdateProgress);

                var steps = new List<(string Name, Func<Task> Action)>();

                if (chkPanelRestore.IsChecked("Documents")) steps.Add(("Documents", () => RestoreStep(
                    Path.Combine(restoreRoot, "Documents"), Path.Combine(userProfile, "Documents"),
                    "Documents", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Desktop")) steps.Add(("Bureau", () => RestoreStep(
                    Path.Combine(restoreRoot, "Desktop"), Path.Combine(userProfile, "Desktop"),
                    "Bureau", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Downloads")) steps.Add(("Téléchargements", () => RestoreStep(
                    Path.Combine(restoreRoot, "Downloads"), Path.Combine(userProfile, "Downloads"),
                    "Téléchargements", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Pictures")) steps.Add(("Images", () => RestoreStep(
                    Path.Combine(restoreRoot, "Pictures"), Path.Combine(userProfile, "Pictures"),
                    "Images", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Music")) steps.Add(("Musique", () => RestoreStep(
                    Path.Combine(restoreRoot, "Music"), Path.Combine(userProfile, "Music"),
                    "Musique", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Videos")) steps.Add(("Vidéos", () => RestoreStep(
                    Path.Combine(restoreRoot, "Videos"), Path.Combine(userProfile, "Videos"),
                    "Vidéos", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("OneNote"))        steps.Add(("Clés registre OneNote", () => RestoreOneNoteAsync(restoreRoot, RestoreLogBox)));

                if (chkPanelRestore.IsChecked("Signatures")) steps.Add(("Signatures Outlook", () => RestoreStep(
                    Path.Combine(restoreRoot, "Signatures"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Signatures"),
                    "Signatures Outlook", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("OfficeTemplates")) steps.Add(("Modèles Office", () => RestoreStep(
                    Path.Combine(restoreRoot, "Templates"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates"),
                    "Modèles Office", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("ExcelMacros")) steps.Add(("Macros Excel (XLSTART)", () => RestoreStep(
                    Path.Combine(restoreRoot, "Excel", "XLSTART"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Excel", "XLSTART"),
                    "Macros Excel (XLSTART)", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Sap")) steps.Add(("SAP GUI", () => RestoreStep(
                    Path.Combine(restoreRoot, "SAP"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SAP"),
                    "SAP GUI", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Public")) steps.Add(("Dossier Public", () => RestoreStep(
                    Path.Combine(restoreRoot, "Public"),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    "Dossier Public", RestoreLogBox, progress, errorList, ct)));

                if (chkPanelRestore.IsChecked("Outlook"))      steps.Add(("Données Outlook",      () => RestoreOutlookDataAsync(restoreRoot, RestoreLogBox, ct)));
                if (chkPanelRestore.IsChecked("StickyNotes"))  steps.Add(("Sticky Notes",         () => RestoreStickyNotesAsync(restoreRoot, RestoreLogBox, ct)));
                if (chkPanelRestore.IsChecked("NetworkDrives")) steps.Add(("Lecteurs réseau",     () => RestoreNetworkDrivesInfoAsync(restoreRoot, RestoreLogBox)));
                if (chkPanelRestore.IsChecked("Wallpaper"))    steps.Add(("Fond d'écran",          () => RestoreWallpaperAsync(restoreRoot, RestoreLogBox, ct)));
                if (chkPanelRestore.IsChecked("Theme"))        steps.Add(("Thème Windows",         () => RunServiceStepAsync("Thème Windows",     RestoreLogBox, () => ThemeService.Restore(restoreRoot,       m => LogInfo(RestoreLogBox, m), m => LogSuccess(RestoreLogBox, m), m => LogWarning(RestoreLogBox, m)))));
                if (chkPanelRestore.IsChecked("Taskbar"))      steps.Add(("Barre des tâches",      () => RunServiceStepAsync("Barre des tâches",  RestoreLogBox, () => TaskbarService.Restore(restoreRoot,     m => LogInfo(RestoreLogBox, m), m => LogSuccess(RestoreLogBox, m), m => LogWarning(RestoreLogBox, m)))));
                if (chkPanelRestore.IsChecked("SystemState"))  steps.Add(("Fonctions Windows",     () => RunServiceStepAsync("Fonctions Windows", RestoreLogBox, () => SystemStateService.Restore(restoreRoot, m => LogInfo(RestoreLogBox, m), m => LogSuccess(RestoreLogBox, m), m => LogWarning(RestoreLogBox, m)))));
                if (chkPanelRestore.IsChecked("IpSoftphone"))  steps.Add(("IP Desktop Softphone",  () => RestoreIpDesktopSoftphoneAsync(restoreRoot, RestoreLogBox, progress, errorList, ct)));

                // Navigateurs — boucle générique sur BrowserService.All
                foreach (var browser in BrowserService.All)
                {
                    if (chkPanelRestore.IsChecked(browser.Key))
                    {
                        var b = browser; // capture locale
                        steps.Add(($"{b.DisplayName}",
                            () => RestoreBrowserAsync(b, restoreRoot, RestoreLogBox, progress, errorList, ct)));
                    }
                }

                int totalSteps = steps.Count;
                int currentStep = 0;

                foreach (var (name, action) in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    currentStep++;
                    UpdateStatus($"Restauration {name} ({currentStep}/{totalSteps})");
                    await action();
                }

                if (chkPanelRestore.IsChecked("LaunchApps"))
                {
                    LogTitle(RestoreLogBox, "Lancement des applications");
                    await Task.Run(() => AppLauncherService.LaunchApplications(
                        msg => LogInfo(RestoreLogBox, msg)), CancellationToken.None);

                    LogTitle(RestoreLogBox, "Synchronisation OneDrive");
                    await Task.Run(() => AppLauncherService.OpenOneDriveBackupSettings(
                        msg => LogInfo(RestoreLogBox, msg)), CancellationToken.None);
                }

                if (errorList.Count > 0)
                {
                    LogTitle(RestoreLogBox, "Résumé des erreurs rencontrées");
                    foreach (var err in errorList)
                        LogWarning(RestoreLogBox, err);
                }

                LogTitle(RestoreLogBox, "Restauration terminée");
                UpdateStatus("Restauration terminée avec succès");
                ToastService.Show(this, "Restauration terminée avec succès !", ToastKind.Success);

                MessageBox.Show("Restauration terminée avec succès !", "Succès",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                LogWarning(RestoreLogBox, "Restauration annulée par l'utilisateur.");
                UpdateStatus("Restauration annulée");
            }
            catch (Exception ex)
            {
                LogError(RestoreLogBox, $"Erreur : {ex.Message}");
                UpdateStatus("Erreur lors de la restauration");
                MessageBox.Show($"Erreur lors de la restauration :\n{ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                HideProgress();
                lock (_logLock) { _logFilePath = null; }
                _ctsRestore?.Dispose();
                _ctsRestore = null;
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

        private async Task RestoreOneNoteAsync(string restoreRoot, RichTextBox rtb)
        {
            Log(rtb, "Import des clés de registre...");
            await Task.Run(() => RegistryService.RestoreOneNoteKeys(restoreRoot, msg => LogInfo(rtb, msg)));
        }

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
                string[] possibleDirs = [Path.Combine(docsPath, "Outlook Files"), Path.Combine(docsPath, "Fichiers Outlook")];
                string mainOutlookDir = possibleDirs.FirstOrDefault(Directory.Exists) ?? possibleDirs[0];
                Directory.CreateDirectory(mainOutlookDir);

                foreach (var pst in pstFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    var destPath = Path.Combine(mainOutlookDir, Path.GetFileName(pst));
                    try
                    {
                        await Task.Run(() => File.Copy(pst, destPath, true), ct);
                        LogSuccess(rtb, $"PST restauré : {Path.GetFileName(pst)} ({FileService.FormatSize(new FileInfo(destPath).Length)})");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { LogError(rtb, $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}"); }
                }

                LogInfo(rtb, $"PST copiés dans : {mainOutlookDir}");
                LogInfo(rtb, "Pour rattacher les archives : Fichier > Ouvrir et exporter > Ouvrir le fichier de données Outlook");
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
            if (regFiles.Length > 0)
            {
                LogTitle(rtb, "Import des profils Outlook (.reg)");
                foreach (var reg in regFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        await Task.Run(() =>
                            RegistryService.ImportRegFile(reg, msg => LogInfo(rtb, msg)), ct);
                        LogSuccess(rtb, $"Profil importé : {Path.GetFileName(reg)}");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogError(rtb, $"Erreur import {Path.GetFileName(reg)} : {ex.Message}");
                    }
                }
            }

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
                        LogSuccess(rtb, $"Règles restaurées : {Path.GetFileName(rule)}");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { LogError(rtb, $"Erreur règles {Path.GetFileName(rule)} : {ex.Message}"); }
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
                        Log(rtb, $"   \u2192 {mailbox}", Color.FromArgb(139, 233, 253));

                    LogInfo(rtb, "");
                    LogInfo(rtb, "COMMENT AJOUTER UNE BOÎTE PARTAGÉE DANS OUTLOOK :");
                    LogInfo(rtb, "  1. Fichier > Paramètres du compte > Paramètres du compte...");
                    LogInfo(rtb, "  2. Sélectionner votre compte > Modifier > Paramètres supplémentaires");
                    LogInfo(rtb, "  3. Onglet Avancé > Ajouter... > Entrer le nom de la boîte");

                    if (OutlookService.CopyToClipboard(sharedMailboxes))
                        LogSuccess(rtb, "Liste copiée dans le presse-papiers ! (Ctrl+V pour coller)");
                }
            }
        }

        private async Task RestoreStickyNotesAsync(string restoreRoot, RichTextBox rtb, CancellationToken ct)
        {
            var stickyDest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState");

            var filesToRestore = new[]
            {
                (Backup: "StickyNotes.sqlite",     Dest: "plum.sqlite"),
                (Backup: "StickyNotes.sqlite-wal", Dest: "plum.sqlite-wal"),
                (Backup: "StickyNotes.sqlite-shm", Dest: "plum.sqlite-shm")
            };

            var existingBackups = filesToRestore
                .Where(f => File.Exists(Path.Combine(restoreRoot, f.Backup)))
                .ToList();

            if (existingBackups.Count == 0)
            {
                LogInfo(rtb, "Pas de Sticky Notes à restaurer.");
                return;
            }

            Directory.CreateDirectory(stickyDest);

            foreach (var file in existingBackups)
            {
                var sourcePath = Path.Combine(restoreRoot, file.Backup);
                var destPath   = Path.Combine(stickyDest, file.Dest);
                await Task.Run(() => File.Copy(sourcePath, destPath, true), ct);
            }

            LogSuccess(rtb, "Sticky Notes restaurés (sqlite + WAL/SHM)");
        }

        /// <summary>
        /// Restauration générique du profil d'un navigateur depuis la sauvegarde.
        /// Vérifie que le navigateur n'est pas actif, copie le profil, relance si nécessaire.
        /// </summary>
        private async Task RestoreBrowserAsync(
            BrowserDef browser,
            string restoreRoot,
            RichTextBox rtb,
            IProgress<int> progress,
            List<string> errorList,
            CancellationToken ct)
        {
            var backupFolder = Path.Combine(restoreRoot, browser.BackupSubFolder);
            if (!Directory.Exists(backupFolder))
            {
                LogInfo(rtb, $"{browser.DisplayName} : aucune sauvegarde trouvée dans {browser.BackupSubFolder}, ignoré.");
                return;
            }

            var profileDest = browser.ProfilePathFactory();
            if (profileDest == null)
            {
                LogWarning(rtb, $"{browser.DisplayName} : impossible de déterminer le dossier de profil cible — restauration ignorée.");
                return;
            }

            // ── Fermeture du navigateur si actif ────────────────────────
            var procs = System.Diagnostics.Process.GetProcessesByName(browser.ProcessName);
            bool wasRunning = procs.Length > 0;

            if (wasRunning)
            {
                LogWarning(rtb, $"{browser.DisplayName} est ouvert — fermeture avant restauration...");
                await Task.Run(() =>
                {
                    foreach (var proc in procs)
                    {
                        try { proc.Kill(entireProcessTree: true); } catch { }
                        finally { proc.Dispose(); }
                    }
                }, ct);

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
                LogInfo(rtb, $"{browser.DisplayName} fermé — démarrage de la restauration.");
            }
            else
            {
                foreach (var p in procs) p.Dispose();
            }

            // ── Copie du profil sauvegardé → destination ────────────────
            await RestoreStep(backupFolder, profileDest,
                $"Profil {browser.DisplayName}", rtb, progress, errorList, ct);

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

        private Task RestoreNetworkDrivesInfoAsync(string restoreRoot, RichTextBox rtb)
        {
            var networkDrivesFile = Path.Combine(restoreRoot, "NetworkDrives.txt");
            if (!File.Exists(networkDrivesFile))
            {
                LogInfo(rtb, "Pas de fichier de lecteurs réseau trouvé.");
                return Task.CompletedTask;
            }

            var entries = NetworkDriveParser.ParseFile(networkDrivesFile);
            if (entries.Count == 0)
            {
                LogInfo(rtb, "Aucun lecteur réseau dans la sauvegarde.");
                return Task.CompletedTask;
            }

            LogTitle(rtb, "Lecteurs réseau de l'ancien poste");

            int wLetter = entries.Max(e => e.Letter.Length);
            int wPath   = Math.Min(60, entries.Max(e => e.UncPath.Length));

            Log(rtb, $"  {"Lettre".PadRight(wLetter + 2)}{"Chemin UNC".PadRight(wPath + 2)}Libellé",
                Color.FromArgb(241, 250, 140));
            Log(rtb, $"  {new string('\u2500', wLetter + 2)}{new string('\u2500', wPath + 2)}{new string('\u2500', 20)}",
                Color.FromArgb(100, 100, 100));

            foreach (var e in entries)
                Log(rtb, $"  {e.Letter.PadRight(wLetter + 2)}{e.UncPath.PadRight(wPath + 2)}{e.Label}",
                    Color.FromArgb(200, 200, 200));

            LogInfo(rtb, "");
            LogInfo(rtb, "Pour recréer ces lecteurs, allez dans l'Explorateur > Ce PC > Connecter un lecteur réseau.");
            return Task.CompletedTask;
        }

        private async Task RestoreWallpaperAsync(string restoreRoot, RichTextBox rtb, CancellationToken ct)
        {
            var candidates = Directory.GetFiles(restoreRoot, "Wallpaper.*")
                .Where(f => !Path.GetFileName(f).Equals("Wallpaper.log", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (candidates.Length == 0)
            {
                LogInfo(rtb, "Aucun fond d'écran sauvegardé.");
                return;
            }

            var wallpaperFile = candidates[0];
            var destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Themes");
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, Path.GetFileName(wallpaperFile));

            await Task.Run(() => File.Copy(wallpaperFile, dest, true), ct);

            await Task.Run(() =>
            {
                try
                {
                    NativeMethods.SystemParametersInfo(
                        NativeMethods.SPI_SETDESKWALLPAPER, 0, dest,
                        NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);
                    LogSuccess(rtb, $"Fond d'écran restauré : {Path.GetFileName(dest)}");
                }
                catch (Exception ex) { LogError(rtb, $"Erreur application fond d'écran : {ex.Message}"); }
            }, ct);
        }

        private async Task RestoreIpDesktopSoftphoneAsync(string restoreRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var ipDir = Path.Combine(restoreRoot, "IpDesktopSoftphone");
            if (!Directory.Exists(ipDir))
            {
                LogInfo(rtb, "IP Desktop Softphone : aucune sauvegarde.");
                return;
            }

            foreach (var vendorDir in Directory.GetDirectories(ipDir))
            {
                ct.ThrowIfCancellationRequested();
                var vendor = Path.GetFileName(vendorDir);
                var dest   = Path.Combine(appDataRoaming, vendor, "IP Desktop Softphone");
                await RestoreStep(vendorDir, dest, $"IP Desktop Softphone ({vendor})",
                    rtb, progress, errorList, ct);
            }
        }
    }
}
