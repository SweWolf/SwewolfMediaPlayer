// FavoriteChannels.cs: the channels the user marked as favorites (the "Favorites" group of the channel list).
//
// Any channel of any list can be a favorite, so the whole entry is kept (name, address, group, logo, user agent), not only the address:
// the Favorites group of a list also shows the favorites that this list does not have. It is kept in
// %AppData%\SweWolfSoftware\SwewolfMediaPlayer\favorite-channels.json (the user would miss it).
// The first version kept only the addresses (a JSON list of strings); such a file is still read. Those entries have no name, so they are
// shown only by a list that has the address, as before.
//
// History:
//   2026-11-13  First version.
//   2026-11-26  Whole entries, so that a favorite from a playlist of the user's also shows in the other lists.

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
                return Load().Select(e => e.Url).ToList();
            }
        }

        // The favorites that have a name (those of the first version have none).
        public static List<M3uEntry> Entries()
        {
            lock (Gate)
            {
                return Load().Where(e => e.Name.Length > 0).ToList();
            }
        }

        public static bool Contains(string url)
        {
            return All().Any(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase));
        }

        // A favorite of the first version (no name) is replaced by the complete entry.
        public static void Add(M3uEntry entry)
        {
            lock (Gate)
            {
                List<M3uEntry> list = Load();
                list.RemoveAll(e => string.Equals(e.Url, entry.Url, StringComparison.OrdinalIgnoreCase));
                list.Add(entry);
                Save(list);
            }
        }

        public static void Remove(string url)
        {
            lock (Gate)
            {
                List<M3uEntry> list = Load();
                if (list.RemoveAll(e => string.Equals(e.Url, url, StringComparison.OrdinalIgnoreCase)) > 0)
                {
                    Save(list);
                }
            }
        }

        private static void Save(List<M3uEntry> list)
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

        private static List<M3uEntry> Load()
        {
            var result = new List<M3uEntry>();
            try
            {
                if (File.Exists(FilePath))
                {
                    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(FilePath));
                    foreach (JsonElement item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } url)
                        {
                            result.Add(new M3uEntry("", url, "", null, null));   // the first version: the address only
                        }
                        else if (item.ValueKind == JsonValueKind.Object && item.Deserialize<M3uEntry>() is { Url: not null } entry)
                        {
                            result.Add(entry with { Name = entry.Name ?? "", Group = entry.Group ?? "" });
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                // Unreadable file: start again with an empty list.
                return new List<M3uEntry>();
            }
            return result;
        }
    }
}
