using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SaveRestoreGUI.Services;

namespace SaveRestoreGUI.UI
{
    public class CheckItem
    {
        public string Key            { get; }
        public string Text           { get; }
        public string Icon           { get; }
        public bool   DefaultChecked { get; }
        public CheckItem(string key, string text, string icon, bool defaultChecked = true)
        { Key = key; Text = text; Icon = icon; DefaultChecked = defaultChecked; }
    }

    public class CheckCategory
    {
        public string      Label    { get; }
        public string      Icon     { get; }
        public CheckItem[] Items    { get; }
        public bool        Expanded { get; set; } = true;
        public CheckCategory(string label, string icon, CheckItem[] items)
        { Label = label; Icon = icon; Items = items; }
    }

    public class CategoryCheckPanel : Panel
    {
        // ── Dimensions
        private const int Cols       = 5;    // tuiles par ligne
        private const int HeaderH    = 34;
        private const int TileW      = 100;  // largeur fixe d'une tuile
        private const int TileH      = 90;   // hauteur fixe d'une tuile
        private const int TileGap    = 8;    // espace entre tuiles
        private const int HorizPad   = 10;
        private const int CheckBoxW  = 16;
        private const int TileRadius = 8;

        private List<CheckCategory>      _categories = new();
        private Dictionary<string, bool> _checked    = new();
        private string?                  _hoverItem  = null;

        public event EventHandler? CheckedChanged;

        public CategoryCheckPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            AutoScroll     = true;
            Cursor         = Cursors.Hand;

            MouseClick += OnMouseClick;
            MouseMove  += OnMouseMove;
            MouseLeave += (_, _) => { _hoverItem = null; Invalidate(); };
        }

        // ── API publique
        public void SetCategories(IEnumerable<CheckCategory> categories)
        {
            _categories = new List<CheckCategory>(categories);
            _checked.Clear();
            foreach (var cat in _categories)
                foreach (var item in cat.Items)
                    _checked[item.Key] = item.DefaultChecked;
            UpdateScrollBounds();
            Invalidate();
        }

        public bool IsChecked(string key) => _checked.TryGetValue(key, out var v) && v;

        public void SetChecked(string key, bool value)
        {
            if (_checked.ContainsKey(key)) { _checked[key] = value; Invalidate(); }
        }

        public void SetAll(bool value)
        {
            foreach (var k in _checked.Keys.ToList()) _checked[k] = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyAutoDetect(AutoDetectResult r)
        {
            if (r.DesktopOnOneDrive)   SetChecked("Desktop",    false);
            if (r.DocumentsOnOneDrive) SetChecked("Documents",  false);
            if (r.PicturesOnOneDrive)  SetChecked("Pictures",   false);
            SetChecked("Sap",         r.SapDetected);
            SetChecked("IpSoftphone", r.IpSoftphoneDetected);
            SetChecked("Outlook",     r.OutlookDetected);
            SetChecked("StickyNotes", r.StickyNotesDetected);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateScrollBounds(); }

        private void UpdateScrollBounds()
        {
            AutoScrollMinSize = new Size(0, TotalContentHeight());
        }

        // ── Calculs de layout
        private int ColsActual()
        {
            int available = Width - HorizPad * 2;
            if (available <= 0) return Cols;
            // Combien de tuiles rentrent ? Au moins 1.
            int n = (available + TileGap) / (TileW + TileGap);
            return Math.Max(1, n);
        }

        private int ActualTileW()
        {
            int cols      = ColsActual();
            int available = Width - HorizPad * 2;
            // Répartir l'espace équitablement, en gardant le gap
            return (available - TileGap * (cols - 1)) / cols;
        }

        private int RowsForCat(CheckCategory cat)
        {
            int cols = ColsActual();
            return (cat.Items.Length + cols - 1) / cols;
        }

        private int GridHeightForCat(CheckCategory cat)
            => RowsForCat(cat) * TileH + Math.Max(0, RowsForCat(cat) - 1) * TileGap;

        private int TotalContentHeight()
        {
            int h = 4;
            foreach (var cat in _categories)
            {
                h += HeaderH + 6;
                if (cat.Expanded) h += GridHeightForCat(cat) + 8;
            }
            return h + 4;
        }

        // ── Rendu
        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(p.Surface);

            int offsetY = AutoScrollPosition.Y;
            int y       = offsetY + 4;

            foreach (var cat in _categories)
            {
                DrawCategoryHeader(g, p, cat, y);
                y += HeaderH + 6;
                if (cat.Expanded)
                {
                    DrawGrid(g, p, cat, y);
                    y += GridHeightForCat(cat) + 8;
                }
            }
        }

        private void DrawCategoryHeader(Graphics g, ThemePalette p, CheckCategory cat, int y)
        {
            if (y + HeaderH < 0 || y > Height) return;
            var rect = new Rectangle(HorizPad, y, Width - HorizPad * 2, HeaderH);
            using var bgPath  = RoundRect(rect, 7);
            using var bgBrush = new SolidBrush(Color.FromArgb(22, p.Accent.R, p.Accent.G, p.Accent.B));
            g.FillPath(bgBrush, bgPath);
            using var accentBrush = new SolidBrush(p.Accent);
            g.FillRectangle(accentBrush, new RectangleF(HorizPad, y + 8, 3.5f, HeaderH - 16));

            // Icône catégorie
            using var emojiFont = new Font("Segoe UI Emoji", 11f);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(p.Text);
            g.DrawString(cat.Icon, emojiFont, textBrush, new RectangleF(HorizPad + 10, y, 26, HeaderH), sf);

            // Label
            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            TextRenderer.DrawText(g, cat.Label, labelFont,
                new Rectangle(HorizPad + 40, y, Width - HorizPad * 2 - 60, HeaderH),
                p.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // Chevron
            using var chevFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            TextRenderer.DrawText(g, cat.Expanded ? "▾" : "▸", chevFont,
                new Rectangle(rect.Right - 22, y, 20, HeaderH),
                p.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void DrawGrid(Graphics g, ThemePalette p, CheckCategory cat, int y)
        {
            int cols  = ColsActual();
            int tileW = ActualTileW();

            for (int i = 0; i < cat.Items.Length; i++)
            {
                int col  = i % cols;
                int row  = i / cols;
                int tx   = HorizPad + col * (tileW + TileGap);
                int ty   = y + row * (TileH + TileGap);

                if (ty + TileH < 0 || ty > Height) continue;

                DrawTile(g, p, cat.Items[i], tx, ty, tileW);
            }
        }

        private void DrawTile(Graphics g, ThemePalette p, CheckItem item, int x, int y, int w)
        {
            bool chk     = _checked.TryGetValue(item.Key, out var cv) && cv;
            bool hovered = _hoverItem == item.Key;

            // Fond de la tuile
            var tileRect = new Rectangle(x, y, w, TileH);
            using var tilePath = RoundRect(tileRect, TileRadius);

            Color bgColor;
            if (chk)
                bgColor = Color.FromArgb(hovered ? 55 : 38, p.Accent.R, p.Accent.G, p.Accent.B);
            else
                bgColor = Color.FromArgb(hovered ? 30 : 16, p.Text.R, p.Text.G, p.Text.B);

            using var bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, tilePath);

            // Bordure accent si coché
            if (chk)
            {
                using var borderPen = new Pen(Color.FromArgb(120, p.Accent.R, p.Accent.G, p.Accent.B), 1.5f);
                g.DrawPath(borderPen, tilePath);
            }
            else
            {
                using var borderPen = new Pen(Color.FromArgb(40, p.Border.R, p.Border.G, p.Border.B), 1f);
                g.DrawPath(borderPen, tilePath);
            }

            // Checkbox en haut à gauche
            int cbx = x + 8;
            int cby = y + 8;
            var boxRect = new Rectangle(cbx, cby, CheckBoxW, CheckBoxW);
            using var boxPath = RoundRect(boxRect, 4);
            if (chk)
            {
                using var fillBrush = new SolidBrush(p.Accent);
                g.FillPath(fillBrush, boxPath);
                using var checkPen = new Pen(Color.White, 1.8f);
                g.DrawLines(checkPen, new[]
                {
                    new PointF(cbx + 2,  cby + CheckBoxW / 2f),
                    new PointF(cbx + 6,  cby + CheckBoxW - 3f),
                    new PointF(cbx + 14, cby + 3f)
                });
            }
            else
            {
                using var emptyBrush = new SolidBrush(p.InputBackground);
                g.FillPath(emptyBrush, boxPath);
                using var bPen = new Pen(p.Border, 1.2f);
                g.DrawPath(bPen, boxPath);
            }

            // Icône centrée (emoji grand)
            using var emojiFont = new Font("Segoe UI Emoji", 22f);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var emojiB = new SolidBrush(Color.FromArgb(chk ? 255 : 160, p.Text));
            // Zone icône : centre de la tuile, un peu au-dessus du texte
            g.DrawString(item.Icon, emojiFont, emojiB,
                new RectangleF(x, y + 10, w, TileH - 28), sf);

            // Texte en bas de la tuile
            using var textFont  = new Font("Segoe UI", 7.5f);
            using var textBrush = new SolidBrush(chk ? p.Text : p.TextSecondary);
            var textRect = new Rectangle(x + 2, y + TileH - 22, w - 4, 20);
            TextRenderer.DrawText(g, item.Text, textFont, textRect,
                chk ? p.Text : p.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
        }

        // ── Interactions
        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            int y = AutoScrollPosition.Y + 4;
            foreach (var cat in _categories)
            {
                // Clic sur le header
                if (new Rectangle(HorizPad, y, Width - HorizPad * 2, HeaderH).Contains(e.Location))
                {
                    cat.Expanded = !cat.Expanded;
                    UpdateScrollBounds();
                    Invalidate();
                    return;
                }
                y += HeaderH + 6;

                if (cat.Expanded)
                {
                    int cols  = ColsActual();
                    int tileW = ActualTileW();
                    for (int i = 0; i < cat.Items.Length; i++)
                    {
                        int col  = i % cols;
                        int row  = i / cols;
                        int tx   = HorizPad + col * (tileW + TileGap);
                        int ty   = y + row * (TileH + TileGap);
                        if (new Rectangle(tx, ty, tileW, TileH).Contains(e.Location))
                        {
                            _checked[cat.Items[i].Key] = !_checked[cat.Items[i].Key];
                            Invalidate();
                            CheckedChanged?.Invoke(this, EventArgs.Empty);
                            return;
                        }
                    }
                    y += GridHeightForCat(cat) + 8;
                }
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            var hit = HitTestItem(e.Location);
            if (hit != _hoverItem) { _hoverItem = hit; Invalidate(); }
        }

        private string? HitTestItem(Point pt)
        {
            int y = AutoScrollPosition.Y + 4;
            foreach (var cat in _categories)
            {
                y += HeaderH + 6;
                if (cat.Expanded)
                {
                    int cols  = ColsActual();
                    int tileW = ActualTileW();
                    for (int i = 0; i < cat.Items.Length; i++)
                    {
                        int col = i % cols;
                        int row = i / cols;
                        int tx  = HorizPad + col * (tileW + TileGap);
                        int ty  = y + row * (TileH + TileGap);
                        if (new Rectangle(tx, ty, tileW, TileH).Contains(pt))
                            return cat.Items[i].Key;
                    }
                    y += GridHeightForCat(cat) + 8;
                }
            }
            return null;
        }

        private int TotalContentHeight()
        {
            int h = 4;
            foreach (var cat in _categories)
            {
                h += HeaderH + 6;
                if (cat.Expanded) h += GridHeightForCat(cat) + 8;
            }
            return h + 4;
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d    = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public static class CheckCatalog
    {
        public static CheckCategory[] Build(
            bool includeOldProfile = false,
            bool includeLaunchApps = false)
        {
            var userFiles = new List<CheckItem>
            {
                new("Desktop",    "Bureau",                    "🖥️"),
                new("Documents",  "Documents",                 "📄"),
                new("Pictures",   "Images",                    "📁"),
                new("Videos",     "Vidéos",                    "🎬"),
                new("Downloads",  "Téléchargements",           "⬇️"),
                new("Music",      "Musique",                   "🎵"),
                new("Public",     "Dossier Public",            "📂"),
            };
            if (includeOldProfile)
                userFiles.Add(new("OldProfile", "Ancien profil", "👤", false));

            var office = new CheckItem[]
            {
                new("Outlook",         "PST Outlook",      "📧"),
                new("Signatures",      "Signatures",       "✍️"),
                new("OfficeTemplates", "Modèles Office",   "📋"),
                new("OneNote",         "OneNote",          "📓"),
                new("StickyNotes",     "Sticky Notes",     "📌"),
                new("ExcelMacros",     "Macros Excel",     "📊"),
            };

            var systemItems = new List<CheckItem>
            {
                new("Wallpaper",     "Fond d'écran",   "🖼️"),
                new("NetworkDrives", "Lecteurs réseau", "🔗"),
            };
            if (includeLaunchApps)
                systemItems.Add(new("LaunchApps", "Applications", "🚀"));

            var business = new CheckItem[]
            {
                new("Sap",         "SAP GUI",         "🗂️", false),
                new("IpSoftphone", "IP Softphone",    "📞", false),
            };

            return new[]
            {
                new CheckCategory("Fichiers utilisateur",       "📁", userFiles.ToArray()),
                new CheckCategory("Bureautique",                "💼", office),
                new CheckCategory("Système & Personnalisation", "⚙️", systemItems.ToArray()),
                new CheckCategory("Logiciels métier",           "🏢", business),
            };
        }
    }
}
