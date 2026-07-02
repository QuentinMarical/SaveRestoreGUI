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
        private const int CardMinH         = 260;
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
        private const int BtnLogsW   = 140;

        // ── Barre de progression (overlay dans contentPanel)
        private const int LogProgressH = 20;

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
            LayoutProgressOverlay();
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE SAUVEGARDE
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutBackupPage()
        {
            if (pageBackup.ClientSize.Width <= 0) return;
            int cw = pageBackup.ClientSize.Width - Margin * 2;

            cardBackupDest.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtBackupPath, btnBrowseBackup);

            int optY = Margin + TopCardH + CardGap;
            int optH = LayoutPanelOptionsCard(cw, chkPanelBackup, btnBrowserPickerBackup, btnSelectAll, btnDeselectAll);
            optH = Math.Max(CardMinH, optH);
            cardBackupOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartBackup, btnCancelBackup, btnExportBackupLog);

            // Bouton "Voir les logs" à droite du bouton Exporter
            btnOpenBackupLog.SetBounds(
                Margin + cw - BtnExportW - BtnLogsW - 8,
                actY + (ActionH - 34) / 2,
                BtnLogsW, 34);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE RESTAURATION
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutRestorePage()
        {
            if (pageRestore.ClientSize.Width <= 0) return;
            int cw = pageRestore.ClientSize.Width - Margin * 2;

            cardRestoreSource.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtRestorePath, btnBrowseRestore);

            int optY = Margin + TopCardH + CardGap;
            int optH = LayoutPanelOptionsCard(cw, chkPanelRestore, btnBrowserPickerRestore, btnRestoreSelectAll, btnRestoreDeselectAll);
            optH = Math.Max(CardMinH, optH);
            cardRestoreOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartRestore, btnCancelRestore, btnExportRestoreLog);

            btnOpenRestoreLog.SetBounds(
                Margin + cw - BtnExportW - BtnLogsW - 8,
                actY + (ActionH - 34) / 2,
                BtnLogsW, 34);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE MIGRATION
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutMigrationPage()
        {
            if (pageMigration.ClientSize.Width <= 0) return;
            int cw = pageMigration.ClientSize.Width - Margin * 2;

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

            btnOpenMigrationLog.SetBounds(
                Margin + cw - BtnExportW - BtnLogsW - 8,
                actY + (ActionH - 34) / 2,
                BtnLogsW, 34);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BARRE DE PROGRESSION (overlay dans contentPanel)
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutProgressOverlay()
        {
            int cw = contentPanel.ClientSize.Width;
            int ch = contentPanel.ClientSize.Height;
            if (cw <= 0 || ch <= 0) return;

            int barW = cw - Margin * 2 - 80;
            int y    = ch - LogProgressH - 6;
            progressBar.SetBounds(Margin, y, barW, LogProgressH);
            lblProgressPercent.SetBounds(Margin + barW, y, 80, LogProgressH);
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
