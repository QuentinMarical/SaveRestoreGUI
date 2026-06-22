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
        // ══════════════════════════════════════════════════════════════════

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

        public static bool IsOperaInstalled()
        {
            string[] paths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Opera", "opera.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Opera", "opera.exe")
            };
            return paths.Any(File.Exists) || IsInstalledViaRegistry("Opera");
        }

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

        // ══════════════════════════════════════════════════════════════════
        //  Exécutables
        // ══════════════════════════════════════════════════════════════════

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
