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
    /// Fournit des icônes Windows natives extraites depuis imageres.dll
    /// (ou imageres.dll.mun) pour chaque élément sauvegardable.
    ///
    /// Index imageres.dll de référence (Win 10/11) :
    ///   3   – Dossier générique
    ///   4   – Dossier ouvert
    ///   5   – Lecteur réseau
    ///   15  – Bureau
    ///   20  – Musique
    ///   25  – Images
    ///   26  – Vidéos
    ///   27  – Téléchargements
    ///   30  – Documents
    ///   34  – Profil utilisateur
    ///   36  – Dossier public
    ///   112 – OneNote (icône dossier magenta)
    ///   168 – Fond d'écran / Personnalisation
    ///   174 – Excel / feuille de calcul
    ///   175 – Word / document Office
    ///   176 – Sticky Notes
    ///   220 – Modèles Office
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsIcons
    {
        // ── P/Invoke ─────────────────────────────────────────────────────────

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(
            string lpszFile,
            int    nIconIndex,
            IntPtr[]? phiconLarge,
            IntPtr[]? phiconSmall,
            int nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // ── Chemins imageres ─────────────────────────────────────────────────

        private static readonly string[] _imageresPaths = BuildPaths();

        private static string[] BuildPaths()
        {
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var list = new List<string>();

            // imageres.dll.mun (Windows 11 — ressources externalisées)
            string mun = Path.Combine(sys32, "imageres.dll.mun");
            if (File.Exists(mun)) list.Add(mun);

            // imageres.dll classique
            string dll = Path.Combine(sys32, "imageres.dll");
            if (File.Exists(dll)) list.Add(dll);

            return list.ToArray();
        }

        // ── Table clé → index imageres ────────────────────────────────────────
        //
        // Indices validés sur Windows 10 21H2 / Windows 11 23H2.
        // En cas de doute, utiliser Resource Hacker ou IconViewer pour vérifier.

        private static readonly Dictionary<string, int> _indexMap =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                // Fichiers utilisateur
                { "OldProfile",      34  },  // Profil utilisateur
                { "Desktop",         15  },  // Bureau
                { "Documents",       30  },  // Documents
                { "Pictures",        25  },  // Images
                { "Videos",          26  },  // Vidéos
                { "Downloads",       27  },  // Téléchargements
                { "Music",           20  },  // Musique
                { "Public",          36  },  // Dossier public

                // Bureautique
                { "Outlook",         174 },  // Enveloppe / Outlook (approx.)
                { "Signatures",      175 },  // Document Word
                { "OfficeTemplates", 220 },  // Modèles Office
                { "OneNote",         112 },  // Dossier OneNote
                { "StickyNotes",     176 },  // Sticky Notes
                { "ExcelMacros",     174 },  // Feuille de calcul

                // Système
                { "Wallpaper",       168 },  // Personnalisation / Bureau
                { "NetworkDrives",   5   },  // Lecteur réseau

                // Logiciels métier — pas d'icône dédiée, dossier générique
                { "Sap",             3   },
                { "IpSoftphone",     3   },
                { "LaunchApps",      3   },
            };

        // ── Cache Bitmap ──────────────────────────────────────────────────────

        private static readonly Dictionary<(string key, int size), Bitmap> _cache =
            new();

        /// <summary>
        /// Retourne un Bitmap <paramref name="size"/>×<paramref name="size"/> de
        /// l'icône Windows correspondant à la clé.
        /// Le bitmap est mis en cache — ne pas disposer.
        /// Retourne null si l'extraction échoue sur toutes les sources.
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

        /// <summary>Vide le cache (à appeler lors d'un changement de thème/DPI).</summary>
        public static void ClearCache()
        {
            foreach (var b in _cache.Values)
                b.Dispose();
            _cache.Clear();
        }

        // ── Extraction imageres.dll ───────────────────────────────────────────

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
            // On demande la grande icône (32×32 ou 48×48 selon le système).
            var large = new IntPtr[1];
            try
            {
                int count = ExtractIconEx(dllPath, index, large, null, 1);
                if (count < 1 || large[0] == IntPtr.Zero)
                    return null;

                using var icon = Icon.FromHandle(large[0]);
                // Convertir en Bitmap 32bpp ARGB pour avoir la transparence
                var bmp32 = IconToBitmap(icon);
                if (bmp32 == null) return null;

                // Redimensionner si nécessaire
                if (bmp32.Width == targetSize && bmp32.Height == targetSize)
                    return bmp32;

                return Resize(bmp32, targetSize);
            }
            catch
            {
                return null;
            }
            finally
            {
                if (large[0] != IntPtr.Zero)
                    try { DestroyIcon(large[0]); } catch { }
            }
        }

        // ── Fallback : SHGetFileInfo sur le dossier shell réel ────────────────

        private static readonly Dictionary<string, string?> _shellPaths =
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                { "Desktop",     Environment.GetFolderPath(Environment.SpecialFolder.Desktop)     },
                { "Documents",   Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)  },
                { "Pictures",    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)   },
                { "Videos",      Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)     },
                { "Music",       Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)      },
                { "Downloads",   GetDownloadsPath()                                                },
                { "Public",      Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) },
            };

        private static string? GetDownloadsPath()
        {
            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string dl   = Path.Combine(home, "Downloads");
                return Directory.Exists(dl) ? dl : null;
            }
            catch { return null; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int    iIcon;
            public uint   dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]  public string szTypeName;
        }

        private const uint SHGFI_ICON      = 0x100;
        private const uint SHGFI_LARGEICON = 0x000;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

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

                var result = bmp32.Width == size && bmp32.Height == size
                    ? bmp32
                    : Resize(bmp32);

                return result;
            }
            catch { return null; }
        }

        private static Bitmap? Resize(Bitmap src, int size = 36)
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

        /// <summary>Convertit une Icon en Bitmap 32bpp ARGB (conserve la transparence).</summary>
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
