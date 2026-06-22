namespace SaveRestoreGUI.Services;

/// <summary>
/// Analyse les lignes de NetworkDrives.txt (format : LETTRE|CHEMIN_UNC|LIBELLE).
/// </summary>
public static class NetworkDriveParser
{
    public record DriveEntry(string Letter, string UncPath, string Label);

    /// <summary>Parse une collection de lignes et retourne les entrées valides.</summary>
    public static List<DriveEntry> ParseLines(IEnumerable<string> lines)
    {
        var result = new List<DriveEntry>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('|');
            if (parts.Length < 3) continue;
            var letter = parts[0].Trim();
            var unc    = parts[1].Trim();
            var label  = parts[2].Trim();
            if (string.IsNullOrEmpty(letter) || string.IsNullOrEmpty(unc)) continue;
            result.Add(new DriveEntry(letter, unc, label));
        }
        return result;
    }

    /// <summary>Parse un fichier NetworkDrives.txt.</summary>
    public static List<DriveEntry> ParseFile(string filePath)
        => File.Exists(filePath)
            ? ParseLines(File.ReadAllLines(filePath))
            : [];
}
