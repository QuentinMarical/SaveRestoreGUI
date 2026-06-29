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
            MinimumSize   = new Size(900, 620);
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

            // progressBar et lblProgressPercent sont positionnés manuellement dans les pages
            progressBar = new ModernProgressBar { Visible = false };
            lblProgressPercent = new Label
            {
                Text = "",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };

            // ── Content panel
            contentPanel = new Panel { Dock = DockStyle.Fill };

            // ── Pages
            pageBackup    = new Panel { Dock = DockStyle.Fill, Visible = true  };
            pageRestore   = new Panel { Dock = DockStyle.Fill, Visible = false };
            pageMigration = new Panel { Dock = DockStyle.Fill, Visible = false };

            BuildPageBackup();
            BuildPageRestore();
            BuildPageMigration();

            contentPanel.Controls.AddRange(new Control[] { pageBackup, pageRestore, pageMigration });
            Controls.AddRange(new Control[] { contentPanel, headerPanel, sidebarPanel, statusPanel });

            ResumeLayout(false);
            PerformLayout();
        }

        // ═══════════════════════════════════════════════════════════════════
        // Page Sauvegarde
        // ═══════════════════════════════════════════════════════════════════
        private void BuildPageBackup()
        {
            cardBackupDest  = new CardPanel();
            lblBackupPath   = new Label { Text = "Dossier de destination", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            txtBackupPath   = new TextBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            btnBrowseBackup = new ModernButton { Text = "Parcourir\u2026", Role = ButtonRole.Secondary, Size = new Size(120, 32) };
            btnBrowseBackup.Click += BtnBrowseBackup_Click;
            cardBackupDest.Controls.AddRange(new Control[] { lblBackupPath, txtBackupPath, btnBrowseBackup });

            cardBackupOptions     = new CardPanel();
            lblBackupOptionsTitle = new Label { Text = "Éléments à sauvegarder", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            chkPanelBackup = new CategoryCheckPanel();
            chkPanelBackup.SetCategories(CheckCatalog.Build(includeOldProfile: true));

            btnSelectAll   = new ModernButton { Text = "Tout cocher",   Role = ButtonRole.Secondary, Size = new Size(120, 34) };
            btnDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary, Size = new Size(130, 34) };
            btnSelectAll.Click   += (s, e) => chkPanelBackup.SetAll(true);
            btnDeselectAll.Click += (s, e) => chkPanelBackup.SetAll(false);

            btnBrowserPickerBackup = new BrowserPickerButton();

            cardBackupOptions.Controls.AddRange(new Control[]
            {
                lblBackupOptionsTitle, chkPanelBackup, btnBrowserPickerBackup,
                btnSelectAll, btnDeselectAll
            });

            btnStartBackup     = new ModernButton { Text = "\u25b6 Démarrer la sauvegarde", Height = 44, Role = ButtonRole.Primary };
            btnCancelBackup    = new ModernButton { Text = "\u2b1b Annuler",                Height = 44, Enabled = false };
            btnExportBackupLog = new ModernButton { Text = "\U0001f4c4 Exporter le log",   Height = 34 };
            btnToggleBackupLog = new ModernButton { Text = "\u25b2 Masquer les logs",       Height = LogToggleH, Role = ButtonRole.Secondary };
            btnStartBackup.Click     += BtnStartBackup_Click;
            btnCancelBackup.Click    += (s, e) => CancelCurrentOperation(rtbBackupLog);
            btnExportBackupLog.Click += (s, e) => ExportLog(rtbBackupLog, $"Sauvegarde_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            btnToggleBackupLog.Click += (s, e) => ToggleBackupLog();

            rtbBackupLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageBackup.Controls.AddRange(new Control[] {
                cardBackupDest, cardBackupOptions,
                btnStartBackup, btnCancelBackup, btnExportBackupLog,
                btnToggleBackupLog, rtbBackupLog
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // Page Restauration
        // ═══════════════════════════════════════════════════════════════════
        private void BuildPageRestore()
        {
            cardRestoreSource  = new CardPanel();
            lblRestorePath     = new Label { Text = "Dossier source de la sauvegarde", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            txtRestorePath   = new TextBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            btnBrowseRestore = new ModernButton { Text = "Parcourir\u2026", Role = ButtonRole.Secondary, Size = new Size(120, 32) };
            btnBrowseRestore.Click += BtnBrowseRestore_Click;
            cardRestoreSource.Controls.AddRange(new Control[] { lblRestorePath, txtRestorePath, btnBrowseRestore });

            cardRestoreOptions     = new CardPanel();
            lblRestoreOptionsTitle = new Label { Text = "Éléments à restaurer", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            chkPanelRestore = new CategoryCheckPanel();
            chkPanelRestore.SetCategories(CheckCatalog.Build(includeLaunchApps: true));

            btnRestoreSelectAll   = new ModernButton { Text = "Tout cocher",   Role = ButtonRole.Secondary, Size = new Size(120, 34) };
            btnRestoreDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary, Size = new Size(130, 34) };
            btnRestoreSelectAll.Click   += (s, e) => chkPanelRestore.SetAll(true);
            btnRestoreDeselectAll.Click += (s, e) => chkPanelRestore.SetAll(false);

            btnBrowserPickerRestore = new BrowserPickerButton();

            cardRestoreOptions.Controls.AddRange(new Control[]
            {
                lblRestoreOptionsTitle, chkPanelRestore, btnBrowserPickerRestore,
                btnRestoreSelectAll, btnRestoreDeselectAll
            });

            btnStartRestore     = new ModernButton { Text = "\u25b6 Démarrer la restauration", Height = 44, Role = ButtonRole.Primary };
            btnCancelRestore    = new ModernButton { Text = "\u2b1b Annuler",                  Height = 44, Enabled = false };
            btnExportRestoreLog = new ModernButton { Text = "\U0001f4c4 Exporter le log",     Height = 34 };
            btnToggleRestoreLog = new ModernButton { Text = "\u25b2 Masquer les logs",         Height = LogToggleH, Role = ButtonRole.Secondary };
            btnStartRestore.Click     += BtnStartRestore_Click;
            btnCancelRestore.Click    += (s, e) => CancelCurrentOperation(rtbRestoreLog);
            btnExportRestoreLog.Click += (s, e) => ExportLog(rtbRestoreLog, $"Restauration_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            btnToggleRestoreLog.Click += (s, e) => ToggleRestoreLog();

            rtbRestoreLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageRestore.Controls.AddRange(new Control[] {
                cardRestoreSource, cardRestoreOptions,
                btnStartRestore, btnCancelRestore, btnExportRestoreLog,
                btnToggleRestoreLog, rtbRestoreLog
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // Page Migration
        // ═══════════════════════════════════════════════════════════════════
        private void BuildPageMigration()
        {
            cardMigrationSource = new CardPanel();
            lblUSBDrives = new Label { Text = "Disque externe contenant Windows", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            cmbUSBDrives = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f), FlatStyle = FlatStyle.Flat };
            cmbUSBDrives.SelectedIndexChanged += CmbUSBDrives_SelectedIndexChanged;

            btnRefreshUSB = new ModernButton { Text = "\U0001f504", Role = ButtonRole.Secondary, Size = new Size(40, 32) };
            btnRefreshUSB.Click += BtnRefreshUSB_Click;

            btnUnlockBitLocker = new ModernButton { Text = "\U0001f512 Déverrouiller ce disque (BitLocker)", Height = 34, Visible = false, Role = ButtonRole.Secondary };
            btnUnlockBitLocker.Click += (s, e) => BtnBitLocker_Click(s, e);

            lblProfiles = new Label { Text = "Profil utilisateur à migrer", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            lstProfiles = new ListBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };

            lblMigrationInfo = new Label { Text = "Sélectionnez un disque pour afficher les profils.", Font = new Font("Segoe UI", 8.5f), BackColor = Color.Transparent, Tag = "secondary" };
            btnBitLocker = new ModernButton { Text = "\U0001f512 Vérifier BitLocker", Role = ButtonRole.Secondary, Size = new Size(180, 32) };
            btnBitLocker.Click += BtnBitLocker_Click;

            lblBitLockerStatus = new Label { Text = "", Font = new Font("Segoe UI", 8.5f), AutoSize = false, BackColor = Color.Transparent, Tag = "secondary" };

            cardMigrationSource.Controls.AddRange(new Control[]
            {
                lblUSBDrives, cmbUSBDrives, btnRefreshUSB, btnUnlockBitLocker,
                lblProfiles, lstProfiles, btnBitLocker, lblBitLockerStatus, lblMigrationInfo
            });

            cardMigrationOptions     = new CardPanel();
            lblMigrationOptionsTitle = new Label { Text = "Éléments à migrer (mode fusion)", Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, BackColor = Color.Transparent };
            chkPanelMigration = new CategoryCheckPanel();
            chkPanelMigration.SetCategories(CheckCatalog.Build());

            btnMigrateSelectAll   = new ModernButton { Text = "Tout cocher",   Role = ButtonRole.Secondary, Size = new Size(120, 34) };
            btnMigrateDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary, Size = new Size(130, 34) };
            btnMigrateSelectAll.Click   += (s, e) => chkPanelMigration.SetAll(true);
            btnMigrateDeselectAll.Click += (s, e) => chkPanelMigration.SetAll(false);

            cardMigrationOptions.Controls.AddRange(new Control[]
            {
                lblMigrationOptionsTitle, chkPanelMigration,
                btnMigrateSelectAll, btnMigrateDeselectAll
            });

            btnStartMigration     = new ModernButton { Text = "\u25b6 Démarrer la migration", Height = 44, Role = ButtonRole.Primary };
            btnCancelMigration    = new ModernButton { Text = "\u2b1b Annuler",               Height = 44, Enabled = false };
            btnExportMigrationLog = new ModernButton { Text = "\U0001f4c4 Exporter le log",  Height = 34 };
            btnToggleMigrationLog = new ModernButton { Text = "\u25b2 Masquer les logs",      Height = LogToggleH, Role = ButtonRole.Secondary };
            btnStartMigration.Click     += BtnStartMigration_Click;
            btnCancelMigration.Click    += (s, e) => CancelCurrentOperation(rtbMigrationLog);
            btnExportMigrationLog.Click += (s, e) => ExportLog(rtbMigrationLog, $"Migration_{DateTime.Now:yyyyMMdd_HHmm}.txt");
            btnToggleMigrationLog.Click += (s, e) => ToggleMigrationLog();

            rtbMigrationLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageMigration.Controls.AddRange(new Control[] {
                cardMigrationSource, cardMigrationOptions,
                btnStartMigration, btnCancelMigration, btnExportMigrationLog,
                btnToggleMigrationLog, rtbMigrationLog
            });
        }

        private void SyncPageSizes()
        {
            pageBackup.Size    = contentPanel.ClientSize;
            pageRestore.Size   = contentPanel.ClientSize;
            pageMigration.Size = contentPanel.ClientSize;
            ApplyResponsiveLayout();
        }

        #endregion
    }
}
