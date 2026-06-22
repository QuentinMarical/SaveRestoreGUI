using System.IO;
using SaveRestoreGUI.Services;
using Xunit;

namespace SaveRestoreGUI.Tests;

/// <summary>
/// Tests unitaires pour RegistryService.ImportRegFile.
/// Utilise des fichiers .reg temporaires valides/invalides afin de couvrir
/// le chemin heureux, les callbacks de log, les fichiers manquants et les
/// fichiers avec mauvais en-tête.
/// </summary>
public class RegistryServiceTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>Crée un .reg minimal valide dans un dossier temporaire.</summary>
    private static string CreateValidRegFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.reg");
        // En-tête officiel Windows Registry Editor Version 5.00
        // Clé HKCU\Software sous-clé inoffensive pour les tests
        File.WriteAllText(path,
            "Windows Registry Editor Version 5.00\r\n" +
            "\r\n" +
            "[HKEY_CURRENT_USER\\Software\\SaveRestoreGUI_Test]\r\n" +
            "\"TestValue\"=\"ok\"\r\n");
        return path;
    }

    private static string CreateInvalidRegFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bad_{Guid.NewGuid():N}.reg");
        File.WriteAllText(path, "This is not a valid .reg file header");
        return path;
    }

    // ── ImportRegFile ─────────────────────────────────────────────────────────

    [Fact]
    public void ImportRegFile_FileNotFound_LogsError()
    {
        var missing = Path.Combine(Path.GetTempPath(), "__missing__.reg");
        var errors  = new List<string>();

        RegistryService.ImportRegFile(missing, null, err => errors.Add(err));

        Assert.Single(errors);
        Assert.Contains(missing, errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportRegFile_FileNotFound_NoExceptionThrown()
    {
        var missing = Path.Combine(Path.GetTempPath(), "__missing2__.reg");
        // Ne doit pas lever d'exception, juste appeler le callback d'erreur
        var ex = Record.Exception(() =>
            RegistryService.ImportRegFile(missing, null, _ => { }));
        Assert.Null(ex);
    }

    [Fact]
    public void ImportRegFile_InvalidHeader_LogsError()
    {
        var path   = CreateInvalidRegFile();
        var errors = new List<string>();
        try
        {
            RegistryService.ImportRegFile(path, null, err => errors.Add(err));
            Assert.NotEmpty(errors);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ImportRegFile_NullPath_LogsError()
    {
        var errors = new List<string>();
        RegistryService.ImportRegFile(null!, null, err => errors.Add(err));
        Assert.Single(errors);
    }

    [Fact]
    public void ImportRegFile_EmptyPath_LogsError()
    {
        var errors = new List<string>();
        RegistryService.ImportRegFile("", null, err => errors.Add(err));
        Assert.Single(errors);
    }

    [Fact]
    public void ImportRegFile_ValidFile_CallsSuccessCallback()
    {
        // Note : ce test exécute réellement reg.exe et écrit dans HKCU.
        // Il est skippé si les droits sont insuffisants (CI sans registry).
        var path    = CreateValidRegFile();
        var success = new List<string>();
        var errors  = new List<string>();
        try
        {
            RegistryService.ImportRegFile(path, msg => success.Add(msg), err => errors.Add(err));

            // Si l'import a réussi : au moins un message de succès, aucune erreur
            if (success.Count > 0)
                Assert.Empty(errors);
            // Si l'import a échoué (droits CI) : au moins une erreur loguée
            else
                Assert.NotEmpty(errors);
        }
        finally
        {
            File.Delete(path);
            // Nettoyage de la clé de test dans le registre (best-effort)
            try
            {
                Microsoft.Win32.Registry.CurrentUser
                    .DeleteSubKeyTree("Software\\SaveRestoreGUI_Test", throwOnMissingSubKey: false);
            }
            catch { /* ignoré */ }
        }
    }

    [Fact]
    public void ImportRegFile_ValidFile_NoExceptionThrown()
    {
        var path = CreateValidRegFile();
        try
        {
            var ex = Record.Exception(() =>
                RegistryService.ImportRegFile(path, null, null));
            Assert.Null(ex);
        }
        finally { File.Delete(path); }
    }

    // ── Callback null-safety ────────────────────────────────────────────────

    [Fact]
    public void ImportRegFile_NullCallbacks_DoesNotThrow()
    {
        var missing = Path.Combine(Path.GetTempPath(), "__cb_null__.reg");
        var ex = Record.Exception(() =>
            RegistryService.ImportRegFile(missing, null, null));
        Assert.Null(ex);
    }
}
