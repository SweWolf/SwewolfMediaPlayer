// HiddenChannels.cs: the channels the user hid (they are left out of the channel list; the "Hidden Channels" group shows them again).
//
// The list holds stream addresses. It is kept in %AppData%\SweWolfSoftware\SwewolfMediaPlayer\hidden-channels.json (the user would miss it).
// Addresses that are not in the channel list that is open are simply not shown, so lists with different channels can share the file.
//
// History:
//   2026-11-16  First version.
//   2026-11-19  RemoveMany (Check Channels: unhide the hidden channels that work again).

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal static class HiddenChannels
    {
        private static readonly object Gate = new();

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "hidden-channels.json");

        public static List<string> All()
        {
            lock (Gate)
            {
                return Load();
            }
        }

        public static bool Contains(string url)
        {
            return All().Any(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase));
        }

        public static void Add(string url)
        {
            lock (Gate)
            {
                List<string> list = Load();
                if (!list.Any(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(url);
                    Save(list);
                }
            }
        }

        public static void AddMany(IEnumerable<string> urls)
        {
            lock (Gate)
            {
                List<string> list = Load();
                var have = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                bool changed = false;
                foreach (string url in urls)
                {
                    if (have.Add(url))
                    {
                        list.Add(url);
                        changed = true;
                    }
                }
                if (changed)
                {
                    Save(list);
                }
            }
        }

        public static void Remove(string url)
        {
            lock (Gate)
            {
                List<string> list = Load();
                if (list.RemoveAll(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase)) > 0)
                {
                    Save(list);
                }
            }
        }

        public static void RemoveMany(IEnumerable<string> urls)
        {
            lock (Gate)
            {
                var gone = new HashSet<string>(urls, StringComparer.OrdinalIgnoreCase);
                List<string> list = Load();
                if (list.RemoveAll(gone.Contains) > 0)
                {
                    Save(list);
                }
            }
        }

        private static void Save(List<string> list)
        {
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
