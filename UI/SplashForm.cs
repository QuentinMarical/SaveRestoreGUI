using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Fenêtre de démarrage sans bordure affichant l'avancement
    /// des initialisations avant l'ouverture de MainForm.
    /// </summary>
    public class SplashForm : Form
    {
        private readonly Label  _lblTitle;
        private readonly Label  _lblStatus;
        private readonly Panel  _progressTrack;
        private readonly Panel  _progressFill;
        private readonly Label  _lblVersion;

        private int _progressValue = 0;

        public SplashForm()
        {
            var p = ThemePalette.Dark;

            // ── Fenêtre
            FormBorderStyle = FormBorderStyle.None;
            StartPosition   = FormStartPosition.CenterScreen;
            Size            = new Size(480, 260);
            BackColor       = p.Sidebar;
            ShowInTaskbar   = false;

            // Coins arrondis (Windows 11)
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 18, 18));

            // ── Logo / titre
            _lblTitle = new Label
            {
                Text      = "SaveRestoreGUI",
                Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = p.Text,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 36, 480, 50)
            };

            var lblSub = new Label
            {
                Text      = "Gestionnaire de profil utilisateur",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = p.TextSecondary,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 88, 480, 24)
            };

            // ── Barre de progression (track)
            _progressTrack = new Panel
            {
                Bounds    = new Rectangle(48, 140, 384, 8),
                BackColor = p.InputBackground
            };
            _progressTrack.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, 384, 8, 4, 4));

            // Remplissage de la barre
            _progressFill = new Panel
            {
                Bounds    = new Rectangle(0, 0, 0, 8),
                BackColor = p.Accent
            };
            _progressFill.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, 384, 8, 4, 4));
            _progressTrack.Controls.Add(_progressFill);

            // ── Label statut
            _lblStatus = new Label
            {
                Text      = "Initialisation...",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = p.TextSecondary,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Bounds    = new Rectangle(0, 158, 480, 24)
            };

            // ── Version
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var vStr = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "";
            _lblVersion = new Label
            {
                Text      = vStr,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(90, 90, 110),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleRight,
                Bounds    = new Rectangle(0, 224, 468, 20)
            };

            Controls.AddRange(new Control[]
            {
                _lblTitle, lblSub,
                _progressTrack, _lblStatus, _lblVersion
            });

            // Ombre portée simulée via Paint
            Paint += OnSplashPaint;
        }

        // ── API publique ─────────────────────────────────────────────────────

        /// <summary>Met à jour le statut et la barre (0-100) depuis n'importe quel thread.</summary>
        public void UpdateProgress(int percent, string status)
        {
            if (InvokeRequired)
            { Invoke(() => UpdateProgress(percent, status)); return; }

            _progressValue = Math.Clamp(percent, 0, 100);
            _lblStatus.Text = status;

            int fillW = (int)(_progressTrack.Width * _progressValue / 100.0);
            _progressFill.Width = fillW;
            _progressFill.Region = fillW > 0
                ? Region.FromHrgn(CreateRoundRectRgn(0, 0, fillW, 8, 4, 4))
                : null;

            Refresh();
            Application.DoEvents();
        }

        // ── Rendu ────────────────────────────────────────────────────────────

        private void OnSplashPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            // Liseré d'accentuation en haut, dans le bleu accent du thème applicatif
            using var accentBrush = new LinearGradientBrush(
                new Point(0, 0), new Point(Width, 0),
                ThemePalette.Dark.Accent,
                ThemePalette.Dark.AccentHover);
            g.FillRectangle(accentBrush, new Rectangle(0, 0, Width, 3));
        }

        // ── Interop (coins arrondis) ──────────────────────────────────────────
        [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);
    }
}
