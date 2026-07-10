using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SaveRestoreGUI.Services
{
    /// <summary>
    /// Capture puis réapplication de l'état de fonctions Windows :
    ///  - Notifications (activées/désactivées)
    ///  - Historique du presse-papiers
    ///  - Plan et mode d'alimentation actifs
    ///  - Délais d'expiration de l'écran, de la veille et de la veille prolongée
    ///  - Actions des boutons d'alimentation/veille et du capot
    ///  - Seuil de l'économiseur de batterie
    /// Les valeurs individuelles sont lues via « powercfg /q » (aucun droit
    /// particulier requis) et réappliquées via /change et /setacvalueindex.
    /// L'export .pow complet du plan est tenté en bonus (droits administrateur).
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static class SystemStateService
    {
        private sealed class SystemState
        {
            public int?    ToastEnabled       { get; set; }   // 1 = notifications actives
            public int?    ClipboardHistory   { get; set; }   // 1 = historique actif
            public string? PowerSchemeGuid    { get; set; }
            public string? PowerSchemeName    { get; set; }
            public string? PowerOverlayGuid   { get; set; }   // mode d'alimentation Win11
            public bool    PowerPlanExported  { get; set; }

            // Secondes (0 = jamais) — CA = secteur, CC = batterie
            public int? MonitorTimeoutAc   { get; set; }
            public int? MonitorTimeoutDc   { get; set; }
            public int? StandbyTimeoutAc   { get; set; }
            public int? StandbyTimeoutDc   { get; set; }
            public int? HibernateTimeoutAc { get; set; }
            public int? HibernateTimeoutDc { get; set; }

            // Index d'action : 0=Ne rien faire 1=Veille 2=Veille prolongée 3=Arrêter
            public int? LidActionAc         { get; set; }
            public int? LidActionDc         { get; set; }
            public int? PowerButtonActionAc { get; set; }
            public int? PowerButtonActionDc { get; set; }
            public int? SleepButtonActionAc { get; set; }
            public int? SleepButtonActionDc { get; set; }

            // Économiseur de batterie : seuil d'activation (%)
            public int? BatterySaverThreshold { get; set; }
        }

        private const string PushKey      = @"Software\Microsoft\Windows\CurrentVersion\PushNotifications";
        private const string ClipboardKey = @"Software\Microsoft\Clipboard";

        // ─────────────────────────── Sauvegarde ───────────────────────────

        public static void Backup(string backupRoot, Action<string> info, Action<string> success, Action<string> warn)
        {
            var outDir = Path.Combine(backupRoot, "SystemState");
            Directory.CreateDirectory(outDir);
            var state = new SystemState();

            // ── Notifications & presse-papiers (registre)
            using (var k = Registry.CurrentUser.OpenSubKey(PushKey))
                state.ToastEnabled = k?.GetValue("ToastEnabled") as int? ?? 1;
            using (var k = Registry.CurrentUser.OpenSubKey(ClipboardKey))
                state.ClipboardHistory = k?.GetValue("EnableClipboardHistory") as int? ?? 0;

            info($"Notifications : {(state.ToastEnabled == 1 ? "activées" : "désactivées")} — " +
                 $"Historique presse-papiers : {(state.ClipboardHistory == 1 ? "activé" : "désactivé")}");

            // ── Plan d'alimentation actif
            var active = RunPowerCfg("/getactivescheme");
            var m = Regex.Match(active ?? "",
                @"([0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})\s*\(([^)]*)\)");
            if (m.Success)
            {
                state.PowerSchemeGuid = m.Groups[1].Value;
                state.PowerSchemeName = m.Groups[3].Value;
                info($"Plan d'alimentation : {state.PowerSchemeName}");
            }
            else warn("Plan d'alimentation actif non identifié.");

            // ── Valeurs individuelles (lisibles sans droits admin)
            (state.MonitorTimeoutAc,   state.MonitorTimeoutDc)   = QueryIndexes("SUB_VIDEO",  "VIDEOIDLE");
            (state.StandbyTimeoutAc,   state.StandbyTimeoutDc)   = QueryIndexes("SUB_SLEEP",  "STANDBYIDLE");
            (state.HibernateTimeoutAc, state.HibernateTimeoutDc) = QueryIndexes("SUB_SLEEP",  "HIBERNATEIDLE");
            (state.LidActionAc,         state.LidActionDc)         = QueryIndexes("SUB_BUTTONS", "LIDACTION");
            (state.PowerButtonActionAc, state.PowerButtonActionDc) = QueryIndexes("SUB_BUTTONS", "PBUTTONACTION");
            (state.SleepButtonActionAc, state.SleepButtonActionDc) = QueryIndexes("SUB_BUTTONS", "SBUTTONACTION");
            (state.BatterySaverThreshold, _) = QueryIndexes("SUB_ENERGYSAVER", "ESBATTTHRESHOLD");

            info($"Écran : {FmtDelay(state.MonitorTimeoutAc)} — Veille : {FmtDelay(state.StandbyTimeoutAc)} — " +
                 $"Veille prolongée : {FmtDelay(state.HibernateTimeoutAc)}");

            // ── Mode d'alimentation Win11 (superposition efficacité/équilibré/perfs)
            var overlay = RunPowerCfg("/getactiveoverlayscheme");
            var mo = Regex.Match(overlay ?? "", @"([0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})");
            if (mo.Success) state.PowerOverlayGuid = mo.Groups[1].Value;

            // ── Export .pow complet (nécessite les droits administrateur)
            if (!string.IsNullOrEmpty(state.PowerSchemeGuid))
            {
                var pow = Path.Combine(outDir, "PowerPlan.pow");
                RunPowerCfg($"/export \"{pow}\" {state.PowerSchemeGuid}");
                if (File.Exists(pow) && new FileInfo(pow).Length > 0)
                {
                    state.PowerPlanExported = true;
                    info("Plan complet exporté (.pow).");
                }
                else
                {
                    try { File.Delete(pow); } catch { }
                    info("Export .pow ignoré (droits administrateur requis) — les réglages individuels sont capturés.");
                }
            }

            File.WriteAllText(Path.Combine(outDir, "settings.json"),
                JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            success("État des fonctions Windows capturé (SystemState\\settings.json).");
        }

        // ─────────────────────────── Restauration ───────────────────────────

        public static void Restore(string restoreRoot, Action<string> info, Action<string> success, Action<string> warn)
        {
            var srcDir = Path.Combine(restoreRoot, "SystemState");
            var jsonPath = Path.Combine(srcDir, "settings.json");
            if (!File.Exists(jsonPath))
            {
                warn("Aucun état système (SystemState\\settings.json) dans la sauvegarde.");
                return;
            }

            var state = JsonSerializer.Deserialize<SystemState>(File.ReadAllText(jsonPath));
            if (state == null) { warn("settings.json illisible."); return; }

            // ── Notifications & presse-papiers
            if (state.ToastEnabled is int toast)
            {
                using var k = Registry.CurrentUser.CreateSubKey(PushKey);
                k.SetValue("ToastEnabled", toast, RegistryValueKind.DWord);
                info($"Notifications : {(toast == 1 ? "activées" : "désactivées")}");
            }
            if (state.ClipboardHistory is int clip)
            {
                using var k = Registry.CurrentUser.CreateSubKey(ClipboardKey);
                k.SetValue("EnableClipboardHistory", clip, RegistryValueKind.DWord);
                info($"Historique presse-papiers : {(clip == 1 ? "activé" : "désactivé")}");
            }

            // ── Plan d'alimentation : réactiver le plan d'origine (import .pow si absent)
            if (!string.IsNullOrEmpty(state.PowerSchemeGuid))
            {
                bool activated = RunPowerCfgOk($"/setactive {state.PowerSchemeGuid}");
                if (!activated && state.PowerPlanExported)
                {
                    var pow = Path.Combine(srcDir, "PowerPlan.pow");
                    if (File.Exists(pow))
                    {
                        info("Plan absent sur ce poste — import du plan sauvegardé...");
                        if (RunPowerCfgOk($"/import \"{pow}\" {state.PowerSchemeGuid}"))
                            activated = RunPowerCfgOk($"/setactive {state.PowerSchemeGuid}");
                    }
                }
                if (activated) info($"Plan d'alimentation actif : {state.PowerSchemeName}");
                else warn($"Plan « {state.PowerSchemeName} » non activé — réglages appliqués au plan courant.");
            }

            // ── Délais (minutes pour /change ; 0 = jamais)
            ChangeTimeout("monitor-timeout-ac",   state.MonitorTimeoutAc);
            ChangeTimeout("monitor-timeout-dc",   state.MonitorTimeoutDc);
            ChangeTimeout("standby-timeout-ac",   state.StandbyTimeoutAc);
            ChangeTimeout("standby-timeout-dc",   state.StandbyTimeoutDc);
            ChangeTimeout("hibernate-timeout-ac", state.HibernateTimeoutAc);
            ChangeTimeout("hibernate-timeout-dc", state.HibernateTimeoutDc);
            info($"Délais réappliqués — Écran : {FmtDelay(state.MonitorTimeoutAc)}, " +
                 $"Veille : {FmtDelay(state.StandbyTimeoutAc)}, " +
                 $"Veille prolongée : {FmtDelay(state.HibernateTimeoutAc)}");

            // ── Boutons / capot / économiseur (index sur le plan courant)
            bool buttonsOk = true;
            buttonsOk &= SetIndexes("SUB_BUTTONS", "LIDACTION",       state.LidActionAc,         state.LidActionDc);
            buttonsOk &= SetIndexes("SUB_BUTTONS", "PBUTTONACTION",   state.PowerButtonActionAc, state.PowerButtonActionDc);
            buttonsOk &= SetIndexes("SUB_BUTTONS", "SBUTTONACTION",   state.SleepButtonActionAc, state.SleepButtonActionDc);
            buttonsOk &= SetIndexes("SUB_ENERGYSAVER", "ESBATTTHRESHOLD", state.BatterySaverThreshold, state.BatterySaverThreshold);
            RunPowerCfg("/setactive SCHEME_CURRENT"); // applique les index modifiés
            if (buttonsOk) info("Boutons d'alimentation, capot et économiseur de batterie réappliqués.");
            else warn("Certains réglages boutons/capot n'ont pas pu être appliqués (droits administrateur ?).");

            // ── Mode d'alimentation (superposition Win11)
            if (!string.IsNullOrEmpty(state.PowerOverlayGuid) &&
                RunPowerCfgOk($"/overlaysetactive {state.PowerOverlayGuid}"))
                info("Mode d'alimentation (superposition) réappliqué.");

            success("État des fonctions Windows restauré.");
        }

        // ─────────────────────────── Helpers powercfg ───────────────────────────

        /// <summary>
        /// Lit les index CA/CC actuels d'un réglage via « powercfg /q ».
        /// Les deux dernières valeurs hexadécimales de la sortie sont, dans
        /// l'ordre, l'index CA (secteur) puis CC (batterie).
        /// </summary>
        private static (int? Ac, int? Dc) QueryIndexes(string subGroup, string setting)
        {
            var output = RunPowerCfg($"/q SCHEME_CURRENT {subGroup} {setting}");
            if (output == null) return (null, null);

            var matches = Regex.Matches(output, @"0x([0-9a-fA-F]{1,8})");
            if (matches.Count < 2) return (null, null);

            int ac = Convert.ToInt32(matches[^2].Groups[1].Value, 16);
            int dc = Convert.ToInt32(matches[^1].Groups[1].Value, 16);
            return (ac, dc);
        }

        /// <summary>Applique un délai via /change (valeur en minutes, 0 = jamais).</summary>
        private static void ChangeTimeout(string alias, int? seconds)
        {
            if (seconds is not int s) return;
            int minutes = s == 0 ? 0 : Math.Max(1, (int)Math.Round(s / 60.0));
            RunPowerCfg($"/change {alias} {minutes}");
        }

        private static bool SetIndexes(string subGroup, string setting, int? ac, int? dc)
        {
            bool ok = true;
            if (ac is int a) ok &= RunPowerCfgOk($"/setacvalueindex SCHEME_CURRENT {subGroup} {setting} {a}");
            if (dc is int d) ok &= RunPowerCfgOk($"/setdcvalueindex SCHEME_CURRENT {subGroup} {setting} {d}");
            return ok;
        }

        private static string FmtDelay(int? seconds) => seconds switch
        {
            null => "?",
            0    => "jamais",
            < 60 => $"{seconds} s",
            _    => $"{seconds / 60} min"
        };

        private static string? RunPowerCfg(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("powercfg.exe", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi)!;
                var output = p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit(30_000);
                return p.ExitCode == 0 ? output : null;
            }
            catch { return null; }
        }

        private static bool RunPowerCfgOk(string args) => RunPowerCfg(args) != null;
    }
}
