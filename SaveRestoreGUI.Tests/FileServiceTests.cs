using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class FileServiceTests
{
    // ── FormatSize ──────────────────────────────────────────────────────────────

    [Fact]
    public void FormatSize_Bytes()
        => Assert.Equal("512 o", FileService.FormatSize(512));

    [Fact]
    public void FormatSize_Kilobytes()
        => Assert.Equal("1,0 Ko", FileService.FormatSize(1024));

    [Fact]
    public void FormatSize_Megabytes()
        => Assert.Equal("1,0 Mo", FileService.FormatSize(1024 * 1024));

    [Fact]
    public void FormatSize_Gigabytes()
        => Assert.Equal("1,0 Go", FileService.FormatSize(1024L * 1024 * 1024));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FormatSize_ZeroOrNegative_ReturnsNonNull(long size)
        => Assert.NotNull(FileService.FormatSize(size));

    [Fact]
    public void FormatSize_ExactlyOneTerabyte()
        => Assert.Equal("1,0 To", FileService.FormatSize(1024L * 1024 * 1024 * 1024));

    // ── CalculateFolderSizeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CalculateFolderSizeAsync_Returns_CorrectSize()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(dir, "a.txt"), new byte[100]);
            await File.WriteAllBytesAsync(Path.Combine(dir, "b.txt"), new byte[200]);

            long size = await FileService.CalculateFolderSizeAsync(dir);
            Assert.Equal(300L, size);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task CalculateFolderSizeAsync_Recursive_IncludesSubfolders()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sub = Path.Combine(dir, "sub");
        Directory.CreateDirectory(sub);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(dir, "root.txt"), new byte[50]);
            await File.WriteAllBytesAsync(Path.Combine(sub,  "child.txt"), new byte[75]);

            long size = await FileService.CalculateFolderSizeAsync(dir);
            Assert.Equal(125L, size);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task CalculateFolderSizeAsync_NonExistentDir_ReturnsZero()
    {
        long size = await FileService.CalculateFolderSizeAsync(
            Path.Combine(Path.GetTempPath(), "__notexist__"));
        Assert.Equal(0L, size);
    }

    [Fact]
    public async Task CalculateFolderSizeAsync_EmptyDir_ReturnsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            long size = await FileService.CalculateFolderSizeAsync(dir);
            Assert.Equal(0L, size);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── CopyFolderAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CopyFolderAsync_CopiesAllFiles()
    {
        var src  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(src);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(src, "file1.txt"), "hello");
            await File.WriteAllTextAsync(Path.Combine(src, "file2.txt"), "world");

            var result = await FileService.CopyFolderAsync(src, dest, null, null, default);

            Assert.Equal(2, result.Copied);
            Assert.Equal(0, result.Errors.Count);
            Assert.True(File.Exists(Path.Combine(dest, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(dest, "file2.txt")));
        }
        finally
        {
            if (Directory.Exists(src))  Directory.Delete(src,  recursive: true);
            if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true);
        }
    }

    [Fact]
    public async Task CopyFolderAsync_CopiesSubfolderRecursively()
    {
        var src  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sub  = Path.Combine(src, "subdir");
        Directory.CreateDirectory(sub);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(src, "root.txt"),  "r");
            await File.WriteAllTextAsync(Path.Combine(sub, "child.txt"), "c");

            var result = await FileService.CopyFolderAsync(src, dest, null, null, default);

            Assert.Equal(2, result.Copied);
            Assert.True(File.Exists(Path.Combine(dest, "subdir", "child.txt")));
        }
        finally
        {
            if (Directory.Exists(src))  Directory.Delete(src,  recursive: true);
            if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true);
        }
    }

    [Fact]
    public async Task CopyFolderAsync_SourceMissing_ReturnsNoCopied()
    {
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var result = await FileService.CopyFolderAsync(
            Path.Combine(Path.GetTempPath(), "__src_missing__"),
            dest, null, null, default);

        Assert.Equal(0, result.Copied);
        if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true);
    }

    [Fact]
    public async Task CopyFolderAsync_SkipsOlderFile_WhenDestIsNewer()
    {
        var src  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dest);
        try
        {
            var srcFile  = Path.Combine(src,  "file.txt");
            var destFile = Path.Combine(dest, "file.txt");

            await File.WriteAllTextAsync(srcFile,  "old");
            await File.WriteAllTextAsync(destFile, "new");

            // Rend la dest plus récente
            File.SetLastWriteTimeUtc(destFile, DateTime.UtcNow.AddHours(1));
            File.SetLastWriteTimeUtc(srcFile,  DateTime.UtcNow.AddHours(-1));

            var result = await FileService.CopyFolderAsync(src, dest, null, null, default);

            // Le fichier dest est plus récent → doit être ignoré (Skipped)
            Assert.Equal(0, result.Copied);
            Assert.Equal(1, result.Skipped);
            Assert.Equal("new", await File.ReadAllTextAsync(destFile));
        }
        finally
        {
            if (Directory.Exists(src))  Directory.Delete(src,  recursive: true);
            if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true);
        }
    }

    [Fact]
    public async Task CopyFolderAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var src  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(src);
        // Crée suffisamment de fichiers pour que l'annulation soit prise en compte
        for (int i = 0; i < 20; i++)
            await File.WriteAllBytesAsync(Path.Combine(src, $"f{i}.txt"), new byte[1024]);
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // annulation immédiate

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => FileService.CopyFolderAsync(src, dest, null, null, cts.Token));
        }
        finally
        {
            if (Directory.Exists(src))  Directory.Delete(src,  recursive: true);
            if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true);
        }
    }
}
