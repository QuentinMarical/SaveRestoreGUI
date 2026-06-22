using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class BackupValidationTests
{
    [Fact]
    public void IsValidBackupFolder_WithDocuments_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(dir, "Documents"));
        try
        {
            Assert.True(BackupValidator.IsValidBackupFolder(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void IsValidBackupFolder_WithNetworkDrivesTxt_ReturnsTrue()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "NetworkDrives.txt"), "Z:|\\\\s\\p|label");
        try
        {
            Assert.True(BackupValidator.IsValidBackupFolder(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void IsValidBackupFolder_EmptyDir_ReturnsFalse()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            Assert.False(BackupValidator.IsValidBackupFolder(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
