using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Versioning;
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

    [SupportedOSPlatform("windows")]
    public class CategoryCheckPanel : Panel
    {
        // ── Dimensions
        private const int HeaderH    = 34;
        private const int TileW      = 100;
        private const int TileH      = 90;
        private const int TileGap    = 8;
        private const int HorizPad   = 10;
        private const int CheckBoxW  = 16;
        private const int TileRadius = 8;

        private List<CheckCategory>      _categories = new();
        private Dictionary<string, bool> _checked    = new();
        private string?                  _hoverItem;

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

        /// <summary>
        /// Applique les résultats d'auto-détection :
        /// - Décoche les dossiers synchronisés par OneDrive.
        /// - Coche les logiciels détectés, décoche ceux qui sont absents.
        /// - Coche/décoche les éléments dont la présence a été sondée.
        /// </summary>
        public void ApplyAutoDetect(AutoDetectResult r)
        {
            // OneDrive
            if (r.DesktopOnOneDrive)   SetChecked("Desktop",   false);
            if (r.DocumentsOnOneDrive) SetChecked("Documents", false);
            if (r.PicturesOnOneDrive)  SetChecked("Pictures",  false);

            // Logiciels métier (présents → coché, absents → décoché)
            SetChecked("Sap",         r.SapDetected);
            SetChecked("IpSoftphone", r.IpSoftphoneDetected);
            SetChecked("Outlook",     r.OutlookDetected);
            SetChecked("StickyNotes", r.StickyNotesDetected);

            // Données bureautique / système (coché uniquement si données présentes)
            SetChecked("Wallpaper",      r.HasWallpaper);
            SetChecked("NetworkDrives",  r.HasNetworkDrives);
            SetChecked("OneNote",        r.HasOneNote);
            SetChecked("Signatures",     r.HasSignatures);
            SetChecked("OfficeTemplates",r.HasOfficeTemplates);
            SetChecked("ExcelMacros",    r.HasExcelMacros);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); UpdateScrollBounds(); }

        private void UpdateScrollBounds() => AutoScrollMinSize = new Size(0, CalcTotalHeight());

        // ── Calculs de layout
        private int ColsActual()
        {
            int available = Width - HorizPad * 2;
            if (available <= 0) return 5;
            int n = (available + TileGap) / (TileW + TileGap);
            return Math.Max(1, n);
        }

        private int ActualTileW()
        {
            int cols      = ColsActual();
            int available = Width - HorizPad * 2;
            return cols <= 1 ? available : (available - TileGap * (cols - 1)) / cols;
        }

        private int RowsForCat(CheckCategory cat)
        {
            int cols = ColsActual();
            return (cat.Items.Length + cols - 1) / cols;
        }

        private int GridHeightForCat(CheckCategory cat)
        {
            int rows = RowsForCat(cat);
            return rows * TileH + Math.Max(0, rows - 1) * TileGap;
        }

        private int CalcTotalHeight()
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

            int y = AutoScrollPosition.Y + 4;
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

            using var emojiFont = new Font("Segoe UI Emoji", 11f);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(p.Text);
            g.DrawString(cat.Icon, emojiFont, textBrush, new RectangleF(HorizPad + 10, y, 26, HeaderH), sf);

            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            TextRenderer.DrawText(g, cat.Label, labelFont,
                new Rectangle(HorizPad + 40, y, Width - HorizPad * 2 - 60, HeaderH),
                p.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

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
                int col = i % cols;
                int row = i / cols;
                int tx  = HorizPad + col * (tileW + TileGap);
                int ty  = y + row * (TileH + TileGap);
                if (ty + TileH < 0 || ty > Height) continue;
                DrawTile(g, p, cat.Items[i], tx, ty, tileW);
            }
        }

        private void DrawTile(Graphics g, ThemePalette p, CheckItem item, int x, int y, int w)
        {
            bool chk     = _checked.TryGetValue(item.Key, out var cv) && cv;
            bool hovered = _hoverItem == item.Key;

            var tileRect = new Rectangle(x, y, w, TileH);
            using var tilePath = RoundRect(tileRect, TileRadius);

            Color bgColor = chk
                ? Color.FromArgb(hovered ? 55 : 38, p.Accent.R, p.Accent.G, p.Accent.B)
                : Color.FromArgb(hovered ? 30 : 16, p.Text.R,   p.Text.G,   p.Text.B);

            using var bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, tilePath);

            if (chk)
            {
                using var bp = new Pen(Color.FromArgb(120, p.Accent.R, p.Accent.G, p.Accent.B), 1.5f);
                g.DrawPath(bp, tilePath);
            }
            else
            {
                using var bp = new Pen(Color.FromArgb(40, p.Border.R, p.Border.G, p.Border.B), 1f);
                g.DrawPath(bp, tilePath);
            }

            // Checkbox
            int cbx = x + 8, cby = y + 8;
            var boxRect = new Rectangle(cbx, cby, CheckBoxW, CheckBoxW);
            using var boxPath = RoundRect(boxRect, 4);
            if (chk)
            {
                using var fb = new SolidBrush(p.Accent);
                g.FillPath(fb, boxPath);
                using var cp = new Pen(Color.White, 1.8f);
                g.DrawLines(cp, new[]
                {
                    new PointF(cbx + 2,  cby + CheckBoxW / 2f),
                    new PointF(cbx + 6,  cby + CheckBoxW - 3f),
                    new PointF(cbx + 14, cby + 3f)
                });
            }
            else
            {
                using var eb = new SolidBrush(p.InputBackground);
                g.FillPath(eb, boxPath);
                using var bp = new Pen(p.Border, 1.2f);
                g.DrawPath(bp, boxPath);
            }

            // Icône emoji centrée
            using var emojiFont = new Font("Segoe UI Emoji", 22f);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var emojiB = new SolidBrush(Color.FromArgb(chk ? 255 : 160, p.Text));
            g.DrawString(item.Icon, emojiFont, emojiB, new RectangleF(x, y + 10, w, TileH - 28), sf);

            // Texte en bas
            using var textFont  = new Font("Segoe UI", 7.5f);
            Color textColor = chk ? p.Text : p.TextSecondary;
            TextRenderer.DrawText(g, item.Text, textFont,
                new Rectangle(x + 2, y + TileH - 22, w - 4, 20),
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
        }

        // ── Interactions
        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            int y = AutoScrollPosition.Y + 4;
            foreach (var cat in _categories)
            {
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
                        int tx = HorizPad + (i % cols) * (tileW + TileGap);
                        int ty = y        + (i / cols) * (TileH  + TileGap);
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
                        int tx = HorizPad + (i % cols) * (tileW + TileGap);
                        int ty = y        + (i / cols) * (TileH  + TileGap);
                        if (new Rectangle(tx, ty, tileW, TileH).Contains(pt))
                            return cat.Items[i].Key;
                    }
                    y += GridHeightForCat(cat) + 8;
                }
            }
            return null;
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
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
                new("Desktop",    "Bureau",           "🖥️"),
                new("Documents",  "Documents",        "📄"),
                new("Pictures",   "Images",           "🗃️"),
                new("Videos",     "Vidéos",           "🎬"),
                new("Downloads",  "Téléchargements",  "⬇️"),
                new("Music",      "Musique",          "🎵"),
                new("Public",     "Dossier Public",   "📂"),
            };
            if (includeOldProfile)
                userFiles.Add(new("OldProfile", "Ancien profil", "👤", false));

            var office = new CheckItem[]
            {
                new("Outlook",         "PST Outlook",    "📧",  false),
                new("Signatures",      "Signatures",     "✍️",  false),
                new("OfficeTemplates", "Modèles Office", "📋",  false),
                new("OneNote",         "OneNote",        "📓",  false),
                new("StickyNotes",     "Sticky Notes",   "📌",  false),
                new("ExcelMacros",     "Macros Excel",   "📊",  false),
            };

            var systemItems = new List<CheckItem>
            {
                new("Wallpaper",     "Fond d'écran",    "🖼️",  false),
                new("NetworkDrives", "Lecteurs réseau", "🔗",  false),
            };
            if (includeLaunchApps)
                systemItems.Add(new("LaunchApps", "Applications", "🚀"));

            var business = new CheckItem[]
            {
                new("Sap",         "SAP GUI",      "🗂️", false),
                new("IpSoftphone", "IP Softphone", "📞", false),
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
