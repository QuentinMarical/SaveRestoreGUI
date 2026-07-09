using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using SaveRestoreGUI.Services;

namespace SaveRestoreGUI.UI
{
    public class CheckItem(string key, string text, string icon, bool defaultChecked = true)
    {
        public string Key            { get; } = key;
        public string Text           { get; } = text;
        public string Icon           { get; } = icon;
        public bool   DefaultChecked { get; } = defaultChecked;
    }

    public class CheckCategory(string label, string icon, CheckItem[] items)
    {
        public string      Label    { get; } = label;
        public string      Icon     { get; } = icon;
        public CheckItem[] Items    { get; } = items;
        public bool        Expanded { get; set; } = true;
    }

    [SupportedOSPlatform("windows")]
    public class CategoryCheckPanel : Panel
    {
        // ── Layout général ──────────────────────────────────────────────────
        private const int HeaderH    = 34;
        private const int TileW      = 100;
        private const int TileH      = 90;
        private const int TileGap    = 8;
        private const int HorizPad   = 10;
        private const int CheckBoxW  = 16;
        private const int TileRadius = 8;

        // ── Zone icône ──────────────────────────────────────────────────────
        private const int IconZoneTop    = 26;
        private const int IconZoneHeight = 42;
        private const int IconSize       = 32;

        // ── Couleurs de fallback fixes par app ──────────────────────────────
        private static readonly Dictionary<string, Color> _appColors = new()
        {
            { "Outlook",         Color.FromArgb(  0, 120, 212) },
            { "OneNote",         Color.FromArgb(119,  25, 170) },
            { "ExcelMacros",     Color.FromArgb( 33, 115,  70) },
            { "OfficeTemplates", Color.FromArgb(216,  59,   1) },
            { "Signatures",      Color.FromArgb( 70, 100, 140) },
            { "BrowserEdge",     Color.FromArgb(  0, 120, 215) },
            { "BrowserChrome",   Color.FromArgb( 66, 133,  44) },
            { "BrowserBrave",    Color.FromArgb(251, 116,  77) },
            { "BrowserVivaldi",  Color.FromArgb(239,  64,  64) },
            { "BrowserOpera",    Color.FromArgb(255,  24,  24) },
            { "BrowserOperaGX",  Color.FromArgb(220,  20,  60) },
            { "BrowserFirefox",  Color.FromArgb(255, 103,   0) },
            { "BrowserLibreWolf",Color.FromArgb(  0, 170, 255) },
            { "BrowserPaleMoon", Color.FromArgb( 73,  97, 165) },
            { "BrowserTor",      Color.FromArgb(126,  56, 178) },
            { "BrowserDDG",      Color.FromArgb(222,  88,  48) },
            { "BrowserArc",      Color.FromArgb( 90,  90, 220) },
            { "BrowserComet",    Color.FromArgb( 32, 178, 170) },
        };

        private List<CheckCategory>      _categories = [];
        private readonly Dictionary<string, bool> _checked    = [];
        private string?                  _hoverItem;

        private readonly Dictionary<string, Bitmap?> _nativeCache = [];

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

        // ── API publique ──────────────────────────────────────────────────

        public void SetCategories(IEnumerable<CheckCategory> categories)
        {
            _categories = [.. categories];
            _checked.Clear();
            _nativeCache.Clear();
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
            if (r.DesktopOnOneDrive)   SetChecked("Desktop",   false);
            if (r.DocumentsOnOneDrive) SetChecked("Documents", false);
            if (r.PicturesOnOneDrive)  SetChecked("Pictures",  false);

            SetChecked("Outlook",     r.OutlookDetected);
            SetChecked("StickyNotes", r.StickyNotesDetected);

            SetChecked("Wallpaper",       r.HasWallpaper);
            SetChecked("NetworkDrives",   r.HasNetworkDrives);
            SetChecked("OneNote",         r.HasOneNote);
            SetChecked("Signatures",      r.HasSignatures);
            SetChecked("OfficeTemplates", r.HasOfficeTemplates);
            SetChecked("ExcelMacros",     r.HasExcelMacros);

            foreach (var browser in BrowserService.All)
            {
                bool shouldCheck = r.BrowsersToPreCheck.Contains(browser.Key);
                SetChecked(browser.Key, shouldCheck);
            }
        }

        public void InvalidateIcons()
        {
            _nativeCache.Clear();
            SvgIcons.ClearCache();
            WindowsIcons.ClearCache();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        { base.OnResize(e); UpdateScrollBounds(); }

        private void UpdateScrollBounds() => AutoScrollMinSize = new Size(0, CalcTotalHeight());

        // ── Résolution d'icône ────────────────────────────────────────────

        private Bitmap GetItemIcon(CheckItem item)
        {
            if (!_nativeCache.TryGetValue(item.Icon, out var native))
            {
                native = WindowsIcons.Get(item.Icon, IconSize);
                _nativeCache[item.Icon] = native;
            }
            if (native != null) return native;

            Color fallback = _appColors.TryGetValue(item.Icon, out var ac)
                ? ac
                : Color.FromArgb(100, 120, 140);

            return SvgIcons.Get(item.Icon, IconSize, fallback);
        }

        // ── Calculs de layout ────────────────────────────────────────────

        private int ColsActual()
        {
            int available = Width - HorizPad * 2;
            if (available <= 0) return 5;
            return Math.Max(1, (available + TileGap) / (TileW + TileGap));
        }

        private int ActualTileW()
        {
            int cols      = ColsActual();
            int available = Width - HorizPad * 2;
            return cols <= 1 ? available : (available - TileGap * (cols - 1)) / cols;
        }

        private int GridHeightForCat(CheckCategory cat)
        {
            int cols = ColsActual();
            int rows = (cat.Items.Length + cols - 1) / cols;
            return rows * TileH + Math.Max(0, rows - 1) * TileGap;
        }

        private int CalcTotalHeight()
        {
            int h = 2;
            foreach (var cat in _categories)
            {
                h += HeaderH + 6;
                if (cat.Expanded) h += GridHeightForCat(cat) + 8;
            }
            return h + 4;
        }

        // ── Rendu principal ──────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(p.Surface);

            int y = AutoScrollPosition.Y + 2;
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
            TextRenderer.DrawText(g, cat.Expanded ? "\u25be" : "\u25b8", chevFont,
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
                int ty  = y + row  * (TileH  + TileGap);
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

            using var borderPen = chk
                ? new Pen(Color.FromArgb(120, p.Accent.R, p.Accent.G, p.Accent.B), 1.5f)
                : new Pen(Color.FromArgb(40,  p.Border.R, p.Border.G, p.Border.B), 1f);
            g.DrawPath(borderPen, tilePath);

            int cbx = x + 8, cby = y + 6;
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
                using var bp2 = new Pen(p.Border, 1.2f);
                g.DrawPath(bp2, boxPath);
            }

            var bmp  = GetItemIcon(item);
            int iconX = x + (w        - IconSize)      / 2;
            int iconY = y + IconZoneTop + (IconZoneHeight - IconSize) / 2;

            if (chk)
            {
                g.DrawImage(bmp, iconX, iconY, IconSize, IconSize);
            }
            else
            {
                using var ia = new ImageAttributes();
                var cm = new ColorMatrix { Matrix33 = 0.45f };
                ia.SetColorMatrix(cm);
                g.DrawImage(bmp,
                    new Rectangle(iconX, iconY, IconSize, IconSize),
                    0, 0, IconSize, IconSize,
                    GraphicsUnit.Pixel, ia);
            }

            using var textFont  = new Font("Segoe UI", 7.5f);
            Color     textColor = chk ? p.Text : p.TextSecondary;
            TextRenderer.DrawText(g, item.Text, textFont,
                new Rectangle(x + 2, y + TileH - 22, w - 4, 20),
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
        }

        // ── Interactions ─────────────────────────────────────────────────

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            int y = AutoScrollPosition.Y + 2;
            foreach (var cat in _categories)
            {
                var headerRect = new Rectangle(HorizPad, y, Width - HorizPad * 2, HeaderH);
                if (headerRect.Contains(e.Location))
                {
                    cat.Expanded = !cat.Expanded;
                    UpdateScrollBounds();
                    Invalidate();
                    return;
                }

                y += HeaderH + 6;
                if (!cat.Expanded) continue;

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

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            var hit = HitTestItem(e.Location);
            if (hit != _hoverItem) { _hoverItem = hit; Invalidate(); }
        }

        private string? HitTestItem(Point pt)
        {
            int y = AutoScrollPosition.Y + 2;
            foreach (var cat in _categories)
            {
                y += HeaderH + 6;
                if (!cat.Expanded) continue;

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
                new("Desktop",   "Bureau",          "Desktop"),
                new("Documents", "Documents",       "Documents"),
                new("Pictures",  "Images",          "Pictures"),
                new("Videos",    "Vid\u00e9os",     "Videos"),
                new("Downloads", "T\u00e9l\u00e9chargements", "Downloads"),
                new("Music",     "Musique",         "Music"),
                new("Public",    "Dossier Public",  "Public"),
            };
            if (includeOldProfile)
                userFiles.Add(new("OldProfile", "Ancien profil", "OldProfile", false));

            CheckItem[] office =
            [
                new("Outlook",         "PST Outlook",    "Outlook",         false),
                new("Signatures",      "Signatures",     "Signatures",      false),
                new("OfficeTemplates", "Mod\u00e8les Office", "OfficeTemplates", false),
                new("OneNote",         "OneNote",        "OneNote",         false),
                new("StickyNotes",     "Sticky Notes",   "StickyNotes",     false),
                new("ExcelMacros",     "Macros Excel",   "ExcelMacros",     false),
            ];

            var systemItems = new List<CheckItem>
            {
                new("Wallpaper",     "Fond d'\u00e9cran",    "Wallpaper",     false),
                new("NetworkDrives", "Lecteurs r\u00e9seau", "NetworkDrives", false),
            };
            if (includeLaunchApps)
                systemItems.Add(new("LaunchApps", "Applications", "LaunchApps"));

            var browserItems = BrowserService.All
                .Select(b => new CheckItem(b.Key, b.DisplayName, b.Key, defaultChecked: false))
                .ToArray();

            return
            [
                new CheckCategory("Fichiers utilisateur",       "\U0001f4c1", userFiles.ToArray()),
                new CheckCategory("Bureautique",                "\U0001f4bc", office),
                new CheckCategory("Navigateurs",                "\U0001f310", browserItems),
                new CheckCategory("Syst\u00e8me & Personnalisation", "\u2699\ufe0f", systemItems.ToArray()),
            ];
        }
    }
}
