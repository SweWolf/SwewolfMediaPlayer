// ChannelMaster.cs: keeps the channels that the owner of a downloaded channel list has removed (the public IPTV list changes now and then,
// and a removed channel may still work).
//
// The list that is shown is the list on the web (new channels, changed names and groups come from it) plus the "gone" channels: those that
// were in an earlier version of the list but are not any more. A gone channel keeps the name and group it had last, and the date it was
// first missed. It is kept in
//   %AppData%\SweWolfSoftware\SwewolfMediaPlayer\Playlists\<hash of the address>.gone.json   (Roaming: the user would miss it)
// A gone channel is forgotten when it has been gone for 14 days or more and has also failed for 14 days or more (see ChannelHistory): nothing
// speaks for it any more. A channel that comes back in the web list stops being "gone". Reset deletes the file: the list is then the
// web list alone.
//
// Self-contained apart from M3uPlaylist and ChannelHistory. To reuse it, change the namespace and AppFolder.
//
// History:
//   2026-11-22  First version.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal sealed class GoneChannel
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Group { get; set; } = "";
        public string? LogoUrl { get; set; }
        public string? UserAgent { get; set; }
        public DateTime GoneSince { get; set; }
    }

    internal static class ChannelMaster
    {
        private const string AppFolder = "SwewolfMediaPlayer";
        private static readonly TimeSpan Forget = TimeSpan.FromDays(14);

        private static string FilePath(string listUrl)
        {
            string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(listUrl.Trim()))).ToLowerInvariant();
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SweWolfSoftware", AppFolder, "Playlists", hash + ".gone.json");
        }

        // The text of the web list with the gone channels added at the end. A text that is not a list of channels is returned as it is.
        public static string Merge(string listUrl, string webText)
        {
            if (!M3uPlaylist.LooksLikeChannelList(webText))
            {
                return webText;
            }
            List<M3uEntry> web = M3uPlaylist.Parse(webText);
            if (web.Count == 0)
            {
                return webText;   // an empty or odd answer must not make every channel "gone"
            }

            string path = FilePath(listUrl);
            Dictionary<string, GoneChannel> gone = Load(path);
            DateTime now = DateTime.Now;
            var inWeb = new HashSet<string>(web.Select(e => e.Url), StringComparer.OrdinalIgnoreCase);

            // The channels the web list has now are the ones to remember in case they go: name and group as they are today.
            // (The first time, and each time, the list on disk is compared with the web list before it is replaced.)
            string before = FilePath(listUrl) + ".last";
            List<M3uEntry> last = LoadLast(before);
            foreach (M3uEntry e in last)
            {
                if (!inWeb.Contains(e.Url) && !gone.ContainsKey(e.Url))
                {
                    gone[e.Url] = new GoneChannel { Name = e.Name, Url = e.Url, Group = e.Group, LogoUrl = e.LogoUrl, UserAgent = e.UserAgent, GoneSince = now };
                }
            }
            foreach (string url in inWeb)
            {
                gone.Remove(url);   // it is back (or never left)
            }
            foreach (GoneChannel g in gone.Values.ToList())
            {
                ChannelHistoryEntry? h = ChannelHistory.Get(g.Url);
                if (g.GoneSince <= now - Forget && h?.FailingSince is DateTime since && since <= now - Forget)
                {
                    gone.Remove(g.Url);
                }
            }
            Save(before, web);
            Save(path, gone);

            if (gone.Count == 0)
            {
                return webText;
            }
            var text = new StringBuilder(webText);
            if (!webText.EndsWith('\n'))
            {
                text.Append('\n');
            }
            foreach (GoneChannel g in gone.Values)
            {
                text.Append("#EXTINF:-1");
                if (g.LogoUrl != null)
                {
                    text.Append(" tvg-logo=\"").Append(Clean(g.LogoUrl)).Append('"');
                }
                if (g.UserAgent != null)
                {
                    text.Append(" http-user-agent=\"").Append(Clean(g.UserAgent)).Append('"');
                }
                text.Append(" group-title=\"").Append(Clean(g.Group)).Append("\",").Append(g.Name.Replace('\n', ' ').Replace('\r', ' ')).Append('\n');
                text.Append(g.Url).Append('\n');
            }
            return text.ToString();
        }

        // Forgets the gone channels of this list: the list is the web list alone from the next load.
        public static void Reset(string listUrl)
        {
            foreach (string path in new[] { FilePath(listUrl), FilePath(listUrl) + ".last" })
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Nothing to do: the file is then used as it is.
                }
            }
        }

        private static string Clean(string value) => value.Replace('"', '\'').Replace('\n', ' ').Replace('\r', ' ');

        private static Dictionary<string, GoneChannel> Load(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    List<GoneChannel>? list = JsonSerializer.Deserialize<List<GoneChannel>>(File.ReadAllText(path));
                    var map = new Dictionary<string, GoneChannel>(StringComparer.OrdinalIgnoreCase);
                    foreach (GoneChannel g in list ?? new())
                    {
                        map[g.Url] = g;
                    }
                    return map;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Unreadable: start again with none.
            }
            return new Dictionary<string, GoneChannel>(StringComparer.OrdinalIgnoreCase);
        }

        private static List<M3uEntry> LoadLast(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return JsonSerializer.Deserialize<List<GoneChannel>>(File.ReadAllText(path))?
                        .Select(g => new M3uEntry(g.Name, g.Url, g.Group, g.LogoUrl, g.UserAgent)).ToList() ?? new();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Unreadable: nothing is known about the earlier list.
            }
            return new List<M3uEntry>();
        }

        private static void Save(string path, Dictionary<string, GoneChannel> gone) => Write(path, gone.Values.ToList());

        private static void Save(string path, List<M3uEntry> web) => Write(path, web.Select(e => new GoneChannel
        {
            Name = e.Name, Url = e.Url, Group = e.Group, LogoUrl = e.LogoUrl, UserAgent = e.UserAgent,
        }).ToList());

        private static void Write(string path, List<GoneChannel> list)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(list));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The memory of gone channels is a convenience; it is tried again at the next load.
            }
        }
    }
}
