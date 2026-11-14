// MediaPlayerForm.Cd.cs: the artist, title and album of an audio CD track. LibVLC's CD reader looks the disc up in an online
// database (CDDB) by itself while the track plays; here the answer is picked up from the Media and shown like the tags of
// a music file (ShowTrackInfo). What was found is kept on this computer, so the next time the same disc is played there is
// no wait and no internet needed: %LocalAppData%\SweWolfSoftware\SwewolfMediaPlayer\CdInfo\<disc id>.json (a cache the
// program can recreate, hence Local). The disc id is the volume serial number Windows makes from the disc's track layout.
// The sibling File Explorer could read these files later to show song titles.
//
// History:
//   2026-11-13  First version: titles from LibVLC's own lookup, kept per disc.

using System.Runtime.InteropServices;
using System.Text.Json;
using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    internal sealed class CdTrackInfo
    {
        public string? Artist { get; set; }
        public string? Title { get; set; }
        public string? Album { get; set; }
    }

    internal sealed class CdDiscInfo
    {
        public Dictionary<string, CdTrackInfo> Tracks { get; set; } = new();
    }

    internal static class CdInfoStore
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeInformationW(string root, char[]? name, int nameSize, out uint serial, out uint maxComponent,
            out uint flags, char[]? fileSystem, int fileSystemSize);

        public static string? DiscId(string path)
        {
            try
            {
                string root = Path.GetPathRoot(Path.GetFullPath(path)) ?? "";
                return GetVolumeInformationW(root, null, 0, out uint serial, out _, out _, null, 0) && serial != 0 ? serial.ToString("X8") : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FileFor(string discId) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "CdInfo", discId + ".json");

        public static CdDiscInfo Load(string discId)
        {
            try
            {
                return JsonSerializer.Deserialize<CdDiscInfo>(File.ReadAllText(FileFor(discId))) ?? new CdDiscInfo();
            }
            catch (Exception)
            {
                return new CdDiscInfo();
            }
        }

        public static void SaveAll(string discId, CdDiscInfo found)
        {
            try
            {
                CdDiscInfo disc = Load(discId);
                foreach (var pair in found.Tracks)
                {
                    disc.Tracks[pair.Key] = pair.Value;
                }
                string file = FileFor(discId);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, JsonSerializer.Serialize(disc, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception)
            {
                // The cache is a convenience: no cache, a new lookup next time.
            }
        }
    }

    public partial class MediaPlayerForm
    {
        // Called when a CD track has started. Shows what is known at once (the cache, else "Track N"). Otherwise the whole disc is
        // read with a second Media (without a track number, so LibVLC makes one sub item per track, with the answer of the online lookup);
        // every track found is kept, so the other tracks of the disc are known the next time without asking again.
        private async Task ShowCdTrackInfoAsync(string path, Media media)
        {
            if (!TryGetCdTrack(path, out string? location, out int track) || _activeLib == null)
            {
                return;
            }

            string? discId = CdInfoStore.DiscId(path);
            if (discId != null && CdInfoStore.Load(discId).Tracks.TryGetValue(track.ToString(), out CdTrackInfo? cached))
            {
                ShowTrackInfo(cached.Artist, cached.Title, cached.Album);
                return;
            }

            ShowTrackInfo("Audio CD", $"Track {track}");
            try
            {
                // The preparser skips discs (Parse says Skipped), so the disc is "played" silently in a second player until its
                // sub items (one per track) are there, and the player is stopped again.
                Media disc = new(_activeLib, location!, FromType.FromLocation);
                disc.AddOption(":no-audio");
                disc.AddOption(":no-video");
                MediaPlayer reader = new(disc);
                reader.Mute = true;
                reader.Play();
                MediaList items = disc.SubItems;
                CdDiscInfo found = new();
                for (int wait = 0; wait < 60 && !IsDisposed; wait++)
                {
                    await Task.Delay(500);
                    if (items.Count > 0)
                    {
                        await Task.Delay(1500);   // the lookup answers a little after the tracks are listed
                        break;
                    }
                }
                for (int i = 0; i < items.Count; i++)
                {
                    using Media item = items[i];
                    if (ReadCdMeta(item, i + 1) is { } info)
                    {
                        found.Tracks[(i + 1).ToString()] = info;
                    }
                }
                _ = Task.Run(() => { reader.Stop(); reader.Dispose(); items.Dispose(); disc.Dispose(); });
                OpenTimingLog.Write($"CD info: {items.Count} tracks, {found.Tracks.Count} with a title");
                if (found.Tracks.Count > 0 && discId != null)
                {
                    CdInfoStore.SaveAll(discId, found);
                }
                if (found.Tracks.TryGetValue(track.ToString(), out CdTrackInfo? mine) && !IsDisposed && _media == media && _filePath == path)
                {
                    ShowTrackInfo(mine.Artist, mine.Title, mine.Album);
                }
            }
            catch (Exception ex)
            {
                OpenTimingLog.Write($"CD info failed: {ex.Message}");
            }
        }

        // The tags LibVLC has for the track, or null while it only knows the generic "Track N" name.
        private static CdTrackInfo? ReadCdMeta(Media media, int track)
        {
            try
            {
                string? title = media.Meta(MetadataType.Title);
                string? artist = media.Meta(MetadataType.Artist);
                string? album = media.Meta(MetadataType.Album);
                bool generic = string.IsNullOrWhiteSpace(title) || System.Text.RegularExpressions.Regex.IsMatch(title.Trim(), @"^(Track|Spår)\s*\d+$|^cdda:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (generic && string.IsNullOrWhiteSpace(artist) && string.IsNullOrWhiteSpace(album))
                {
                    return null;
                }
                // The database often has "Title - Artist" as the title: the artist has its own line.
                if (!generic && !string.IsNullOrWhiteSpace(artist) && title!.EndsWith(" - " + artist.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    title = title[..^(artist.Trim().Length + 3)];
                }
                return new CdTrackInfo { Artist = artist, Title = generic ? $"Track {track}" : title, Album = album };
            }
            catch (Exception)
            {
                return null;   // the Media was disposed meanwhile
            }
        }
    }
}
