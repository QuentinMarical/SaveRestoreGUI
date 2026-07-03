using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Fournit des <see cref="Bitmap"/> extraites directement depuis
    /// <c>imageres.dll</c> / <c>imageres.dll.mun</c> (icônes Windows officielles)
    /// via <c>ExtractIconEx</c>.  Chaque clé est mappée sur l'index d'icône
    /// correspondant dans la DLL.  Un fallback GDI+ est assuré si l'extraction
    /// échoue (DLL absente, Wine, etc.).
    ///
    /// Cache indexé par (clé, taille, colorHex) pour s'adapter aux changements de thème.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class SvgIcons
    {
        // ── P/Invoke ──────────────────────────────────────────────────────────

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(
            string lpszFile, int nIconIndex,
            IntPtr[] phiconLarge, IntPtr[] phiconSmall,
            int nIcons);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // ── Mapping clé → index imageres.dll ──────────────────────────────────
        // Sources : https://renenyffenegger.ch/development/Windows/PowerShell/examples/WinAPI/ExtractIconEx/imageres/index
        // (indices valides sous Windows 10/11 ; index 0-based).
        private static readonly Dictionary<string, int> _imageres = new()
        {
            // Dossiers / emplacements utilisateur
            { "OldProfile",      3  },   // Dossier utilisateur générique
            { "Desktop",         183 },  // Bureau
            { "Documents",       2   },  // Documents
            { "Pictures",        108 },  // Images
            { "Music",           103 },  // Musique
            { "Videos",          116 },  // Vidéos
            { "Downloads",       184 },  // Téléchargements
            { "Public",          117 },  // Dossier public (utilisateurs partagés)
            // Réseau / système
            { "NetworkDrives",   28  },  // Lecteur réseau
            { "Wallpaper",       197 },  // Personnalisation / fond d'écran
            // Applications Office (fallback dossier si .mun absent)
            { "Outlook",         2   },  // Pas dans imageres → fallback Documents
            { "Signatures",      2   },  // idem
            { "OfficeTemplates", 2   },  // idem
            { "OneNote",         2   },  // idem
            { "ExcelMacros",     2   },  // idem
            { "StickyNotes",     256 },  // Note adhésive (Win 11) – fallback si absent
            { "LaunchApps",      15  },  // Applications
        };

        // Chemin imageres.dll (System32 garanti sur Windows)
        private static readonly string ImageresDll =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                         "imageres.dll");

        // ── Cache ─────────────────────────────────────────────────────────────
        private static readonly Dictionary<(string key, int size, string hex), Bitmap> _cache = new();

        public static Bitmap Get(string key, int size, Color color)
        {
            string hex      = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            var    cacheKey = (key, size, hex);
            if (!_cache.TryGetValue(cacheKey, out var bmp))
            {
                bmp = RenderIcon(key, size, color);
                _cache[cacheKey] = bmp;
            }
            return bmp;
        }

        public static void ClearCache()
        {
            foreach (var b in _cache.Values) b.Dispose();
            _cache.Clear();
        }

        // ── Rendu ─────────────────────────────────────────────────────────────

        private static Bitmap RenderIcon(string key, int size, Color tintColor)
        {
            // 1. Tentative extraction imageres.dll
            if (File.Exists(ImageresDll) && _imageres.TryGetValue(key, out int idx))
            {
                var bmp = ExtractFromImageres(idx, size);
                if (bmp != null)
                {
                    // Colorier en mode StickyNotes → conserver couleurs natives
                    if (key == "StickyNotes") return bmp;
                    return TintBitmap(bmp, tintColor);
                }
            }

            // 2. Fallback GDI+ (formes simples)
            return FallbackGdi(key, size, tintColor);
        }

        /// <summary>Extrait une icône depuis imageres.dll et la rend en Bitmap.</summary>
        private static Bitmap? ExtractFromImageres(int index, int size)
        {
            // On utilise les grandes icônes (phiconLarge) qui sont 32×32+
            var large = new IntPtr[1];
            var small = new IntPtr[1];
            try
            {
                int extracted = ExtractIconEx(ImageresDll, index, large, small, 1);
                if (extracted <= 0 || large[0] == IntPtr.Zero) return null;

                // Convertir HICON → Bitmap
                using var ico = Icon.FromHandle(large[0]);
                var src = ico.ToBitmap();

                // Redimensionner si nécessaire
                if (src.Width == size && src.Height == size)
                    return (Bitmap)src.Clone();

                var dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(dst);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.DrawImage(src, 0, 0, size, size);
                src.Dispose();
                return dst;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (large[0] != IntPtr.Zero) DestroyIcon(large[0]);
                if (small[0] != IntPtr.Zero) DestroyIcon(small[0]);
            }
        }

        /// <summary>
        /// Applique une teinte : remplace la luminosité de chaque pixel par
        /// la couleur de thème, en préservant la transparence d'origine.
        /// </summary>
        private static Bitmap TintBitmap(Bitmap src, Color tint)
        {
            int w = src.Width, h = src.Height;
            var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            for (int py = 0; py < h; py++)
                for (int px = 0; px < w; px++)
                {
                    var pix = src.GetPixel(px, py);
                    if (pix.A < 4) { dst.SetPixel(px, py, Color.Transparent); continue; }
                    // Luminosité du pixel source → module l'intensité de la teinte
                    float lum = (0.299f * pix.R + 0.587f * pix.G + 0.114f * pix.B) / 255f;
                    int r = Clamp((int)(tint.R * lum));
                    int g = Clamp((int)(tint.G * lum));
                    int b = Clamp((int)(tint.B * lum));
                    dst.SetPixel(px, py, Color.FromArgb(pix.A, r, g, b));
                }
            src.Dispose();
            return dst;
        }

        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;

        // ── Fallback GDI+ (si imageres.dll inaccessible) ──────────────────────

        private static Bitmap FallbackGdi(string key, int size, Color c)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);

            float scale = size / 256f;
            g.Transform = new Matrix(scale, 0, 0, scale, 0, 0);

            switch (key)
            {
                case "StickyNotes":     FbStickyNotes(g);     break;
                case "Desktop":         FbDesktop(g, c);      break;
                case "Pictures":        FbPictures(g, c);     break;
                case "Music":           FbMusic(g, c);        break;
                case "Videos":          FbVideos(g, c);       break;
                case "Downloads":       FbDownloads(g, c);    break;
                case "NetworkDrives":   FbNetwork(g, c);      break;
                default:                FbFolder(g, c);       break;
            }
            return bmp;
        }

        // Fallbacks minimalistes (formes géométriques)
        private static void FbFolder(Graphics g, Color c)
        {
            using var b = new SolidBrush(c);
            FillRR(g, b, new RectangleF(30, 100, 196, 126), 10);
            FillRR(g, b, new RectangleF(30, 84,  80,  22),  6);
        }
        private static void FbDesktop(Graphics g, Color c)
        {
            using var b = new SolidBrush(c);
            FillRR(g, b, new RectangleF(28, 48, 200, 130), 8);
            using var ib = new SolidBrush(ContrastColor(c));
            FillRR(g, ib, new RectangleF(38, 58, 180, 106), 4);
            g.FillRectangle(b, new RectangleF(108, 178, 40, 18));
            FillRR(g, b, new RectangleF(72, 194, 112, 14), 4);
        }
        private static void FbPictures(Graphics g, Color c)
        {
            FbFolder(g, c);
            using var y = new SolidBrush(Color.FromArgb(255, 200, 30));
            g.FillEllipse(y, 50f, 116f, 32f, 32f);
            using var m = new SolidBrush(ContrastColor(c));
            g.FillPolygon(m, new PointF[] { new(60,200), new(130,134), new(200,200) });
        }
        private static void FbMusic(Graphics g, Color c)
        {
            FbFolder(g, c);
            using var p = new Pen(ContrastColor(c), 7f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(p, 140f, 130f, 140f, 186f);
            g.DrawLine(p, 140f, 130f, 180f, 120f);
            g.DrawLine(p, 180f, 120f, 180f, 176f);
            using var nb = new SolidBrush(ContrastColor(c));
            g.FillEllipse(nb, 120f, 180f, 26f, 18f);
            g.FillEllipse(nb, 160f, 170f, 26f, 18f);
        }
        private static void FbVideos(Graphics g, Color c)
        {
            FbFolder(g, c);
            using var pb = new SolidBrush(ContrastColor(c));
            g.FillPolygon(pb, new PointF[] { new(100,122), new(100,196), new(178,159) });
        }
        private static void FbDownloads(Graphics g, Color c)
        {
            FbFolder(g, c);
            using var ap = new Pen(ContrastColor(c), 10f)
                { StartCap = LineCap.Round, EndCap = LineCap.ArrowAnchor };
            g.DrawLine(ap, 128f, 120f, 128f, 188f);
            using var bp = new Pen(ContrastColor(c), 10f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(bp, 94f, 196f, 162f, 196f);
        }
        private static void FbNetwork(Graphics g, Color c)
        {
            using var b = new SolidBrush(c);
            FillRR(g, b, new RectangleF(30, 70, 196, 90), 10);
            using var gb = new SolidBrush(Color.FromArgb(40, 200, 40));
            g.FillEllipse(gb, 190f, 100f, 18f, 18f);
            FillRR(g, new SolidBrush(Color.FromArgb(160, c.R, c.G, c.B)),
                   new RectangleF(30, 172, 196, 52), 8);
        }
        private static void FbStickyNotes(Graphics g)
        {
            using var yb = new SolidBrush(Color.FromArgb(253, 211, 0));
            FillRR(g, yb, new RectangleF(30, 30, 196, 196), 8);
            using var yd = new SolidBrush(Color.FromArgb(235, 171, 3));
            g.FillPolygon(yd, new PointF[] { new(164,226), new(226,164), new(226,226) });
            using var lp = new Pen(Color.FromArgb(100, 60, 40, 0), 5f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(lp, 55f,  80f, 190f,  80f);
            g.DrawLine(lp, 55f, 108f, 170f, 108f);
            g.DrawLine(lp, 55f, 136f, 180f, 136f);
            g.DrawLine(lp, 55f, 164f, 155f, 164f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void FillRR(Graphics g, Brush b, RectangleF r, float radius)
        {
            float d = radius * 2f;
            using var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(b, path);
        }

        private static Color ContrastColor(Color c)
        {
            double lum = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            return lum > 160 ? Color.FromArgb(40, 40, 40) : Color.White;
        }
    }
}
