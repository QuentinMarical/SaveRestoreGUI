using System.Diagnostics;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Installation d'applications via le gestionnaire de paquets Windows (winget).
    ///
    /// Ne propose QUE des logiciels réellement disponibles sur la source « winget ».
    /// Le mapping <see cref="BrowserPackages"/> associe une clé de navigateur
    /// (<see cref="BrowserService"/>) à son identifiant de paquet winget ; les apps
    /// sans paquet (ex. Perplexity Comet) en sont volontairement absentes.
    /// </summary>
    public static class WingetService
    {
        /// <summary>
        /// Clé de navigateur → identifiant de paquet winget (IDs vérifiés via
        /// « winget search »). Absents : Perplexity Comet (pas de paquet winget).
        /// </summary>
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

        private static bool? _available;

        /// <summary>
        /// Indique si winget est utilisable sur ce poste (résultat mis en cache).
        /// </summary>
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
        /// Installe un paquet winget en silencieux. Retourne true si l'installation
        /// a réussi (code de sortie 0). Journalise le déroulé via <paramref name="log"/>.
        /// </summary>
        public static async Task<bool> InstallAsync(
            string packageId, string displayName, Action<string> log, CancellationToken ct)
        {
            log($"Installation de {displayName} (winget : {packageId})…");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "winget",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                };
                psi.ArgumentList.Add("install");
                psi.ArgumentList.Add("--id");
                psi.ArgumentList.Add(packageId);
                psi.ArgumentList.Add("--exact");
                psi.ArgumentList.Add("--source");
                psi.ArgumentList.Add("winget");
                psi.ArgumentList.Add("--silent");
                psi.ArgumentList.Add("--accept-package-agreements");
                psi.ArgumentList.Add("--accept-source-agreements");
                psi.ArgumentList.Add("--disable-interactivity");

                using var proc = Process.Start(psi);
                if (proc == null) { log($"{displayName} : winget introuvable, installation ignorée."); return false; }

                var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
                var stderrTask = proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);
                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                if (proc.ExitCode == 0)
                {
                    log($"✓ {displayName} installé avec succès.");
                    return true;
                }

                // Codes fréquents : 0x8A15002B (-1978335189) « aucun paquet applicable / déjà installé ».
                var detail = LastMeaningfulLine(stdout + "\n" + stderr);
                log($"⚠ {displayName} : winget code {proc.ExitCode}." + (detail.Length > 0 ? $" {detail}" : ""));
                return false;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                log($"✗ {displayName} : erreur winget — {ex.Message}");
                return false;
            }
        }

        private static string LastMeaningfulLine(string output)
        {
            var lines = output.Split('\n', '\r');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var t = lines[i].Trim();
                // Ignore les lignes de barre de progression (caractères de spinner/blocs).
                if (t.Length == 0) continue;
                if (t.All(c => "-\\|/█▒░ .".Contains(c))) continue;
                return t.Length > 160 ? t[..160] : t;
            }
            return string.Empty;
        }
    }
}
