using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class FileServiceTests
{
    [Fact] public void CopyFile_CreatesDestination()
    {
        var src = Path.GetTempFileName();
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(src, "test");
        try { FileService.CopyFile(src, dst); Assert.True(File.Exists(dst)); }
        finally { File.Delete(src); if (File.Exists(dst)) File.Delete(dst); }
    }

    [Fact] public void CopyFile_PreservesContent()
    {
        var src = Path.GetTempFileName();
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(src, "hello world");
        try { FileService.CopyFile(src, dst); Assert.Equal("hello world", File.ReadAllText(dst)); }
        finally { File.Delete(src); if (File.Exists(dst)) File.Delete(dst); }
    }

    [Fact] public void CopyFile_OverwritesExisting()
    {
        var src = Path.GetTempFileName();
        var dst = Path.GetTempFileName();
        File.WriteAllText(src, "new content");
        File.WriteAllText(dst, "old content");
        try { FileService.CopyFile(src, dst); Assert.Equal("new content", File.ReadAllText(dst)); }
        finally { File.Delete(src); File.Delete(dst); }
    }

    [Theory]
    [InlineData("Documents")]
    [InlineData("Desktop")]
    public void IsKnownProfileFolder_ReturnsTrue(string folderName)
    {
        Assert.True(FileService.IsKnownProfileFolder(folderName));
    }

    [Fact] public void IsKnownProfileFolder_UnknownFolder_ReturnsFalse()
    {
        Assert.False(FileService.IsKnownProfileFolder("RandomFolder_" + Guid.NewGuid()));
    }

    [Fact] public void EnsureDirectoryExists_CreatesIfMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try { FileService.EnsureDirectoryExists(dir); Assert.True(Directory.Exists(dir)); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir); }
    }

    [Fact] public void EnsureDirectoryExists_DoesNotThrowIfExists()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try { var ex = Record.Exception(() => FileService.EnsureDirectoryExists(dir)); Assert.Null(ex); }
        finally { Directory.Delete(dir); }
    }

    [Fact] public void GetRelativePath_ReturnsCorrectRelativePath()
    {
        var basePath = @"C:\Users\test\backup";
        var fullPath = @"C:\Users\test\backup\Documents\file.txt";
        var result = FileService.GetRelativePath(basePath, fullPath);
        Assert.Equal(Path.Combine("Documents", "file.txt"), result);
    }

    [Fact] public void GetRelativePath_SameDirectory_ReturnsFileName()
    {
        var basePath = @"C:\Users\test\backup";
        var fullPath = @"C:\Users\test\backup\file.txt";
        var result = FileService.GetRelativePath(basePath, fullPath);
        Assert.Equal("file.txt", result);
    }

    [Fact] public void CopyDirectory_CopiesAllFiles()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "a.txt"), "A");
        File.WriteAllText(Path.Combine(src, "b.txt"), "B");
        try
        {
            FileService.CopyDirectory(src, dst);
            Assert.True(File.Exists(Path.Combine(dst, "a.txt")));
            Assert.True(File.Exists(Path.Combine(dst, "b.txt")));
        }
        finally
        {
            if (Directory.Exists(src)) Directory.Delete(src, true);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
        }
    }

    [Fact] public void CopyDirectory_CopiesSubdirectories()
    {
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(src, "sub"));
        File.WriteAllText(Path.Combine(src, "sub", "c.txt"), "C");
        try
        {
            FileService.CopyDirectory(src, dst);
            Assert.True(File.Exists(Path.Combine(dst, "sub", "c.txt")));
        }
        finally
        {
            if (Directory.Exists(src)) Directory.Delete(src, true);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
        }
    }

    [Fact] public void SafeDeleteFile_DeletesExistingFile()
    {
        var f = Path.GetTempFileName();
        FileService.SafeDeleteFile(f);
        Assert.False(File.Exists(f));
    }

    [Fact] public void SafeDeleteFile_DoesNotThrowIfMissing()
    {
        var f = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        var ex = Record.Exception(() => FileService.SafeDeleteFile(f));
        Assert.Null(ex);
    }

    [Fact] public void GetFileSizeBytes_ReturnsCorrectSize()
    {
        var f = Path.GetTempFileName();
        var content = "hello";
        File.WriteAllText(f, content, System.Text.Encoding.UTF8);
        try
        {
            var size = FileService.GetFileSizeBytes(f);
            Assert.True(size > 0);
        }
        finally { File.Delete(f); }
    }
}
