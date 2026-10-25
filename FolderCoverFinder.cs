// FolderCoverFinder.cs: the picture that stands for a folder, for a music file that has no cover art of its own.
// The same rules as SweWolf File Explorer's FolderCoverFinder (the picture under its folder tree), so the player
// shows what the explorer shows for the folder. In this order:
//   0. The picture the user chose for the folder in the explorer (Set as Folder Picture), kept in the shared
//      library (LibraryStore.GetFolderPicture). "No Folder Picture" (an empty name) gives nothing.
//   1. A picture in the folder with a cover-like name, best first: Folder, Cover, Front, Poster, AlbumArt (also
//      Windows Media Player's AlbumArt_{...}_Large), Album, Thumb, Thumbnail, or a name equal to the folder's
//      (or ending in "cover"). Hidden files count: Folder.jpg is usually hidden.
//   1b. The folder's own icon from its desktop.ini (IconResource), the very last fallback. Not for the standard Windows
//      folders (Desktop, Downloads, Documents, Pictures, Music, Videos; the explorer does show theirs): only for a
//      folder the user made or marked.
// Not copied from the explorer: pictures inside archives. There is no
// "first picture of the folder" fallback on purpose: in a folder of photos it would be a random one. And a cover
// is never taken from another music file: the folder may be a Downloads folder, not an album.
// Safe to call from a background thread. Any problem (no access, damaged picture) gives null.
//
// When the explorer's rules change, change them here too.
//
// History:
//   2026-10-25  The icon of the folder's desktop.ini as the last fallback, except for the standard Windows folders.
//   2026-10-25  No more cover from other music files of the folder (step 2 removed; Find has no musicExtensions).
//   2026-10-23  First version.

namespace SwewolfMediaPlayer
{
    internal static class FolderCoverFinder
    {
        private static readonly string[] CoverNames = { "folder", "cover", "front", "poster", "albumart", "album", "thumb", "thumbnail" };

        // The picture types System.Drawing can read.
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".bmp", ".tif", ".tiff" };

        private static readonly System.Text.RegularExpressions.Regex WmpArt =
            new(@"^AlbumArt_(\{[0-9A-Fa-f-]+\})_(Large|Small)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        // The picture for the folder of musicFile, or null.
        public static Bitmap? Find(string musicFile)
        {
            try
            {
                string? folder = Path.GetDirectoryName(Path.GetFullPath(musicFile));
                if (folder == null || !Directory.Exists(folder))
                {
                    return null;
                }

                // 0. The picture the user chose for this folder; if it is gone or unreadable the search goes on.
                string? choice = LibraryStore.GetFolderPicture(folder);
                if (choice == "")
                {
                    return null;
                }
                if (choice is { Length: > 0 })
                {
                    string chosen = Path.Combine(folder, choice);   // a name in the folder, or a full path
                    if (File.Exists(chosen) && Load(chosen) is { } picked)
                    {
                        return picked;
                    }
                }

                List<string> files = Directory.EnumerateFiles(folder).ToList();   // hidden files included
                string name = Path.GetFileName(folder.TrimEnd('\\', '/'));

                // 1. A picture with a cover-like name. If the best one cannot be read, the next one is tried.
                List<string> pictures = files.Where(IsImage).ToList();
                var wmpArt = pictures.Where(f => WmpArt.IsMatch(Path.GetFileNameWithoutExtension(f))).ToList();
                // With more than one Windows Media Player album in a folder none of them is the picture of the folder itself.
                if (wmpArt.Select(f => WmpArt.Match(Path.GetFileNameWithoutExtension(f)).Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                {
                    pictures = pictures.Where(f => !Path.GetFileNameWithoutExtension(f).StartsWith("albumart", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                foreach (string picture in Ranked(pictures, name))
                {
                    if (Load(picture) is { } image)
                    {
                        return image;
                    }
                }

                // 1b. The folder's own icon from its desktop.ini, as the very last fallback and never for the standard Windows
                //     folders (Desktop, Downloads, Documents, Pictures, Music, Videos): their icon is Windows' own, not the user's.
                if (!IsStandardFolder(folder) && FromDesktopIni(folder, files) is { } icon)
                {
                    return icon;
                }
            }
            catch (Exception)
            {
                // A folder that is gone, no access: no picture.
            }
            return null;
        }

        // The known folders whose desktop.ini icon is not used (found by Windows' known-folder lookup, so a moved or
        // renamed one counts too). Only the folders themselves, not what is inside them.
        private static readonly Guid[] StandardFolderIds =
        {
            new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"),   // Desktop
            new("FDD39AD0-238F-46AF-ADB4-6C85480369C7"),   // Documents
            new("374DE290-123F-4565-9164-39C4925E467B"),   // Downloads
            new("33E28130-4E1E-4676-835A-98395C3BC3BB"),   // Pictures
            new("4BD8D571-6D19-48D3-BE97-422220080E43"),   // Music
            new("18989B1D-99B5-455B-841C-AB7C74E4DDFC"),   // Videos
        };

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern uint PrivateExtractIcons(string file, int index, int cx, int cy, IntPtr[] icons, int[] ids, uint count, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr icon);

        private static bool IsStandardFolder(string folder)
        {
            string wanted = Path.GetFullPath(folder).TrimEnd('\\', '/');
            foreach (Guid id in StandardFolderIds)
            {
                Guid copy = id;
                if (SHGetKnownFolderPath(ref copy, 0, IntPtr.Zero, out IntPtr pointer) != 0)
                {
                    continue;
                }
                try
                {
                    string? known = System.Runtime.InteropServices.Marshal.PtrToStringUni(pointer);
                    if (known != null && string.Equals(Path.GetFullPath(known).TrimEnd('\\', '/'), wanted, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pointer);
                }
            }
            return false;
        }

        // The icon a folder's desktop.ini names: IconResource=path,index (or IconFile= and IconIndex=) in [.ShellClassInfo];
        // a negative index is a resource id, as in imageres.dll,-184. Drawn at 256 pixels. Null when there is none or it cannot be read.
        private static Bitmap? FromDesktopIni(string folder, List<string> files)
        {
            try
            {
                string? ini = files.FirstOrDefault(f => string.Equals(Path.GetFileName(f), "desktop.ini", StringComparison.OrdinalIgnoreCase));
                if (ini == null || new FileInfo(ini).Length is 0 or > 256 * 1024)
                {
                    return null;
                }

                string? resource = null, iconFile = null, iconIndex = null;
                bool inSection = false;
                foreach (string raw in DecodeText(File.ReadAllBytes(ini)).Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.StartsWith('['))
                    {
                        inSection = line.Equals("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    int eq = line.IndexOf('=');
                    if (!inSection || eq <= 0)
                    {
                        continue;
                    }
                    string key = line[..eq].Trim();
                    string value = line[(eq + 1)..].Trim().Trim('"');
                    if (key.Equals("IconResource", StringComparison.OrdinalIgnoreCase)) resource = value;
                    else if (key.Equals("IconFile", StringComparison.OrdinalIgnoreCase)) iconFile = value;
                    else if (key.Equals("IconIndex", StringComparison.OrdinalIgnoreCase)) iconIndex = value;
                }

                string path;
                int index = 0;
                if (!string.IsNullOrEmpty(resource))
                {
                    int comma = resource.LastIndexOf(',');
                    path = comma > 0 ? resource[..comma] : resource;
                    if (comma > 0)
                    {
                        int.TryParse(resource[(comma + 1)..].Trim(), out index);
                    }
                }
                else if (!string.IsNullOrEmpty(iconFile))
                {
                    path = iconFile;
                    int.TryParse(iconIndex, out index);
                }
                else
                {
                    return null;
                }

                path = Environment.ExpandEnvironmentVariables(path.Trim());
                if (!Path.IsPathRooted(path))   // next to the desktop.ini, else in System32
                {
                    string local = Path.Combine(folder, path);
                    path = File.Exists(local) ? local : Path.Combine(Environment.SystemDirectory, path);
                }
                return File.Exists(path) ? ExtractIcon(path, index) : null;
            }
            catch (Exception)
            {
                return null;   // locked, damaged, no such resource
            }
        }

        // An icon of an exe / dll / ico at 256 pixels as a bitmap; null if it cannot be read.
        private static Bitmap? ExtractIcon(string path, int index)
        {
            IntPtr[] icons = new IntPtr[1];
            if (PrivateExtractIcons(path, index, 256, 256, icons, new int[1], 1, 0) < 1 || icons[0] == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                using Icon icon = Icon.FromHandle(icons[0]);
                using Bitmap drawn = icon.ToBitmap();
                return new Bitmap(drawn);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                DestroyIcon(icons[0]);
            }
        }

        // A desktop.ini is ANSI, UTF-8 or UTF-16 depending on its characters.
        private static string DecodeText(byte[] b)
        {
            if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) return System.Text.Encoding.Unicode.GetString(b, 2, b.Length - 2);
            if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) return System.Text.Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2);
            if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) return System.Text.Encoding.UTF8.GetString(b, 3, b.Length - 3);
            try
            {
                return new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(b);
            }
            catch (System.Text.DecoderFallbackException)
            {
                return System.Text.Encoding.Latin1.GetString(b);
            }
        }

        private static bool IsImage(string file) => ImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);

        private static Bitmap? Load(string file)
        {
            try
            {
                using Image loaded = Image.FromFile(file);
                return new Bitmap(loaded);   // a copy: the file is not kept open
            }
            catch (Exception)
            {
                return null;
            }
        }

        // The pictures that look like a cover, best first: by their name, and among equals the biggest file.
        private static IEnumerable<string> Ranked(IEnumerable<string> pictures, string folderName)
        {
            return pictures
                .Select(p => (Path: p, Rank: Rank(System.IO.Path.GetFileNameWithoutExtension(p), folderName)))
                .Where(x => x.Rank >= 0)
                .OrderBy(x => x.Rank)
                .ThenByDescending(x => Length(x.Path))   // AlbumArt_..._Large before ..._Small
                .Select(x => x.Path);
        }

        // 0 is the best name; -1 is not a cover name.
        private static int Rank(string baseName, string folderName)
        {
            for (int i = 0; i < CoverNames.Length; i++)
            {
                if (baseName.Equals(CoverNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            if (baseName.StartsWith("albumart", StringComparison.OrdinalIgnoreCase))
            {
                return Array.IndexOf(CoverNames, "albumart");   // AlbumArt_{GUID}_Large.jpg, AlbumArtSmall.jpg
            }
            if (baseName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
            {
                return CoverNames.Length;
            }
            return baseName.EndsWith("cover", StringComparison.OrdinalIgnoreCase) ? CoverNames.Length + 1 : -1;
        }

        private static long Length(string file)
        {
            try
            {
                return new FileInfo(file).Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
