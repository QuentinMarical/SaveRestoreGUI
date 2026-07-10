using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Sauvegarde du thème Windows actif au format .deskthemepack (archive CAB
    /// construite via makecab : fichier .theme + fonds d'écran embarqués) et
    /// restauration par simple application du pack (double-clic programmatique).
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static class ThemeService
    {
        private const string ThemesRegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes";

        // ─────────────────────────── Sauvegarde ───────────────────────────

        public static void Backup(string backupRoot, Action<string> info, Action<string> success, Action<string> warn)
        {
            // 1. Localiser le thème actif
            string? themePath;
            using (var key = Registry.CurrentUser.OpenSubKey(ThemesRegKey))
                themePath = key?.GetValue("CurrentTheme") as string;

            if (string.IsNullOrWhiteSpace(themePath) || !File.Exists(themePath))
            {
                warn("Thème actif introuvable dans le registre.");
                return;
            }
            info($"Thème actif : {Path.GetFileName(themePath)}");

            string themeName = SanitizeName(Path.GetFileNameWithoutExtension(themePath));
            if (string.IsNullOrEmpty(themeName) || themeName.Equals("Custom", StringComparison.OrdinalIgnoreCase))
                themeName = "ThemeSauvegarde";

            // 2. Dossier de travail : .theme réécrit + images
            var workDir = Path.Combine(Path.GetTempPath(), "SRG-Theme-" + Guid.NewGuid().ToString("N"));
            var bgDir   = Path.Combine(workDir, "DesktopBackground");
            Directory.CreateDirectory(bgDir);

            try
            {
                var lines = File.ReadAllLines(themePath).ToList();
                var packedFiles = new List<(string Abs, string Rel)>();
                bool inSlideshow = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i].TrimStart();
                    if (line.StartsWith('[')) inSlideshow = line.StartsWith("[Slideshow]", StringComparison.OrdinalIgnoreCase);

                    // Fond d'écran statique : réécrit vers un chemin relatif au .theme
                    if (line.StartsWith("Wallpaper=", StringComparison.OrdinalIgnoreCase))
                    {
                        var src = Environment.ExpandEnvironmentVariables(line["Wallpaper=".Length..].Trim());
                        if (File.Exists(src))
                        {
                            var rel = Path.Combine("DesktopBackground", Path.GetFileName(src));
                            packedFiles.Add((src, rel));
                            lines[i] = "Wallpaper=" + rel;
                        }
                    }

                    // Diaporama : embarque les images du dossier source
                    if (inSlideshow && line.StartsWith("ImagesRootPath=", StringComparison.OrdinalIgnoreCase))
                    {
                        var srcDir = Environment.ExpandEnvironmentVariables(line["ImagesRootPath=".Length..].Trim());
                        if (Directory.Exists(srcDir))
                        {
                            foreach (var img in Directory.EnumerateFiles(srcDir)
                                         .Where(f => IsImage(f)).Take(50))
                                packedFiles.Add((img, Path.Combine("DesktopBackground", Path.GetFileName(img))));
                            lines[i] = "ImagesRootPath=DesktopBackground";
                        }
                    }
                }

                // Copier les ressources dans le dossier de travail
                foreach (var (abs, rel) in packedFiles.DistinctBy(p => p.Rel))
                    File.Copy(abs, Path.Combine(workDir, rel), overwrite: true);

                var packedTheme = Path.Combine(workDir, themeName + ".theme");
                File.WriteAllLines(packedTheme, lines, Encoding.Unicode);

                // 3. Empaqueter en .deskthemepack via makecab
                var themeOutDir = Path.Combine(backupRoot, "Theme");
                Directory.CreateDirectory(themeOutDir);
                var packName = themeName + ".deskthemepack";

                var ddf = new StringBuilder();
                ddf.AppendLine(".OPTION EXPLICIT");
                ddf.AppendLine($".Set CabinetNameTemplate={packName}");
                ddf.AppendLine($".Set DiskDirectoryTemplate={themeOutDir}");
                ddf.AppendLine(".Set CompressionType=LZX");
                ddf.AppendLine(".Set Cabinet=on");
                ddf.AppendLine(".Set Compress=on");
                ddf.AppendLine(".Set MaxDiskSize=0");
                ddf.AppendLine(".Set RptFileName=nul");
                ddf.AppendLine(".Set InfFileName=nul");
                ddf.AppendLine(".Set DestinationDir=");
                ddf.AppendLine($"\"{packedTheme}\" \"{themeName}.theme\"");
                foreach (var (_, rel) in packedFiles.DistinctBy(p => p.Rel))
                {
                    ddf.AppendLine(".Set DestinationDir=DesktopBackground");
                    ddf.AppendLine($"\"{Path.Combine(workDir, rel)}\" \"{Path.GetFileName(rel)}\"");
                }

                var ddfPath = Path.Combine(workDir, "pack.ddf");
                File.WriteAllText(ddfPath, ddf.ToString(), Encoding.Default);

                var psi = new ProcessStartInfo("makecab.exe", $"/F \"{ddfPath}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workDir
                };
                using var proc = Process.Start(psi)!;
                // Vider les flux AVANT d'attendre : makecab remplit le tampon stdout
                // et se bloque sinon (deadlock de pipe plein).
                proc.StandardOutput.ReadToEnd();
                proc.StandardError.ReadToEnd();
                proc.WaitForExit(60_000);

                var packPath = Path.Combine(themeOutDir, packName);
                if (proc.ExitCode == 0 && File.Exists(packPath))
                {
                    success($"Thème empaqueté : {packName} " +
                            $"({FileService.FormatSize(new FileInfo(packPath).Length)}, {packedFiles.Count} image(s))");
                }
                else
                {
                    warn($"makecab a échoué (code {proc.ExitCode}) — copie brute du .theme à la place.");
                    File.Copy(themePath, Path.Combine(themeOutDir, Path.GetFileName(themePath)), true);
                }
            }
            finally
            {
                try { Directory.Delete(workDir, recursive: true); } catch { }
            }
        }

        // ─────────────────────────── Restauration ───────────────────────────

        public static void Restore(string restoreRoot, Action<string> info, Action<string> success, Action<string> warn)
        {
            var themeDir = Path.Combine(restoreRoot, "Theme");
            if (!Directory.Exists(themeDir))
            {
                warn("Aucun dossier Theme dans la sauvegarde.");
                return;
            }

            var pack = Directory.GetFiles(themeDir, "*.deskthemepack").FirstOrDefault()
                    ?? Directory.GetFiles(themeDir, "*.themepack").FirstOrDefault()
                    ?? Directory.GetFiles(themeDir, "*.theme").FirstOrDefault();

            if (pack == null)
            {
                warn("Aucun pack de thème (.deskthemepack) trouvé dans la sauvegarde.");
                return;
            }

            info($"Application du thème : {Path.GetFileName(pack)}");
            // L'ouverture du pack l'installe dans %LocalAppData%\Microsoft\Windows\Themes
            // et l'applique immédiatement (la page Personnalisation s'ouvre brièvement).
            Process.Start(new ProcessStartInfo(pack) { UseShellExecute = true });
            success("Thème appliqué — la fenêtre Personnalisation peut s'ouvrir, elle peut être fermée.");
        }

        // ─────────────────────────── Helpers ───────────────────────────

        private static bool IsImage(string path) =>
            Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".jfif" or ".webp";

        private static string SanitizeName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}
