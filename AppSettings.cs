// AppSettings.cs: the program's settings, kept in %AppData%\SweWolfSoftware\SwewolfMediaPlayer\settings.json.
//
// Self-contained: to reuse it in another app, change the namespace and AppFolder and replace the
// properties. A missing or damaged file gives the defaults, and a file that cannot be written is
// ignored (a setting that is not remembered is not worth an error message).
//
// History:
//   2026-10-13  First version: WhenVideoEnds.

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal sealed class AppSettings
    {
        private const string AppFolder = "SwewolfMediaPlayer";

        // What the player does when a video has played to its end: 0 = show the last frame,
        // 1 = show a black screen, 2 = repeat.
        public int WhenVideoEnds { get; set; }

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", AppFolder, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
