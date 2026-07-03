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
    /// Icônes Windows natives extraites depuis imageres.dll (Win 10) ou
    /// imageres.dll.mun (Win 11 - ressources MUI externalisées).
    ///
    /// Stratégie d'extraction :
    ///   1. SHDefExtractIcon sur imageres.dll.mun  (Win 11)
    ///   2. SHDefExtractIcon sur imageres.dll      (Win 10/11)
    ///   3. SHGetFileInfo sur le dossier shell réel (fallback)
    ///
    /// SHDefExtractIcon gère nativement les fichiers .mun et les DLL
    /// standard, contrairement à ExtractIconEx qui échoue sur .mun.
    ///
    /// Index imageres validés Win 10 21H2 / Win 11 23H2 :
    ///   3   Dossier générique          15  Bureau
    ///   5   Lecteur réseau             20  Musique
    ///   25  Images                     26  Vidéos
    ///   27  Téléchargements            30  Documents
    ///   34  Profil utilisateur         36  Dossier public
    ///   112 OneNote                   168  Fond d'écran
    ///   174 Excel/feuille calcul      175  Word/document
    ///   176 Sticky Notes              220  Modèles Office
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsIcons
    {
        // ── P/Invoke ──────────────────────────────────────────────────────────

        // SHDefExtractIcon : fonctionne sur .dll et .mun
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int SHDefExtractIconW(
            string pszIconFile,
            int    iIndex,
            uint   uFlags,
            out IntPtr phiconLarge,
            out IntPtr phiconSmall,
            uint   nIconSize);   // LOWORD = large size, HIWORD = small size

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // SHGetFileInfo (fallback dossiers shell réels)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int    iIcon;
            public uint   dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]  public string szTypeName;
        }

        private const uint SHGFI_ICON      = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        // ── Chemins imageres ──────────────────────────────────────────────────

        private static readonly string[] _imageresPaths = BuildPaths();

        private static string[] BuildPaths()
        {
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var list = new List<string>();

            // .mun en premier : Win 11 y stocke les vraies ressources
            string mun = Path.Combine(sys32, "imageres.dll.mun");
            if (File.Exists(mun)) list.Add(mun);

            string dll = Path.Combine(sys32, "imageres.dll");
            if (File.Exists(dll)) list.Add(dll);

            return list.ToArray();
        }

        // ── Table clé → index imageres ────────────────────────────────────────

        private static readonly Dictionary<string, int> _indexMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Fichiers utilisateur
                { "OldProfile",      34  },
                { "Desktop",         15  },
                { "Documents",       30  },
                { "Pictures",        25  },
                { "Videos",          26  },
                { "Downloads",       27  },
                { "Music",           20  },
                { "Public",          36  },

                // Bureautique
                { "Outlook",         174 },
                { "Signatures",      175 },
                { "OfficeTemplates", 220 },
                { "OneNote",         112 },
                { "StickyNotes",     176 },
                { "ExcelMacros",     174 },

                // Système
                { "Wallpaper",       168 },
                { "NetworkDrives",   5   },

                // Logiciels métier – dossier générique
                { "Sap",             3   },
                { "IpSoftphone",     3   },
                { "LaunchApps",      3   },
            };

        // ── Cache ─────────────────────────────────────────────────────────────

        private static readonly Dictionary<(string key, int size), Bitmap> _cache = new();

        /// <summary>
        /// Retourne un Bitmap <paramref name="size"/>×<paramref name="size"/> de
        /// l'icône Windows correspondant à la clé, ou null en cas d'échec.
        /// Le bitmap est mis en cache — ne pas disposer.
        /// </summary>
        public static Bitmap? Get(string key, int size)
        {
            var cacheKey = (key, size);
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            if (!_indexMap.TryGetValue(key, out int index))
                return null;

            Bitmap? bmp = TryExtractFromImageres(index, size)
                       ?? TryExtractFromShellFolder(key, size);

            if (bmp != null)
                _cache[cacheKey] = bmp;

            return bmp;
        }

        /// <summary>Vide le cache (changement de thème/DPI).</summary>
        public static void ClearCache()
        {
            foreach (var b in _cache.Values) b.Dispose();
            _cache.Clear();
        }

        // ── Extraction via SHDefExtractIcon ───────────────────────────────────

        private static Bitmap? TryExtractFromImageres(int index, int size)
        {
            foreach (string path in _imageresPaths)
            {
                Bitmap? bmp = ExtractOne(path, index, size);
                if (bmp != null) return bmp;
            }
            return null;
        }

        private static Bitmap? ExtractOne(string dllPath, int index, int targetSize)
        {
            // nIconSize : LOWORD = taille grande icône demandée (ex. 48)
            // On demande 48 et on redimensionne après si besoin.
            uint iconSize = (uint)Math.Min(48, Math.Max(16, targetSize));
            uint nIconSize = iconSize; // LOWORD seulement suffit

            IntPtr hLarge = IntPtr.Zero;
            IntPtr hSmall = IntPtr.Zero;
            try
            {
                // S_OK = 0, S_FALSE = 1, tout autre code = erreur
                int hr = SHDefExtractIconW(dllPath, index, 0,
                                           out hLarge, out hSmall, nIconSize);
                if (hr != 0 && hr != 1) return null;

                IntPtr hUse = (hLarge != IntPtr.Zero) ? hLarge : hSmall;
                if (hUse == IntPtr.Zero) return null;

                using var icon = Icon.FromHandle(hUse);
                var bmp32 = IconToBitmap(icon);
                if (bmp32 == null) return null;

                if (bmp32.Width == targetSize && bmp32.Height == targetSize)
                    return bmp32;

                return Resize(bmp32, targetSize);
            }
            catch { return null; }
            finally
            {
                if (hLarge != IntPtr.Zero) try { DestroyIcon(hLarge); } catch { }
                if (hSmall != IntPtr.Zero && hSmall != hLarge)
                    try { DestroyIcon(hSmall); } catch { }
            }
        }

        // ── Fallback SHGetFileInfo (dossiers shell réels) ─────────────────────

        private static readonly Dictionary<string, string?> _shellPaths =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Desktop",   Environment.GetFolderPath(Environment.SpecialFolder.Desktop)      },
                { "Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)  },
                { "Pictures",  Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)   },
                { "Videos",    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)     },
                { "Music",     Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)      },
                { "Downloads", GetDownloadsPath()                                                },
                { "Public",    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) },
            };

        private static string? GetDownloadsPath()
        {
            try
            {
                string dl = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                return Directory.Exists(dl) ? dl : null;
            }
            catch { return null; }
        }

        private static Bitmap? TryExtractFromShellFolder(string key, int size)
        {
            if (!_shellPaths.TryGetValue(key, out string? path) ||
                string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return null;

            try
            {
                var sfi = new SHFILEINFO();
                IntPtr res = SHGetFileInfo(path, 0, ref sfi,
                    (uint)Marshal.SizeOf(sfi), SHGFI_ICON | SHGFI_LARGEICON);

                if (res == IntPtr.Zero || sfi.hIcon == IntPtr.Zero) return null;

                using var icon = Icon.FromHandle(sfi.hIcon);
                var bmp32 = IconToBitmap(icon);
                if (bmp32 == null) return null;

                return bmp32.Width == size && bmp32.Height == size
                    ? bmp32
                    : Resize(bmp32, size);
            }
            catch { return null; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Bitmap? Resize(Bitmap src, int size)
        {
            try
            {
                var dst = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(dst);
                g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode      = SmoothingMode.AntiAlias;
                g.PixelOffsetMode    = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(src, 0, 0, size, size);
                src.Dispose();
                return dst;
            }
            catch { src.Dispose(); return null; }
        }

        private static Bitmap? IconToBitmap(Icon icon)
        {
            try
            {
                var bmp = new Bitmap(icon.Width, icon.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Transparent);
                g.DrawIcon(icon, 0, 0);
                return bmp;
            }
            catch { return null; }
        }
    }
}
