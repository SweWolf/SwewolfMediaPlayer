// RecordingStore.cs: the list of the recordings the player has made ("My Recordings" menu and the All Recordings window).
//
// The list is kept in %AppData%\SweWolfSoftware\SwewolfMediaPlayer\recordings.json (the user would miss it). Each entry has the path of the MP4 file,
// the channel, the stream address and the channel list it came from, when it was recorded, its length and its size.
//
// A recording that was renamed or moved is found again through the shared library (LibraryStore): when a recording is saved the player writes the
// stream address into the library as "where the file came from" (ItemInfo.HostUrl, the channel list address as ReferrerUrl), and the explorer
// carries what the library remembers about a file along when it renames or moves the file there. So Locate asks the library for the files that came
// from the same stream address, and picks the one that no other entry owns (the one of the right size first). Nothing is added to the library's
// tables, and LibraryStore.cs is used as it is (only its public Resolve / Open / Command).
//
// History:
//   2026-11-12  First version.

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal sealed class RecordingEntry
    {
        public Guid Id { get; set; }
        public string Path { get; set; } = "";
        public string Channel { get; set; } = "";
        public string? Url { get; set; }        // the stream address
        public string? ListUrl { get; set; }    // the channel list it was chosen from, when that was a web address
        public DateTime RecordedAt { get; set; }
        public long DurationMs { get; set; }
        public long SizeBytes { get; set; }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
    }

    internal static class RecordingStore
    {
        private const string AppFolder = "SwewolfMediaPlayer";
        private static readonly object Gate = new();
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        /// <summary>Only for tests: use this file instead of the user's list.</summary>
        public static string? FileOverride { get; set; }

        private static string FilePath => FileOverride ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", AppFolder, "recordings.json");

        // Every entry, the newest first.
        public static List<RecordingEntry> All()
        {
            lock (Gate)
            {
                return Load().OrderByDescending(e => e.RecordedAt).ToList();
            }
        }

        public static bool Any()
        {
            lock (Gate)
            {
                return Load().Count > 0;
            }
        }

        public static void Add(RecordingEntry entry)
        {
            lock (Gate)
            {
                List<RecordingEntry> list = Load();
                if (entry.Id == Guid.Empty)
                {
                    entry.Id = Guid.NewGuid();
                }
                list.Add(entry);
                Save(list);
            }
        }

        public static void Remove(Guid id)
        {
            lock (Gate)
            {
                List<RecordingEntry> list = Load();
                if (list.RemoveAll(e => e.Id == id) > 0)
                {
                    Save(list);
                }
            }
        }

        // Forgets the list. The recording files are not touched.
        public static void Clear()
        {
            lock (Gate)
            {
                Save(new List<RecordingEntry>());
            }
        }

        public static void SetPath(Guid id, string path, long sizeBytes)
        {
            lock (Gate)
            {
                List<RecordingEntry> list = Load();
                RecordingEntry? entry = list.FirstOrDefault(e => e.Id == id);
                if (entry != null)
                {
                    entry.Path = path;
                    entry.SizeBytes = sizeBytes;
                    Save(list);
                }
            }
        }

        // The file of the recording as it is now: its own path, or else where the library says a file from the same stream is now. Null when
        // it is nowhere to be found. A new place is written to the list.
        public static string? Locate(RecordingEntry entry)
        {
            if (File.Exists(entry.Path))
            {
                return entry.Path;
            }
            if (string.IsNullOrEmpty(entry.Url))
            {
                return null;
            }

            // The files that other entries own do not count (several recordings of one channel have the same address).
            // (Paths are compared in their full form: the library and the file dialogs do not always write a path the same way.)
            HashSet<string> owned = All().Where(e => e.Id != entry.Id && File.Exists(e.Path)).Select(e => Full(e.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            string own = Full(entry.Path);
            List<string> candidates = FindInLibrary(entry.Url, entry.Path).Select(Full)
                .Where(p => !owned.Contains(p) && !string.Equals(p, own, StringComparison.OrdinalIgnoreCase) && File.Exists(p))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            string? found = candidates.FirstOrDefault(p => entry.SizeBytes > 0 && new FileInfo(p).Length == entry.SizeBytes)
                ?? (candidates.Count == 1 ? candidates[0] : null);
            if (found != null)
            {
                SetPath(entry.Id, found, new FileInfo(found).Length);
                entry.Path = found;
            }
            return found;
        }

        private static string Full(string path)
        {
            try
            {
                return System.IO.Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or System.IO.PathTooLongException)
            {
                return path;
            }
        }

        // The files the library remembers as coming from this stream address (ItemInfo.HostUrl), in the library file of the drive the recording was on
        // and in the main file (which holds the other fixed drives). Paths are made from the keys the way LibraryStore.KeyOf writes them.
        private static List<string> FindInLibrary(string url, string hintPath)
        {
            var result = new List<string>();
            try
            {
                var routes = new List<LibraryRoute>();
                foreach (string probe in new[] { hintPath, System.IO.Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\" })
                {
                    LibraryRoute? route = string.IsNullOrEmpty(probe) ? null : LibraryStore.Resolve(probe, create: false);
                    if (route != null && !routes.Any(r => r.DbFile == route.DbFile && r.Prefix == route.Prefix))
                    {
                        routes.Add(route);
                    }
                }

                lock (LibraryStore.Gate)
                {
                    foreach (LibraryRoute route in routes)
                    {
                        using var connection = LibraryStore.Open(route, create: false);
                        if (connection == null)
                        {
                            continue;
                        }
                        using var command = LibraryStore.Command(connection, "SELECT Folder, Name FROM ItemInfo WHERE HostUrl = $h", ("$h", url));
                        using var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            string? path = ToPath(route, reader.GetString(0), reader.GetString(1));
                            if (path != null)
                            {
                                result.Add(path);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // A library that cannot be read only means that nothing is found by it.
            }
            return result;
        }

        private static string? ToPath(LibraryRoute route, string folderKey, string name)
        {
            if (!route.Relative)
            {
                return System.IO.Path.IsPathRooted(folderKey) ? System.IO.Path.Combine(folderKey, name) : null;
            }
            if (!folderKey.StartsWith(route.Prefix, StringComparison.Ordinal))
            {
                return null;
            }
            return route.Root.TrimEnd('\\', '/') + folderKey[route.Prefix.Length..] + "\\" + name;
        }

        private static List<RecordingEntry> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonSerializer.Deserialize<List<RecordingEntry>>(File.ReadAllText(FilePath)) ?? new List<RecordingEntry>();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
            }
            return new List<RecordingEntry>();
        }

        private static void Save(List<RecordingEntry> list)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath)!);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(list, Json));
                File.Move(temp, FilePath, overwrite: true);   // never a half-written list
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
