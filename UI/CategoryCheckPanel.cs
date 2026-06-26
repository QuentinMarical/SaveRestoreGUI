using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SaveRestoreGUI.UI
{
    // ─────────────────────────────────────────────────────────────────────────
    // Descripteur d'une case à cocher dans une catégorie
    // ─────────────────────────────────────────────────────────────────────────
    public class CheckItem
    {
        /// <summary>Clé unique (nom de propriété logique, ex. "Documents").</summary>
        public string Key  { get; }
        /// <summary>Libellé affiché.</summary>
        public string Text { get; }
        /// <summary>Icône emoji affiché à gauche (style icône bureau).</summary>
        public string Icon { get; }
        /// <summary>Coché par défaut ?</summary>
        public bool DefaultChecked { get; }

        public CheckItem(string key, string text, string icon, bool defaultChecked = true)
        { Key = key; Text = text; Icon = icon; DefaultChecked = defaultChecked; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Un groupe de cases (catégorie repliable)
    // ─────────────────────────────────────────────────────────────────────────
    public class CheckCategory
    {
        public string      Label    { get; }
        public string      Icon     { get; }
        public CheckItem[] Items    { get; }
        public bool        Expanded { get; set; } = true;

        public CheckCategory(string label, string icon, CheckItem[] items)
        { Label = label; Icon = icon; Items = items; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CategoryCheckPanel  – liste de catégories repliables avec cases à cocher
    // rendu style icônes Bureau / Explorateur Windows
    // ─────────────────────────────────────────────────────────────────────────
    public class CategoryCheckPanel : Panel
    {
        // ── Constantes layout
        private const int HeaderH   = 34;   // hauteur bandeau catégorie
        private const int ItemH     = 34;   // hauteur d'une case
        private const int CheckBoxW = 18;   // taille carré de la case
        private const int HorizPad  = 10;   // marge gauche/droite
        private const int ItemRadius = 6;   // arrondi case survolée

        private List<CheckCategory>      _categories  = new();
        private Dictionary<string, bool> _checked     = new();
        private string?                  _hoverItem   = null;
        private int                      _scrollOffset = 0;

        public event EventHandler? CheckedChanged;

        public CategoryCheckPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            AutoScroll     = false;
            Cursor         = Cursors.Hand;
            MouseWheel += OnMouseWheel;
            MouseClick += OnMouseClick;
            MouseMove  += OnMouseMove;
            MouseLeave += (_, __) => { _hoverItem = null; Invalidate(); };
        }

        // ── API publique ───────────────────────────────────────────────────

        public void SetCategories(IEnumerable<CheckCategory> categories)
        {
            _categories = new List<CheckCategory>(categories);
            _checked.Clear();
            foreach (var cat in _categories)
                foreach (var item in cat.Items)
                    _checked[item.Key] = item.DefaultChecked;
            _scrollOffset = 0;
            Invalidate();
        }

        public bool IsChecked(string key)
            => _checked.TryGetValue(key, out var v) && v;

        public void SetChecked(string key, bool value)
        { if (_checked.ContainsKey(key)) { _checked[key] = value; Invalidate(); } }

        public void SetAll(bool value)
        {
            foreach (var k in _checked.Keys.ToList())
                _checked[k] = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Rendu ─────────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Palette;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(p.Surface);

            int y = -_scrollOffset + 4;

            foreach (var cat in _categories)
            {
                DrawCategoryHeader(g, p, cat, y);
                y += HeaderH + 4;

                if (cat.Expanded)
                {
                    foreach (var item in cat.Items)
                    {
                        DrawItem(g, p, item, y);
                        y += ItemH;
                    }
                    y += 6;
                }
            }
        }

        private void DrawCategoryHeader(Graphics g, ThemePalette p, CheckCategory cat, int y)
        {
            if (y + HeaderH < 0 || y > Height) return;

            var rect = new Rectangle(HorizPad, y, Width - HorizPad * 2, HeaderH);

            // Fond légèrement teinté
            using var bgPath  = RoundRect(rect, 7);
            using var bgBrush = new SolidBrush(Color.FromArgb(22, p.Accent.R, p.Accent.G, p.Accent.B));
            g.FillPath(bgBrush, bgPath);

            // Barre d'accent gauche
            using var accentBrush = new SolidBrush(p.Accent);
            g.FillRectangle(accentBrush, new RectangleF(HorizPad, y + 8, 3.5f, HeaderH - 16));

            // Chevron
            string chevron = cat.Expanded ? "▾" : "▸";
            TextRenderer.DrawText(g, chevron,
                new Font("Segoe UI", 9f, FontStyle.Bold),
                new Rectangle(rect.Right - 22, y, 20, HeaderH),
                p.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Icône + libellé
            TextRenderer.DrawText(g, $"{cat.Icon}  {cat.Label}",
                new Font("Segoe UI", 9f, FontStyle.Bold),
                new Rectangle(HorizPad + 10, y, Width - HorizPad * 2 - 28, HeaderH),
                p.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private void DrawItem(Graphics g, ThemePalette p, CheckItem item, int y)
        {
            if (y + ItemH < 0 || y > Height) return;

            bool chk      = _checked.TryGetValue(item.Key, out var cv) && cv;
            bool hovered  = _hoverItem == item.Key;

            // Fond survol
            if (hovered)
            {
                var bgRect = new Rectangle(HorizPad + 4, y + 2, Width - HorizPad * 2 - 8, ItemH - 4);
                using var bgPath  = RoundRect(bgRect, ItemRadius);
                using var bgBrush = new SolidBrush(Color.FromArgb(28, p.Accent.R, p.Accent.G, p.Accent.B));
                g.FillPath(bgBrush, bgPath);
            }

            // Case à cocher
            int cx      = HorizPad + 16;
            int cy      = y + (ItemH - CheckBoxW) / 2;
            var boxRect = new Rectangle(cx, cy, CheckBoxW, CheckBoxW);
            using var boxPath = RoundRect(boxRect, 5);
            if (chk)
            {
                using var fillBrush = new SolidBrush(p.Accent);
                g.FillPath(fillBrush, boxPath);
                using var checkPen = new Pen(Color.White, 2f);
                g.DrawLines(checkPen, new[]
                {
                    new PointF(cx + 3,  cy + CheckBoxW / 2f),
                    new PointF(cx + 7,  cy + CheckBoxW - 4f),
                    new PointF(cx + 15, cy + 4f)
                });
            }
            else
            {
                using var emptyBrush = new SolidBrush(p.InputBackground);
                g.FillPath(emptyBrush, boxPath);
                using var borderPen = new Pen(p.Border, 1.4f);
                g.DrawPath(borderPen, boxPath);
            }

            // Icône emoji style bureau Windows (24px)
            int iconX = cx + CheckBoxW + 8;
            using var emojiFont = new Font("Segoe UI Emoji", 13f);
            TextRenderer.DrawText(g, item.Icon, emojiFont,
                new Rectangle(iconX, y, 26, ItemH),
                p.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Libellé
            TextRenderer.DrawText(g, item.Text,
                new Font("Segoe UI", 9.5f),
                new Rectangle(iconX + 28, y, Width - iconX - 40, ItemH),
                chk ? p.Text : p.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // ── Interactions ───────────────────────────────────────────────────

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            int y = -_scrollOffset + 4;
            foreach (var cat in _categories)
            {
                // Clic bandeau → plier / déplier
                if (new Rectangle(HorizPad, y, Width - HorizPad * 2, HeaderH).Contains(e.Location))
                {
                    cat.Expanded = !cat.Expanded;
                    Invalidate();
                    return;
                }
                y += HeaderH + 4;

                if (cat.Expanded)
                {
                    foreach (var item in cat.Items)
                    {
                        if (new Rectangle(0, y, Width, ItemH).Contains(e.Location))
                        {
                            _checked[item.Key] = !_checked[item.Key];
                            Invalidate();
                            CheckedChanged?.Invoke(this, EventArgs.Empty);
                            return;
                        }
                        y += ItemH;
                    }
                    y += 6;
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
            int y = -_scrollOffset + 4;
            foreach (var cat in _categories)
            {
                y += HeaderH + 4;
                if (cat.Expanded)
                {
                    foreach (var item in cat.Items)
                    {
                        if (new Rectangle(0, y, Width, ItemH).Contains(pt)) return item.Key;
                        y += ItemH;
                    }
                    y += 6;
                }
            }
            return null;
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            int max = Math.Max(0, TotalContentHeight() - Height + 8);
            _scrollOffset = Math.Clamp(_scrollOffset - e.Delta / 4, 0, max);
            Invalidate();
        }

        private int TotalContentHeight()
        {
            int h = 8;
            foreach (var cat in _categories)
            {
                h += HeaderH + 4;
                if (cat.Expanded) h += cat.Items.Length * ItemH + 6;
            }
            return h;
        }

        // ── Helpers ────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // Catalogue partagé des catégories + items (Sauvegarde / Restauration / Migration)
    // ─────────────────────────────────────────────────────────────────────────
    public static class CheckCatalog
    {
        /// <summary>
        /// Retourne les catégories standard.
        /// • includeBrowserPicker = false  → le nœud "Profil navigateur" n'apparaît pas
        ///   (il est géré séparément par BrowserPickerButton).
        /// • includeOldProfile    = true   → ajoute "Détecter ancien profil" dans Système.
        /// • includeLaunchApps    = true   → ajoute "Lancer les applications" dans Système.
        /// </summary>
        public static CheckCategory[] Build(
            bool includeOldProfile = false,
            bool includeLaunchApps = false)
        {
            // ── Catégorie 1 : Fichiers utilisateur
            var userFiles = new List<CheckItem>
            {
                new("Desktop",       "Bureau",          "🖥️"),
                new("Documents",     "Documents",       "📄"),
                new("Pictures",      "Images",          "🖼️"),
                new("Videos",        "Vidéos",          "🎬"),
                new("Downloads",     "Téléchargements", "⬇️"),
                new("Music",         "Musique",         "🎵"),
                new("Public",        "Dossier Public (%public%)", "📁"),
            };
            if (includeOldProfile)
                userFiles.Add(new("OldProfile", "Détecter ancien profil", "👤", false));

            // ── Catégorie 2 : Bureautique
            var office = new CheckItem[]
            {
                new("Outlook",      "PST Outlook",              "📧"),
                new("Signatures",   "Signatures Outlook",       "✍️"),
                new("OfficeTemplates", "Modèles Office",        "📋"),
                new("OneNote",      "OneNote (registre)",       "📓"),
                new("StickyNotes",  "Sticky Notes",             "📌"),
                new("ExcelMacros",  "Macros Excel (XLSTART)",   "📊"),
            };

            // ── Catégorie 3 : Système & Personnalisation
            var systemItems = new List<CheckItem>
            {
                new("Wallpaper",      "Fond d'écran",         "🖼️"),
                new("NetworkDrives",  "Lecteurs réseau",      "🔗"),
            };
            if (includeLaunchApps)
                systemItems.Add(new("LaunchApps", "Lancer les applications", "🚀"));

            // ── Catégorie 4 : Logiciels métier
            var business = new CheckItem[]
            {
                new("Sap",              "SAP GUI",              "🗂️"),
                new("IpSoftphone",      "IP Desktop Softphone", "📞", false),
            };

            return new[]
            {
                new CheckCategory("Fichiers utilisateur", "📁", userFiles.ToArray()),
                new CheckCategory("Bureautique",          "💼", office),
                new CheckCategory("Système & Personnalisation", "⚙️", systemItems.ToArray()),
                new CheckCategory("Logiciels métier",    "🏢", business),
            };
        }
    }
}
