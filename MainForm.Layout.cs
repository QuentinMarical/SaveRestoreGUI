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
        // ── Marges et espacements ──────────────────────────────────────────────────────
        private new const int Margin = 28;   // marge gauche/droite des pages
        private const int CardGap      = 14;   // espace vertical entre les cartes
        private const int InnerPad     = 16;   // padding interne des cartes

        // ── Carte du haut (destination / source) ──────────────────────────────────
        private const int TopCardH     = 90;   // hauteur carte Backup/Restore
        private const int MigTopCardH  = 290;  // hauteur carte source Migration

        // ── Carte des options (checkboxes) ──────────────────────────────────
        private const int ChkLabelY    = 12;   // titre de la carte
        private const int ChkStartY    = 44;   // première ligne de checkbox
        private const int ChkStepY     = 32;   // pas vertical entre lignes
        private const int ChkH         = 28;   // hauteur d'une checkbox
        private const int ChkColGap    = 12;   // espace entre colonnes
        private const int BtnGapY      = 14;   // espace entre dernière checkbox et boutons
        private const int CardPadBot   = 16;   // padding bas carte options

        // ── Barre d'actions ─────────────────────────────────────────────────────
        private const int ActionH      = 44;
        private const int BtnStartW    = 230;
        private const int BtnCancelW   = 120;
        private const int BtnExportW   = 150;

        // ── Console log ──────────────────────────────────────────────────────────
        private const int LogMinH      = 120;
        private const int LogMarginBot = 12;

        // ── Migration : zones internes de la carte source ──────────────────────
        private const int MigCmbY      = 40;
        private const int MigCmbH      = 30;
        private const int MigLblProfY  = 84;
        private const int MigListY     = 106;
        private const int MigListH     = 128;
        private const int MigInfoY     = 242;
        private const int MigInfoH     = 40;

        // ──────────────────────────────────────────────────────────────────

        public void ApplyResponsiveLayout()
        {
            LayoutBackupPage();
            LayoutRestorePage();
            LayoutMigrationPage();
        }

        // ════════════════════════════════════════════════════════════════════
        //  PAGE SAUVEGARDE
        //  4 colonnes × 5 lignes = 20 cases max
        //  Col 1 : Documents, Bureau, Téléchargements, Images, Musique
        //  Col 2 : Vidéos, Outlook, Signatures, Sticky Notes, Profil Edge
        //  Col 3 : Fond d'écran, Lecteurs réseau, Modèles, OneNote, Macros Excel
        //  Col 4 : SAP, Ancien profil, Public, IP Softphone
        // ════════════════════════════════════════════════════════════════════
        private void LayoutBackupPage()
        {
            if (pageBackup.ClientSize.Width <= 0) return;
            int W  = pageBackup.ClientSize.Width;
            int H  = pageBackup.ClientSize.Height;
            int cw = W - Margin * 2;

            // ── Carte destination ──
            cardBackupDest.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtBackupPath, btnBrowseBackup);

            // ── Carte options ──
            int optY = Margin + TopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkDocuments,     chkDesktop,        chkDownloads,      chkPictures,         chkMusic          },
                new[] { chkVideos,        chkOutlook,        chkSignatures,     chkStickyNotes,      chkEdgeProfile    },
                new[] { chkWallpaper,     chkNetworkDrives,  chkTemplates,      chkOneNote,          chkExcelMacros    },
                new[] { chkSap,           chkOldProfile,     chkPublic,         chkIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnSelectAll, btnDeselectAll);
            cardBackupOptions.SetBounds(Margin, optY, cw, optH);

            // ── Barre d'actions ──
            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartBackup, btnCancelBackup, btnExportBackupLog);

            // ── Console ──
            int logY = actY + ActionH + CardGap;
            rtbBackupLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
        }

        // ════════════════════════════════════════════════════════════════════
        //  PAGE RESTAURATION
        //  4 colonnes × 5 lignes = 20 cases max
        //  Col 1 : Documents, Bureau, Téléchargements, Images, Musique
        //  Col 2 : Vidéos, Outlook, Signatures, Sticky Notes, Profil Edge
        //  Col 3 : Fond d'écran, Lecteurs réseau, Modèles, OneNote, Macros Excel
        //  Col 4 : SAP, Public, Lancer apps, IP Softphone
        // ════════════════════════════════════════════════════════════════════
        private void LayoutRestorePage()
        {
            if (pageRestore.ClientSize.Width <= 0) return;
            int W  = pageRestore.ClientSize.Width;
            int H  = pageRestore.ClientSize.Height;
            int cw = W - Margin * 2;

            // ── Carte source ──
            cardRestoreSource.SetBounds(Margin, Margin, cw, TopCardH);
            LayoutDestCard(cw, txtRestorePath, btnBrowseRestore);

            // ── Carte options ──
            int optY = Margin + TopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkRestoreDocuments,  chkRestoreDesktop,      chkRestoreDownloads,    chkRestorePictures,      chkRestoreMusic          },
                new[] { chkRestoreVideos,     chkRestoreOutlook,      chkRestoreSignatures,   chkRestoreStickyNotes,   chkRestoreEdgeProfile    },
                new[] { chkRestoreWallpaper,  chkRestoreNetworkDrives,chkRestoreTemplates,    chkRestoreOneNote,       chkRestoreExcelMacros    },
                new[] { chkRestoreSap,        chkRestorePublic,       chkLaunchApps,          chkRestoreIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnRestoreSelectAll, btnRestoreDeselectAll);
            cardRestoreOptions.SetBounds(Margin, optY, cw, optH);

            // ── Barre d'actions ──
            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartRestore, btnCancelRestore, btnExportRestoreLog);

            // ── Console ──
            int logY = actY + ActionH + CardGap;
            rtbRestoreLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
        }

        // ════════════════════════════════════════════════════════════════════
        //  PAGE MIGRATION
        //  4 colonnes × 5 lignes = 20 cases max
        //  Col 1 : Documents, Bureau, Téléchargements, Images, Musique
        //  Col 2 : Vidéos, Outlook, Signatures, Macros Excel, Sticky Notes
        //  Col 3 : Profil Edge, Fond d'écran, Lecteurs réseau, OneNote, Modèles
        //  Col 4 : SAP, Public, IP Softphone
        // ════════════════════════════════════════════════════════════════════
        private void LayoutMigrationPage()
        {
            if (pageMigration.ClientSize.Width <= 0) return;
            int W  = pageMigration.ClientSize.Width;
            int H  = pageMigration.ClientSize.Height;
            int cw = W - Margin * 2;

            // ── Carte source (spécifique migration) ──
            cardMigrationSource.SetBounds(Margin, Margin, cw, MigTopCardH);

            // ComboBox disque + bouton refresh (coordonnées relatives à la carte)
            int refreshW = btnRefreshUSB.Width > 0 ? btnRefreshUSB.Width : 40;
            int cmbW     = cw - InnerPad * 2 - refreshW - ChkColGap;
            cmbUSBDrives.SetBounds(InnerPad, MigCmbY, cmbW, MigCmbH);
            btnRefreshUSB.SetBounds(InnerPad + cmbW + ChkColGap, MigCmbY, refreshW, MigCmbH + 2);

            // Label "Profil utilisateur à migrer"
            lblProfiles.SetBounds(InnerPad, MigLblProfY, cw - InnerPad * 2, 20);

            // ListBox profils
            lstProfiles.SetBounds(InnerPad, MigListY, cw - InnerPad * 2, MigListH);

            // Label info
            lblMigrationInfo.SetBounds(InnerPad, MigInfoY, cw - InnerPad * 2, MigInfoH);

            // ── Carte options migration ──
            int optY = Margin + MigTopCardH + CardGap;
            var cols = new ModernCheckBox[][]
            {
                new[] { chkMigrateDocuments,      chkMigrateDesktop,       chkMigrateDownloads,      chkMigratePictures,      chkMigrateMusic          },
                new[] { chkMigrateVideos,         chkMigrateOutlook,       chkMigrateSignatures,     chkMigrateExcelMacros,   chkMigrateStickyNotes    },
                new[] { chkMigrateEdgeProfile,    chkMigrateWallpaper,     chkMigrateNetworkDrives,  chkMigrateOneNote,       chkMigrateTemplates      },
                new[] { chkMigrateSap,            chkMigratePublic,        chkMigrateIpDesktopSoftphone }
            };
            int optH = LayoutOptionsCard(cw, cols, btnMigrateSelectAll, btnMigrateDeselectAll);
            cardMigrationOptions.SetBounds(Margin, optY, cw, optH);

            // ── Barre d'actions ──
            int actY = optY + optH + CardGap;
            LayoutActionBar(Margin, actY, cw, btnStartMigration, btnCancelMigration, btnExportMigrationLog);

            // ── Console ──
            int logY = actY + ActionH + CardGap;
            rtbMigrationLog.SetBounds(Margin, logY, cw, Math.Max(LogMinH, H - logY - LogMarginBot));
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS COMMUNS
        // ════════════════════════════════════════════════════════════════════

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
            int colCount  = cols.Length;
            int availW    = cardWidth - InnerPad * 2;
            int colW      = Math.Max(160, (availW - (colCount - 1) * ChkColGap) / colCount);
            int totalW    = colCount * colW + (colCount - 1) * ChkColGap;
            int startX    = InnerPad + Math.Max(0, (availW - totalW) / 2);

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
                int btnY = lastChkBottom + BtnGapY;
                btnAll.SetBounds(InnerPad, btnY, btnAll.Width > 0 ? btnAll.Width : 120, btnAll.Height > 0 ? btnAll.Height : 34);
                btnNone.SetBounds(InnerPad + btnAll.Width + 8, btnY, btnNone.Width > 0 ? btnNone.Width : 130, btnNone.Height > 0 ? btnNone.Height : 34);
                return btnY + Math.Max(btnAll.Height, btnNone.Height) + CardPadBot;
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
