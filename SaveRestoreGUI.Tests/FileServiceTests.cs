using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class FileServiceTests
{
    // ── FormatSize ────────────────────────────────────────────────────────────

    [Fact] public void FormatSize_Bytes_ReturnsOctets()
    {
        var result = FileService.FormatSize(512);
        Assert.Contains("octets", result);
    }

    [Fact] public void FormatSize_Kilobytes_ReturnsKo()
    {
        var result = FileService.FormatSize(2048);
        Assert.Contains("Ko", result);
    }

    [Fact] public void FormatSize_Megabytes_ReturnsMo()
    {
        var result = FileService.FormatSize(2 * 1_048_576);
        Assert.Contains("Mo", result);
    }

    [Fact] public void FormatSize_Gigabytes_ReturnsGo()
    {
        var result = FileService.FormatSize(2L * 1_073_741_824);
        Assert.Contains("Go", result);
    }

    [Fact] public void FormatSize_Zero_ReturnsZeroOctets()
    {
        var result = FileService.FormatSize(0);
        Assert.Equal("0 octets", result);
    }

    // ── EnumerateFilesSafe ────────────────────────────────────────────────────

    [Fact] public void EnumerateFilesSafe_ListsAllFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "a.txt"), "A");
        File.WriteAllText(Path.Combine(dir, "b.txt"), "B");
        try
        {
            var files = FileService.EnumerateFilesSafe(dir).ToList();
            Assert.Equal(2, files.Count);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact] public void EnumerateFilesSafe_RecursesSubdirectories()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sub = Path.Combine(dir, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(dir, "root.txt"), "R");
        File.WriteAllText(Path.Combine(sub, "child.txt"), "C");
        try
        {
            var files = FileService.EnumerateFilesSafe(dir).ToList();
            Assert.Equal(2, files.Count);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact] public void EnumerateFilesSafe_EmptyDirectory_ReturnsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var files = FileService.EnumerateFilesSafe(dir).ToList();
            Assert.Empty(files);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact] public void EnumerateFilesSafe_MissingDirectory_ReturnsEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var files = FileService.EnumerateFilesSafe(dir).ToList();
        Assert.Empty(files);
    }

    // ── GetDirectorySize ──────────────────────────────────────────────────────

    [Fact] public void GetDirectorySize_ReturnsCorrectTotalSize()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        var content = "hello world";
        File.WriteAllText(Path.Combine(dir, "f.txt"), content, System.Text.Encoding.UTF8);
        try
        {
            var size = FileService.GetDirectorySize(dir);
            Assert.True(size > 0);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact] public void GetDirectorySize_EmptyDirectory_ReturnsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            Assert.Equal(0, FileService.GetDirectorySize(dir));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact] public void GetDirectorySize_MissingDirectory_ReturnsZero()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.Equal(0, FileService.GetDirectorySize(dir));
    }

    // ── CopyFolderAsync ───────────────────────────────────────────────────────

    [Fact] public async Task CopyFolderAsync_CopiesFiles()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "test.txt"), "content");
        try
        {
            var progress = new Progress<int>();
            var result = await FileService.CopyFolderAsync(
                src, dst, progress, null, CancellationToken.None);
            Assert.True(result.Copied > 0 || result.Skipped >= 0);
            Assert.True(File.Exists(Path.Combine(dst, "test.txt")));
        }
        finally
        {
            if (Directory.Exists(src)) Directory.Delete(src, true);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
        }
    }

    [Fact] public async Task CopyFolderAsync_MissingSource_ReturnsEmptyResult()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var progress = new Progress<int>();
        var result = await FileService.CopyFolderAsync(
            src, dst, progress, null, CancellationToken.None);
        Assert.Equal(0, result.Copied);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.TotalBytes);
    }

    [Fact] public async Task CopyFolderAsync_PreservesSubdirectories()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sub = Path.Combine(src, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "deep.txt"), "deep");
        try
        {
            var progress = new Progress<int>();
            await FileService.CopyFolderAsync(src, dst, progress, null, CancellationToken.None);
            Assert.True(File.Exists(Path.Combine(dst, "sub", "deep.txt")));
        }
        finally
        {
            if (Directory.Exists(src)) Directory.Delete(src, true);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
        }
    }

    [Fact] public async Task CopyFolderAsync_SkipsDesktopIni()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "desktop.ini"), "[.ShellClassInfo]");
        File.WriteAllText(Path.Combine(src, "real.txt"), "data");
        try
        {
            var progress = new Progress<int>();
            var result = await FileService.CopyFolderAsync(src, dst, progress, null, CancellationToken.None);
            Assert.False(File.Exists(Path.Combine(dst, "desktop.ini")));
            Assert.True(File.Exists(Path.Combine(dst, "real.txt")));
        }
        finally
        {
            if (Directory.Exists(src)) Directory.Delete(src, true);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
        }
    }
}
