using Microsoft.Win32;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

public class RegistryServiceTests
{
    private const string TestKeyPath = @"SOFTWARE\SaveRestoreGUI_Tests";

    private static void CleanupTestKey()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(TestKeyPath, throwOnMissingSubKey: false); }
        catch { /* ignore */ }
    }

    [Fact] public void ReadString_ExistingValue_ReturnsValue()
    {
        CleanupTestKey();
        using var key = Registry.CurrentUser.CreateSubKey(TestKeyPath);
        key.SetValue("TestVal", "hello");
        try
        {
            var result = RegistryService.ReadString(Registry.CurrentUser, TestKeyPath, "TestVal");
            Assert.Equal("hello", result);
        }
        finally { CleanupTestKey(); }
    }

    [Fact] public void ReadString_MissingValue_ReturnsNull()
    {
        CleanupTestKey();
        try
        {
            var result = RegistryService.ReadString(Registry.CurrentUser, TestKeyPath, "NoSuchValue");
            Assert.Null(result);
        }
        finally { CleanupTestKey(); }
    }

    [Fact] public void WriteString_ThenRead_RoundTrip()
    {
        CleanupTestKey();
        try
        {
            RegistryService.WriteString(Registry.CurrentUser, TestKeyPath, "RoundTrip", "data123");
            var result = RegistryService.ReadString(Registry.CurrentUser, TestKeyPath, "RoundTrip");
            Assert.Equal("data123", result);
        }
        finally { CleanupTestKey(); }
    }

    [Fact] public void DeleteValue_RemovesIt()
    {
        CleanupTestKey();
        using var key = Registry.CurrentUser.CreateSubKey(TestKeyPath);
        key.SetValue("ToDelete", "bye");
        try
        {
            RegistryService.DeleteValue(Registry.CurrentUser, TestKeyPath, "ToDelete");
            var result = RegistryService.ReadString(Registry.CurrentUser, TestKeyPath, "ToDelete");
            Assert.Null(result);
        }
        finally { CleanupTestKey(); }
    }

    [Fact] public void DeleteValue_MissingValue_DoesNotThrow()
    {
        CleanupTestKey();
        try
        {
            var ex = Record.Exception(() =>
                RegistryService.DeleteValue(Registry.CurrentUser, TestKeyPath, "Ghost"));
            Assert.Null(ex);
        }
        finally { CleanupTestKey(); }
    }

    [Fact] public void KeyExists_ExistingKey_ReturnsTrue()
    {
        CleanupTestKey();
        Registry.CurrentUser.CreateSubKey(TestKeyPath).Dispose();
        try
        {
            Assert.True(RegistryService.KeyExists(Registry.CurrentUser, TestKeyPath));
        }
        finally { CleanupTestKey(); }
    }

    [Fact] public void KeyExists_MissingKey_ReturnsFalse()
    {
        CleanupTestKey();
        Assert.False(RegistryService.KeyExists(Registry.CurrentUser, TestKeyPath));
    }

    [Fact] public void WriteString_OverwritesExistingValue()
    {
        CleanupTestKey();
        try
        {
            RegistryService.WriteString(Registry.CurrentUser, TestKeyPath, "Key", "first");
            RegistryService.WriteString(Registry.CurrentUser, TestKeyPath, "Key", "second");
            var result = RegistryService.ReadString(Registry.CurrentUser, TestKeyPath, "Key");
            Assert.Equal("second", result);
        }
        finally { CleanupTestKey(); }
    }
}
