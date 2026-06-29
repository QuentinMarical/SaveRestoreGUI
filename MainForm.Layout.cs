using System;
using System.Drawing;
using System.Windows.Forms;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Mise en page responsive : toute la géométrie est calculée ici, une seule fois,
    /// à partir de page.ClientSize.
    /// </summary>
    public partial class MainForm
    {
        // ── Marges et espacements
        private new const int Margin   = 28;
        private const int CardGap      = 14;
        private const int InnerPad     = 16;

        // ── Carte du haut
        private const int TopCardH     = 90;
        private const int MigTopCardH  = 340;  // carte source Migration

        // ── Carte options (CategoryCheckPanel)
        private const int ChkPanelH    = 320;  // hauteur fixe du CategoryCheckPanel
        private const int BtnGapY      = 14;
        private const int CardPadBot   = 16;
        private const int ChkStartY    = 44;
        private const int ChkColGap    = 12;

        // ── Barre d'actions
        private const int ActionH      = 44;
        private const int BtnStartW    = 230;
        private const int BtnCancelW   = 120;
        private const int BtnExportW   = 150;

        // ── Console log
        private const int LogMinH      = 120;
        private const int LogMarginBot = 12;

        // ── Migration : zones internes de la carte source
        //  Y=12  : lblUSBDrives
        //  Y=40  : cmbUSBDrives + btnRefreshUSB
        //  Y=78  : btnUnlockBitLocker (visible si disque verrouillé)
        //  Y=120 : lblProfiles
        //  Y=142 : lstProfiles (H=128 → fin à 270)
        //  Y=278 : lblBitLockerStatus (pleine largeur)
        //  Y=318 : lblMigrationInfo
        //  carte = 340 px
        private const int MigCmbY           = 40;
        private const int MigCmbH           = 30;
        private const int MigBitlocY        = 78;
        private const int MigBitlocH        = 34;
        private const int MigLblProfY       = 120;
        private const int MigListY          = 142;
        private const int MigListH          = 128;
        private const int MigBitLockerSY    = 278;
        private const int MigBitLockerSH    = 32;
        private const int MigInfoY          = 318;
        private const int MigInfoH          = 16;

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

            int optY  = Margin + TopCardH + CardGap;
            int optH  = LayoutPanelOptionsCard(cw, chkPanelBackup, btnSelectAll, btnDeselectAll);
            cardBackupOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartBackup, btnCancelBackup, btnExportBackupLog);

            int logY = actY + ActionH + CardGap;
            rtbBackupLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
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

            int optY  = Margin + TopCardH + CardGap;
            int optH  = LayoutPanelOptionsCard(cw, chkPanelRestore, btnRestoreSelectAll, btnRestoreDeselectAll);
            cardRestoreOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartRestore, btnCancelRestore, btnExportRestoreLog);

            int logY = actY + ActionH + CardGap;
            rtbRestoreLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
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

            // ComboBox + refresh
            int refreshW = btnRefreshUSB.Width > 0 ? btnRefreshUSB.Width : 40;
            int cmbW     = cw - InnerPad * 2 - refreshW - ChkColGap;
            cmbUSBDrives.SetBounds(InnerPad, MigCmbY, cmbW, MigCmbH);
            btnRefreshUSB.SetBounds(InnerPad + cmbW + ChkColGap, MigCmbY, refreshW, MigCmbH + 2);

            // Bouton déverrouillage BitLocker
            btnUnlockBitLocker.SetBounds(InnerPad, MigBitlocY, cw - InnerPad * 2, MigBitlocH);

            // Label + ListBox profils
            lblProfiles.SetBounds(InnerPad, MigLblProfY, cw - InnerPad * 2, 20);
            lstProfiles.SetBounds(InnerPad, MigListY,    cw - InnerPad * 2, MigListH);

            // Label statut BitLocker (pleine largeur, btnBitLocker supprimé)
            lblBitLockerStatus.SetBounds(
                InnerPad,
                MigBitLockerSY,
                cw - InnerPad * 2,
                MigBitLockerSH);

            // Label info (bas de carte)
            lblMigrationInfo.SetBounds(InnerPad, MigInfoY, cw - InnerPad * 2, MigInfoH);

            // Carte options
            int optY  = Margin + MigTopCardH + CardGap;
            int optH  = LayoutPanelOptionsCard(cw, chkPanelMigration, btnMigrateSelectAll, btnMigrateDeselectAll);
            cardMigrationOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartMigration, btnCancelMigration, btnExportMigrationLog);

            int logY = actY + ActionH + CardGap;
            rtbMigrationLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
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

        /// <summary>
        /// Positionne un CategoryCheckPanel dans sa carte options,
        /// puis les boutons Tout cocher / Tout décocher en dessous.
        /// Retourne la hauteur totale calculée de la carte.
        /// </summary>
        private static int LayoutPanelOptionsCard(
            int cardWidth,
            CategoryCheckPanel panel,
            Button btnAll,
            Button btnNone)
        {
            int innerW = cardWidth - InnerPad * 2;

            // Le panel commence sous le titre de la carte, à ChkStartY
            panel.SetBounds(InnerPad, ChkStartY, innerW, ChkPanelH);

            int btnY  = ChkStartY + ChkPanelH + BtnGapY;
            int bAllW = btnAll  != null && btnAll.Width  > 0 ? btnAll.Width  : 120;
            int bAllH = btnAll  != null && btnAll.Height > 0 ? btnAll.Height : 34;
            int bNoW  = btnNone != null && btnNone.Width > 0 ? btnNone.Width : 130;
            int bNoH  = btnNone != null && btnNone.Height > 0 ? btnNone.Height : 34;

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
            int exportX = left + availableWidth - BtnExportW;
            export.SetBounds(exportX, top + (ActionH - 34) / 2, BtnExportW, 34);
        }
    }
}
