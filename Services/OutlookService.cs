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
        /// Équivalent de la méthode registre de Save-OutlookData (sans COM, plus fiable en GUI).
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
    /// Service de lancement d'applications post-restauration (Outlook, OneNote, Edge, SAP).
    /// Équivalent de la section "LANCEMENT DES APPLICATIONS" de Restauration.ps1.
    /// </summary>
    public static class AppLauncherService
    {
        public static void LaunchApplications(bool launchSap, Action<string> log)
        {
            TryLaunch("OUTLOOK", "Outlook", log);
            TryLaunch("ONENOTE", "OneNote", log);
            TryLaunch("msedge", "Edge", log);
            if (launchSap)
                TryLaunch("saplogon", "SAP GUI", log);
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
