using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class NetworkDrivesTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static string WriteTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"nd_{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        return path;
    }

    // ── ParseNetworkDrivesFile ────────────────────────────────────────────────

    [Fact]
    public void Parse_TypicalLine_ReturnsDrive()
    {
        var path = WriteTempFile("G:\t\\\\srv\\share\tPartage Compta");
        try
        {
            var drives = NetworkDriveParser.ParseFile(path);
            Assert.Single(drives);
            Assert.Equal("G:", drives[0].Letter);
            Assert.Equal("\\\\srv\\share", drives[0].UncPath);
            Assert.Equal("Partage Compta", drives[0].Label);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_EmptyFile_ReturnsEmpty()
    {
        var path = WriteTempFile("");
        try   { Assert.Empty(NetworkDriveParser.ParseFile(path)); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_IgnoresCommentLines()
    {
        var path = WriteTempFile("# commentaire\nG:\t\\\\srv\\share\tLabel");
        try
        {
            var drives = NetworkDriveParser.ParseFile(path);
            Assert.Single(drives);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_IgnoresBlankLines()
    {
        var path = WriteTempFile("\n\nG:\t\\\\srv\\share\tLabel\n\n");
        try
        {
            var drives = NetworkDriveParser.ParseFile(path);
            Assert.Single(drives);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_MultipleDrives_ReturnsAll()
    {
        var content = string.Join("\n",
            "G:\t\\\\srv\\share1\tLabel1",
            "H:\t\\\\srv\\share2\tLabel2",
            "Z:\t\\\\srv\\share3\tLabel3");
        var path = WriteTempFile(content);
        try
        {
            var drives = NetworkDriveParser.ParseFile(path);
            Assert.Equal(3, drives.Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_MissingLabel_IsEmpty()
    {
        var path = WriteTempFile("G:\t\\\\srv\\share");
        try
        {
            var drives = NetworkDriveParser.ParseFile(path);
            Assert.Single(drives);
            Assert.Equal("", drives[0].Label);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_InvalidLine_IsSkipped()
    {
        var path = WriteTempFile("invalid_no_tab_separator");
        try   { Assert.Empty(NetworkDriveParser.ParseFile(path)); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Parse_FileNotFound_ReturnsEmpty()
    {
        var missing = Path.Combine(Path.GetTempPath(), "__no_such_file__.txt");
        Assert.Empty(NetworkDriveParser.ParseFile(missing));
    }

    // ── Lookup NetworkDrives.txt dans backupRoot ────────────────────────────────
    // Simule le comportement de MigrateNetworkDrivesAsync : cherche NetworkDrives.txt
    // à la racine du drive (backupRoot), pas dans un sous-dossier utilisateur.

    [Fact]
    public void Lookup_FindsFile_AtDriveRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        var expected = Path.Combine(root, "NetworkDrives.txt");
        File.WriteAllText(expected, "G:\t\\\\srv\\share\tLabel");
        try
        {
            var found = NetworkDriveParser.FindFile(root);
            Assert.NotNull(found);
            Assert.Equal(expected, found);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Lookup_NotFoundInRoot_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        try
        {
            var found = NetworkDriveParser.FindFile(root);
            Assert.Null(found);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Lookup_DoesNotSearchSubfolders()
    {
        // S'assure que FindFile cherche uniquement à la racine, pas dans les sous-dossiers
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sub  = Path.Combine(root, "Users", "Quentin");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "NetworkDrives.txt"), "G:\t\\\\srv\\share\tLabel");
        try
        {
            // Le fichier est dans un sous-dossier -> FindFile (root-only) doit retourner null
            var found = NetworkDriveParser.FindFile(root);
            Assert.Null(found);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
