using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Fenêtre flottante affichant les logs d'une opération.
    /// Elle s'ouvre depuis le bouton "📋 Voir les logs" de chaque page
    /// et se ferme automatiquement quand l'utilisateur change d'onglet.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class LogWindow : Form
    {
        public RichTextBox LogBox { get; }

        private readonly ModernButton _btnExport;
        private readonly ModernButton _btnClear;
        private readonly string       _defaultExportName;

        public LogWindow(string title, string defaultExportName)
        {
            _defaultExportName = defaultExportName;

            Text            = title;
            Size            = new Size(820, 480);
            MinimumSize     = new Size(500, 300);
            StartPosition   = FormStartPosition.Manual;
            ShowInTaskbar   = false;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // ── Log box
            LogBox = new RichTextBox
            {
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                Dock        = DockStyle.Fill,
                Font        = new Font("Cascadia Mono", 8.5f, FontStyle.Regular,
                                       GraphicsUnit.Point),
                ScrollBars  = RichTextBoxScrollBars.Vertical,
            };

            // ── Toolbar
            var toolbar = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            _btnExport = new ModernButton { Text = "💾 Exporter", AutoSize = true };
            _btnExport.SetBounds(8, 4, 120, 32);
            _btnExport.Click += OnExport;

            _btnClear = new ModernButton { Text = "🗑 Effacer", AutoSize = true };
            _btnClear.SetBounds(136, 4, 110, 32);
            _btnClear.Click += (_, _) => LogBox.Clear();

            toolbar.Controls.AddRange(new Control[] { _btnExport, _btnClear });

            Controls.AddRange(new Control[] { LogBox, toolbar });

            ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ThemeManager.ThemeChanged -= ApplyTheme;
            base.OnFormClosed(e);
        }

        // Ferme proprement sans déclencher DialogResult (appelé par ShowPage)
        public void CloseIfOpen()
        {
            if (!IsDisposed && Visible) Close();
        }

        // Positionne la fenêtre à droite / en bas de son parent
        public void PositionNear(Form owner)
        {
            if (owner == null) return;
            int x = owner.Right - Width - 16;
            int y = owner.Bottom - Height - 48;
            x = Math.Max(Screen.PrimaryScreen?.WorkingArea.Left ?? 0, x);
            y = Math.Max(Screen.PrimaryScreen?.WorkingArea.Top  ?? 0, y);
            Location = new Point(x, y);
        }

        private void OnExport(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter   = "Fichier texte|*.txt",
                FileName = _defaultExportName
            };
            if (sfd.ShowDialog() == DialogResult.OK)
                File.WriteAllText(sfd.FileName, LogBox.Text, Encoding.UTF8);
        }

        private void ApplyTheme()
        {
            if (IsDisposed) return;
            if (InvokeRequired) { Invoke(ApplyTheme); return; }

            var p = ThemeManager.Palette;
            BackColor       = p.Background;
            LogBox.BackColor = p.ConsoleBackground;
            LogBox.ForeColor = p.ConsoleText;

            foreach (Control c in Controls)
                if (c is Panel tb)
                {
                    tb.BackColor = p.Sidebar;
                    foreach (Control b in tb.Controls)
                        b.BackColor = p.Surface;
                }
        }
    }
}
