using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class NetworkDrivesTests
{
    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        File.WriteAllText(path, content);
        return path;
    }

    // ── ParseLines ────────────────────────────────────────────────────────────

    [Fact] public void ParseLines_ValidLine_ReturnsEntry()
    {
        var lines = new[] { @"Z:|\\server\share|MyLabel" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Single(entries);
        Assert.Equal("Z:", entries[0].Letter);
        Assert.Equal(@"\\server\share", entries[0].UncPath);
        Assert.Equal("MyLabel", entries[0].Label);
    }

    [Fact] public void ParseLines_MultipleLines_ReturnsAll()
    {
        var lines = new[]
        {
            @"Z:|\\server\share|Label1",
            @"Y:|\\server2\share2|Label2"
        };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Equal(2, entries.Count);
    }

    [Fact] public void ParseLines_EmptyInput_ReturnsEmpty()
    {
        var entries = NetworkDriveParser.ParseLines(Array.Empty<string>());
        Assert.Empty(entries);
    }

    [Fact] public void ParseLines_WhitespaceLines_AreIgnored()
    {
        var lines = new[] { "", "   ", @"Z:|\\server\share|Label", "" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Single(entries);
    }

    [Fact] public void ParseLines_MalformedLine_IsSkipped()
    {
        var lines = new[] { "malformed_no_pipe", @"Z:|\\server\share|Label" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Single(entries);
    }

    [Fact] public void ParseLines_TwoParts_IsSkipped()
    {
        // Seulement 2 colonnes (pas de label) → moins de 3 parts → ignoré
        var lines = new[] { @"Z:|\\server\share" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Empty(entries);
    }

    [Fact] public void ParseLines_EmptyLetter_IsSkipped()
    {
        var lines = new[] { @"|\\server\share|Label" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Empty(entries);
    }

    [Fact] public void ParseLines_EmptyUncPath_IsSkipped()
    {
        var lines = new[] { "Z:||Label" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Empty(entries);
    }

    // ── ParseFile ─────────────────────────────────────────────────────────────

    [Fact] public void ParseFile_ValidFile_ReturnsEntries()
    {
        var file = CreateTempFile(@"Z:|\\server\share|MyLabel");
        try
        {
            var entries = NetworkDriveParser.ParseFile(file);
            Assert.Single(entries);
            Assert.Equal("Z:", entries[0].Letter);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseFile_MissingFile_ReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        var entries = NetworkDriveParser.ParseFile(path);
        Assert.Empty(entries);
    }

    [Fact] public void ParseFile_EmptyFile_ReturnsEmpty()
    {
        var file = CreateTempFile("");
        try
        {
            var entries = NetworkDriveParser.ParseFile(file);
            Assert.Empty(entries);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseFile_MultipleLines_ReturnsAll()
    {
        var file = CreateTempFile("Z:|\\\\server\\share|Label1\nY:|\\\\server2\\share2|Label2");
        try
        {
            var entries = NetworkDriveParser.ParseFile(file);
            Assert.Equal(2, entries.Count);
        }
        finally { File.Delete(file); }
    }

    // ── DriveEntry record ─────────────────────────────────────────────────────

    [Fact] public void DriveEntry_Equality_SameValues()
    {
        var a = new NetworkDriveParser.DriveEntry("Z:", @"\\server\share", "Label");
        var b = new NetworkDriveParser.DriveEntry("Z:", @"\\server\share", "Label");
        Assert.Equal(a, b);
    }

    [Fact] public void DriveEntry_Properties_AreAccessible()
    {
        var entry = new NetworkDriveParser.DriveEntry("Z:", @"\\server\share", "MyLabel");
        Assert.Equal("Z:", entry.Letter);
        Assert.Equal(@"\\server\share", entry.UncPath);
        Assert.Equal("MyLabel", entry.Label);
    }
}
