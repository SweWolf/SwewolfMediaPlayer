// RecentChannels.cs: the channels the user watched most recently (the "Recently Watched" group of the channel list).
//
// Only channels that really started to play are added. The list holds the stream addresses, newest first, at most 10. It is kept in
// %AppData%\SweWolfSoftware\SwewolfMediaPlayer\recent-channels.json (the user would miss it). Addresses that are not in the channel list that is
// open are simply not shown, so lists with different channels can share the file.
//
// History:
//   2026-11-13  First version.

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal static class RecentChannels
    {
        public const int Max = 10;
        private static readonly object Gate = new();

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "recent-channels.json");

        // The stream addresses, the newest first.
        public static List<string> All()
        {
            lock (Gate)
            {
                return Load();
            }
        }

        // Puts the address on top (moving it up when it is already there) and keeps the newest ten.
        public static void Add(string url)
        {
            lock (Gate)
            {
                List<string> list = Load();
                list.RemoveAll(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase));
                list.Insert(0, url);
                if (list.Count > Max)
                {
                    list.RemoveRange(Max, list.Count - Max);
                }
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.WriteAllText(FilePath, JsonSerializer.Serialize(list));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The list is a convenience: not being able to save it is not worth a message.
                }
            }
        }

        private static List<string> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(FilePath)) ?? new List<string>();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Unreadable file: start again with an empty list.
            }
            return new List<string>();
        }
    }
}
