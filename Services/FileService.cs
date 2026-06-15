namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Service de copie de fichiers avec progression, exclusions et mode fusion.
    /// Cœur commun aux opérations de sauvegarde, restauration et migration.
    /// </summary>
    public static class FileService
    {
        /// <summary>Fichiers parasites exclus de toute copie.</summary>
        private static readonly string[] ExcludedFileNames = { "desktop.ini", "Thumbs.db" };

        public sealed record CopyResult(int Copied, int Skipped, long TotalBytes, List<string> Errors)
        {
            public static CopyResult Empty { get; } = new(0, 0, 0, new List<string>());
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F2} Go";
            if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F2} Mo";
            if (bytes >= 1024) return $"{bytes / 1024.0:F2} Ko";
            return $"{bytes} octets";
        }

        /// <summary>
        /// Énumère les fichiers en ignorant les points de reparse (liens symboliques, jonctions)
        /// et les dossiers inaccessibles.
        /// </summary>
        public static IEnumerable<string> EnumerateFilesSafe(string directory)
        {
            var dirs = new Stack<string>();
            dirs.Push(directory);

            while (dirs.Count > 0)
            {
                var currentDir = dirs.Pop();

                string[] subDirs;
                try { subDirs = Directory.GetDirectories(currentDir); }
                catch (UnauthorizedAccessException) { continue; }
                catch (DirectoryNotFoundException) { continue; }

                foreach (var subDir in subDirs)
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(subDir);
                        if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                            continue;
                        dirs.Push(subDir);
                    }
                    catch { /* dossier inaccessible */ }
                }

                string[] files;
                try { files = Directory.GetFiles(currentDir); }
                catch (UnauthorizedAccessException) { continue; }
                catch (DirectoryNotFoundException) { continue; }

                foreach (var file in files)
                    yield return file;
            }
        }

        private static bool IsExcluded(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return ExcludedFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase)
                   || Path.GetExtension(filePath).Equals(".search-ms", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Copie un dossier avec progression et annulation.
        /// En mode fusion (mergeMode), les fichiers de destination plus récents sont conservés.
        /// Les fichiers identiques (taille + date) sont ignorés.
        /// </summary>
        public static async Task<CopyResult> CopyFolderAsync(
            string source, string destination,
            IProgress<int> progress, IProgress<string>? currentFile,
            CancellationToken ct, bool mergeMode = false)
        {
            var errors = new List<string>();
            if (!Directory.Exists(source))
                return new CopyResult(0, 0, 0, errors);

            var files = EnumerateFilesSafe(source).ToArray();
            if (files.Length == 0)
                return new CopyResult(0, 0, 0, errors);

            int count = 0, copied = 0, skipped = 0;
            long totalSize = 0;

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = file.AsSpan(source.Length).TrimStart(['\\', '/']).ToString();
                var destPath = Path.Combine(destination, relativePath);
                var destDir = Path.GetDirectoryName(destPath);

                if (IsExcluded(file))
                {
                    count++;
                    continue;
                }

                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                try
                {
                    var sourceInfo = new FileInfo(file);

                    if (File.Exists(destPath))
                    {
                        var destInfo = new FileInfo(destPath);

                        // Fichier identique → ignorer
                        if (sourceInfo.Length == destInfo.Length &&
                            Math.Abs((sourceInfo.LastWriteTime - destInfo.LastWriteTime).TotalSeconds) < 2)
                        {
                            skipped++; count++;
                            progress.Report((count * 100) / files.Length);
                            continue;
                        }

                        // Mode fusion → conserver le plus récent
                        if (mergeMode && destInfo.LastWriteTime > sourceInfo.LastWriteTime)
                        {
                            skipped++; count++;
                            progress.Report((count * 100) / files.Length);
                            continue;
                        }
                    }

                    currentFile?.Report(relativePath);
                    await Task.Run(() => File.Copy(file, destPath, true), ct);
                    copied++;
                    totalSize += sourceInfo.Length;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    errors.Add($"{relativePath}: {ex.Message}");
                }

                count++;
                progress.Report((count * 100) / files.Length);
            }

            return new CopyResult(copied, skipped, totalSize, errors);
        }

        /// <summary>Calcule la taille totale d'un dossier (récursif, sans erreur bloquante).</summary>
        public static long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                foreach (var file in EnumerateFilesSafe(path))
                {
                    try { size += new FileInfo(file).Length; }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}
