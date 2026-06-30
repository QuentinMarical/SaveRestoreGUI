using System;
using System.Drawing;
using System.Windows.Forms;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    public partial class MainForm
    {
        // ── Marges et espacements
        private new const int Margin   = 28;
        private const int CardGap      = 14;
        private const int InnerPad     = 16;

        // ── Carte du haut
        private const int TopCardH     = 90;
        private const int MigTopCardH  = 340;

        // ── Carte options
        private const int CardMinH         = 260; // hauteur minimale utile
        private const int BtnGapY          = 14;
        private const int CardPadBot       = 16;
        private const int ChkStartY        = 44;
        private const int ChkColGap        = 12;
        private const int BrowserPickerH   = 28;
        private const int BrowserPickerGapY= 8;

        // ── Barre d'actions
        private const int ActionH    = 44;
        private const int BtnStartW  = 230;
        private const int BtnCancelW = 120;
        private const int BtnExportW = 150;

        // ── Console log
        private const int LogMinH       = 120;
        private const int LogMarginBot  = 12;
        private const int LogToggleH    = 28;
        private const int LogToggleGapY = 6;
        private const int LogProgressH  = 20;

        // ── Migration
        private const int MigCmbY        = 40;
        private const int MigCmbH        = 30;
        private const int MigBitlocY     = 78;
        private const int MigBitlocH     = 34;
        private const int MigLblProfY    = 120;
        private const int MigListY       = 142;
        private const int MigListH       = 128;
        private const int MigBitLockerSY = 278;
        private const int MigBitLockerSH = 32;
        private const int MigInfoY       = 318;
        private const int MigInfoH       = 16;

        public void ApplyResponsiveLayout()
        {
            LayoutBackupPage();
            LayoutRestorePage();
            LayoutMigrationPage();
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE SAUVEGARDE
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutBackupPage()
        {
            if (pageBackup.ClientSize.Width <= 0) return;
            int W  = pageBackup.ClientSize.Width;
            int H  = pageBackup.ClientSize.Height;
            int cw = W - Margin * 2;

            cardBackupDest.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtBackupPath, btnBrowseBackup);

            int optY = Margin + TopCardH + CardGap;
            int optH = LayoutPanelOptionsCard(cw, chkPanelBackup, btnBrowserPickerBackup, btnSelectAll, btnDeselectAll);
            optH = Math.Max(CardMinH, optH);
            cardBackupOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartBackup, btnCancelBackup, btnExportBackupLog);

            int toggleY = actY + ActionH + LogToggleGapY;
            btnToggleBackupLog.SetBounds(Margin, toggleY, 160, LogToggleH);
            btnToggleBackupLog.Text = _backupLogCollapsed ? "▼ Afficher les logs" : "▲ Masquer les logs";

            int logY = toggleY + LogToggleH + LogToggleGapY;
            int logH = _backupLogCollapsed
                ? LogCollapsedHeight
                : Math.Max(LogMinH, H - logY - LogMarginBot - LogProgressH - 4);
            rtbBackupLog.SetBounds(Margin, logY, cw, logH);

            int progressY = logY + logH + 4;
            progressBar.SetBounds(Margin, progressY, cw - 80, LogProgressH);
            lblProgressPercent.SetBounds(Margin + cw - 80, progressY, 80, LogProgressH);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE RESTAURATION
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutRestorePage()
        {
            if (pageRestore.ClientSize.Width <= 0) return;
            int W  = pageRestore.ClientSize.Width;
            int H  = pageRestore.ClientSize.Height;
            int cw = W - Margin * 2;

            cardRestoreSource.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtRestorePath, btnBrowseRestore);

            int optY = Margin + TopCardH + CardGap;
            int optH = LayoutPanelOptionsCard(cw, chkPanelRestore, btnBrowserPickerRestore, btnRestoreSelectAll, btnRestoreDeselectAll);
            optH = Math.Max(CardMinH, optH);
            cardRestoreOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartRestore, btnCancelRestore, btnExportRestoreLog);

            int toggleY = actY + ActionH + LogToggleGapY;
            btnToggleRestoreLog.SetBounds(Margin, toggleY, 160, LogToggleH);
            btnToggleRestoreLog.Text = _restoreLogCollapsed ? "▼ Afficher les logs" : "▲ Masquer les logs";

            int logY = toggleY + LogToggleH + LogToggleGapY;
            int logH = _restoreLogCollapsed
                ? LogCollapsedHeight
                : Math.Max(LogMinH, H - logY - LogMarginBot - LogProgressH - 4);
            rtbRestoreLog.SetBounds(Margin, logY, cw, logH);

            int progressY = logY + logH + 4;
            progressBar.SetBounds(Margin, progressY, cw - 80, LogProgressH);
            lblProgressPercent.SetBounds(Margin + cw - 80, progressY, 80, LogProgressH);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE MIGRATION
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutMigrationPage()
        {
            if (pageMigration.ClientSize.Width <= 0) return;
            int W  = pageMigration.ClientSize.Width;
            int H  = pageMigration.ClientSize.Height;
            int cw = W - Margin * 2;

            cardMigrationSource.SetBounds(Margin, Margin, cw, MigTopCardH);

            int refreshW = btnRefreshUSB.Width > 0 ? btnRefreshUSB.Width : 40;
            int cmbW     = cw - InnerPad * 2 - refreshW - ChkColGap;
            cmbUSBDrives.SetBounds(InnerPad, MigCmbY, cmbW, MigCmbH);
            btnRefreshUSB.SetBounds(InnerPad + cmbW + ChkColGap, MigCmbY, refreshW, MigCmbH + 2);

            btnUnlockBitLocker.SetBounds(InnerPad, MigBitlocY, cw - InnerPad * 2, MigBitlocH);
            lblProfiles.SetBounds(InnerPad, MigLblProfY, cw - InnerPad * 2, 20);
            lstProfiles.SetBounds(InnerPad, MigListY,    cw - InnerPad * 2, MigListH);
            lblBitLockerStatus.SetBounds(InnerPad, MigBitLockerSY, cw - InnerPad * 2, MigBitLockerSH);
            lblMigrationInfo.SetBounds(InnerPad, MigInfoY, cw - InnerPad * 2, MigInfoH);

            int optY = Margin + MigTopCardH + CardGap;
            int optH = LayoutPanelOptionsCard(cw, chkPanelMigration, null, btnMigrateSelectAll, btnMigrateDeselectAll);
            optH = Math.Max(CardMinH, optH);
            cardMigrationOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartMigration, btnCancelMigration, btnExportMigrationLog);

            int toggleY = actY + ActionH + LogToggleGapY;
            btnToggleMigrationLog.SetBounds(Margin, toggleY, 160, LogToggleH);
            btnToggleMigrationLog.Text = _migrationLogCollapsed ? "▼ Afficher les logs" : "▲ Masquer les logs";

            int logY = toggleY + LogToggleH + LogToggleGapY;
            int logH = _migrationLogCollapsed
                ? LogCollapsedHeight
                : Math.Max(LogMinH, H - logY - LogMarginBot - LogProgressH - 4);
            rtbMigrationLog.SetBounds(Margin, logY, cw, logH);

            int progressY = logY + logH + 4;
            progressBar.SetBounds(Margin, progressY, cw - 80, LogProgressH);
            lblProgressPercent.SetBounds(Margin + cw - 80, progressY, 80, LogProgressH);
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static void LayoutDestCard(int cardWidth, TextBox txt, Button browse)
        {
            int innerW  = cardWidth - InnerPad * 2;
            int browseW = browse.Width > 0 ? browse.Width : 120;
            int txtW    = Math.Max(80, innerW - browseW - ChkColGap);
            txt.SetBounds(InnerPad, 38, txtW, 30);
            browse.SetBounds(InnerPad + txtW + ChkColGap, 36, browseW, 32);
        }

        private static int LayoutPanelOptionsCard(
            int cardWidth,
            CategoryCheckPanel panel,
            BrowserPickerButton? browserPicker,
            Button btnAll,
            Button btnNone)
        {
            int innerW = cardWidth - InnerPad * 2;
            panel.SetBounds(InnerPad, ChkStartY, innerW, Math.Max(CardMinH - 80, 180));

            int nextY = ChkStartY + panel.Height;

            if (browserPicker != null)
            {
                nextY += BrowserPickerGapY;
                browserPicker.SetBounds(InnerPad, nextY, 240, BrowserPickerH);
                nextY += BrowserPickerH;
            }

            int btnY  = nextY + BtnGapY;
            int bAllW = btnAll?.Width  > 0 ? btnAll.Width  : 120;
            int bAllH = btnAll?.Height > 0 ? btnAll.Height : 34;
            int bNoW  = btnNone?.Width > 0 ? btnNone.Width : 130;
            int bNoH  = btnNone?.Height > 0 ? btnNone.Height : 34;

            btnAll?.SetBounds(InnerPad, btnY, bAllW, bAllH);
            btnNone?.SetBounds(InnerPad + bAllW + 8, btnY, bNoW, bNoH);

            return btnY + Math.Max(bAllH, bNoH) + CardPadBot;
        }

        private static void LayoutActionBar(
            int left, int top, int availableWidth,
            Button start, Button cancel, Button export)
        {
            start.SetBounds(left, top, BtnStartW, ActionH);
            cancel.SetBounds(left + BtnStartW + 8, top, BtnCancelW, ActionH);
            export.SetBounds(left + availableWidth - BtnExportW, top + (ActionH - 34) / 2, BtnExportW, 34);
        }
    }
}
