using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class NetworkDrivesTests
{
    [Fact]
    public void ParseNetworkDrives_ValidLine_ReturnsEntry()
    {
        // Format: LETTRE|CHEMIN_UNC|LIBELLE
        var lines = new[] { "Z:|\\\\serveur\\partage|Documents Communs" };
        var entries = NetworkDriveParser.ParseLines(lines);

        Assert.Single(entries);
        Assert.Equal("Z:", entries[0].Letter);
        Assert.Equal("\\\\serveur\\partage", entries[0].UncPath);
        Assert.Equal("Documents Communs", entries[0].Label);
    }

    [Fact]
    public void ParseNetworkDrives_EmptyLine_Ignored()
    {
        var lines = new[] { "", "  ", "Z:|\\\\srv\\share|label" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Single(entries);
    }

    [Fact]
    public void ParseNetworkDrives_MalformedLine_Ignored()
    {
        var lines = new[] { "not_valid", "Z:|\\\\srv\\share|label" };
        var entries = NetworkDriveParser.ParseLines(lines);
        Assert.Single(entries);
    }
}
