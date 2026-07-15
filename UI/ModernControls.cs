using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Bouton moderne à coins arrondis avec effet hover (style Fluent).
    /// </summary>
    public class ModernButton : Button
    {
        private bool _hovered;
        private bool _pressed;
        private int _radius = Dpi.S(6);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get => _radius; set { _radius = value; Invalidate(); } }

        /// <summary>Rôle visuel : Primary (accent), Secondary (surface), Danger, Success.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ButtonRole Role { get; set; } = ButtonRole.Primary;

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Dpi.Font("Segoe UI", 9.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            MouseEnter += (s, e) => { _hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _hovered = false; _pressed = false; Invalidate(); };
            MouseDown += (s, e) => { _pressed = true; Invalidate(); };
            MouseUp   += (s, e) => { _pressed = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? p.Background);

            Color baseColor = Role switch
            {
                ButtonRole.Primary => p.Accent,
                ButtonRole.Danger => p.Danger,
                ButtonRole.Success => p.Success,
                _ => p.Surface
            };
            // Sur fond sombre, le survol/l'appui éclaircissent plutôt qu'ils n'assombrissent
            // (convention Fluent dark : les états d'interaction gagnent en luminosité).
            Color hoverColor = Role switch
            {
                ButtonRole.Primary => p.AccentHover,
                _ => ControlPaint.Light(baseColor, 0.15f)
            };
            Color pressedColor = ControlPaint.Dark(hoverColor, 0.08f);

            var fill = !Enabled
                ? Color.FromArgb(120, baseColor)
                : (_pressed ? pressedColor : (_hovered ? hoverColor : baseColor));
            Color textColor = Role == ButtonRole.Secondary ? p.Text : Color.White;
            if (!Enabled) textColor = Color.FromArgb(160, textColor);

            using var path = RoundedRect(ClientRectangle, _radius);
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);

            if (Role == ButtonRole.Secondary)
            {
                using var pen = new Pen(p.Border, 1f);
                g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        internal static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            if (d <= 0) { path.AddRectangle(rect); return path; }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public enum ButtonRole { Primary, Secondary, Danger, Success }

    /// <summary>
    /// Panneau « carte » à coins arrondis avec bordure subtile.
    /// </summary>
    public class CardPanel : Panel
    {
        private int _radius = Dpi.S(12);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get => _radius; set { _radius = value; Invalidate(); } }

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? p.Background);

            using var path = ModernButton.RoundedRect(ClientRectangle, _radius);
            using var brush = new SolidBrush(p.Surface);
            g.FillPath(brush, path);
            using var pen = new Pen(p.Border, 1f);
            g.DrawPath(pen, path);

            // Liseré clair en haut : accroche de lumière façon matériau Mica (Fluent Win11)
            using var highlightPen = new Pen(p.CardHighlight, 1f);
            g.DrawLine(highlightPen, _radius, 1, Width - _radius, 1);
        }
    }

    /// <summary>
    /// Bouton de navigation latérale (sidebar) avec indicateur de sélection.
    /// </summary>
    public class NavButton : Button
    {
        private bool _hovered;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected { get; set; }

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Dpi.Font("Segoe UI", 10.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
            Height = Dpi.S(48);
            MouseEnter += (s, e) => { _hovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _hovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(p.Sidebar);

            if (Selected || _hovered)
            {
                var rect = new Rectangle(Dpi.S(6), Dpi.S(4), Width - Dpi.S(12), Height - Dpi.S(8));
                using var path = ModernButton.RoundedRect(rect, Dpi.S(8));
                using var brush = new SolidBrush(Selected ? p.Surface : Color.FromArgb(40, p.Accent));
                g.FillPath(brush, path);
            }

            if (Selected)
            {
                using var accentBrush = new SolidBrush(p.Accent);
                g.FillRectangle(accentBrush, new RectangleF(Dpi.S(6), Height / 2f - Dpi.Sf(10), Dpi.Sf(3.5f), Dpi.Sf(20)));
            }

            var textColor = Selected ? p.Text : p.TextSecondary;
            var textRect = new Rectangle(Dpi.S(22), 0, Width - Dpi.S(24), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    /// <summary>
    /// Case à cocher stylisée (coche dessinée, coins arrondis).
    /// </summary>
    public class ModernCheckBox : CheckBox
    {
        public ModernCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Dpi.Font("Segoe UI", 9.5f);
            Cursor = Cursors.Hand;
            AutoSize = false;
            Height = Dpi.S(28);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? p.Surface);

            int boxSize = Dpi.S(18);
            int boxY = (Height - boxSize) / 2;
            var boxRect = new Rectangle(0, boxY, boxSize, boxSize);

            using var path = ModernButton.RoundedRect(boxRect, Dpi.S(5));
            if (Checked)
            {
                using var brush = new SolidBrush(Enabled ? p.Accent : Color.FromArgb(120, p.Accent));
                g.FillPath(brush, path);
                using var pen = new Pen(Color.White, Dpi.Sf(2f));
                g.DrawLines(pen, new[]
                {
                    new PointF(Dpi.Sf(4),    boxY + Dpi.Sf(9)),
                    new PointF(Dpi.Sf(7.5f), boxY + Dpi.Sf(13)),
                    new PointF(Dpi.Sf(14),   boxY + Dpi.Sf(5))
                });
            }
            else
            {
                using var brush = new SolidBrush(p.InputBackground);
                g.FillPath(brush, path);
                using var pen = new Pen(p.Border, 1.4f);
                g.DrawPath(pen, path);
            }

            var textColor = Enabled ? p.Text : p.TextSecondary;
            var textRect = new Rectangle(boxSize + Dpi.S(8), 0, Width - boxSize - Dpi.S(8), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>
    /// Barre de progression moderne à coins arrondis avec animation fluide.
    /// </summary>
    public class ModernProgressBar : Control
    {
        private int _value;
        private float _displayValue;
        private readonly System.Windows.Forms.Timer _animTimer;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _value;
            set { _value = Math.Clamp(value, 0, 100); _animTimer.Start(); }
        }

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = Dpi.S(8);
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) =>
            {
                float diff = _value - _displayValue;
                if (Math.Abs(diff) < 0.5f) { _displayValue = _value; _animTimer.Stop(); }
                else _displayValue += diff * 0.25f;
                Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? p.Background);

            using var bgPath = ModernButton.RoundedRect(ClientRectangle, Height / 2);
            using var bgBrush = new SolidBrush(p.InputBackground);
            g.FillPath(bgBrush, bgPath);

            int fillWidth = (int)(Width * _displayValue / 100f);
            if (fillWidth > Height)
            {
                var fillRect = new Rectangle(0, 0, fillWidth, Height);
                using var fillPath = ModernButton.RoundedRect(fillRect, Height / 2);
                using var fillBrush = new LinearGradientBrush(fillRect, p.Accent, p.AccentHover, 0f);
                g.FillPath(fillBrush, fillPath);
            }

            if (_animTimer.Enabled == false && _displayValue != _value) _animTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _animTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Notification toast non-intrusive affichée en bas à droite, avec animation de fondu.
    /// </summary>
    public static class ToastService
    {
        public static void Show(Form owner, string message, ToastKind kind = ToastKind.Info)
        {
            if (owner.IsDisposed) return;
            var p = ThemeManager.Palette;

            Color accent = kind switch
            {
                ToastKind.Success => p.Success,
                ToastKind.Warning => p.Warning,
                ToastKind.Error => p.Danger,
                _ => p.Accent
            };
            string icon = kind switch
            {
                ToastKind.Success => "✓",
                ToastKind.Warning => "⚠",
                ToastKind.Error => "✗",
                _ => "ℹ"
            };

            var toast = new CardPanel
            {
                Size = new Size(Dpi.S(360), Dpi.S(56)),
                CornerRadius = Dpi.S(10)
            };

            var iconLabel = new Label
            {
                Text = icon,
                Font = Dpi.Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = false,
                Size = new Size(Dpi.S(40), Dpi.S(56)),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Location = new Point(Dpi.S(4), 0)
            };
            var msgLabel = new Label
            {
                Text = message.Length > 90 ? message[..90] + "…" : message,
                Font = Dpi.Font("Segoe UI", 9f),
                ForeColor = p.Text,
                AutoSize = false,
                Size = new Size(Dpi.S(300), Dpi.S(56)),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
                Location = new Point(Dpi.S(46), 0)
            };
            toast.Controls.Add(iconLabel);
            toast.Controls.Add(msgLabel);

            // Empiler les toasts existants
            int offset = Dpi.S(16);
            foreach (Control c in owner.Controls)
            {
                if (c is CardPanel cp && cp.Tag as string == "toast")
                    offset += cp.Height + Dpi.S(10);
            }

            toast.Tag = "toast";
            toast.Location = new Point(owner.ClientSize.Width - toast.Width - Dpi.S(16),
                                       owner.ClientSize.Height - toast.Height - offset);
            toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            owner.Controls.Add(toast);
            toast.BringToFront();

            var timer = new System.Windows.Forms.Timer { Interval = 3000 };
            timer.Tick += (s, e) =>
            {
                owner.Controls.Remove(toast);
                toast.Dispose();
                timer.Dispose();
            };
            timer.Start();
        }
    }

    public enum ToastKind { Info, Success, Warning, Error }
}
