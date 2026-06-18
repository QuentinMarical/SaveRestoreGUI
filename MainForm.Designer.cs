using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
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
        private Label lblMigrationSourceTitle;
        private ComboBox cmbUSBDrives;
        private ModernButton btnRefreshUSB;
        private ModernButton btnUnlockBitLocker;
        private Label lblProfiles;
        private ListBox lstProfiles;
        private Label lblMigrationInfo;
        private ModernButton btnBitLocker;           // ← nouveau
        private Label lblBitLockerStatus;            // ← nouveau
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

        // ─── Timer clignotement BitLocker ───
        private System.Windows.Forms.Timer _bitlockerBlinkTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            // ── Fenêtre principale ──────────────────────────────────────────
            this.Text            = "SaveRestore GUI";
            this.Size            = new Size(1100, 780);
            this.MinimumSize     = new Size(900, 620);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Font            = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            // ── Sidebar ────────────────────────────────────────────────────
            sidebarPanel = new Panel { Dock = DockStyle.Left, Width = 220 };

            lblAppTitle    = new Label { Text = "SaveRestore", AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold) };
            lblAppSubtitle = new Label { Text = "Gestionnaire de profil", AutoSize = true };
            lblAppTitle.SetBounds(20, 20, 180, 30);
            lblAppSubtitle.SetBounds(20, 52, 180, 20);
            lblAppSubtitle.Tag = "secondary";

            navBackup    = new NavButton { Text = "💾  Sauvegarde",   SetBounds_Params = (20, 100, 180, 44) };
            navRestore   = new NavButton { Text = "📂  Restauration", SetBounds_Params = (20, 152, 180, 44) };
            navMigration = new NavButton { Text = "🔄  Migration USB",SetBounds_Params = (20, 204, 180, 44) };
            navBackup.SetBounds(20, 100, 180, 44);
            navRestore.SetBounds(20, 152, 180, 44);
            navMigration.SetBounds(20, 204, 180, 44);

            navBackup.Click    += (s, e) => ShowPage(0);
            navRestore.Click   += (s, e) => ShowPage(1);
            navMigration.Click += (s, e) => ShowPage(2);

            btnToggleTheme = new ModernButton { Text = "🌙  Thème sombre", AutoSize = true };
            btnToggleTheme.SetBounds(16, -44, 188, 34); // positionné dynamiquement dans ApplyTheme
            btnToggleTheme.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnToggleTheme.SetBounds(16, 680, 188, 34);
            btnToggleTheme.Click += (s, e) => { ThemeManager.Toggle(); ApplyTheme(); };

            sidebarPanel.Controls.AddRange(new Control[] {
                lblAppTitle, lblAppSubtitle,
                navBackup, navRestore, navMigration,
                btnToggleTheme
            });

            // ── Header ─────────────────────────────────────────────────────
            headerPanel = new Panel { Dock = DockStyle.Top, Height = 72 };
            lblPageTitle    = new Label { Text = "Sauvegarde", AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold) };
            lblPageSubtitle = new Label { Text = "", AutoSize = true };
            lblPageTitle.SetBounds(28, 14, 600, 28);
            lblPageSubtitle.SetBounds(28, 44, 700, 20);
            lblPageSubtitle.Tag = "secondary";
            headerPanel.Controls.AddRange(new Control[] { lblPageTitle, lblPageSubtitle });

            // ── Status bar ─────────────────────────────────────────────────
            statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            statusLabel  = new Label { Text = "Prêt", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            statusLabel.Padding = new Padding(12, 0, 0, 0);
            progressBar = new ModernProgressBar { Dock = DockStyle.Right, Width = 200, Visible = false };
            statusPanel.Controls.AddRange(new Control[] { statusLabel, progressBar });

            // ── Content panel ──────────────────────────────────────────────
            contentPanel = new Panel { Dock = DockStyle.Fill };

            // ── Pages ──────────────────────────────────────────────────────
            pageBackup    = new Panel { Dock = DockStyle.Fill, Visible = true  };
            pageRestore   = new Panel { Dock = DockStyle.Fill, Visible = false };
            pageMigration = new Panel { Dock = DockStyle.Fill, Visible = false };

            BuildPageBackup();
            BuildPageRestore();
            BuildPageMigration();

            contentPanel.Controls.AddRange(new Control[] { pageBackup, pageRestore, pageMigration });

            // ── Assemblage final ───────────────────────────────────────────
            this.Controls.AddRange(new Control[] { contentPanel, headerPanel, sidebarPanel, statusPanel });

            // ── Timer BitLocker ────────────────────────────────────────────
            _bitlockerBlinkTimer = new System.Windows.Forms.Timer(this.components)
            {
                Interval = 500,
                Enabled  = false
            };
            _bitlockerBlinkTimer.Tick += BitLockerBlinkTimer_Tick;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Construction de la page Sauvegarde
        // ══════════════════════════════════════════════════════════════════════
        private void BuildPageBackup()
        {
            // Carte destination
            cardBackupDest  = new CardPanel();
            lblBackupPath   = new Label { Text = "Dossier de destination :", AutoSize = true };
            lblBackupPath.SetBounds(16, 14, 300, 18);
            txtBackupPath   = new TextBox { PlaceholderText = @"Ex : D:\Sauvegardes" };
            btnBrowseBackup = new ModernButton { Text = "Parcourir…", Width = 120, Height = 32 };
            btnBrowseBackup.Click += BtnBrowseBackup_Click;
            cardBackupDest.Controls.AddRange(new Control[] { lblBackupPath, txtBackupPath, btnBrowseBackup });

            // Carte options
            cardBackupOptions    = new CardPanel();
            lblBackupOptionsTitle = new Label { Text = "Éléments à sauvegarder :", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            lblBackupOptionsTitle.SetBounds(16, 12, 300, 18);

            chkDocuments          = new ModernCheckBox { Text = "Documents",          Checked = true };
            chkDesktop            = new ModernCheckBox { Text = "Bureau",             Checked = true };
            chkDownloads          = new ModernCheckBox { Text = "Téléchargements",    Checked = true };
            chkPictures           = new ModernCheckBox { Text = "Images",             Checked = true };
            chkMusic              = new ModernCheckBox { Text = "Musique",            Checked = false };
            chkVideos             = new ModernCheckBox { Text = "Vidéos",             Checked = false };
            chkOutlook            = new ModernCheckBox { Text = "Données Outlook",   Checked = true };
            chkSignatures         = new ModernCheckBox { Text = "Signatures Outlook",Checked = true };
            chkStickyNotes        = new ModernCheckBox { Text = "Sticky Notes",      Checked = true };
            chkEdgeProfile        = new ModernCheckBox { Text = "Profil Edge",        Checked = true };
            chkWallpaper          = new ModernCheckBox { Text = "Fond d'écran",       Checked = true };
            chkNetworkDrives      = new ModernCheckBox { Text = "Lecteurs réseau",    Checked = true };
            chkTemplates          = new ModernCheckBox { Text = "Modèles Office",     Checked = true };
            chkOneNote            = new ModernCheckBox { Text = "OneNote",            Checked = true };
            chkExcelMacros        = new ModernCheckBox { Text = "Macros Excel",       Checked = true };
            chkSap                = new ModernCheckBox { Text = "SAP GUI",            Checked = true };
            chkOldProfile         = new ModernCheckBox { Text = "Ancien profil",      Checked = false };
            chkPublic             = new ModernCheckBox { Text = "Dossier Public",     Checked = false };
            chkIpDesktopSoftphone = new ModernCheckBox { Text = "IP Softphone",       Checked = false };

            btnSelectAll   = new ModernButton { Text = "✓ Tout sélectionner",  Width = 160, Height = 34 };
            btnDeselectAll = new ModernButton { Text = "✗ Tout désélectionner",Width = 170, Height = 34 };
            btnSelectAll.Click   += (s, e) => SetBackupAll(true);
            btnDeselectAll.Click += (s, e) => SetBackupAll(false);

            cardBackupOptions.Controls.AddRange(new Control[] {
                lblBackupOptionsTitle,
                chkDocuments, chkDesktop, chkDownloads, chkPictures, chkMusic,
                chkVideos, chkOutlook, chkSignatures, chkStickyNotes, chkEdgeProfile,
                chkWallpaper, chkNetworkDrives, chkTemplates, chkOneNote, chkExcelMacros,
                chkSap, chkOldProfile, chkPublic, chkIpDesktopSoftphone,
            // Pas de Dock — taille gérée par SyncPageSizes
            this.pageBackup = new Panel { Visible = false };

            this.cardBackupDest = new CardPanel();
            this.lblBackupPath = new Label
            {
                Text = "Dossier de destination",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.txtBackupPath = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.btnBrowseBackup = new ModernButton
            {
                Text = "Parcourir…",
                Role = ButtonRole.Secondary,
                Size = new Size(120, 32)
            };
            this.btnBrowseBackup.Click += BtnBrowseBackup_Click;
            this.cardBackupDest.Controls.Add(this.lblBackupPath);
            this.cardBackupDest.Controls.Add(this.txtBackupPath);
            this.cardBackupDest.Controls.Add(this.btnBrowseBackup);

            this.cardBackupOptions = new CardPanel();
            this.lblBackupOptionsTitle = new Label
            {
                Text = "Éléments à sauvegarder",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            this.chkDocuments    = MakeCheck("📄 Documents", true);
            this.chkDesktop      = MakeCheck("🖥️ Bureau", true);
            this.chkDownloads    = MakeCheck("⬇️ Téléchargements", true);
            this.chkPictures     = MakeCheck("🖼️ Images", true);
            this.chkMusic        = MakeCheck("🎵 Musique", true);
            this.chkVideos       = MakeCheck("🎬 Vidéos", true);

            this.chkOutlook      = MakeCheck("📧 Outlook (PST, profils)", true);
            this.chkSignatures   = MakeCheck("✍️ Signatures Outlook", true);
            this.chkStickyNotes  = MakeCheck("📌 Sticky Notes", true);
            this.chkEdgeProfile  = MakeCheck("🌐 Profil Edge", true);
            this.chkWallpaper    = MakeCheck("🖼️ Fond d'écran", true);
            this.chkNetworkDrives = MakeCheck("🔗 Lecteurs réseau", true);

            this.chkOldProfile   = MakeCheck("👤 Détecter ancien profil", false);
            this.chkTemplates    = MakeCheck("📋 Modèles Office", true);
            this.chkOneNote      = MakeCheck("📓 OneNote (registre)", true);
            this.chkExcelMacros  = MakeCheck("📊 Macros Excel (XLSTART)", true);
            this.chkSap          = MakeCheck("💼 SAP GUI", true);
            this.chkPublic       = MakeCheck("📁 Dossier Public (%public%)", true);

            // IP Desktop Softphone — désactivé, en cours de développement
            this.chkIpDesktopSoftphone = MakeCheck("📞 IP Desktop Softphone", false);
            this.chkIpDesktopSoftphone.Enabled = false;

            this.btnSelectAll = new ModernButton
            {
                Text = "Tout cocher",
                Role = ButtonRole.Secondary,
                Size = new Size(120, 34)
            };
            this.btnSelectAll.Click += (s, e) => SetAllChecks(cardBackupOptions, true);
            this.btnDeselectAll = new ModernButton
            {
                Text = "Tout décocher",
                Role = ButtonRole.Secondary,
                Size = new Size(130, 34)
            };
            this.btnDeselectAll.Click += (s, e) => SetAllChecks(cardBackupOptions, false);

            this.cardBackupOptions.Controls.Add(this.lblBackupOptionsTitle);
            this.cardBackupOptions.Controls.AddRange(new Control[]
            {
                chkDocuments, chkDesktop, chkDownloads, chkPictures, chkMusic, chkVideos,
                chkOutlook, chkSignatures, chkStickyNotes, chkEdgeProfile, chkWallpaper, chkNetworkDrives,
                chkOldProfile, chkTemplates, chkOneNote, chkExcelMacros, chkSap,
                chkPublic, chkIpDesktopSoftphone,
                btnSelectAll, btnDeselectAll
            });

            btnStartBackup    = new ModernButton { Text = "▶  Démarrer la sauvegarde", Height = 44, IsPrimary = true };
            btnCancelBackup   = new ModernButton { Text = "⬛  Annuler", Height = 44, Enabled = false };
            btnExportBackupLog= new ModernButton { Text = "📄 Exporter le log", Height = 34 };
            btnStartBackup.Click    += BtnStartBackup_Click;
            btnCancelBackup.Click   += (s, e) => CancelCurrentOperation(rtbBackupLog);
            btnExportBackupLog.Click+= (s, e) => ExportLog(rtbBackupLog, $"Sauvegarde_{DateTime.Now:yyyyMMdd_HHmm}.txt");

            rtbBackupLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageBackup.Controls.AddRange(new Control[] {
                cardBackupDest, cardBackupOptions,
                btnStartBackup, btnCancelBackup, btnExportBackupLog,
                rtbBackupLog
            });
        }

        private void SetBackupAll(bool value)
        {
            foreach (Control c in cardBackupOptions.Controls)
                if (c is ModernCheckBox chk && chk != chkOldProfile) chk.Checked = value;
        }
            this.pageRestore = new Panel { Visible = false };

            this.cardRestoreSource = new CardPanel();
            this.lblRestorePath = new Label
            {
                Text = "Dossier source de la sauvegarde",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.txtRestorePath = new TextBox
            {
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.btnBrowseRestore = new ModernButton
            {
                Text = "Parcourir…",
                Role = ButtonRole.Secondary,
                Size = new Size(120, 32)
            };
            this.btnBrowseRestore.Click += BtnBrowseRestore_Click;
            this.cardRestoreSource.Controls.Add(this.lblRestorePath);
            this.cardRestoreSource.Controls.Add(this.txtRestorePath);
            this.cardRestoreSource.Controls.Add(this.btnBrowseRestore);

            this.cardRestoreOptions = new CardPanel();
            this.lblRestoreOptionsTitle = new Label
            {
                Text = "Éléments à restaurer",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            this.chkRestoreDocuments   = MakeCheck("📄 Documents", true);
            this.chkRestoreDesktop     = MakeCheck("🖥️ Bureau", true);
            this.chkRestoreDownloads   = MakeCheck("⬇️ Téléchargements", true);
            this.chkRestorePictures    = MakeCheck("🖼️ Images", true);
            this.chkRestoreMusic       = MakeCheck("🎵 Musique", true);
            this.chkRestoreVideos      = MakeCheck("🎬 Vidéos", true);

            this.chkRestoreOutlook     = MakeCheck("📧 Outlook (PST, règles)", true);
            this.chkRestoreSignatures  = MakeCheck("✍️ Signatures Outlook", true);
            this.chkRestoreStickyNotes = MakeCheck("📌 Sticky Notes", true);
            this.chkRestoreEdgeProfile = MakeCheck("🌐 Profil Edge", true);
            this.chkRestoreWallpaper   = MakeCheck("🖼️ Fond d'écran", true);
            this.chkRestoreNetworkDrives = MakeCheck("🔗 Lecteurs réseau (info)", true);

            this.chkRestoreOneNote     = MakeCheck("📓 OneNote (registre)", true);
            this.chkRestoreExcelMacros = MakeCheck("📊 Macros Excel (XLSTART)", true);
            this.chkRestoreTemplates   = MakeCheck("📋 Modèles Office", true);
            this.chkRestoreSap         = MakeCheck("💼 SAP GUI", true);
            this.chkRestorePublic      = MakeCheck("📁 Dossier Public (%public%)", true);
            this.chkLaunchApps         = MakeCheck("🚀 Lancer les applications", true);

            // IP Desktop Softphone — désactivé, en cours de développement
            this.chkRestoreIpDesktopSoftphone = MakeCheck("📞 IP Desktop Softphone", false);
            this.chkRestoreIpDesktopSoftphone.Enabled = false;

            this.btnRestoreSelectAll = new ModernButton
            {
                Text = "Tout cocher",
                Role = ButtonRole.Secondary,
                Size = new Size(120, 34)
            };
            this.btnRestoreSelectAll.Click += (s, e) => SetAllChecks(cardRestoreOptions, true);
            this.btnRestoreDeselectAll = new ModernButton
            {
                Text = "Tout décocher",
                Role = ButtonRole.Secondary,
                Size = new Size(130, 34)
            };
            this.btnRestoreDeselectAll.Click += (s, e) => SetAllChecks(cardRestoreOptions, false);

        // ══════════════════════════════════════════════════════════════════════
        //  Construction de la page Restauration
        // ══════════════════════════════════════════════════════════════════════
        private void BuildPageRestore()
        {
            cardRestoreSource  = new CardPanel();
            lblRestorePath     = new Label { Text = "Dossier source (sauvegarde) :", AutoSize = true };
            lblRestorePath.SetBounds(16, 14, 300, 18);
            txtRestorePath     = new TextBox { PlaceholderText = @"Ex : D:\Sauvegardes\utilisateur" };
            btnBrowseRestore   = new ModernButton { Text = "Parcourir…", Width = 120, Height = 32 };
            btnBrowseRestore.Click += BtnBrowseRestore_Click;
            cardRestoreSource.Controls.AddRange(new Control[] { lblRestorePath, txtRestorePath, btnBrowseRestore });

            cardRestoreOptions    = new CardPanel();
            lblRestoreOptionsTitle = new Label { Text = "Éléments à restaurer :", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            lblRestoreOptionsTitle.SetBounds(16, 12, 300, 18);

            chkRestoreDocuments      = new ModernCheckBox { Text = "Documents",         Checked = true  };
            chkRestoreDesktop        = new ModernCheckBox { Text = "Bureau",            Checked = true  };
            chkRestoreDownloads      = new ModernCheckBox { Text = "Téléchargements",   Checked = true  };
            chkRestorePictures       = new ModernCheckBox { Text = "Images",            Checked = true  };
            chkRestoreMusic          = new ModernCheckBox { Text = "Musique",           Checked = false };
            chkRestoreVideos         = new ModernCheckBox { Text = "Vidéos",            Checked = false };
            chkRestoreOutlook        = new ModernCheckBox { Text = "Données Outlook",  Checked = true  };
            chkRestoreSignatures     = new ModernCheckBox { Text = "Signatures",        Checked = true  };
            chkRestoreStickyNotes    = new ModernCheckBox { Text = "Sticky Notes",      Checked = true  };
            chkRestoreEdgeProfile    = new ModernCheckBox { Text = "Profil Edge",       Checked = true  };
            chkRestoreWallpaper      = new ModernCheckBox { Text = "Fond d'écran",      Checked = true  };
            chkRestoreNetworkDrives  = new ModernCheckBox { Text = "Lecteurs réseau",   Checked = true  };
            chkRestoreTemplates      = new ModernCheckBox { Text = "Modèles Office",    Checked = true  };
            chkRestoreOneNote        = new ModernCheckBox { Text = "OneNote",           Checked = true  };
            chkRestoreExcelMacros    = new ModernCheckBox { Text = "Macros Excel",      Checked = true  };
            chkRestoreSap            = new ModernCheckBox { Text = "SAP GUI",           Checked = true  };
            chkRestorePublic         = new ModernCheckBox { Text = "Dossier Public",    Checked = false };
            chkLaunchApps            = new ModernCheckBox { Text = "Lancer apps",       Checked = false };
            chkRestoreIpDesktopSoftphone = new ModernCheckBox { Text = "IP Softphone",  Checked = false };

            btnRestoreSelectAll   = new ModernButton { Text = "✓ Tout sélectionner",   Width = 160, Height = 34 };
            btnRestoreDeselectAll = new ModernButton { Text = "✗ Tout désélectionner", Width = 170, Height = 34 };
            btnRestoreSelectAll.Click   += (s, e) => SetRestoreAll(true);
            btnRestoreDeselectAll.Click += (s, e) => SetRestoreAll(false);

            cardRestoreOptions.Controls.AddRange(new Control[] {
                lblRestoreOptionsTitle,
                chkRestoreDocuments, chkRestoreDesktop, chkRestoreDownloads, chkRestorePictures, chkRestoreMusic,
                chkRestoreVideos, chkRestoreOutlook, chkRestoreSignatures, chkRestoreStickyNotes, chkRestoreEdgeProfile,
                chkRestoreWallpaper, chkRestoreNetworkDrives, chkRestoreTemplates, chkRestoreOneNote, chkRestoreExcelMacros,
                chkRestoreSap, chkRestorePublic, chkLaunchApps, chkRestoreIpDesktopSoftphone,
                btnRestoreSelectAll, btnRestoreDeselectAll
            });

            btnStartRestore    = new ModernButton { Text = "▶  Démarrer la restauration", Height = 44, IsPrimary = true };
            btnCancelRestore   = new ModernButton { Text = "⬛  Annuler", Height = 44, Enabled = false };
            btnExportRestoreLog= new ModernButton { Text = "📄 Exporter le log", Height = 34 };
            btnStartRestore.Click    += BtnStartRestore_Click;
            btnCancelRestore.Click   += (s, e) => CancelCurrentOperation(rtbRestoreLog);
            btnExportRestoreLog.Click+= (s, e) => ExportLog(rtbRestoreLog, $"Restauration_{DateTime.Now:yyyyMMdd_HHmm}.txt");

            rtbRestoreLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageRestore.Controls.AddRange(new Control[] {
                cardRestoreSource, cardRestoreOptions,
                btnStartRestore, btnCancelRestore, btnExportRestoreLog,
                rtbRestoreLog
            });
        }

        private void SetRestoreAll(bool value)
        {
            foreach (Control c in cardRestoreOptions.Controls)
                if (c is ModernCheckBox chk) chk.Checked = value;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Construction de la page Migration
        // ══════════════════════════════════════════════════════════════════════
        private void BuildPageMigration()
        {
            // ── Carte source ──────────────────────────────────────────────
            cardMigrationSource   = new CardPanel();
            lblMigrationSourceTitle = new Label { Text = "Source — disque externe", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            lblMigrationSourceTitle.SetBounds(16, 12, 300, 18);

            cmbUSBDrives = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbUSBDrives.SelectedIndexChanged += CmbUSBDrives_SelectedIndexChanged;

            btnRefreshUSB = new ModernButton { Text = "🔄", Width = 40, Height = 32, ToolTipText = "Actualiser la liste" };
            btnRefreshUSB.Click += BtnRefreshUSB_Click;

            // ── Bouton BitLocker — caché par défaut, affiché uniquement si disque verrouillé ──
            btnUnlockBitLocker = new ModernButton
            {
                Text    = "🔒  Déverrouiller ce disque (BitLocker)",
                Height  = 34,
                Visible = false,   // ← caché par défaut
                Enabled = true,
                IsPrimary = false
            };
            btnUnlockBitLocker.BackColor = Color.Empty;
            btnUnlockBitLocker.Click += BtnUnlockBitLocker_Click;

            lblProfiles  = new Label { Text = "Profil utilisateur à migrer :", AutoSize = true };
            lstProfiles  = new ListBox { IntegralHeight = false };
            lblMigrationInfo = new Label { Text = "Branchez un disque externe contenant Windows.", AutoSize = false };
            lblMigrationInfo.SetBounds(16, 298, 600, 50);

            cardMigrationSource.Controls.AddRange(new Control[] {
                lblMigrationSourceTitle,
                cmbUSBDrives, btnRefreshUSB,
                btnUnlockBitLocker,
                lblProfiles, lstProfiles,
                lblMigrationInfo
            });
            this.pageMigration = new Panel { Visible = false };

            this.cardMigrationSource = new CardPanel();
            this.lblUSBDrives = new Label
            {
                Text = "Disque externe contenant Windows",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.cmbUSBDrives = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat
            };
            this.cmbUSBDrives.SelectedIndexChanged += CmbUSBDrives_SelectedIndexChanged;
            this.btnRefreshUSB = new ModernButton
            {
                Text = "🔄",
                Role = ButtonRole.Secondary,
                Size = new Size(40, 32)
            };
            this.btnRefreshUSB.Click += BtnRefreshUSB_Click;

            this.lblProfiles = new Label
            {
                Text = "Profil utilisateur à migrer",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.lstProfiles = new ListBox
            {
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.lblMigrationInfo = new Label
            {
                Text = "Sélectionnez un disque pour afficher les profils.",
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.Transparent,
                Tag = "secondary"
            };

            // ── Bouton BitLocker ──
            this.btnBitLocker = new ModernButton
            {
                Text = "🔒 Vérifier BitLocker",
                Role = ButtonRole.Secondary,
                Size = new Size(180, 32)
            };
            this.btnBitLocker.Click += BtnBitLocker_Click;

            // ── Label statut BitLocker ──
            this.lblBitLockerStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                BackColor = Color.Transparent,
                Tag = "secondary"
            };

            this.cardMigrationSource.Controls.Add(this.lblUSBDrives);
            this.cardMigrationSource.Controls.Add(this.cmbUSBDrives);
            this.cardMigrationSource.Controls.Add(this.btnRefreshUSB);
            this.cardMigrationSource.Controls.Add(this.lblProfiles);
            this.cardMigrationSource.Controls.Add(this.lstProfiles);
            this.cardMigrationSource.Controls.Add(this.lblMigrationInfo);
            this.cardMigrationSource.Controls.Add(this.btnBitLocker);       // ← ajouté
            this.cardMigrationSource.Controls.Add(this.lblBitLockerStatus); // ← ajouté

            this.cardMigrationOptions = new CardPanel();
            this.lblMigrationOptionsTitle = new Label
            {
                Text = "Éléments à migrer (mode fusion : les fichiers plus récents sont conservés)",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            this.chkMigrateDocuments   = MakeCheck("📄 Documents", true);
            this.chkMigrateDesktop     = MakeCheck("🖥️ Bureau", true);
            this.chkMigrateDownloads   = MakeCheck("⬇️ Téléchargements", true);
            this.chkMigratePictures    = MakeCheck("🖼️ Images", true);
            this.chkMigrateMusic       = MakeCheck("🎵 Musique", true);
            this.chkMigrateVideos      = MakeCheck("🎬 Vidéos", true);
            this.chkMigrateOutlook     = MakeCheck("📧 Outlook (PST)", true);
            this.chkMigrateSignatures  = MakeCheck("✍️ Signatures Outlook", true);
            this.chkMigrateExcelMacros = MakeCheck("📊 Macros Excel (XLSTART)", true);
            this.chkMigrateStickyNotes = MakeCheck("📌 Sticky Notes", true);
            this.chkMigrateEdgeProfile = MakeCheck("🌐 Profil Edge", true);
            this.chkMigrateWallpaper   = MakeCheck("🖼️ Fond d'écran", true);
            this.chkMigrateNetworkDrives = MakeCheck("🔗 Lecteurs réseau", true);
            this.chkMigrateOneNote     = MakeCheck("📓 OneNote (registre)", true);
            this.chkMigrateTemplates   = MakeCheck("📋 Modèles Office", true);
            this.chkMigrateSap         = MakeCheck("💼 SAP GUI", true);
            this.chkMigratePublic      = MakeCheck("📁 Dossier Public (%public%)", true);

            // IP Desktop Softphone — désactivé, en cours de développement
            this.chkMigrateIpDesktopSoftphone = MakeCheck("📞 IP Desktop Softphone", false);
            this.chkMigrateIpDesktopSoftphone.Enabled = false;

            this.btnMigrateSelectAll = new ModernButton
            {
                Text = "Tout cocher",
                Role = ButtonRole.Secondary,
                Size = new Size(120, 34)
            };
            this.btnMigrateSelectAll.Click += (s, e) => SetAllChecks(cardMigrationOptions, true);
            this.btnMigrateDeselectAll = new ModernButton
            {
                Text = "Tout décocher",
                Role = ButtonRole.Secondary,
                Size = new Size(130, 34)
            };
            this.btnMigrateDeselectAll.Click += (s, e) => SetAllChecks(cardMigrationOptions, false);

            // ── Carte options ─────────────────────────────────────────────
            cardMigrationOptions    = new CardPanel();
            lblMigrationOptionsTitle = new Label { Text = "Éléments à migrer :", AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            lblMigrationOptionsTitle.SetBounds(16, 12, 300, 18);

            chkMigrateDocuments      = new ModernCheckBox { Text = "Documents",        Checked = true  };
            chkMigrateDesktop        = new ModernCheckBox { Text = "Bureau",           Checked = true  };
            chkMigrateDownloads      = new ModernCheckBox { Text = "Téléchargements",  Checked = true  };
            chkMigratePictures       = new ModernCheckBox { Text = "Images",           Checked = true  };
            chkMigrateMusic          = new ModernCheckBox { Text = "Musique",          Checked = false };
            chkMigrateVideos         = new ModernCheckBox { Text = "Vidéos",           Checked = false };
            chkMigrateOutlook        = new ModernCheckBox { Text = "Données Outlook", Checked = true  };
            chkMigrateSignatures     = new ModernCheckBox { Text = "Signatures",       Checked = true  };
            chkMigrateExcelMacros    = new ModernCheckBox { Text = "Macros Excel",     Checked = true  };
            chkMigrateStickyNotes    = new ModernCheckBox { Text = "Sticky Notes",     Checked = true  };
            chkMigrateEdgeProfile    = new ModernCheckBox { Text = "Profil Edge",      Checked = true  };
            chkMigrateWallpaper      = new ModernCheckBox { Text = "Fond d'écran",     Checked = true  };
            chkMigrateNetworkDrives  = new ModernCheckBox { Text = "Lecteurs réseau",  Checked = true  };
            chkMigrateOneNote        = new ModernCheckBox { Text = "OneNote",          Checked = true  };
            chkMigrateTemplates      = new ModernCheckBox { Text = "Modèles Office",   Checked = true  };
            chkMigrateSap            = new ModernCheckBox { Text = "SAP GUI",          Checked = true  };
            chkMigratePublic         = new ModernCheckBox { Text = "Dossier Public",   Checked = false };
            chkMigrateIpDesktopSoftphone = new ModernCheckBox { Text = "IP Softphone", Checked = false };

            btnMigrateSelectAll   = new ModernButton { Text = "✓ Tout sélectionner",   Width = 160, Height = 34 };
            btnMigrateDeselectAll = new ModernButton { Text = "✗ Tout désélectionner", Width = 170, Height = 34 };
            btnMigrateSelectAll.Click   += (s, e) => SetMigrateAll(true);
            btnMigrateDeselectAll.Click += (s, e) => SetMigrateAll(false);

            cardMigrationOptions.Controls.AddRange(new Control[] {
                lblMigrationOptionsTitle,
                chkMigrateDocuments, chkMigrateDesktop, chkMigrateDownloads, chkMigratePictures, chkMigrateMusic,
                chkMigrateVideos, chkMigrateOutlook, chkMigrateSignatures, chkMigrateExcelMacros, chkMigrateStickyNotes,
                chkMigrateEdgeProfile, chkMigrateWallpaper, chkMigrateNetworkDrives, chkMigrateOneNote, chkMigrateTemplates,
                chkMigrateSap, chkMigratePublic, chkMigrateIpDesktopSoftphone,
                btnMigrateSelectAll, btnMigrateDeselectAll
            });

            btnStartMigration    = new ModernButton { Text = "▶  Démarrer la migration", Height = 44, IsPrimary = true };
            btnCancelMigration   = new ModernButton { Text = "⬛  Annuler", Height = 44, Enabled = false };
            btnExportMigrationLog= new ModernButton { Text = "📄 Exporter le log", Height = 34 };
            btnStartMigration.Click    += BtnStartMigration_Click;
            btnCancelMigration.Click   += (s, e) => CancelCurrentOperation(rtbMigrationLog);
            btnExportMigrationLog.Click+= (s, e) => ExportLog(rtbMigrationLog, $"Migration_{DateTime.Now:yyyyMMdd_HHmm}.txt");

            rtbMigrationLog = new RichTextBox { ReadOnly = true, Font = new Font("Consolas", 9f), ScrollBars = RichTextBoxScrollBars.Vertical };

            pageMigration.Controls.AddRange(new Control[] {
                cardMigrationSource, cardMigrationOptions,
                btnStartMigration, btnCancelMigration, btnExportMigrationLog,
                rtbMigrationLog
            });
        }

        private void SetMigrateAll(bool value)
        {
            foreach (Control c in cardMigrationOptions.Controls)
                if (c is ModernCheckBox chk) chk.Checked = value;
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
