using System.Text;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Fenêtre principale — refonte graphique V5 :
    /// navigation latérale moderne, cartes arrondies, thème clair/sombre dynamique.
    /// Structure : Sidebar (gauche) + zone de contenu (pages empilées) + barre d'état.
    /// </summary>
    public partial class MainForm : Form
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _logFilePath;

        public MainForm()
        {
            InitializeComponent();
            ThemeManager.ThemeChanged += OnThemeChanged;
            this.Load += (_, _) => SyncPageSizes();
            this.Resize += (_, _) => SyncPageSizes();
            ApplyTheme();
            UpdateOldProfileOptionState();
            LoadUSBDrives();
            ShowPage(0);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            base.OnFormClosed(e);
        }

        // ───────────────────────────── Navigation ─────────────────────────────

        private void ShowPage(int index)
        {
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

        // ───────────────────────────── Thème ─────────────────────────────

        private void OnThemeChanged() => ApplyTheme();

        private void ApplyTheme()
        {
            var p = ThemeManager.Palette;

            BackColor = p.Background;
            sidebarPanel.BackColor = p.Sidebar;
            contentPanel.BackColor = p.Background;
            headerPanel.BackColor = p.Background;
            statusPanel.BackColor = p.Sidebar;

            lblAppTitle.ForeColor = p.Text;
            lblAppSubtitle.ForeColor = p.TextSecondary;
            lblPageTitle.ForeColor = p.Text;
            lblPageSubtitle.ForeColor = p.TextSecondary;
            statusLabel.ForeColor = p.TextSecondary;

            btnToggleTheme.Text = ThemeManager.IsDark ? "🌙  Thème sombre" : "☀️  Thème clair";

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
                    case RichTextBox rtb:
                        rtb.BackColor = p.ConsoleBackground;
                        rtb.ForeColor = p.ConsoleText;
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

        // ───────────────────────────── Helpers UI ─────────────────────────────

        private void Log(RichTextBox rtb, string message, Color? color = null, bool toast = false, ToastKind kind = ToastKind.Info)
        {
            if (InvokeRequired)
            {
                Invoke(() => Log(rtb, message, color, toast, kind));
                return;
            }

            var p = ThemeManager.Palette;
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var fullMessage = $"[{timestamp}] {message}\n";

            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionColor = color ?? p.ConsoleText;
            rtb.AppendText(fullMessage);
            rtb.ScrollToCaret();

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try { File.AppendAllText(_logFilePath, fullMessage, Encoding.UTF8); }
                catch { }
            }

            if (toast) ToastService.Show(this, message, kind);
        }

        private void LogSuccess(RichTextBox rtb, string message) => Log(rtb, "✓ " + message, Color.FromArgb(80, 250, 123));
        private void LogError(RichTextBox rtb, string message) => Log(rtb, "✗ " + message, Color.FromArgb(255, 121, 121), toast: true, kind: ToastKind.Error);
        private void LogWarning(RichTextBox rtb, string message) => Log(rtb, "⚠ " + message, Color.FromArgb(241, 250, 140));
        private void LogInfo(RichTextBox rtb, string message) => Log(rtb, "ℹ " + message, Color.FromArgb(139, 233, 253));
        private void LogTitle(RichTextBox rtb, string message) => Log(rtb, $"\n══════ {message.ToUpper()} ══════", Color.FromArgb(255, 184, 108));

        private void UpdateStatus(string message)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatus(message)); return; }
            statusLabel.Text = message;
        }

        private void UpdateProgress(int percent)
        {
            if (InvokeRequired) { Invoke(() => UpdateProgress(percent)); return; }
            progressBar.Visible = true;
            progressBar.Value = Math.Min(100, Math.Max(0, percent));
        }

        private void HideProgress()
        {
            if (InvokeRequired) { Invoke(HideProgress); return; }
            progressBar.Visible = false;
            progressBar.Value = 0;
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
            if (enabled)
            {
                UpdateOldProfileOptionState();
            }
            btnSelectAll.Enabled   = enabled;
            btnDeselectAll.Enabled = enabled;
            btnRestoreSelectAll.Enabled   = enabled;
            btnRestoreDeselectAll.Enabled = enabled;
            btnMigrateSelectAll.Enabled   = enabled;
            btnMigrateDeselectAll.Enabled = enabled;
            cmbUSBDrives.Enabled  = enabled;
            lstProfiles.Enabled   = enabled;
            btnRefreshUSB.Enabled = enabled;

            btnCancelBackup.Enabled    = !enabled;
            btnCancelRestore.Enabled   = !enabled;
            btnCancelMigration.Enabled = !enabled;
        }

        private void ExportLog(RichTextBox rtb, string defaultName)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Fichier texte|*.txt",
                FileName = defaultName
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, rtb.Text, Encoding.UTF8);
                ToastService.Show(this, $"Log exporté : {Path.GetFileName(sfd.FileName)}", ToastKind.Success);
            }
        }

        /// <summary>
        /// Active/désactive l'option "ancien profil" selon la présence d'un profil
        /// {utilisateur}.ZEPRODBUR (ou fallback {utilisateur}.*) dans C:\Users.
        /// </summary>
        private void UpdateOldProfileOptionState()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var usersDir    = Path.GetDirectoryName(userProfile);
            var currentUser = Environment.UserName;
            if (usersDir == null)
            {
                chkOldProfile.Checked = false;
                chkOldProfile.Enabled = false;
                return;
            }

            var exact = Path.Combine(usersDir, currentUser + ".ZEPRODBUR");
            bool hasOldProfile = Directory.Exists(exact);

            if (!hasOldProfile)
            {
                var excluded = new[] { "Public", "Default", "Default User", "All Users", "defaultuser0" };
                hasOldProfile = Directory.GetDirectories(usersDir)
                    .Select(Path.GetFileName)
                    .Any(name =>
                        !string.IsNullOrWhiteSpace(name)
                        && !name.Equals(currentUser, StringComparison.OrdinalIgnoreCase)
                        && !excluded.Contains(name, StringComparer.OrdinalIgnoreCase)
                        && name.StartsWith(currentUser + ".", StringComparison.OrdinalIgnoreCase));
            }

            chkOldProfile.Enabled = hasOldProfile;
            if (!hasOldProfile)
                chkOldProfile.Checked = false;
        }

        private void CancelCurrentOperation(RichTextBox rtb)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                LogWarning(rtb, "Annulation en cours...");
            }
        }
    }
}
