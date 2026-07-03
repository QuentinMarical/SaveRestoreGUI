using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using Svg;

namespace SaveRestoreGUI.UI
{
    /// &lt;summary&gt;
    /// Fournit les icônes SVG pour chaque élément sauvegardable.
    /// Tous les SVG utilisent des coordonnées directes dans un viewBox 0 0 256 256.
    /// #FILL est remplacé à la volée par la couleur demandée (sauf StickyNotes, couleurs figées).
    /// &lt;/summary&gt;
    [SupportedOSPlatform("windows")]
    public static class SvgIcons
    {
        // ── Ancien profil : dossier + flèche retour ──────────────────────────
        public const string OldProfile =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;path d="M24 80 Q24 68 36 68 L100 68 L116 52 L220 52 Q232 52 232 64 L232 192 Q232 204 220 204 L36 204 Q24 204 24 192 Z"/&gt;
                &lt;path d="M148 108 Q148 94 134 94 L112 94 L120 86 L108 86 L90 104 L108 122 L120 122 L112 114 L134 114 Q138 114 138 118 L138 150 Q138 158 130 158 L100 158 L100 170 L130 170 Q150 170 150 150 Z"
                      fill="white" opacity="0.85"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Bureau : moniteur + pied ──────────────────────────────────────────
        public const string Desktop =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;rect x="20" y="40" width="216" height="144" rx="10" ry="10"/&gt;
                &lt;rect x="32" y="52" width="192" height="120" rx="4" ry="4" fill="white" opacity="0.2"/&gt;
                &lt;rect x="108" y="184" width="40" height="22" rx="2"/&gt;
                &lt;rect x="82" y="204" width="92" height="12" rx="6"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Documents : feuille A4 avec coin plié + lignes ───────────────────
        public const string Documents =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;path d="M52 28 L164 28 L204 68 L204 228 Q204 236 196 236 L60 236 Q52 236 52 228 Z"/&gt;
                &lt;path d="M164 28 L164 68 L204 68 Z" fill="white" opacity="0.3"/&gt;
                &lt;rect x="76" y="100" width="104" height="10" rx="4" fill="white" opacity="0.45"/&gt;
                &lt;rect x="76" y="122" width="88"  height="10" rx="4" fill="white" opacity="0.45"/&gt;
                &lt;rect x="76" y="144" width="96"  height="10" rx="4" fill="white" opacity="0.45"/&gt;
                &lt;rect x="76" y="166" width="80"  height="10" rx="4" fill="white" opacity="0.45"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Macros Excel : fichier + X ────────────────────────────────────────
        public const string ExcelMacros =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;path d="M48 28 L164 28 L208 72 L208 228 Q208 236 200 236 L56 236 Q48 236 48 228 Z"/&gt;
                &lt;path d="M164 28 L164 72 L208 72 Z" fill="white" opacity="0.3"/&gt;
                &lt;path d="M82 108 L102 140 L82 172 L104 172 L120 152 L136 172 L158 172 L138 140 L158 108 L136 108 L120 128 L104 108 Z"
                      fill="white"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Images : cadre photo + paysage ────────────────────────────────────
        public const string Pictures =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;rect x="24" y="48" width="208" height="160" rx="10" ry="10"/&gt;
                &lt;rect x="36" y="60" width="184" height="136" rx="4" ry="4" fill="white" opacity="0.18"/&gt;
                &lt;circle cx="84" cy="96" r="22" fill="white" opacity="0.6"/&gt;
                &lt;polygon points="36,196 96,116 152,172 188,136 220,196" fill="white" opacity="0.5"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Lecteur réseau : racks serveur + câble ───────────────────────────
        public const string NetworkDrives =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;rect x="36" y="36" width="184" height="56" rx="8" ry="8"/&gt;
                &lt;circle cx="196" cy="64" r="9" fill="white" opacity="0.5"/&gt;
                &lt;rect x="52" y="56" width="80" height="16" rx="4" fill="white" opacity="0.25"/&gt;
                &lt;rect x="36" y="108" width="184" height="56" rx="8" ry="8"/&gt;
                &lt;circle cx="196" cy="136" r="9" fill="white" opacity="0.5"/&gt;
                &lt;rect x="52" y="128" width="80" height="16" rx="4" fill="white" opacity="0.25"/&gt;
                &lt;rect x="120" y="164" width="16" height="52" rx="6"/&gt;
                &lt;rect x="92"  y="208" width="72"  height="12" rx="6"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Modèles Office : dossier + lettre W ──────────────────────────────
        public const string OfficeTemplates =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;path d="M24 80 Q24 68 36 68 L100 68 L116 52 L220 52 Q232 52 232 64 L232 192 Q232 204 220 204 L36 204 Q24 204 24 192 Z"/&gt;
                &lt;path d="M68 100 L84 100 L100 152 L116 116 L132 152 L148 100 L164 100 L140 172 L116 172 L100 136 L84 172 L60 172 Z"
                      fill="white"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Musique : note de musique ─────────────────────────────────────────
        public const string Music =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;ellipse cx="96" cy="184" rx="36" ry="24" transform="rotate(-15 96 184)"/&gt;
                &lt;rect x="128" y="52" width="18" height="136" rx="7"/&gt;
                &lt;path d="M146 52 Q196 72 192 120 Q168 104 146 114 Z"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── OneNote : cahier à spirale + N ────────────────────────────────────
        public const string OneNote =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;rect x="52" y="28" width="168" height="200" rx="10" ry="10"/&gt;
                &lt;circle cx="52" cy="64"  r="10" fill="white" opacity="0.65"/&gt;
                &lt;circle cx="52" cy="100" r="10" fill="white" opacity="0.65"/&gt;
                &lt;circle cx="52" cy="136" r="10" fill="white" opacity="0.65"/&gt;
                &lt;circle cx="52" cy="172" r="10" fill="white" opacity="0.65"/&gt;
                &lt;path d="M84 88 L84 168 L102 168 L102 120 L154 168 L172 168 L172 88 L154 88 L154 136 L102 88 Z"
                      fill="white"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Dossier public : dossier + silhouettes ────────────────────────────
        public const string Public =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;path d="M24 80 Q24 68 36 68 L100 68 L116 52 L220 52 Q232 52 232 64 L232 192 Q232 204 220 204 L36 204 Q24 204 24 192 Z"/&gt;
                &lt;circle cx="96" cy="112" r="16" fill="white" opacity="0.75"/&gt;
                &lt;path d="M64 172 Q64 146 96 146 Q128 146 128 172 Z" fill="white" opacity="0.75"/&gt;
                &lt;circle cx="148" cy="112" r="14" fill="white" opacity="0.45"/&gt;
                &lt;path d="M118 172 Q118 148 148 148 Q178 148 178 172 Z" fill="white" opacity="0.45"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Sticky Notes : post-it jaune (couleurs figées, pas de #FILL) ──────
        public const string StickyNotes =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;path d="M20 20 L236 20 L236 196 L196 236 L20 236 Z" fill="#FDD300"/&gt;
              &lt;path d="M196 236 L196 196 L236 196 Z" fill="#EBAB03"/&gt;
              &lt;rect x="44" y="72"  width="140" height="11" rx="4" fill="#c8a600"/&gt;
              &lt;rect x="44" y="100" width="124" height="11" rx="4" fill="#c8a600"/&gt;
              &lt;rect x="44" y="128" width="132" height="11" rx="4" fill="#c8a600"/&gt;
              &lt;rect x="44" y="156" width="108" height="11" rx="4" fill="#c8a600"/&gt;
            &lt;/svg&gt;
            """;

        // ── Téléchargements : dossier + flèche bas ────────────────────────────
        public const string Downloads =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;path d="M24 80 Q24 68 36 68 L100 68 L116 52 L220 52 Q232 52 232 64 L232 192 Q232 204 220 204 L36 204 Q24 204 24 192 Z"/&gt;
                &lt;rect x="120" y="88" width="20" height="60" rx="8" fill="white"/&gt;
                &lt;polygon points="92,148 128,188 164,148" fill="white"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Vidéos : écran + bouton play ──────────────────────────────────────
        public const string Videos =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;rect x="20" y="44" width="216" height="168" rx="10" ry="10"/&gt;
                &lt;rect x="32" y="56" width="192" height="144" rx="4" ry="4" fill="white" opacity="0.15"/&gt;
                &lt;polygon points="96,84 96,172 180,128" fill="white" opacity="0.85"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Fond d'écran : moniteur + paysage ────────────────────────────────
        public const string Wallpaper =
            """
            &lt;svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256"&gt;
              &lt;g fill="#FILL"&gt;
                &lt;rect x="20" y="32" width="216" height="148" rx="10" ry="10"/&gt;
                &lt;rect x="32" y="44" width="192" height="124" rx="4" ry="4" fill="white" opacity="0.18"/&gt;
                &lt;circle cx="76" cy="80" r="20" fill="white" opacity="0.55"/&gt;
                &lt;polygon points="32,168 88,100 144,152 180,116 224,168" fill="white" opacity="0.45"/&gt;
                &lt;rect x="108" y="180" width="40" height="22" rx="2"/&gt;
                &lt;rect x="80"  y="200" width="96"  height="12" rx="6"/&gt;
              &lt;/g&gt;
            &lt;/svg&gt;
            """;

        // ── Mapping clé → SVG string ──────────────────────────────────────────

        private static readonly Dictionary&lt;string, string&gt; _svgMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["OldProfile"]      = OldProfile,
            ["Desktop"]         = Desktop,
            ["Documents"]       = Documents,
            ["ExcelMacros"]     = ExcelMacros,
            ["Pictures"]        = Pictures,
            ["NetworkDrives"]   = NetworkDrives,
            ["OfficeTemplates"] = OfficeTemplates,
            ["Music"]           = Music,
            ["OneNote"]         = OneNote,
            ["Public"]          = Public,
            ["StickyNotes"]     = StickyNotes,
            ["Downloads"]       = Downloads,
            ["Videos"]          = Videos,
            ["Wallpaper"]       = Wallpaper,
        };

        // ── Cache Bitmap (clé + taille + couleur RGB) ─────────────────────────
        private static readonly Dictionary&lt;(string key, int size, int rgb), Bitmap&gt; _cache = new();

        /// &lt;summary&gt;
        /// Retourne un Bitmap rendu du SVG associé à la clé, à la taille indiquée.
        /// Retourne null si la clé n'a pas de SVG associé ou si le rendu échoue.
        /// Le bitmap est mis en cache — ne pas disposer.
        /// &lt;/summary&gt;
        public static Bitmap? Get(string key, int size, Color fillColor)
        {
            int rgb      = (fillColor.R &lt;&lt; 16) | (fillColor.G &lt;&lt; 8) | fillColor.B;
            var cacheKey = (key, size, rgb);
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            if (!_svgMap.TryGetValue(key, out var svgSource))
                return null;

            try
            {
                string hex     = $"#{fillColor.R:X2}{fillColor.G:X2}{fillColor.B:X2}";
                string svgText = svgSource.Replace("#FILL", hex);

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgText));
                var doc = SvgDocument.Open&lt;SvgDocument&gt;(stream);
                doc.Width  = new SvgUnit(SvgUnitType.Pixel, size);
                doc.Height = new SvgUnit(SvgUnitType.Pixel, size);

                var bmp = doc.Draw(size, size);
                _cache[cacheKey] = bmp;
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        /// &lt;summary&gt;Vide le cache (à appeler lors d'un changement de thème).&lt;/summary&gt;
        public static void ClearCache() =&gt; _cache.Clear();

        /// &lt;summary&gt;Retourne true si une icône SVG existe pour cette clé.&lt;/summary&gt;
        public static bool Has(string key) =&gt; _svgMap.ContainsKey(key);
    }
}
