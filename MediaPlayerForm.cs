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

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm : Form, IMessageFilter
    {
        // Caption for message boxes. Change it when copying the form to another project.
        private const string AppName = "SweWolf Media Player";

        // LibVLC is loaded once per process and shared by all player windows.
        private static LibVLC? _libVlc;

        private readonly string _filePath;
        private MediaPlayer? _player;
        private Media? _media;
        private long _lengthMs;
        private bool _seeking;
        private string? _lastSaveFolder;
        private bool _paused;
        private long _stepTimeMs = -1;   // tracked position while stepping frames, -1 = not stepping
        private double _frameMs;         // length of one frame, 0 = not known yet
        private bool _muted;
        private bool _fullScreen;
        private FormWindowState _windowStateBeforeFullScreen;

        public MediaPlayerForm(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            Text = Path.GetFileName(filePath);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // The window may be made much smaller than the designer size, but not so small
            // that the control bar no longer fits.
            MinimumSize = SizeFromClientSize(new Size(LogicalToDeviceUnits(800), LogicalToDeviceUnits(200)));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_libVlc == null)
            {
                Core.Initialize();
                _libVlc = new LibVLC();
            }

            _player = new MediaPlayer(_libVlc);
            _player.Playing += (s, a) => Post(() =>
            {
                _paused = false;
                _stepTimeMs = -1;
                btnPlayPause.Text = "Pause";
            });
            _player.Paused += (s, a) => Post(() =>
            {
                _paused = true;
                btnPlayPause.Text = "Play";
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

            _player.Volume = trkVolume.Value;
            _media = new Media(_libVlc, _filePath, FromType.FromPath);
            _player.Play(_media);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            Application.RemoveMessageFilter(this);

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
            if (keyData is Keys.Left or Keys.Right && _paused)
            {
                StepFrame(keyData == Keys.Right ? 1 : -1);
                return true;
            }
            if (keyData == Keys.S)
            {
                SaveFrame();
                return true;
            }
            if (keyData == Keys.F)
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
            if (m.Msg != WM_MOUSEWHEEL || !_paused || ActiveForm != this)
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
            if (_player == null)
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

        private void ShowStopped()
        {
            _paused = false;
            _stepTimeMs = -1;
            btnPlayPause.Text = "Play";
            if (!_seeking)
            {
                trkSeek.Value = 0;
            }
            UpdateTime(0);
        }

        private void UpdateTime(long timeMs)
        {
            lblTime.Text = $"{FormatTime(timeMs, _paused)} / {FormatTime(_lengthMs, false)}";
            if (!_seeking && _lengthMs > 0)
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
                FileName = $"{Path.GetFileNameWithoutExtension(_filePath)}_{stamp}.png",
                InitialDirectory = _lastSaveFolder ?? Path.GetDirectoryName(Path.GetFullPath(_filePath)),
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

        private void btnMute_Click(object? sender, EventArgs e)
        {
            if (_player != null)
            {
                // Keep our own flag: LibVLC applies the mute asynchronously, so reading
                // _player.Mute right after setting it still returns the old value.
                _muted = !_muted;
                _player.Mute = _muted;
                btnMute.Text = _muted ? "Unmute" : "Mute";
            }
        }

        private void trkVolume_Scroll(object? sender, EventArgs e)
        {
            if (_player != null)
            {
                _player.Volume = trkVolume.Value;
            }
        }
    }
}
