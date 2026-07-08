using System.Diagnostics;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Détection d'installation et lancement des applications post-restauration.
    /// Pour les navigateurs, toute la logique de détection est déléguée à
    /// <see cref="BrowserService"/> — AppLauncherService n'est plus la source
    /// de vérité pour les chemins/registre navigateurs.
    /// </summary>
    public static class AppLauncherService
    {
        // ════════════════════════════════════════════════════════════════
        //  Navigateurs — délégation à BrowserService
        // ════════════════════════════════════════════════════════════════

        /// <summary>Retourne true si le navigateur identifié par <paramref name="key"/> est installé.</summary>
        public static bool IsBrowserInstalled(string key)
        {
            var def = BrowserService.All.FirstOrDefault(b => b.Key == key);
            return def != null && BrowserService.IsInstalled(def);
        }

        /// <summary>Retourne le chemin de profil du navigateur, ou chaîne vide si introuvable.</summary>
        public static string GetBrowserProfilePath(string key)
        {
            var def = BrowserService.All.FirstOrDefault(b => b.Key == key);
            if (def == null) return string.Empty;
            try { return def.ProfilePathFactory() ?? string.Empty; }
            catch { return string.Empty; }
        }

        /// <summary>Retourne le premier exécutable trouvé pour le navigateur, ou null.</summary>
        public static string? FindBrowserExe(string key)
        {
            var def = BrowserService.All.FirstOrDefault(b => b.Key == key);
            return def?.ExecutableCandidates.FirstOrDefault(File.Exists);
        }

        // ── Raccourcis nominatifs (rétro-compat) ────────────────────────
        public static bool IsEdgeInstalled()           => IsBrowserInstalled("BrowserEdge");
        public static bool IsChromeInstalled()         => IsBrowserInstalled("BrowserChrome");
        public static bool IsFirefoxInstalled()        => IsBrowserInstalled("BrowserFirefox");
        public static bool IsBraveInstalled()          => IsBrowserInstalled("BrowserBrave");
        public static bool IsOperaInstalled()          => IsBrowserInstalled("BrowserOpera");
        public static bool IsOperaGxInstalled()        => IsBrowserInstalled("BrowserOperaGX");
        public static bool IsVivaldiInstalled()        => IsBrowserInstalled("BrowserVivaldi");
        public static bool IsArcInstalled()            => IsBrowserInstalled("BrowserArc");
        public static bool IsCometInstalled()          => IsBrowserInstalled("BrowserComet");
        public static bool IsLibreWolfInstalled()      => IsBrowserInstalled("BrowserLibreWolf");
        public static bool IsPaleMoonInstalled()       => IsBrowserInstalled("BrowserPaleMoon");
        public static bool IsTorBrowserInstalled()     => IsBrowserInstalled("BrowserTor");
        public static bool IsDuckDuckGoBrowserInstalled() => IsBrowserInstalled("BrowserDDG");

        // ── Chemins de profil (rétro-compat) ───────────────────────────
        public static string GetEdgeProfilePath()       => GetBrowserProfilePath("BrowserEdge");
        public static string GetChromeProfilePath()     => GetBrowserProfilePath("BrowserChrome");
        public static string GetFirefoxProfilesPath()   => GetBrowserProfilePath("BrowserFirefox");
        public static string GetBraveProfilePath()      => GetBrowserProfilePath("BrowserBrave");
        public static string GetOperaProfilePath()      => GetBrowserProfilePath("BrowserOpera");
        public static string GetOperaGxProfilePath()    => GetBrowserProfilePath("BrowserOperaGX");
        public static string GetVivaldiProfilePath()    => GetBrowserProfilePath("BrowserVivaldi");
        public static string GetArcProfilePath()        => GetBrowserProfilePath("BrowserArc");
        public static string GetCometProfilePath()      => GetBrowserProfilePath("BrowserComet");
        public static string GetLibreWolfProfilesPath() => GetBrowserProfilePath("BrowserLibreWolf");
        public static string GetPaleMoonProfilesPath()  => GetBrowserProfilePath("BrowserPaleMoon");
        public static string GetTorBrowserProfilePath() => GetBrowserProfilePath("BrowserTor");
        public static string GetDuckDuckGoProfilePath() => GetBrowserProfilePath("BrowserDDG");

        // ── Exécutables (rétro-compat) ──────────────────────────────────
        public static string? FindEdgeExe()         => FindBrowserExe("BrowserEdge");
        public static string? FindChromeExe()       => FindBrowserExe("BrowserChrome");
        public static string? FindFirefoxExe()      => FindBrowserExe("BrowserFirefox");
        public static string? FindBraveExe()        => FindBrowserExe("BrowserBrave");
        public static string? FindOperaExe()        => FindBrowserExe("BrowserOpera");
        public static string? FindOperaGxExe()      => FindBrowserExe("BrowserOperaGX");
        public static string? FindVivaldiExe()      => FindBrowserExe("BrowserVivaldi");
        public static string? FindArcExe()          => FindBrowserExe("BrowserArc");
        public static string? FindCometExe()        => FindBrowserExe("BrowserComet");
        public static string? FindLibreWolfExe()    => FindBrowserExe("BrowserLibreWolf");
        public static string? FindPaleMoonExe()     => FindBrowserExe("BrowserPaleMoon");
        public static string? FindTorBrowserExe()   => FindBrowserExe("BrowserTor");
        public static string? FindDuckDuckGoExe()   => FindBrowserExe("BrowserDDG");

        // ════════════════════════════════════════════════════════════════
        //  OneDrive (non couvert par BrowserService)
        // ════════════════════════════════════════════════════════════════

        public static bool IsOneDriveInstalled()
        {
            var exe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive", "OneDrive.exe");
            return File.Exists(exe) || IsInstalledViaRegistry("OneDrive");
        }

        // ════════════════════════════════════════════════════════════════
        //  Factory — BrowserEntry pour BrowserPickerButton
        // ════════════════════════════════════════════════════════════════

        private static readonly Dictionary<string, string> _browserIcons = new()
        {
            { "BrowserEdge",     "🌐" },
            { "BrowserChrome",   "🔵" },
            { "BrowserBrave",    "🦁" },
            { "BrowserVivaldi",  "🎼" },
            { "BrowserOpera",    "🎭" },
            { "BrowserOperaGX",  "🎮" },
            { "BrowserArc",      "🌈" },
            { "BrowserComet",    "🪐" },
            { "BrowserFirefox",  "🦊" },
            { "BrowserLibreWolf","🐺" },
            { "BrowserPaleMoon", "🌙" },
            { "BrowserTor",      "🔒" },
            { "BrowserDDG",      "🦆" },
        };

        /// <summary>
        /// Retourne la liste des navigateurs supportés pour <see cref="BrowserPickerButton.SetBrowsers"/>.
        /// Utilise <see cref="BrowserService.All"/> comme source unique.
        /// </summary>
        public static IReadOnlyList<BrowserEntry> GetBrowserEntries()
            => BrowserService.All
                .Select(b =>
                {
                    var icon    = _browserIcons.TryGetValue(b.Key, out var ic) ? ic : "🌐";
                    var profile = GetBrowserProfilePath(b.Key);
                    var exe     = FindBrowserExe(b.Key);
                    var avail   = BrowserService.IsInstalled(b) || BrowserService.HasProfileData(b);
                    return new BrowserEntry(b.DisplayName, icon, avail, profile, exe);
                })
                .ToList()
                .AsReadOnly();

        // ════════════════════════════════════════════════════════════════
        //  Lancement des applications post-restauration
        // ════════════════════════════════════════════════════════════════

        public static void LaunchApplications(Action<string> log)
        {
            var apps = new[]
            {
                ("Teams",   @"AppData\Local\Microsoft\Teams\current\Teams.exe", true),
                ("Outlook", FindOfficeRootOrEmpty(), false)
            };

            foreach (var (name, relOrAbsPath, isRelative) in apps)
            {
                try
                {
                    var fullPath = isRelative
                        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), relOrAbsPath)
                        : relOrAbsPath;

                    if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                    { log($"{name} : exécutable introuvable, lancement ignoré."); continue; }

                    Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
                    log($"{name} lancé.");
                }
                catch (Exception ex) { log($"{name} : erreur de lancement — {ex.Message}"); }
            }
        }

        /// <summary>Lance un navigateur par sa clé BrowserService (ex. "BrowserEdge").</summary>
        public static void LaunchBrowser(string key, Action<string> log)
        {
            var def = BrowserService.All.FirstOrDefault(b => b.Key == key);
            if (def == null) { log($"Navigateur '{key}' inconnu."); return; }
            LaunchExe(def.DisplayName, FindBrowserExe(key), log);
        }

        // ── Raccourcis nominatifs (rétro-compat) ────────────────────────
        public static void LaunchEdge(Action<string> log)       => LaunchBrowser("BrowserEdge",     log);
        public static void LaunchChrome(Action<string> log)     => LaunchBrowser("BrowserChrome",   log);
        public static void LaunchFirefox(Action<string> log)    => LaunchBrowser("BrowserFirefox",  log);
        public static void LaunchBrave(Action<string> log)      => LaunchBrowser("BrowserBrave",    log);
        public static void LaunchOpera(Action<string> log)      => LaunchBrowser("BrowserOpera",    log);
        public static void LaunchOperaGx(Action<string> log)    => LaunchBrowser("BrowserOperaGX",  log);
        public static void LaunchVivaldi(Action<string> log)    => LaunchBrowser("BrowserVivaldi",  log);
        public static void LaunchArc(Action<string> log)        => LaunchBrowser("BrowserArc",      log);
        public static void LaunchComet(Action<string> log)      => LaunchBrowser("BrowserComet",    log);
        public static void LaunchLibreWolf(Action<string> log)  => LaunchBrowser("BrowserLibreWolf",log);
        public static void LaunchPaleMoon(Action<string> log)   => LaunchBrowser("BrowserPaleMoon", log);
        public static void LaunchTorBrowser(Action<string> log) => LaunchBrowser("BrowserTor",      log);
        public static void LaunchDuckDuckGo(Action<string> log) => LaunchBrowser("BrowserDDG",      log);

        public static void OpenOneDriveBackupSettings(Action<string> log)
        {
            var oneDriveExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive", "OneDrive.exe");

            if (!File.Exists(oneDriveExe)) { log("OneDrive : introuvable, étape ignorée."); return; }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = oneDriveExe, Arguments = "/backuppc", UseShellExecute = true
                });
                log("OneDrive — fenêtre 'Gérer la sauvegarde' ouverte.");
            }
            catch (Exception ex) { log($"OneDrive : erreur — {ex.Message}"); }
        }

        // ════════════════════════════════════════════════════════════════
        //  Helpers privés
        // ════════════════════════════════════════════════════════════════

        private static void LaunchExe(string name, string? exePath, Action<string> log)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            { log($"{name} : exécutable introuvable, lancement ignoré."); return; }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
                log($"{name} lancé.");
            }
            catch (Exception ex) { log($"{name} : erreur de lancement — {ex.Message}"); }
        }

        private static bool IsInstalledViaRegistry(string displayNameContains)
        {
            string[] keys =
            [
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            ];
            foreach (var keyPath in keys)
            {
                try
                {
                    using var root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                    if (root == null) continue;
                    foreach (var subName in root.GetSubKeyNames())
                    {
                        using var sub = root.OpenSubKey(subName);
                        if (sub?.GetValue("DisplayName") is string dn
                            && dn.Contains(displayNameContains, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch { /* registre inaccessible */ }
            }
            return false;
        }

        private static string FindOfficeRootOrEmpty()
        {
            string[] roots =
            [
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            ];
            foreach (var root in roots)
            {
                var officeRoot = Path.Combine(root, "Microsoft Office");
                if (!Directory.Exists(officeRoot)) continue;
                var outlookExe = Directory.GetFiles(officeRoot, "OUTLOOK.EXE", SearchOption.AllDirectories)
                                          .FirstOrDefault();
                if (outlookExe != null) return outlookExe;
            }
            return string.Empty;
        }
    }
}
