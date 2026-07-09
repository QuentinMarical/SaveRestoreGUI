namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Palette de couleurs unique — style Fluent / Windows 11, thème sombre.
    /// </summary>
    public sealed class ThemePalette
    {
        public Color Background { get; init; }
        public Color Surface { get; init; }
        public Color SurfaceHover { get; init; }
        public Color Sidebar { get; init; }
        public Color Text { get; init; }
        public Color TextSecondary { get; init; }
        public Color Accent { get; init; }
        public Color AccentHover { get; init; }
        public Color Success { get; init; }
        public Color Warning { get; init; }
        public Color Danger { get; init; }
        public Color Border { get; init; }
        public Color InputBackground { get; init; }
        public Color ConsoleBackground { get; init; }
        public Color ConsoleText { get; init; }

        /// <summary>Liseré clair simulant l'accroche de lumière d'un matériau Mica en haut des cartes.</summary>
        public Color CardHighlight { get; init; }

        public static ThemePalette Dark { get; } = new()
        {
            Background      = Color.FromArgb(32,  32,  32),
            Surface         = Color.FromArgb(44,  44,  44),
            SurfaceHover    = Color.FromArgb(56,  56,  56),
            Sidebar         = Color.FromArgb(24,  24,  24),
            Text            = Color.FromArgb(255, 255, 255),
            TextSecondary   = Color.FromArgb(168, 168, 168),
            Accent          = Color.FromArgb(65, 145, 255),
            AccentHover     = Color.FromArgb(100, 170, 255),
            Success         = Color.FromArgb(108, 203, 95),
            Warning         = Color.FromArgb(255, 185, 0),
            Danger          = Color.FromArgb(255, 99,  87),
            Border          = Color.FromArgb(64,  64,  64),
            InputBackground = Color.FromArgb(40,  40,  40),
            ConsoleBackground = Color.FromArgb(12, 12, 12),
            ConsoleText     = Color.FromArgb(80, 250, 123),
            CardHighlight   = Color.FromArgb(18, 255, 255, 255),
        };
    }

    /// <summary>
    /// Gestionnaire de thème global — thème sombre unique (pas de bascule clair/sombre).
    /// </summary>
    public static class ThemeManager
    {
        public static ThemePalette Palette => ThemePalette.Dark;
    }
}
