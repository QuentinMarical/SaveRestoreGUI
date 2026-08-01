using System.IO;

namespace SaveRestoreGUI.Services;

/// <summary>
/// Valide qu'un dossier contient bien les marqueurs d'une sauvegarde SaveRestoreGUI.
/// Utilisé dans BtnBrowseRestore_Click pour avertir si le dossier semble invalide.
/// </summary>
public static class BackupValidator
{
    private static readonly string[] KnownMarkers =
    [
        "Documents",
        "Desktop",
        "Downloads",
        "NetworkDrives.txt",
        "BackupInfo.json",
        "Outlook",
        "Signatures",
    ];

    /// <summary>
    /// Retourne <c>true</c> si le dossier contient au moins un marqueur reconnu.
    /// </summary>
    public static bool IsValidBackupFolder(string path)
    {
        if (!Directory.Exists(path)) return false;

        foreach (var marker in KnownMarkers)
        {
            var full = Path.Combine(path, marker);
            if (Directory.Exists(full) || File.Exists(full))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Détecte, pour un dossier de sauvegarde donné, quelles catégories (clés
    /// <see cref="UI.CheckItem.Key"/>) contiennent effectivement des données.
    /// Chaque condition reproduit exactement le test de présence utilisé par
    /// l'étape de restauration correspondante (RestoreStep, ThemeService.Restore,
    /// TaskbarService.Restore, SystemStateService.Restore, RegistryService.RestoreOneNoteKeys…)
    /// afin qu'une case cochée corresponde toujours à une donnée réellement restaurable.
    /// </summary>
    public static HashSet<string> DetectPresentCategories(string backupRoot)
    {
        var present = new HashSet<string>(StringComparer.Ordinal);
        if (!Directory.Exists(backupRoot)) return present;

        void AddIfDir(string key, string relativePath)
        {
            if (Directory.Exists(Path.Combine(backupRoot, relativePath))) present.Add(key);
        }

        AddIfDir("Documents", "Documents");
        AddIfDir("Desktop", "Desktop");
        AddIfDir("Downloads", "Downloads");
        AddIfDir("Pictures", "Pictures");
        AddIfDir("Music", "Music");
        AddIfDir("Videos", "Videos");
        AddIfDir("Public", "Public");
        AddIfDir("Signatures", "Signatures");
        AddIfDir("OfficeTemplates", "Templates");
        AddIfDir("ExcelMacros", Path.Combine("Excel", "XLSTART"));
        AddIfDir("Sap", "SAP");
        AddIfDir("Outlook", "OutlookData");
        AddIfDir("Theme", "Theme");
        AddIfDir("Taskbar", "Taskbar");

        var ipDir = Path.Combine(backupRoot, "IpDesktopSoftphone");
        if (Directory.Exists(ipDir) && Directory.GetDirectories(ipDir).Length > 0)
            present.Add("IpSoftphone");

        if (Directory.GetFiles(backupRoot, "OneNote_*.reg").Length > 0
            || File.Exists(Path.Combine(backupRoot, "OpenNotebook.reg")))
            present.Add("OneNote");

        if (File.Exists(Path.Combine(backupRoot, "StickyNotes.sqlite"))
            || File.Exists(Path.Combine(backupRoot, "StickyNotes.sqlite-wal"))
            || File.Exists(Path.Combine(backupRoot, "StickyNotes.sqlite-shm")))
            present.Add("StickyNotes");

        if (File.Exists(Path.Combine(backupRoot, "NetworkDrives.txt")))
            present.Add("NetworkDrives");

        if (File.Exists(Path.Combine(backupRoot, "SystemState", "settings.json")))
            present.Add("SystemState");

        foreach (var f in Directory.GetFiles(backupRoot, "Wallpaper.*"))
        {
            if (!Path.GetFileName(f).Equals("Wallpaper.log", StringComparison.OrdinalIgnoreCase))
            {
                present.Add("Wallpaper");
                break;
            }
        }

        foreach (var browser in BrowserService.All)
            AddIfDir(browser.Key, browser.BackupSubFolder);

        return present;
    }
}
