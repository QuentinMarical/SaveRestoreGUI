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

        // ── Carte options (checkboxes)
        private const int ChkLabelY    = 12;
        private const int ChkStartY    = 44;
        private const int ChkMinH      = 22;   // hauteur minimale par case
        private const int ChkRowGap    = 6;    // espacement vertical entre cases
        private const int ChkColGap    = 12;
        private const int BtnGapY      = 14;
        private const int CardPadBot   = 16;

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

            int optY = Margin + TopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkDocuments,     chkDesktop,        chkDownloads,      chkPictures,           chkMusic          },
                new[] { chkVideos,        chkOutlook,        chkSignatures,     chkStickyNotes,        chkEdgeProfile    },
                new[] { chkWallpaper,     chkNetworkDrives,  chkTemplates,      chkOneNote,            chkExcelMacros    },
                new[] { chkSap,           chkOldProfile,     chkPublic,         chkIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnSelectAll, btnDeselectAll);
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

            int optY = Margin + TopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkRestoreDocuments,  chkRestoreDesktop,       chkRestoreDownloads,   chkRestorePictures,      chkRestoreMusic       },
                new[] { chkRestoreVideos,     chkRestoreOutlook,       chkRestoreSignatures,  chkRestoreStickyNotes,   chkRestoreEdgeProfile },
                new[] { chkRestoreWallpaper,  chkRestoreNetworkDrives, chkRestoreTemplates,   chkRestoreOneNote,       chkRestoreExcelMacros },
                new[] { chkRestoreSap,        chkRestorePublic,        chkLaunchApps,         chkRestoreIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnRestoreSelectAll, btnRestoreDeselectAll);
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
            int optY = Margin + MigTopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkMigrateDocuments,   chkMigrateDesktop,      chkMigrateDownloads,     chkMigratePictures,    chkMigrateMusic          },
                new[] { chkMigrateVideos,      chkMigrateOutlook,      chkMigrateSignatures,    chkMigrateExcelMacros, chkMigrateStickyNotes    },
                new[] { chkMigrateEdgeProfile, chkMigrateWallpaper,    chkMigrateNetworkDrives, chkMigrateOneNote,     chkMigrateTemplates      },
                new[] { chkMigrateSap,         chkMigratePublic,       chkMigrateIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnMigrateSelectAll, btnMigrateDeselectAll);
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
        /// Positionne les colonnes de checkboxes et les boutons Tout/Décocher.
        /// La hauteur de chaque case est calculée dynamiquement d'après le texte
        /// et la police réelle → les libellés longs ne sont plus coupés.
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

            // Calcule la hauteur réelle de chaque case selon son texte et sa police.
            // On mesure avec un padding horizontal de 20 px (case + espace texte).
            static int MeasureChkHeight(ModernCheckBox chk, int width)
            {
                var font    = chk.Font ?? SystemFonts.DefaultFont;
                var maxSize = new Size(Math.Max(1, width - 20), 0);
                var size    = TextRenderer.MeasureText(
                    chk.Text,
                    font,
                    maxSize,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                return Math.Max(ChkMinH, size.Height + 4);  // +4 px de respiration
            }

            // Applique AutoSize=false + alignement TopLeft sur toutes les cases
            // pour que le texte multi-ligne s'affiche correctement.
            foreach (var col in cols)
                foreach (var chk in col)
                {
                    chk.AutoSize     = false;
                    chk.CheckAlign   = ContentAlignment.TopLeft;
                    chk.TextAlign    = ContentAlignment.TopLeft;
                    chk.UseMnemonic  = false;
                }

            int globalMaxBottom = ChkStartY;

            for (int c = 0; c < colCount; c++)
            {
                int x       = startX + c * (colW + ChkColGap);
                int yOffset = ChkStartY;

                for (int r = 0; r < cols[c].Length; r++)
                {
                    var chk = cols[c][r];
                    int h   = MeasureChkHeight(chk, colW);
                    chk.SetBounds(x, yOffset, colW, h);
                    yOffset += h + ChkRowGap;
                }

                globalMaxBottom = Math.Max(globalMaxBottom, yOffset - ChkRowGap);
            }

            int lastChkBottom = globalMaxBottom;

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
