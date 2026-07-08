using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Définition statique d'un navigateur pris en charge.
    /// </summary>
    public sealed class BrowserDef
    {
        /// <summary>Clé unique (utilisée dans CheckCatalog et IsChecked).</summary>
        public string Key { get; }

        /// <summary>Nom affiché dans l'interface.</summary>
        public string DisplayName { get; }

        /// <summary>Nom du processus Windows (sans .exe) pour vérification fermeture.</summary>
        public string ProcessName { get; }

        /// <summary>Sous-dossier de destination dans la sauvegarde (ex. "Browsers\\Edge").</summary>
        public string BackupSubFolder { get; }

        /// <summary>
        /// Retourne le chemin AppData source du profil (null si introuvable).
        /// Évalué à l'exécution car dépend de l'environnement.
        /// </summary>
        public Func<string?> ProfilePathFactory { get; }

        /// <summary>
        /// Noms DisplayName de clés registre HKLM Uninstall pour détecter l'installation.
        /// Peut être vide si on se fie uniquement à la présence de l'exécutable ou du profil.
        /// </summary>
        public string[] RegistryUninstallKeys { get; }

        /// <summary>Chemins d'exécutable candidats pour détecter une installation sans registre.</summary>
        public string[] ExecutableCandidates { get; }

        public BrowserDef(
            string key,
            string displayName,
            string processName,
            string backupSubFolder,
            Func<string?> profilePathFactory,
            string[] registryUninstallKeys,
            string[] executableCandidates)
        {
            Key                   = key;
            DisplayName           = displayName;
            ProcessName           = processName;
            BackupSubFolder       = backupSubFolder;
            ProfilePathFactory    = profilePathFactory;
            RegistryUninstallKeys = registryUninstallKeys;
            ExecutableCandidates  = executableCandidates;
        }
    }

    /// <summary>
    /// Catalogue statique des navigateurs pris en charge et utilitaires de détection.
    /// </summary>
    public static class BrowserService
    {
        private static readonly string _local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string _roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string _pf86    = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        private static readonly string _pf64    = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        /// <summary>Catalogue ordonné de tous les navigateurs supportés.</summary>
        public static readonly IReadOnlyList<BrowserDef> All = new[]
        {
            // ── Chromium-based ──────────────────────────────────────────────

            new BrowserDef(
                key:                   "BrowserEdge",
                displayName:           "Microsoft Edge",
                processName:           "msedge",
                backupSubFolder:       @"Browsers\Edge",
                profilePathFactory:    () => Path.Combine(_local, "Microsoft", "Edge", "User Data", "Default"),
                registryUninstallKeys: new[] { "Microsoft Edge" },
                executableCandidates:  new[]
                {
                    Path.Combine(_pf86, "Microsoft", "Edge", "Application", "msedge.exe"),
                    Path.Combine(_pf64, "Microsoft", "Edge", "Application", "msedge.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserChrome",
                displayName:           "Google Chrome",
                processName:           "chrome",
                backupSubFolder:       @"Browsers\Chrome",
                profilePathFactory:    () => Path.Combine(_local, "Google", "Chrome", "User Data", "Default"),
                registryUninstallKeys: new[] { "Google Chrome" },
                executableCandidates:  new[]
                {
                    Path.Combine(_pf86, "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(_pf64, "Google", "Chrome", "Application", "chrome.exe"),
                    Path.Combine(_local, "Google", "Chrome", "Application", "chrome.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserBrave",
                displayName:           "Brave",
                processName:           "brave",
                backupSubFolder:       @"Browsers\Brave",
                profilePathFactory:    () => Path.Combine(_local, "BraveSoftware", "Brave-Browser", "User Data", "Default"),
                registryUninstallKeys: new[] { "BraveSoftware Brave-Browser" },
                executableCandidates:  new[]
                {
                    Path.Combine(_pf86, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                    Path.Combine(_pf64, "BraveSoftware", "Brave-Browser", "Application", "brave.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserVivaldi",
                displayName:           "Vivaldi",
                processName:           "vivaldi",
                backupSubFolder:       @"Browsers\Vivaldi",
                profilePathFactory:    () => Path.Combine(_local, "Vivaldi", "User Data", "Default"),
                registryUninstallKeys: new[] { "Vivaldi" },
                executableCandidates:  new[]
                {
                    Path.Combine(_local, "Vivaldi", "Application", "vivaldi.exe"),
                    Path.Combine(_pf86,  "Vivaldi", "Application", "vivaldi.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserOpera",
                displayName:           "Opera",
                processName:           "opera",
                backupSubFolder:       @"Browsers\Opera",
                profilePathFactory:    () => Path.Combine(_roaming, "Opera Software", "Opera Stable"),
                registryUninstallKeys: new[] { "Opera Stable" },
                executableCandidates:  new[]
                {
                    Path.Combine(_local, "Programs", "Opera", "opera.exe"),
                    Path.Combine(_pf86,  "Opera", "opera.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserOperaGX",
                displayName:           "Opera GX",
                processName:           "opera",
                backupSubFolder:       @"Browsers\OperaGX",
                profilePathFactory:    () => Path.Combine(_roaming, "Opera Software", "Opera GX Stable"),
                registryUninstallKeys: new[] { "Opera GX Stable" },
                executableCandidates:  new[]
                {
                    Path.Combine(_local, "Programs", "Opera GX", "opera.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserArc",
                displayName:           "Arc",
                processName:           "arc",
                backupSubFolder:       @"Browsers\Arc",
                profilePathFactory:    () =>
                {
                    var chromiumPath = Path.Combine(_local, "Arc", "User Data", "Default");
                    if (Directory.Exists(chromiumPath)) return chromiumPath;

                    var msixBase = Path.Combine(_local, "Packages");
                    if (Directory.Exists(msixBase))
                    {
                        var pkg = Directory.EnumerateDirectories(msixBase)
                            .FirstOrDefault(d => Path.GetFileName(d).StartsWith(
                                "TheBrowserCompany", StringComparison.OrdinalIgnoreCase));
                        if (pkg != null) return Path.Combine(pkg, "LocalState");
                    }
                    return null;
                },
                registryUninstallKeys: new[] { "Arc" },
                executableCandidates:  new[]
                {
                    Path.Combine(_local, "Arc", "arc.exe"),
                    Path.Combine(_pf64,  "Arc", "arc.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserComet",
                displayName:           "Perplexity Comet",
                processName:           "comet",
                backupSubFolder:       @"Browsers\Comet",
                profilePathFactory:    () =>
                {
                    var chromiumPath = Path.Combine(_local, "Perplexity", "Comet", "User Data", "Default");
                    if (Directory.Exists(chromiumPath)) return chromiumPath;

                    var msixBase = Path.Combine(_local, "Packages");
                    if (Directory.Exists(msixBase))
                    {
                        var pkg = Directory.EnumerateDirectories(msixBase)
                            .FirstOrDefault(d => Path.GetFileName(d).StartsWith(
                                "Perplexity", StringComparison.OrdinalIgnoreCase));
                        if (pkg != null) return Path.Combine(pkg, "LocalState");
                    }
                    return null;
                },
                registryUninstallKeys: new[] { "Perplexity Comet", "Comet" },
                executableCandidates:  new[]
                {
                    Path.Combine(_local, "Perplexity", "Comet", "comet.exe"),
                    Path.Combine(_pf64,  "Perplexity", "Comet", "comet.exe"),
                }),

            // ── Firefox-based ────────────────────────────────────────────────

            new BrowserDef(
                key:                   "BrowserFirefox",
                displayName:           "Mozilla Firefox",
                processName:           "firefox",
                backupSubFolder:       @"Browsers\Firefox",
                profilePathFactory:    () => FindMozillaProfile("Mozilla", "Firefox"),
                registryUninstallKeys: new[] { "Mozilla Firefox" },
                executableCandidates:  new[]
                {
                    Path.Combine(_pf86, "Mozilla Firefox", "firefox.exe"),
                    Path.Combine(_pf64, "Mozilla Firefox", "firefox.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserLibreWolf",
                displayName:           "LibreWolf",
                processName:           "librewolf",
                backupSubFolder:       @"Browsers\LibreWolf",
                profilePathFactory:    () => FindMozillaProfile("LibreWolf", "LibreWolf"),
                registryUninstallKeys: new[] { "LibreWolf" },
                executableCandidates:  new[]
                {
                    Path.Combine(_pf86, "LibreWolf", "librewolf.exe"),
                    Path.Combine(_pf64, "LibreWolf", "librewolf.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserPaleMoon",
                displayName:           "Pale Moon",
                processName:           "palemoon",
                backupSubFolder:       @"Browsers\PaleMoon",
                profilePathFactory:    () => FindMozillaProfile("Moonchild Productions", "Pale Moon"),
                registryUninstallKeys: new[] { "Pale Moon" },
                executableCandidates:  new[]
                {
                    Path.Combine(_pf86, "Pale Moon", "palemoon.exe"),
                    Path.Combine(_pf64, "Pale Moon", "palemoon.exe"),
                }),

            new BrowserDef(
                key:                   "BrowserTor",
                displayName:           "Tor Browser",
                processName:           "firefox",   // Tor embarque firefox.exe
                backupSubFolder:       @"Browsers\TorBrowser",
                profilePathFactory:    () =>
                {
                    // Tor est généralement portable, vérifier plusieurs emplacements courants
                    var candidates = new[]
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "Tor Browser", "Browser", "TorBrowser", "Data", "Browser", "profile.default"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "Downloads", "Tor Browser", "Browser", "TorBrowser", "Data", "Browser", "profile.default"),
                        Path.Combine(_local,  "Tor Browser", "Browser", "TorBrowser", "Data", "Browser", "profile.default"),
                        Path.Combine(_pf64,   "Tor Browser", "Browser", "TorBrowser", "Data", "Browser", "profile.default"),
                    };
                    return candidates.FirstOrDefault(Directory.Exists);
                },
                registryUninstallKeys: new[] { "Tor Browser" },
                executableCandidates:  Array.Empty<string>()),  // portable, pas de chemin fixe

            new BrowserDef(
                key:                   "BrowserDDG",
                displayName:           "DuckDuckGo",
                processName:           "duckduckgo",
                backupSubFolder:       @"Browsers\DuckDuckGo",
                profilePathFactory:    () =>
                {
                    // DuckDuckGo for Windows (MSIX)
                    var msixBase = Path.Combine(_local, "Packages");
                    if (Directory.Exists(msixBase))
                    {
                        var pkg = Directory.EnumerateDirectories(msixBase)
                            .FirstOrDefault(d => Path.GetFileName(d).StartsWith(
                                "DuckDuckGo", StringComparison.OrdinalIgnoreCase));
                        if (pkg != null) return Path.Combine(pkg, "LocalState");
                    }
                    // Fallback chemin classique
                    var classic = Path.Combine(_local, "DuckDuckGo", "Browser", "User Data", "Default");
                    return Directory.Exists(classic) ? classic : null;
                },
                registryUninstallKeys: new[] { "DuckDuckGo" },
                executableCandidates:  Array.Empty<string>()),
        };

        // ── Utilitaires publics ─────────────────────────────────────────────

        /// <summary>
        /// Retourne true si le navigateur est installé (clé Uninstall ou exécutable présent).
        /// </summary>
        public static bool IsInstalled(BrowserDef browser)
        {
            foreach (var keyName in browser.RegistryUninstallKeys)
            {
                if (IsInUninstall(keyName, RegistryView.Registry64)) return true;
                if (IsInUninstall(keyName, RegistryView.Registry32)) return true;
            }
            return browser.ExecutableCandidates.Any(File.Exists);
        }

        /// <summary>
        /// Retourne true si des données de profil existent et ne sont pas vides.
        /// </summary>
        public static bool HasProfileData(BrowserDef browser)
        {
            try
            {
                var path = browser.ProfilePathFactory();
                return path != null && Directory.Exists(path) &&
                       Directory.EnumerateFileSystemEntries(path).Any();
            }
            catch { return false; }
        }

        /// <summary>
        /// Retourne true si la case doit être pré-cochée :
        /// des données de profil existent (installé ou non).
        /// </summary>
        public static bool ShouldPreCheck(BrowserDef browser)
            => HasProfileData(browser);

        // ── Helpers privés ──────────────────────────────────────────────────

        private static bool IsInUninstall(string displayNameFragment, RegistryView view)
        {
            try
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstall = hklm.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall == null) return false;

                foreach (var subName in uninstall.GetSubKeyNames())
                {
                    using var sub = uninstall.OpenSubKey(subName);
                    var dn = sub?.GetValue("DisplayName") as string;
                    if (dn != null && dn.Contains(
                            displayNameFragment, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Cherche le profil actif d'un navigateur basé sur Mozilla (profiles.ini).
        /// Retourne le chemin du profil marqué Default=1, ou le premier disponible.
        /// </summary>
        public static string? FindMozillaProfile(string vendorFolder, string appFolder)
        {
            var basePath = Path.Combine(_roaming, vendorFolder, appFolder, "Profiles");
            if (!Directory.Exists(basePath)) return null;

            var iniPath = Path.Combine(Path.GetDirectoryName(basePath)!, "profiles.ini");

            if (File.Exists(iniPath))
            {
                var lines    = File.ReadAllLines(iniPath);
                var sections = new Dictionary<string, Dictionary<string, string>>();
                string? currentSection = null;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                    {
                        currentSection = trimmed[1..^1];
                        sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    else if (currentSection != null && trimmed.Contains('='))
                    {
                        var eq = trimmed.IndexOf('=');
                        sections[currentSection][trimmed[..eq].Trim()] = trimmed[(eq + 1)..].Trim();
                    }
                }

                // Profil marqué Default=1 en priorité
                foreach (var sec in sections.Values)
                {
                    if (sec.TryGetValue("Default", out var isDefault) && isDefault == "1" &&
                        sec.TryGetValue("Path",    out var relPath))
                    {
                        var fullPath = Path.IsPathRooted(relPath)
                            ? relPath
                            : Path.Combine(Path.GetDirectoryName(iniPath)!,
                                relPath.Replace('/', Path.DirectorySeparatorChar));
                        if (Directory.Exists(fullPath)) return fullPath;
                    }
                }

                // Fallback : premier profil avec un chemin valide
                foreach (var sec in sections.Values)
                {
                    if (sec.TryGetValue("Path", out var relPath))
                    {
                        var fullPath = Path.IsPathRooted(relPath)
                            ? relPath
                            : Path.Combine(Path.GetDirectoryName(iniPath)!,
                                relPath.Replace('/', Path.DirectorySeparatorChar));
                        if (Directory.Exists(fullPath)) return fullPath;
                    }
                }
            }

            // Dernier recours : premier sous-dossier de Profiles/
            var dirs = Directory.GetDirectories(basePath);
            return dirs.Length > 0 ? dirs[0] : null;
        }
    }
}
