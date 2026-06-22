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
}
