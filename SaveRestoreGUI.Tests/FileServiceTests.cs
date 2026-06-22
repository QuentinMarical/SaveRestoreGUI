using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class FileServiceTests
{
    [Fact]
    public void FormatSize_Bytes()
    {
        Assert.Equal("512 o", FileService.FormatSize(512));
    }

    [Fact]
    public void FormatSize_Kilobytes()
    {
        Assert.Equal("1,0 Ko", FileService.FormatSize(1024));
    }

    [Fact]
    public void FormatSize_Megabytes()
    {
        Assert.Equal("1,0 Mo", FileService.FormatSize(1024 * 1024));
    }

    [Fact]
    public void FormatSize_Gigabytes()
    {
        Assert.Equal("1,0 Go", FileService.FormatSize(1024L * 1024 * 1024));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FormatSize_ZeroOrNegative_ReturnsZeroBytes(long size)
    {
        var result = FileService.FormatSize(size);
        Assert.NotNull(result);
    }

    // ── Calcul de taille dossier ────────────────────────────────────────────────
    [Fact]
    public async Task CalculateFolderSizeAsync_Returns_CorrectSize()
    {
        // Crée un dossier temp avec 2 fichiers
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(dir, "a.txt"), new byte[100]);
            await File.WriteAllBytesAsync(Path.Combine(dir, "b.txt"), new byte[200]);

            long size = await FileService.CalculateFolderSizeAsync(dir);
            Assert.Equal(300L, size);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task CalculateFolderSizeAsync_NonExistentDir_ReturnsZero()
    {
        long size = await FileService.CalculateFolderSizeAsync(Path.Combine(Path.GetTempPath(), "__notexist__"));
        Assert.Equal(0L, size);
    }
}
