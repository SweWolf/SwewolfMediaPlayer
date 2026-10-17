// AppSettings.cs: the program's settings, kept in %AppData%\SweWolfSoftware\SwewolfMediaPlayer\settings.json.
//
// Self-contained: to reuse it in another app, change the namespace and AppFolder and replace the
// properties. A missing or damaged file gives the defaults, and a file that cannot be written is
// ignored (a setting that is not remembered is not worth an error message).
//
// History:
//   2026-10-13  First version: WhenVideoEnds.
//   2026-10-15  StartFullScreen and CheckForUpdatesOnStartup (both on by default).
//   2026-10-16  StartFullScreen renamed StartFullScreenForVideo: audio files never open in full screen
//               (an old settings.json value is ignored, the default is on).
//   2026-10-16  ShowSeekBarThumbnails (on by default).
//   2026-10-16  SaveResumePoint and ResumeAudioMinMinutes (resume points in the shared library database).
//   2026-10-16  ResumeWhenOpening (Yes / Ask / No); SaveResumePoint has two values now (the old "none" is the new No).
//   2026-10-16  ResumeRewindSeconds (default 10).

using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal sealed class AppSettings
    {
        private const string AppFolder = "SwewolfMediaPlayer";

        // What the player does when a video has played to its end: 0 = show the last frame,
        // 1 = show a black screen, 2 = repeat.
        public int WhenVideoEnds { get; set; }

        // Whether the player opens video files in full screen mode (audio files never do).
        public bool StartFullScreenForVideo { get; set; } = true;

        // Whether a small picture of the video pops up while the pointer is on the seek bar.
        public bool ShowSeekBarThumbnails { get; set; } = true;

        // What happens when a file is opened that has a saved resume point: 0 = Yes (continue without asking),
        // 1 = Ask (the default), 2 = No (start from the beginning).
        public int ResumeWhenOpening { get; set; } = 1;

        // Which files get a resume point (where you stopped, saved in the library database): 0 = video and audio
        // files, 1 = video files, and audio files of at least ResumeAudioMinMinutes (the default).
        public int SaveResumePoint { get; set; } = 1;
        public int ResumeAudioMinMinutes { get; set; } = 10;

        // The saved position is this many seconds before the point where the user stopped (0 = the exact point).
        public int ResumeRewindSeconds { get; set; } = 10;

        // Whether the main window asks GitHub for a newer version when it starts.
        public bool CheckForUpdatesOnStartup { get; set; } = true;

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
