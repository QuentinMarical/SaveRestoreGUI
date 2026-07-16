using System.Diagnostics;
using System.Text;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Installation d'applications via le gestionnaire de paquets Windows (winget).
    ///
    /// Les installeurs des navigateurs sont « machine-scope » et exigent une
    /// élévation : lancés depuis un process non-élevé, ils échouent silencieusement
    /// tout en laissant winget croire à une réussite. L'installation est donc
    /// exécutée <b>élevée</b> (une seule invite UAC pour tout le lot), et le succès
    /// réel est <b>revérifié par détection</b> côté appelant (registre + exécutable).
    ///
    /// <see cref="BrowserPackages"/> associe une clé de navigateur à son identifiant
    /// winget ; les apps sans paquet (ex. Perplexity Comet) en sont absentes.
    /// </summary>
    public static class WingetService
    {
        /// <summary>Clé de navigateur → identifiant de paquet winget (IDs vérifiés).</summary>
        public static readonly IReadOnlyDictionary<string, string> BrowserPackages =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BrowserEdge"]     = "Microsoft.Edge",
                ["BrowserChrome"]   = "Google.Chrome",
                ["BrowserBrave"]    = "Brave.Brave",
                ["BrowserVivaldi"]  = "Vivaldi.Vivaldi",
                ["BrowserOpera"]    = "Opera.Opera",
                ["BrowserOperaGX"]  = "Opera.OperaGX",
                ["BrowserArc"]      = "TheBrowserCompany.Arc",
                ["BrowserFirefox"]  = "Mozilla.Firefox",
                ["BrowserLibreWolf"]= "LibreWolf.LibreWolf",
                ["BrowserPaleMoon"] = "MoonchildProductions.PaleMoon",
                ["BrowserTor"]      = "TorProject.TorBrowser",
                ["BrowserDDG"]      = "DuckDuckGo.DesktopBrowser",
            };

        /// <summary>Code retourné quand l'utilisateur refuse l'élévation (UAC annulé).</summary>
        public const int ElevationCancelled = 1223;

        private static bool? _available;

        /// <summary>Indique si winget est utilisable sur ce poste (mis en cache).</summary>
        public static bool IsAvailable()
        {
            if (_available.HasValue) return _available.Value;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget",
                    Arguments              = "--version",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                };
                using var proc = Process.Start(psi);
                if (proc == null) { _available = false; return false; }

                if (!proc.WaitForExit(5000))
                {
                    try { proc.Kill(true); } catch { }
                    _available = false;
                    return false;
                }
                _available = proc.ExitCode == 0;
            }
            catch { _available = false; }

            return _available!.Value;
        }

        /// <summary>
        /// Installe tout un lot de paquets en une seule opération élevée (une invite
        /// UAC). Diffuse la sortie winget en direct via <paramref name="log"/>.
        /// Retourne le code de sortie du process (0 = ok, <see cref="ElevationCancelled"/>
        /// = UAC refusé). Le succès réel doit être revérifié par détection.
        /// </summary>
        public static async Task<int> InstallManyElevatedAsync(
            IReadOnlyList<(string Id, string Name)> packages, Action<string> log, CancellationToken ct)
        {
            if (packages.Count == 0) return 0;

            var stamp      = Guid.NewGuid().ToString("N");
            var scriptFile = Path.Combine(Path.GetTempPath(), $"srgui-winget-{stamp}.cmd");
            var outFile    = Path.Combine(Path.GetTempPath(), $"srgui-winget-{stamp}.out");

            await File.WriteAllTextAsync(scriptFile, BuildScript(packages), new UTF8Encoding(false), ct);
            await File.WriteAllTextAsync(outFile, string.Empty, ct);

            try
            {
                var psi = new ProcessStartInfo
                {
                    // La sortie est redirigée vers un fichier par le script lui-même :
                    // impossible de rediriger les flux d'un process élevé (ShellExecute).
                    FileName        = "cmd.exe",
                    Arguments       = $"/c \"\"{scriptFile}\" > \"{outFile}\" 2>&1\"",
                    UseShellExecute = true,
                    Verb            = "runas",
                    WindowStyle     = ProcessWindowStyle.Hidden,
                    CreateNoWindow  = true,
                };

                Process? proc;
                try { proc = Process.Start(psi); }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == ElevationCancelled)
                {
                    return ElevationCancelled;
                }
                if (proc == null) { log("winget : impossible de démarrer l'installation élevée."); return -1; }

                using (proc)
                {
                    long pos = 0;
                    while (!proc.HasExited)
                    {
                        await Task.Delay(1500, ct);
                        pos = EmitNewLines(outFile, pos, log);
                    }
                    EmitNewLines(outFile, pos, log);
                    await proc.WaitForExitAsync(CancellationToken.None);
                    return proc.ExitCode;
                }
            }
            finally
            {
                TryDelete(scriptFile);
                TryDelete(outFile);
            }
        }

        private static string BuildScript(IReadOnlyList<(string Id, string Name)> packages)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 >nul");
            // Résout winget : l'alias PATH fonctionne si l'élévation garde le même
            // utilisateur ; sinon on cible l'exécutable réel dans WindowsApps.
            sb.AppendLine("set \"WG=winget\"");
            sb.AppendLine("where winget >nul 2>&1 && goto :run");
            sb.AppendLine("for /d %%D in (\"%ProgramFiles%\\WindowsApps\\Microsoft.DesktopAppInstaller_*__8wekyb3d8bbwe\") do if exist \"%%D\\winget.exe\" set \"WG=%%D\\winget.exe\"");
            sb.AppendLine(":run");
            foreach (var (id, name) in packages)
            {
                sb.AppendLine($"echo === {name} ({id}) ===");
                sb.AppendLine($"\"%WG%\" install --id {id} --exact --source winget --silent --accept-package-agreements --accept-source-agreements");
            }
            return sb.ToString();
        }

        /// <summary>Lit les nouvelles lignes complètes du fichier de sortie et les journalise (hors barres de progression).</summary>
        private static long EmitNewLines(string path, long fromPos, Action<string> log)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length <= fromPos) return fromPos;

                fs.Seek(fromPos, SeekOrigin.Begin);
                using var sr = new StreamReader(fs, Encoding.UTF8);
                var text = sr.ReadToEnd();

                // Ne traiter que jusqu'au dernier saut de ligne (ligne en cours d'écriture ignorée).
                int lastNl = text.LastIndexOf('\n');
                if (lastNl < 0) return fromPos;

                foreach (var raw in text[..lastNl].Split('\n'))
                {
                    var line = raw.Replace("\r", string.Empty).Trim();
                    if (IsMeaningful(line)) log(line.Length > 200 ? line[..200] : line);
                }
                return fromPos + Encoding.UTF8.GetByteCount(text[..(lastNl + 1)]);
            }
            catch { return fromPos; }
        }

        /// <summary>Vrai si la ligne mérite d'être journalisée (contient du texte, pas une barre de progression).</summary>
        private static bool IsMeaningful(string line)
        {
            if (line.Length == 0) return false;
            // Une ligne utile contient au moins une lettre ; les barres de progression
            // winget ne sont que des blocs/traits/chiffres.
            return line.Any(char.IsLetter);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
