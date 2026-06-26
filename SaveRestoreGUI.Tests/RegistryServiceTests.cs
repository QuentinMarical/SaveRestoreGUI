using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

/// <summary>
/// Tests pour RegistryService. Les méthodes publiques (ExportKey, ImportRegFile,
/// BackupOneNoteKeys, etc.) appellent reg.exe — on teste uniquement les chemins
/// sûrs qui ne modifient pas le registre de la machine.
/// </summary>
public class RegistryServiceTests
{
    // ── ExportKey ─────────────────────────────────────────────────────────────

    [Fact] public void ExportKey_NonExistentKey_ReturnsFalse()
    {
        var regFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".reg");
        try
        {
            // Clé inexistante → doit retourner false sans lever d'exception
            var result = RegistryService.ExportKey(
                @"HKEY_CURRENT_USER\Software\SaveRestoreGUI_NonExistentKey_" + Guid.NewGuid(),
                regFile);
            Assert.False(result);
        }
        finally { if (File.Exists(regFile)) File.Delete(regFile); }
    }

    [Fact] public void ExportKey_DoesNotThrow_WhenKeyMissing()
    {
        var regFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".reg");
        try
        {
            var ex = Record.Exception(() =>
                RegistryService.ExportKey(
                    @"HKEY_CURRENT_USER\Software\SaveRestoreGUI_Ghost_" + Guid.NewGuid(),
                    regFile));
            Assert.Null(ex);
        }
        finally { if (File.Exists(regFile)) File.Delete(regFile); }
    }

    // ── ImportRegFile ─────────────────────────────────────────────────────────

    [Fact] public void ImportRegFile_MissingFile_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".reg");
        var logs = new List<string>();
        var result = RegistryService.ImportRegFile(path, logs.Add);
        Assert.False(result);
    }

    [Fact] public void ImportRegFile_MissingFile_LogsMessage()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".reg");
        var logs = new List<string>();
        RegistryService.ImportRegFile(path, logs.Add);
        Assert.NotEmpty(logs);
    }

    [Fact] public void ImportRegFile_DoesNotThrow_WhenFileMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".reg");
        var ex = Record.Exception(() => RegistryService.ImportRegFile(path));
        Assert.Null(ex);
    }

    // ── BackupOneNoteKeys ─────────────────────────────────────────────────────

    [Fact] public void BackupOneNoteKeys_DoesNotThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var ex = Record.Exception(() =>
                RegistryService.BackupOneNoteKeys(dir, _ => { }));
            Assert.Null(ex);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact] public void BackupOneNoteKeys_ReturnsListWithoutThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var result = RegistryService.BackupOneNoteKeys(dir, _ => { });
            Assert.NotNull(result);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── BackupOpenNotebookKey ─────────────────────────────────────────────────

    [Fact] public void BackupOpenNotebookKey_DoesNotThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var ex = Record.Exception(() =>
                RegistryService.BackupOpenNotebookKey(dir, _ => { }));
            Assert.Null(ex);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // ── RestoreOneNoteKeys ────────────────────────────────────────────────────

    [Fact] public void RestoreOneNoteKeys_EmptyDir_DoesNotThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            var ex = Record.Exception(() =>
                RegistryService.RestoreOneNoteKeys(dir, _ => { }));
            Assert.Null(ex);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
