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
    /// </summary>
    public partial class MainForm
    {
        // ── Colonne de contenu
        private new const int Margin      = 28;
        private const int MaxContentW     = 1060;  // largeur max du contenu (≈ app Paramètres)
        private const int RowGap          = 8;     // entre cartes-lignes d'un même groupe
        private const int CardGap         = 14;    // entre groupes

        // ── Cartes options (HeaderMode)
        // Plancher bas volontaire : CategoryCheckPanel est scrollable (AutoScroll),
        // donc la carte peut rétrécir plutôt que de recouvrir la barre d'action.
        private const int CardOptMinH     = 120;
        private const int OptInnerPadX    = 12;
        private const int OptInnerPadBot  = 12;

        // ── Contrôles dans les cartes-lignes
        private const int CtlH            = 30;    // hauteur standard d'un contrôle hébergé
        private const int CtlGap          = 8;
        private const int BtnBrowseW      = 110;
        private const int BtnRefreshW     = 120;
        private const int BtnBitLockerW   = 175;
        private const int BtnAllW         = 110;
        private const int BtnNoneW        = 130;
        private const int TitleZoneMinW   = 260;   // place réservée au bloc icône+titre+description

        // ── Carte profil (Migration, HeaderMode)
        private const int ProfCardH       = 142;
        private const int ProfSelH        = 40;
        private const int ProfInfoH       = 40;   // 2 lignes de texte secondaire

        // ── Barre d'actions
        private const int ActionH    = 44;
        private const int BtnStartW  = 230;
        private const int BtnCancelW = 120;
        private const int BtnExportW = 150;
        private const int BtnLogsW   = 140;

        // ── Barre de progression (overlay dans contentPanel)
        private const int LogProgressH   = 20;
        private const int ProgressGapY   = 12;
        private const int ProgressAreaH  = LogProgressH + ProgressGapY * 2;
        private const int ProgressPctW   = 80;

        public void ApplyResponsiveLayout()
        {
            LayoutBackupPage();
            LayoutRestorePage();
            LayoutMigrationPage();
            LayoutProgressOverlay();
        }

        /// <summary>Largeur de la colonne de contenu, bornée façon app Paramètres.</summary>
        private static int ContentWidth(Control page)
            => Math.Min(page.ClientSize.Width - Margin * 2, MaxContentW);

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
            int cmbW     = Math.Max(160, Math.Min(320,
                cw - SettingCard.PadX * 2 - TitleZoneMinW - BtnRefreshW - CtlGap));
            int cmbX     = refreshX - CtlGap - cmbW;
            int ctlY     = (SettingCard.RowH - CtlH) / 2;
            cmbUSBDrives.SetBounds(cmbX, ctlY, cmbW, CtlH);
            btnRefreshUSB.SetBounds(refreshX, ctlY, BtnRefreshW, CtlH + 2);
            y += SettingCard.RowH + RowGap;

            // ── Ligne 2 : BitLocker (statut + bouton à droite)
            cardMigrationBitLocker.SetBounds(Margin, y, cw, SettingCard.RowH);
            int bitBtnX   = cw - SettingCard.PadX - BtnBitLockerW;
            int statusW   = Math.Max(120, Math.Min(360,
                cw - SettingCard.PadX * 2 - TitleZoneMinW - BtnBitLockerW - CtlGap));
            lblBitLockerStatus.SetBounds(bitBtnX - CtlGap - statusW, ctlY, statusW, CtlH);
            btnUnlockBitLocker.SetBounds(bitBtnX, ctlY, BtnBitLockerW, CtlH + 2);
            y += SettingCard.RowH + RowGap;

            // ── Ligne 3 : profil détecté (contenu sous l'en-tête)
            cardMigrationProfile.SetBounds(Margin, y, cw, ProfCardH);
            int innerW = cw - SettingCard.PadX * 2;
            lblSelectedProfile.SetBounds(SettingCard.PadX, SettingCard.HeaderH, innerW, ProfSelH);
            lblMigrationInfo.SetBounds(SettingCard.PadX, SettingCard.HeaderH + ProfSelH + 4, innerW, ProfInfoH);
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
            // (contentPanel est lui-même borné à la zone de travail via MaximizedBounds).
            int cw   = Math.Min(pw - Margin * 2, MaxContentW);
            int barW = Math.Max(80, cw - ProgressPctW);
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
            int txtW    = Math.Max(180, Math.Min(420,
                cardWidth - SettingCard.PadX * 2 - TitleZoneMinW - BtnBrowseW - CtlGap));
            int y = (SettingCard.RowH - CtlH) / 2;
            txt.SetBounds(browseX - CtlGap - txtW, y, txtW, CtlH);
            browse.SetBounds(browseX, y - 1, BtnBrowseW, CtlH + 2);
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
                Math.Max(60, cardHeight - SettingCard.HeaderH - OptInnerPadBot));
        }

        private static void LayoutActionBar(
            int left, int top, int availableWidth,
            Button start, Button cancel, Button export, Button logs)
        {
            start.SetBounds(left, top, BtnStartW, ActionH);
            cancel.SetBounds(left + BtnStartW + CtlGap, top, BtnCancelW, ActionH);

            int btnY = top + (ActionH - 34) / 2;
            export.SetBounds(left + availableWidth - BtnExportW, btnY, BtnExportW, 34);
            logs.SetBounds(left + availableWidth - BtnExportW - BtnLogsW - CtlGap, btnY, BtnLogsW, 34);
        }
    }
}
