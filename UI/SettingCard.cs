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
        public const int PadX = 16;

        /// <summary>Hauteur de la zone icône+titre en mode Header.</summary>
        public const int HeaderH = 52;

        /// <summary>Hauteur standard d'une carte-ligne simple.</summary>
        public const int RowH = 68;

        private const int IconZoneW = 40;

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
                using var iconFont = new Font("Segoe UI Emoji", 12f);
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                using var iconBrush = new SolidBrush(p.Text);
                g.DrawString(IconGlyph, iconFont, iconBrush,
                    new RectangleF(PadX, 0, 30, zoneH), sf);
                textX = PadX + IconZoneW;
            }

            // Le texte s'arrête avant le premier contrôle hébergé dans la zone titre
            // (contrôles de droite d'une carte-ligne, boutons d'en-tête en HeaderMode).
            int rightLimit = Width - PadX;
            foreach (Control c in Controls)
                if (c.Visible && c.Top < zoneH - 4)
                    rightLimit = Math.Min(rightLimit, c.Left - 12);
            int availW = Math.Max(40, rightLimit - textX);

            using var titleFont = new Font("Segoe UI", 10f);
            const TextFormatFlags flags =
                TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;

            if (!string.IsNullOrEmpty(Description))
            {
                const int titleH = 20, descH = 17;
                int blockY = (zoneH - titleH - descH) / 2;
                TextRenderer.DrawText(g, Title, titleFont,
                    new Rectangle(textX, blockY, availW, titleH), p.Text, flags);

                using var descFont = new Font("Segoe UI", 8.5f);
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
