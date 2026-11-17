// DvdSupport.cs: video DVDs (with their menus) played through LibVLC's "dvdnav" reader.
//
// A DVD is opened like a file whose name is VIDEO_TS.IFO (the disc in a drive, or a copy of the VIDEO_TS folder on disk) or an .iso image;
// MediaPlayerForm.CreateMedia turns that into the address LibVLC reads (Location). The LibVLC package of this program has no DVD reader, so the
// two plugins of VLC 3.0.24 are copied next to it (Resources\VlcDvd, see the README.txt there). The decryption of copy-protected discs is
// inside libdvdread_plugin.dll.
//
// History:
//   2026-11-16  First version: Media > Open DVD (one disc: opened at once, several: the user chooses), VIDEO_TS.IFO and .iso files.
//   2026-11-17  A drive (E: or E:\) or a folder with a VIDEO_TS folder typed in the Media File box (or given on the command line) opens as a DVD (TryResolve).

namespace SwewolfMediaPlayer
{
    /// <summary>A video DVD found in a drive.</summary>
    internal sealed record DvdDisc(string Root, string Label)
    {
        /// <summary>The file that stands for the disc: the player opens this path.</summary>
        public string InfoFile => Path.Combine(Root, "VIDEO_TS", DvdSupport.InfoFileName);

        /// <summary>"E: Jedi Junkies" for a menu.</summary>
        public string Display => string.IsNullOrWhiteSpace(Label) ? Root.TrimEnd('\\') : $"{Root.TrimEnd('\\')} {Label}";
    }

    internal static class DvdSupport
    {
        public const string InfoFileName = "VIDEO_TS.IFO";

        /// <summary>True for VIDEO_TS.IFO (of a disc or a folder) and for an .iso image (taken to be a DVD image).</summary>
        public static bool IsDvdPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            return Path.GetFileName(path).Equals(InfoFileName, StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(path).Equals(".iso", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>"E:", "E:\", a folder that holds a VIDEO_TS folder, or the VIDEO_TS folder itself: the VIDEO_TS.IFO file that stands for that DVD.</summary>
        public static bool TryResolve(string? text, out string infoFile)
        {
            infoFile = "";
            string path = (text ?? "").Trim().Trim('"');
            if (path.Length == 2 && path[1] == ':')
            {
                path += Path.DirectorySeparatorChar;   // "E:" alone means the current folder of that drive: take its root
            }
            if (path.Length == 0 || !Path.IsPathRooted(path) || !Directory.Exists(path))
            {
                return false;
            }
            string folder = Path.GetFullPath(path);
            string candidate = Path.Combine(folder, "VIDEO_TS", InfoFileName);
            if (!File.Exists(candidate))
            {
                candidate = Path.Combine(folder, InfoFileName);   // the VIDEO_TS folder itself
            }
            if (!File.Exists(candidate))
            {
                return false;
            }
            infoFile = candidate;
            return true;
        }

        private static bool IsImage(string path) => Path.GetExtension(path).Equals(".iso", StringComparison.OrdinalIgnoreCase);

        /// <summary>The address LibVLC reads: dvdnav:///E: for a disc, the folder holding VIDEO_TS, or the image file.</summary>
        public static string Location(string path)
        {
            string full = Path.GetFullPath(path);
            if (!IsImage(full))
            {
                string? videoTs = Path.GetDirectoryName(full);   // ...\VIDEO_TS
                full = (videoTs == null ? null : Path.GetDirectoryName(videoTs)) ?? full;   // the disc or folder above it
            }
            return "dvdnav:///" + full.TrimEnd('\\').Replace('\\', '/');   // "E:" for the root of a drive
        }

        /// <summary>What the window says: the disc's name for a disc, the folder name for a copy, the file name for an image.</summary>
        public static string Title(string path)
        {
            if (IsImage(path))
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            string? folder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(path)));   // above VIDEO_TS
            if (folder == null)
            {
                return "DVD";
            }
            string name = Path.GetFileName(folder);
            if (name.Length > 0)
            {
                return name;
            }
            try
            {
                var drive = new DriveInfo(folder);
                return drive.IsReady && drive.VolumeLabel.Length > 0 ? drive.VolumeLabel : "DVD";
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
            {
                return "DVD";
            }
        }

        /// <summary>The optical drives that hold a video DVD (a VIDEO_TS folder), in drive letter order.</summary>
        public static List<DvdDisc> FindDiscs()
        {
            var found = new List<DvdDisc>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType == DriveType.CDRom && drive.IsReady
                        && File.Exists(Path.Combine(drive.RootDirectory.FullName, "VIDEO_TS", InfoFileName)))
                    {
                        found.Add(new DvdDisc(drive.RootDirectory.FullName, drive.VolumeLabel));
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // A drive that is not ready or cannot be read: it holds no disc for us.
                }
            }
            return found;
        }

        /// <summary>True when the machine has an optical drive at all (for the message when no disc is found).</summary>
        public static bool HasOpticalDrive() => DriveInfo.GetDrives().Any(d => d.DriveType == DriveType.CDRom);
    }
}
