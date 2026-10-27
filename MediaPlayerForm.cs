// MediaPlayerForm: a stand-alone window that plays one media file with LibVLC.
// It has play/pause, stop, a seek bar with the elapsed and total time, mute and volume.
//
// To reuse it in another project:
//   - Copy MediaPlayerForm.cs, MediaPlayerForm.Trim.cs, MediaPlayerForm.Designer.cs and
//     MediaPlayerForm.resx.
//   - Change the namespace.
//   - Add the NuGet packages LibVLCSharp.WinForms, VideoLAN.LibVLC.Windows and TagLibSharp.
//   - Use: new MediaPlayerForm(path).Show();
//
// History:
//   2026-10-07  First version: plays a file.
//   2026-10-07  Added the control bar and the Space key for play/pause.
//   2026-10-07  Added full screen (F, Esc); fixed the Mute text and the time label while paused.
//   2026-10-07  Added frame stepping while paused (wheel over the time label, Left/Right keys)
//               and fractions of a second in the time label while paused.
//   2026-10-07  Frame stepping with the wheel works anywhere in the window (not over the
//               sliders), wheel up = forward; removed the time label tooltip.
//   2026-10-07  Added Save Frame (button and S key): saves the frame on screen as a PNG.
//   2026-10-07  Clicking the seek bar or volume slider jumps to that position (JumpTrackBar,
//               at the end of this file); C toggles the subtitles.
//   2026-10-08  The buttons show icons (drawn by IconButton, at the end of this file) instead of
//               text; the text is in tooltips. Dark colors. Right-click menu with the audio
//               and subtitle track. LibVLC's video window does not pass mouse clicks on, so a
//               timer watches the right button (CheckRightClick). The play/pause icon shows
//               the pause bars show while playing.
//   2026-10-08  Mute and the volume slider work together (see SetMuted).
//   2026-10-08  Double-clicking the video toggles full screen.
//   2026-10-09  Plays streams: a web address (http, https, rtsp, rtmp, mms) can be given
//               instead of a file path. HLS streams that start with a black picture (video
//               group without an address) are fixed automatically (EnsureVideoTrack).
//   2026-10-11  The progress bar stays at the end when a video ends. Save Frame at the end takes the
//               very last frame with FFmpeg (SaveLastFrameWithFfmpegAsync), if it is available.
//   2026-10-11  A video that has played to its end stays paused on its last frame (OnEndReached),
//               so Save Frame works and Play starts again from the beginning. Save Frame on a
//               stopped player says so. LibVLC cannot show the last 1-2 frames of a file.
//   2026-10-09  The pointer is hidden over the video after 3 seconds without movement or clicks.
//   2026-10-09  LibVLC is loaded on a worker thread (PreloadAsync), so the window does not freeze.
//               Alt+Enter toggles full screen like F.
//   2026-10-09  Clicking the video toggled play/pause for a while; removed again. The right-click
//               menu moves to the place of a new right click and closes on a left click elsewhere.
//   2026-10-09  The volume slider shows the volume in percent in a tooltip.
//   2026-10-09  Streams: the time label shows only the elapsed time, the seek bar and frame
//               stepping are off. The muted icon is red.
//   2026-10-11  Trim feature (MediaPlayerForm.Trim.cs): a group box, shown from a right-click menu on
//               the control bar, trims the file in place with SplitMediaFiles.
//   2026-10-12  Dragging the seek bar while the video is stopped is remembered (_pendingSeekMs): Play
//               starts from there, and it no longer jumps back to 0.
//   2026-10-12  The volume and mute state are set again once a file is playing (ApplyAudioState),
//               because LibVLC could ignore them before that and leave the sound off.
//   2026-10-12  A file dropped anywhere on the window is played; the Trim fields are cleared.
//   2026-10-12  Audio files show their embedded cover art (ShowCoverArt, picCover) instead of a
//               black screen. Needs the NuGet package TagLibSharp.
//   2026-10-12  Save Frame saves the cover art (SaveCoverArt) when an audio file shows it.
//   2026-10-16  Full screen at start is for video files only (IsAudioFile, setting StartFullScreenForVideo).
//   2026-10-16  Thumbnail picture above the seek bar while pointing at it (MediaPlayerForm.Thumbnails.cs,
//               FrameThumbnailer.cs; needs FFmpeg).
//   2026-10-16  Visualizations for audio files (MediaPlayerForm.Visualization.cs).
//   2026-10-16  Escape leaves full screen, then hides the group boxes, then turns off a visualization, then closes the window.
//   2026-10-16  The pointer is hidden again after a change to or from full screen (it stayed visible before).
//   2026-10-16  A "More Options" button (three dots, right end of the control bar) opens the control bar menu.
//   2026-10-16  The Save Frame button is only shown when there is a picture (video, or audio with cover art).
//   2026-10-16  Files with several tunes (SID): buttons, Page Up/Down and a menu to switch tune (MediaPlayerForm.Tunes.cs).
//   2026-10-16  CanSeek: SID files and files LibVLC cannot seek in get a read-only seek bar, no frame
//               stepping (keys, wheel), no media-key jumps and no trimming.
//   2026-10-16  SID files show their length from the song length database (SidSongLengths.cs).
//   2026-10-16  SID files: the plugin is copied from the user's VLC the first time (see
//               MediaPlayerForm.SidSupport.cs and SidPluginSetup.cs); PreloadAsync sets the plugin path.
//   2026-10-16  Fixed: while the control bar was hidden (full screen) the seek bar was laid out under the Save Frame button,
//               and S did not save a frame (the Visible getter is false then): _saveFrameShown, _tuneButtonsShown.
//   2026-10-16  Read marks and resume points in the library shared with SweWolf File Explorer
//               (MediaPlayerForm.Library.cs, LibraryStore.cs); the constructor takes an optional library path.
//   2026-10-23  Music files show artist, title and album in the top left corner (MediaPlayerForm.TrackInfo.cs).
//   2026-10-23  A music file without cover art of its own shows the picture of its folder (FolderCoverFinder.cs, the explorer's rules).
//   2026-10-23  SID files: tune names from HVSC's STIL (SidTuneTitles.cs) and a tune list on the right (MediaPlayerForm.TuneList.cs).
//   2026-10-25  Chapters: previous / next buttons, keys P and N, a Chapters menu; only for files that have chapters (MediaPlayerForm.Chapters.cs).
//   2026-10-25  Icons: chapters have a bar and one triangle (as in VLC); previous / next tune and track have a bar and two triangles.
//   2026-10-26  Playback speed box left of the time, keys + - Backspace (SpeedBox.cs, MediaPlayerForm.Speed.cs); the seek bar is 30 px shorter.
//   2026-10-26  The audio and subtitle track chosen for a file are remembered in the library and used the next time (MediaPlayerForm.Tracks.cs);
//               "Turn On Subtitles When a Video Opens" in Tools > Settings decides for files with no choice of their own.
//   2026-10-27  Setting "Show Track Menus Only When There Is a Choice": the track menus of the right-click menu are left out when there is nothing to choose.
//   2026-10-27  Right-click menu: Video Track (files with several video streams), between Audio Track and Subtitle Track as in VLC.

using System.ComponentModel;
using System.Drawing.Drawing2D;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm : Form, IMessageFilter
    {
        // Caption for message boxes. Change it when copying the form to another project.
        private const string AppName = "SweWolf Media Player";

        // LibVLC is loaded once per process and shared by all player windows.
        private static LibVLC? _libVlc;
        private static Task? _loadTask;

        // Loads LibVLC on a worker thread: it takes several seconds, and on the UI thread the
        // window would freeze meanwhile. Safe to call more than once; call it early (the main
        // window does) and the first player window opens without any wait.
        public static Task PreloadAsync()
        {
            return _loadTask ??= Task.Run(() =>
            {
                OpenTimingLog.Write("LibVLC load: start");
                var watch = System.Diagnostics.Stopwatch.StartNew();
                SidPluginSetup.ConfigurePluginPath();
                Core.Initialize();
                OpenTimingLog.Write($"LibVLC load: Core.Initialize done after {watch.ElapsedMilliseconds} ms");
                _libVlc = new LibVLC();
                OpenTimingLog.Write($"LibVLC load: engine created, total {watch.ElapsedMilliseconds} ms");
            });
        }

        private string _filePath;   // changes when another file is dropped on the window
        private bool _isStream;   // a web address (e.g. an HLS stream) and not a file
        private MediaPlayer? _player;
        private Media? _media;
        private System.Diagnostics.Stopwatch? _openWatch;   // timing log: runs from Play until the first time update
        private long _lastUiTickTimestamp;   // timing log: when the 30 ms timer last ticked
        private long _lengthMs;
        private bool _vlcSeekable = true;   // LibVLC's own answer: false for a file it cannot jump in
        private long _knownLengthMs;   // SID files: from the song length database, as LibVLC does not know it
        private bool _seeking;
        private string? _lastSaveFolder;
        private int _lastSubtitleTrack = -1;
        private bool _paused;
        private bool _restoringEnd;   // true while OnEndReached starts the video again to show its last frame
        private long _stepTimeMs = -1;   // tracked position while stepping frames, -1 = not stepping
        // Where the seek bar was dragged while the player was stopped, -1 = nowhere. A stopped
        // player ignores seeks, so Play starts from here instead (see RestartPlayback).
        private long _pendingSeekMs = -1;
        private int _audioStateApplied;   // see ApplyAudioState
        private double _frameMs;         // length of one frame, 0 = not known yet
        private bool _muted;
        private System.Windows.Forms.Timer? _rightClickTimer;
        private bool _rightButtonWasDown;
        private bool _shownPlaying;
        // The muted symbol is red. There is no setting for it yet: a setting would only have to
        // change this value.
        private bool _mutedSymbolIsRed = true;
        private bool _videoCheckDone;
        private long _playingSinceTicks;
        private bool _leftButtonWasDown;
        private long _lastClickTicks;
        private Point _lastClickPosition;

        // Double-clicking the video toggles full screen. There is no setting for it yet.
        private bool _doubleClickTogglesFullScreen = true;
        private bool _menuWasOpen;

        // The mouse pointer is hidden over the video after it has been still for a while. There
        // is no setting for it yet: a setting would only have to change these two values.
        private bool _hideCursorWhenIdle = true;
        private const int CursorHideDelayMs = 3000;
        private static Cursor? _blankCursor;
        private bool _cursorHidden;
        // Starts at the current position, so the pointer is hidden from the start until it moves or clicks.
        private Point _lastCursorPosition = Cursor.Position;
        private long _lastCursorActivityTicks;
        private bool _fullScreen;
        private FormWindowState _windowStateBeforeFullScreen;

        // True for a web address or other stream location that LibVLC can open, as opposed to a
        // file on disk. A path such as C:\video.mp4 is not a stream.
        public static bool TryGetStreamUri(string text, out Uri? uri)
        {
            string[] schemes = { "http", "https", "rtsp", "rtmp", "mms" };
            if (Uri.TryCreate(text.Trim(), UriKind.Absolute, out uri) && schemes.Contains(uri.Scheme))
            {
                return true;
            }
            uri = null;
            return false;
        }

        // libraryPath: what the file is called in the library (read marks and resume points, MediaPlayerForm.Library.cs).
        // It differs from filePath when the explorer has unpacked the file from an archive to play it.
        public MediaPlayerForm(string filePath, string? libraryPath = null)
        {
            InitializeComponent();
            InitControlPanel();
            InitTuneControls();
            InitBookmarkButton();
            InitChapterControls();
            InitSpeedBox();
            StyleMenu(menuVideo);
            _filePath = filePath;
            _libraryPath = libraryPath ?? filePath;
            _isStream = TryGetStreamUri(filePath, out Uri? uri);
            Text = _isStream ? uri!.Host : Path.GetFileName(filePath);
            // A stream cannot be seeked or stepped back: the seek bar is of no use.
            ApplySeekState();
            ShowLoading("Loading the video engine...");
        }

        // The first start after a while can take 20 seconds or more (Windows Defender scans the LibVLC files),
        // so the video area says what is going on until the file plays. There is no setting for it yet.
        private Label? _loadingLabel;

        private void ShowLoading(string text)
        {
            if (_loadingLabel == null)
            {
                _loadingLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black,
                    ForeColor = Color.Gainsboro,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(Font.FontFamily, Font.Size * 1.4f),
                    UseMnemonic = false,
                };
                Controls.Add(_loadingLabel);
            }
            _loadingText = text;
            ApplyLoadingText();
            _loadingLabel.Visible = true;
            _loadingLabel.BringToFront();

            // Still waiting after a few seconds: say why (Windows checks the new files the first time).
            if (_slowLoadTimer == null && !_slowLoadHint)
            {
                _slowLoadTimer = new System.Windows.Forms.Timer { Interval = SlowLoadHintDelayMs };
                _slowLoadTimer.Tick += (s, a) =>
                {
                    _slowLoadTimer?.Stop();
                    _slowLoadHint = true;
                    ApplyLoadingText();
                };
                _slowLoadTimer.Start();
            }
        }

        private const int SlowLoadHintDelayMs = 5000;
        private System.Windows.Forms.Timer? _slowLoadTimer;
        private bool _slowLoadHint;
        private string _loadingText = "";

        private void ApplyLoadingText()
        {
            if (_loadingLabel != null)
            {
                _loadingLabel.Text = _slowLoadHint
                    ? _loadingText + "\n\nStarting is taking longer than usual.\nYour security software is probably checking the program files. This is normal after an update,\nand the next start is usually much faster."
                    : _loadingText;
            }
        }

        private void HideLoading()
        {
            _slowLoadTimer?.Stop();
            _slowLoadTimer?.Dispose();
            _slowLoadTimer = null;
            _slowLoadHint = false;
            if (_loadingLabel != null)
            {
                _loadingLabel.Visible = false;
            }
        }

        // A file dragged from Windows Explorer can be dropped anywhere in the window and is played
        // instead of the current one. The Trim fields are cleared: their times belonged to the old file.
        private void MediaPlayerForm_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = !_trimming && e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private async void MediaPlayerForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (_player == null || _trimming || _restoringEnd
                || e.Data?.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files || !File.Exists(files[0]))
            {
                return;
            }

            await OpenAnotherAsync(files[0], files[0]);
        }

        // Plays another file in this window: a file dropped on it, or one the explorer sends through the pipe (MediaPlayerForm.Pipe.cs).
        internal async Task OpenAnotherAsync(string path, string libraryPath)
        {
            MediaPlayer? player = _player;
            if (player == null || _trimming || _restoringEnd)
            {
                return;
            }

            EndPreview(false);
            Media? oldMedia = _media;
            _media = null;
            _filePath = path;
            _libraryPath = libraryPath;
            _isStream = TryGetStreamUri(path, out Uri? uri);
            Text = _isStream ? uri!.Host : Path.GetFileName(path);
            _vlcSeekable = true;
            ApplySeekState();
            txtStartAt.Clear();
            txtEndAt.Clear();
            txtFadeIn.Text = "0";
            txtFadeOut.Text = "0";
            _lengthMs = 0;
            _frameMs = 0;
            _pendingSeekMs = -1;
            _lastSubtitleTrack = -1;
            _videoCheckDone = false;
            _playingSinceTicks = 0;
            await Task.Run(() =>
            {
                player.Stop();
                oldMedia?.Dispose();
            });
            if (_player == player && !IsDisposed && await EnsureSidSupportAsync(false))
            {
                StartMedia();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // The window may be made much smaller than the designer size, but not so small
            // that the control bar no longer fits.
            UpdateMinimumSize();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            OpenTimingLog.Write($"Player window shown, preload {(_loadTask == null ? "NOT started yet" : _loadTask.IsCompleted ? "already done" : "still running")}");

            if (_settings.StartFullScreenForVideo && !IsAudioFile(_filePath))
            {
                SetFullScreen(true);
            }

            try
            {
                await PreloadAsync();
            }
            catch (Exception ex)
            {
                _loadTask = null;   // allow a new try the next time
                if (!IsDisposed)
                {
                    HideLoading();
                    MessageBox.Show(this, $"The video engine (LibVLC) could not be loaded.\n\n{ex.Message}", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
                return;
            }

            // The window may have been closed while LibVLC was loading.
            if (IsDisposed || _libVlc == null)
            {
                return;
            }

            // A SID file needs a plugin from VLC the first time (SidPluginSetup).
            if (!await EnsureSidSupportAsync(true))
            {
                if (!IsDisposed)
                {
                    Close();
                }
                return;
            }
            if (IsDisposed || _libVlc == null)
            {
                return;
            }

            // A visualization needs an engine of its own (see MediaPlayerForm.Visualization.cs).
            LibVLC engine = _libVlc!;
            if (ActiveVisualization() != null)
            {
                engine = _ownLibVlc = await CreateOwnEngineAsync() ?? engine;
                if (IsDisposed)
                {
                    DisposeOwnEngine();
                    return;
                }
            }
            OpenTimingLog.Write("Engine and SID check ready, attaching the player");
            ShowLoading("Opening the file...");
            AttachPlayer(engine);
            Application.AddMessageFilter(this);

            // The video window of LibVLC does not pass mouse clicks on to the form, so watch the
            // right mouse button ourselves and open the menu when it is released over the video.
            _rightClickTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _rightClickTimer.Tick += (s, a) =>
            {
                // This timer ticks every 30 ms, so a long gap means the UI thread was busy (window not responding).
                long nowTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                if (_lastUiTickTimestamp != 0)
                {
                    long gapMs = (nowTicks - _lastUiTickTimestamp) * 1000 / System.Diagnostics.Stopwatch.Frequency;
                    if (gapMs >= 500)
                    {
                        OpenTimingLog.Write($"UI thread was blocked for {gapMs} ms");
                    }
                }
                _lastUiTickTimestamp = nowTicks;
                CheckRightClick();
                CheckLeftClick();
                CheckCursorIdle();
                CheckPlayerState();
            };
            _rightClickTimer.Start();

            _player!.Volume = trkVolume.Value;
            StartMedia();
            ProcessPendingPipeCommands();   // the explorer asked for another file while the engine was loading
        }

        // Makes the player (on the engine lib) and connects its events and the video window. Used when the window
        // opens, and again when a new visualization needs a new engine.
        private void AttachPlayer(LibVLC lib)
        {
            _activeLib = lib;
            _player = new MediaPlayer(lib);
            // Timing log: when LibVLC reports the first steps of opening (these events come on LibVLC's own threads).
            _player.Opening += (s, a) => OpenTimingLog.Write($"LibVLC: Opening ({_openWatch?.ElapsedMilliseconds} ms after Play was called)");
            _player.Playing += (s, a) =>
            {
                if (_openWatch is { IsRunning: true })
                {
                    OpenTimingLog.Write($"LibVLC: Playing, {_openWatch.ElapsedMilliseconds} ms after Play was called");
                }
            };
            _player.TimeChanged += (s, a) =>
            {
                if (_openWatch is { IsRunning: true } && a.Time > 0)
                {
                    OpenTimingLog.Write($"LibVLC: first picture/time update (position {a.Time} ms), {_openWatch.ElapsedMilliseconds} ms after Play was called");
                    _openWatch.Stop();
                }
            };
            // Without this the native video window swallows the mouse, and no right-click menu opens.
            _player.EnableMouseInput = false;
            _player.Playing += (s, a) => Post(() =>
            {
                HideLoading();
                if (_restoringEnd)
                {
                    return;
                }
                _paused = false;
                _stepTimeMs = -1;
                ShowPlayPause(true);
                ApplyAudioState();
            });
            _player.Paused += (s, a) => Post(() =>
            {
                if (_restoringEnd)
                {
                    return;
                }
                _paused = true;
                ShowPlayPause(false);
                if (_player != null)
                {
                    UpdateTime(_player.Time);
                }
                SavePosition(false);   // where the user paused (after the first 60 seconds of playing)
            });
            _player.Stopped += (s, a) => Post(() =>
            {
                if (!_restoringEnd)
                {
                    ShowStopped();
                }
            });
            _player.EndReached += (s, a) => Post(OnEndReached);
            _player.SeekableChanged += (s, a) => Post(() =>
            {
                _vlcSeekable = a.Seekable != 0;
                ApplySeekState();
            });
            _player.LengthChanged += (s, a) => Post(() =>
            {
                _lengthMs = a.Length;
                if (!_restoringEnd)
                {
                    UpdateTime(0);
                }
            });
            // While paused the label is driven by seeking and frame stepping, not by LibVLC.
            _player.TimeChanged += (s, a) => Post(() =>
            {
                if (!_paused)
                {
                    UpdateTime(a.Time);
                }
                ApplyAudioState();
                ApplyResume();
            });
            videoView.MediaPlayer = _player;
        }

        // Opens the file (or stream) and plays it. Also used to open a file again after a trim.
        // restart: the same file starts again (for a new visualization): the library and the resume offer are left alone.
        private void StartMedia(bool restart = false)
        {
            if (_player == null || _activeLib == null)
            {
                return;
            }

            OpenTimingLog.Write($"StartMedia: {OpenTimingLog.Describe(_filePath, _isStream)}{(restart ? " (restart)" : "")}");
            var stepWatch = System.Diagnostics.Stopwatch.StartNew();
            _audioStateApplied = 0;
            _knownLengthMs = 0;
            ResetTunes();
            ResetChapters();
            ResetSpeed();
            ResetTracks();
            _vlcSeekable = true;
            ApplySeekState();
            ResetThumbnails();
            OpenTimingLog.Write($"StartMedia: reset done, {stepWatch.ElapsedMilliseconds} ms");
            stepWatch.Restart();
            ShowCoverArt();
            UpdateSaveFrameButton();
            OpenTimingLog.Write($"StartMedia: cover art and save-frame button, {stepWatch.ElapsedMilliseconds} ms");
            stepWatch.Restart();
            if (!restart)
            {
                StartLibrary();   // forget what was counted for the last file; offer to resume
            }
            // Includes the time the resume prompt waits for the user, if it is shown.
            OpenTimingLog.Write($"StartMedia: library/resume, {stepWatch.ElapsedMilliseconds} ms");
            stepWatch.Restart();
            _media = new Media(_activeLib, _filePath, _isStream ? FromType.FromLocation : FromType.FromPath);
            _openWatch = System.Diagnostics.Stopwatch.StartNew();
            _player.Play(_media);
            OpenTimingLog.Write($"StartMedia: Media created and Play called, {stepWatch.ElapsedMilliseconds} ms");

            if (!_isStream && SidPluginSetup.IsSidFile(_filePath))
            {
                _ = ShowSidLengthAsync(_filePath);
                _ = LoadSidInfoAsync(_filePath);
            }
        }

        // LibVLC cannot tell how long a SID tune is: look it up (SidSongLengths). An unknown length
        // is left blank. Only the time label and the progress bar use it, not seeking or trimming.
        private async Task ShowSidLengthAsync(string path)
        {
            SidTunes? tunes = await SidSongLengths.FindTunesAsync(path);
            if (tunes != null && !IsDisposed && path == _filePath && _lengthMs <= 0)
            {
                _tuneLengths = tunes.Lengths;
                if (!_tuneChecked)
                {
                    _tuneIndex = tunes.StartSong - 1;   // until LibVLC says which tune plays
                }
                ApplyTune();
            }
        }

        // An audio file has no picture of its own, so LibVLC shows black. If the file has embedded
        // cover art (e.g. an MP3 with an album picture), show that over the video area instead.
        private static readonly string[] AudioExtensions = { ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".oga", ".opus", ".wma", ".wav", ".aif", ".aiff" };

        // Whether the user can jump around in the file: not in a stream, not in SID music (the plugin only
        // plays on) and not in anything else LibVLC reports as not seekable. When it is false the seek
        // bar is read-only (it still shows the progress) and frame stepping and trimming are off.
        private bool CanSeek => !_isStream && !SidPluginSetup.IsSidFile(_filePath) && _vlcSeekable;

        private void ApplySeekState()
        {
            trkSeek.Enabled = CanSeek;
            if (speedBox != null)
            {
                speedBox.Enabled = CanSeek;
            }
        }

        // Audio files do not open in full screen mode, even when the setting asks for it for video.
        // SID music has no picture either.
        private bool IsAudioFile(string path)
        {
            return !_isStream && (SidPluginSetup.IsSidFile(path)
                || AudioExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
        }

        private void ShowCoverArt()
        {
            Image? old = picCover.Image;
            picCover.Image = null;
            picCover.Visible = false;
            old?.Dispose();

            HideTrackInfo();
            if (_isStream || !AudioExtensions.Contains(Path.GetExtension(_filePath), StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            // A visualization takes the place of the cover art.
            bool wantPicture = ActiveVisualization() == null;
            try
            {
                using TagLib.File tags = TagLib.File.Create(_filePath);
                ShowTrackInfo(tags.Tag.FirstPerformer ?? tags.Tag.FirstAlbumArtist, tags.Tag.Title, tags.Tag.Album);

                if (wantPicture && tags.Tag.Pictures.Length > 0)
                {
                    // Prefer the front cover; otherwise take the first picture.
                    TagLib.IPicture picture = tags.Tag.Pictures.FirstOrDefault(p => p.Type == TagLib.PictureType.FrontCover) ?? tags.Tag.Pictures[0];
                    using MemoryStream stream = new(picture.Data.Data);
                    using Image loaded = Image.FromStream(stream);
                    picCover.Image = new Bitmap(loaded);   // a copy: the stream is closed when this method ends
                    picCover.Visible = true;
                    return;
                }
            }
            catch (Exception)
            {
                // No readable tags or picture: the folder's picture is tried, else the black video area stays.
            }

            if (wantPicture)
            {
                _ = ShowFolderCoverAsync(_filePath);
            }
        }

        // The file has no cover art of its own: show the picture of its folder (FolderCoverFinder, the same as the explorer shows).
        // Looked for in the background: the folder may be on a slow drive.
        private async Task ShowFolderCoverAsync(string path)
        {
            Bitmap? cover = await Task.Run(() => FolderCoverFinder.Find(path));
            if (cover == null)
            {
                return;
            }
            if (IsDisposed || path != _filePath || picCover.Image != null || ActiveVisualization() != null)
            {
                cover.Dispose();   // another file or a visualization came meanwhile
                return;
            }

            picCover.Image = cover;
            picCover.Visible = true;
            UpdateSaveFrameButton();
        }

        // LibVLC creates its audio output when playback starts and can ignore a volume or mute set
        // before that, which left some files silent. So the volume and mute state of the controls
        // are set once more when the file is playing, and again at its first time update.
        private void ApplyAudioState()
        {
            if (_player == null || _audioStateApplied >= 2 || _restoringEnd || _previewing)
            {
                return;
            }

            _audioStateApplied++;
            _player.Volume = trkVolume.Value;
            _player.Mute = _muted;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // SplitMediaFiles is replacing the file and the player waits for it.
            if (_trimming)
            {
                e.Cancel = true;
                MessageBox.Show(this, "The file is being trimmed. Wait until SplitMediaFiles is ready, or cancel it there.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SavePosition(true);   // before the player is released: where the user stopped
            ReportClosed();
            _pipe?.Dispose();
            EndPreview(false);
            _previewTimer?.Dispose();
            _previewTimer = null;
            DisposeThumbnails();
            Application.RemoveMessageFilter(this);
            _rightClickTimer?.Dispose();
            _rightClickTimer = null;

            // Detach and release the player on a worker thread: stopping LibVLC from the
            // UI thread can deadlock.
            MediaPlayer? player = _player;
            Media? media = _media;
            LibVLC? ownEngine = _ownLibVlc;
            _player = null;
            _media = null;
            _ownLibVlc = null;
            videoView.MediaPlayer = null;
            if (player != null || ownEngine != null)
            {
                Task.Run(() =>
                {
                    player?.Stop();
                    player?.Dispose();
                    media?.Dispose();
                    ownEngine?.Dispose();
                });
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_trimming)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
            if (keyData == Keys.Space)
            {
                TogglePlayPause();
                return true;
            }
            if (keyData == Keys.PageUp && (_tuneCount > 1 || TrackSkipping) && RestartIfPlayedAWhile())
            {
                return true;
            }
            if (keyData is Keys.PageUp or Keys.PageDown && _tuneCount > 1)
            {
                ChangeTune(_tuneIndex + (keyData == Keys.PageDown ? 1 : -1));
                return true;
            }
            if (keyData is Keys.PageUp or Keys.PageDown && TrackSkipping)
            {
                SkipTrack(keyData == Keys.PageDown ? 1 : -1);   // the explorer's playlist: previous / next track
                return true;
            }
            if (keyData is Keys.Left or Keys.Right && _paused && CanSeek)
            {
                StepFrame(keyData == Keys.Right ? 1 : -1);
                return true;
            }
            // N / P: next / previous chapter, only for a file that has chapters (and not while typing in a text box).
            if (keyData is Keys.N or Keys.P && ChaptersAvailable && !(ActiveControl is TextBoxBase { Visible: true }))
            {
                GoToChapter(keyData == Keys.N ? 1 : -1);
                return true;
            }
            // + / -: faster / slower (] and [ too), Backspace: normal speed; not while typing in a text box.
            if (SpeedAvailable && !(ActiveControl is TextBoxBase { Visible: true }))
            {
                if (keyData is Keys.Oemplus or Keys.Add or Keys.OemCloseBrackets)
                {
                    StepSpeed(+1);
                    return true;
                }
                if (keyData is Keys.OemMinus or Keys.Subtract or Keys.OemOpenBrackets)
                {
                    StepSpeed(-1);
                    return true;
                }
                if (keyData == Keys.Back)
                {
                    ResetSpeed();
                    return true;
                }
            }
            if (keyData == Keys.C)
            {
                ToggleSubtitles();
                return true;
            }
            if (keyData == Keys.S)
            {
                SaveFrame();
                return true;
            }
            if (keyData == (Keys.Control | Keys.D))
            {
                AddBookmark();   // a bookmark for the file explorer (the usual key for it)
                return true;
            }
            if (keyData == Keys.F || keyData == (Keys.Alt | Keys.Enter))
            {
                SetFullScreen(!_fullScreen);
                return true;
            }
            // X alone closes the player window (like Escape, in every view), except while typing in a visible text box
            // (a hidden group box can still be the ActiveControl).
            if (keyData == Keys.X && !(ActiveControl is TextBoxBase { Visible: true }))
            {
                Close();
                return true;
            }
            if (keyData == Keys.Escape && _fullScreen)
            {
                SetFullScreen(false);
                return true;
            }
            // Escape hides the group box that has the focus; when none of them has it, all of them.
            if (keyData == Keys.Escape && (_panelVisible || _trimVisible))
            {
                if (_panelVisible && IsInside(ActiveControl, grpControlPanel))
                {
                    SetPanelVisible(false);
                }
                else if (_trimVisible && IsInside(ActiveControl, grpTrim))
                {
                    SetTrimVisible(false);
                }
                else
                {
                    SetPanelVisible(false);
                    SetTrimVisible(false);
                }
                return true;
            }
            // Third: a visualization that is on is turned off.
            if (keyData == Keys.Escape && ActiveVisualization() != null)
            {
                SetVisualization("none");
                return true;
            }
            // Fourth: with nothing left to leave, hide or turn off, Escape closes the window.
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static bool IsInside(Control? control, Control container)
        {
            for (Control? c = control; c != null; c = c.Parent)
            {
                if (c == container)
                {
                    return true;
                }
            }
            return false;
        }

        // While paused, the mouse wheel anywhere in the window steps one frame (wheel up =
        // forward, wheel down = back), except over the seek bar and the volume slider, which
        // keep using the wheel themselves.
        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL || !_paused || !CanSeek || _trimming || ActiveForm != this)
            {
                return false;
            }

            Point cursor = Cursor.Position;
            if (!Bounds.Contains(cursor) || IsOver(trkSeek, cursor) || IsOver(trkVolume, cursor))
            {
                return false;
            }

            int delta = (short)((long)m.WParam >> 16);
            if (delta != 0)
            {
                StepFrame(delta > 0 ? 1 : -1);
            }
            return true;
        }

        private static bool IsOver(Control control, Point screenPoint)
        {
            return control.Visible && control.RectangleToScreen(control.ClientRectangle).Contains(screenPoint);
        }

        // Steps one frame by seeking one frame length from the tracked time. LibVLC can only
        // step forward natively, and this way both directions behave the same.
        private void StepFrame(int direction)
        {
            if (_player == null || !CanSeek)
            {
                return;
            }

            if (_stepTimeMs < 0)
            {
                _stepTimeMs = _player.Time;
            }

            double frameMs = GetFrameMs();
            _stepTimeMs = (long)Math.Clamp(_stepTimeMs + direction * frameMs, 0, Math.Max(_lengthMs - 1, 0));
            _player.Time = _stepTimeMs;
            UpdateTime(_stepTimeMs);
        }

        private double GetFrameMs()
        {
            if (_frameMs <= 0 && _media != null)
            {
                try
                {
                    foreach (MediaTrack track in _media.Tracks)
                    {
                        if (track.TrackType == TrackType.Video && track.Data.Video.FrameRateNum > 0 && track.Data.Video.FrameRateDen > 0)
                        {
                            _frameMs = 1000.0 * track.Data.Video.FrameRateDen / track.Data.Video.FrameRateNum;
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // The frame rate is not known (yet): use the fallback below.
                }
            }
            return _frameMs > 0 ? _frameMs : 40;   // 25 fps if the real rate is unknown
        }

        // Full screen hides the border and the control bar. Space still toggles play/pause.
        private void SetFullScreen(bool fullScreen)
        {
            if (fullScreen == _fullScreen)
            {
                return;
            }

            _fullScreen = fullScreen;
            if (fullScreen)
            {
                _windowStateBeforeFullScreen = WindowState;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _windowStateBeforeFullScreen;
            }
            pnlControls.Visible = !fullScreen;
            grpTrim.Visible = _trimVisible && !fullScreen;
            grpControlPanel.Visible = _panelVisible && !fullScreen;

            // Changing the border style makes a new window, which shows the normal pointer again: forget that it was
            // hidden, and count the pointer as idle already, so it is hidden again at once (until it moves or clicks).
            _cursorHidden = false;
            videoView.Cursor = Cursors.Default;
            _lastCursorPosition = Cursor.Position;
            _lastCursorActivityTicks = Environment.TickCount64 - CursorHideDelayMs;

            // A tooltip of a button that has just been hidden stays on the screen, as the mouse never leaves it.
            toolTip.Active = false;
            toolTip.Active = true;
        }

        // LibVLC raises its events on its own threads: hand the work over to the UI thread.
        private void Post(Action action)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(action);
            }
        }

        // The play/pause button shows what a click will do: pause bars while playing, a play
        // triangle otherwise. It follows the real state of the player (see CheckPlayerState).
        private void ShowPlayPause(bool playing)
        {
            if (playing == _shownPlaying)
            {
                return;
            }

            _shownPlaying = playing;
            btnPlayPause.Icon = playing ? IconKind.Pause : IconKind.Play;
            toolTip.SetToolTip(btnPlayPause, playing ? "Pause (Space)" : "Play (Space)");
        }

        // When a video has played to its end, LibVLC drops the picture and the player is "Ended":
        // nothing can be saved then, and Play does not restart it. So for a file, start it again
        // silently and pause on the last frame it can show. Play then starts from the beginning.
        private async void OnEndReached()
        {
            LibraryOnEnded();
            MediaPlayer? player = _player;

            // The explorer is told. If it runs a playlist it sends the next file: the end actions below are not used.
            if (!_previewing && ReportEnded())
            {
                _ = Task.Run(player.Stop);   // let go of the file: the explorer may rename or move it at once
                ShowStopped();
                return;
            }

            if (player != null && !_previewing && EffectiveEndAction == EndAction.ClosePlayer)
            {
                // Closed from the message loop, not from inside the LibVLC event.
                BeginInvoke(Close);
                return;
            }

            if (player == null || _isStream)
            {
                ShowStopped();
                return;
            }

            // The Control Panel decides what happens at the end (not while a preview plays).
            if (!_previewing && EffectiveEndAction == EndAction.Repeat)
            {
                RestartPlayback();
                return;
            }
            if (!_previewing && EffectiveEndAction == EndAction.BlackScreen)
            {
                _ = Task.Run(player.Stop);
                ShowStopped();
                return;
            }

            // Show the end at once and keep it there: the player is started again behind the scenes.
            _restoringEnd = true;
            _paused = true;
            _stepTimeMs = -1;
            ShowPlayPause(false);
            UpdateTime(player.Length > 0 ? player.Length : _lengthMs);

            bool wasMuted = _muted;
            try
            {
                player.Mute = true;
                await Task.Run(() => player.Stop());
                if (_player != player)
                {
                    return;
                }

                player.Play();
                for (int i = 0; i < 60 && !player.IsPlaying; i++)
                {
                    await Task.Delay(50);
                }
                if (_player != player)
                {
                    return;
                }

                player.SetPause(true);
                await Task.Delay(300);
                if (_player != player)
                {
                    return;
                }

                long endMs = player.Length > 0 ? player.Length : _lengthMs;
                player.Time = endMs;
                await Task.Delay(300);
                if (_player == player)
                {
                    UpdateTime(endMs);
                }
            }
            finally
            {
                _restoringEnd = false;
                if (_player == player)
                {
                    player.Mute = wasMuted;
                    _paused = true;
                    ShowPlayPause(false);
                }
            }
        }

        private void ShowStopped()
        {
            _paused = false;
            _stepTimeMs = -1;
            _pendingSeekMs = -1;
            ShowPlayPause(false);
            if (!_seeking)
            {
                trkSeek.Value = 0;
            }
            UpdateTime(0);
        }

        private void UpdateTime(long timeMs)
        {
            // A stream has no real length (a live stream reports a fixed window): show only the time.
            long length = _lengthMs > 0 ? _lengthMs : _knownLengthMs;
            lblTime.Text = _isStream || length <= 0
                ? FormatTime(timeMs, _paused)
                : $"{FormatTime(timeMs, _paused)} / {FormatTime(length, false)}";
            if (!_seeking && !_isStream && length > 0)
            {
                trkSeek.Value = (int)Math.Clamp(timeMs * trkSeek.Maximum / length, 0, trkSeek.Maximum);
            }
        }

        private static string FormatTime(long ms, bool withFraction)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(Math.Max(ms, 0));
            string text = t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
            return withFraction ? $"{text}.{t.Milliseconds:000}" : text;
        }

        private void TogglePlayPause()
        {
            if (_player == null || _restoringEnd)
            {
                return;
            }

            _tuneAdvanceAtTicks = 0;   // the user decides now: no automatic next tune

            if (_player.State is VLCState.Stopped or VLCState.Ended or VLCState.Error or VLCState.NothingSpecial)
            {
                RestartPlayback();
            }
            else if (!_player.IsPlaying && _lengthMs > 0 && _player.Time >= _lengthMs - 200)
            {
                // Paused on the last frame: play again from the beginning.
                _player.Time = 0;
                _player.SetPause(false);
            }
            else
            {
                _player.SetPause(_player.IsPlaying);
            }
        }

        private static bool IsStopped(MediaPlayer player)
        {
            return player.State is VLCState.Stopped or VLCState.Ended or VLCState.Error or VLCState.NothingSpecial;
        }

        // An ended player only plays again after a Stop.
        private async void RestartPlayback()
        {
            MediaPlayer? player = _player;
            if (player == null)
            {
                return;
            }

            await Task.Run(() => player.Stop());
            if (_player != player)
            {
                return;
            }

            // The position the user dragged the seek bar to while the video was stopped.
            long seekTo = _pendingSeekMs;
            _pendingSeekMs = -1;
            if (seekTo <= 0)
            {
                player.Play();
                return;
            }

            // The seek only works once the video is playing: keep the sound off until then, so
            // the first moment of the video is not heard.
            player.Mute = true;
            player.Play();
            for (int i = 0; i < 60 && !player.IsPlaying; i++)
            {
                await Task.Delay(50);
            }
            if (_player == player)
            {
                player.Time = seekTo;
                player.Mute = _muted;
            }
        }

        private void btnPlayPause_Click(object? sender, EventArgs e)
        {
            TogglePlayPause();
        }

        private void btnSaveFrame_Click(object? sender, EventArgs e)
        {
            SaveFrame();
        }

        // Saves the frame on screen as a PNG, at the video's own resolution. The file name
        // suggested contains the (approximate) time of the frame. When the video is at its end,
        // the very last frame is taken with FFmpeg, because LibVLC cannot show the last frames.
        //
        // With asPreviewPicture (right-click on the camera button: Set as Preview Picture) the frame is saved as the video's
        // preview picture for the file explorer: the suggested name is <video name>-poster.png in the folder of the video, the
        // file gets the Hidden attribute, and the picture the user saved it as is written to the library (the table PreviewPick,
        // which the explorer reads), so the explorer shows it for this video.
        private async void SaveFrame(bool asPreviewPicture = false)
        {
            if (_player == null || !_saveFrameShown)
            {
                return;
            }

            // An audio file showing its cover art: save that picture, at its own resolution.
            if (!asPreviewPicture && picCover.Visible && picCover.Image != null)
            {
                SaveCoverArt();
                return;
            }

            bool atEnd = IsAtEnd();

            // A stopped player has no picture: saving would fail.
            if (!atEnd && _player.State is VLCState.Stopped or VLCState.Ended or VLCState.Error or VLCState.NothingSpecial)
            {
                MessageBox.Show(this, "There is no picture to save, because the video is stopped. Press Play first.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long timeMs = atEnd ? _lengthMs : _paused && _stepTimeMs >= 0 ? _stepTimeMs : _player.Time;
            TimeSpan t = TimeSpan.FromMilliseconds(Math.Max(timeMs, 0));
            string stamp = $"{(int)t.TotalHours}-{t.Minutes:00}-{t.Seconds:00}-{t.Milliseconds:000}";

            string folder = _isStream ? "" : Path.GetDirectoryName(Path.GetFullPath(_filePath)) ?? "";
            using var dialog = new SaveFileDialog
            {
                Title = asPreviewPicture ? "Set as Preview Picture" : "Save Frame as PNG",
                Filter = "PNG Image|*.png",
                DefaultExt = "png",
                FileName = asPreviewPicture
                    ? $"{Path.GetFileNameWithoutExtension(_filePath)}-poster.png"
                    : $"{(_isStream ? "stream" : Path.GetFileNameWithoutExtension(_filePath))}_{stamp}{(atEnd ? "_last" : "")}.png",
                InitialDirectory = asPreviewPicture ? folder : _lastSaveFolder ?? (_isStream ? null : folder),
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!asPreviewPicture)
            {
                _lastSaveFolder = Path.GetDirectoryName(dialog.FileName);
            }

            // A hidden poster (the explorer makes them hidden) cannot be written over as it is: the user has just agreed to
            // replace it, so the attribute goes first and comes back with the new picture.
            if (asPreviewPicture)
            {
                ClearHidden(dialog.FileName);
            }
            DateTime started = DateTime.UtcNow;

            if (atEnd)
            {
                if (await SaveLastFrameWithFfmpegAsync(dialog.FileName))
                {
                    if (asPreviewPicture) await FinishPreviewPictureAsync(dialog.FileName, started);
                    return;
                }
                if (IsDisposed || _player == null)
                {
                    return;
                }

                // FFmpeg is not available, or failed: save what the player can show instead.
                if (_player.TakeSnapshot(0, dialog.FileName, 0, 0))
                {
                    if (asPreviewPicture) await FinishPreviewPictureAsync(dialog.FileName, started);
                    MessageBox.Show(this, "FFmpeg was not found or could not read the video, so the frame closest to the end that the player can show was saved instead. It may be one or two frames before the very last one.", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            else if (_player.TakeSnapshot(0, dialog.FileName, 0, 0))
            {
                if (asPreviewPicture) await FinishPreviewPictureAsync(dialog.FileName, started);
                return;
            }

            if (asPreviewPicture)
            {
                SetHidden(dialog.FileName);   // an old poster that was there stays hidden
            }
            MessageBox.Show(this, "The frame could not be saved.", AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void ClearHidden(string file)
        {
            try
            {
                if (File.Exists(file)) File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.Hidden);
            }
            catch { /* the save will say if it cannot write */ }
        }

        private static void SetHidden(string file)
        {
            try
            {
                if (File.Exists(file)) File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.Hidden);
            }
            catch { /* a file that stays visible is not an error */ }
        }

        // The frame is saved as the preview picture: hide the file and remember it in the library for this video.
        // LibVLC writes a snapshot a moment after TakeSnapshot has returned, so the file is waited for (it must have been
        // written after the save started, and stop growing) before it gets the Hidden attribute.
        private async Task FinishPreviewPictureAsync(string file, DateTime startedUtc)
        {
            long lastLength = -1;
            for (int i = 0; i < 100; i++)   // at most 5 seconds
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.Exists && info.Length > 0 && info.LastWriteTimeUtc >= startedUtc.AddSeconds(-1))
                    {
                        if (info.Length == lastLength) break;   // unchanged since the last look: it is complete
                        lastLength = info.Length;
                    }
                }
                catch { }
                await Task.Delay(50);
            }

            SetHidden(file);

            if (!LibraryStore.SetPreviewChoice(_libraryPath, new LibraryStore.PreviewChoice(false, file)))
            {
                MessageBox.Show(this, "The picture was saved, but it could not be remembered for this video in the library (the drive may be write-protected).", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Right-click on the camera button: only for a video file on the disk (not a stream, not a file unpacked from an archive,
        // not an audio file's cover art), as that is what the explorer shows a preview picture for.
        private void menuSaveFrame_Opening(object? sender, CancelEventArgs e)
        {
            bool available = _saveFrameShown && !_isStream && !(picCover.Visible && picCover.Image != null)
                && string.Equals(_libraryPath, _filePath, StringComparison.OrdinalIgnoreCase);
            if (!available)
            {
                e.Cancel = true;
            }
        }

        private void mnuSetPreviewPicture_Click(object? sender, EventArgs e)
        {
            SaveFrame(asPreviewPicture: true);
        }

        // Saves the cover art on screen as a PNG (there is no video frame to take).
        private void SaveCoverArt()
        {
            if (picCover.Image == null)
            {
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Save Cover Art as PNG",
                Filter = "PNG Image|*.png",
                DefaultExt = "png",
                FileName = $"{Path.GetFileNameWithoutExtension(_filePath)}_cover.png",
                InitialDirectory = _lastSaveFolder ?? Path.GetDirectoryName(Path.GetFullPath(_filePath)),
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _lastSaveFolder = Path.GetDirectoryName(dialog.FileName);
            try
            {
                picCover.Image.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"The cover art could not be saved.\n\n{ex.Message}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // True when a file has played to its end, or the user has moved to the end.
        private bool IsAtEnd()
        {
            if (_player == null || _isStream || _lengthMs <= 0)
            {
                return false;
            }
            if (_restoringEnd || _player.State == VLCState.Ended)
            {
                return true;
            }
            return _player.State == VLCState.Paused && _player.Time >= _lengthMs - 20;
        }

        // FFmpeg reads the last two seconds of the file and keeps overwriting the one picture
        // file, so what is left is the very last frame.
        private async Task<bool> SaveLastFrameWithFfmpegAsync(string outputPath)
        {
            string? ffmpeg = FindFfmpeg();
            if (ffmpeg == null)
            {
                return false;
            }

            UseWaitCursor = true;
            try
            {
                return await Task.Run(() =>
                {
                    System.Diagnostics.ProcessStartInfo info = new(ffmpeg)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    };
                    foreach (string argument in new[] { "-y", "-loglevel", "error", "-sseof", "-2", "-i", _filePath, "-an", "-sn", "-update", "1", outputPath })
                    {
                        info.ArgumentList.Add(argument);
                    }

                    using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(info);
                    if (process == null)
                    {
                        return false;
                    }
                    process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(60000))
                    {
                        process.Kill();
                        return false;
                    }
                    return process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
                });
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // ffmpeg.exe next to the program, or else the first one found in the PATH.
        private static string? FindFfmpeg()
        {
            string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(local))
            {
                return local;
            }

            foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(folder.Trim().Trim('"'), "ffmpeg.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch (ArgumentException)
                {
                    // A folder in the PATH with illegal characters: skip it.
                }
            }
            return null;
        }
        // The video window does not pass mouse clicks on either, so left clicks are detected here:
        // a press of the main mouse button. While the right-click menu is open, a press outside
        // it closes it. Two presses over the video, close in time and place, are a double click.
        private void CheckLeftClick()
        {
            const int VK_LBUTTON = 0x01;
            const int VK_RBUTTON = 0x02;
            int key = SystemInformation.MouseButtonsSwapped ? VK_RBUTTON : VK_LBUTTON;
            bool down = (GetAsyncKeyState(key) & 0x8000) != 0;
            bool pressed = down && !_leftButtonWasDown;
            _leftButtonWasDown = down;

            bool menuOpen = menuVideo.Visible;
            bool menuWasOpen = _menuWasOpen;
            _menuWasOpen = menuOpen;
            if (!pressed)
            {
                return;
            }

            Point cursor = Cursor.Position;
            if (menuOpen)
            {
                if (!IsOverMenu(cursor))
                {
                    menuVideo.Close();
                }
                return;
            }

            // The press that just closed the menu is not a click on the video.
            if (menuWasOpen || ActiveForm != this || !IsOver(videoView, cursor))
            {
                return;
            }

            long now = Environment.TickCount64;
            Size area = SystemInformation.DoubleClickSize;
            bool isDoubleClick = now - _lastClickTicks <= SystemInformation.DoubleClickTime
                && Math.Abs(cursor.X - _lastClickPosition.X) <= area.Width / 2
                && Math.Abs(cursor.Y - _lastClickPosition.Y) <= area.Height / 2;

            if (isDoubleClick)
            {
                _lastClickTicks = 0;
                if (_doubleClickTogglesFullScreen)
                {
                    SetFullScreen(!_fullScreen);
                }
            }
            else
            {
                _lastClickTicks = now;
                _lastClickPosition = cursor;
            }
        }

        // Hides the pointer over the video when it has not moved or clicked for a while, and shows
        // it again at the first movement or click. The video window is on a thread of its own, so
        // this too is watched with the timer.
        private void CheckCursorIdle()
        {
            const int VK_LBUTTON = 0x01;
            const int VK_RBUTTON = 0x02;
            const int VK_MBUTTON = 0x04;

            Point position = Cursor.Position;
            long now = Environment.TickCount64;
            bool buttonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0
                || (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0
                || (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0;
            if (position != _lastCursorPosition || buttonDown)
            {
                _lastCursorPosition = position;
                _lastCursorActivityTicks = now;
                SetCursorHidden(false);
                return;
            }

            bool canHide = _hideCursorWhenIdle && ActiveForm == this && !menuVideo.Visible && IsOver(videoView, position);
            SetCursorHidden(canHide && now - _lastCursorActivityTicks >= CursorHideDelayMs);
            if (_cursorHidden && _blankCursor != null)
            {
                // Windows may put the normal pointer back without any movement (a new window after a full screen
                // change, a tooltip going away): keep the blank one on for as long as the pointer should be hidden.
                Cursor.Current = _blankCursor;
            }
        }

        private void SetCursorHidden(bool hidden)
        {
            if (hidden == _cursorHidden)
            {
                return;
            }

            _blankCursor ??= CreateBlankCursor();
            videoView.Cursor = hidden ? _blankCursor : Cursors.Default;
            // A still pointer gets no new WM_SETCURSOR (e.g. right after a full screen change, which makes a
            // new window), so the Cursor property alone changes nothing on screen: set the pointer itself too.
            Cursor.Current = hidden ? _blankCursor : Cursors.Default;
            _cursorHidden = hidden;
        }

        // A pointer that cannot be seen: a fully transparent picture. The Cursor does not own
        // the icon handle, so it is kept for as long as the program runs (one small icon).
        private static Cursor CreateBlankCursor()
        {
            using Bitmap bitmap = new(32, 32);
            return new Cursor(bitmap.GetHicon());
        }

        // True when the point is on the right-click menu or one of its open submenus.
        private bool IsOverMenu(Point screenPoint)
        {
            if (menuVideo.Visible && menuVideo.Bounds.Contains(screenPoint))
            {
                return true;
            }
            return menuVideo.Items.OfType<ToolStripDropDownItem>()
                .Any(item => item.DropDown.Visible && item.DropDown.Bounds.Contains(screenPoint));
        }
        // Makes sure the play/pause icon matches what the player is really doing, also when an
        // event was missed or arrived in another order.
        private void CheckPlayerState()
        {
            if (_player == null)
            {
                return;
            }

            if (!_restoringEnd)
            {
                ShowPlayPause(_player.IsPlaying);
            }

            CheckTunes();
            CheckChapters();
            CheckSpeed();
            CheckTracks();
            TrackLibrary();

            if (_isStream && !_videoCheckDone && _player.IsPlaying)
            {
                if (_playingSinceTicks == 0)
                {
                    _playingSinceTicks = Environment.TickCount64;
                }
                else if (Environment.TickCount64 - _playingSinceTicks > 1500)
                {
                    EnsureVideoTrack();
                }
            }
        }

        // Some HLS streams start with the sound only and a black picture. Their playlist has a
        // video group without an address (EXT-X-MEDIA TYPE=VIDEO), which LibVLC shows as an extra
        // entry named "video Video" in the audio track list instead of as a video track. Picking
        // that entry, then pausing and resuming briefly, makes the picture appear.
        private async void EnsureVideoTrack()
        {
            MediaPlayer? player = _player;
            if (player == null)
            {
                return;
            }

            if (player.VideoTrackDescription.Any(t => t.Id >= 0) || player.VideoTrack >= 0)
            {
                _videoCheckDone = true;   // there is a normal video track
                return;
            }

            // The track ids change while the stream starts, so look the entry up by name now.
            var videoEntries = player.AudioTrackDescription
                .Where(t => t.Id >= 0 && t.Name.StartsWith("video", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (videoEntries.Length == 0)
            {
                // The tracks may not be known yet: look again for a while.
                _videoCheckDone = Environment.TickCount64 - _playingSinceTicks > 15000;
                return;
            }

            _videoCheckDone = true;
            player.SetAudioTrack(videoEntries[0].Id);
            await Task.Delay(500);
            if (_player == player)
            {
                player.SetPause(true);
                await Task.Delay(300);
            }
            if (_player == player)
            {
                player.SetPause(false);
            }
        }
        // ---- Right-click menu: audio track and subtitle track, as in VLC ----

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        private void CheckRightClick()
        {
            const int VK_LBUTTON = 0x01;
            const int VK_RBUTTON = 0x02;
            // The menu button is the right one, or the left one when the buttons are swapped.
            int key = SystemInformation.MouseButtonsSwapped ? VK_LBUTTON : VK_RBUTTON;
            bool down = (GetAsyncKeyState(key) & 0x8000) != 0;
            bool released = _rightButtonWasDown && !down;
            _rightButtonWasDown = down;

            Point cursor = Cursor.Position;
            if (released && (ActiveForm == this || menuVideo.Visible) && IsOver(videoView, cursor) && !IsOverMenu(cursor))
            {
                // A right click somewhere else moves the menu there.
                if (menuVideo.Visible)
                {
                    menuVideo.Close();
                }
                menuVideo.Show(videoView, videoView.PointToClient(cursor));
            }
        }

        private void menuVideo_Opening(object? sender, CancelEventArgs e)
        {
            MediaPlayer? player = _player;
            if (player == null || _trimming)
            {
                e.Cancel = true;
                return;
            }

            menuVideo.Items.Clear();

            ToolStripMenuItem audio = new("Audio Track");
            int currentAudio = player.AudioTrack;
            foreach (TrackDescription track in player.AudioTrackDescription.Where(t => t.Id >= 0))
            {
                int id = track.Id;
                audio.DropDownItems.Add(TrackItem(track.Name, id == currentAudio, () => ChooseAudio(id)));
            }
            audio.Enabled = audio.DropDownItems.Count > 0;
            // Setting: a track menu is only shown when there is a choice (subtitles: one track is enough, they can be turned off).
            bool onlyWithChoice = AppSettings.Load().TrackMenusOnlyWithChoice;
            if (!onlyWithChoice || audio.DropDownItems.Count > 1)
            {
                menuVideo.Items.Add(audio);
            }

            // A file with several pictures (angles, e.g. an MKV with two video streams); the order is the one of VLC: audio, video, subtitle.
            ToolStripMenuItem video = new("Video Track");
            int currentVideo = player.VideoTrack;
            foreach (TrackDescription track in player.VideoTrackDescription.Where(t => t.Id >= 0))
            {
                int id = track.Id;
                video.DropDownItems.Add(TrackItem(track.Name, id == currentVideo, () => ChooseVideo(id)));
            }
            video.Enabled = video.DropDownItems.Count > 0;
            if (!onlyWithChoice || video.DropDownItems.Count > 1)
            {
                menuVideo.Items.Add(video);
            }

            ToolStripMenuItem subtitles = new("Subtitle Track");
            int currentSpu = player.Spu;
            subtitles.DropDownItems.Add(TrackItem("Disable", currentSpu < 0, () => ChooseSubtitle(-1)));
            foreach (TrackDescription track in player.SpuDescription.Where(t => t.Id >= 0))
            {
                int id = track.Id;
                subtitles.DropDownItems.Add(TrackItem(track.Name, id == currentSpu, () => ChooseSubtitle(id)));
            }
            subtitles.Enabled = subtitles.DropDownItems.Count > 1;
            if (!onlyWithChoice || subtitles.DropDownItems.Count > 1)   // the first item is "Disable"
            {
                menuVideo.Items.Add(subtitles);
            }
            AddDefaultTracksMenuItem();
            AddTuneMenu();
            AddChapterMenu();
            AddVisualizationMenu();

            // WinForms sets Cancel when the menu has no items yet, which is the case here.
            e.Cancel = false;

            // The menu itself is styled once in the constructor; the submenus are new each time.
            foreach (ToolStripItem item in menuVideo.Items)
            {
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    StyleMenu(dropDownItem.DropDown);
                }
            }
        }

        private static ToolStripMenuItem TrackItem(string text, bool isCurrent, Action select)
        {
            ToolStripMenuItem item = new(text) { Checked = isCurrent };
            item.Click += (s, a) => select();
            return item;
        }

        // Dark colors for the menu and its submenus.
        private static void StyleMenu(ToolStrip menu)
        {
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false };
            menu.BackColor = Color.FromArgb(43, 43, 43);
            menu.ForeColor = Color.FromArgb(230, 230, 230);
            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    StyleMenu(dropDownItem.DropDown);
                }
            }
        }

        // The C key: switches the subtitles off, or back on (the track that was on, or else the first one). Like a choice in the menu,
        // it is remembered for the file (MediaPlayerForm.Tracks.cs).
        private void ToggleSubtitles()
        {
            MediaPlayer? player = _player;
            if (player == null)
            {
                return;
            }

            if (player.Spu >= 0)
            {
                ChooseSubtitle(-1);
                return;
            }

            int id = FirstSubtitleToTurnOn(player);
            if (id >= 0)
            {
                ChooseSubtitle(id);
            }
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
            _tuneAdvanceAtTicks = 0;
            EndPreview(false);
            MediaPlayer? player = _player;
            if (player != null)
            {
                Task.Run(player.Stop);
            }
        }

        private void trkSeek_MouseDown(object? sender, MouseEventArgs e)
        {
            _seeking = true;
        }

        private void trkSeek_MouseUp(object? sender, MouseEventArgs e)
        {
            _seeking = false;
        }

        private void trkSeek_Scroll(object? sender, EventArgs e)
        {
            if (_lengthMs > 0)
            {
                SeekTo(_lengthMs * trkSeek.Value / trkSeek.Maximum);
            }
        }

        private void SeekTo(long timeMs)
        {
            if (_player == null || _lengthMs <= 0)
            {
                return;
            }

            EndPreview(false);
            if (IsStopped(_player))
            {
                // A stopped player ignores seeks: remember the position for Play.
                _pendingSeekMs = timeMs;
                UpdateTime(timeMs);
                return;
            }
            _player.Time = timeMs;
            _stepTimeMs = timeMs;
            // LibVLC raises no time events while paused, so update the label ourselves.
            UpdateTime(timeMs);
        }

        // The media keys on some keyboards arrive as WM_APPCOMMAND, not as key presses.
        // Previous/Next track jump to the beginning/end of the video (there are no chapters).
        protected override void WndProc(ref Message m)
        {
            const int WM_APPCOMMAND = 0x0319;
            const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
            const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;
            const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;
            const int APPCOMMAND_MEDIA_STOP = 13;
            const int APPCOMMAND_MEDIA_PLAY = 46;
            const int APPCOMMAND_MEDIA_PAUSE = 47;

            if (m.Msg == WM_APPCOMMAND)
            {
                int command = (int)(((long)m.LParam >> 16) & 0x7FFF);
                if (command is APPCOMMAND_MEDIA_PLAY_PAUSE or APPCOMMAND_MEDIA_STOP or APPCOMMAND_MEDIA_PLAY or APPCOMMAND_MEDIA_PAUSE
                    or APPCOMMAND_MEDIA_NEXTTRACK or APPCOMMAND_MEDIA_PREVIOUSTRACK)
                {
                    if (!_trimming)
                    {
                        if (command == APPCOMMAND_MEDIA_PLAY_PAUSE)
                        {
                            TogglePlayPause();
                        }
                        else if (command == APPCOMMAND_MEDIA_STOP)
                        {
                            btnStop_Click(null, EventArgs.Empty);
                        }
                        else if (command is APPCOMMAND_MEDIA_PLAY or APPCOMMAND_MEDIA_PAUSE)
                        {
                            // Separate Play and Pause keys: only act when the state differs.
                            if (_player != null && _player.IsPlaying == (command == APPCOMMAND_MEDIA_PAUSE))
                            {
                                TogglePlayPause();
                            }
                        }
                        else if (command == APPCOMMAND_MEDIA_PREVIOUSTRACK && (TrackSkipping || _tuneCount > 1) && RestartIfPlayedAWhile())
                        {
                            // back to the start of the file
                        }
                        else if (TrackSkipping)
                        {
                            SkipTrack(command == APPCOMMAND_MEDIA_PREVIOUSTRACK ? -1 : 1);
                        }
                        else if (_tuneCount > 1)
                        {
                            ChangeTune(_tuneIndex + (command == APPCOMMAND_MEDIA_PREVIOUSTRACK ? -1 : 1));
                        }
                        else if (CanSeek)
                        {
                            // Stay a little before the end, so playback does not end by itself.
                            SeekTo(command == APPCOMMAND_MEDIA_PREVIOUSTRACK ? 0 : Math.Max(_lengthMs - 100, 0));
                        }
                    }
                    m.Result = 1;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        // Mute and the volume slider work together:
        //  - Moving the slider while muted unmutes, at the new volume.
        //  - Dragging the slider down to 0 is the same as muting; Unmute then goes to full
        //    volume (there is no earlier volume worth going back to).
        //  - Mute and Unmute with the button keep the slider where it was.
        private void btnMute_Click(object? sender, EventArgs e)
        {
            if (_player == null)
            {
                return;
            }

            if (!_muted)
            {
                SetMuted(true);
            }
            else if (trkVolume.Value == 0)
            {
                trkVolume.Value = trkVolume.Maximum;
                _player.Volume = trkVolume.Value;
                SetMuted(false);
                ShowVolumeTip();
            }
            else
            {
                SetMuted(false);
            }
        }

        private void trkVolume_Scroll(object? sender, EventArgs e)
        {
            if (_player == null)
            {
                return;
            }

            _player.Volume = trkVolume.Value;
            SetMuted(trkVolume.Value == 0);
            ShowVolumeTip();
        }

        // Shows the volume in percent in a tooltip above the thumb while the slider is used.
        private void ShowVolumeTip()
        {
            string text = $"Volume: {trkVolume.Value}%";
            int pad = LogicalToDeviceUnits(8);   // about half the width of the thumb
            int thumbX = pad + (int)((double)trkVolume.Value / trkVolume.Maximum * (trkVolume.Width - 2 * pad));
            toolTip.SetToolTip(trkVolume, text);
            toolTip.Show(text, trkVolume, thumbX - LogicalToDeviceUnits(30), -LogicalToDeviceUnits(28), 2000);
        }

        private void SetMuted(bool muted)
        {
            if (_player == null)
            {
                return;
            }

            // Keep our own flag: LibVLC applies the mute asynchronously, so reading
            // _player.Mute right after setting it still returns the old value.
            _muted = muted;
            _player.Mute = muted;
            btnMute.MutedColor = _mutedSymbolIsRed ? Color.FromArgb(235, 75, 75) : null;
            btnMute.Icon = muted ? IconKind.Muted : IconKind.Volume;
            toolTip.SetToolTip(btnMute, muted ? "Unmute" : "Mute");
        }
    }

    // A TrackBar where a click on the bar jumps straight to that position (as in most media
    // players) and the drag continues from there. A standard TrackBar only moves one page step.
    internal sealed class JumpTrackBar : TrackBar
    {
        private const int WM_LBUTTONDOWN = 0x0201;

        // The position (0 to 1) of the bar under a pointer at x (in the control's pixels).
        public double RatioAt(int x)
        {
            int pad = LogicalToDeviceUnits(8);
            int channel = Math.Max(Width - 2 * pad, 1);
            return Math.Clamp((x - pad) / (double)channel, 0, 1);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN && Orientation == Orientation.Horizontal)
            {
                int x = (short)((long)m.LParam & 0xFFFF);
                int pad = LogicalToDeviceUnits(8);   // about half the width of the thumb
                int channel = Math.Max(Width - 2 * pad, 1);
                double thumbX = pad + (double)(Value - Minimum) / Math.Max(Maximum - Minimum, 1) * channel;

                // A click on the thumb itself starts a normal drag.
                if (Math.Abs(x - thumbX) > pad)
                {
                    Value = Minimum + (int)Math.Round(RatioAt(x) * (Maximum - Minimum));
                    OnScroll(EventArgs.Empty);
                }
            }
            base.WndProc(ref m);
        }
    }

    // A button that draws its own icon, so it can be filled and take the colors of the dark theme.
    // BackColor is the button color, ForeColor the icon color.
    internal enum IconKind { Play, Pause, Stop, Camera, Volume, Muted, PreviousTune, NextTune, More, Bookmark, PreviousChapter, NextChapter }

    internal sealed class IconButton : Button
    {
        private IconKind _icon;
        private Color? _mutedColor;
        private bool _hot;
        private bool _down;

        public IconButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        // Color of the muted icon; null = same as ForeColor.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color? MutedColor
        {
            get => _mutedColor;
            set { _mutedColor = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IconKind Icon
        {
            get => _icon;
            set { _icon = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hot = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color back = !Enabled ? BackColor : _down ? Lighten(BackColor, 0.25f) : _hot ? Lighten(BackColor, 0.12f) : BackColor;
            Color fore = !Enabled ? Color.FromArgb(110, 110, 110) : _icon == IconKind.Muted && _mutedColor != null ? _mutedColor.Value : ForeColor;
            g.Clear(back);

            // The icon is drawn in a square box of about half the button height.
            float size = Math.Min(Width, Height) * 0.5f;
            RectangleF box = new((Width - size) / 2, (Height - size) / 2, size, size);
            using SolidBrush brush = new(fore);
            using Pen pen = new(fore, Math.Max(1.5f, size * 0.09f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            PointF P(float x, float y) => new(box.X + x * box.Width, box.Y + y * box.Height);
            RectangleF R(float x, float y, float w, float h) => new(box.X + x * box.Width, box.Y + y * box.Height, w * box.Width, h * box.Height);

            switch (_icon)
            {
                case IconKind.Play:
                    g.FillPolygon(brush, new[] { P(0.18f, 0f), P(0.18f, 1f), P(0.96f, 0.5f) });
                    break;
                case IconKind.Pause:
                    g.FillRectangle(brush, R(0.10f, 0f, 0.32f, 1f));
                    g.FillRectangle(brush, R(0.58f, 0f, 0.32f, 1f));
                    break;
                case IconKind.Stop:
                    g.FillRectangle(brush, R(0.08f, 0.08f, 0.84f, 0.84f));
                    break;
                case IconKind.PreviousChapter:   // a bar and one triangle, as VLC draws the chapter buttons
                    g.FillRectangle(brush, R(0.08f, 0.05f, 0.14f, 0.90f));
                    g.FillPolygon(brush, new[] { P(0.95f, 0.05f), P(0.95f, 0.95f), P(0.30f, 0.5f) });
                    break;
                case IconKind.More:
                    foreach (float cx in new[] { 0.10f, 0.50f, 0.90f })
                    {
                        g.FillEllipse(brush, R(cx - 0.12f, 0.38f, 0.24f, 0.24f));
                    }
                    break;
                case IconKind.NextChapter:
                    g.FillRectangle(brush, R(0.78f, 0.05f, 0.14f, 0.90f));
                    g.FillPolygon(brush, new[] { P(0.05f, 0.05f), P(0.05f, 0.95f), P(0.70f, 0.5f) });
                    break;
                case IconKind.PreviousTune:   // a bar and two triangles: previous tune / track
                    g.FillRectangle(brush, R(0.0f, 0.05f, 0.12f, 0.90f));
                    g.FillPolygon(brush, new[] { P(0.55f, 0.05f), P(0.55f, 0.95f), P(0.15f, 0.5f) });
                    g.FillPolygon(brush, new[] { P(0.98f, 0.05f), P(0.98f, 0.95f), P(0.58f, 0.5f) });
                    break;
                case IconKind.NextTune:
                    g.FillRectangle(brush, R(0.88f, 0.05f, 0.12f, 0.90f));
                    g.FillPolygon(brush, new[] { P(0.02f, 0.05f), P(0.02f, 0.95f), P(0.42f, 0.5f) });
                    g.FillPolygon(brush, new[] { P(0.45f, 0.05f), P(0.45f, 0.95f), P(0.85f, 0.5f) });
                    break;
                case IconKind.Camera:
                    g.FillRectangle(brush, R(0.30f, 0.05f, 0.40f, 0.25f));
                    g.FillRectangle(brush, R(0f, 0.22f, 1f, 0.70f));
                    using (SolidBrush hole = new(back))
                    {
                        g.FillEllipse(hole, R(0.30f, 0.34f, 0.40f, 0.40f));
                    }
                    g.FillEllipse(brush, R(0.38f, 0.42f, 0.24f, 0.24f));
                    break;
                case IconKind.Bookmark:   // a ribbon with a notch at the bottom
                    g.FillPolygon(brush, new[] { P(0.17f, 0f), P(0.83f, 0f), P(0.83f, 1f), P(0.5f, 0.70f), P(0.17f, 1f) });
                    break;
                case IconKind.Volume:
                case IconKind.Muted:
                    g.FillPolygon(brush, new[] { P(0f, 0.34f), P(0.22f, 0.34f), P(0.52f, 0.08f), P(0.52f, 0.92f), P(0.22f, 0.66f), P(0f, 0.66f) });
                    if (_icon == IconKind.Volume)
                    {
                        g.DrawArc(pen, R(0.40f, 0.30f, 0.40f, 0.40f), -50, 100);
                        g.DrawArc(pen, R(0.30f, 0.12f, 0.70f, 0.76f), -50, 100);
                    }
                    else
                    {
                        g.DrawLine(pen, P(0.68f, 0.34f), P(1f, 0.66f));
                        g.DrawLine(pen, P(1f, 0.34f), P(0.68f, 0.66f));
                    }
                    break;
            }

            if (Focused && ShowFocusCues)
            {
                using Pen focus = new(Color.FromArgb(120, fore)) { DashStyle = DashStyle.Dot };
                g.DrawRectangle(focus, 2, 2, Width - 5, Height - 5);
            }
        }

        private static Color Lighten(Color c, float amount)
        {
            return Color.FromArgb(c.R + (int)((255 - c.R) * amount), c.G + (int)((255 - c.G) * amount), c.B + (int)((255 - c.B) * amount));
        }
    }

    // Colors for the right-click menu in the dark theme.
    internal sealed class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color Back = Color.FromArgb(43, 43, 43);
        private static readonly Color Hover = Color.FromArgb(70, 70, 70);
        private static readonly Color Line = Color.FromArgb(80, 80, 80);

        public override Color ToolStripDropDownBackground => Back;
        public override Color ImageMarginGradientBegin => Back;
        public override Color ImageMarginGradientMiddle => Back;
        public override Color ImageMarginGradientEnd => Back;
        public override Color MenuBorder => Line;
        public override Color MenuItemBorder => Hover;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Hover;
        public override Color MenuItemPressedGradientMiddle => Hover;
        public override Color MenuItemPressedGradientEnd => Hover;
        public override Color SeparatorDark => Line;
        public override Color SeparatorLight => Line;
        public override Color CheckBackground => Hover;
        public override Color CheckSelectedBackground => Hover;
        public override Color CheckPressedBackground => Hover;
    }
}
