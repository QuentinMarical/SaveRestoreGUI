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
        private Panel sidebarDivider;
        private Panel headerDivider;
        private Label lblAppTitle;
        private Label lblAppSubtitle;
        private Label lblPageTitle;
        private Label lblPageSubtitle;
        private NavButton navBackup;
        private NavButton navRestore;
        private NavButton navMigration;
        private Label statusLabel;
        private ModernProgressBar progressBar;
        private Label lblProgressPercent;

        // ─── Page Sauvegarde ───
        private Panel pageBackup;
        private SettingCard cardBackupDest;
        private TextBox txtBackupPath;
        private ModernButton btnBrowseBackup;
        private SettingCard cardBackupOptions;
        private CategoryCheckPanel chkPanelBackup;
        private ModernButton btnSelectAll;
        private ModernButton btnDeselectAll;
        private ModernButton btnStartBackup;
        private ModernButton btnCancelBackup;
        private ModernButton btnExportBackupLog;
        private ModernButton btnOpenBackupLog;

        // ─── Page Restauration ───
        private Panel pageRestore;
        private SettingCard cardRestoreSource;
        private TextBox txtRestorePath;
        private ModernButton btnBrowseRestore;
        private SettingCard cardRestoreOptions;
        private CategoryCheckPanel chkPanelRestore;
        private ModernButton btnRestoreSelectAll;
        private ModernButton btnRestoreDeselectAll;
        private ModernButton btnStartRestore;
        private ModernButton btnCancelRestore;
        private ModernButton btnExportRestoreLog;
        private ModernButton btnOpenRestoreLog;

        // ─── Page Migration ───
        private Panel pageMigration;
        private SettingCard cardMigrationSource;
        private SettingCard cardMigrationBitLocker;
        private SettingCard cardMigrationProfile;
        private ComboBox cmbUSBDrives;
        private ModernButton btnRefreshUSB;
        private ModernButton btnUnlockBitLocker;
        private Label lblSelectedProfile;
        private Label lblMigrationInfo;
        private ModernButton btnBitLocker;
        private Label lblBitLockerStatus;
        private SettingCard cardMigrationOptions;
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

            // Fenêtre réellement maximisée (bords collés à l'écran) mais non
            // redimensionnable : FixedSingle supprime la poignée de resize,
            // MaximizeBox=false retire le bouton agrandir/restaurer. Les bornes
            // de maximisation sont posées dans OnHandleCreated (zone de travail).
            Text            = "SaveRestore GUI";
            Size            = new Size(1100, 780);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition   = FormStartPosition.CenterScreen;
            WindowState     = FormWindowState.Maximized;
            MaximizeBox     = false;
            MinimizeBox     = true;
            Font            = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

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

            // Liseré de séparation nav pane / contenu (repère de profondeur Fluent Win11)
            sidebarDivider = new Panel { Dock = DockStyle.Right, Width = 1 };
            sidebarPanel.Controls.AddRange(new Control[] {
                lblAppTitle, lblAppSubtitle,
                navBackup, navRestore, navMigration,
                sidebarDivider
            });

            // ── Header
            headerPanel     = new Panel { Dock = DockStyle.Top, Height = 72 };
            lblPageTitle    = new Label { Text = "Sauvegarde", AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold) };
            lblPageSubtitle = new Label { Text = "", AutoSize = true, Tag = "secondary" };
            lblPageTitle.SetBounds(28, 14, 600, 28);
            lblPageSubtitle.SetBounds(28, 44, 700, 20);
            headerDivider = new Panel { Dock = DockStyle.Bottom, Height = 1 };
            headerPanel.Controls.AddRange(new Control[] { lblPageTitle, lblPageSubtitle, headerDivider });

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
            cardBackupDest = new SettingCard
            {
                IconGlyph   = "\U0001f4c1",
                Title       = "Dossier de sauvegarde",
                Description = "Destination des données à sauvegarder"
            };
            txtBackupPath   = new TextBox { BorderStyle = BorderStyle.FixedSingle };
            btnBrowseBackup = new ModernButton { Text = "Parcourir...", Role = ButtonRole.Secondary };
            btnBrowseBackup.Click += BtnBrowseBackup_Click;
            cardBackupDest.Controls.AddRange(new Control[] { txtBackupPath, btnBrowseBackup });

            cardBackupOptions = new SettingCard
            {
                IconGlyph  = "\U0001f4cb",
                Title      = "Éléments à sauvegarder",
                HeaderMode = true
            };
            chkPanelBackup = new CategoryCheckPanel();
            btnSelectAll   = new ModernButton { Text = "Tout cocher", Role = ButtonRole.Secondary };
            btnDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary };
            btnSelectAll.Click   += (s, e) => chkPanelBackup.SetAll(true);
            btnDeselectAll.Click += (s, e) => chkPanelBackup.SetAll(false);
            cardBackupOptions.Controls.AddRange(new Control[] { chkPanelBackup, btnSelectAll, btnDeselectAll });

            btnStartBackup     = new ModernButton { Text = "Démarrer la sauvegarde" };
            btnCancelBackup    = new ModernButton { Text = "Annuler", Role = ButtonRole.Secondary };
            btnExportBackupLog = new ModernButton { Text = "Exporter les logs", Role = ButtonRole.Secondary };
            btnOpenBackupLog   = new ModernButton { Text = "\U0001f4cb Voir les logs", Role = ButtonRole.Secondary };

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
            cardRestoreSource = new SettingCard
            {
                IconGlyph   = "\U0001f4c2",
                Title       = "Dossier de sauvegarde",
                Description = "Source contenant la sauvegarde à restaurer"
            };
            txtRestorePath   = new TextBox { BorderStyle = BorderStyle.FixedSingle };
            btnBrowseRestore = new ModernButton { Text = "Parcourir...", Role = ButtonRole.Secondary };
            btnBrowseRestore.Click += BtnBrowseRestore_Click;
            cardRestoreSource.Controls.AddRange(new Control[] { txtRestorePath, btnBrowseRestore });

            cardRestoreOptions = new SettingCard
            {
                IconGlyph  = "\U0001f4cb",
                Title      = "Éléments à restaurer",
                HeaderMode = true
            };
            chkPanelRestore       = new CategoryCheckPanel();
            btnRestoreSelectAll   = new ModernButton { Text = "Tout cocher", Role = ButtonRole.Secondary };
            btnRestoreDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary };
            btnRestoreSelectAll.Click   += (s, e) => chkPanelRestore.SetAll(true);
            btnRestoreDeselectAll.Click += (s, e) => chkPanelRestore.SetAll(false);
            cardRestoreOptions.Controls.AddRange(new Control[] { chkPanelRestore, btnRestoreSelectAll, btnRestoreDeselectAll });

            btnStartRestore     = new ModernButton { Text = "Démarrer la restauration" };
            btnCancelRestore    = new ModernButton { Text = "Annuler", Role = ButtonRole.Secondary };
            btnExportRestoreLog = new ModernButton { Text = "Exporter les logs", Role = ButtonRole.Secondary };
            btnOpenRestoreLog   = new ModernButton { Text = "\U0001f4cb Voir les logs", Role = ButtonRole.Secondary };

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
            cardMigrationSource = new SettingCard
            {
                IconGlyph   = "\U0001f4be",
                Title       = "Lecteur source",
                Description = "Disque externe contenant l'ancien profil"
            };
            // FlatStyle.Flat requis : en DropDownList, le rendu par défaut ignore
            // BackColor et reste clair quel que soit le thème appliqué.
            cmbUSBDrives  = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            btnRefreshUSB = new ModernButton { Text = "\U0001f504 Actualiser", Role = ButtonRole.Secondary };
            cmbUSBDrives.SelectedIndexChanged += CmbUSBDrives_SelectedIndexChanged;
            btnRefreshUSB.Click               += BtnRefreshUSB_Click;
            cardMigrationSource.Controls.AddRange(new Control[] { cmbUSBDrives, btnRefreshUSB });

            cardMigrationBitLocker = new SettingCard
            {
                IconGlyph   = "\U0001f512",
                Title       = "Chiffrement BitLocker",
                Description = "État et déverrouillage du lecteur sélectionné"
            };
            btnUnlockBitLocker = new ModernButton { Text = "\U0001f512 Vérifier BitLocker", Role = ButtonRole.Secondary };
            lblBitLockerStatus = new Label { Text = "", AutoSize = false, TextAlign = ContentAlignment.MiddleRight };
            btnBitLocker = btnUnlockBitLocker;
            btnUnlockBitLocker.Click += BtnBitLocker_Click;
            cardMigrationBitLocker.Controls.AddRange(new Control[] { lblBitLockerStatus, btnUnlockBitLocker });

            cardMigrationProfile = new SettingCard
            {
                IconGlyph  = "\U0001f464",
                Title      = "Profil détecté",
                HeaderMode = true
            };
            lblSelectedProfile = new Label { Text = "", AutoSize = false };
            lblMigrationInfo   = new Label { Text = "", AutoSize = false, Tag = "secondary" };
            cardMigrationProfile.Controls.AddRange(new Control[] { lblSelectedProfile, lblMigrationInfo });

            cardMigrationOptions = new SettingCard
            {
                IconGlyph  = "\U0001f4cb",
                Title      = "Éléments à migrer",
                HeaderMode = true
            };
            chkPanelMigration     = new CategoryCheckPanel();
            btnMigrateSelectAll   = new ModernButton { Text = "Tout cocher", Role = ButtonRole.Secondary };
            btnMigrateDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary };
            btnMigrateSelectAll.Click   += (s, e) => chkPanelMigration.SetAll(true);
            btnMigrateDeselectAll.Click += (s, e) => chkPanelMigration.SetAll(false);
            cardMigrationOptions.Controls.AddRange(new Control[] { chkPanelMigration, btnMigrateSelectAll, btnMigrateDeselectAll });

            btnStartMigration     = new ModernButton { Text = "Démarrer la migration" };
            btnCancelMigration    = new ModernButton { Text = "Annuler", Role = ButtonRole.Secondary };
            btnExportMigrationLog = new ModernButton { Text = "Exporter les logs", Role = ButtonRole.Secondary };
            btnOpenMigrationLog   = new ModernButton { Text = "\U0001f4cb Voir les logs", Role = ButtonRole.Secondary };

            btnStartMigration.Click     += BtnStartMigration_Click;
            btnCancelMigration.Click    += BtnCancelMigration_Click;
            btnExportMigrationLog.Click += (s, e) => ExportLog(_logWindowMigration.LogBox, "migration-log.txt");
            btnOpenMigrationLog.Click   += (s, e) => OpenLogWindow(2);

            pageMigration.Controls.AddRange(new Control[] {
                cardMigrationSource, cardMigrationBitLocker, cardMigrationProfile, cardMigrationOptions,
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
