// ChannelHistory.cs: when a channel last worked, and since when it has not (the tooltip of the channel list, the offer to hide a channel that keeps failing).
//
// One memory for all channel lists, by stream address (like Favorites and Hidden Channels), in
// %AppData%\SweWolfSoftware\SwewolfMediaPlayer\channel-history.json (the user would miss it). Per channel:
//   LastWorked    the last time it played (two checks in a row, see PlayChannelAsync)
//   FailingSince  the first failure after the last success; a success clears it
//   LastFailed    the latest failure
//   FailCount     the failures in a row since the last success
// A failure is only recorded while the computer is online (InternetCheck.cs), so being offline does not count against a channel.
//
// History:
//   2026-11-18  First version.

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal sealed class ChannelHistoryEntry
    {
        public DateTime? LastWorked { get; set; }
        public DateTime? FailingSince { get; set; }
        public DateTime? LastFailed { get; set; }
        public int FailCount { get; set; }
    }

    internal static class ChannelHistory
    {
        private static readonly object Gate = new();
        private static Dictionary<string, ChannelHistoryEntry>? _all;

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "channel-history.json");

        public static ChannelHistoryEntry? Get(string url)
        {
            lock (Gate)
            {
                return Load().TryGetValue(url, out ChannelHistoryEntry? entry) ? entry : null;
            }
        }

        public static void RecordWorked(string url)
        {
            lock (Gate)
            {
                Dictionary<string, ChannelHistoryEntry> all = Load();
                if (!all.TryGetValue(url, out ChannelHistoryEntry? entry))
                {
                    all[url] = entry = new ChannelHistoryEntry();
                }
                entry.LastWorked = DateTime.Now;
                entry.FailingSince = null;
                entry.FailCount = 0;
                Save(all);
            }
        }

        // Returns the entry as it is after this failure.
        public static ChannelHistoryEntry RecordFailed(string url)
        {
            lock (Gate)
            {
                Dictionary<string, ChannelHistoryEntry> all = Load();
                if (!all.TryGetValue(url, out ChannelHistoryEntry? entry))
                {
                    all[url] = entry = new ChannelHistoryEntry();
                }
                DateTime now = DateTime.Now;
                entry.FailingSince ??= now;
                entry.LastFailed = now;
                entry.FailCount++;
                Save(all);
                return entry;
            }
        }

        private static void Save(Dictionary<string, ChannelHistoryEntry> all)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(all));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The history is a convenience: not being able to save it is not worth a message.
            }
        }

        private static Dictionary<string, ChannelHistoryEntry> Load()
        {
            if (_all != null)
            {
                return _all;
            }
            try
            {
                if (File.Exists(FilePath))
                {
                    _all = new Dictionary<string, ChannelHistoryEntry>(
                        JsonSerializer.Deserialize<Dictionary<string, ChannelHistoryEntry>>(File.ReadAllText(FilePath)) ?? new(), StringComparer.OrdinalIgnoreCase);
                    return _all;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Unreadable file: start again with an empty history.
            }
            return _all = new Dictionary<string, ChannelHistoryEntry>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
