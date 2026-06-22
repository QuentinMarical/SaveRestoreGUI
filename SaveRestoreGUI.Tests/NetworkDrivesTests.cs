using System.IO;
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

    [Fact] public void ParseNetworkDrivesFile_ValidLine_ReturnsEntry()
    {
        var file = CreateTempFile("Z:|\\\\server\\share|MyLabel");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Single(entries);
            Assert.Equal("Z:", entries[0].DriveLetter);
            Assert.Equal("\\\\server\\share", entries[0].UncPath);
            Assert.Equal("MyLabel", entries[0].Label);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseNetworkDrivesFile_MultipleLines_ReturnsAll()
    {
        var file = CreateTempFile("Z:|\\\\server\\share|Label1\nY:|\\\\server2\\share2|Label2");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Equal(2, entries.Count);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseNetworkDrivesFile_EmptyFile_ReturnsEmpty()
    {
        var file = CreateTempFile("");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Empty(entries);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseNetworkDrivesFile_CommentLines_AreIgnored()
    {
        var file = CreateTempFile("# commentaire\nZ:|\\\\server\\share|Label");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Single(entries);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseNetworkDrivesFile_MalformedLine_IsSkipped()
    {
        var file = CreateTempFile("ligne_malformee_sans_pipe\nZ:|\\\\server\\share|Label");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Single(entries);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void SerializeNetworkDrivesFile_WritesCorrectFormat()
    {
        var entries = new System.Collections.Generic.List<NetworkDriveEntry>
        {
            new() { DriveLetter = "Z:", UncPath = "\\\\server\\share", Label = "MyLabel" }
        };
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            NetworkDrivesService.SerializeNetworkDrivesFile(file, entries);
            var content = File.ReadAllText(file);
            Assert.Contains("Z:", content);
            Assert.Contains("\\\\server\\share", content);
            Assert.Contains("MyLabel", content);
        }
        finally { if (File.Exists(file)) File.Delete(file); }
    }

    [Fact] public void RoundTrip_ParseAndSerialize_PreservesData()
    {
        var original = "Z:|\\\\server\\share|MyLabel";
        var file = CreateTempFile(original);
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            var outFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            NetworkDrivesService.SerializeNetworkDrivesFile(outFile, entries);
            var reparsed = NetworkDrivesService.ParseNetworkDrivesFile(outFile);
            Assert.Single(reparsed);
            Assert.Equal(entries[0].DriveLetter, reparsed[0].DriveLetter);
            Assert.Equal(entries[0].UncPath, reparsed[0].UncPath);
            Assert.Equal(entries[0].Label, reparsed[0].Label);
            if (File.Exists(outFile)) File.Delete(outFile);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseNetworkDrivesFile_WhitespaceLines_AreIgnored()
    {
        var file = CreateTempFile("\n   \nZ:|\\\\server\\share|Label\n\n");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Single(entries);
        }
        finally { File.Delete(file); }
    }

    [Fact] public void ParseNetworkDrivesFile_FileNotFound_ThrowsOrReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        var ex = Record.Exception(() => NetworkDrivesService.ParseNetworkDrivesFile(path));
        Assert.True(ex is null || ex is FileNotFoundException || ex is DirectoryNotFoundException);
    }

    [Fact] public void ParseNetworkDrivesFile_LabelOptional_ParsesWithoutLabel()
    {
        var file = CreateTempFile("Z:|\\\\server\\share");
        try
        {
            var entries = NetworkDrivesService.ParseNetworkDrivesFile(file);
            Assert.Single(entries);
            Assert.Equal("Z:", entries[0].DriveLetter);
        }
        finally { File.Delete(file); }
    }
}
