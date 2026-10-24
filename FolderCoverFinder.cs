// FolderCoverFinder.cs: the picture that stands for a folder, for a music file that has no cover art of its own.
// The same rules as SweWolf File Explorer's FolderCoverFinder (the picture under its folder tree), so the player
// shows what the explorer shows for the folder. In this order:
//   0. The picture the user chose for the folder in the explorer (Set as Folder Picture), kept in the shared
//      library (LibraryStore.GetFolderPicture). "No Folder Picture" (an empty name) gives nothing.
//   1. A picture in the folder with a cover-like name, best first: Folder, Cover, Front, Poster, AlbumArt (also
//      Windows Media Player's AlbumArt_{...}_Large), Album, Thumb, Thumbnail, or a name equal to the folder's
//      (or ending in "cover"). Hidden files count: Folder.jpg is usually hidden.
//   2. The cover embedded in the first music files of the folder (up to MaxMusicFilesTried of them).
// Not copied from the explorer: the folder's own icon from desktop.ini and pictures inside archives. There is no
// "first picture of the folder" fallback on purpose: in a folder of photos it would be a random one.
// Safe to call from a background thread. Any problem (no access, damaged picture) gives null.
//
// When the explorer's rules change, change them here too.
//
// History:
//   2026-10-23  First version.

namespace SwewolfMediaPlayer
{
    internal static class FolderCoverFinder
    {
        private static readonly string[] CoverNames = { "folder", "cover", "front", "poster", "albumart", "album", "thumb", "thumbnail" };

        // The picture types System.Drawing can read.
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".bmp", ".tif", ".tiff" };

        private const int MaxMusicFilesTried = 5;

        private static readonly System.Text.RegularExpressions.Regex WmpArt =
            new(@"^AlbumArt_(\{[0-9A-Fa-f-]+\})_(Large|Small)$", System.Text.RegularExpressions.RegexOptions.Compiled);

        // The picture for the folder of musicFile, or null. musicExtensions: the file types whose tags may hold a cover.
        public static Bitmap? Find(string musicFile, string[] musicExtensions)
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

                // 2. The cover embedded in a music file of the folder.
                var music = files.Where(f => musicExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                    .Take(MaxMusicFilesTried);
                foreach (string song in music)
                {
                    if (EmbeddedCover(song) is { } cover)
                    {
                        return cover;
                    }
                }
            }
            catch (Exception)
            {
                // A folder that is gone, no access: no picture.
            }
            return null;
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

        private static Bitmap? EmbeddedCover(string song)
        {
            try
            {
                using TagLib.File tags = TagLib.File.Create(song);
                if (tags.Tag.Pictures.Length == 0)
                {
                    return null;
                }
                TagLib.IPicture picture = tags.Tag.Pictures.FirstOrDefault(p => p.Type == TagLib.PictureType.FrontCover) ?? tags.Tag.Pictures[0];
                using MemoryStream stream = new(picture.Data.Data);
                using Image loaded = Image.FromStream(stream);
                return new Bitmap(loaded);
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
