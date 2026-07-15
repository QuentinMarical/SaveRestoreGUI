using System.Runtime.InteropServices;

namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Facteur d'échelle DPI global, calculé une fois au démarrage.
    ///
    /// L'app est DPI SystemAware : les polices (en points) sont déjà agrandies par
    /// Windows selon la mise à l'échelle de l'écran. Il reste à agrandir au même
    /// rythme TOUTE la géométrie en pixels (marges, hauteurs de cartes, tuiles…),
    /// sinon le texte déborde et s'écrase sur les écrans haute densité (13-14").
    ///
    /// <see cref="Scale"/> multiplie la géométrie. <see cref="FontScale"/> vaut
    /// normalement 1 (les points sont déjà agrandis par l'OS) et n'est relevé que
    /// pour SIMULER un écran haute densité en test (variable SRGUI_UISCALE), afin
    /// de reproduire fidèlement le rendu sans changer la mise à l'échelle Windows.
    /// </summary>
    public static class Dpi
    {
        /// <summary>Facteur appliqué à la géométrie (1.0 à 100 %, 1.5 à 150 %…).</summary>
        public static float Scale { get; private set; } = 1f;

        /// <summary>Facteur appliqué aux polices (1.0 en prod ; = Scale en simulation).</summary>
        public static float FontScale { get; private set; } = 1f;

        [DllImport("user32.dll")]
        private static extern int GetDpiForSystem();

        /// <summary>
        /// Initialise l'échelle depuis le DPI système. Si la variable d'environnement
        /// SRGUI_UISCALE est définie (ex. « 1.5 »), elle force l'échelle et le facteur
        /// de police pour simuler un écran haute densité sur un poste à 100 %.
        /// </summary>
        public static void Initialize()
        {
            float systemScale;
            try { systemScale = GetDpiForSystem() / 96f; }
            catch { systemScale = 1f; }

            var sim = Environment.GetEnvironmentVariable("SRGUI_UISCALE");
            if (!string.IsNullOrWhiteSpace(sim)
                && float.TryParse(sim, System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture, out var forced)
                && forced > 0.5f && forced < 4f)
            {
                // Simulation : on agrandit géométrie ET polices (l'OS ne le fera pas
                // puisque le poste de test est à 100 %).
                Scale = forced;
                FontScale = forced;
            }
            else
            {
                Scale = systemScale < 0.5f ? 1f : systemScale;
                FontScale = 1f;
            }
        }

        /// <summary>Met un entier (pixels logiques 96 dpi) à l'échelle de la géométrie.</summary>
        public static int S(int value) => (int)MathF.Round(value * Scale);

        /// <summary>Met un flottant à l'échelle de la géométrie.</summary>
        public static float Sf(float value) => value * Scale;

        /// <summary>Crée une police en points, agrandie par <see cref="FontScale"/> (no-op en prod).</summary>
        public static Font Font(string family, float pointSize, FontStyle style = FontStyle.Regular)
            => new(family, pointSize * FontScale, style);
    }
}
