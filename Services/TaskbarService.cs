using System.Diagnostics;
using Microsoft.Win32;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Sauvegarde/restauration de la configuration de la barre des tâches :
    /// bouton Rechercher (masqué/icône/zone), Vue Tâches, Reprendre, alignement
    /// (centré/gauche), masquage automatique, badges, combinaison des boutons,
    /// multi-écrans… Les clés registre concernées sont exportées intégralement
    /// (reg export) pour couvrir aussi les options futures, puis réimportées à
    /// la restauration avec redémarrage d'Explorer pour application immédiate.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static class TaskbarService
    {
        private static readonly (string RegKey, string File)[] Keys =
        [
            // Alignement (TaskbarAl), Vue Tâches (ShowTaskViewButton), Reprendre,
            // badges/flash, combinaison des boutons (TaskbarGlomLevel), tailles…
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",    "Taskbar-Advanced.reg"),
            // Mode du bouton Rechercher (SearchboxTaskbarMode : 0..3)
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Search",               "Taskbar-Search.reg"),
            // Position / masquage automatique (structure binaire)
            (@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3", "Taskbar-StuckRects3.reg"),
        ];

        // ─────────────────────────── Sauvegarde ───────────────────────────

        public static void Backup(string backupRoot, Action<string> info, Action<string> success, Action<string> warn)
        {
            var outDir = Path.Combine(backupRoot, "Taskbar");
            Directory.CreateDirectory(outDir);

            int exported = 0;
            foreach (var (regKey, file) in Keys)
            {
                var dest = Path.Combine(outDir, file);
                if (RunReg($"export \"{regKey}\" \"{dest}\" /y") && File.Exists(dest)) exported++;
                else warn($"Export impossible : {regKey}");
            }

            if (exported == 0) { warn("Aucune clé de barre des tâches exportée."); return; }

            // Résumé lisible des réglages principaux (diagnostic / vérification)
            File.WriteAllLines(Path.Combine(outDir, "Resume.txt"), DescribeCurrentSettings(),
                System.Text.Encoding.UTF8);
            success($"Barre des tâches : {exported}/{Keys.Length} clé(s) exportée(s) — {string.Join(", ", DescribeCurrentSettings())}");
        }

        // ─────────────────────────── Restauration ───────────────────────────

        public static void Restore(string restoreRoot, Action<string> info, Action<string> success, Action<string> warn)
        {
            var srcDir = Path.Combine(restoreRoot, "Taskbar");
            if (!Directory.Exists(srcDir))
            {
                warn("Aucun dossier Taskbar dans la sauvegarde.");
                return;
            }

            int imported = 0;
            foreach (var (regKey, file) in Keys)
            {
                var reg = Path.Combine(srcDir, file);
                if (!File.Exists(reg)) continue;
                if (RunReg($"import \"{reg}\"")) imported++;
                else warn($"Import impossible : {file}");
            }

            if (imported == 0) { warn("Aucun fichier .reg de barre des tâches trouvé."); return; }

            // Redémarrage d'Explorer requis pour appliquer alignement/StuckRects
            info("Redémarrage de l'Explorateur Windows pour appliquer les réglages...");
            RestartExplorer();
            success($"Barre des tâches restaurée ({imported} clé(s) réimportée(s)).");
        }

        // ─────────────────────────── Helpers ───────────────────────────

        private static bool RunReg(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("reg.exe", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi)!;
                p.WaitForExit(30_000);
                return p.ExitCode == 0;
            }
            catch { return false; }
        }

        private static void RestartExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                { try { p.Kill(); } catch { } }
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            }
            catch { /* Explorer redémarre de lui-même si tué */ }
        }

        /// <summary>Lecture des réglages actuels pour le résumé de sauvegarde.</summary>
        private static IEnumerable<string> DescribeCurrentSettings()
        {
            using var adv = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
            using var search = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Search");

            yield return "Alignement=" + ((adv?.GetValue("TaskbarAl") as int? ?? 1) == 0 ? "Gauche" : "Centré");
            yield return "Recherche=" + ((search?.GetValue("SearchboxTaskbarMode") as int? ?? 2) switch
            {
                0 => "Masquer", 1 => "Icône", 2 => "Zone de recherche", 3 => "Icône et étiquette",
                var v => $"Mode {v}"
            });
            yield return "VueTâches=" + ((adv?.GetValue("ShowTaskViewButton") as int? ?? 1) == 1 ? "Activé" : "Désactivé");
            yield return "CombinerBoutons=" + ((adv?.GetValue("TaskbarGlomLevel") as int? ?? 0) switch
            {
                0 => "Toujours", 1 => "Barre pleine", _ => "Jamais"
            });
        }
    }
}
