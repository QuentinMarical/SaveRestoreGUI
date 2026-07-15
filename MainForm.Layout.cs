using System;
using System.Drawing;
using System.Windows.Forms;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    /// <summary>
    /// Mise en page façon app Paramètres de Windows 11 : colonne de contenu
    /// à largeur bornée, cartes-lignes (SettingCard) empilées avec petits
    /// espacements, contrôles alignés à droite dans chaque carte.
    ///
    /// Toute la géométrie est en pixels logiques (96 dpi) mise à l'échelle par
    /// <see cref="Dpi.S"/> : les constantes ci-dessous sont donc des valeurs
    /// déjà agrandies au facteur DPI courant. Les tailles issues de SettingCard
    /// (RowH, HeaderH, PadX) sont elles aussi déjà mises à l'échelle.
    /// </summary>
    public partial class MainForm
    {
        // ── Colonne de contenu
        private new static readonly int Margin  = Dpi.S(28);
        private static readonly int RowGap      = Dpi.S(8);     // entre cartes-lignes d'un même groupe
        private static readonly int CardGap     = Dpi.S(14);    // entre groupes

        // ── Cartes options (HeaderMode)
        // Plancher bas volontaire : CategoryCheckPanel est scrollable (AutoScroll),
        // donc la carte peut rétrécir plutôt que de recouvrir la barre d'action.
        private static readonly int CardOptMinH   = Dpi.S(120);
        private static readonly int OptInnerPadX   = Dpi.S(12);
        private static readonly int OptInnerPadBot = Dpi.S(12);

        // ── Contrôles dans les cartes-lignes
        private static readonly int CtlH          = Dpi.S(30);  // hauteur standard d'un contrôle hébergé
        private static readonly int CtlGap        = Dpi.S(8);
        private static readonly int BtnBrowseW    = Dpi.S(110);
        private static readonly int BtnRefreshW   = Dpi.S(120);
        private static readonly int BtnBitLockerW = Dpi.S(175);
        private static readonly int BtnAllW       = Dpi.S(110);
        private static readonly int BtnNoneW      = Dpi.S(130);
        private static readonly int TitleZoneMinW = Dpi.S(260); // place réservée au bloc icône+titre+description

        // ── Carte profil (Migration, HeaderMode)
        private static readonly int ProfCardH = Dpi.S(128);
        private static readonly int ProfSelH  = Dpi.S(36);
        private static readonly int ProfInfoH = Dpi.S(36);      // 2 lignes de texte secondaire

        // ── Barre d'actions
        private static readonly int ActionH    = Dpi.S(44);
        private static readonly int BtnStartW  = Dpi.S(230);
        private static readonly int BtnCancelW = Dpi.S(120);
        private static readonly int BtnExportW = Dpi.S(150);
        private static readonly int BtnLogsW   = Dpi.S(140);
        private static readonly int ActionSecBtnH = Dpi.S(34);  // hauteur boutons secondaires de la barre

        // ── Barre de progression (overlay dans contentPanel)
        private static readonly int LogProgressH  = Dpi.S(20);
        private static readonly int ProgressGapY  = Dpi.S(12);
        private static readonly int ProgressAreaH = LogProgressH + ProgressGapY * 2;
        private static readonly int ProgressPctW  = Dpi.S(80);

        public void ApplyResponsiveLayout()
        {
            LayoutBackupPage();
            LayoutRestorePage();
            LayoutMigrationPage();
            LayoutProgressOverlay();
        }

        /// <summary>Largeur de la colonne de contenu (pleine largeur moins marges).</summary>
        private static int ContentWidth(Control page)
            => page.ClientSize.Width - Margin * 2;

        // ═══════════════════════════════════════════════════════════════════
        // PAGE SAUVEGARDE
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutBackupPage()
        {
            if (pageBackup.ClientSize.Width <= 0) return;
            int cw = ContentWidth(pageBackup);
            int ch = pageBackup.ClientSize.Height;

            int y = Margin;
            cardBackupDest.SetBounds(Margin, y, cw, SettingCard.RowH);
            LayoutPathRow(cw, txtBackupPath, btnBrowseBackup);
            y += SettingCard.RowH + CardGap;

            int actY = ch - ActionH - Margin - ProgressAreaH;
            int optH = Math.Max(CardOptMinH, actY - y - CardGap);
            cardBackupOptions.SetBounds(Margin, y, cw, optH);
            LayoutOptionsCard(cw, optH, chkPanelBackup, btnSelectAll, btnDeselectAll);

            LayoutActionBar(Margin, actY, cw,
                btnStartBackup, btnCancelBackup, btnExportBackupLog, btnOpenBackupLog);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE RESTAURATION
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutRestorePage()
        {
            if (pageRestore.ClientSize.Width <= 0) return;
            int cw = ContentWidth(pageRestore);
            int ch = pageRestore.ClientSize.Height;

            int y = Margin;
            cardRestoreSource.SetBounds(Margin, y, cw, SettingCard.RowH);
            LayoutPathRow(cw, txtRestorePath, btnBrowseRestore);
            y += SettingCard.RowH + CardGap;

            int actY = ch - ActionH - Margin - ProgressAreaH;
            int optH = Math.Max(CardOptMinH, actY - y - CardGap);
            cardRestoreOptions.SetBounds(Margin, y, cw, optH);
            LayoutOptionsCard(cw, optH, chkPanelRestore, btnRestoreSelectAll, btnRestoreDeselectAll);

            LayoutActionBar(Margin, actY, cw,
                btnStartRestore, btnCancelRestore, btnExportRestoreLog, btnOpenRestoreLog);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PAGE MIGRATION
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutMigrationPage()
        {
            if (pageMigration.ClientSize.Width <= 0) return;
            int cw = ContentWidth(pageMigration);
            int ch = pageMigration.ClientSize.Height;

            int y = Margin;

            // ── Ligne 1 : lecteur source (combo + actualiser à droite)
            cardMigrationSource.SetBounds(Margin, y, cw, SettingCard.RowH);
            int refreshX = cw - SettingCard.PadX - BtnRefreshW;
            int cmbW     = Math.Max(Dpi.S(160), Math.Min(Dpi.S(320),
                cw - SettingCard.PadX * 2 - TitleZoneMinW - BtnRefreshW - CtlGap));
            int cmbX     = refreshX - CtlGap - cmbW;
            int ctlY     = (SettingCard.RowH - CtlH) / 2;
            cmbUSBDrives.SetBounds(cmbX, ctlY, cmbW, CtlH);
            btnRefreshUSB.SetBounds(refreshX, ctlY, BtnRefreshW, CtlH + Dpi.S(2));
            y += SettingCard.RowH + RowGap;

            // ── Ligne 2 : BitLocker (statut + bouton à droite)
            cardMigrationBitLocker.SetBounds(Margin, y, cw, SettingCard.RowH);
            int bitBtnX   = cw - SettingCard.PadX - BtnBitLockerW;
            int statusW   = Math.Max(Dpi.S(120), Math.Min(Dpi.S(360),
                cw - SettingCard.PadX * 2 - TitleZoneMinW - BtnBitLockerW - CtlGap));
            lblBitLockerStatus.SetBounds(bitBtnX - CtlGap - statusW, ctlY, statusW, CtlH);
            btnUnlockBitLocker.SetBounds(bitBtnX, ctlY, BtnBitLockerW, CtlH + Dpi.S(2));
            y += SettingCard.RowH + RowGap;

            // ── Ligne 3 : profil détecté (contenu sous l'en-tête)
            cardMigrationProfile.SetBounds(Margin, y, cw, ProfCardH);
            int innerW = cw - SettingCard.PadX * 2;
            lblSelectedProfile.SetBounds(SettingCard.PadX, SettingCard.HeaderH, innerW, ProfSelH);
            lblMigrationInfo.SetBounds(SettingCard.PadX, SettingCard.HeaderH + ProfSelH + Dpi.S(4), innerW, ProfInfoH);
            y += ProfCardH + CardGap;

            // ── Carte options + barre d'action
            int actY = ch - ActionH - Margin - ProgressAreaH;
            int optH = Math.Max(CardOptMinH, actY - y - CardGap);
            cardMigrationOptions.SetBounds(Margin, y, cw, optH);
            LayoutOptionsCard(cw, optH, chkPanelMigration, btnMigrateSelectAll, btnMigrateDeselectAll);

            LayoutActionBar(Margin, actY, cw,
                btnStartMigration, btnCancelMigration, btnExportMigrationLog, btnOpenMigrationLog);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BARRE DE PROGRESSION (overlay dans contentPanel)
        // ═══════════════════════════════════════════════════════════════════
        private void LayoutProgressOverlay()
        {
            int pw = contentPanel.ClientSize.Width;
            int ph = contentPanel.ClientSize.Height;
            if (pw <= 0 || ph <= 0) return;

            // Alignée sur la colonne de contenu, toujours au-dessus du bord bas
            // (la fenêtre maximisée est bornée à la zone de travail via MaximizedBounds).
            int cw   = pw - Margin * 2;
            int barW = Math.Max(Dpi.S(80), cw - ProgressPctW);
            int y    = ph - LogProgressH - ProgressGapY;
            progressBar.SetBounds(Margin, y, barW, LogProgressH);
            lblProgressPercent.SetBounds(Margin + barW, y, ProgressPctW, LogProgressH);
            progressBar.BringToFront();
            lblProgressPercent.BringToFront();
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Carte-ligne « dossier » : champ de chemin + bouton Parcourir à droite.</summary>
        private static void LayoutPathRow(int cardWidth, TextBox txt, Button browse)
        {
            int browseX = cardWidth - SettingCard.PadX - BtnBrowseW;
            int txtW    = Math.Max(Dpi.S(180), Math.Min(Dpi.S(420),
                cardWidth - SettingCard.PadX * 2 - TitleZoneMinW - BtnBrowseW - CtlGap));
            int y = (SettingCard.RowH - CtlH) / 2;
            txt.SetBounds(browseX - CtlGap - txtW, y, txtW, CtlH);
            browse.SetBounds(browseX, y - Dpi.S(1), BtnBrowseW, CtlH + Dpi.S(2));
        }

        /// <summary>
        /// Carte options (HeaderMode) : boutons Tout cocher / Tout décocher dans
        /// l'en-tête à droite, panneau de cases remplissant le reste de la carte.
        /// </summary>
        private static void LayoutOptionsCard(
            int cardWidth,
            int cardHeight,
            CategoryCheckPanel panel,
            Button btnAll,
            Button btnNone)
        {
            int btnY  = (SettingCard.HeaderH - CtlH) / 2;
            int noneX = cardWidth - SettingCard.PadX - BtnNoneW;
            int allX  = noneX - CtlGap - BtnAllW;
            btnAll.SetBounds(allX, btnY, BtnAllW, CtlH);
            btnNone.SetBounds(noneX, btnY, BtnNoneW, CtlH);

            panel.SetBounds(
                OptInnerPadX,
                SettingCard.HeaderH,
                cardWidth - OptInnerPadX * 2,
                Math.Max(Dpi.S(60), cardHeight - SettingCard.HeaderH - OptInnerPadBot));
        }

        private static void LayoutActionBar(
            int left, int top, int availableWidth,
            Button start, Button cancel, Button export, Button logs)
        {
            start.SetBounds(left, top, BtnStartW, ActionH);
            cancel.SetBounds(left + BtnStartW + CtlGap, top, BtnCancelW, ActionH);

            int btnY = top + (ActionH - ActionSecBtnH) / 2;
            export.SetBounds(left + availableWidth - BtnExportW, btnY, BtnExportW, ActionSecBtnH);
            logs.SetBounds(left + availableWidth - BtnExportW - BtnLogsW - CtlGap, btnY, BtnLogsW, ActionSecBtnH);
        }
    }
}
