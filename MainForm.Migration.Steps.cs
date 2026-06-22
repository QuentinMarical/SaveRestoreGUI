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
        // ═══════════════════════════════════════════════════════════════════
        //  COPIE GÉNÉRIQUE DE DOSSIER (équivalent de CopyStep / RestoreStep)
        // ═══════════════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════════════
        //  STICKY NOTES
        // ═══════════════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════════════
        //  OUTLOOK (PST + autocompltion + règles + boîtes partagées)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Migre les données Outlook depuis le profil USB :
        /// fichiers PST, cache d'autocompltion RoamCache et fichiers de règles .rwz.
        /// </summary>
        private async Task MigrateOutlookAsync(string sourceProfile, CancellationToken ct)
        {
            // ── 1. Fichiers PST ────────────────────────────────────────────────────────────
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

            // ── 2. Cache d'autocompltion ───────────────────────────────────────────────────────────
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
                        $"Cache autocompltion Outlook migré ({autocomplete.Length} fichier(s)).");
            }

            // ── 3. Règles .rwz ────────────────────────────────────────────────────────────────
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

        // ═══════════════════════════════════════════════════════════════════
        //  FOND D'ÉCRAN
        // ═══════════════════════════════════════════════════════════════════

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
                    string? wallpaperPath = null;
                    try
                    {
                        using var key = Microsoft.Win32.Registry.CurrentUser
                            .OpenSubKey(@"Control Panel\Desktop");
                        wallpaperPath = key?.GetValue("Wallpaper") as string;
                    }
                    catch { /* registre courant — pas celui du profil source */ }

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

        // ═══════════════════════════════════════════════════════════════════
        //  LECTEURS RÉSEAU
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lit la liste des lecteurs réseau sauvegardés et affiche les chemins UNC dans le log.
        /// Ordre de recherche du fichier NetworkDrives.txt :
        ///   1. Racine du drive USB (ex. E:\NetworkDrives.txt)
        ///   2. Dossier parent du profil source (ex. E:\Backup\NetworkDrives.txt)
        ///   3. Dossier du profil source lui-même
        /// </summary>
        private async Task MigrateNetworkDrivesAsync(string sourceProfile)
        {
            await Task.Run(() =>
            {
                try
                {
                    // #4 — Cherche NetworkDrives.txt dans cet ordre de priorité :
                    //   1. Racine du drive USB (E:\ par exemple)
                    //   2. Dossier parent du profil (E:\Backup\)
                    //   3. Dossier du profil lui-même
                    var driveRoot     = Path.GetPathRoot(sourceProfile) ?? sourceProfile;
                    var parentFolder  = Path.GetDirectoryName(sourceProfile) ?? sourceProfile;

                    string? networkDrivesFile = null;
                    foreach (var candidate in new[]
                    {
                        Path.Combine(driveRoot,    "NetworkDrives.txt"),
                        Path.Combine(parentFolder, "NetworkDrives.txt"),
                        Path.Combine(sourceProfile,"NetworkDrives.txt"),
                    })
                    {
                        if (File.Exists(candidate))
                        {
                            networkDrivesFile = candidate;
                            LogInfo(rtbMigrationLog, $"NetworkDrives.txt trouvé : {candidate}");
                            break;
                        }
                    }

                    if (networkDrivesFile != null)
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

                    // Fallback : lecteurs actuellement montés via WMI
                    var mapped = new List<string>();
                    using var searcher = new System.Management.ManagementObjectSearcher(
                        "SELECT * FROM Win32_MappedLogicalDisk");
                    foreach (var drive in searcher.Get().Cast<System.Management.ManagementObject>())
                    {
                        var letter       = drive["DeviceID"]?.ToString();
                        var providerName = drive["ProviderName"]?.ToString();
                        if (!string.IsNullOrEmpty(letter) && !string.IsNullOrEmpty(providerName))
                            mapped.Add($"{letter} \u2192 {providerName}");
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

        // ═══════════════════════════════════════════════════════════════════
        //  ONENOTE
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Migre la configuration OneNote depuis le profil USB :
        /// <list type="number">
        ///   <item>Copie le dossier AppData\Roaming\Microsoft\OneNote (cache + options).</item>
        ///   <item>Importe les clés de registre OneNote depuis l'hive ntuser.dat du profil
        ///         source via reg.exe /load + /export (si ntuser.dat accessible).</item>
        /// </list>
        /// </summary>
        private async Task MigrateOneNoteAsync(string sourceProfile)
        {
            await Task.Run(() =>
            {
                try
                {
                    // ── 1. Copie du dossier AppData\Roaming\Microsoft\OneNote ───────────
                    var oneNoteSrc = Path.Combine(
                        sourceProfile, "AppData", "Roaming", "Microsoft", "OneNote");
                    var oneNoteDest = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Microsoft", "OneNote");

                    if (Directory.Exists(oneNoteSrc))
                    {
                        CopyDirectoryRecursive(oneNoteSrc, oneNoteDest);
                        LogSuccess(rtbMigrationLog, "OneNote : dossier de configuration migré.");
                    }
                    else
                    {
                        LogInfo(rtbMigrationLog, "OneNote : aucun dossier de configuration trouvé dans le profil source.");
                    }

                    // ── 2. Import des clés de registre depuis ntuser.dat ──────────────
                    var ntUserDat = Path.Combine(sourceProfile, "NTUSER.DAT");
                    if (!File.Exists(ntUserDat))
                    {
                        LogInfo(rtbMigrationLog, "OneNote : ntuser.dat inaccessible — import registre ignoré.");
                        return;
                    }

                    var tempHive    = $"HKLM\\SAVERESTORE_MIGRATE_{Guid.NewGuid():N}";
                    var tempRegFile = Path.Combine(Path.GetTempPath(), $"onenote_migrate_{Guid.NewGuid():N}.reg");

                    try
                    {
                        // Charger le hive temporaire
                        RunRegExe($"load \"{tempHive}\" \"{ntUserDat}\"");

                        // Exporter la branche OneNote
                        var oneNoteHiveKey = $"{tempHive}\\Software\\Microsoft\\Office";
                        RunRegExe($"export \"{oneNoteHiveKey}\" \"{tempRegFile}\" /y");

                        if (File.Exists(tempRegFile))
                        {
                            // Filtrer pour ne garder que les clés OneNote, remplacer le chemin du hive
                            var regContent = File.ReadAllText(tempRegFile);
                            regContent = regContent.Replace(
                                tempHive.Replace("\\", "\\\\"),
                                "HKEY_CURRENT_USER",
                                StringComparison.OrdinalIgnoreCase);

                            var filteredLines = regContent
                                .Split('\n')
                                .Where(l => l.Contains("OneNote", StringComparison.OrdinalIgnoreCase)
                                         || l.StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase)
                                         || string.IsNullOrWhiteSpace(l)
                                         || l.StartsWith('['))
                                .ToArray();

                            var filteredPath = Path.Combine(Path.GetTempPath(), $"onenote_filtered_{Guid.NewGuid():N}.reg");
                            File.WriteAllText(filteredPath, string.Join("\n", filteredLines));

                            RunRegExe($"import \"{filteredPath}\"");
                            File.Delete(filteredPath);

                            LogSuccess(rtbMigrationLog, "OneNote : clés de registre importées.");
                        }
                        else
                        {
                            LogInfo(rtbMigrationLog, "OneNote : aucune clé de registre Office trouvée dans le hive.");
                        }
                    }
                    finally
                    {
                        // Décharger le hive temporaire (toujours, même en cas d'erreur)
                        RunRegExe($"unload \"{tempHive}\"");
                        if (File.Exists(tempRegFile)) File.Delete(tempRegFile);
                    }
                }
                catch (Exception ex)
                {
                    LogError(rtbMigrationLog, $"OneNote : erreur migration — {ex.Message}");
                }
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  IP DESKTOP SOFTPHONE (Alcatel-Lucent)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Migre la configuration IP Desktop Softphone (Alcatel-Lucent) depuis le profil USB.
        /// Copie les dossiers AppData\Roaming et AppData\Local d'Alcatel-Lucent.
        /// </summary>
        private async Task MigrateIpDesktopSoftphoneAsync(
            string sourceProfile,
            IProgress<int> progress,
            List<string> errorList,
            CancellationToken ct)
        {
            var sourceDirs = new[]
            {
                (
                    Path.Combine(sourceProfile, "AppData", "Roaming", "Alcatel-Lucent", "IP Desktop Softphone"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alcatel-Lucent", "IP Desktop Softphone")
                ),
                (
                    Path.Combine(sourceProfile, "AppData", "Local", "Alcatel-Lucent", "IP Desktop Softphone"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Alcatel-Lucent", "IP Desktop Softphone")
                )
            };

            bool anyFound = false;

            foreach (var (src, dest) in sourceDirs)
            {
                if (!Directory.Exists(src)) continue;
                anyFound = true;

                Log(rtbMigrationLog, $"IP Desktop Softphone : migration de {Path.GetFileName(Path.GetDirectoryName(src) ?? src)}...");

                var result = await FileService.CopyFolderAsync(src, dest, progress, null, ct);

                foreach (var err in result.Errors)
                {
                    LogError(rtbMigrationLog, $"Erreur copie IP Softphone : {err}");
                    errorList.Add($"IP Desktop Softphone : {err}");
                }

                LogSuccess(rtbMigrationLog,
                    $"IP Desktop Softphone ({(src.Contains("Roaming") ? "Roaming" : "Local")}) : " +
                    $"{result.Copied} fichier(s) migré(s) — {FileService.FormatSize(result.TotalBytes)}");
            }

            if (!anyFound)
                LogInfo(rtbMigrationLog,
                    "IP Desktop Softphone : aucun dossier de configuration trouvé dans le profil source.");
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HELPERS PRIVÉS (utilisés par plusieurs étapes)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Copie récursive d'un dossier source vers destination.
        /// Utilisé par les étapes qui n'ont pas besoin du rapport de progression granulaire.
        /// </summary>
        private static void CopyDirectoryRecursive(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                var dest = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
            }
            foreach (var dir in Directory.GetDirectories(source))
            {
                CopyDirectoryRecursive(
                    dir,
                    Path.Combine(destination, Path.GetFileName(dir)));
            }
        }

        /// <summary>
        /// Lance reg.exe avec les arguments spécifiés et attend la fin du processus.
        /// Nécessite des droits administrateur pour reg load/unload.
        /// </summary>
        private static void RunRegExe(string arguments)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "reg.exe",
                Arguments              = arguments,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                Verb                   = "runas"
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            proc?.WaitForExit();
        }
    }
}
