using System.Diagnostics;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Service d'export / import de clés de registre (OneNote, OpenNotebook, profils et signatures Outlook).
    /// Reproduit les fonctionnalités des scripts Sauvegarde.ps1 / Restauration.ps1.
    /// </summary>
    public static class RegistryService
    {
        /// <summary>Versions d'Office sondées pour les clés Outlook/OneNote.</summary>
        private static readonly string[] OfficeVersions = { "12.0", "14.0", "15.0", "16.0" };
        private static readonly string[] SignatureVersions = { "12.0", "14.0", "15.0", "16.0", "17.0", "18.0" };

        /// <summary>Teste l'existence d'une clé HKCU via 'reg query'.</summary>
        private static bool RegKeyExists(string keyPath)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = $"query \"{keyPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                if (process == null) return false;
                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch { return false; }
        }

        /// <summary>Exporte une clé de registre vers un fichier .reg. Retourne true si réussi.</summary>
        public static bool ExportKey(string keyPath, string regFilePath)
        {
            try
            {
                if (!RegKeyExists(keyPath)) return false;
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = $"export \"{keyPath}\" \"{regFilePath}\" /y",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                if (process == null) return false;
                process.WaitForExit(10000);
                return process.ExitCode == 0 && File.Exists(regFilePath);
            }
            catch { return false; }
        }

        /// <summary>
        /// Importe un fichier .reg dans le registre. Retourne true si réussi.
        /// Le callback <paramref name="log"/> optionnel reçoit les messages de progression.
        /// </summary>
        public static bool ImportRegFile(string regFilePath, Action<string>? log = null)
        {
            try
            {
                if (!File.Exists(regFilePath))
                {
                    log?.Invoke($"Fichier introuvable : {regFilePath}");
                    return false;
                }
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = $"import \"{regFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                if (process == null)
                {
                    log?.Invoke("Impossible de démarrer reg.exe pour l'import.");
                    return false;
                }
                process.WaitForExit(10000);
                bool success = process.ExitCode == 0;
                if (!success)
                {
                    var err = process.StandardError.ReadToEnd().Trim();
                    log?.Invoke($"reg import a retourné ExitCode={process.ExitCode}{(string.IsNullOrEmpty(err) ? "" : $" : {err}")}");
                }
                return success;
            }
            catch (Exception ex)
            {
                log?.Invoke($"Exception ImportRegFile : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sauvegarde les clés OneNote (RecentNotebooks + User MRU) pour toutes les versions d'Office.
        /// Équivalent de la section "SAUVEGARDE CLÉS DE REGISTRE ONENOTE" de Sauvegarde.ps1.
        /// </summary>
        public static List<string> BackupOneNoteKeys(string backupRoot, Action<string> log)
        {
            var exported = new List<string>();
            bool foundAny = false;

            // Office 13.0 n'existe pas (Microsoft a sauté de 12.0 à 14.0) — on itère uniquement OfficeVersions.
            foreach (var version in OfficeVersions.OrderBy(v => v))
            {
                var oneNoteRoot = $"HKEY_CURRENT_USER\\Software\\Microsoft\\Office\\{version}\\OneNote";
                if (!RegKeyExists(oneNoteRoot)) continue;

                foundAny = true;
                var regFile = Path.Combine(backupRoot, $"OneNote_RecentNotes_{version.Replace(".", "_")}_OneNote.reg");
                bool foundOne = false;

                foreach (var sub in new[] { "RecentNotebooks", "User MRU" })
                {
                    var fullKey = $"{oneNoteRoot}\\{sub}";
                    if (!RegKeyExists(fullKey)) continue;

                    foundOne = true;
                    var tempFile = Path.Combine(backupRoot, $"temp_{version.Replace(".", "_")}_{sub.Replace(" ", "_")}.reg");
                    if (ExportKey(fullKey, tempFile))
                    {
                        if (!File.Exists(regFile))
                        {
                            File.Move(tempFile, regFile);
                        }
                        else
                        {
                            // Fusionner en sautant l'en-tête "Windows Registry Editor..."
                            var content = File.ReadAllLines(tempFile).Skip(1);
                            File.AppendAllLines(regFile, content);
                            File.Delete(tempFile);
                        }
                        log($"Clé OneNote exportée : {fullKey}");
                        if (!exported.Contains(regFile)) exported.Add(regFile);
                    }
                }

                if (!foundOne) log($"Aucune clé RecentNotebooks ni User MRU pour Office {version}.");
            }

            if (!foundAny) log("Aucune version de OneNote détectée sur ce profil.");
            return exported;
        }

        /// <summary>
        /// Sauvegarde la clé OpenNotebook (notes ouvertes OneNote 16.0).
        /// </summary>
        public static bool BackupOpenNotebookKey(string backupRoot, Action<string> log)
        {
            var key = "HKEY_CURRENT_USER\\Software\\Microsoft\\Office\\16.0\\OneNote\\OpenNotebook";
            var regFile = Path.Combine(backupRoot, "OpenNotebook.reg");
            if (ExportKey(key, regFile))
            {
                log($"Clé OpenNotebook exportée : {regFile}");
                return true;
            }
            log("Clé OpenNotebook non trouvée, saut de la sauvegarde.");
            return false;
        }

        /// <summary>
        /// Exporte les paramètres de signatures Outlook (MailSettings) pour toutes les versions d'Office.
        /// </summary>
        public static void BackupOutlookSignatureSettings(string backupRoot, Action<string> log)
        {
            foreach (var version in SignatureVersions)
            {
                var keyPath = $"HKEY_CURRENT_USER\\Software\\Microsoft\\Office\\{version}\\Common\\MailSettings";
                var regFile = Path.Combine(backupRoot, $"Outlook_Signature_Settings_{version}.reg");
                if (ExportKey(keyPath, regFile))
                    log($"Paramètres signatures Outlook {version} exportés.");
            }
        }

        /// <summary>
        /// Exporte les profils Outlook (registre) pour les versions d'Office détectées.
        /// </summary>
        public static void BackupOutlookProfiles(string outlookBackupDir, Action<string> log)
        {
            foreach (var version in new[] { "14.0", "15.0", "16.0" })
            {
                var keyPath = $"HKEY_CURRENT_USER\\Software\\Microsoft\\Office\\{version}\\Outlook";
                var regFile = Path.Combine(outlookBackupDir, $"Outlook_Profile_{version}.reg");
                if (ExportKey(keyPath, regFile))
                    log($"Profil Outlook {version} exporté.");
            }
        }

        /// <summary>
        /// Importe tous les fichiers .reg OneNote + OpenNotebook depuis le dossier de sauvegarde.
        /// Équivalent de la section "IMPORT DES CLÉS REGISTRE" de Restauration.ps1.
        /// </summary>
        public static void RestoreOneNoteKeys(string restoreRoot, Action<string> log)
        {
            var regFiles = Directory.GetFiles(restoreRoot, "OneNote_*.reg");
            foreach (var reg in regFiles)
            {
                if (ImportRegFile(reg, log))
                    log($"Clés importées : {Path.GetFileName(reg)}");
                else
                    log($"Échec import : {Path.GetFileName(reg)}");
            }

            var openNotebook = Path.Combine(restoreRoot, "OpenNotebook.reg");
            if (File.Exists(openNotebook))
            {
                if (ImportRegFile(openNotebook, log))
                    log("Clés OpenNotebook importées.");
                else
                    log("Échec import OpenNotebook.reg.");
            }

            if (regFiles.Length == 0 && !File.Exists(openNotebook))
                log("Aucune clé de registre OneNote à importer.");

            // Paramètres de signatures Outlook
            var sigFiles = Directory.GetFiles(restoreRoot, "Outlook_Signature_Settings_*.reg");
            foreach (var sig in sigFiles)
            {
                if (ImportRegFile(sig, log))
                    log($"Paramètres signatures importés : {Path.GetFileName(sig)}");
            }
        }
    }
}
