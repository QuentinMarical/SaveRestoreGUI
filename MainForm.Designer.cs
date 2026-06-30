using System;
using System.Drawing;
using System.Windows.Forms;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // ─── Structure ───
        private Panel sidebarPanel;
        private Panel contentPanel;
        private Panel headerPanel;
        private Panel statusPanel;
        private Label lblAppTitle;
        private Label lblAppSubtitle;
        private Label lblPageTitle;
        private Label lblPageSubtitle;
        private NavButton navBackup;
        private NavButton navRestore;
        private NavButton navMigration;
        private ModernButton btnToggleTheme;
        private Label statusLabel;
        private ModernProgressBar progressBar;
        private Label lblProgressPercent;

        // ─── Page Sauvegarde ───
        private Panel pageBackup;
        private CardPanel cardBackupDest;
        private Label lblBackupPath;
        private TextBox txtBackupPath;
        private ModernButton btnBrowseBackup;
        private CardPanel cardBackupOptions;
        private Label lblBackupOptionsTitle;
        private CategoryCheckPanel chkPanelBackup;
        private ModernButton btnSelectAll;
        private ModernButton btnDeselectAll;
        private ModernButton btnStartBackup;
        private ModernButton btnCancelBackup;
        private ModernButton btnExportBackupLog;
        private ModernButton btnToggleBackupLog;
        private RichTextBox rtbBackupLog;
        private BrowserPickerButton btnBrowserPickerBackup;

        // ─── Page Restauration ───
        private Panel pageRestore;
        private CardPanel cardRestoreSource;
        private Label lblRestorePath;
        private TextBox txtRestorePath;
        private ModernButton btnBrowseRestore;
        private CardPanel cardRestoreOptions;
        private Label lblRestoreOptionsTitle;
        private CategoryCheckPanel chkPanelRestore;
        private ModernButton btnRestoreSelectAll;
        private ModernButton btnRestoreDeselectAll;
        private ModernButton btnStartRestore;
        private ModernButton btnCancelRestore;
        private ModernButton btnExportRestoreLog;
        private ModernButton btnToggleRestoreLog;
        private RichTextBox rtbRestoreLog;
        private BrowserPickerButton btnBrowserPickerRestore;

        // ─── Page Migration ───
        private Panel pageMigration;
        private CardPanel cardMigrationSource;
        private Label lblUSBDrives;
        private ComboBox cmbUSBDrives;
        private ModernButton btnRefreshUSB;
        private ModernButton btnUnlockBitLocker;
        private Label lblProfiles;
        private ListBox lstProfiles;
        private Label lblMigrationInfo;
        private ModernButton btnBitLocker;
        private Label lblBitLockerStatus;
        private CardPanel cardMigrationOptions;
        private Label lblMigrationOptionsTitle;
        private CategoryCheckPanel chkPanelMigration;
        private ModernButton btnMigrateSelectAll;
        private ModernButton btnMigrateDeselectAll;
        private ModernButton btnStartMigration;
        private ModernButton btnCancelMigration;
        private ModernButton btnExportMigrationLog;
        private ModernButton btnToggleMigrationLog;
        private RichTextBox rtbMigrationLog;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            Text          = "SaveRestore GUI";
            Size          = new Size(1100, 780);
            MinimumSize   = new Size(1024, 700); // fenêtre minimale pour éviter une interface "ratiboisée"
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            // ── Sidebar
            sidebarPanel   = new Panel { Dock = DockStyle.Left, Width = 220 };
            lblAppTitle    = new Label { Text = "SaveRestore", AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
            lblAppSubtitle = new Label { Text = "Gestionnaire de profil", AutoSize = true, Tag = "secondary" };
            lblAppTitle.SetBounds(20, 20, 180, 30);
            lblAppSubtitle.SetBounds(20, 52, 180, 20);

            navBackup    = new NavButton { Text = "\U0001f4be Sauvegarde" };
            navRestore   = new NavButton { Text = "\U0001f4c2 Restauration" };
            navMigration = new NavButton { Text = "\U0001f504 Migration USB" };
            navBackup.SetBounds(20, 100, 180, 44);
            navRestore.SetBounds(20, 152, 180, 44);
            navMigration.SetBounds(20, 204, 180, 44);
            navBackup.Click    += (s, e) => ShowPage(0);
            navRestore.Click   += (s, e) => ShowPage(1);
            navMigration.Click += (s, e) => ShowPage(2);

            btnToggleTheme = new ModernButton { Text = "\U0001f319 Thème sombre", AutoSize = true };
            btnToggleTheme.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnToggleTheme.SetBounds(16, 680, 188, 34);
            btnToggleTheme.Click += (s, e) => { ThemeManager.Toggle(); ApplyTheme(); };

            sidebarPanel.Controls.AddRange(new Control[] {
                lblAppTitle, lblAppSubtitle,
                navBackup, navRestore, navMigration,
                btnToggleTheme
            });

            // ── Header
            headerPanel     = new Panel { Dock = DockStyle.Top, Height = 72 };
            lblPageTitle    = new Label { Text = "Sauvegarde", AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold) };
            lblPageSubtitle = new Label { Text = "", AutoSize = true, Tag = "secondary" };
            lblPageTitle.SetBounds(28, 14, 600, 28);
            lblPageSubtitle.SetBounds(28, 44, 700, 20);
            headerPanel.Controls.AddRange(new Control[] { lblPageTitle, lblPageSubtitle });

            // ── Status bar (sans progressBar)
            statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            statusLabel = new Label { Text = "Prêt", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            statusLabel.Padding = new Padding(12, 0, 0, 0);
            statusPanel.Controls.Add(statusLabel);

            // progressBar et lblProgressPercent en overlay dans contentPanel
            progressBar = new ModernProgressBar { Visible = false };
            lblProgressPercent = new Label
            {
                Text       = "",
                AutoSize   = false,
                TextAlign  = ContentAlignment.MiddleRight,
                Visible    = false,
                BackColor  = Color.Transparent
            };

            // ── Content panel
            contentPanel = new Panel { Dock = DockStyle.Fill };
            contentPanel.Controls.Add(progressBar);
            contentPanel.Controls.Add(lblProgressPercent);

            // ── Pages
            pageBackup    = new Panel { Dock = DockStyle.Fill, Visible = true  };
            pageRestore   = new Panel { Dock = DockStyle.Fill, Visible = false };
            pageMigration = new Panel { Dock = DockStyle.Fill, Visible = false };

            // Construction détaillée des pages dans les autres parties de MainForm
            // pour laisser le concepteur générer uniquement la structure.

            contentPanel.Controls.AddRange(new Control[] { pageBackup, pageRestore, pageMigration });
            Controls.AddRange(new Control[] { contentPanel, headerPanel, sidebarPanel, statusPanel });

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
