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
        private ModernCheckBox chkEdgeProfile;       // renommé : était chkEdgeFavorites
        private ModernCheckBox chkWallpaper;
        private ModernCheckBox chkNetworkDrives;
        private ModernCheckBox chkOneNote;
        private ModernCheckBox chkTemplates;
        private ModernCheckBox chkExcelMacros;
        private ModernCheckBox chkSap;
        private ModernCheckBox chkPublic;            // nouveau
        private ModernCheckBox chkIpDesktopSoftphone; // nouveau — désactivé (en cours de dev)
        private ModernCheckBox chkOldProfile;
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
        private ModernCheckBox chkRestoreEdgeProfile;       // renommé : était chkRestoreEdgeFavorites
        private ModernCheckBox chkRestoreWallpaper;
        private ModernCheckBox chkRestoreOneNote;
        private ModernCheckBox chkRestoreTemplates;
        private ModernCheckBox chkRestoreExcelMacros;
        private ModernCheckBox chkRestoreSap;
        private ModernCheckBox chkRestorePublic;             // nouveau
        private ModernCheckBox chkRestoreIpDesktopSoftphone; // nouveau — désactivé
        private ModernCheckBox chkRestoreNetworkDrives;
        private ModernCheckBox chkLaunchApps;
        private ModernButton btnRestoreSelectAll;
        private ModernButton btnRestoreDeselectAll;
        private ModernButton btnStartRestore;
        private ModernButton btnCancelRestore;
        private ModernButton btnExportRestoreLog;
        private RichTextBox rtbRestoreLog;

        // ─── Page Migration USB ───
        private Panel pageMigration;
        private CardPanel cardMigrationSource;
        private Label lblUSBDrives;
        private ComboBox cmbUSBDrives;
        private ModernButton btnRefreshUSB;
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
        private ModernCheckBox chkMigrateEdgeProfile;       // renommé : était chkMigrateEdgeFavorites
        private ModernCheckBox chkMigrateWallpaper;
        private ModernCheckBox chkMigrateNetworkDrives;
        private ModernCheckBox chkMigrateOneNote;
        private ModernCheckBox chkMigrateTemplates;
        private ModernCheckBox chkMigrateSap;
        private ModernCheckBox chkMigratePublic;             // nouveau
        private ModernCheckBox chkMigrateIpDesktopSoftphone; // nouveau — désactivé
        private ModernButton btnMigrateSelectAll;
        private ModernButton btnMigrateDeselectAll;
        private ModernButton btnStartMigration;
        private ModernButton btnCancelMigration;
        private ModernButton btnExportMigrationLog;
        private RichTextBox rtbMigrationLog;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ═════════════ FENÊTRE ═════════════
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new Size(1180, 760);
            this.MinimumSize = new Size(1024, 680);
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.Font = new Font("Segoe UI", 9.5f);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Save & Restore — Outil de migration de profil";

            // ═════════════ SIDEBAR ═════════════
            this.sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240
            };

            this.lblAppTitle = new Label
            {
                Text = "💾  Save && Restore",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(240, 42),
                Location = new Point(0, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            this.lblAppSubtitle = new Label
            {
                Text = "V5 — Profils utilisateurs",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Size = new Size(240, 20),
                Location = new Point(0, 64),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            this.navBackup = new NavButton
            {
                Text = "💾   Sauvegarde",
                Location = new Point(0, 110),
                Size = new Size(240, 48)
            };
            this.navBackup.Click += (s, e) => ShowPage(0);

            this.navRestore = new NavButton
            {
                Text = "📥   Restauration",
                Location = new Point(0, 162),
                Size = new Size(240, 48)
            };
            this.navRestore.Click += (s, e) => ShowPage(1);

            this.navMigration = new NavButton
            {
                Text = "🔌   Migration USB",
                Location = new Point(0, 214),
                Size = new Size(240, 48)
            };
            this.navMigration.Click += (s, e) => ShowPage(2);

            this.btnToggleTheme = new ModernButton
            {
                Text = "🌙  Thème sombre",
                Role = ButtonRole.Secondary,
                Size = new Size(200, 38),
                Location = new Point(20, 16),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            this.btnToggleTheme.Click += (s, e) => ThemeManager.Toggle();

            var sidebarBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70
            };
            sidebarBottom.Controls.Add(this.btnToggleTheme);

            this.sidebarPanel.Controls.Add(this.lblAppTitle);
            this.sidebarPanel.Controls.Add(this.lblAppSubtitle);
            this.sidebarPanel.Controls.Add(this.navBackup);
            this.sidebarPanel.Controls.Add(this.navRestore);
            this.sidebarPanel.Controls.Add(this.navMigration);
            this.sidebarPanel.Controls.Add(sidebarBottom);

            // ═════════════ BARRE D'ÉTAT ═════════════
            this.statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };
            this.statusLabel = new Label
            {
                Text = "Prêt",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Size = new Size(500, 40),
                Location = new Point(16, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            this.progressBar = new ModernProgressBar
            {
                Size = new Size(320, 8),
                Location = new Point(0, 16),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Visible = false
            };
            this.statusPanel.Controls.Add(this.statusLabel);
            this.statusPanel.Controls.Add(this.progressBar);

            // ═════════════ EN-TÊTE CONTENU ═════════════
            this.headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80
            };
            this.lblPageTitle = new Label
            {
                Text = "Sauvegarde",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(800, 38),
                Location = new Point(28, 10),
                BackColor = Color.Transparent
            };
            this.lblPageSubtitle = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = false,
                Size = new Size(800, 22),
                Location = new Point(30, 50),
                BackColor = Color.Transparent
            };
            this.headerPanel.Controls.Add(this.lblPageTitle);
            this.headerPanel.Controls.Add(this.lblPageSubtitle);

            // ═════════════ ZONE DE CONTENU ═════════════
            this.contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = Padding.Empty
            };

            // Les pages n'ont PAS Dock=Fill — elles sont dimensionnées
            // explicitement par ApplyResponsiveLayout via contentPanel.Resize
            BuildBackupPage();
            BuildRestorePage();
            BuildMigrationPage();

            this.contentPanel.Controls.Add(this.pageBackup);
            this.contentPanel.Controls.Add(this.pageRestore);
            this.contentPanel.Controls.Add(this.pageMigration);

            // Redimensionner les pages quand contentPanel change de taille
            this.contentPanel.Resize += (s, e) => SyncPageSizes();

            // Ordre d'ancrage DockStyle : Fill en premier, puis Top, Bottom, Left
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.statusPanel);
            this.Controls.Add(this.sidebarPanel);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Synchronise la taille de chaque page avec celle de contentPanel,
        /// puis déclenche le layout responsive. Appelé à chaque resize.
        /// </summary>
        private void SyncPageSizes()
        {
            var sz = contentPanel.ClientSize;
            if (sz.Width <= 0 || sz.Height <= 0) return;
            pageBackup.Size = sz;
            pageRestore.Size = sz;
            pageMigration.Size = sz;
            ApplyResponsiveLayout();
        }

        // ───────────────────────── PAGE SAUVEGARDE ─────────────────────────
        private void BuildBackupPage()
        {
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

            this.btnStartBackup = new ModernButton
            {
                Text = "▶  Démarrer la sauvegarde",
                Role = ButtonRole.Primary,
                Size = new Size(240, 44),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            this.btnStartBackup.Click += BtnStartBackup_Click;
            this.btnCancelBackup = new ModernButton
            {
                Text = "✕  Annuler",
                Role = ButtonRole.Danger,
                Size = new Size(120, 44),
                Enabled = false
            };
            this.btnCancelBackup.Click += (s, e) => CancelCurrentOperation(rtbBackupLog);
            this.btnExportBackupLog = new ModernButton
            {
                Text = "💾 Exporter le log",
                Role = ButtonRole.Secondary,
                Size = new Size(150, 34)
            };
            this.btnExportBackupLog.Click += (s, e) => ExportLog(rtbBackupLog, "Sauvegarde_log.txt");

            this.rtbBackupLog = MakeConsole();

            this.pageBackup.Controls.Add(this.cardBackupDest);
            this.pageBackup.Controls.Add(this.cardBackupOptions);
            this.pageBackup.Controls.Add(this.btnStartBackup);
            this.pageBackup.Controls.Add(this.btnCancelBackup);
            this.pageBackup.Controls.Add(this.btnExportBackupLog);
            this.pageBackup.Controls.Add(this.rtbBackupLog);
        }

        // ───────────────────────── PAGE RESTAURATION ─────────────────────────
        private void BuildRestorePage()
        {
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

            this.cardRestoreOptions.Controls.Add(this.lblRestoreOptionsTitle);
            this.cardRestoreOptions.Controls.AddRange(new Control[]
            {
                chkRestoreDocuments, chkRestoreDesktop, chkRestoreDownloads, chkRestorePictures,
                chkRestoreMusic, chkRestoreVideos,
                chkRestoreOutlook, chkRestoreSignatures, chkRestoreStickyNotes, chkRestoreEdgeProfile,
                chkRestoreWallpaper, chkRestoreNetworkDrives,
                chkRestoreOneNote, chkRestoreExcelMacros, chkRestoreTemplates, chkRestoreSap,
                chkRestorePublic, chkLaunchApps, chkRestoreIpDesktopSoftphone,
                btnRestoreSelectAll, btnRestoreDeselectAll
            });

            this.btnStartRestore = new ModernButton
            {
                Text = "▶  Démarrer la restauration",
                Role = ButtonRole.Primary,
                Size = new Size(240, 44),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            this.btnStartRestore.Click += BtnStartRestore_Click;
            this.btnCancelRestore = new ModernButton
            {
                Text = "✕  Annuler",
                Role = ButtonRole.Danger,
                Size = new Size(120, 44),
                Enabled = false
            };
            this.btnCancelRestore.Click += (s, e) => CancelCurrentOperation(rtbRestoreLog);
            this.btnExportRestoreLog = new ModernButton
            {
                Text = "💾 Exporter le log",
                Role = ButtonRole.Secondary,
                Size = new Size(150, 34)
            };
            this.btnExportRestoreLog.Click += (s, e) => ExportLog(rtbRestoreLog, "Restauration_log.txt");

            this.rtbRestoreLog = MakeConsole();

            this.pageRestore.Controls.Add(this.cardRestoreSource);
            this.pageRestore.Controls.Add(this.cardRestoreOptions);
            this.pageRestore.Controls.Add(this.btnStartRestore);
            this.pageRestore.Controls.Add(this.btnCancelRestore);
            this.pageRestore.Controls.Add(this.btnExportRestoreLog);
            this.pageRestore.Controls.Add(this.rtbRestoreLog);
        }

        // ───────────────────────── PAGE MIGRATION USB ─────────────────────────
        private void BuildMigrationPage()
        {
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

            this.cardMigrationOptions.Controls.Add(this.lblMigrationOptionsTitle);
            this.cardMigrationOptions.Controls.AddRange(new Control[]
            {
                chkMigrateDocuments, chkMigrateDesktop, chkMigrateDownloads,
                chkMigratePictures, chkMigrateMusic, chkMigrateVideos,
                chkMigrateOutlook, chkMigrateSignatures, chkMigrateExcelMacros,
                chkMigrateStickyNotes, chkMigrateEdgeProfile, chkMigrateWallpaper,
                chkMigrateNetworkDrives, chkMigrateOneNote, chkMigrateTemplates, chkMigrateSap,
                chkMigratePublic, chkMigrateIpDesktopSoftphone,
                btnMigrateSelectAll, btnMigrateDeselectAll
            });

            this.btnStartMigration = new ModernButton
            {
                Text = "▶  Démarrer la migration",
                Role = ButtonRole.Primary,
                Size = new Size(240, 44),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
            };
            this.btnStartMigration.Click += BtnStartMigration_Click;
            this.btnCancelMigration = new ModernButton
            {
                Text = "✕  Annuler",
                Role = ButtonRole.Danger,
                Size = new Size(120, 44),
                Enabled = false
            };
            this.btnCancelMigration.Click += (s, e) => CancelCurrentOperation(rtbMigrationLog);
            this.btnExportMigrationLog = new ModernButton
            {
                Text = "💾 Exporter le log",
                Role = ButtonRole.Secondary,
                Size = new Size(150, 34)
            };
            this.btnExportMigrationLog.Click += (s, e) => ExportLog(rtbMigrationLog, "Migration_log.txt");

            this.rtbMigrationLog = MakeConsole();

            this.pageMigration.Controls.Add(this.cardMigrationSource);
            this.pageMigration.Controls.Add(this.cardMigrationOptions);
            this.pageMigration.Controls.Add(this.btnStartMigration);
            this.pageMigration.Controls.Add(this.btnCancelMigration);
            this.pageMigration.Controls.Add(this.btnExportMigrationLog);
            this.pageMigration.Controls.Add(this.rtbMigrationLog);
        }

        // ───────────────────────── Fabriques ─────────────────────────

        private static ModernCheckBox MakeCheck(string text, bool isChecked)
        {
            return new ModernCheckBox
            {
                Text = text,
                Checked = isChecked
            };
        }

        private static RichTextBox MakeConsole()
        {
            return new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = GetConsoleFont(),
                DetectUrls = false
            };
        }

        private static Font GetConsoleFont()
        {
            foreach (var name in new[] { "Cascadia Code", "Cascadia Mono" })
            {
                var font = new Font(name, 8.75f);
                if (string.Equals(font.Name, name, StringComparison.OrdinalIgnoreCase))
                    return font;
                font.Dispose();
            }
            return new Font("Segoe UI", 9f);
        }

        private static void SetAllChecks(Control container, bool value)
        {
            foreach (Control c in container.Controls)
            {
                if (c is ModernCheckBox cb) cb.Checked = value;
            }
        }

        #endregion
    }
}
