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
        // ══════════════════════════════════════════════════════════════════
        //  Détection — navigateurs
        //  Ordre de priorité : Edge > Chrome > Firefox > Brave > Opera > Opera GX
        // ══════════════════════════════════════════════════════════════════

        // ── Microsoft Edge ───────────────────────────────────────────────

        public static bool IsEdgeInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                // Edge est préinstallé dans System32 sur Windows 11
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "msedge.exe")
            };
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Microsoft Edge");
        }

        // ── Google Chrome ────────────────────────────────────────────────

        public static bool IsChromeInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "Application", "chrome.exe")
            };
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Google Chrome");
        }

        // ── Mozilla Firefox ──────────────────────────────────────────────

        public static bool IsFirefoxInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Mozilla Firefox", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Mozilla Firefox", "firefox.exe")
            };
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Mozilla Firefox");
        }

        // ── Brave ────────────────────────────────────────────────────────

        public static bool IsBraveInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
            };
            return paths.Any(File.Exists);
        }

        // ── Opera ────────────────────────────────────────────────────────

        public static bool IsOperaInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera", "opera.exe")
            };
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Opera Stable");
        }

        // ── Opera GX ─────────────────────────────────────────────────────

        public static bool IsOperaGxInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera GX", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera GX", "opera.exe")
            };
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Opera GX");
        }

        // ── OneDrive ─────────────────────────────────────────────────────

        public static bool IsOneDriveInstalled()
        {
            var oneDriveExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive", "OneDrive.exe");
            return File.Exists(oneDriveExe) || IsInstalledViaRegistry("OneDrive");
        }

        // ══════════════════════════════════════════════════════════════════
        //  Chemins de profil — navigateurs
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Dossier Default du profil Edge local.</summary>
        public static string GetEdgeProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default");

        /// <summary>Dossier Default du profil Chrome local.</summary>
        public static string GetChromeProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google", "Chrome", "User Data", "Default");

        /// <summary>Dossier racine des profils Firefox (contient les sous-dossiers *.default-release).</summary>
        public static string GetFirefoxProfilesPath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Mozilla", "Firefox", "Profiles");

        /// <summary>Dossier Default du profil Brave local.</summary>
        public static string GetBraveProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BraveSoftware", "Brave-Browser", "User Data", "Default");

        /// <summary>Dossier Default du profil Opera local (roaming).</summary>
        public static string GetOperaProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Opera Software", "Opera Stable");

        /// <summary>Dossier Default du profil Opera GX local (roaming).</summary>
        public static string GetOperaGxProfilePath()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Opera Software", "Opera GX Stable");

        // ══════════════════════════════════════════════════════════════════
        //  Exécutables
        // ══════════════════════════════════════════════════════════════════

        public static string? FindEdgeExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "msedge.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        public static string? FindChromeExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "Application", "chrome.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        public static string? FindFirefoxExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Mozilla Firefox", "firefox.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Mozilla Firefox", "firefox.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        public static string? FindBraveExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        public static string? FindOperaExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera", "opera.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        public static string? FindOperaGxExe()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera GX", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera GX", "opera.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Lancement des applications post-restauration
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lance les applications standards (Teams, Outlook, etc.).
        /// Appelé après restauration si chkLaunchApps est coché.
        /// </summary>
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
                    {
                        log($"{name} : exécutable introuvable, lancement ignoré.");
                        continue;
                    }

                    Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
                    log($"{name} lancé.");
                }
                catch (Exception ex)
                {
                    log($"{name} : erreur de lancement — {ex.Message}");
                }
            }
        }

        /// <summary>Lance Edge si installé (navigateur principal).</summary>
        public static void LaunchEdge(Action<string> log)
            => LaunchExe("Edge", FindEdgeExe(), log);

        /// <summary>Lance Chrome si installé.</summary>
        public static void LaunchChrome(Action<string> log)
            => LaunchExe("Chrome", FindChromeExe(), log);

        /// <summary>Lance Firefox si installé.</summary>
        public static void LaunchFirefox(Action<string> log)
            => LaunchExe("Firefox", FindFirefoxExe(), log);

        /// <summary>Lance Brave si installé.</summary>
        public static void LaunchBrave(Action<string> log)
            => LaunchExe("Brave", FindBraveExe(), log);

        /// <summary>Lance Opera si installé.</summary>
        public static void LaunchOpera(Action<string> log)
            => LaunchExe("Opera", FindOperaExe(), log);

        /// <summary>Lance Opera GX si installé.</summary>
        public static void LaunchOperaGx(Action<string> log)
            => LaunchExe("Opera GX", FindOperaGxExe(), log);

        /// <summary>
        /// Ouvre la popup OneDrive «\u00a0Gérer la sauvegarde\u00a0» pour activer la synchro
        /// Bureau / Documents / Images sur le nouveau poste.
        /// </summary>
        public static void OpenOneDriveBackupSettings(Action<string> log)
        {
            var oneDriveExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "OneDrive", "OneDrive.exe");

            if (!File.Exists(oneDriveExe))
            {
                log("OneDrive : introuvable, étape ignorée.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = oneDriveExe,
                    Arguments       = "/backuppc",
                    UseShellExecute = true
                });
                log("OneDrive — fenêtre 'Gérer la sauvegarde' ouverte.");
            }
            catch (Exception ex)
            {
                log($"OneDrive : erreur — {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Factory — liste pour BrowserPickerButton
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Retourne la liste ordonnée des navigateurs (Edge en premier)
        /// avec leur état d'installation, icône et chemin de profil.
        /// À injecter dans BrowserPickerButton.SetBrowsers().
        /// </summary>
        public static IReadOnlyList<SaveRestoreGUI.UI.BrowserEntry> GetBrowserEntries()
            =>
            [
                new("Edge",     "\U0001F1EA\U0001F1FA", IsEdgeInstalled(),     GetEdgeProfilePath(),    FindEdgeExe()),
                new("Chrome",   "\U0001F535",           IsChromeInstalled(),   GetChromeProfilePath(),  FindChromeExe()),
                new("Firefox",  "\U0001F98A",           IsFirefoxInstalled(),  GetFirefoxProfilesPath(), FindFirefoxExe()),
                new("Brave",    "\U0001F981",           IsBraveInstalled(),    GetBraveProfilePath(),   FindBraveExe()),
                new("Opera",    "\U0001F3AD",           IsOperaInstalled(),    GetOperaProfilePath(),   FindOperaExe()),
                new("Opera GX", "\U0001F3AE",           IsOperaGxInstalled(),  GetOperaGxProfilePath(), FindOperaGxExe()),
            ];

        // ══════════════════════════════════════════════════════════════════
        //  Helpers privés
        // ══════════════════════════════════════════════════════════════════

        private static void LaunchExe(string name, string? exePath, Action<string> log)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                log($"{name} : exécutable introuvable, lancement ignoré.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
                log($"{name} lancé.");
            }
            catch (Exception ex)
            {
                log($"{name} : erreur de lancement — {ex.Message}");
            }
        }

        private static bool IsInstalledViaRegistry(string displayNameContains)
        {
            string[] keys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var keyPath in keys)
            {
                try
                {
                    using var root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                    if (root == null) continue;

                    foreach (var subName in root.GetSubKeyNames())
                    {
                        using var sub = root.OpenSubKey(subName);
                        var dn = sub?.GetValue("DisplayName") as string;
                        if (dn != null && dn.Contains(displayNameContains, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch { /* registre inaccessible */ }
            }

            return false;
        }

        private static string FindOfficeRootOrEmpty()
        {
            // Cherche le premier dossier Office dans Program Files (x86) puis Program Files
            foreach (var root in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            })
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
