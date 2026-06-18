using System;
using System.Drawing;
using System.Windows.Forms;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Mise en page responsive : toute la géométrie est calculée ici, une seule fois,
    /// à partir de page.ClientSize. Chaque page a sa propre méthode de layout
    /// pour garantir un positionnement précis et indépendant.
    /// </summary>
    public partial class MainForm
    {
        // ── Marges et espacements ──────────────────────────────────────────
        private new const int Margin      = 28;   // marge gauche/droite des pages
        private const int CardGap         = 14;   // espace vertical entre les cartes
        private const int InnerPad        = 16;   // padding interne des cartes

        // ── Carte du haut ─────────────────────────────────────────────────
        private const int TopCardH        = 90;   // hauteur carte Backup/Restore
        private const int MigTopCardH     = 340;  // hauteur carte source Migration

        // ── Carte des options (checkboxes) ────────────────────────────────
        private const int ChkLabelY       = 12;
        private const int ChkStartY       = 44;
        private const int ChkStepY        = 32;
        private const int ChkH            = 28;
        private const int ChkColGap       = 12;
        private const int BtnGapY         = 14;
        private const int CardPadBot      = 16;

        // ── Barre d'actions ───────────────────────────────────────────────
        private const int ActionH         = 44;
        private const int BtnStartW       = 230;
        private const int BtnCancelW      = 120;
        private const int BtnExportW      = 150;

        // ── Console log ───────────────────────────────────────────────────
        private const int LogMinH         = 120;
        private const int LogMarginBot    = 12;

        // ── Migration : zones internes de la carte source ─────────────────
        //  Y=12  : lblUSBDrives
        //  Y=38  : cmbUSBDrives + btnRefreshUSB
        //  Y=76  : btnUnlockBitLocker (visible si disque verrouillé BitLocker)
        //  Y=118 : lblProfiles
        //  Y=140 : lstProfiles (hauteur 128 → bas à Y=268)
        //  Y=220 : btnBitLocker + lblBitLockerStatus  (dans lstProfiles, superposé si Visible)
        //  Y=276 : lblMigrationInfo
        //  carte = 340 px de haut
        private const int MigCmbY         = 38;
        private const int MigCmbH         = 30;
        private const int MigBitlocY      = 76;   // btnUnlockBitLocker
        private const int MigBitlocH      = 34;
        private const int MigLblProfY     = 118;
        private const int MigListY        = 140;
        private const int MigListH        = 128;
        private const int MigBitLockerY   = 220;  // btnBitLocker (vérif statut)
        private const int MigBitLockerH   = 32;
        private const int MigBitLockerSY  = 220;  // lblBitLockerStatus
        private const int MigBitLockerSH  = 32;
        private const int MigInfoY        = 276;
        private const int MigInfoH        = 40;

        // ─────────────────────────────────────────────────────────────────

        public void ApplyResponsiveLayout()
        {
            LayoutBackupPage();
            LayoutRestorePage();
            LayoutMigrationPage();
        }

        // ══════════════════════════════════════════════════════════════════
        // PAGE SAUVEGARDE
        // ══════════════════════════════════════════════════════════════════
        private void LayoutBackupPage()
        {
            if (pageBackup.ClientSize.Width <= 0) return;
            int W  = pageBackup.ClientSize.Width;
            int H  = pageBackup.ClientSize.Height;
            int cw = W - Margin * 2;

            cardBackupDest.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtBackupPath, btnBrowseBackup);

            int optY = Margin + TopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkDocuments,  chkDesktop,       chkDownloads,   chkPictures,           chkMusic },
                new[] { chkVideos,     chkOutlook,        chkSignatures,  chkStickyNotes,        chkEdgeProfile },
                new[] { chkWallpaper,  chkNetworkDrives,  chkTemplates,   chkOneNote,            chkExcelMacros },
                new[] { chkSap,        chkOldProfile,     chkPublic,      chkIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnSelectAll, btnDeselectAll);
            cardBackupOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartBackup, btnCancelBackup, btnExportBackupLog);

            int logY = actY + ActionH + CardGap;
            rtbBackupLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
        }

        // ══════════════════════════════════════════════════════════════════
        // PAGE RESTAURATION
        // ══════════════════════════════════════════════════════════════════
        private void LayoutRestorePage()
        {
            if (pageRestore.ClientSize.Width <= 0) return;
            int W  = pageRestore.ClientSize.Width;
            int H  = pageRestore.ClientSize.Height;
            int cw = W - Margin * 2;

            cardRestoreSource.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtRestorePath, btnBrowseRestore);

            int optY = Margin + TopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkRestoreDocuments, chkRestoreDesktop,       chkRestoreDownloads,    chkRestorePictures,    chkRestoreMusic },
                new[] { chkRestoreVideos,    chkRestoreOutlook,        chkRestoreSignatures,   chkRestoreStickyNotes, chkRestoreEdgeProfile },
                new[] { chkRestoreWallpaper, chkRestoreNetworkDrives,  chkRestoreTemplates,    chkRestoreOneNote,     chkRestoreExcelMacros },
                new[] { chkRestoreSap,       chkRestorePublic,         chkLaunchApps,          chkRestoreIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnRestoreSelectAll, btnRestoreDeselectAll);
            cardRestoreOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartRestore, btnCancelRestore, btnExportRestoreLog);

            int logY = actY + ActionH + CardGap;
            rtbRestoreLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
        }

        // ══════════════════════════════════════════════════════════════════
        // PAGE MIGRATION
        // ══════════════════════════════════════════════════════════════════
        private void LayoutMigrationPage()
        {
            if (pageMigration.ClientSize.Width <= 0) return;
            int W  = pageMigration.ClientSize.Width;
            int H  = pageMigration.ClientSize.Height;
            int cw = W - Margin * 2;

            // Carte source
            cardMigrationSource.SetBounds(Margin, Margin, cw, MigTopCardH);

            // ComboBox + bouton refresh
            int refreshW = btnRefreshUSB.Width > 0 ? btnRefreshUSB.Width : 40;
            int cmbW     = cw - InnerPad * 2 - refreshW - ChkColGap;
            cmbUSBDrives.SetBounds(InnerPad, MigCmbY, cmbW, MigCmbH);
            btnRefreshUSB.SetBounds(InnerPad + cmbW + ChkColGap, MigCmbY, refreshW, MigCmbH + 2);

            // Bouton déverrouillage BitLocker (visible si disque verrouillé)
            btnUnlockBitLocker.SetBounds(InnerPad, MigBitlocY, cw - InnerPad * 2, MigBitlocH);

            // Label + ListBox profils
            lblProfiles.SetBounds(InnerPad, MigLblProfY, cw - InnerPad * 2, 20);
            lstProfiles.SetBounds(InnerPad, MigListY,    cw - InnerPad * 2, MigListH);

            // Bouton BitLocker (vérification statut) + label statut
            int bitlockerBtnW = btnBitLocker.Width > 0 ? btnBitLocker.Width : 180;
            btnBitLocker.SetBounds(InnerPad, MigBitLockerY, bitlockerBtnW, MigBitLockerH);
            lblBitLockerStatus.SetBounds(
                InnerPad + bitlockerBtnW + 12,
                MigBitLockerSY,
                cw - InnerPad * 2 - bitlockerBtnW - 12,
                MigBitLockerSH);

            // Label info
            lblMigrationInfo.SetBounds(InnerPad, MigInfoY, cw - InnerPad * 2, MigInfoH);

            // Carte options migration
            int optY = Margin + MigTopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkMigrateDocuments,   chkMigrateDesktop,       chkMigrateDownloads,     chkMigratePictures,    chkMigrateMusic },
                new[] { chkMigrateVideos,       chkMigrateOutlook,        chkMigrateSignatures,    chkMigrateExcelMacros, chkMigrateStickyNotes },
                new[] { chkMigrateEdgeProfile,  chkMigrateWallpaper,      chkMigrateNetworkDrives, chkMigrateOneNote,     chkMigrateTemplates },
                new[] { chkMigrateSap,          chkMigratePublic,         chkMigrateIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnMigrateSelectAll, btnMigrateDeselectAll);
            cardMigrationOptions.SetBounds(Margin, optY, cw, optH);

            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartMigration, btnCancelMigration, btnExportMigrationLog);

            int logY = actY + ActionH + CardGap;
            rtbMigrationLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS COMMUNS
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Positionne le TextBox et le bouton Parcourir dans la carte destination/source.
        /// Coordonnées relatives à la carte (enfants directs).
        /// </summary>
        private static void LayoutDestCard(int cardWidth, TextBox txt, Button browse)
        {
            int innerW  = cardWidth - InnerPad * 2;
            int browseW = browse.Width > 0 ? browse.Width : 120;
            int txtW    = Math.Max(80, innerW - browseW - ChkColGap);
            txt.SetBounds(InnerPad, 38, txtW, 30);
            browse.SetBounds(InnerPad + txtW + ChkColGap, 36, browseW, 32);
        }

        /// <summary>
        /// Positionne les colonnes de checkboxes et les boutons Tout/Décocher.
        /// Retourne la hauteur totale calculée de la carte.
        /// </summary>
        private static int LayoutOptionsCard(
            int cardWidth,
            ModernCheckBox[][] cols,
            Button btnAll,
            Button btnNone)
        {
            int colCount = cols.Length;
            int availW   = cardWidth - InnerPad * 2;
            int colW     = Math.Max(160, (availW - (colCount - 1) * ChkColGap) / colCount);
            int totalW   = colCount * colW + (colCount - 1) * ChkColGap;
            int startX   = InnerPad + Math.Max(0, (availW - totalW) / 2);

            int maxRows = 0;
            for (int c = 0; c < colCount; c++)
            {
                int x = startX + c * (colW + ChkColGap);
                maxRows = Math.Max(maxRows, cols[c].Length);
                for (int r = 0; r < cols[c].Length; r++)
                {
                    cols[c][r].SetBounds(x, ChkStartY + r * ChkStepY, colW, ChkH);
                }
            }

            int lastChkBottom = ChkStartY + (maxRows - 1) * ChkStepY + ChkH;

            if (btnAll != null && btnNone != null)
            {
                int btnY  = lastChkBottom + BtnGapY;
                int bAllW = btnAll.Width  > 0 ? btnAll.Width  : 120;
                int bAllH = btnAll.Height > 0 ? btnAll.Height : 34;
                int bNoW  = btnNone.Width > 0 ? btnNone.Width : 130;
                int bNoH  = btnNone.Height > 0 ? btnNone.Height : 34;
                btnAll.SetBounds(InnerPad, btnY, bAllW, bAllH);
                btnNone.SetBounds(InnerPad + bAllW + 8, btnY, bNoW, bNoH);
                return btnY + Math.Max(bAllH, bNoH) + CardPadBot;
            }

            return lastChkBottom + CardPadBot;
        }

        /// <summary>
        /// Positionne les trois boutons d'action sur une ligne horizontale.
        /// </summary>
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
