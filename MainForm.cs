using System.Reflection;
using System.Text;
using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Fenêtre principale — reçoit les résultats pré-calculés par Program.cs
    /// (navigateurs détectés, auto-détection OneDrive/logiciels).
    /// </summary>
    public partial class MainForm : Form
    {
        // Un CancellationTokenSource distinct par onglet pour éviter les race conditions
        private CancellationTokenSource? _ctsBackup;
        private CancellationTokenSource? _ctsRestore;
        private CancellationTokenSource? _ctsMigration;

        /// <summary>Profils système exclus de toutes les détections de profils.</summary>
        internal static readonly string[] ExcludedProfiles =
            ["Public", "Default", "Default User", "All Users", "defaultuser0"];

        private string? _logFilePath;
        private readonly Lock _logLock = new();

        // Résultats passés par Program.cs (pré-calculés pendant le splash)
        private readonly IReadOnlyList<BrowserEntry> _browserEntries;
        private readonly AutoDetectResult _autoDetect;

        // Fenêtres de logs flottantes (une par onglet)
        private readonly LogWindow _logWindowBackup;
        private readonly LogWindow _logWindowRestore;
        private readonly LogWindow _logWindowMigration;

        public MainForm(
            IReadOnlyList<BrowserEntry> browserEntries,
            AutoDetectResult autoDetect)
        {
            _browserEntries = browserEntries;
            _autoDetect     = autoDetect;

            // Créer les LogWindows avant InitializeComponent (Designer les référence)
            _logWindowBackup    = new LogWindow("Logs — Sauvegarde",    "backup-log.txt");
            _logWindowRestore   = new LogWindow("Logs — Restauration",  "restore-log.txt");
            _logWindowMigration = new LogWindow("Logs — Migration USB", "migration-log.txt");

            InitializeComponent();

            // Peupler les panneaux de cases à cocher
            chkPanelBackup.SetCategories(
                CheckCatalog.Build(includeOldProfile: true, includeLaunchApps: false));
            chkPanelRestore.SetCategories(
                CheckCatalog.Build(includeOldProfile: false, includeLaunchApps: false));
            chkPanelMigration.SetCategories(
                CheckCatalog.Build(includeOldProfile: true, includeLaunchApps: false));

            this.Load += (_, _) =>
            {
                SyncPageSizes();
                ApplyAutoDetect();
            };
            // Recalcule les pages au retour de minimisation (la taille de fenêtre
            // est fixe par ailleurs : FixedSingle, bounds posés au démarrage).
            this.Resize += (_, _) => SyncPageSizes();

            ApplyTheme();
            UpdateOldProfileOptionState();
            LoadUSBDrives();
            ShowPage(0);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionStr = version != null
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : "?";
            this.Text = $"SaveRestoreGUI v{versionStr}";
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            FitToWorkingArea();
            NativeMethods.ApplyWin11WindowStyle(Handle);
        }

        /// <summary>
        /// Cale la fenêtre (fixe, non redimensionnable) exactement sur la zone de
        /// travail de l'écran courant — remplace l'état Maximized, dont le rendu
        /// est imprévisible quand WS_MAXIMIZEBOX est absent (MaximizeBox = false).
        /// </summary>
        private void FitToWorkingArea()
        {
            Bounds = Screen.FromControl(this).WorkingArea;
        }

        private void SyncPageSizes()
        {
            var size = contentPanel.ClientSize;

            pageBackup.Bounds    = new Rectangle(0, 0, size.Width, size.Height);
            pageRestore.Bounds   = new Rectangle(0, 0, size.Width, size.Height);
            pageMigration.Bounds = new Rectangle(0, 0, size.Width, size.Height);

            ApplyResponsiveLayout();
        }

        private void ApplyAutoDetect()
        {
            chkPanelBackup.ApplyAutoDetect(_autoDetect);
            chkPanelRestore.ApplyAutoDetect(_autoDetect);
            chkPanelMigration.ApplyAutoDetect(_autoDetect);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _logWindowBackup.CloseIfOpen();
            _logWindowRestore.CloseIfOpen();
            _logWindowMigration.CloseIfOpen();
            base.OnFormClosed(e);
        }

        // ───────────────────────────── Navigation ─────────────────────────────

        private void ShowPage(int index)
        {
            // Fermer les fenêtres de log des onglets non-actifs
            if (index != 0) _logWindowBackup.CloseIfOpen();
            if (index != 1) _logWindowRestore.CloseIfOpen();
            if (index != 2) _logWindowMigration.CloseIfOpen();

            pageBackup.Visible    = index == 0;
            pageRestore.Visible   = index == 1;
            pageMigration.Visible = index == 2;

            navBackup.Selected    = index == 0;
            navRestore.Selected   = index == 1;
            navMigration.Selected = index == 2;
            navBackup.Invalidate();
            navRestore.Invalidate();
            navMigration.Invalidate();

            lblPageTitle.Text = index switch
            {
                0 => "Sauvegarde",
                1 => "Restauration",
                2 => "Migration USB",
                _ => ""
            };
            lblPageSubtitle.Text = index switch
            {
                0 => "Sauvegardez vos données utilisateur vers un dossier de destination",
                1 => "Restaurez vos données depuis une sauvegarde existante",
                2 => "Migrez les données d'un profil depuis un disque externe",
                _ => ""
            };

            ApplyResponsiveLayout();
        }

        // Ouvre (ou met au premier plan) la LogWindow de l'onglet courant
        private void OpenLogWindow(int index)
        {
            var win = index switch
            {
                0 => _logWindowBackup,
                1 => _logWindowRestore,
                2 => _logWindowMigration,
                _ => _logWindowBackup
            };

            if (win.Visible)
            {
                win.BringToFront();
                return;
            }

            win.PositionNear(this);
            win.Show(this);
        }

        // Accesseur utilisé par les fichiers Backup/Restore/Migration
        internal RichTextBox BackupLogBox    => _logWindowBackup.LogBox;
        internal RichTextBox RestoreLogBox   => _logWindowRestore.LogBox;
        internal RichTextBox MigrationLogBox => _logWindowMigration.LogBox;

        // ───────────────────────────── Thème ──────────────────────────────

        private void ApplyTheme()
        {
            var p = ThemeManager.Palette;

            BackColor              = p.Background;
            sidebarPanel.BackColor = p.Sidebar;
            contentPanel.BackColor = p.Background;
            headerPanel.BackColor  = p.Background;
            statusPanel.BackColor  = p.Sidebar;
            sidebarDivider.BackColor = p.Border;
            headerDivider.BackColor  = p.Border;

            lblAppTitle.ForeColor     = p.Text;
            lblAppSubtitle.ForeColor  = p.TextSecondary;
            lblPageTitle.ForeColor    = p.Text;
            lblPageSubtitle.ForeColor = p.TextSecondary;
            statusLabel.ForeColor     = p.TextSecondary;

            lblProgressPercent.ForeColor = p.TextSecondary;

            ApplyThemeRecursive(pageBackup, p);
            ApplyThemeRecursive(pageRestore, p);
            ApplyThemeRecursive(pageMigration, p);

            Invalidate(true);
        }

        private static void ApplyThemeRecursive(Control root, ThemePalette p)
        {
            foreach (Control ctrl in root.Controls)
            {
                switch (ctrl)
                {
                    case CardPanel card:
                        ApplyThemeRecursive(card, p);
                        card.Invalidate();
                        break;
                    case TextBox txt:
                        txt.BackColor = p.InputBackground;
                        txt.ForeColor = p.Text;
                        break;
                    case ComboBox cmb:
                        cmb.BackColor = p.InputBackground;
                        cmb.ForeColor = p.Text;
                        break;
                    case ListBox lst:
                        lst.BackColor = p.InputBackground;
                        lst.ForeColor = p.Text;
                        break;
                    case Label lbl:
                        lbl.ForeColor = lbl.Tag as string == "secondary" ? p.TextSecondary : p.Text;
                        lbl.BackColor = Color.Transparent;
                        break;
                    case ModernCheckBox chk:
                        chk.Invalidate();
                        break;
                    default:
                        if (ctrl.HasChildren) ApplyThemeRecursive(ctrl, p);
                        ctrl.Invalidate();
                        break;
                }
            }
        }

        // ───────────────────────────── Helpers UI ────────────────────

        private void Log(RichTextBox rtb, string message, Color? color = null, bool toast = false, ToastKind kind = ToastKind.Info)
        {
            if (InvokeRequired)
            { Invoke(() => Log(rtb, message, color, toast, kind)); return; }

            var p         = ThemeManager.Palette;
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var fullMsg   = $"[{timestamp}] {message}\n";

            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionColor = color ?? p.ConsoleText;
            rtb.AppendText(fullMsg);
            rtb.ScrollToCaret();

            string? logPath;
            lock (_logLock) { logPath = _logFilePath; }
            if (!string.IsNullOrEmpty(logPath))
            {
                try
                {
                    lock (_logLock) { File.AppendAllText(logPath, fullMsg, Encoding.UTF8); }
                }
                catch (Exception ex)
                {
                    // Trace l'erreur d'ecriture directement dans la RichTextBox (sans boucle recursive)
                    var warn = $"[{DateTime.Now:HH:mm:ss}] ⚠ Impossible d'écrire dans le log fichier : {ex.Message}\n";
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionColor = Color.FromArgb(241, 250, 140);
                    rtb.AppendText(warn);
                    rtb.ScrollToCaret();
                }
            }

            if (toast) ToastService.Show(this, message, kind);
        }

        private void LogSuccess(RichTextBox rtb, string m) => Log(rtb, "✓ " + m, Color.FromArgb(80, 250, 123));
        private void LogError  (RichTextBox rtb, string m) => Log(rtb, "✗ " + m, Color.FromArgb(255, 121, 121), toast: true, kind: ToastKind.Error);
        private void LogWarning(RichTextBox rtb, string m) => Log(rtb, "⚠ " + m, Color.FromArgb(241, 250, 140));
        private void LogInfo   (RichTextBox rtb, string m) => Log(rtb, "ℹ " + m, Color.FromArgb(139, 233, 253));
        private void LogTitle  (RichTextBox rtb, string m) => Log(rtb, $"\n══════ {m.ToUpper()} ══════", Color.FromArgb(255, 184, 108));

        private void UpdateStatus(string message)
        { if (InvokeRequired) { Invoke(() => UpdateStatus(message)); return; } statusLabel.Text = message; }

        private void UpdateProgress(int percent)
        {
            if (InvokeRequired) { Invoke(() => UpdateProgress(percent)); return; }
            int clamped = Math.Min(100, Math.Max(0, percent));
            progressBar.Visible = true;
            progressBar.Value   = clamped;
            lblProgressPercent.Visible = true;
            lblProgressPercent.Text    = $"{clamped}%";
            ApplyResponsiveLayout();
        }

        private void HideProgress()
        {
            if (InvokeRequired) { Invoke(HideProgress); return; }
            progressBar.Visible        = false;
            progressBar.Value          = 0;
            lblProgressPercent.Visible = false;
            lblProgressPercent.Text    = string.Empty;
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (InvokeRequired) { Invoke(() => SetControlsEnabled(enabled)); return; }

            navBackup.Enabled    = enabled;
            navRestore.Enabled   = enabled;
            navMigration.Enabled = enabled;

            btnStartBackup.Enabled    = enabled;
            btnStartRestore.Enabled   = enabled;
            btnStartMigration.Enabled = enabled;
            btnBrowseBackup.Enabled   = enabled;
            btnBrowseRestore.Enabled  = enabled;
            if (enabled) UpdateOldProfileOptionState();

            btnSelectAll.Enabled          = enabled;
            btnDeselectAll.Enabled        = enabled;
            btnRestoreSelectAll.Enabled   = enabled;
            btnRestoreDeselectAll.Enabled = enabled;
            btnMigrateSelectAll.Enabled   = enabled;
            btnMigrateDeselectAll.Enabled = enabled;
            cmbUSBDrives.Enabled  = enabled;
            btnRefreshUSB.Enabled = enabled;

            btnCancelBackup.Enabled    = !enabled;
            btnCancelRestore.Enabled   = !enabled;
            btnCancelMigration.Enabled = !enabled;
        }

        private void ExportLog(RichTextBox rtb, string defaultName)
        {
            using var sfd = new SaveFileDialog { Filter = "Fichier texte|*.txt", FileName = defaultName };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                File.WriteAllText(sfd.FileName, rtb.Text, Encoding.UTF8);
                ToastService.Show(this, $"Log exporté : {Path.GetFileName(sfd.FileName)}", ToastKind.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Impossible d'écrire le fichier log :\n{ex.Message}",
                    "Erreur d'exportation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UpdateOldProfileOptionState()
        {
            if (chkPanelBackup == null) return;

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var usersDir    = Path.GetDirectoryName(userProfile);
            var currentUser = Environment.UserName;
            if (usersDir == null)
            {
                chkPanelBackup.SetChecked("OldProfile", false);
                return;
            }

            var exact = Path.Combine(usersDir, currentUser + ".ZEPRODBUR");
            bool hasOldProfile = Directory.Exists(exact);

            if (!hasOldProfile)
            {
                hasOldProfile = Directory.GetDirectories(usersDir)
                    .Select(Path.GetFileName)
                    .Any(name =>
                        !string.IsNullOrWhiteSpace(name)
                        && !name!.Equals(currentUser, StringComparison.OrdinalIgnoreCase)
                        && !ExcludedProfiles.Contains(name, StringComparer.OrdinalIgnoreCase)
                        && name.StartsWith(currentUser + ".", StringComparison.OrdinalIgnoreCase));
            }

            if (!hasOldProfile) chkPanelBackup.SetChecked("OldProfile", false);
        }

        private void DetectAndLogOldProfiles(RichTextBox rtb)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var usersDir    = Path.GetDirectoryName(userProfile);
            var currentUser = Environment.UserName;

            if (usersDir == null) return;

            var oldProfiles = Directory.GetDirectories(usersDir)
                .Where(d =>
                {
                    var name = Path.GetFileName(d);
                    return name != null
                        && !name.Equals(currentUser, StringComparison.OrdinalIgnoreCase)
                        && !ExcludedProfiles.Contains(name, StringComparer.OrdinalIgnoreCase)
                        && name.StartsWith(currentUser + ".", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (oldProfiles.Count == 0)
            {
                LogInfo(rtb, "Aucun ancien profil domaine détecté.");
                return;
            }

            LogTitle(rtb, "Anciens profils domaine détectés");
            foreach (var profile in oldProfiles)
                LogInfo(rtb, $"  → {Path.GetFileName(profile)}  ({profile})");
        }

        private void CancelCurrentOperation(RichTextBox rtb)
        {
            // Annule l'operation de l'onglet correspondant selon le rtb transmis
            var cts = ReferenceEquals(rtb, BackupLogBox)    ? _ctsBackup
                    : ReferenceEquals(rtb, RestoreLogBox)   ? _ctsRestore
                    : ReferenceEquals(rtb, MigrationLogBox) ? _ctsMigration
                    : null;

            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                LogWarning(rtb, "Annulation en cours...");
            }
        }
    }
}
