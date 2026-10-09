// MediaPlayerForm: a stand-alone window that plays one media file with LibVLC.
// It has play/pause, stop, a seek bar with the elapsed and total time, mute and volume.
//
// To reuse it in another project:
//   - Copy MediaPlayerForm.cs, MediaPlayerForm.Designer.cs and MediaPlayerForm.resx.
//   - Change the namespace.
//   - Add the NuGet packages LibVLCSharp.WinForms and VideoLAN.LibVLC.Windows.
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
//   2026-10-09  LibVLC is loaded on a worker thread (PreloadAsync), so the window does not freeze.
//               Alt+Enter toggles full screen like F.
//   2026-10-09  Clicking the video toggled play/pause for a while; removed again. The right-click
//               menu moves to the place of a new right click and closes on a left click elsewhere.
//   2026-10-09  The volume slider shows the volume in percent in a tooltip.
//   2026-10-09  Streams: the time label shows only the elapsed time, the seek bar and frame
//               stepping are off. The muted icon is red.

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
                Core.Initialize();
                _libVlc = new LibVLC();
            });
        }

        private readonly string _filePath;
        private readonly bool _isStream;   // a web address (e.g. an HLS stream) and not a file
        private MediaPlayer? _player;
        private Media? _media;
        private long _lengthMs;
        private bool _seeking;
        private string? _lastSaveFolder;
        private int _lastSubtitleTrack = -1;
        private bool _paused;
        private long _stepTimeMs = -1;   // tracked position while stepping frames, -1 = not stepping
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

        public MediaPlayerForm(string filePath)
        {
            InitializeComponent();
            StyleMenu(menuVideo);
            _filePath = filePath;
            _isStream = TryGetStreamUri(filePath, out Uri? uri);
            Text = _isStream ? uri!.Host : Path.GetFileName(filePath);
            // A stream cannot be seeked or stepped back: the seek bar is of no use.
            trkSeek.Enabled = !_isStream;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // The window may be made much smaller than the designer size, but not so small
            // that the control bar no longer fits.
            MinimumSize = SizeFromClientSize(new Size(LogicalToDeviceUnits(800), LogicalToDeviceUnits(200)));
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                await PreloadAsync();
            }
            catch (Exception ex)
            {
                _loadTask = null;   // allow a new try the next time
                if (!IsDisposed)
                {
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

            _player = new MediaPlayer(_libVlc);
            // Without this the native video window swallows the mouse, and no right-click menu opens.
            _player.EnableMouseInput = false;
            _player.Playing += (s, a) => Post(() =>
            {
                _paused = false;
                _stepTimeMs = -1;
                ShowPlayPause(true);
            });
            _player.Paused += (s, a) => Post(() =>
            {
                _paused = true;
                ShowPlayPause(false);
                if (_player != null)
                {
                    UpdateTime(_player.Time);
                }
            });
            _player.Stopped += (s, a) => Post(ShowStopped);
            _player.EndReached += (s, a) => Post(ShowStopped);
            _player.LengthChanged += (s, a) => Post(() =>
            {
                _lengthMs = a.Length;
                UpdateTime(0);
            });
            // While paused the label is driven by seeking and frame stepping, not by LibVLC.
            _player.TimeChanged += (s, a) => Post(() =>
            {
                if (!_paused)
                {
                    UpdateTime(a.Time);
                }
            });
            videoView.MediaPlayer = _player;
            Application.AddMessageFilter(this);

            // The video window of LibVLC does not pass mouse clicks on to the form, so watch the
            // right mouse button ourselves and open the menu when it is released over the video.
            _rightClickTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _rightClickTimer.Tick += (s, a) =>
            {
                CheckRightClick();
                CheckLeftClick();
                CheckPlayerState();
            };
            _rightClickTimer.Start();

            _player.Volume = trkVolume.Value;
            _media = new Media(_libVlc, _filePath, _isStream ? FromType.FromLocation : FromType.FromPath);
            _player.Play(_media);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            Application.RemoveMessageFilter(this);
            _rightClickTimer?.Dispose();
            _rightClickTimer = null;

            // Detach and release the player on a worker thread: stopping LibVLC from the
            // UI thread can deadlock.
            MediaPlayer? player = _player;
            Media? media = _media;
            _player = null;
            _media = null;
            videoView.MediaPlayer = null;
            if (player != null)
            {
                Task.Run(() =>
                {
                    player.Stop();
                    player.Dispose();
                    media?.Dispose();
                });
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space)
            {
                TogglePlayPause();
                return true;
            }
            if (keyData is Keys.Left or Keys.Right && _paused && !_isStream)
            {
                StepFrame(keyData == Keys.Right ? 1 : -1);
                return true;
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
            if (keyData == Keys.F || keyData == (Keys.Alt | Keys.Enter))
            {
                SetFullScreen(!_fullScreen);
                return true;
            }
            if (keyData == Keys.Escape && _fullScreen)
            {
                SetFullScreen(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // While paused, the mouse wheel anywhere in the window steps one frame (wheel up =
        // forward, wheel down = back), except over the seek bar and the volume slider, which
        // keep using the wheel themselves.
        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL || !_paused || _isStream || ActiveForm != this)
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
            if (_player == null || _isStream)
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

        private void ShowStopped()
        {
            _paused = false;
            _stepTimeMs = -1;
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
            lblTime.Text = _isStream || _lengthMs <= 0
                ? FormatTime(timeMs, _paused)
                : $"{FormatTime(timeMs, _paused)} / {FormatTime(_lengthMs, false)}";
            if (!_seeking && !_isStream && _lengthMs > 0)
            {
                trkSeek.Value = (int)Math.Clamp(timeMs * trkSeek.Maximum / _lengthMs, 0, trkSeek.Maximum);
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
            if (_player == null)
            {
                return;
            }

            if (_player.State is VLCState.Stopped or VLCState.Ended or VLCState.Error or VLCState.NothingSpecial)
            {
                _player.Play();
            }
            else
            {
                _player.SetPause(_player.IsPlaying);
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
        // suggested contains the (approximate) time of the frame.
        private void SaveFrame()
        {
            if (_player == null)
            {
                return;
            }

            long timeMs = _paused && _stepTimeMs >= 0 ? _stepTimeMs : _player.Time;
            TimeSpan t = TimeSpan.FromMilliseconds(Math.Max(timeMs, 0));
            string stamp = $"{(int)t.TotalHours}-{t.Minutes:00}-{t.Seconds:00}-{t.Milliseconds:000}";

            using var dialog = new SaveFileDialog
            {
                Title = "Save Frame as PNG",
                Filter = "PNG Image|*.png",
                DefaultExt = "png",
                FileName = $"{(_isStream ? "stream" : Path.GetFileNameWithoutExtension(_filePath))}_{stamp}.png",
                InitialDirectory = _lastSaveFolder ?? (_isStream ? null : Path.GetDirectoryName(Path.GetFullPath(_filePath))),
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _lastSaveFolder = Path.GetDirectoryName(dialog.FileName);
            if (!_player.TakeSnapshot(0, dialog.FileName, 0, 0))
            {
                MessageBox.Show(this, "The frame could not be saved.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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

            ShowPlayPause(_player.IsPlaying);
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
            if (player == null)
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
                audio.DropDownItems.Add(TrackItem(track.Name, id == currentAudio, () => player.SetAudioTrack(id)));
            }
            audio.Enabled = audio.DropDownItems.Count > 0;
            menuVideo.Items.Add(audio);

            ToolStripMenuItem subtitles = new("Subtitle Track");
            int currentSpu = player.Spu;
            subtitles.DropDownItems.Add(TrackItem("Disable", currentSpu < 0, () =>
            {
                if (player.Spu >= 0)
                {
                    _lastSubtitleTrack = player.Spu;
                }
                player.SetSpu(-1);
            }));
            foreach (TrackDescription track in player.SpuDescription.Where(t => t.Id >= 0))
            {
                int id = track.Id;
                subtitles.DropDownItems.Add(TrackItem(track.Name, id == currentSpu, () => player.SetSpu(id)));
            }
            subtitles.Enabled = subtitles.DropDownItems.Count > 1;
            menuVideo.Items.Add(subtitles);

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

        // Switches the subtitles off, or back on (the track that was on, or else the first one).
        private void ToggleSubtitles()
        {
            if (_player == null)
            {
                return;
            }

            if (_player.Spu >= 0)
            {
                _lastSubtitleTrack = _player.Spu;
                _player.SetSpu(-1);
                return;
            }

            var tracks = _player.SpuDescription;
            if (_lastSubtitleTrack >= 0 && tracks.Any(t => t.Id == _lastSubtitleTrack))
            {
                _player.SetSpu(_lastSubtitleTrack);
            }
            else
            {
                // Track id -1 is the "disabled" entry in the list.
                var first = tracks.FirstOrDefault(t => t.Id >= 0);
                if (first.Id >= 0)
                {
                    _player.SetSpu(first.Id);
                }
            }
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
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
            if (_player != null && _lengthMs > 0)
            {
                long timeMs = _lengthMs * trkSeek.Value / trkSeek.Maximum;
                _player.Time = timeMs;
                _stepTimeMs = timeMs;
                // LibVLC raises no time events while paused, so update the label ourselves.
                UpdateTime(timeMs);
            }
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
                    double ratio = Math.Clamp((x - pad) / (double)channel, 0, 1);
                    Value = Minimum + (int)Math.Round(ratio * (Maximum - Minimum));
                    OnScroll(EventArgs.Empty);
                }
            }
            base.WndProc(ref m);
        }
    }

    // A button that draws its own icon, so it can be filled and take the colors of the dark theme.
    // BackColor is the button color, ForeColor the icon color.
    internal enum IconKind { Play, Pause, Stop, Camera, Volume, Muted }

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
                case IconKind.Camera:
                    g.FillRectangle(brush, R(0.30f, 0.05f, 0.40f, 0.25f));
                    g.FillRectangle(brush, R(0f, 0.22f, 1f, 0.70f));
                    using (SolidBrush hole = new(back))
                    {
                        g.FillEllipse(hole, R(0.30f, 0.34f, 0.40f, 0.40f));
                    }
                    g.FillEllipse(brush, R(0.38f, 0.42f, 0.24f, 0.24f));
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
