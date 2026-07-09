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
    }
}
