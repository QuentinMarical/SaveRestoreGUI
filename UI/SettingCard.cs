using System.ComponentModel;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Carte-ligne façon app Paramètres de Windows 11 : icône emoji à gauche,
    /// titre + description empilés, contrôles hébergés alignés à droite par le layout.
    /// En mode <see cref="HeaderMode"/>, l'icône et le titre occupent seulement la
    /// zone haute (<see cref="HeaderH"/>) et le reste de la carte accueille du
    /// contenu libre (ex. CategoryCheckPanel) — équivalent d'un expander déplié.
    /// </summary>
    public class SettingCard : CardPanel
    {
        /// <summary>Marge horizontale interne commune (icône, texte, contrôles).</summary>
        public static readonly int PadX = Dpi.S(16);

        /// <summary>Hauteur de la zone icône+titre en mode Header.</summary>
        public static readonly int HeaderH = Dpi.S(52);

        /// <summary>Hauteur standard d'une carte-ligne simple.</summary>
        public static readonly int RowH = Dpi.S(68);

        private static readonly int IconZoneW = Dpi.S(40);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string IconGlyph { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Description { get; set; } = "";

        /// <summary>true : icône/titre dessinés dans la zone haute uniquement.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HeaderMode { get; set; }

        public SettingCard()
        {
            CornerRadius = 8;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var p = ThemeManager.Palette;
            var g = e.Graphics;

            int zoneH = HeaderMode ? HeaderH : Height;
            int textX = PadX;

            if (!string.IsNullOrEmpty(IconGlyph))
            {
                using var iconFont = Dpi.Font("Segoe UI Emoji", 12f);
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                using var iconBrush = new SolidBrush(p.Text);
                g.DrawString(IconGlyph, iconFont, iconBrush,
                    new RectangleF(PadX, 0, Dpi.S(30), zoneH), sf);
                textX = PadX + IconZoneW;
            }

            // Le texte s'arrête avant le premier contrôle hébergé dans la zone titre
            // (contrôles de droite d'une carte-ligne, boutons d'en-tête en HeaderMode).
            int rightLimit = Width - PadX;
            foreach (Control c in Controls)
                if (c.Visible && c.Top < zoneH - Dpi.S(4))
                    rightLimit = Math.Min(rightLimit, c.Left - Dpi.S(12));
            int availW = Math.Max(Dpi.S(40), rightLimit - textX);

            using var titleFont = Dpi.Font("Segoe UI", 10f);
            const TextFormatFlags flags =
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            if (!string.IsNullOrEmpty(Description))
            {
                int titleH = Dpi.S(20), descH = Dpi.S(17);
                int blockY = (zoneH - titleH - descH) / 2;
                TextRenderer.DrawText(g, Title, titleFont,
                    new Rectangle(textX, blockY, availW, titleH), p.Text, flags);

                using var descFont = Dpi.Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(g, Description, descFont,
                    new Rectangle(textX, blockY + titleH, availW, descH), p.TextSecondary, flags);
            }
            else
            {
                TextRenderer.DrawText(g, Title, titleFont,
                    new Rectangle(textX, 0, availW, zoneH), p.Text,
                    flags | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
