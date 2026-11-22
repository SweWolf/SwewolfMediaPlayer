// RecentFiles.cs: the media files the user opened most recently from the main window (Media > Recent Media Files).
//
// The list holds full paths, newest first, at most 9 (the menu numbers them 1 to 9). It is kept in
// %AppData%\SweWolfSoftware\SwewolfMediaPlayer\recent-files.json (the user would miss it). Only files are added: not web addresses or channel
// lists, not the throwaway playlist of an audio CD (it is in the Temp folder), and not a disc in a CD/DVD drive (the disc may be gone).
//
// History:
//   2026-11-22  First version.

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal static class RecentFiles
    {
        public const int Max = 9;
        private static readonly object Gate = new();

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "recent-files.json");

        // The paths, the newest first.
        public static List<string> All()
        {
            lock (Gate)
            {
                return Load();
            }
        }

        // Puts the file on top (moving it up when it is already there) and keeps the newest nine. Paths that do not qualify are ignored.
        public static void Add(string path)
        {
            if (!Qualifies(path))
            {
                return;
            }
            lock (Gate)
            {
                List<string> list = Load();
                list.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                list.Insert(0, path);
                if (list.Count > Max)
                {
                    list.RemoveRange(Max, list.Count - Max);
                }
                Save(list);
            }
        }

        public static void Clear()
        {
            lock (Gate)
            {
                Save(new List<string>());
            }
        }

        private static bool Qualifies(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !File.Exists(path))
                {
                    return false;
                }
                string full = Path.GetFullPath(path);
                string temp = Path.GetFullPath(Path.GetTempPath());
                if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase))
                {
                    return false;   // the playlist file of an audio CD, and other throwaway files
                }
                string? root = Path.GetPathRoot(full);
                return string.IsNullOrEmpty(root) || new DriveInfo(root).DriveType != DriveType.CDRom;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return false;
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
