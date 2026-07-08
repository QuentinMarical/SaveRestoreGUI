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
        private const int MigTopCardH  = 260;  // plus de ComboBox profil, layout resserré

        // ── Carte options
        private const int CardMinH         = 260;
        private const int BtnGapY          = 14;
        private const int CardPadBot       = 16;
        private const int ChkStartY        = 44;
        private const int ChkColGap        = 12;

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
        private const int MigBitLockerSY = 120;   // juste après le bouton BitLocker (112 + 8)
        private const int MigBitLockerSH = 32;
        private const int MigSelProfY    = 160;   // label profil auto-sélectionné
        private const int MigSelProfH    = 48;    // 2-3 lignes
        private const int MigInfoY       = 216;
        private const int MigInfoH       = 30;

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
            int ch = pageBackup.ClientSize.Height;

            cardBackupDest.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtBackupPath, btnBrowseBackup);

            int optY = Margin + TopCardH + CardGap;

            // Barre d'action + progression en bas
            int progressAreaH = LogProgressH + 10;
            int actY = ch - ActionH - Margin - progressAreaH;
            int optH = Math.Max(CardMinH, actY - optY - CardGap);
            cardBackupOptions.SetBounds(Margin, optY, cw, optH);

            LayoutPanelOptionsCard(cw, optH, chkPanelBackup, btnSelectAll, btnDeselectAll);

            LayoutActionBar(Margin, actY, cw, btnStartBackup, btnCancelBackup, btnExportBackupLog);

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
            int ch = pageRestore.ClientSize.Height;

            cardRestoreSource.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtRestorePath, btnBrowseRestore);

            int optY = Margin + TopCardH + CardGap;

            int progressAreaH = LogProgressH + 10;
            int actY = ch - ActionH - Margin - progressAreaH;
            int optH = Math.Max(CardMinH, actY - optY - CardGap);
            cardRestoreOptions.SetBounds(Margin, optY, cw, optH);

            LayoutPanelOptionsCard(cw, optH, chkPanelRestore, btnRestoreSelectAll, btnRestoreDeselectAll);

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
            int ch = pageMigration.ClientSize.Height;

            cardMigrationSource.SetBounds(Margin, Margin, cw, MigTopCardH);

            int refreshW = btnRefreshUSB.Width > 0 ? btnRefreshUSB.Width : 40;
            int cmbW     = cw - InnerPad * 2 - refreshW - ChkColGap;
            cmbUSBDrives.SetBounds(InnerPad, MigCmbY, cmbW, MigCmbH);
            btnRefreshUSB.SetBounds(InnerPad + cmbW + ChkColGap, MigCmbY, refreshW, MigCmbH + 2);

            btnUnlockBitLocker.SetBounds(InnerPad, MigBitlocY, cw - InnerPad * 2, MigBitlocH);
            lblBitLockerStatus.SetBounds(InnerPad, MigBitLockerSY, cw - InnerPad * 2, MigBitLockerSH);
            lblSelectedProfile.SetBounds(InnerPad, MigSelProfY, cw - InnerPad * 2, MigSelProfH);
            lblMigrationInfo.SetBounds(InnerPad, MigInfoY, cw - InnerPad * 2, MigInfoH);

            int optY = Margin + MigTopCardH + CardGap;

            int progressAreaH = LogProgressH + 10;
            int actY = ch - ActionH - Margin - progressAreaH;
            int optH = Math.Max(CardMinH, actY - optY - CardGap);
            cardMigrationOptions.SetBounds(Margin, optY, cw, optH);

            LayoutPanelOptionsCard(cw, optH, chkPanelMigration, btnMigrateSelectAll, btnMigrateDeselectAll);

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

        private static void LayoutPanelOptionsCard(
            int cardWidth,
            int cardHeight,
            CategoryCheckPanel panel,
            Button btnAll,
            Button btnNone)
        {
            int innerW = cardWidth - InnerPad * 2;

            int bAllH = btnAll?.Height  > 0 ? btnAll.Height  : 34;
            int bNoH  = btnNone?.Height > 0 ? btnNone.Height : 34;
            int btnRowH = Math.Max(bAllH, bNoH);

            // Le panel checkboxes remplit tout l'espace disponible entre le titre et les boutons
            int chkH = Math.Max(180, cardHeight - ChkStartY - BtnGapY - btnRowH - CardPadBot);
            panel.SetBounds(InnerPad, ChkStartY, innerW, chkH);

            int btnY  = ChkStartY + chkH + BtnGapY;
            int bAllW = btnAll?.Width  > 0 ? btnAll.Width  : 120;
            int bNoW  = btnNone?.Width > 0 ? btnNone.Width : 130;

            btnAll?.SetBounds(InnerPad, btnY, bAllW, bAllH);
            btnNone?.SetBounds(InnerPad + bAllW + 8, btnY, bNoW, bNoH);
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
