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

        // Présence des données → pré-cocher uniquement si des données existent
        public bool HasWallpaper        { get; set; }
        public bool HasNetworkDrives    { get; set; }
        public bool HasOneNote          { get; set; }
        public bool HasSignatures       { get; set; }
        public bool HasOfficeTemplates  { get; set; }
        public bool HasExcelMacros      { get; set; }
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

            progress?.Invoke("Détection des données utilisateur...");
            DetectPresence(r);

            return r;
        }

        // ── OneDrive ──────────────────────────────────────────────────────────────
        private static void DetectOneDrive(AutoDetectResult r)
        {
            try
            {
                var oneDriveRoots = new List<string>();

                foreach (var subkey in new[] { "Personal", "Business1", "Business2" })
                {
                    using var key = Registry.CurrentUser.OpenSubKey(
                        $@"Software\Microsoft\OneDrive\Accounts\{subkey}");
                    var folder = key?.GetValue("UserFolder") as string;
                    if (!string.IsNullOrEmpty(folder)) oneDriveRoots.Add(folder!);
                }

                var envOneDrive = Environment.GetEnvironmentVariable("OneDrive");
                if (!string.IsNullOrEmpty(envOneDrive) && !oneDriveRoots.Contains(envOneDrive!))
                    oneDriveRoots.Add(envOneDrive!);

                if (oneDriveRoots.Count == 0) return;

                bool IsOnOneDrive(string knownFolder) =>
                    oneDriveRoots.Any(root =>
                        knownFolder.StartsWith(root, StringComparison.OrdinalIgnoreCase));

                r.DesktopOnOneDrive   = IsOnOneDrive(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
                r.DocumentsOnOneDrive = IsOnOneDrive(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                r.PicturesOnOneDrive  = IsOnOneDrive(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            }
            catch { }
        }

        // ── Logiciels ─────────────────────────────────────────────────────────────
        private static void DetectSoftware(AutoDetectResult r)
        {
            var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDataLocal   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            r.SapDetected = Directory.Exists(Path.Combine(appDataRoaming, "SAP"));

            r.IpSoftphoneDetected =
                Directory.Exists(Path.Combine(appDataRoaming,  "Alcatel-Lucent",    "IP Desktop Softphone")) ||
                Directory.Exists(Path.Combine(appDataLocal,    "Alcatel-Lucent",    "IP Desktop Softphone")) ||
                Directory.Exists(Path.Combine(appDataRoaming,  "ALE International", "IP Desktop Softphone")) ||
                Directory.Exists(Path.Combine(appDataLocal,    "ALE International", "IP Desktop Softphone"));

            r.OutlookDetected = OutlookService.FindPstFiles().Count > 0 ||
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office\16.0\Outlook\Profiles") != null ||
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office\15.0\Outlook\Profiles") != null;

            r.StickyNotesDetected = File.Exists(Path.Combine(
                appDataLocal,
                "Packages", "Microsoft.MicrosoftStickyNotes_8wekyb3d8bbwe",
                "LocalState", "plum.sqlite"));
        }

        // ── Présence des données (pour pré-cocher intelligemment) ─────────────────
        private static void DetectPresence(AutoDetectResult r)
        {
            try
            {
                var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // Fond d'écran personnalisé : la valeur WallPaper dans le registre
                // diffère du fond Windows par défaut uniquement si l'utilisateur en a choisi un.
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Control Panel\Desktop"))
                {
                    var wp = key?.GetValue("WallPaper") as string;
                    r.HasWallpaper = !string.IsNullOrEmpty(wp) &&
                        !wp!.Contains("Windows", StringComparison.OrdinalIgnoreCase);
                }

                // Lecteurs réseau : sous-clés de HKCU\Network
                using (var netKey = Registry.CurrentUser.OpenSubKey("Network"))
                    r.HasNetworkDrives = netKey != null && (netKey.GetSubKeyNames().Length > 0);

                // OneNote : dossiers de blocs-notes dans %AppData%\Microsoft\OneNote
                var oneNoteDir = Path.Combine(appDataRoaming, "Microsoft", "OneNote");
                r.HasOneNote = Directory.Exists(oneNoteDir) &&
                    Directory.EnumerateDirectories(oneNoteDir).Any();

                // Signatures Outlook : dossier non vide
                var sigDir = Path.Combine(appDataRoaming, "Microsoft", "Signatures");
                r.HasSignatures = Directory.Exists(sigDir) &&
                    Directory.EnumerateFiles(sigDir).Any();

                // Modèles Office : %AppData%\Microsoft\Templates non vide
                var tplDir = Path.Combine(appDataRoaming, "Microsoft", "Templates");
                r.HasOfficeTemplates = Directory.Exists(tplDir) &&
                    Directory.EnumerateFiles(tplDir, "*", SearchOption.AllDirectories).Any(f =>
                        !Path.GetFileName(f).Equals("Normal.dotm", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals("NormalEmail.dotm", StringComparison.OrdinalIgnoreCase));

                // Macros Excel : %AppData%\Microsoft\Excel\XLSTART non vide
                var xlStartDir = Path.Combine(appDataRoaming, "Microsoft", "Excel", "XLSTART");
                r.HasExcelMacros = Directory.Exists(xlStartDir) &&
                    Directory.EnumerateFiles(xlStartDir).Any();
            }
            catch { }
        }
    }
}
