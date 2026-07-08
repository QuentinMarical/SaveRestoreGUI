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
        private ModernButton btnOpenBackupLog;

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
        private ModernButton btnOpenRestoreLog;

        // ─── Page Migration ───
        private Panel pageMigration;
        private CardPanel cardMigrationSource;
        private Label lblUSBDrives;
        private ComboBox cmbUSBDrives;
        private ModernButton btnRefreshUSB;
        private ModernButton btnUnlockBitLocker;
        private Label lblSelectedProfile;
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
        private ModernButton btnOpenMigrationLog;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            Text          = "SaveRestore GUI";
            Size          = new Size(1100, 780);
            MinimumSize   = new Size(1024, 700);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState   = FormWindowState.Maximized;
            MaximizeBox   = false;
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

            // ── Status bar
            statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            statusLabel = new Label
            {
                Text      = "Prêt",
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusLabel.Padding = new Padding(12, 0, 0, 0);
            statusPanel.Controls.Add(statusLabel);

            // ── Content panel + overlays progress
            contentPanel = new Panel { Dock = DockStyle.Fill };

            progressBar = new ModernProgressBar { Visible = false };
            lblProgressPercent = new Label
            {
                Text      = "",
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleRight,
                Visible   = false,
                BackColor = Color.Transparent
            };
            contentPanel.Controls.Add(progressBar);
            contentPanel.Controls.Add(lblProgressPercent);

            // ── Pages (panels)
            pageBackup    = new Panel { Dock = DockStyle.Fill, Visible = true  };
            pageRestore   = new Panel { Dock = DockStyle.Fill, Visible = false };
            pageMigration = new Panel { Dock = DockStyle.Fill, Visible = false };

            // ════════════════════════════════════════════════
            // CONTRÔLES PAGE SAUVEGARDE
            // ════════════════════════════════════════════════
            cardBackupDest = new CardPanel();
            lblBackupPath  = new Label  { Text = "Dossier de sauvegarde :", AutoSize = true };
            txtBackupPath  = new TextBox();
            btnBrowseBackup = new ModernButton { Text = "Parcourir..." };
            cardBackupDest.Controls.AddRange(new Control[] { lblBackupPath, txtBackupPath, btnBrowseBackup });

            cardBackupOptions     = new CardPanel();
            lblBackupOptionsTitle = new Label { Text = "Éléments à sauvegarder", AutoSize = true, Tag = "secondary" };
            chkPanelBackup        = new CategoryCheckPanel();
            btnSelectAll   = new ModernButton { Text = "Tout cocher" };
            btnDeselectAll = new ModernButton { Text = "Tout décocher" };
            cardBackupOptions.Controls.AddRange(new Control[] { lblBackupOptionsTitle, chkPanelBackup, btnSelectAll, btnDeselectAll });

            btnStartBackup     = new ModernButton { Text = "Démarrer la sauvegarde" };
            btnCancelBackup    = new ModernButton { Text = "Annuler" };
            btnExportBackupLog = new ModernButton { Text = "Exporter les logs" };
            btnOpenBackupLog   = new ModernButton { Text = "\U0001f4cb Voir les logs" };

            btnStartBackup.Click     += BtnStartBackup_Click;
            btnCancelBackup.Click    += BtnCancelBackup_Click;
            btnExportBackupLog.Click += (s, e) => ExportLog(_logWindowBackup.LogBox, "backup-log.txt");
            btnOpenBackupLog.Click   += (s, e) => OpenLogWindow(0);

            pageBackup.Controls.AddRange(new Control[] {
                cardBackupDest, cardBackupOptions,
                btnStartBackup, btnCancelBackup, btnExportBackupLog, btnOpenBackupLog
            });

            // ════════════════════════════════════════════════
            // CONTRÔLES PAGE RESTAURATION
            // ════════════════════════════════════════════════
            cardRestoreSource = new CardPanel();
            lblRestorePath    = new Label  { Text = "Dossier de sauvegarde :", AutoSize = true };
            txtRestorePath    = new TextBox();
            btnBrowseRestore  = new ModernButton { Text = "Parcourir..." };
            cardRestoreSource.Controls.AddRange(new Control[] { lblRestorePath, txtRestorePath, btnBrowseRestore });

            cardRestoreOptions     = new CardPanel();
            lblRestoreOptionsTitle = new Label { Text = "Éléments à restaurer", AutoSize = true, Tag = "secondary" };
            chkPanelRestore        = new CategoryCheckPanel();
            btnRestoreSelectAll   = new ModernButton { Text = "Tout cocher" };
            btnRestoreDeselectAll = new ModernButton { Text = "Tout décocher" };
            cardRestoreOptions.Controls.AddRange(new Control[] { lblRestoreOptionsTitle, chkPanelRestore, btnRestoreSelectAll, btnRestoreDeselectAll });

            btnStartRestore     = new ModernButton { Text = "Démarrer la restauration" };
            btnCancelRestore    = new ModernButton { Text = "Annuler" };
            btnExportRestoreLog = new ModernButton { Text = "Exporter les logs" };
            btnOpenRestoreLog   = new ModernButton { Text = "\U0001f4cb Voir les logs" };

            btnStartRestore.Click     += BtnStartRestore_Click;
            btnCancelRestore.Click    += BtnCancelRestore_Click;
            btnExportRestoreLog.Click += (s, e) => ExportLog(_logWindowRestore.LogBox, "restore-log.txt");
            btnOpenRestoreLog.Click   += (s, e) => OpenLogWindow(1);

            pageRestore.Controls.AddRange(new Control[] {
                cardRestoreSource, cardRestoreOptions,
                btnStartRestore, btnCancelRestore, btnExportRestoreLog, btnOpenRestoreLog
            });

            // ════════════════════════════════════════════════
            // CONTRÔLES PAGE MIGRATION
            // ════════════════════════════════════════════════
            cardMigrationSource = new CardPanel();
            lblUSBDrives        = new Label   { Text = "Lecteur source :", AutoSize = true };
            cmbUSBDrives        = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            btnRefreshUSB       = new ModernButton { Text = "\U0001f504 Actualiser" };
            btnUnlockBitLocker  = new ModernButton { Text = "\U0001f512 Vérifier BitLocker" };
            lblSelectedProfile  = new Label   { Text = "", AutoSize = false };
            lblBitLockerStatus  = new Label   { Text = "", AutoSize = false };
            lblMigrationInfo    = new Label   { Text = "", AutoSize = false, Tag = "secondary" };

            btnBitLocker = btnUnlockBitLocker;

            cmbUSBDrives.SelectedIndexChanged += CmbUSBDrives_SelectedIndexChanged;
            btnRefreshUSB.Click               += BtnRefreshUSB_Click;
            btnUnlockBitLocker.Click          += BtnBitLocker_Click;

            cardMigrationSource.Controls.AddRange(new Control[] {
                lblUSBDrives, cmbUSBDrives, btnRefreshUSB, btnUnlockBitLocker,
                lblBitLockerStatus, lblSelectedProfile, lblMigrationInfo
            });

            cardMigrationOptions     = new CardPanel();
            lblMigrationOptionsTitle = new Label { Text = "Éléments à migrer", AutoSize = true, Tag = "secondary" };
            chkPanelMigration        = new CategoryCheckPanel();
            btnMigrateSelectAll   = new ModernButton { Text = "Tout cocher" };
            btnMigrateDeselectAll = new ModernButton { Text = "Tout décocher" };
            cardMigrationOptions.Controls.AddRange(new Control[] { lblMigrationOptionsTitle, chkPanelMigration, btnMigrateSelectAll, btnMigrateDeselectAll });

            btnStartMigration     = new ModernButton { Text = "Démarrer la migration" };
            btnCancelMigration    = new ModernButton { Text = "Annuler" };
            btnExportMigrationLog = new ModernButton { Text = "Exporter les logs" };
            btnOpenMigrationLog   = new ModernButton { Text = "\U0001f4cb Voir les logs" };

            btnStartMigration.Click     += BtnStartMigration_Click;
            btnCancelMigration.Click    += BtnCancelMigration_Click;
            btnExportMigrationLog.Click += (s, e) => ExportLog(_logWindowMigration.LogBox, "migration-log.txt");
            btnOpenMigrationLog.Click   += (s, e) => OpenLogWindow(2);

            pageMigration.Controls.AddRange(new Control[] {
                cardMigrationSource, cardMigrationOptions,
                btnStartMigration, btnCancelMigration, btnExportMigrationLog, btnOpenMigrationLog
            });

            // ── Assemblage final
            contentPanel.Controls.AddRange(new Control[] { pageBackup, pageRestore, pageMigration });
            Controls.AddRange(new Control[] { contentPanel, headerPanel, sidebarPanel, statusPanel });

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
