using System.Diagnostics;
using System.Text;
using Microsoft.Win32;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Service Outlook : détection PST (emplacements standards + registre), règles .rwz,
    /// boîtes partagées (registre), cache d'autocomplétion.
    /// Reproduit les fonctions Get-OutlookPSTFiles / Save-OutlookData / Restore-OutlookData des scripts.
    /// </summary>
    public static class OutlookService
    {
        private static readonly string[] OfficeVersions = { "14.0", "15.0", "16.0" };

        /// <summary>
        /// Recherche les fichiers PST dans les emplacements standards ET dans le registre Outlook
        /// (PST attachés au profil). Équivalent de Get-OutlookPSTFiles.
        /// </summary>
        public static List<string> FindPstFiles()
        {
            var pstFiles = new List<string>(StringComparer.OrdinalIgnoreCase.Equals("a", "a") ? 8 : 8);
            var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var standardPaths = new[]
            {
                Path.Combine(docsPath, "Outlook Files"),
                Path.Combine(docsPath, "Fichiers Outlook"),
                Path.Combine(localAppData, "Microsoft", "Outlook")
            };

            foreach (var path in standardPaths)
            {
                if (!Directory.Exists(path)) continue;
                foreach (var f in Directory.GetFiles(path, "*.pst"))
                {
                    if (!pstFiles.Contains(f, StringComparer.OrdinalIgnoreCase))
                        pstFiles.Add(f);
                }
            }

            // Recherche dans le registre des PST attachés au profil Outlook
            foreach (var version in OfficeVersions)
            {
                try
                {
                    using var profilesKey = Registry.CurrentUser.OpenSubKey(
                        $@"Software\Microsoft\Office\{version}\Outlook\Profiles");
                    if (profilesKey == null) continue;
                    ScanRegistryForPst(profilesKey, pstFiles);
                }
                catch { /* accès registre refusé — ignorer */ }
            }

            return pstFiles;
        }

        private static void ScanRegistryForPst(RegistryKey key, List<string> pstFiles)
        {
            foreach (var valueName in key.GetValueNames())
            {
                if (key.GetValue(valueName) is byte[] bytes)
                {
                    var str = Encoding.Unicode.GetString(bytes).Replace("\0", "");
                    if (str.EndsWith(".pst", StringComparison.OrdinalIgnoreCase) && File.Exists(str)
                        && !pstFiles.Contains(str, StringComparer.OrdinalIgnoreCase))
                    {
                        pstFiles.Add(str);
                    }
                }
            }
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey != null) ScanRegistryForPst(subKey, pstFiles);
                }
                catch { }
            }
        }

        /// <summary>
        /// Détecte les boîtes aux lettres partagées via le registre Outlook
        /// (adresses email dans les profils, hors compte principal).
        /// </summary>
        public static List<string> FindSharedMailboxes()
        {
            var mailboxes = new List<string>();
            var currentUser = Environment.UserName;

            foreach (var version in OfficeVersions)
            {
                try
                {
                    using var profilesKey = Registry.CurrentUser.OpenSubKey(
                        $@"Software\Microsoft\Office\{version}\Outlook\Profiles");
                    if (profilesKey == null) continue;
                    ScanRegistryForEmails(profilesKey, mailboxes, currentUser);
                }
                catch { }
            }

            return mailboxes
                .Where(m => !string.IsNullOrWhiteSpace(m)
                            && !m.StartsWith(currentUser, StringComparison.OrdinalIgnoreCase)
                            && !m.Contains("Outlook", StringComparison.OrdinalIgnoreCase)
                            && !m.EndsWith(".pst", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .ToList();
        }

        private static void ScanRegistryForEmails(RegistryKey key, List<string> mailboxes, string currentUser)
        {
            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName);
                string? candidate = null;

                if (value is string s && System.Text.RegularExpressions.Regex.IsMatch(
                        s, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
                {
                    candidate = s;
                }
                else if (value is byte[] bytes)
                {
                    var str = Encoding.Unicode.GetString(bytes).Replace("\0", "");
                    var match = System.Text.RegularExpressions.Regex.Match(
                        str, @"([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})");
                    if (match.Success) candidate = match.Groups[1].Value;
                }

                if (candidate != null
                    && !candidate.Contains(currentUser, StringComparison.OrdinalIgnoreCase)
                    && !mailboxes.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    mailboxes.Add(candidate);
                }
            }
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey != null) ScanRegistryForEmails(subKey, mailboxes, currentUser);
                }
                catch { }
            }
        }

        /// <summary>
        /// Recherche les fichiers de règles Outlook (.rwz) dans %APPDATA%\Microsoft\Outlook.
        /// </summary>
        public static string[] FindRulesFiles()
        {
            var rulesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Outlook");
            return Directory.Exists(rulesPath) ? Directory.GetFiles(rulesPath, "*.rwz") : Array.Empty<string>();
        }

        /// <summary>
        /// Copie la liste des boîtes partagées dans le presse-papiers (pour reconfiguration manuelle).
        /// </summary>
        public static bool CopyToClipboard(IEnumerable<string> lines)
        {
            try
            {
                var text = string.Join(Environment.NewLine, lines);
                if (string.IsNullOrEmpty(text)) return false;
                Clipboard.SetText(text);
                return true;
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Service de lancement d'applications post-restauration (Outlook, OneNote, Edge,
    /// navigateurs grand public + alternatifs) et d'activation de la synchronisation OneDrive.
    /// SAP Logon retiré — non pertinent sur les postes utilisateurs standard.
    /// </summary>
    public static class AppLauncherService
    {
        // ── Variables helper ──────────────────────────────────────────────────────────
        private static string Local  => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string Prog   => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        private static string Prog86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        private static string Profile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // ── Table des navigateurs (ordre = priorité chemin, premier trouvé est lancé) ─
        // Chaque entrée : (cheminExe, nomAffichage)
        // SAP Logon intentionnellement absent.
        private static readonly (string Path, string Name)[] Browsers =
        {
            // ── Microsoft Edge (inclus Windows 10/11 — toujours présent) ─────────────
            ("msedge",                                                                      "Microsoft Edge"),

            // ── Firefox ───────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Prog,   "Mozilla Firefox",  "firefox.exe"),            "Firefox"),
            (System.IO.Path.Combine(Prog86, "Mozilla Firefox",  "firefox.exe"),            "Firefox"),

            // ── Brave ─────────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "BraveSoftware", "Brave-Browser", "Application", "brave.exe"), "Brave"),
            (System.IO.Path.Combine(Prog,   "BraveSoftware", "Brave-Browser", "Application", "brave.exe"), "Brave"),

            // ── Google Chrome ─────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Google", "Chrome", "Application", "chrome.exe"),  "Google Chrome"),
            (System.IO.Path.Combine(Prog,   "Google", "Chrome", "Application", "chrome.exe"),  "Google Chrome"),
            (System.IO.Path.Combine(Prog86, "Google", "Chrome", "Application", "chrome.exe"),  "Google Chrome"),

            // ── Opera ─────────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Programs", "Opera",    "launcher.exe"),        "Opera"),
            (System.IO.Path.Combine(Prog,   "Opera",                "launcher.exe"),        "Opera"),

            // ── Opera GX ─────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Programs", "Opera GX", "launcher.exe"),        "Opera GX"),
            (System.IO.Path.Combine(Prog,   "Opera GX",             "launcher.exe"),        "Opera GX"),

            // ── Vivaldi ───────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Vivaldi", "Application", "vivaldi.exe"),       "Vivaldi"),
            (System.IO.Path.Combine(Prog,   "Vivaldi", "Application", "vivaldi.exe"),       "Vivaldi"),

            // ── Arc (The Browser Company) ─────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Programs", "Arc",       "Arc.exe"),            "Arc"),
            (System.IO.Path.Combine(Prog,   "Arc",                   "Arc.exe"),            "Arc"),

            // ── Perplexity Comet ──────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Programs", "Perplexity Comet", "Comet.exe"),   "Perplexity Comet"),
            (System.IO.Path.Combine(Prog,   "Perplexity Comet",             "Comet.exe"),   "Perplexity Comet"),

            // ── LibreWolf ─────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "Programs", "LibreWolf", "librewolf.exe"),      "LibreWolf"),
            (System.IO.Path.Combine(Prog,   "LibreWolf",             "librewolf.exe"),      "LibreWolf"),

            // ── Pale Moon ─────────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Prog,   "Pale Moon",  "palemoon.exe"),                  "Pale Moon"),
            (System.IO.Path.Combine(Prog86, "Pale Moon",  "palemoon.exe"),                  "Pale Moon"),

            // ── Tor Browser ───────────────────────────────────────────────────────────
            (System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "Tor Browser", "Browser", "firefox.exe"),                                   "Tor Browser"),
            (System.IO.Path.Combine(Profile, "Desktop",    "Tor Browser", "Browser", "firefox.exe"), "Tor Browser"),
            (System.IO.Path.Combine(Profile, "Downloads",  "tor-browser",  "Browser", "firefox.exe"), "Tor Browser"),

            // ── DuckDuckGo Browser ────────────────────────────────────────────────────
            (System.IO.Path.Combine(Local,  "DuckDuckGo", "DuckDuckGo.exe"),                "DuckDuckGo Browser"),
            (System.IO.Path.Combine(Prog,   "DuckDuckGo", "DuckDuckGo.exe"),                "DuckDuckGo Browser"),
        };

        /// <summary>
        /// Lance Outlook, OneNote, et tous les navigateurs détectés sur le poste.
        /// SAP Logon n'est plus inclus.
        /// </summary>
        public static void LaunchApplications(Action<string> log)
        {
            TryLaunch("OUTLOOK",  "Outlook",  log);
            TryLaunch("ONENOTE",  "OneNote",  log);

            // Lance chaque navigateur trouvé (un seul lancement par nom même si plusieurs chemins)
            var launched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (path, name) in Browsers)
            {
                if (launched.Contains(name)) continue;

                // Edge est lancé via son nom de processus (pas un chemin fichier)
                bool found = name == "Microsoft Edge"
                    ? true
                    : File.Exists(path);

                if (!found) continue;

                try
                {
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                    log($"{name} démarré.");
                }
                catch
                {
                    log($"Impossible de démarrer {name}.");
                }
                launched.Add(name);
            }
        }

        /// <summary>
        /// Ouvre la popup OneDrive « Gérer la sauvegarde » (Bureau / Documents / Images).
        /// Tente d'abord l'URI onedrive:, puis OneDrive.exe /configure_backup.
        /// </summary>
        public static void OpenOneDriveBackupSettings(Action<string> log)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "onedrive:",
                    Arguments       = "?fref=pcb",
                    UseShellExecute = true
                });
                log("OneDrive : popup \"Gérer la sauvegarde\" ouverte.");
                return;
            }
            catch { }

            var oneDrivePaths = new[]
            {
                System.IO.Path.Combine(Local, "Microsoft", "OneDrive", "OneDrive.exe"),
                System.IO.Path.Combine(Prog,  "Microsoft OneDrive", "OneDrive.exe"),
                System.IO.Path.Combine(Prog86,"Microsoft OneDrive", "OneDrive.exe"),
            };

            foreach (var exe in oneDrivePaths)
            {
                if (!File.Exists(exe)) continue;
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = exe,
                        Arguments       = "/configure_backup",
                        UseShellExecute = true
                    });
                    log("OneDrive : popup \"Gérer la sauvegarde\" ouverte via OneDrive.exe.");
                    return;
                }
                catch { }
            }

            log("OneDrive : impossible d'ouvrir la popup de sauvegarde (OneDrive non installé ?).");
        }

        private static void TryLaunch(string processName, string displayName, Action<string> log)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = processName, UseShellExecute = true });
                log($"{displayName} démarré.");
            }
            catch
            {
                log($"Impossible de démarrer {displayName} (non installé ?).");
            }
        }
    }
}
