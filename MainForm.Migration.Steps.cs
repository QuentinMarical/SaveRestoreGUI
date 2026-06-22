using SaveRestoreGUI.Services;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Étapes atomiques de migration — équivalent des étapes Backup/Restore mais
    /// pour la copie directe depuis un profil USB vers le profil courant.
    /// Utilisées par BtnStartMigration_Click dans MainForm.Migration.cs.
    /// </summary>
    public partial class MainForm
    {
        // ───────────────────────────────────────────────────────────────────
        //  COPIE GÉNÉRIQUE DE DOSSIER (équivalent de CopyStep / RestoreStep)
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Copie <paramref name="source"/> vers <paramref name="destination"/>.
        /// Les fichiers plus récents écrasent les anciens ; les erreurs sont accumulées
        /// dans <paramref name="errorList"/> sans interrompre la migration.
        /// </summary>
        private async Task MigrateFolderStep(
            string source,
            string destination,
            string name,
            IProgress<int> progress,
            List<string> errorList,
            CancellationToken ct)
        {
            if (!Directory.Exists(source))
            {
                LogInfo(rtbMigrationLog, $"{name} : dossier source introuvable — ignoré.");
                return;
            }

            Log(rtbMigrationLog, $"Migration de {name}...");
            var result = await FileService.CopyFolderAsync(source, destination, progress, null, ct);

            foreach (var err in result.Errors)
            {
                LogError(rtbMigrationLog, $"Erreur copie {err}");
                errorList.Add($"{name} : {err}");
            }

            LogSuccess(rtbMigrationLog,
                $"{name} : {result.Copied} fichier(s) migré(s), {result.Skipped} ignoré(s) — {FileService.FormatSize(result.TotalBytes)}");
        }

        // ───────────────────────────────────────────────────────────────────
        //  STICKY NOTES
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Migre la base Sticky Notes (plum.sqlite) depuis le profil USB.
        /// </summary>
        private async Task MigrateStickyNotesAsync(string sourceProfile, CancellationToken ct)
        {
            var stickySource = Path.Combine(
                sourceProfile,
                "AppData", "Local",
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe",
                "LocalState", "plum.sqlite");

            if (!File.Exists(stickySource))
            {
                LogInfo(rtbMigrationLog, "Sticky Notes : aucune donnée trouvée sur le disque source.");
                return;
            }

            var destDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe", "LocalState");

            Directory.CreateDirectory(destDir);
            var destFile = Path.Combine(destDir, "plum.sqlite");

            await Task.Run(() => File.Copy(stickySource, destFile, overwrite: true), ct);
            LogSuccess(rtbMigrationLog, "Sticky Notes migrés.");
        }

        // ───────────────────────────────────────────────────────────────────
        //  OUTLOOK (PST + autocomplétion + règles + boîtes partagées)
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Migre les données Outlook depuis le profil USB :
        /// fichiers PST, cache d'autocomplétion RoamCache et fichiers de règles .rwz.
        /// </summary>
        private async Task MigrateOutlookAsync(string sourceProfile, CancellationToken ct)
        {
            // ── 1. Fichiers PST ──────────────────────────────────────────
            var pstDirs = new[]
            {
                Path.Combine(sourceProfile, "Documents", "Outlook Files"),
                Path.Combine(sourceProfile, "Documents", "Fichiers Outlook"),
                Path.Combine(sourceProfile, "AppData", "Local", "Microsoft", "Outlook"),
                Path.Combine(sourceProfile, "AppData", "Roaming", "Microsoft", "Outlook"),
            };

            var docsPath   = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var pstDestDir = Directory.Exists(Path.Combine(docsPath, "Fichiers Outlook"))
                ? Path.Combine(docsPath, "Fichiers Outlook")
                : Path.Combine(docsPath, "Outlook Files");
            Directory.CreateDirectory(pstDestDir);

            var pstCopied = 0;
            foreach (var dir in pstDirs.Where(Directory.Exists))
            {
                foreach (var pst in Directory.GetFiles(dir, "*.pst"))
                {
                    ct.ThrowIfCancellationRequested();
                    var dest = Path.Combine(pstDestDir, Path.GetFileName(pst));
                    try
                    {
                        await Task.Run(() => File.Copy(pst, dest, overwrite: true), ct);
                        var size = new FileInfo(dest).Length;
                        LogSuccess(rtbMigrationLog,
                            $"PST migré : {Path.GetFileName(pst)} ({FileService.FormatSize(size)})");
                        pstCopied++;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogError(rtbMigrationLog,
                            $"Erreur PST {Path.GetFileName(pst)} : {ex.Message}");
                    }
                }
            }

            if (pstCopied == 0)
                LogInfo(rtbMigrationLog, "Outlook : aucun fichier PST trouvé.");
            else
            {
                LogInfo(rtbMigrationLog, $"Les fichiers PST ont été copiés dans : {pstDestDir}");
                LogInfo(rtbMigrationLog, "Pour les rattacher dans Outlook :");
                LogInfo(rtbMigrationLog, "  Fichier > Ouvrir et exporter > Ouvrir le fichier de données Outlook");
            }

            // ── 2. Cache d'autocomplétion ────────────────────────────────
            var roamSrc = Path.Combine(
                sourceProfile, "AppData", "Local", "Microsoft", "Outlook", "RoamCache");

            if (Directory.Exists(roamSrc))
            {
                var roamDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Outlook", "RoamCache");
                Directory.CreateDirectory(roamDest);

                var autocomplete = Directory.GetFiles(roamSrc, "Stream_Autocomplete_*.dat");
                foreach (var file in autocomplete)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Run(() =>
                        File.Copy(file, Path.Combine(roamDest, Path.GetFileName(file)), overwrite: true), ct);
                }

                if (autocomplete.Length > 0)
                    LogSuccess(rtbMigrationLog,
                        $"Cache autocomplétion Outlook migré ({autocomplete.Length} fichier(s)).");
            }

            // ── 3. Règles .rwz ───────────────────────────────────────────
            var outlookRoamSrc = Path.Combine(
                sourceProfile, "AppData", "Roaming", "Microsoft", "Outlook");

            if (Directory.Exists(outlookRoamSrc))
            {
                var ruleDest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Outlook");
                Directory.CreateDirectory(ruleDest);

                foreach (var rule in Directory.GetFiles(outlookRoamSrc, "*.rwz"))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        await Task.Run(() =>
                            File.Copy(rule, Path.Combine(ruleDest, Path.GetFileName(rule)), overwrite: true), ct);
                        LogSuccess(rtbMigrationLog, $"Règles Outlook migrées : {Path.GetFileName(rule)}");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogError(rtbMigrationLog,
                            $"Erreur règles {Path.GetFileName(rule)} : {ex.Message}");
                    }
                }
            }
        }

        // ───────────────────────────────────────────────────────────────────
        //  FOND D'ÉCRAN
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Migre le fond d'écran depuis le profil USB.
        /// Cherche d'abord le chemin dans la clé de registre, puis le TranscodedWallpaper.
        /// </summary>
        private async Task MigrateWallpaperAsync(string sourceProfile)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Tentative via clé de registre de l'ancien profil (si accessible)
                    string? wallpaperPath = null;
                    try
                    {
                        using var key = Microsoft.Win32.Registry.CurrentUser
                            .OpenSubKey(@"Control Panel\Desktop");
                        wallpaperPath = key?.GetValue("Wallpaper") as string;
                    }
                    catch { /* registre courant — pas celui du profil source */ }

                    // TranscodedWallpaper dans le profil source
                    var transcoded = Path.Combine(
                        sourceProfile,
                        "AppData", "Roaming", "Microsoft", "Windows", "Themes",
                        "TranscodedWallpaper");

                    string? sourceFile = null;

                    if (!string.IsNullOrEmpty(wallpaperPath) && File.Exists(wallpaperPath))
                        sourceFile = wallpaperPath;
                    else if (File.Exists(transcoded))
                        sourceFile = transcoded;

                    if (sourceFile == null)
                    {
                        LogInfo(rtbMigrationLog, "Fond d'écran : aucun fichier trouvé sur le disque source.");
                        return;
                    }

                    var destDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Microsoft", "Windows", "Themes");
                    Directory.CreateDirectory(destDir);

                    File.Copy(sourceFile, Path.Combine(destDir, "TranscodedWallpaper"), overwrite: true);
                    LogSuccess(rtbMigrationLog, "Fond d'écran migré (visible après reconnexion).");
                }
                catch (Exception ex)
                {
                    LogError(rtbMigrationLog, $"Erreur fond d'écran : {ex.Message}");
                }
            });
        }

        // ───────────────────────────────────────────────────────────────────
        //  LECTEURS RÉSEAU
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lit la liste des lecteurs réseau depuis le registre du profil source
        /// et affiche les chemins UNC dans le log pour recréation manuelle.
        /// (La recréation automatique nécessiterait d'impersonner l'utilisateur source,
        ///  ce qui dépasse le périmètre de la migration locale.)
        /// </summary>
        private async Task MigrateNetworkDrivesAsync(string sourceProfile)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Recherche le fichier NetworkDrives.txt si une sauvegarde préalable existe
                    var networkDrivesFile = Path.Combine(sourceProfile, "..", "NetworkDrives.txt");
                    networkDrivesFile = Path.GetFullPath(networkDrivesFile);

                    if (File.Exists(networkDrivesFile))
                    {
                        var lines = File.ReadAllLines(networkDrivesFile)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToArray();

                        if (lines.Length > 0)
                        {
                            LogInfo(rtbMigrationLog, "Lecteurs réseau de l'ancien profil :");
                            foreach (var line in lines)
                                Log(rtbMigrationLog, $"   {line}");
                            LogWarning(rtbMigrationLog,
                                "Merci de recréer manuellement ces lecteurs réseau sur le nouveau poste.");
                            return;
                        }
                    }

                    // Fallback : WMI sur le poste courant (lecteurs actuellement montés)
                    var mapped = new List<string>();
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT * FROM Win32_MappedLogicalDisk");
                    foreach (var drive in searcher.Get().Cast<System.Management.ManagementObject>())
                    {
                        var letter       = drive["DeviceID"]?.ToString();
                        var providerName = drive["ProviderName"]?.ToString();
                        if (!string.IsNullOrEmpty(letter) && !string.IsNullOrEmpty(providerName))
                            mapped.Add($"{letter} → {providerName}");
                    }

                    if (mapped.Count > 0)
                    {
                        LogInfo(rtbMigrationLog, "Lecteurs réseau actuellement montés (poste courant) :");
                        foreach (var m in mapped)
                            Log(rtbMigrationLog, $"   {m}");
                    }
                    else
                    {
                        LogInfo(rtbMigrationLog, "Aucun lecteur réseau détecté.");
                    }
                }
                catch (Exception ex)
                {
                    LogError(rtbMigrationLog, $"Erreur lecteurs réseau : {ex.Message}");
                }
            });
        }
    }
}
