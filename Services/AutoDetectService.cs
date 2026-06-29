using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Résultat de l'auto-détection au démarrage.
    /// </summary>
    public class AutoDetectResult
    {
        // Dossiers synchronisés OneDrive → décocher la case correspondante
        public bool DesktopOnOneDrive   { get; set; }
        public bool DocumentsOnOneDrive { get; set; }
        public bool PicturesOnOneDrive  { get; set; }

        // Logiciels détectés → cocher la case
        public bool SapDetected          { get; set; }
        public bool IpSoftphoneDetected  { get; set; }
        public bool OutlookDetected      { get; set; }
        public bool StickyNotesDetected  { get; set; }
    }

    public static class AutoDetectService
    {
        public static AutoDetectResult Detect(Action<string>? progress = null)
        {
            var r = new AutoDetectResult();

            progress?.Invoke("Détection de la synchronisation OneDrive...");
            DetectOneDrive(r);

            progress?.Invoke("Détection des logiciels installés...");
            DetectSoftware(r);

            return r;
        }

        // ── OneDrive ──────────────────────────────────────────────────────────────
        // OneDrive redirige Desktop/Documents/Pictures via le registre :
        //   HKCU\Software\Microsoft\OneDrive\Accounts\Personal  →  UserFolder
        // et les dossiers connus pointent vers le dossier OneDrive.
        private static void DetectOneDrive(AutoDetectResult r)
        {
            try
            {
                // Récupère le chemin racine OneDrive (Personal ou Business)
                var oneDriveRoots = new List<string>();

                foreach (var subkey in new[] { "Personal", "Business1", "Business2" })
                {
                    using var key = Registry.CurrentUser.OpenSubKey(
                        $@"Software\Microsoft\OneDrive\Accounts\{subkey}");
                    var folder = key?.GetValue("UserFolder") as string;
                    if (!string.IsNullOrEmpty(folder)) oneDriveRoots.Add(folder!);
                }

                // Fallback : variable d'environnement
                var envOneDrive = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrEmpty(envOneDrive) && !oneDriveRoots.Contains(envOneDrive!))
                    oneDriveRoots.Add(envOneDrive!);

                if (oneDriveRoots.Count == 0) return;

                // Vérifie si les dossiers connus pointent DANS un dossier OneDrive
                bool IsOnOneDrive(string knownFolder) =>
                    oneDriveRoots.Any(root =>
                        knownFolder.StartsWith(root, StringComparison.OrdinalIgnoreCase));

                r.DesktopOnOneDrive   = IsOnOneDrive(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                r.DocumentsOnOneDrive = IsOnOneDrive(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                r.PicturesOnOneDrive  = IsOnOneDrive(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            }
            catch { /* ne pas planter au démarrage */ }
        }

        // ── Logiciels ─────────────────────────────────────────────────────────────
        private static void DetectSoftware(AutoDetectResult r)
        {
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDataLocal   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // SAP : données présentes dans %AppData%\SAP
            r.SapDetected = Directory.Exists(Path.Combine(appDataRoaming, "SAP"));

            // IP Desktop Softphone : Alcatel-Lucent ou ALE International
            r.IpSoftphoneDetected =
                Directory.Exists(Path.Combine(appDataRoaming,  "Alcatel-Lucent",  "IP Desktop Softphone")) ||
                Directory.Exists(Path.Combine(appDataLocal,    "Alcatel-Lucent",  "IP Desktop Softphone")) ||
                Directory.Exists(Path.Combine(appDataRoaming,  "ALE International", "IP Desktop Softphone")) ||
                Directory.Exists(Path.Combine(appDataLocal,    "ALE International", "IP Desktop Softphone"));

            // Outlook : profils dans le registre
            r.OutlookDetected = OutlookService.FindPstFiles().Count > 0 ||
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Office\16.0\Outlook\Profiles") != null ||
                Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Office\15.0\Outlook\Profiles") != null;

            // Sticky Notes : base SQLite présente
            r.StickyNotesDetected = File.Exists(Path.Combine(
                appDataLocal,
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe",
                "LocalState", "plum.sqlite"));
        }
    }
}
