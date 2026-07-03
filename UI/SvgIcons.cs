using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Fournit des <see cref="Bitmap"/> issues des SVG systèmes (dossiers Windows,
    /// applications Office…) rendus via GDI+ pur (aucune dépendance NuGet).
    /// Le cache est indexé par (clé, taille, couleurHex) pour s'adapter au thème.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class SvgIcons
    {
        // ── Cache ─────────────────────────────────────────────────────────────
        private static readonly Dictionary<(string key, int size, string hex), Bitmap> _cache = new();

        /// <summary>Retourne un bitmap mis en cache pour la clé/taille/couleur données.</summary>
        public static Bitmap Get(string key, int size, Color color)
        {
            string hex = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            var cacheKey = (key, size, hex);
            if (!_cache.TryGetValue(cacheKey, out var bmp))
            {
                bmp = Render(key, size, color);
                _cache[cacheKey] = bmp;
            }
            return bmp;
        }

        /// <summary>Vide le cache (ex. changement de thème).</summary>
        public static void ClearCache()
        {
            foreach (var b in _cache.Values) b.Dispose();
            _cache.Clear();
        }

        // ── Rendu GDI+ ────────────────────────────────────────────────────────

        private static Bitmap Render(string key, int size, Color color)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);

            // Chaque icône est définie en coordonnées 256×256, puis on scale.
            float scale = size / 256f;
            using var m = new Matrix(scale, 0, 0, scale, 0, 0);
            g.Transform = m;

            DrawIcon(g, key, color);
            return bmp;
        }

        // ── Dispatching des icônes ─────────────────────────────────────────────

        private static void DrawIcon(Graphics g, string key, Color c)
        {
            switch (key)
            {
                case "OldProfile":      DrawOldProfile(g, c);      break;
                case "Desktop":         DrawDesktop(g, c);         break;
                case "Documents":       DrawDocuments(g, c);       break;
                case "ExcelMacros":     DrawExcelMacros(g, c);     break;
                case "Pictures":        DrawPictures(g, c);        break;
                case "NetworkDrives":   DrawNetworkDrives(g, c);   break;
                case "OfficeTemplates": DrawOfficeTemplates(g, c); break;
                case "Music":           DrawMusic(g, c);           break;
                case "OneNote":         DrawOneNote(g, c);         break;
                case "Public":          DrawPublic(g, c);          break;
                case "StickyNotes":     DrawStickyNotes(g);        break;  // couleurs figées
                case "Downloads":       DrawDownloads(g, c);       break;
                case "Videos":          DrawVideos(g, c);          break;
                case "Wallpaper":       DrawWallpaper(g, c);       break;
                default:                DrawGenericFolder(g, c);   break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Brush B(Color c) => new SolidBrush(c);
        private static Pen   P(Color c, float w = 1f) => new Pen(c, w);

        // Applique un SVG path GDI+ depuis les points fournis (coordonnées 256×256).
        // Tous les paths ci-dessous sont issus des SVG transmis par l'utilisateur.

        // ── Icônes ────────────────────────────────────────────────────────────

        // Ancien profil — dossier + flèche retour
        private static void DrawOldProfile(Graphics g, Color c)
        {
            using var brush = B(c);
            // Corps du dossier
            var folder = new RectangleF(30, 100, 196, 126);
            g.FillRoundedRect(brush, folder, 10);
            // Onglet
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // Flèche courbe (sens antihoraire) symbolisant "ancien profil"
            using var pen = P(InvertContrast(c), 8f);
            pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            g.DrawArc(pen, new RectangleF(90, 108, 76, 60), 200, 280);
        }

        // Bureau — écran + pied
        private static void DrawDesktop(Graphics g, Color c)
        {
            using var brush = B(c);
            // Écran
            g.FillRoundedRect(brush, new RectangleF(28, 48, 200, 130), 8);
            // Découpe intérieure (zone noire)
            using var innerBrush = B(InvertContrast(c));
            g.FillRoundedRect(innerBrush, new RectangleF(38, 58, 180, 106), 4);
            // Pied
            g.FillRectangle(brush, new RectangleF(108, 178, 40, 18));
            // Base
            g.FillRoundedRect(brush, new RectangleF(72, 194, 112, 14), 4);
        }

        // Documents — dossier + coin plié
        private static void DrawDocuments(Graphics g, Color c)
        {
            using var brush = B(c);
            // Corps dossier
            g.FillRoundedRect(brush, new RectangleF(30, 102, 196, 122), 10);
            // Onglet
            g.FillRoundedRect(brush, new RectangleF(30, 86, 90, 22), 6);
            // Feuille blanche
            using var wb = B(InvertContrast(c));
            var pts = new PointF[] {
                new(78,118), new(78,202), new(178,202), new(178,142), new(154,118)
            };
            g.FillPolygon(wb, pts);
            // Coin plié
            using var cb = B(Color.FromArgb(80, c));
            g.FillPolygon(cb, new PointF[] { new(154,118), new(178,142), new(154,142) });
        }

        // Macros Excel — dossier + X vert
        private static void DrawExcelMacros(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // X façon Excel
            using var xPen = P(Color.FromArgb(40, 167, 69), 14f);
            xPen.StartCap = LineCap.Round;
            xPen.EndCap   = LineCap.Round;
            g.DrawLine(xPen, 88f, 120f, 136f, 190f);
            g.DrawLine(xPen, 136f, 120f, 88f, 190f);
            // badge XL fond vert
            using var gbr = B(Color.FromArgb(40, 167, 69));
            g.FillRoundedRect(gbr, new RectangleF(148, 118, 62, 72), 6);
            using var wb = B(Color.White);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("XL", font, wb, new RectangleF(148, 118, 62, 72), sf);
        }

        // Images — dossier + paysage
        private static void DrawPictures(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // Soleil
            using var yb = B(Color.FromArgb(255, 200, 30));
            g.FillEllipse(yb, 50f, 116f, 32f, 32f);
            // Montagne
            using var mb = B(InvertContrast(c));
            g.FillPolygon(mb, new PointF[] { new(60,200), new(130,134), new(200,200) });
            using var mb2 = B(Color.FromArgb(120, c));
            g.FillPolygon(mb2, new PointF[] { new(130,134), new(175,168), new(200,200), new(130,200) });
        }

        // Lecteurs réseau — boîtier réseau + disque
        private static void DrawNetworkDrives(Graphics g, Color c)
        {
            using var brush = B(c);
            // Boîtier
            g.FillRoundedRect(brush, new RectangleF(30, 70, 196, 90), 10);
            // Voyant
            using var gb = B(Color.FromArgb(40, 200, 40));
            g.FillEllipse(gb, 190f, 100f, 18f, 18f);
            // Disque
            using var db = B(Color.FromArgb(160, c.R, c.G, c.B));
            g.FillRoundedRect(db, new RectangleF(30, 172, 196, 52), 8);
            // Connecteurs réseau (lignes)
            using var lp = P(InvertContrast(c), 5f);
            g.DrawLine(lp, 128f, 160f, 128f, 172f);
            g.DrawLine(lp, 80f, 160f, 80f, 148f);
            g.DrawLine(lp, 176f, 160f, 176f, 148f);
            g.DrawLine(lp, 80f, 148f, 176f, 148f);
        }

        // Modèles Office — document + W/P/X
        private static void DrawOfficeTemplates(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            using var wb = B(Color.White);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            // Badge bleu Word
            using var wbr = B(Color.FromArgb(40, 90, 180));
            g.FillRoundedRect(wbr, new RectangleF(52, 118, 44, 48), 5);
            using var font = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("W", font, wb, new RectangleF(52, 118, 44, 48), sf);
            // Badge vert Excel
            using var xbr = B(Color.FromArgb(33, 115, 70));
            g.FillRoundedRect(xbr, new RectangleF(106, 118, 44, 48), 5);
            g.DrawString("X", font, wb, new RectangleF(106, 118, 44, 48), sf);
            // Badge orange PowerPoint
            using var pbr = B(Color.FromArgb(210, 70, 20));
            g.FillRoundedRect(pbr, new RectangleF(160, 118, 44, 48), 5);
            g.DrawString("P", font, wb, new RectangleF(160, 118, 44, 48), sf);
        }

        // Musique — dossier + note
        private static void DrawMusic(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // Note de musique
            using var np = P(InvertContrast(c), 7f);
            np.StartCap = LineCap.Round;
            np.EndCap   = LineCap.Round;
            // Queue verticale
            g.DrawLine(np, 140f, 130f, 140f, 186f);
            // Barre horizontale
            g.DrawLine(np, 140f, 130f, 180f, 120f);
            // Queue 2
            g.DrawLine(np, 180f, 120f, 180f, 176f);
            // Têtes de note
            using var nb = B(InvertContrast(c));
            g.FillEllipse(nb, 120f, 180f, 26f, 18f);
            g.FillEllipse(nb, 160f, 170f, 26f, 18f);
        }

        // OneNote — dossier + N violet
        private static void DrawOneNote(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            using var nbr = B(Color.FromArgb(122, 59, 172));
            g.FillRoundedRect(nbr, new RectangleF(76, 116, 104, 80), 7);
            using var wb  = B(Color.White);
            using var sf  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 38f, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("N", font, wb, new RectangleF(76, 116, 104, 80), sf);
        }

        // Dossier Public — dossier + silhouettes
        private static void DrawPublic(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // Silhouette principale
            using var pb = B(InvertContrast(c));
            g.FillEllipse(pb, 98f, 118f, 36f, 36f);  // tête
            g.FillRoundedRect(pb, new RectangleF(88f, 154f, 56f, 44f), 10); // corps
            // Silhouette secondaire (petite, à droite)
            using var pb2 = B(Color.FromArgb(140, InvertContrast(c).R, InvertContrast(c).G, InvertContrast(c).B));
            g.FillEllipse(pb2, 148f, 124f, 26f, 26f);
            g.FillRoundedRect(pb2, new RectangleF(140f, 150f, 42f, 36f), 8);
        }

        // Sticky Notes — post-it jaune + coin plié gris
        private static void DrawStickyNotes(Graphics g)
        {
            // Corps jaune
            using var yb = B(Color.FromArgb(253, 211, 0));
            g.FillRoundedRect(yb, new RectangleF(30, 30, 196, 196), 8);
            // Coin plié (bas-droite) en jaune foncé
            using var yd = B(Color.FromArgb(235, 171, 3));
            g.FillPolygon(yd, new PointF[] { new(164,226), new(226,164), new(226,226) });
            // Séparateur diagonal gris foncé
            using var gp = P(Color.FromArgb(64, 64, 64), 3f);
            g.DrawLine(gp, 164f, 226f, 226f, 164f);
            // Lignes de texte simulées
            using var lp = P(Color.FromArgb(100, 60, 40, 0), 5f);
            lp.StartCap = LineCap.Round;
            lp.EndCap   = LineCap.Round;
            g.DrawLine(lp, 55f, 80f, 190f, 80f);
            g.DrawLine(lp, 55f, 108f, 170f, 108f);
            g.DrawLine(lp, 55f, 136f, 180f, 136f);
            g.DrawLine(lp, 55f, 164f, 155f, 164f);
        }

        // Téléchargements — dossier + flèche bas
        private static void DrawDownloads(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // Flèche vers le bas
            using var ap = P(InvertContrast(c), 10f);
            ap.StartCap = LineCap.Round;
            ap.EndCap   = LineCap.ArrowAnchor;
            g.DrawLine(ap, 128f, 120f, 128f, 188f);
            // Barre horizontale
            using var bp = P(InvertContrast(c), 10f);
            bp.StartCap = LineCap.Round;
            bp.EndCap   = LineCap.Round;
            g.DrawLine(bp, 94f, 196f, 162f, 196f);
        }

        // Vidéos — dossier + pellicule
        private static void DrawVideos(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
            // Triangle "play"
            using var pb = B(InvertContrast(c));
            g.FillPolygon(pb, new PointF[] { new(100,122), new(100,196), new(178,159) });
            // Bandes pellicule (gauche et droite)
            using var fp = B(Color.FromArgb(80, InvertContrast(c).R, InvertContrast(c).G, InvertContrast(c).B));
            g.FillRoundedRect(fp, new RectangleF(34, 108, 18, 106), 3);
            g.FillRoundedRect(fp, new RectangleF(204, 108, 18, 106), 3);
        }

        // Fond d'écran — moniteur + image
        private static void DrawWallpaper(Graphics g, Color c)
        {
            using var brush = B(c);
            // Cadre moniteur
            g.FillRoundedRect(brush, new RectangleF(28, 40, 200, 146), 10);
            // Ciel gradient (bleu → blanc)
            using var skyB = B(Color.FromArgb(100, 149, 237));
            g.FillRoundedRect(skyB, new RectangleF(40, 52, 176, 80), 4);
            // Soleil
            using var sunB = B(Color.FromArgb(255, 220, 50));
            g.FillEllipse(sunB, 48f, 58f, 30f, 30f);
            // Collines
            using var hillB = B(Color.FromArgb(80, 140, 60));
            g.FillEllipse(hillB, 30f, 108f, 100f, 60f);
            using var hillB2 = B(Color.FromArgb(60, 120, 50));
            g.FillEllipse(hillB2, 110f, 112f, 120f, 60f);
            // Pied + base
            g.FillRectangle(brush, new RectangleF(118, 186, 20, 18));
            g.FillRoundedRect(brush, new RectangleF(78, 202, 100, 12), 4);
        }

        // Dossier générique (fallback)
        private static void DrawGenericFolder(Graphics g, Color c)
        {
            using var brush = B(c);
            g.FillRoundedRect(brush, new RectangleF(30, 100, 196, 126), 10);
            g.FillRoundedRect(brush, new RectangleF(30, 84, 80, 22), 6);
        }

        // ── Utilitaires couleur ───────────────────────────────────────────────

        /// <summary>Renvoie blanc ou noir en fonction de la luminosité de c.</summary>
        private static Color InvertContrast(Color c)
        {
            double lum = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
            return lum > 160 ? Color.FromArgb(40, 40, 40) : Color.White;
        }
    }

    // ── Extension GDI+ ────────────────────────────────────────────────────────

    [SupportedOSPlatform("windows")]
    internal static class GraphicsExtensions
    {
        public static void FillRoundedRect(this Graphics g, Brush brush, RectangleF rect, float radius)
        {
            using var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
