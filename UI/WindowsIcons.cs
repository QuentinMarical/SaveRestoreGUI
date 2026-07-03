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
    /// Icônes Windows natives via SHGetKnownFolderPath + SHGetFileInfo.
    /// Stratégie par type de clé :
    ///   - Dossiers shell  → KNOWNFOLDERID GUID  → SHGetFileInfo
    ///   - Apps Office     → chemin exe          → SHGetFileInfo
    ///   - Fallback        → dossier générique réel
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsIcons
    {
        // ── P/Invoke ──────────────────────────────────────────────────────────

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath(
            ref Guid rfid, uint dwFlags, IntPtr hToken,
            out IntPtr ppszPath);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr ptr);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int    iIcon;
            public uint   dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]  public string szTypeName;
        }

        private const uint SHGFI_ICON       = 0x100;
        private const uint SHGFI_LARGEICON  = 0x000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x080;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // ── KnownFolder GUIDs (stables sur toutes versions Windows) ───────────
        // Source : https://learn.microsoft.com/windows/win32/shell/knownfolderid

        private static readonly Guid FOLDERID_Desktop         = new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
        private static readonly Guid FOLDERID_Documents       = new("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
        private static readonly Guid FOLDERID_Pictures        = new("33E28130-4E1E-4676-835A-98395C3BC3BB");
        private static readonly Guid FOLDERID_Videos          = new("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
        private static readonly Guid FOLDERID_Downloads       = new("374DE290-123F-4565-9164-39C4925E467B");
        private static readonly Guid FOLDERID_Music           = new("4BD8D571-6D19-48D3-BE97-422220080E43");
        private static readonly Guid FOLDERID_PublicDocuments = new("ED4824AF-DCE4-45A8-81E2-FC7965083634");
        private static readonly Guid FOLDERID_Profile         = new("5E6C858F-0E22-4760-9AFE-EA3317B67173");
        private static readonly Guid FOLDERID_Wallpaper       = new("B7BEDE81-DF94-4682-A7D8-57A52620B86F"); // Themes
        // Lecteurs réseau : on utilise un chemin UNC fictif pour obtenir l'icône réseau

        // ── Table clé → stratégie ─────────────────────────────────────────────

        private enum IconStrategy { KnownFolder, ExePath, NetworkDrive, GenericFolder }

        private class IconSource
        {
            public IconStrategy Strategy;
            public Guid         FolderGuid;  // pour KnownFolder
            public string[]?    ExeCandidates; // pour ExePath
        }

        private static readonly Dictionary<string, IconSource> _sources =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Dossiers utilisateur via KNOWNFOLDERID
                { "Desktop",     new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Desktop         } },
                { "Documents",   new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Documents       } },
                { "Pictures",    new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Pictures        } },
                { "Videos",      new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Videos          } },
                { "Downloads",   new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Downloads       } },
                { "Music",       new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Music           } },
                { "Public",      new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_PublicDocuments } },
                { "OldProfile",  new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Profile         } },
                { "Wallpaper",   new() { Strategy = IconStrategy.KnownFolder, FolderGuid = FOLDERID_Wallpaper       } },

                // Lecteur réseau
                { "NetworkDrives", new() { Strategy = IconStrategy.NetworkDrive } },

                // Applications Office via leur exécutable
                { "Outlook", new() { Strategy = IconStrategy.ExePath, ExeCandidates = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\OUTLOOK.EXE",
                        @"C:\Program Files\Microsoft Office\Office16\OUTLOOK.EXE",
                    }}},
                { "Signatures", new() { Strategy = IconStrategy.ExePath, ExeCandidates = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\OUTLOOK.EXE",
                    }}},
                { "OfficeTemplates", new() { Strategy = IconStrategy.ExePath, ExeCandidates = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\WINWORD.EXE",
                    }}},
                { "OneNote", new() { Strategy = IconStrategy.ExePath, ExeCandidates = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\ONENOTE.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\ONENOTE.EXE",
                        // OneNote UWP
                        @"C:\Windows\SystemApps\Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe\OneNote.exe",
                    }}},
                { "StickyNotes", new() { Strategy = IconStrategy.ExePath, ExeCandidates = new[]
                    {
                        // UWP : MicrosoftStickyNotes
                        @"C:\Program Files\WindowsApps\Microsoft.MicrosoftStickyNotes_6.1.4.0_x64__8wekyb3d8bbwe\Microsoft.Notes.exe",
                        // Cherché dynamiquement dans WindowsApps si le chemin fixe échoue
                    }}},
                { "ExcelMacros", new() { Strategy = IconStrategy.ExePath, ExeCandidates = new[]
                    {
                        @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE",
                        @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE",
                    }}},

                // SAP et IP Softphone : dossier générique
                { "Sap",         new() { Strategy = IconStrategy.GenericFolder } },
                { "IpSoftphone", new() { Strategy = IconStrategy.GenericFolder } },
                { "LaunchApps",  new() { Strategy = IconStrategy.GenericFolder } },
            };

        // ── Cache ──────────────────────────────────────────────────────────────

        private static readonly Dictionary<(string key, int size), Bitmap> _cache = new();

        public static Bitmap? Get(string key, int size)
        {
            var cacheKey = (key, size);
            if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

            if (!_sources.TryGetValue(key, out var src)) return null;

            Bitmap? bmp = src.Strategy switch
            {
                IconStrategy.KnownFolder  => BmpFromKnownFolder(src.FolderGuid, size),
                IconStrategy.ExePath      => BmpFromExe(src.ExeCandidates!, key, size),
                IconStrategy.NetworkDrive => BmpFromNetworkDrive(size),
                IconStrategy.GenericFolder => BmpFromGenericFolder(size),
                _                         => null
            };

            if (bmp != null) _cache[cacheKey] = bmp;
            return bmp;
        }

        public static void ClearCache()
        {
            foreach (var b in _cache.Values) b.Dispose();
            _cache.Clear();
        }

        // ── Stratégies ────────────────────────────────────────────────────────

        private static Bitmap? BmpFromKnownFolder(Guid guid, int size)
        {
            try
            {
                Guid g = guid;
                int hr = SHGetKnownFolderPath(ref g, 0, IntPtr.Zero, out IntPtr ptr);
                if (hr != 0) return BmpFromPath(null, size); // fallback générique
                string path;
                try   { path = Marshal.PtrToStringUni(ptr)!; }
                finally { CoTaskMemFree(ptr); }
                return BmpFromPath(path, size);
            }
            catch { return null; }
        }

        private static Bitmap? BmpFromExe(string[] candidates, string key, int size)
        {
            // Cherche d'abord parmi les chemins fixes
            foreach (string c in candidates)
                if (File.Exists(c))
                    return BmpFromPath(c, size, isFile: true);

            // Pour StickyNotes UWP : cherche dans WindowsApps
            if (key.Equals("StickyNotes", StringComparison.OrdinalIgnoreCase))
            {
                string? exe = FindStickyNotesExe();
                if (exe != null) return BmpFromPath(exe, size, isFile: true);
            }

            // Fallback : dossier générique
            return BmpFromGenericFolder(size);
        }

        private static string? FindStickyNotesExe()
        {
            try
            {
                string wa = @"C:\Program Files\WindowsApps";
                if (!Directory.Exists(wa)) return null;
                foreach (var dir in Directory.GetDirectories(wa, "Microsoft.MicrosoftStickyNotes*"))
                {
                    // Cherche l'exe principal
                    foreach (string candidate in new[] { "Microsoft.Notes.exe", "StickyNotes.exe" })
                    {
                        string p = Path.Combine(dir, candidate);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { }
            return null;
        }

        private static Bitmap? BmpFromNetworkDrive(int size)
        {
            // Utilise le chemin d'un dossier réseau fictif type UNC pour l'icône
            // Si échec, repli sur imageres index 5 via SHDefExtractIcon
            string? anyNet = FindAnyNetworkDrivePath();
            if (anyNet != null)
                return BmpFromPath(anyNet, size);

            // Repli : icône générique réseau depuis shell32.dll index 275
            return ExtractFromDll(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"),
                275, size);
        }

        private static string? FindAnyNetworkDrivePath()
        {
            try
            {
                foreach (var di in DriveInfo.GetDrives())
                    if (di.DriveType == DriveType.Network && Directory.Exists(di.Name))
                        return di.Name;
            }
            catch { }
            return null;
        }

        private static Bitmap? BmpFromGenericFolder(int size)
        {
            // Icône dossier générique : on fait SHGetFileInfo sur %TEMP%
            string temp = Path.GetTempPath();
            return BmpFromPath(temp, size);
        }

        // ── BmpFromPath (cœur) : SHGetFileInfo sur un chemin réel ──────────────

        private static Bitmap? BmpFromPath(string? path, int size, bool isFile = false)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                var sfi  = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_LARGEICON;
                if (isFile) flags |= SHGFI_USEFILEATTRIBUTES;
                uint attr  = isFile ? FILE_ATTRIBUTE_NORMAL : 0;

                IntPtr res = SHGetFileInfo(path, attr, ref sfi,
                    (uint)Marshal.SizeOf(sfi), flags);

                if (res == IntPtr.Zero || sfi.hIcon == IntPtr.Zero) return null;

                try
                {
                    using var icon  = Icon.FromHandle(sfi.hIcon);
                    var bmp32 = IconToBitmap(icon);
                    if (bmp32 == null) return null;
                    return bmp32.Width == size && bmp32.Height == size
                        ? bmp32
                        : Resize(bmp32, size);
                }
                finally
                {
                    DestroyIcon(sfi.hIcon);
                }
            }
            catch { return null; }
        }

        // ── ExtractFromDll (fallback shell32) ──────────────────────────────────

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int SHDefExtractIconW(
            string pszIconFile, int iIndex, uint uFlags,
            out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIconSize);

        private static Bitmap? ExtractFromDll(string dll, int index, int size)
        {
            IntPtr hL = IntPtr.Zero, hS = IntPtr.Zero;
            try
            {
                int hr = SHDefExtractIconW(dll, index, 0, out hL, out hS, (uint)size);
                if (hr != 0 && hr != 1) return null;
                IntPtr h = hL != IntPtr.Zero ? hL : hS;
                if (h == IntPtr.Zero) return null;
                using var icon = Icon.FromHandle(h);
                var bmp = IconToBitmap(icon);
                if (bmp == null) return null;
                return bmp.Width == size && bmp.Height == size ? bmp : Resize(bmp, size);
            }
            catch { return null; }
            finally
            {
                if (hL != IntPtr.Zero) try { DestroyIcon(hL); } catch { }
                if (hS != IntPtr.Zero && hS != hL) try { DestroyIcon(hS); } catch { }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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
