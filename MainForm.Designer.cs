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

        // ─── Page Sauvegarde ───
        private Panel pageBackup;
        private CardPanel cardBackupDest;
        private Label lblBackupPath;
        private TextBox txtBackupPath;
        private ModernButton btnBrowseBackup;
        private CardPanel cardBackupOptions;
        private Label lblBackupOptionsTitle;
        private ModernCheckBox chkDocuments;
        private ModernCheckBox chkDesktop;
        private ModernCheckBox chkDownloads;
        private ModernCheckBox chkPictures;
        private ModernCheckBox chkMusic;
        private ModernCheckBox chkVideos;
        private ModernCheckBox chkOutlook;
        private ModernCheckBox chkSignatures;
        private ModernCheckBox chkStickyNotes;
        private ModernCheckBox chkEdgeProfile;
        private ModernCheckBox chkWallpaper;
        private ModernCheckBox chkNetworkDrives;
        private ModernCheckBox chkTemplates;
        private ModernCheckBox chkOneNote;
        private ModernCheckBox chkExcelMacros;
        private ModernCheckBox chkSap;
        private ModernCheckBox chkOldProfile;
        private ModernCheckBox chkPublic;
        private ModernCheckBox chkIpDesktopSoftphone;
        private ModernButton btnSelectAll;
        private ModernButton btnDeselectAll;
        private ModernButton btnStartBackup;
        private ModernButton btnCancelBackup;
        private ModernButton btnExportBackupLog;
        private RichTextBox rtbBackupLog;

        // ─── Page Restauration ───
        private Panel pageRestore;
        private CardPanel cardRestoreSource;
        private Label lblRestorePath;
        private TextBox txtRestorePath;
        private ModernButton btnBrowseRestore;
        private CardPanel cardRestoreOptions;
        private Label lblRestoreOptionsTitle;
        private ModernCheckBox chkRestoreDocuments;
        private ModernCheckBox chkRestoreDesktop;
        private ModernCheckBox chkRestoreDownloads;
        private ModernCheckBox chkRestorePictures;
        private ModernCheckBox chkRestoreMusic;
        private ModernCheckBox chkRestoreVideos;
        private ModernCheckBox chkRestoreOutlook;
        private ModernCheckBox chkRestoreSignatures;
        private ModernCheckBox chkRestoreStickyNotes;
        private ModernCheckBox chkRestoreEdgeProfile;
        private ModernCheckBox chkRestoreWallpaper;
        private ModernCheckBox chkRestoreNetworkDrives;
        private ModernCheckBox chkRestoreTemplates;
        private ModernCheckBox chkRestoreOneNote;
        private ModernCheckBox chkRestoreExcelMacros;
        private ModernCheckBox chkRestoreSap;
        private ModernCheckBox chkRestorePublic;
        private ModernCheckBox chkLaunchApps;
        private ModernCheckBox chkRestoreIpDesktopSoftphone;
        private ModernButton btnRestoreSelectAll;
        private ModernButton btnRestoreDeselectAll;
        private ModernButton btnStartRestore;
        private ModernButton btnCancelRestore;
        private ModernButton btnExportRestoreLog;
        private RichTextBox rtbRestoreLog;

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
        private Label lblBitLockerStatus;
        private CardPanel cardMigrationOptions;
        private Label lblMigrationOptionsTitle;
        private ModernCheckBox chkMigrateDocuments;
        private ModernCheckBox chkMigrateDesktop;
        private ModernCheckBox chkMigrateDownloads;
        private ModernCheckBox chkMigratePictures;
        private ModernCheckBox chkMigrateMusic;
        private ModernCheckBox chkMigrateVideos;
        private ModernCheckBox chkMigrateOutlook;
        private ModernCheckBox chkMigrateSignatures;
        private ModernCheckBox chkMigrateExcelMacros;
        private ModernCheckBox chkMigrateStickyNotes;
        private ModernCheckBox chkMigrateEdgeProfile;
        private ModernCheckBox chkMigrateWallpaper;
        private ModernCheckBox chkMigrateNetworkDrives;
        private ModernCheckBox chkMigrateOneNote;
        private ModernCheckBox chkMigrateTemplates;
        private ModernCheckBox chkMigrateSap;
        private ModernCheckBox chkMigratePublic;
        private ModernCheckBox chkMigrateIpDesktopSoftphone;
        private ModernButton btnMigrateSelectAll;
        private ModernButton btnMigrateDeselectAll;
        private ModernButton btnStartMigration;
        private ModernButton btnCancelMigration;
        private ModernButton btnExportMigrationLog;
        private RichTextBox rtbMigrationLog;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            // ── Fenêtre principale
            Text          = "SaveRestore GUI";
            Size          = new Size(1100, 780);
            MinimumSize   = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            // ── Sidebar
            sidebarPanel = new Panel { Dock = DockStyle.Left, Width = 220 };

            lblAppTitle    = new Label { Text = "SaveRestore", AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
            lblAppSubtitle = new Label { Text = "Gestionnaire de profil", AutoSize = true, Tag = "secondary" };
            lblAppTitle.SetBounds(20, 20, 180, 30);
            lblAppSubtitle.SetBounds(20, 52, 180, 20);

            navBackup    = new NavButton { Text = "\U0001f4be  Sauvegarde" };
            navRestore   = new NavButton { Text = "\U0001f4c2  Restauration" };
            navMigration = new NavButton { Text = "\U0001f504  Migration USB" };
            navBackup.SetBounds(20, 100, 180, 44);
            navRestore.SetBounds(20, 152, 180, 44);
            navMigration.SetBounds(20, 204, 180, 44);
            navBackup.Click    += (s, e) => ShowPage(0);
            navRestore.Click   += (s, e) => ShowPage(1);
            navMigration.Click += (s, e) => ShowPage(2);

            btnToggleTheme = new ModernButton { Text = "\U0001f319  Thème sombre", AutoSize = true };
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
            statusLabel = new Label { Text = "Prêt", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            statusLabel.Padding = new Padding(12, 0, 0, 0);
            progressBar = new ModernProgressBar { Dock = DockStyle.Right, Width = 200, Visible = false };
            statusPanel.Controls.AddRange(new Control[] { statusLabel, progressBar });

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

            // ── Assemblage final
            Controls.AddRange(new Control[] { contentPanel, headerPanel, sidebarPanel, statusPanel });

            ResumeLayout(false);
            PerformLayout();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Page Sauvegarde
        // ═══════════════════════════════════════════════════════════════════
        private void BuildPageBackup()
        {
            cardBackupDest = new CardPanel();
            lblBackupPath  = new Label
            {
                Text      = "Dossier de destination",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            txtBackupPath   = new TextBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            btnBrowseBackup = new ModernButton { Text = "Parcourir\u2026", Role = ButtonRole.Secondary, Size = new Size(120, 32) };
            btnBrowseBackup.Click += BtnBrowseBackup_Click;
            cardBackupDest.Controls.AddRange(new Control[] { lblBackupPath, txtBackupPath, btnBrowseBackup });

            cardBackupOptions    = new CardPanel();
            lblBackupOptionsTitle = new Label
            {
                Text      = "Éléments à sauvegarder",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };

            chkDocuments          = MakeCheck("\U0001f4c4 Documents", true);
            chkDesktop            = MakeCheck("\U0001f5a5\ufe0f Bureau", true);
            chkDownloads          = MakeCheck("\u2b07\ufe0f Téléchargements", true);
            chkPictures           = MakeCheck("\U0001f5bc\ufe0f Images", true);
            chkMusic              = MakeCheck("\U0001f3b5 Musique", true);
            chkVideos             = MakeCheck("\U0001f3ac Vidéos", true);
            chkOutlook            = MakeCheck("\U0001f4e7 Outlook (PST, profils)", true);
            chkSignatures         = MakeCheck("\u270d\ufe0f Signatures Outlook", true);
            chkStickyNotes        = MakeCheck("\U0001f4cc Sticky Notes", true);
            chkEdgeProfile        = MakeCheck("\U0001f310 Profil Edge", true);
            chkWallpaper          = MakeCheck("\U0001f5bc\ufe0f Fond d'écran", true);
            chkNetworkDrives      = MakeCheck("\U0001f517 Lecteurs réseau", true);
            chkOldProfile         = MakeCheck("\U0001f464 Détecter ancien profil", false);
            chkTemplates          = MakeCheck("\U0001f4cb Modèles Office", true);
            chkOneNote            = MakeCheck("\U0001f4d3 OneNote (registre)", true);
            chkExcelMacros        = MakeCheck("\U0001f4ca Macros Excel (XLSTART)", true);
            chkSap                = MakeCheck("\U0001f4bc SAP GUI", true);
            chkPublic             = MakeCheck("\U0001f4c1 Dossier Public (%public%)", true);
            chkIpDesktopSoftphone = MakeCheck("\U0001f4de IP Desktop Softphone", false);
            chkIpDesktopSoftphone.Enabled = false;

            btnSelectAll   = new ModernButton { Text = "Tout cocher",   Role = ButtonRole.Secondary, Size = new Size(120, 34) };
            btnDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary, Size = new Size(130, 34) };
            btnSelectAll.Click   += (s, e) => SetAllChecks(cardBackupOptions, true);
            btnDeselectAll.Click += (s, e) => SetAllChecks(cardBackupOptions, false);

            cardBackupOptions.Controls.Add(lblBackupOptionsTitle);
            cardBackupOptions.Controls.AddRange(new Control[]
            {
                chkDocuments, chkDesktop, chkDownloads, chkPictures, chkMusic, chkVideos,
                chkOutlook, chkSignatures, chkStickyNotes, chkEdgeProfile, chkWallpaper, chkNetworkDrives,
                chkOldProfile, chkTemplates, chkOneNote, chkExcelMacros, chkSap,
                chkPublic, chkIpDesktopSoftphone,
                btnSelectAll, btnDeselectAll
            });

            btnStartBackup     = new ModernButton { Text = "\u25b6  Démarrer la sauvegarde",  Height = 44, Role = ButtonRole.Primary };
            btnCancelBackup    = new ModernButton { Text = "\u2b1b  Annuler",                  Height = 44, Enabled = false };
            btnExportBackupLog = new ModernButton { Text = "\U0001f4c4 Exporter le log",       Height = 34 };
            btnStartBackup.Click     += BtnStartBackup_Click;
            btnCancelBackup.Click    += (s, e) => CancelCurrentOperation(rtbBackupLog);
            btnExportBackupLog.Click += (s, e) => ExportLog(rtbBackupLog, $"Sauvegarde_{System.DateTime.Now:yyyyMMdd_HHmm}.txt");

            rtbBackupLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageBackup.Controls.AddRange(new Control[] {
                cardBackupDest, cardBackupOptions,
                btnStartBackup, btnCancelBackup, btnExportBackupLog,
                rtbBackupLog
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Page Restauration
        // ═══════════════════════════════════════════════════════════════════
        private void BuildPageRestore()
        {
            cardRestoreSource = new CardPanel();
            lblRestorePath    = new Label
            {
                Text      = "Dossier source de la sauvegarde",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            txtRestorePath   = new TextBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };
            btnBrowseRestore = new ModernButton { Text = "Parcourir\u2026", Role = ButtonRole.Secondary, Size = new Size(120, 32) };
            btnBrowseRestore.Click += BtnBrowseRestore_Click;
            cardRestoreSource.Controls.AddRange(new Control[] { lblRestorePath, txtRestorePath, btnBrowseRestore });

            cardRestoreOptions    = new CardPanel();
            lblRestoreOptionsTitle = new Label
            {
                Text      = "Éléments à restaurer",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };

            chkRestoreDocuments          = MakeCheck("\U0001f4c4 Documents", true);
            chkRestoreDesktop            = MakeCheck("\U0001f5a5\ufe0f Bureau", true);
            chkRestoreDownloads          = MakeCheck("\u2b07\ufe0f Téléchargements", true);
            chkRestorePictures           = MakeCheck("\U0001f5bc\ufe0f Images", true);
            chkRestoreMusic              = MakeCheck("\U0001f3b5 Musique", true);
            chkRestoreVideos             = MakeCheck("\U0001f3ac Vidéos", true);
            chkRestoreOutlook            = MakeCheck("\U0001f4e7 Outlook (PST, règles)", true);
            chkRestoreSignatures         = MakeCheck("\u270d\ufe0f Signatures Outlook", true);
            chkRestoreStickyNotes        = MakeCheck("\U0001f4cc Sticky Notes", true);
            chkRestoreEdgeProfile        = MakeCheck("\U0001f310 Profil Edge", true);
            chkRestoreWallpaper          = MakeCheck("\U0001f5bc\ufe0f Fond d'écran", true);
            chkRestoreNetworkDrives      = MakeCheck("\U0001f517 Lecteurs réseau (info)", true);
            chkRestoreOneNote            = MakeCheck("\U0001f4d3 OneNote (registre)", true);
            chkRestoreExcelMacros        = MakeCheck("\U0001f4ca Macros Excel (XLSTART)", true);
            chkRestoreTemplates          = MakeCheck("\U0001f4cb Modèles Office", true);
            chkRestoreSap                = MakeCheck("\U0001f4bc SAP GUI", true);
            chkRestorePublic             = MakeCheck("\U0001f4c1 Dossier Public (%public%)", true);
            chkLaunchApps               = MakeCheck("\U0001f680 Lancer les applications", true);
            chkRestoreIpDesktopSoftphone = MakeCheck("\U0001f4de IP Desktop Softphone", false);
            chkRestoreIpDesktopSoftphone.Enabled = false;

            btnRestoreSelectAll   = new ModernButton { Text = "Tout cocher",   Role = ButtonRole.Secondary, Size = new Size(120, 34) };
            btnRestoreDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary, Size = new Size(130, 34) };
            btnRestoreSelectAll.Click   += (s, e) => SetAllChecks(cardRestoreOptions, true);
            btnRestoreDeselectAll.Click += (s, e) => SetAllChecks(cardRestoreOptions, false);

            cardRestoreOptions.Controls.Add(lblRestoreOptionsTitle);
            cardRestoreOptions.Controls.AddRange(new Control[]
            {
                chkRestoreDocuments, chkRestoreDesktop, chkRestoreDownloads, chkRestorePictures, chkRestoreMusic, chkRestoreVideos,
                chkRestoreOutlook, chkRestoreSignatures, chkRestoreStickyNotes, chkRestoreEdgeProfile, chkRestoreWallpaper, chkRestoreNetworkDrives,
                chkRestoreOneNote, chkRestoreExcelMacros, chkRestoreTemplates, chkRestoreSap,
                chkRestorePublic, chkLaunchApps, chkRestoreIpDesktopSoftphone,
                btnRestoreSelectAll, btnRestoreDeselectAll
            });

            btnStartRestore     = new ModernButton { Text = "\u25b6  Démarrer la restauration", Height = 44, Role = ButtonRole.Primary };
            btnCancelRestore    = new ModernButton { Text = "\u2b1b  Annuler",                   Height = 44, Enabled = false };
            btnExportRestoreLog = new ModernButton { Text = "\U0001f4c4 Exporter le log",        Height = 34 };
            btnStartRestore.Click     += BtnStartRestore_Click;
            btnCancelRestore.Click    += (s, e) => CancelCurrentOperation(rtbRestoreLog);
            btnExportRestoreLog.Click += (s, e) => ExportLog(rtbRestoreLog, $"Restauration_{System.DateTime.Now:yyyyMMdd_HHmm}.txt");

            rtbRestoreLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageRestore.Controls.AddRange(new Control[] {
                cardRestoreSource, cardRestoreOptions,
                btnStartRestore, btnCancelRestore, btnExportRestoreLog,
                rtbRestoreLog
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Page Migration
        // ═══════════════════════════════════════════════════════════════════
        private void BuildPageMigration()
        {
            // ── Carte source
            cardMigrationSource = new CardPanel();

            lblUSBDrives = new Label
            {
                Text      = "Disque externe contenant Windows",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };

            cmbUSBDrives = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9.5f),
                FlatStyle     = FlatStyle.Flat
            };
            cmbUSBDrives.SelectedIndexChanged += CmbUSBDrives_SelectedIndexChanged;

            btnRefreshUSB = new ModernButton { Text = "\U0001f504", Role = ButtonRole.Secondary, Size = new Size(40, 32) };
            btnRefreshUSB.Click += BtnRefreshUSB_Click;

            btnUnlockBitLocker = new ModernButton
            {
                Text    = "\U0001f512 Déverrouiller ce disque (BitLocker)",
                Height  = 34,
                Visible = true,
                Role    = ButtonRole.Secondary
            };
            btnUnlockBitLocker.Click += (s, e) => BtnBitLocker_Click(s, e);

            lblProfiles = new Label
            {
                Text      = "Profil utilisateur à migrer",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            lstProfiles = new ListBox { Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };

            lblMigrationInfo = new Label
            {
                Text      = "Sélectionnez un disque pour afficher les profils.",
                Font      = new Font("Segoe UI", 8.5f),
                BackColor = Color.Transparent,
                Tag       = "secondary"
            };

            lblBitLockerStatus = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8.5f),
                AutoSize  = false,
                BackColor = Color.Transparent,
                Tag       = "secondary"
            };

            cardMigrationSource.Controls.Add(lblUSBDrives);
            cardMigrationSource.Controls.Add(cmbUSBDrives);
            cardMigrationSource.Controls.Add(btnRefreshUSB);
            cardMigrationSource.Controls.Add(btnUnlockBitLocker);
            cardMigrationSource.Controls.Add(lblProfiles);
            cardMigrationSource.Controls.Add(lstProfiles);
            cardMigrationSource.Controls.Add(lblBitLockerStatus);
            cardMigrationSource.Controls.Add(lblMigrationInfo);

            // ── Carte options
            cardMigrationOptions = new CardPanel();
            lblMigrationOptionsTitle = new Label
            {
                Text      = "Éléments à migrer (mode fusion : les fichiers plus récents sont conservés)",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = true,
                BackColor = Color.Transparent
            };

            chkMigrateDocuments          = MakeCheck("\U0001f4c4 Documents", true);
            chkMigrateDesktop            = MakeCheck("\U0001f5a5\ufe0f Bureau", true);
            chkMigrateDownloads          = MakeCheck("\u2b07\ufe0f Téléchargements", true);
            chkMigratePictures           = MakeCheck("\U0001f5bc\ufe0f Images", true);
            chkMigrateMusic              = MakeCheck("\U0001f3b5 Musique", true);
            chkMigrateVideos             = MakeCheck("\U0001f3ac Vidéos", true);
            chkMigrateOutlook            = MakeCheck("\U0001f4e7 Outlook (PST)", true);
            chkMigrateSignatures         = MakeCheck("\u270d\ufe0f Signatures Outlook", true);
            chkMigrateExcelMacros        = MakeCheck("\U0001f4ca Macros Excel (XLSTART)", true);
            chkMigrateStickyNotes        = MakeCheck("\U0001f4cc Sticky Notes", true);
            chkMigrateEdgeProfile        = MakeCheck("\U0001f310 Profil Edge", true);
            chkMigrateWallpaper          = MakeCheck("\U0001f5bc\ufe0f Fond d'écran", true);
            chkMigrateNetworkDrives      = MakeCheck("\U0001f517 Lecteurs réseau", true);
            chkMigrateOneNote            = MakeCheck("\U0001f4d3 OneNote (registre)", true);
            chkMigrateTemplates          = MakeCheck("\U0001f4cb Modèles Office", true);
            chkMigrateSap                = MakeCheck("\U0001f4bc SAP GUI", true);
            chkMigratePublic             = MakeCheck("\U0001f4c1 Dossier Public (%public%)", true);
            chkMigrateIpDesktopSoftphone = MakeCheck("\U0001f4de IP Desktop Softphone", false);
            chkMigrateIpDesktopSoftphone.Enabled = false;

            btnMigrateSelectAll   = new ModernButton { Text = "Tout cocher",   Role = ButtonRole.Secondary, Size = new Size(120, 34) };
            btnMigrateDeselectAll = new ModernButton { Text = "Tout décocher", Role = ButtonRole.Secondary, Size = new Size(130, 34) };
            btnMigrateSelectAll.Click   += (s, e) => SetAllChecks(cardMigrationOptions, true);
            btnMigrateDeselectAll.Click += (s, e) => SetAllChecks(cardMigrationOptions, false);

            cardMigrationOptions.Controls.Add(lblMigrationOptionsTitle);
            cardMigrationOptions.Controls.AddRange(new Control[]
            {
                chkMigrateDocuments, chkMigrateDesktop, chkMigrateDownloads, chkMigratePictures, chkMigrateMusic,
                chkMigrateVideos, chkMigrateOutlook, chkMigrateSignatures, chkMigrateExcelMacros, chkMigrateStickyNotes,
                chkMigrateEdgeProfile, chkMigrateWallpaper, chkMigrateNetworkDrives, chkMigrateOneNote, chkMigrateTemplates,
                chkMigrateSap, chkMigratePublic, chkMigrateIpDesktopSoftphone,
                btnMigrateSelectAll, btnMigrateDeselectAll
            });

            btnStartMigration     = new ModernButton { Text = "\u25b6  Démarrer la migration", Height = 44, Role = ButtonRole.Primary };
            btnCancelMigration    = new ModernButton { Text = "\u2b1b  Annuler",                Height = 44, Enabled = false };
            btnExportMigrationLog = new ModernButton { Text = "\U0001f4c4 Exporter le log",     Height = 34 };
            btnStartMigration.Click     += BtnStartMigration_Click;
            btnCancelMigration.Click    += (s, e) => CancelCurrentOperation(rtbMigrationLog);
            btnExportMigrationLog.Click += (s, e) => ExportLog(rtbMigrationLog, $"Migration_{System.DateTime.Now:yyyyMMdd_HHmm}.txt");

            rtbMigrationLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageMigration.Controls.AddRange(new Control[] {
                cardMigrationSource, cardMigrationOptions,
                btnStartMigration, btnCancelMigration, btnExportMigrationLog,
                rtbMigrationLog
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
