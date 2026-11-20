// CdPlaylist.cs: an audio CD played from the first track to the last. The tracks of the disc (the .cda files in the drive's root) are
// written to a small playlist file and that file is opened like any playlist (MediaPlayerForm.Playlist.cs): the order is the order of the
// disc and cannot be changed. The titles in the file are what the shared lookup (CdDiscInfo.cs) knows at that moment; the list in the
// player is brought up to date by itself when the lookup answers a little later (MediaPlayerForm.RefreshCdNames).
//
// The playlist file is a throwaway: %TEMP%\SweWolfSoftware\SwewolfMediaPlayer\<album or "Audio CD (E)">.m3u8, written again every time.
//
// History:
//   2026-11-19  First version: Media > Open DVD/CD and a drive typed in the Media File box.

using System.Text;

namespace SwewolfMediaPlayer
{
    internal sealed record AudioCd(string Root, int Tracks)
    {
        /// <summary>For the list of discs to choose from: "E: Audio CD (12 tracks)", with the album when it is known.</summary>
        public string Display
        {
            get
            {
                string drive = Root.TrimEnd('\\');
                string? title = CdDiscInfo.DriveTitle(Root);
                return $"{drive} {(string.IsNullOrEmpty(title) ? "Audio CD" : title)} ({Tracks} {(Tracks == 1 ? "track" : "tracks")})";
            }
        }
    }

    internal static class CdPlaylist
    {
        /// <summary>The optical drives that hold an audio CD, in drive letter order.</summary>
        public static List<AudioCd> FindDiscs()
        {
            var found = new List<AudioCd>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType == DriveType.CDRom && drive.IsReady)
                    {
                        int tracks = Directory.EnumerateFiles(drive.RootDirectory.FullName, "*.cda").Count();
                        if (tracks > 0)
                        {
                            found.Add(new AudioCd(drive.RootDirectory.FullName, tracks));
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // A drive that is not ready or cannot be read: it holds no disc for us.
                }
            }
            return found;
        }

        /// <summary>A drive typed as "E:" or "E:\" that holds an audio CD becomes its playlist file (see <see cref="Write"/>).</summary>
        public static bool TryResolve(string text, out string playlist)
        {
            playlist = "";
            string trimmed = text.Trim().Trim('"');
            if (trimmed.Length < 2 || trimmed.Length > 3 || trimmed[1] != ':' || !char.IsLetter(trimmed[0]))
            {
                return false;
            }
            string root = char.ToUpperInvariant(trimmed[0]) + @":\";
            AudioCd? disc = FindDiscs().FirstOrDefault(d => string.Equals(d.Root, root, StringComparison.OrdinalIgnoreCase));
            if (disc == null)
            {
                return false;
            }
            string? file = Write(disc);
            if (file == null)
            {
                return false;
            }
            playlist = file;
            return true;
        }

        /// <summary>Writes the playlist of the disc (tracks in disc order) and returns its path; null if it could not be written.</summary>
        public static string? Write(AudioCd disc)
        {
            try
            {
                List<string> tracks = Directory.EnumerateFiles(disc.Root, "*.cda")
                    .OrderBy(CdDiscInfo.TrackNumber).ThenBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
                var text = new StringBuilder("#EXTM3U\r\n");
                foreach (string track in tracks)
                {
                    CdTrackInfo? info = CdDiscInfo.InfoOf(track);   // also starts the lookup of a new disc
                    string name = info == null ? $"Track {CdDiscInfo.TrackNumber(track)}"
                        : string.IsNullOrWhiteSpace(info.Artist) ? info.Title!.Trim() : $"{info.Artist.Trim()} - {info.Title!.Trim()}";
                    text.Append("#EXTINF:").Append((int)CdDiscInfo.TrackLength(track).TotalSeconds).Append(',').Append(name.Replace('\r', ' ').Replace('\n', ' ')).Append("\r\n");
                    text.Append(track).Append("\r\n");
                }

                string folder = Path.Combine(Path.GetTempPath(), "SweWolfSoftware", "SwewolfMediaPlayer");
                Directory.CreateDirectory(folder);
                string title = CdDiscInfo.DriveTitle(disc.Root) ?? $"Audio CD ({disc.Root.TrimEnd('\\', ':')})";
                foreach (char bad in Path.GetInvalidFileNameChars())
                {
                    title = title.Replace(bad, '_');
                }
                string file = Path.Combine(folder, title + ".m3u8");
                File.WriteAllText(file, text.ToString(), new UTF8Encoding(false));
                return file;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
