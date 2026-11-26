// MediaPlayerForm.ExactSteps.cs: exact frame stepping while paused ("Frame Stepping when Paused" in the Control Panel).
//
// Normal (VLC): the mouse wheel and the Left / Right keys seek one frame length from a tracked time (StepFrame in MediaPlayerForm.cs).
// LibVLC lands up to a few frames away from the time asked for and never says which frame it shows, so the frame and the time shown are
// only estimates.
//
// Exact (FFmpeg), the default when FFmpeg is found: FFmpeg decodes a window of frames around the position (ExactFrames.cs), with the exact
// time of each one. The first step finds out which frame LibVLC shows (a snapshot is compared with the frames of the window), and from there
// every step shows the previous or the next frame of the window in a picture box over the video, with its exact time in the time label (and
// so for Save Frame, the Now buttons of the Trim group box and the seek bar). Near the edge of the window a new one is loaded around the
// current frame. Play puts LibVLC at that frame (a seek, so it may start a frame or two off) and removes the picture.
//
// Not remembered between runs (decided 2026-11-26). Without FFmpeg, or for audio, GIF, DVD and streams, the setting is not shown / not used.
//
// History:
//   2026-11-26  First version.

using System.Drawing.Imaging;
using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private const double ExactWindowSeconds = 1.2;   // frames this far before and after the position are decoded

        private PictureBox? _exactBox;                   // the picture of the frame, over the video
        private List<ExactFrame>? _exactFrames;          // the window of frames; null = not known (yet) for this file and position
        private int _exactIndex;                         // the frame of the window that is the current one
        private string _exactFile = "";
        private Size _exactSize;                         // the size the frames were decoded at
        private bool _exactShown;                        // the picture box is on the screen (otherwise LibVLC's own picture is)
        private bool _exactBusy;                         // a step is being worked out (the window is loading)
        private bool _recentring;                        // a new window is being loaded in the background
        private int _exactPending;                       // steps asked for while busy
        private int _exactEpoch;                         // counts the times everything was forgotten: work that started before it is dropped

        // Exact stepping can be used for this file (the setting is hidden when this is never true: no FFmpeg).
        private bool ExactStepAvailable => FfmpegPath.Value != null && !_isStream && !IsDvd && !IsGifFile && !IsWebpFile
            && !IsRecordingPlayback && !IsAudioFile(_filePath);

        private bool ExactMode => cboFrameStepping.SelectedIndex == 1 && ExactStepAvailable;

        private void InitFrameStepping()
        {
            bool hasFfmpeg = FfmpegPath.Value != null;
            lblFrameStepping.Visible = cboFrameStepping.Visible = hasFfmpeg;   // without FFmpeg there is only the normal way: nothing to choose
            cboFrameStepping.Items.AddRange(new object[] { "Normal (VLC)", "Exact (FFmpeg)" });
            cboFrameStepping.SelectedIndex = hasFfmpeg ? 1 : 0;
            lblFrameStepping.SizeChanged += (_, _) => PositionFrameStepping();
            videoView.SizeChanged += (_, _) =>
            {
                if (_exactShown && _exactBox != null)
                {
                    _exactBox.Bounds = videoView.Bounds;   // the frames are decoded again at the new size at the next step
                }
            };
            PositionFrameStepping();
        }

        // Right of the "When the Media File Ends" drop-down.
        private void PositionFrameStepping()
        {
            lblFrameStepping.Left = cboWhenEnds.Right + LogicalToDeviceUnits(32);
            cboFrameStepping.Left = lblFrameStepping.Right + LogicalToDeviceUnits(8);
        }

        private void cboFrameStepping_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Back to the normal way: LibVLC is put at the frame that was shown, and shows its own picture again (so the two ways can be compared).
            if (cboFrameStepping.SelectedIndex == 0)
            {
                LeaveExact(true);
            }
        }

        // ---- Stepping ----

        // direction: +1 / -1 for the next / previous frame (a fast wheel gives several at once).
        private async void ExactStepAsync(int steps)
        {
            _exactPending += steps;
            if (_exactBusy)
            {
                return;   // the steps are made when the current work is done
            }

            _exactBusy = true;
            try
            {
                while (_exactPending != 0 && _paused && ExactMode)
                {
                    int wanted = _exactPending;
                    _exactPending = 0;
                    if (!await EnsureExactWindowAsync())
                    {
                        // FFmpeg did not work for this file: the normal way.
                        StepFrameNormal(wanted > 0 ? 1 : -1);
                        continue;
                    }
                    await MoveExactAsync(wanted);
                }
            }
            catch (OperationCanceledException)
            {
                // The user played, sought or opened another file while this was worked out: the steps are dropped.
            }
            finally
            {
                _exactBusy = false;
                _exactPending = 0;
            }
        }

        // Size the frames are decoded at: the picture as large as it can be in the video area, with its own proportions (pixel shape included).
        private Size ExactFrameSize()
        {
            int areaWidth = videoView.ClientSize.Width;
            int areaHeight = videoView.ClientSize.Height;
            double displayWidth = 16, displayHeight = 9;
            try
            {
                if (_media != null)
                {
                    foreach (MediaTrack track in _media.Tracks)
                    {
                        if (track.TrackType == TrackType.Video && track.Data.Video.Width > 0 && track.Data.Video.Height > 0)
                        {
                            uint num = track.Data.Video.SarNum, den = track.Data.Video.SarDen;
                            displayWidth = track.Data.Video.Width * (num > 0 && den > 0 ? (double)num / den : 1);
                            displayHeight = track.Data.Video.Height;
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // The shape is not known: 16:9.
            }

            if (areaWidth < 16 || areaHeight < 16)
            {
                return Size.Empty;
            }
            double scale = Math.Min(areaWidth / displayWidth, areaHeight / displayHeight);
            int width = Math.Max(2, (int)Math.Round(displayWidth * scale) / 2 * 2);
            int height = Math.Max(2, (int)Math.Round(displayHeight * scale) / 2 * 2);
            return new Size(width, height);
        }

        private static int NearestByTime(List<ExactFrame> frames, double timeMs)
        {
            int nearest = 0;
            for (int i = 1; i < frames.Count; i++)
            {
                if (Math.Abs(frames[i].PtsMs - timeMs) < Math.Abs(frames[nearest].PtsMs - timeMs))
                {
                    nearest = i;
                }
            }
            return nearest;
        }

        // Makes sure there is a window of frames for this file and size, and that _exactIndex is the frame that is on the screen: LibVLC's own
        // picture is looked at (a snapshot) to find which frame that is. False when FFmpeg could not give the frames.
        private async Task<bool> EnsureExactWindowAsync()
        {
            string? ffmpeg = FfmpegPath.Value;
            MediaPlayer? player = _player;
            Size size = ExactFrameSize();
            if (ffmpeg == null || player == null || size.IsEmpty)
            {
                return false;
            }

            string file = _filePath;
            int epoch = _exactEpoch;
            bool known = _exactFrames != null && _exactFile == file && _exactIndex >= 0 && _exactIndex < _exactFrames.Count;
            if (known && _exactSize == size)
            {
                return true;
            }

            double estimate;
            byte[]? signature = null;
            if (known)
            {
                estimate = _exactFrames![_exactIndex].PtsMs;   // only the size changed: the current frame is known exactly
            }
            else
            {
                estimate = CurrentTimeMs();
                if (!_exactShown)
                {
                    signature = await SnapshotSignatureAsync(player, size);
                }
            }

            List<ExactFrame>? frames = await Task.Run(() => ExactFrames.Load(ffmpeg, file, estimate / 1000 - ExactWindowSeconds,
                estimate / 1000 + ExactWindowSeconds, size.Width, size.Height, CancellationToken.None));
            if (epoch != _exactEpoch || !_paused || file != _filePath || !ReferenceEquals(player, _player))
            {
                throw new OperationCanceledException();
            }
            if (frames == null || frames.Count == 0)
            {
                return false;
            }

            _exactFrames = frames;
            _exactFile = file;
            _exactSize = size;
            _exactIndex = known || signature == null ? NearestByTime(frames, estimate) : ExactFrames.FindClosest(frames, signature, estimate);
            if (!known && !_exactShown)
            {
                SetExactTime(frames[_exactIndex].PtsMs);   // LibVLC's frame is now known: the label says its real time
            }
            return true;
        }

        // A small grey picture of what LibVLC shows now (the snapshot is a PNG in the temp folder), or null.
        private async Task<byte[]?> SnapshotSignatureAsync(MediaPlayer player, Size size)
        {
            string path = Path.Combine(Path.GetTempPath(), "swp-snap-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                if (!player.TakeSnapshot(0, path, (uint)size.Width, 0))
                {
                    return null;
                }
                for (int i = 0; i < 60 && !File.Exists(path); i++)
                {
                    await Task.Delay(25);
                }
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    await Task.Delay(60);   // the file may still be written
                    try
                    {
                        return await Task.Run(() =>
                        {
                            using FileStream stream = File.OpenRead(path);
                            return ExactFrames.SignatureOf(stream);
                        });
                    }
                    catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
                    {
                        // Not complete yet: try again.
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                    // Left in the temp folder.
                }
            }
        }

        // Shows the frame "steps" frames from the current one. A step beyond the window loads a new window around the current frame first.
        private async Task MoveExactAsync(int steps)
        {
            List<ExactFrame>? frames = _exactFrames;
            if (frames == null)
            {
                return;
            }

            int maximum = Math.Max(1, frames.Count / 2 - 2);
            steps = Math.Clamp(steps, -maximum, maximum);
            int target = _exactIndex + steps;
            if ((target < 0 && frames[0].PtsMs > GetFrameMs() * 0.9) || (target >= frames.Count && !ExactAtEnd(frames)))
            {
                // Past the edge of the window: a new one around the current frame (no frame is skipped: it is found again by its time).
                double current = frames[_exactIndex].PtsMs;
                if (!await ReloadExactWindowAsync(current))
                {
                    return;
                }
                frames = _exactFrames!;
                target = _exactIndex + steps;
            }

            target = Math.Clamp(target, 0, frames.Count - 1);
            ShowExactFrame(target);
        }

        private bool ExactAtEnd(List<ExactFrame> frames)
        {
            return _lengthMs > 0 && frames[^1].PtsMs + GetFrameMs() * 1.5 >= _lengthMs;
        }

        // A new window around the frame at aroundMs (the current frame, known exactly); _exactIndex is that frame in it.
        private async Task<bool> ReloadExactWindowAsync(double aroundMs)
        {
            string? ffmpeg = FfmpegPath.Value;
            Size size = ExactFrameSize();
            string file = _filePath;
            int epoch = _exactEpoch;
            if (ffmpeg == null || size.IsEmpty)
            {
                return false;
            }

            List<ExactFrame>? frames = await Task.Run(() => ExactFrames.Load(ffmpeg, file, aroundMs / 1000 - ExactWindowSeconds,
                aroundMs / 1000 + ExactWindowSeconds, size.Width, size.Height, CancellationToken.None));
            if (epoch != _exactEpoch || file != _filePath)
            {
                throw new OperationCanceledException();
            }
            if (frames == null || frames.Count == 0)
            {
                return false;
            }

            _exactFrames = frames;
            _exactFile = file;
            _exactSize = size;
            _exactIndex = NearestByTime(frames, aroundMs);
            return true;
        }

        private void ShowExactFrame(int index)
        {
            List<ExactFrame>? frames = _exactFrames;
            if (frames == null || index < 0 || index >= frames.Count)
            {
                return;
            }

            _exactIndex = index;
            if (_exactBox == null)
            {
                _exactBox = new PictureBox { BackColor = Color.Black, SizeMode = PictureBoxSizeMode.Zoom, TabStop = false, Visible = false };
                Controls.Add(_exactBox);
                // Just over the video, under everything else (the floating bar, the loading message).
                Controls.SetChildIndex(_exactBox, Controls.GetChildIndex(videoView));
            }

            using MemoryStream stream = new(frames[index].Jpeg);
            using Image decoded = Image.FromStream(stream);
            Image? old = _exactBox.Image;
            _exactBox.Image = new Bitmap(decoded);
            old?.Dispose();
            _exactBox.Bounds = videoView.Bounds;
            _exactBox.Visible = true;
            _exactShown = true;
            SetExactTime(frames[index].PtsMs);

            // Close to the edge of the window: a new one around this frame is loaded meanwhile, so stepping goes on without a wait.
            double frameMs = GetFrameMs();
            if (!_recentring && ((index < 8 && frames[0].PtsMs > frameMs * 0.9) || (index > frames.Count - 9 && !ExactAtEnd(frames))))
            {
                _ = RecentreExactAsync();
            }
        }

        private async Task RecentreExactAsync()
        {
            _recentring = true;
            try
            {
                string? ffmpeg = FfmpegPath.Value;
                Size size = ExactFrameSize();
                List<ExactFrame>? current = _exactFrames;
                if (ffmpeg == null || size.IsEmpty || current == null)
                {
                    return;
                }

                string file = _filePath;
                double around = current[_exactIndex].PtsMs;
                List<ExactFrame>? frames = await Task.Run(() => ExactFrames.Load(ffmpeg, file, around / 1000 - ExactWindowSeconds,
                    around / 1000 + ExactWindowSeconds, size.Width, size.Height, CancellationToken.None));
                if (frames == null || frames.Count == 0 || file != _filePath || _exactFrames != current)
                {
                    return;
                }

                // The user may have stepped meanwhile: the frame that is shown now is found again by its time.
                double now = current[_exactIndex].PtsMs;
                _exactFrames = frames;
                _exactSize = size;
                _exactIndex = NearestByTime(frames, now);
            }
            finally
            {
                _recentring = false;
            }
        }

        // The label, the seek bar and everything that asks for the time of the picture use _stepTimeMs while paused. Cut off, not rounded: a time
        // a little after the frame's would be the next frame for FFmpeg.
        private void SetExactTime(double ptsMs)
        {
            _stepTimeMs = (long)Math.Floor(ptsMs);
            UpdateTime(_stepTimeMs);
        }

        // ---- Leaving exact stepping ----

        // The picture box goes away and what was worked out is forgotten (a seek, Play, Stop, another file, another size...). With seekPlayer, LibVLC is
        // first put at the frame that was shown (when the way is changed, or at Play), so it goes on from there.
        private void LeaveExact(bool seekPlayer)
        {
            if (seekPlayer && _exactShown && _exactFrames != null && _exactIndex >= 0 && _exactIndex < _exactFrames.Count && _player != null)
            {
                _player.Time = (long)Math.Floor(_exactFrames[_exactIndex].PtsMs);
            }

            HideExact();
        }

        private void HideExact()
        {
            _exactEpoch++;
            _exactFrames = null;
            _exactShown = false;
            _exactPending = 0;
            if (_exactBox != null)
            {
                _exactBox.Visible = false;
                _exactBox.Image?.Dispose();
                _exactBox.Image = null;
            }
        }
    }
}
