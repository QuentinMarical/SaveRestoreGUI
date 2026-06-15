namespace SaveRestoreGUI.UI
{
    /// <summary>
    /// Palette de couleurs pour les thèmes clair et sombre (style Fluent / Windows 11).
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

        public static ThemePalette Dark { get; } = new()
        {
            Background = Color.FromArgb(24, 24, 37),
            Surface = Color.FromArgb(33, 33, 50),
            SurfaceHover = Color.FromArgb(45, 45, 65),
            Sidebar = Color.FromArgb(17, 17, 27),
            Text = Color.FromArgb(220, 224, 240),
            TextSecondary = Color.FromArgb(150, 155, 175),
            Accent = Color.FromArgb(59, 130, 246),
            AccentHover = Color.FromArgb(37, 99, 235),
            Success = Color.FromArgb(34, 197, 94),
            Warning = Color.FromArgb(245, 158, 11),
            Danger = Color.FromArgb(239, 68, 68),
            Border = Color.FromArgb(55, 55, 75),
            InputBackground = Color.FromArgb(28, 28, 42),
            ConsoleBackground = Color.FromArgb(13, 13, 20),
            ConsoleText = Color.FromArgb(80, 250, 123)
        };

        public static ThemePalette Light { get; } = new()
        {
            Background = Color.FromArgb(243, 244, 248),
            Surface = Color.White,
            SurfaceHover = Color.FromArgb(232, 234, 240),
            Sidebar = Color.FromArgb(228, 230, 238),
            Text = Color.FromArgb(28, 28, 32),
            TextSecondary = Color.FromArgb(105, 110, 125),
            Accent = Color.FromArgb(37, 99, 235),
            AccentHover = Color.FromArgb(29, 78, 216),
            Success = Color.FromArgb(22, 163, 74),
            Warning = Color.FromArgb(217, 119, 6),
            Danger = Color.FromArgb(220, 38, 38),
            Border = Color.FromArgb(210, 213, 222),
            InputBackground = Color.White,
            ConsoleBackground = Color.FromArgb(20, 20, 28),
            ConsoleText = Color.FromArgb(80, 250, 123)
        };
    }

    /// <summary>
    /// Gestionnaire de thème global : conserve le thème actif et notifie les abonnés lors d'un changement.
    /// </summary>
    public static class ThemeManager
    {
        private static bool _dark = true;

        public static bool IsDark => _dark;
        public static ThemePalette Palette => _dark ? ThemePalette.Dark : ThemePalette.Light;

        public static event Action? ThemeChanged;

        public static void Toggle()
        {
            _dark = !_dark;
            ThemeChanged?.Invoke();
        }
    }
}
