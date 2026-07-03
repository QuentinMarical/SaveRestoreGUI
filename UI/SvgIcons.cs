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
    /// <c>imageres.dll</c> (icônes Windows officielles) via <c>ExtractIconEx</c>.
    /// Les couleurs natives de l'OS sont conservées intactes — aucune teinte
    /// n'est appliquée sur les icônes extraites.
    /// Le fallback GDI+ utilise des couleurs fixes propres à chaque app.
    ///
    /// Cache indexé par (clé, taille) ; la couleur n'entre plus dans la clé
    /// car les icônes natives sont indépendantes du thème.
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
            { "OldProfile",      3   },  // Dossier utilisateur générique
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
            // Applications Office — pas d'entrée dans imageres → fallback GDI+
            // (indices ci-dessous intentionnellement absents pour forcer le fallback)
        };

        // Chemin imageres.dll (System32 garanti sur Windows)
        private static readonly string ImageresDll =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                         "imageres.dll");

        // ── Cache ─────────────────────────────────────────────────────────────
        // La clé n'inclut PAS la couleur : les icônes natives sont indépendantes
        // du thème. Pour les fallbacks GDI+, la couleur est fixe par app.
        private static readonly Dictionary<(string key, int size), Bitmap> _cache = new();

        /// <summary>
        /// Retourne l'icône pour la clé donnée.
        /// <paramref name="fallbackColor"/> n'est utilisé QUE si imageres.dll
        /// ne fournit pas d'icône native (cas fallback GDI+).
        /// </summary>
        public static Bitmap Get(string key, int size, Color fallbackColor)
        {
            var cacheKey = (key, size);
            if (!_cache.TryGetValue(cacheKey, out var bmp))
            {
                bmp = RenderIcon(key, size, fallbackColor);
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

        private static Bitmap RenderIcon(string key, int size, Color fallbackColor)
        {
            // 1. Tentative extraction imageres.dll (couleurs natives OS conservées)
            if (File.Exists(ImageresDll) && _imageres.TryGetValue(key, out int idx))
            {
                var bmp = ExtractFromImageres(idx, size);
                if (bmp != null) return bmp; // couleurs natives, aucune modification
            }

            // 2. Fallback GDI+ — couleurs fixes propres à chaque app
            return FallbackGdi(key, size, fallbackColor);
        }

        /// <summary>Extrait une icône depuis imageres.dll et la rend en Bitmap.</summary>
        private static Bitmap? ExtractFromImageres(int index, int size)
        {
            var large = new IntPtr[1];
            var small = new IntPtr[1];
            try
            {
                int extracted = ExtractIconEx(ImageresDll, index, large, small, 1);
                if (extracted <= 0 || large[0] == IntPtr.Zero) return null;

                using var ico = Icon.FromHandle(large[0]);
                var src = ico.ToBitmap();

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

        // ── Fallback GDI+ ─────────────────────────────────────────────────────
        // Chaque app a ses couleurs propres et reconnaissables.
        // Le paramètre «c» (fallbackColor) est réservé aux dossiers génériques
        // et aux apps dont la couleur suit le thème.

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
                // ── Apps couleur fixe ─────────────────────────────────────────
                case "StickyNotes":     FbStickyNotes(g);     break;
                case "Outlook":         FbOutlook(g);         break;
                case "OneNote":         FbOneNote(g);         break;
                case "ExcelMacros":     FbExcel(g);           break;
                case "OfficeTemplates": FbOffice(g);          break;
                case "Signatures":      FbSignatures(g);      break;

                // ── Dossiers / couleur thème ──────────────────────────────────
                case "Desktop":         FbDesktop(g, c);      break;
                case "Pictures":        FbPictures(g, c);     break;
                case "Music":           FbMusic(g, c);        break;
                case "Videos":          FbVideos(g, c);       break;
                case "Downloads":       FbDownloads(g, c);    break;
                case "NetworkDrives":   FbNetwork(g, c);      break;
                case "Wallpaper":       FbWallpaper(g, c);    break;
                default:                FbFolder(g, c);       break;
            }
            return bmp;
        }

        // ─── Apps couleur fixe ─────────────────────────────────────────────────

        /// <summary>Sticky Notes — jaune Office (#FDD300)</summary>
        private static void FbStickyNotes(Graphics g)
        {
            // Corps de la note
            var yellow     = Color.FromArgb(253, 211,   0);
            var yellowDark = Color.FromArgb(235, 171,   3);
            var lineColor  = Color.FromArgb(120,  60,  40,  0);

            using var yb = new SolidBrush(yellow);
            FillRR(g, yb, new RectangleF(28, 28, 200, 200), 10);

            // Coin replié (coin bas-droit)
            using var yd = new SolidBrush(yellowDark);
            g.FillPolygon(yd, new PointF[] { new(162, 228), new(228, 162), new(228, 228) });

            // Lignes de texte
            using var lp = new Pen(lineColor, 5f)
                { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(lp,  54f,  82f, 188f,  82f);
            g.DrawLine(lp,  54f, 110f, 172f, 110f);
            g.DrawLine(lp,  54f, 138f, 180f, 138f);
            g.DrawLine(lp,  54f, 166f, 156f, 166f);
        }

        /// <summary>Outlook — bleu Microsoft (#0078D4)</summary>
        private static void FbOutlook(Graphics g)
        {
            var blue   = Color.FromArgb(  0, 120, 212);
            var blueDk = Color.FromArgb(  0,  88, 168);
            var white  = Color.White;

            // Enveloppe
            using var bb = new SolidBrush(blue);
            FillRR(g, bb, new RectangleF(28, 68, 200, 148), 10);

            // Rabat haut de l'enveloppe
            using var rb = new SolidBrush(blueDk);
            g.FillPolygon(rb, new PointF[] { new(28, 68), new(128, 148), new(228, 68) });

            // Lettre blanche
            using var wp = new Pen(white, 3f);
            g.DrawLine(wp, 28f, 68f, 128f, 148f);
            g.DrawLine(wp, 128f, 148f, 228f, 68f);
        }

        /// <summary>OneNote — violet Microsoft (#7719AA)</summary>
        private static void FbOneNote(Graphics g)
        {
            var purple = Color.FromArgb(119, 25, 170);
            var white  = Color.White;

            using var pb = new SolidBrush(purple);
            FillRR(g, pb, new RectangleF(28, 28, 200, 200), 12);

            // Lettre «N» blanche
            using var wp = new Pen(white, 18f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(wp,  76f,  68f,  76f, 188f);
            g.DrawLine(wp,  76f,  68f, 180f, 188f);
            g.DrawLine(wp, 180f,  68f, 180f, 188f);
        }

        /// <summary>Excel — vert Microsoft (#217346)</summary>
        private static void FbExcel(Graphics g)
        {
            var green  = Color.FromArgb( 33, 115,  70);
            var greenL = Color.FromArgb( 40, 150,  90);
            var white  = Color.White;

            using var gb = new SolidBrush(green);
            FillRR(g, gb, new RectangleF(28, 28, 200, 200), 12);

            // Croix «X» blanche
            using var xp = new Pen(white, 22f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(xp,  76f,  76f, 180f, 180f);
            g.DrawLine(xp, 180f,  76f,  76f, 180f);
        }

        /// <summary>Modèles Office — orange Microsoft (#D83B01)</summary>
        private static void FbOffice(Graphics g)
        {
            var orange = Color.FromArgb(216, 59,   1);
            var white  = Color.White;

            using var ob = new SolidBrush(orange);
            FillRR(g, ob, new RectangleF(28, 28, 200, 200), 12);

            // Lettre «W» stylisée
            using var wp = new Pen(white, 14f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(wp,  60f, 72f,  88f, 184f);
            g.DrawLine(wp,  88f, 184f, 128f, 110f);
            g.DrawLine(wp, 128f, 110f, 168f, 184f);
            g.DrawLine(wp, 168f, 184f, 196f,  72f);
        }

        /// <summary>Signatures — bleu-gris neutre</summary>
        private static void FbSignatures(Graphics g)
        {
            var slate = Color.FromArgb( 70, 100, 140);
            var white = Color.White;

            using var sb = new SolidBrush(slate);
            FillRR(g, sb, new RectangleF(28, 28, 200, 200), 12);

            // Stylo / plume
            using var pp = new Pen(white, 10f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pp,  68f, 188f, 188f,  68f);
            g.DrawLine(pp, 188f,  68f, 168f,  88f);
            g.DrawLine(pp,  68f, 188f,  56f, 200f);
        }

        // ─── Dossiers (couleur thème) ──────────────────────────────────────────

        private static void FbFolder(Graphics g, Color c)
        {
            using var b = new SolidBrush(c);
            FillRR(g, b, new RectangleF(30, 100, 196, 126), 10);
            FillRR(g, b, new RectangleF(30,  84,  80,  22),  6);
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
            g.FillPolygon(m, new PointF[] { new(60, 200), new(130, 134), new(200, 200) });
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
            g.FillPolygon(pb, new PointF[] { new(100, 122), new(100, 196), new(178, 159) });
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

        private static void FbWallpaper(Graphics g, Color c)
        {
            // Écran avec image paysage
            using var b = new SolidBrush(c);
            FillRR(g, b, new RectangleF(28, 48, 200, 130), 8);
            // Ciel et sol
            using var sky = new SolidBrush(Color.FromArgb(100, 180, 255));
            FillRR(g, sky, new RectangleF(38, 58, 180, 70), 4);
            using var gnd = new SolidBrush(Color.FromArgb(60, 160, 60));
            FillRR(g, gnd, new RectangleF(38, 128, 180, 46), 4);
            // Pied
            g.FillRectangle(b, new RectangleF(108, 178, 40, 18));
            FillRR(g, b, new RectangleF(72, 194, 112, 14), 4);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void FillRR(Graphics g, Brush b, RectangleF r, float radius)
        {
            float d = radius * 2f;
            using var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180,  90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270,  90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0,  90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90,  90);
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
