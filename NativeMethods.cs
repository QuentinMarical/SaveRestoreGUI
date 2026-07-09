using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SaveRestoreGUI
{
    [SupportedOSPlatform("windows")]
    internal static class NativeMethods
    {
        public const uint SPI_SETDESKWALLPAPER  = 0x0014;
        public const uint SPIF_UPDATEINIFILE    = 0x0001;
        public const uint SPIF_SENDCHANGE       = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(
            uint uiAction,
            uint uiParam,
            string pvParam,
            uint fWinIni);

        // ── DWM (Desktop Window Manager) — habillage Windows 11 natif ──────────
        // Titre de fenêtre sombre + coins arrondis : purement cosmétique sur la
        // zone non-cliente, sans effet sur le rendu (opaque) des contrôles custom.
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE  = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND                   = 2;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        /// <summary>
        /// Applique le titre de fenêtre sombre et les coins arrondis (Windows 11).
        /// Sans effet (échec silencieux) sur les versions antérieures de Windows.
        /// </summary>
        public static void ApplyWin11WindowStyle(IntPtr handle)
        {
            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                int corner = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            }
            catch { /* Windows < 11 ou DWM indisponible : ignoré volontairement */ }
        }
    }
}
