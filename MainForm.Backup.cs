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

                // ── Détection double profil NOM.DOMAINE + NOM ──────────────────────
                var currentUsername = Environment.UserName;
                string? domainProfilePath = null;
                string? cleanProfilePath  = null;

                if (chkOldProfile.Checked)
                {
                    var usersDir = Path.GetDirectoryName(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                    if (usersDir != null)
                    {
                        var excluded = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };

                        // NOM.DOMAINE : commence par NOM. mais n'est pas NOM exact
                        domainProfilePath = Directory.GetDirectories(usersDir)
                            .FirstOrDefault(d =>
                            {
                                var name = Path.GetFileName(d);
                                return name != null
                                    && name.StartsWith(currentUsername + ".", StringComparison.OrdinalIgnoreCase)
                                    && !excluded.Contains(name, StringComparer.OrdinalIgnoreCase);
                            });

                        // NOM : correspondance exacte
                        var exactDir = Path.Combine(usersDir, currentUsername);
                        if (Directory.Exists(exactDir))
                            cleanProfilePath = exactDir;
                    }
                }

                bool doubleBackup = domainProfilePath != null && cleanProfilePath != null;

                if (doubleBackup)
                {
                    LogInfo(rtbBackupLog,
                        $"⚠️ Double profil détecté : « {Path.GetFileName(domainProfilePath)} » + « {currentUsername} »");
                    LogInfo(rtbBackupLog,
                        "La sauvegarde copiera d'abord l'ancien profil domaine, puis le profil actuel (priorité au plus récent).");
                }

                var progress = new Progress<int>(UpdateProgress);

                // ── Passe 1 : NOM.DOMAINE (si double profil) ──────────────────────
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

                // ── Passe 2 (ou unique) : profil actuel ───────────────────────────
                if (doubleBackup)
                    LogTitle(rtbBackupLog, $"Passe 2 — Profil actuel : {currentUsername}");

                var steps = BuildBackupSteps(backupRoot, cleanProfilePath ?? string.Empty,
                    progress, errorList, ct,
                    includePublic: true, includeAppData: true);

                int totalSteps = steps.Count;
                int currentStep = 0;

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

        // ───────────────────────────────────────────────────────────────────────────────
        //  Construction des étapes de sauvegarde
        // ───────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Construit la liste des étapes de sauvegarde pour un profil source donné.
        /// <para>
        /// Quand <paramref name="sourceProfileOverride"/> est non-vide, les dossiers
        /// utilisateurs sont lus depuis ce chemin (profil NOM.DOMAINE) au lieu du profil
        /// Windows courant. Dans ce cas on copie uniquement les dossiers personnels ;
        /// les données AppData (Outlook, Signatures…) ne sont incluses que pour le
        /// profil actuel (<paramref name="includeAppData"/> = true).
        /// </para>
        /// </summary>
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

            // Résolution des sources :
            // - Si sourceProfileOverride est défini → on lit depuis ce profil externe.
            // - Sinon → on utilise les chemins Windows standard (profil courant).
            bool useOverride = !string.IsNullOrEmpty(sourceProfileOverride)
                               && Directory.Exists(sourceProfileOverride);

            string Src(string relativePath) =>
                useOverride
                    ? Path.Combine(sourceProfileOverride, relativePath)
                    : relativePath;   // relativePath est déjà un chemin complet

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (chkDocuments.Checked) steps.Add(("Documents", () => CopyStep(
                useOverride ? Src("Documents") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Path.Combine(backupRoot, "Documents"), "Documents", rtbBackupLog, progress, errorList, ct)));

            if (chkDesktop.Checked) steps.Add(("Bureau", () => CopyStep(
                useOverride ? Src("Desktop") : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Path.Combine(backupRoot, "Desktop"), "Bureau", rtbBackupLog, progress, errorList, ct)));

            if (chkDownloads.Checked) steps.Add(("Téléchargements", () => CopyStep(
                useOverride ? Src("Downloads") : Path.Combine(userProfile, "Downloads"),
                Path.Combine(backupRoot, "Downloads"), "Téléchargements", rtbBackupLog, progress, errorList, ct)));

            if (chkPictures.Checked) steps.Add(("Images", () => CopyStep(
                useOverride ? Src("Pictures") : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Path.Combine(backupRoot, "Pictures"), "Images", rtbBackupLog, progress, errorList, ct)));

            if (chkMusic.Checked) steps.Add(("Musique", () => CopyStep(
                useOverride ? Src("Music") : Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Path.Combine(backupRoot, "Music"), "Musique", rtbBackupLog, progress, errorList, ct)));

            if (chkVideos.Checked) steps.Add(("Vidéos", () => CopyStep(
                useOverride ? Src("Videos") : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Path.Combine(backupRoot, "Videos"), "Vidéos", rtbBackupLog, progress, errorList, ct)));

            // AppData-dependent items — uniquement pour le profil actuel (ou si pas d'override)
            if (includeAppData)
            {
                var appDataRoaming = useOverride
                    ? Path.Combine(sourceProfileOverride, "AppData", "Roaming")
                    : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (chkSignatures.Checked) steps.Add(("Signatures Outlook", () => BackupSignaturesAsync(
                    backupRoot, rtbBackupLog, progress, errorList, ct)));

                if (chkTemplates.Checked) steps.Add(("Modèles Office", () => CopyStep(
                    Path.Combine(appDataRoaming, "Microsoft", "Templates"),
                    Path.Combine(backupRoot, "Templates"), "Modèles Office", rtbBackupLog, progress, errorList, ct)));

                if (chkExcelMacros.Checked) steps.Add(("Macros Excel (XLSTART)", () => CopyStep(
                    Path.Combine(appDataRoaming, "Microsoft", "Excel", "XLSTART"),
                    Path.Combine(backupRoot, "Excel", "XLSTART"), "Macros Excel (XLSTART)", rtbBackupLog, progress, errorList, ct)));

                if (chkSap.Checked) steps.Add(("SAP GUI", () => CopyStep(
                    Path.Combine(appDataRoaming, "SAP"),
                    Path.Combine(backupRoot, "SAP"), "SAP GUI", rtbBackupLog, progress, errorList, ct)));

                if (chkOutlook.Checked) steps.Add(("Données Outlook", () => BackupOutlookDataAsync(backupRoot, rtbBackupLog, ct)));
                if (chkOneNote.Checked) steps.Add(("OneNote (registre)", () => BackupOneNoteAsync(backupRoot, rtbBackupLog)));
                if (chkStickyNotes.Checked) steps.Add(("Sticky Notes", () => BackupStickyNotesAsync(backupRoot, rtbBackupLog, ct)));
                if (chkEdgeProfile.Checked) steps.Add(("Profil Edge", () => BackupEdgeProfileAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
                if (chkWallpaper.Checked) steps.Add(("Fond d'écran", () => BackupWallpaperAsync(backupRoot, rtbBackupLog)));
                if (chkNetworkDrives.Checked) steps.Add(("Lecteurs réseau", () => BackupNetworkDrivesAsync(backupRoot, rtbBackupLog)));
                if (chkIpDesktopSoftphone.Checked) steps.Add(("IP Desktop Softphone", () => BackupIpDesktopSoftphoneAsync(backupRoot, rtbBackupLog, progress, errorList, ct)));
            }

            if (includePublic && chkPublic.Checked) steps.Add(("Dossier Public", () => CopyStep(
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

        /// <summary>
        /// Sauvegarde le profil Edge.
        /// Si msedge.exe tourne au moment de la sauvegarde, les processus sont tués
        /// (arbre complet) afin de libérer les fichiers du profil, la copie est réalisée,
        /// puis Edge est relancé uniquement s'il était ouvert avant.
        /// </summary>
        private async Task BackupEdgeProfileAsync(string backupRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var edgeDefault = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default");

            // ── 1. Détection Edge ────────────────────────────────────────────────────
            var edgeProcesses = System.Diagnostics.Process.GetProcessesByName("msedge");
            bool edgeWasRunning = edgeProcesses.Length > 0;

            if (edgeWasRunning)
            {
                LogInfo(rtb, $"Edge détecté ({edgeProcesses.Length} processus) — fermeture avant copie...");

                // ── 2. Kill de l'arbre complet ───────────────────────────────────────
                await Task.Run(() =>
                {
                    foreach (var proc in edgeProcesses)
                    {
                        try { proc.Kill(entireProcessTree: true); }
                        catch { /* déjà terminé */ }
                        finally { proc.Dispose(); }
                    }
                }, ct);

                // ── 3. Attente libération des fichiers (max 5 s) ────────────────────
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(300, ct);
                    if (System.Diagnostics.Process.GetProcessesByName("msedge").Length == 0)
                        break;
                }

                LogInfo(rtb, "Edge fermé — démarrage de la copie du profil.");
            }

            // ── 4. Copie du profil ───────────────────────────────────────────────────
            await CopyStep(edgeDefault,
                Path.Combine(backupRoot, "EdgeProfile"),
                "Profil Edge",
                rtb, progress, errorList, ct);

            // ── 5. Relance Edge si besoin ────────────────────────────────────────────
            if (edgeWasRunning)
            {
                await Task.Run(() =>
                {
                    var candidates = new[]
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                            "Microsoft", "Edge", "Application", "msedge.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            "Microsoft", "Edge", "Application", "msedge.exe")
                    };

                    var edgeExe = candidates.FirstOrDefault(File.Exists);
                    if (edgeExe != null)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName        = edgeExe,
                            UseShellExecute = true
                        });
                        LogInfo(rtb, "Edge relancé.");
                    }
                    else
                    {
                        LogWarning(rtb, "Impossible de relancer Edge : exécutable introuvable.");
                    }
                }, CancellationToken.None);
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

        /// <summary>
        /// Sauvegarde la configuration IP Desktop Softphone (Alcatel-Lucent / ALE International).
        /// Recherche dans AppData\Roaming et AppData\Local pour les deux noms d'éditeur connus.
        /// </summary>
        private async Task BackupIpDesktopSoftphoneAsync(string backupRoot, RichTextBox rtb,
            IProgress<int> progress, List<string> errorList, CancellationToken ct)
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Alcatel-Lucent", "IP Desktop Softphone"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Alcatel-Lucent", "IP Desktop Softphone"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ALE International", "IP Desktop Softphone"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ALE International", "IP Desktop Softphone"),
            };

            bool found = false;
            foreach (var src in candidates.Where(Directory.Exists))
            {
                var vendorName = Path.GetFileName(Path.GetDirectoryName(src)!);
                var destDir = Path.Combine(backupRoot, "IpDesktopSoftphone", vendorName);
                await CopyStep(src, destDir, $"IP Desktop Softphone ({vendorName})", rtb, progress, errorList, ct);
                found = true;
            }

            if (!found)
                LogInfo(rtb, "IP Desktop Softphone : aucune configuration trouvée.");
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
