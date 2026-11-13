// FavoriteChannels.cs: the channels the user marked as favorites (the "Favorites" group of the channel list).
//
// The list holds stream addresses. It is kept in %AppData%\SweWolfSoftware\SwewolfMediaPlayer\favorite-channels.json (the user would miss it).
// Addresses that are not in the channel list that is open are simply not shown, so lists with different channels can share the file.
//
// History:
//   2026-11-13  First version.

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal static class FavoriteChannels
    {
        private static readonly object Gate = new();

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "favorite-channels.json");

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
