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
        private ModernCheckBox chkEdgeProfile;       // renommé : était chkEdgeFav
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
        private ModernButton btnUnlockBitLocker;   // ← NOUVEAU — déverrouillage BitLocker
        private Label lblProfiles;
        private ListBox lstProfiles;
        private Label lblMigrationInfo;
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
        private System.Windows.Forms.Timer _bitlockerBlinkTimer;  // ← NOUVEAU

        #endregion
    }
}
