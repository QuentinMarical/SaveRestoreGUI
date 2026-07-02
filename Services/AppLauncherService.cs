using System.Diagnostics;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Détection d'installation et lancement des applications post-restauration.
    /// Chaque méthode IsXxxInstalled() inspecte les chemins d'installation courants
    /// et/ou le registre pour renvoyer true uniquement si l'app est présente.
    /// </summary>
    public static class AppLauncherService
    {
        // ════════════════════════════════════════════════════════════════
        //  Détection — navigateurs
        // ════════════════════════════════════════════════════════════════

        // ── Microsoft Edge ────────────────────────────────────────────────
        public static bool IsEdgeInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "msedge.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Microsoft Edge");
        }

        // ── Google Chrome ───────────────────────────────────────────────
        public static bool IsChromeInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "Application", "chrome.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Google Chrome");
        }

        // ── Mozilla Firefox ──────────────────────────────────────────────
        public static bool IsFirefoxInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Mozilla Firefox", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Mozilla Firefox", "firefox.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Mozilla Firefox");
        }

        // ── Brave ─────────────────────────────────────────────────────────
        public static bool IsBraveInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
            ];
            return paths.Any(File.Exists);
        }

        // ── Opera ─────────────────────────────────────────────────────────
        public static bool IsOperaInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera", "opera.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Opera Stable");
        }

        // ── Opera GX ─────────────────────────────────────────────────────
        public static bool IsOperaGxInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera GX", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera GX", "opera.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Opera GX");
        }

        // ── Vivaldi ────────────────────────────────────────────────────────
        public static bool IsVivaldiInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Vivaldi", "Application", "vivaldi.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Vivaldi", "Application", "vivaldi.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Vivaldi");
        }

        // ── Arc (The Browser Company) ─────────────────────────────────────
        public static bool IsArcInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Arc", "app-latest", "Arc.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Arc", "Arc.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Arc");
        }

        // ── Perplexity Comet ──────────────────────────────────────────────
        public static bool IsCometInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Perplexity", "Comet", "Comet.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Perplexity", "Comet", "Comet.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Comet");
        }

        // ── LibreWolf ──────────────────────────────────────────────────────
        public static bool IsLibreWolfInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "LibreWolf", "librewolf.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "LibreWolf", "librewolf.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("LibreWolf");
        }

        // ── Pale Moon ──────────────────────────────────────────────────────
        public static bool IsPaleMoonInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Pale Moon", "palemoon.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Pale Moon", "palemoon.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Pale Moon");
        }

        // ── Tor Browser ────────────────────────────────────────────────────
        public static bool IsTorBrowserInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Tor Browser", "Browser", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Desktop", "Tor Browser", "Browser", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Tor Browser", "Browser", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Tor Browser", "Browser", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tor Browser", "Browser", "firefox.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Tor Browser");
        }

        // ── DuckDuckGo Browser ─────────────────────────────────────────────
        public static bool IsDuckDuckGoBrowserInstalled()
        {
            string[] paths =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "DuckDuckGo", "DuckDuckGo.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DuckDuckGo", "DuckDuckGo.exe")
            ];
            return paths.Any(File.Exists) || IsInstalledViaRegistry("DuckDuckGo");
        }

        // ── OneDrive ────────────────────────────────────────────────────────
        public static bool IsOneDriveInstalled()
        {
            var oneDriveExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive", "OneDrive.exe");
            return File.Exists(oneDriveExe) || IsInstalledViaRegistry("OneDrive");
        }

        // ════════════════════════════════════════════════════════════════
        //  Chemins de profil — navigateurs
        // ════════════════════════════════════════════════════════════════

        public static string GetEdgeProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data");

        public static string GetChromeProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data");

        public static string GetFirefoxProfilesPath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Mozilla", "Firefox", "Profiles");

        public static string GetBraveProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BraveSoftware", "Brave-Browser", "User Data");

        public static string GetOperaProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Opera Software", "Opera Stable");

        public static string GetOperaGxProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Opera Software", "Opera GX Stable");

        public static string GetVivaldiProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Vivaldi", "User Data");

        public static string GetArcProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Arc", "User Data");

        public static string GetCometProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Perplexity", "Comet", "User Data");

        public static string GetLibreWolfProfilesPath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LibreWolf", "Profiles");

        public static string GetPaleMoonProfilesPath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Moonchild Productions", "Pale Moon", "Profiles");

        /// <summary>Tor Browser stocke le profil dans son dossier d'installation (portable).</summary>
        public static string GetTorBrowserProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Tor Browser", "Browser", "TorBrowser", "Data", "Browser");

        public static string GetDuckDuckGoProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DuckDuckGo", "User Data");

        // ════════════════════════════════════════════════════════════════
        //  Exécutables
        // ════════════════════════════════════════════════════════════════

        public static string? FindEdgeExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msedge.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindChromeExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "Application", "chrome.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindFirefoxExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Mozilla Firefox", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Mozilla Firefox", "firefox.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindBraveExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindOperaExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera", "opera.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindOperaGxExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera GX", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera GX", "opera.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindVivaldiExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Vivaldi", "Application", "vivaldi.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Vivaldi", "Application", "vivaldi.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindArcExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Arc", "app-latest", "Arc.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Arc", "Arc.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindCometExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Perplexity", "Comet", "Comet.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Perplexity", "Comet", "Comet.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindLibreWolfExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "LibreWolf", "librewolf.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "LibreWolf", "librewolf.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindPaleMoonExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Pale Moon", "palemoon.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Pale Moon", "palemoon.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindTorBrowserExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Tor Browser", "Browser", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Desktop", "Tor Browser", "Browser", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Tor Browser", "Browser", "firefox.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        public static string? FindDuckDuckGoExe()
        {
            string[] c =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "DuckDuckGo", "DuckDuckGo.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DuckDuckGo", "DuckDuckGo.exe")
            ];
            return c.FirstOrDefault(File.Exists);
        }

        // ════════════════════════════════════════════════════════════════
        //  Lancement des applications post-restauration
        // ════════════════════════════════════════════════════════════════

        public static void LaunchApplications(Action<string> log)
        {
            var apps = new[]
            {
                ("Teams",   @"AppData\Local\Microsoft\Teams\current\Teams.exe", true),
                ("Outlook", Path.Combine(FindOfficeRootOrEmpty(), "OUTLOOK.EXE"), false)
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

        public static void LaunchEdge(Action<string> log)      => LaunchExe("Edge",         FindEdgeExe(),        log);
        public static void LaunchChrome(Action<string> log)    => LaunchExe("Chrome",       FindChromeExe(),      log);
        public static void LaunchFirefox(Action<string> log)   => LaunchExe("Firefox",      FindFirefoxExe(),     log);
        public static void LaunchBrave(Action<string> log)     => LaunchExe("Brave",        FindBraveExe(),       log);
        public static void LaunchOpera(Action<string> log)     => LaunchExe("Opera",        FindOperaExe(),       log);
        public static void LaunchOperaGx(Action<string> log)   => LaunchExe("Opera GX",    FindOperaGxExe(),     log);
        public static void LaunchVivaldi(Action<string> log)   => LaunchExe("Vivaldi",      FindVivaldiExe(),     log);
        public static void LaunchArc(Action<string> log)       => LaunchExe("Arc",          FindArcExe(),         log);
        public static void LaunchComet(Action<string> log)     => LaunchExe("Comet",        FindCometExe(),       log);
        public static void LaunchLibreWolf(Action<string> log) => LaunchExe("LibreWolf",    FindLibreWolfExe(),   log);
        public static void LaunchPaleMoon(Action<string> log)  => LaunchExe("Pale Moon",    FindPaleMoonExe(),    log);
        public static void LaunchTorBrowser(Action<string> log) => LaunchExe("Tor Browser", FindTorBrowserExe(), log);
        public static void LaunchDuckDuckGo(Action<string> log) => LaunchExe("DuckDuckGo",  FindDuckDuckGoExe(), log);

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
        //  Factory — liste pour BrowserPickerButton
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Retourne la liste complète des navigateurs supportés (13 entrées)
        /// avec leur état d'installation, icône et chemin de profil.
        /// À injecter dans BrowserPickerButton.SetBrowsers().
        /// </summary>
        public static IReadOnlyList<SaveRestoreGUI.UI.BrowserEntry> GetBrowserEntries()
            =>
            [
                new("Microsoft Edge",       "🃪🇺", IsEdgeInstalled(),              GetEdgeProfilePath(),          FindEdgeExe()),
                new("Google Chrome",        "🔵",       IsChromeInstalled(),         GetChromeProfilePath(),        FindChromeExe()),
                new("Mozilla Firefox",      "𞦊",       IsFirefoxInstalled(),        GetFirefoxProfilesPath(),      FindFirefoxExe()),
                new("Brave",                "🦁",       IsBraveInstalled(),          GetBraveProfilePath(),         FindBraveExe()),
                new("Opera",               "🎭",       IsOperaInstalled(),          GetOperaProfilePath(),         FindOperaExe()),
                new("Opera GX",            "🎮",       IsOperaGxInstalled(),        GetOperaGxProfilePath(),       FindOperaGxExe()),
                new("Vivaldi",             "🎼",       IsVivaldiInstalled(),        GetVivaldiProfilePath(),       FindVivaldiExe()),
                new("Arc",                 "🌈",       IsArcInstalled(),            GetArcProfilePath(),           FindArcExe()),
                new("Perplexity Comet",    "🪐",       IsCometInstalled(),          GetCometProfilePath(),         FindCometExe()),
                new("LibreWolf",           "🐺",       IsLibreWolfInstalled(),      GetLibreWolfProfilesPath(),    FindLibreWolfExe()),
                new("Pale Moon",           "🌙",       IsPaleMoonInstalled(),       GetPaleMoonProfilesPath(),     FindPaleMoonExe()),
                new("Tor Browser",         "🧕",       IsTorBrowserInstalled(),     GetTorBrowserProfilePath(),    FindTorBrowserExe()),
                new("DuckDuckGo Browser",  "🦆",       IsDuckDuckGoBrowserInstalled(), GetDuckDuckGoProfilePath(), FindDuckDuckGoExe()),
            ];

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
