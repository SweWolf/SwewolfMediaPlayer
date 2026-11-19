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
//   2026-11-13  SaveResumePoint (two choices) replaced by ResumeAudioFiles (off by default); an old SaveResumePoint in settings.json is ignored.
//   2026-10-23  ShowTuneList (on by default).
//   2026-10-23  CoverPictureSize (Medium by default).
//   2026-10-26  ShowSubtitles (on by default): the default for videos with no choice of their own.
//   2026-10-27  TrackMenusOnlyWithChoice (off by default).
//   2026-11-08  ClassicTrackButtons (off by default = modern look).
//   2026-11-10  SaveFramesFolder (the Windows Pictures folder by default; blank = the folder of the video).
//   2026-11-10  MediaStartFolder (the Windows Videos folder by default; blank = the last folder used) and LastMediaFolder.
//   2026-11-12  ListSide (right by default): the side of the tune list and the channel list.
//   2026-11-19  PlaylistNoticeSeconds (10 by default): how long the message about a playlist item that cannot be played stays.

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

        // What happens when a VIDEO file is opened that has a saved resume point (audio files: ResumeAudioFiles): 0 = Yes (continue without asking),
        // 1 = Ask (the default), 2 = No (start from the beginning).
        public int ResumeWhenOpening { get; set; } = 1;

        // Video files always get a resume point (where you stopped, saved in the library database). Audio files (also a track
        // of an audio CD does not count, see PositionWanted) only when ResumeAudioFiles is on, and only when they are at least
        // ResumeAudioMinMinutes long (audio books, podcasts). Off by default: nobody wants to resume a song.
        public bool ResumeAudioFiles { get; set; }

        // What happens when an audio file with a saved position is opened (only when ResumeAudioFiles is on): 0 = Yes, 1 = Ask (the default).
        public int ResumeAudioMode { get; set; } = 1;
        public int ResumeAudioMinMinutes { get; set; } = 10;

        // The saved position is this many seconds before the point where the user stopped (0 = the exact point).
        public int ResumeRewindSeconds { get; set; } = 10;

        // How large the cover picture of a music file is shown: 0 = Medium (75 percent of what fits, as in TeraTool), 1 = Maximum.
        public int CoverPictureSize { get; set; }

        // Whether the list of tunes is shown on the right while a SID file with several tunes plays.
        public bool ShowTuneList { get; set; } = true;

        // Whether a video shows its subtitles when it is opened for the first time (Tools > Settings, "Turn On Subtitles When a Video Opens"; on by default).
        // What the user chose for a file in the right-click menu is remembered for that file and wins (the library, TrackChoice).
        public bool ShowSubtitles { get; set; } = true;

        // Whether the right-click menu shows Audio Track and Video Track only for a file with more than one of them, and Subtitle Track only for a
        // file with at least one (one is enough: the menu also turns subtitles off). Off by default: the menus are always there, greyed out when empty.
        public bool TrackMenusOnlyWithChoice { get; set; }

        // Look of the previous / next tune and track buttons: false = Modern (one triangle and a bar, the default), true = Classic (two triangles and a bar).
        public bool ClassicTrackButtons { get; set; }

        // The folder the Save Frame dialog (and the cover art dialog) starts in. The Windows Pictures folder until the user changes it;
        // blank means the folder of the video file. A folder the user picks in the dialog is used for that player window only.
        public string SaveFramesFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        // The folder the Browse button of the main window starts in (Open a Media File). The Windows Videos folder until the user changes
        // it; blank means the last folder a file was opened from (LastMediaFolder, kept between runs). A folder that does not exist is skipped.
        public string MediaStartFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        // The folder of the last file chosen with the Browse button: used when MediaStartFolder is blank.
        public string LastMediaFolder { get; set; } = "";

        // Which side of the window the tune list and the channel list are on: 0 = right (the default), 1 = left.
        public int ListSide { get; set; }

        // How many seconds the message about a playlist item that cannot be played stays before the next item starts (1 to 60, default 10).
        public int PlaylistNoticeSeconds { get; set; } = 10;

        // The folder of the last recording of a stream (the Record button): the Save As dialog starts there, and the .ts file is written there while
        // it records. The Windows Videos folder until the first recording.
        public string RecordFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

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
