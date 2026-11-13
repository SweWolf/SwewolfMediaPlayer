// MediaPlayerForm.Record.cs: records the stream that plays (a live TV channel) to a file.
//
// The Record button (red circle, right of Stop; only for streams) starts FFmpeg at once (StreamRecorder.cs), which copies the stream into a
// .ts file in the recording folder (the Windows Videos folder the first time, then the folder of the last recording). While it records, a
// Save As dialog asks for the name of the MP4 file (the usual question when the file exists). A few seconds later the player switches from the
// live stream to the .ts file, so you watch what is being stored; Pause only pauses what you see, the recording goes on. If the player
// reaches the end of the growing file (the recording was slow for a moment) it starts again where it was.
//
// Stop (or the Record button again) ends the recording: FFmpeg closes the .ts file, which is converted to the MP4 file with the name you chose, and
// the .ts file is deleted. If the conversion fails the .ts file is kept and its place is shown. If the stream ends by itself the recording is
// saved in the same way. Closing the window while it records asks first. While it records, other files and channels cannot be opened.
// A recording leaves no trace in the library (no read mark, no resume point). The seek bar and skip buttons are off while the .ts file plays.
//
// History:
//   2026-11-12  First version.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private IconButton? btnRecord;
        private StreamRecorder? _recorder;          // the recording that runs, null = none
        private string? _recordTarget;              // the MP4 file the user chose, null until the dialog is answered
        private bool _recorderEndedEarly;           // FFmpeg ended while the name dialog was still open
        private bool _recordFinishing;              // stopping and converting is going on
        private bool _recordShown;                  // what Visible of the button was set to (the getter says false while the control bar is hidden)
        private double? _nextStartSeconds;          // the next media of the .ts file starts here (after its end was reached)
        private long _recLastTimeMs;                // where the .ts file played last
        private System.Windows.Forms.Timer? _recWatchTimer;

        // The .ts file of the recording is what plays now.
        private bool IsRecordingPlayback => _recorder != null && string.Equals(_filePath, _recorder.TsPath, StringComparison.OrdinalIgnoreCase);

        private void InitRecordButton()
        {
            btnRecord = new IconButton
            {
                BackColor = btnStop.BackColor,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(225, 70, 70),
                Icon = IconKind.Record,
                Name = "btnRecord",
                Size = btnStop.Size,
                Top = btnStop.Top,
                TabStop = false,
                UseVisualStyleBackColor = false,
                Visible = false,
            };
            toolTip.SetToolTip(btnRecord, "Record (saves the stream to a file)");
            btnRecord.Click += async (_, _) => await RecordClickAsync();
            pnlControls.Controls.Add(btnRecord);
        }

        // Shown for a stream that plays (not for the list itself), and while a recording runs.
        private void UpdateRecordButton()
        {
            if (btnRecord == null)
            {
                return;
            }

            bool channelPlays = _channels == null || (_channelIndex >= 0 && _channelIndex < _channels.Count && _channels[_channelIndex].Url == _filePath);
            _recordShown = _recorder != null || (_isStream && channelPlays);
            btnRecord.Visible = _recordShown;
            btnRecord.BackColor = _recorder != null ? Color.FromArgb(110, 30, 30) : btnStop.BackColor;
            toolTip.SetToolTip(btnRecord, _recorder != null ? "Stop Recording and Save" : "Record (saves the stream to a file)");
        }

        private async Task RecordClickAsync()
        {
            if (_recorder != null)
            {
                // The Record button toggles: it ends the recording and goes back to the live stream. (Stop saves it and stops everything.)
                await FinishRecordingAsync(true, resumeLive: true);
                return;
            }
            if (_player == null || !_isStream || !_recordShown || _restoringEnd)
            {
                return;
            }

            string? ffmpeg = FfmpegPath.Value;
            if (ffmpeg == null)
            {
                MessageBox.Show(this, "FFmpeg was not found, so the stream cannot be recorded.\n\nPut ffmpeg.exe in the program folder or in a folder in the PATH.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string url = _filePath;
            string folder = RecordFolder();
            string ts = Path.Combine(folder, $"Recording {DateTime.Now:yyyy-MM-dd HH-mm-ss}.ts");
            string suggestion = $"{SafeFileName(StreamName(url))} {DateTime.Now:yyyy-MM-dd HH-mm}";

            RecLog($"Record clicked: {url}, .ts file {ts}");
            var recorder = new StreamRecorder(ffmpeg, url, url == _channelUserAgentFor ? _channelUserAgent : null, ts);
            recorder.Exited += () => Post(OnRecorderExited);
            // Starting FFmpeg takes half a second: not on the UI thread.
            string error = "";
            bool started = await Task.Run(() =>
            {
                bool ok = recorder.Start(out string message);
                error = message;
                return ok;
            });
            if (!started)
            {
                RecLog($"FFmpeg could not be started: {error}");
                recorder.Dispose();
                MessageBox.Show(this, $"The recording could not be started.\n\n{error}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _recorder = recorder;
            _recordTarget = null;
            _recorderEndedEarly = false;
            _recordFinishing = false;
            UpdateRecordButton();
            RecLog("FFmpeg started, the name dialog is shown");

            // The recording runs while the question is open.
            string? target = AskRecordingName(folder, suggestion);
            if (target == null)
            {
                RecLog("The name dialog was canceled: the recording is discarded");
                await FinishRecordingAsync(true, discard: true);   // Cancel: no recording
                return;
            }

            _recordTarget = target;
            RecLog($"Name chosen: {target}, the .ts file is {RecAge(recorder)} s old, {RecSize(recorder.TsPath):N0} bytes");
            RememberRecordFolder(Path.GetDirectoryName(target));
            if (_recorderEndedEarly)
            {
                RecLog("FFmpeg ended while the dialog was open");
                await FinishRecordingAsync(false);
                return;
            }

            _recWatchTimer?.Dispose();
            _recWatchTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _recWatchTimer.Tick += (_, _) => CheckRecordingReady();
            _recWatchTimer.Start();
        }

        // How much of the stream is in the .ts file that can be played: the time since the recording started, less a few seconds (FFmpeg writes its
        // file in large pieces and needs a moment to connect), so a click at the right end of the seek bar lands where there is something to play.
        private long RecordedMs()
        {
            return _recorder == null ? 0 : Math.Max(0, (long)(DateTime.Now - _recorder.StartedAt).TotalMilliseconds - 4000);
        }

        // The usual Save As question: the dialog asks by itself what to do with a name that exists (replace it, or choose another name).
        private string? AskRecordingName(string folder, string suggestion)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Recording As",
                Filter = "MP4 Video|*.mp4",
                DefaultExt = "mp4",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = suggestion,
                InitialDirectory = folder,
            };
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        }

        // Some seconds of the stream are on disk: watch the file instead of the live stream.
        private void CheckRecordingReady()
        {
            StreamRecorder? recorder = _recorder;
            if (recorder == null || _recordFinishing)
            {
                _recWatchTimer?.Stop();
                return;
            }
            // A .ts file that is too short is not worth opening: the player needs some seconds of picture and sound to start from.
            if (RecAge(recorder) < MinRecordSecondsBeforeWatching)
            {
                return;
            }

            long size = RecSize(recorder.TsPath);
            if (size < MinRecordBytesBeforeWatching)
            {
                return;   // not enough yet (or nothing is coming: the stream is dead, and FFmpeg ends by itself)
            }

            _recWatchTimer?.Stop();
            RecLog($"Switching to the .ts file: {RecAge(recorder)} s old, {size:N0} bytes");
            _ = OpenAnotherAsync(recorder.TsPath, "", false);
        }

        private const int MinRecordSecondsBeforeWatching = 8;
        private const long MinRecordBytesBeforeWatching = 1_000_000;

        private static void RecLog(string message)
        {
            OpenTimingLog.Write("REC: " + message);
        }

        private static int RecAge(StreamRecorder recorder)
        {
            return (int)(DateTime.Now - recorder.StartedAt).TotalSeconds;
        }

        // The real size of the file while FFmpeg is writing it (the directory entry can be behind, so ask the open file).
        private static long RecSize(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                return stream.Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return 0;
            }
        }

        // FFmpeg ended by itself: the stream ended, or it was not possible to record it.
        private void OnRecorderExited()
        {
            if (_recorder == null || _recordFinishing)
            {
                return;
            }
            if (_recordTarget == null)
            {
                _recorderEndedEarly = true;   // the name dialog is open: it is dealt with when it closes
                return;
            }
            _ = FinishRecordingAsync(false);
        }

        // Ends the recording: the player lets go of the .ts file, FFmpeg closes it, and it becomes the MP4 file. discard: no file is made.
        // Whatever goes wrong, the recording is over afterwards (so Stop and Close never get stuck), and the .ts file is kept.
        // resumeLive: afterwards the live stream plays again (the message stays on the screen for a few seconds first), instead of the player being stopped.
        private async Task FinishRecordingAsync(bool byUser, bool discard = false, bool resumeLive = false)
        {
            StreamRecorder? recorder = _recorder;
            if (recorder == null || _recordFinishing)
            {
                return;
            }

            try
            {
                await FinishRecordingCoreAsync(recorder, byUser, discard, resumeLive);
            }
            catch (Exception ex)
            {
                RecLog($"Finishing the recording failed: {ex}");
                recorder.Dispose();
                _recorder = null;
                _recordTarget = null;
                _recordFinishing = false;
                if (!IsDisposed)
                {
                    UpdateRecordButton();
                    MessageBox.Show(this, $"The recording could not be finished.\n\n{ex.Message}\n\nThe .ts file may still be there:\n{recorder.TsPath}", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private async Task FinishRecordingCoreAsync(StreamRecorder recorder, bool byUser, bool discard, bool resumeLive)
        {
            _recordFinishing = true;
            _recWatchTimer?.Stop();
            RecLog($"Finishing the recording (by the user: {byUser}, discard: {discard}), the .ts file is {RecSize(recorder.TsPath):N0} bytes");
            string? target = discard ? null : _recordTarget;
            string url = recorder.Url;
            string ts = recorder.TsPath;

            MediaPlayer? player = _player;
            if (player != null)
            {
                _tuneAdvanceAtTicks = 0;
                EndPreview(false);
                // The player lets go of the .ts file. LibVLC is not waited for longer than 8 seconds: a stuck Stop must not keep the recording open.
                Task stopping = StopPlayerTask(player);
                if (await Task.WhenAny(stopping, Task.Delay(8000)) != stopping)
                {
                    RecLog("The player did not stop within 8 seconds; going on without it");
                }
            }

            ShowVideoMessage(discard ? "Canceling the recording..." : "Finishing the recording...");
            await recorder.StopAsync();
            RecLog("FFmpeg has stopped");

            string message;
            long size = RecSize(ts);
            if (target == null || size == 0)
            {
                StreamRecorder.TryDelete(ts);
                message = discard ? "The recording was canceled."
                    : "Nothing was recorded." + (recorder.LastMessage.Length > 0 ? $"\n\n{recorder.LastMessage}" : "");
            }
            else
            {
                ShowVideoMessage("Converting the recording to MP4...");
                RecLog($"Converting {size:N0} bytes to {target}");
                string? problem = await StreamRecorder.ConvertToMp4Async(FfmpegPath.Value ?? "ffmpeg", ts, target);
                RecLog(problem == null ? "Converted" : $"The conversion failed: {problem}");
                if (problem == null)
                {
                    StreamRecorder.TryDelete(ts);
                    message = (byUser ? "" : "The stream ended.\n") + $"The recording was saved:\n{target}";
                }
                else
                {
                    message = $"The recording could not be converted to MP4.\n\n{problem}\n\nThe .ts file was kept:\n{ts}";
                    if (!IsDisposed)
                    {
                        MessageBox.Show(this, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            recorder.Dispose();
            _recorder = null;
            _recordTarget = null;
            _recordFinishing = false;
            if (IsDisposed)
            {
                return;
            }

            // Back to the stream.
            _filePath = url;
            _isStream = TryGetStreamUri(url, out _);
            ApplySeekState();
            Text = TitleBase();
            if (resumeLive && _player != null && _activeLib != null)
            {
                // It plays live again at once; the message stays over it for 4 seconds (see HideLoading).
                Media? old = _media;
                _media = null;
                ShowVideoMessage(message);
                _holdMessageUntilTicks = Environment.TickCount64 + 4000;
                StartMedia();
                if (old != null)
                {
                    _ = Task.Run(old.Dispose);
                }
                return;
            }

            // Stopped: Play watches it live again. The player must hold the stream as its media now (it still has the .ts file, which is gone).
            if (_player != null && _activeLib != null)
            {
                Media? old = _media;
                _media = CreateMedia(_activeLib, url, true);
                _player.Media = _media;
                if (old != null)
                {
                    _ = Task.Run(old.Dispose);
                }
            }
            ShowStopped();
            ShowVideoMessage(message);
        }

        // The player reached the end of the growing .ts file (the recording stood still for a moment): start again where it was.
        private async Task ReopenRecordingAsync()
        {
            await Task.Delay(1500);
            StreamRecorder? recorder = _recorder;
            if (recorder == null || _recordFinishing || IsDisposed || !IsRecordingPlayback)
            {
                return;
            }

            _nextStartSeconds = Math.Max(0, _recLastTimeMs - 500) / 1000.0;
            await OpenAnotherAsync(recorder.TsPath, "", false);
        }

        // Closing the window while it records: ask. Returns true when the closing is canceled.
        private bool CancelCloseWhileRecording()
        {
            if (_recorder == null)
            {
                return false;
            }
            if (!_recordFinishing && MessageBox.Show(this, "A recording is running.\n\nStop it and save the file?", AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                _ = FinishAndCloseAsync();
            }
            return true;
        }

        private async Task FinishAndCloseAsync()
        {
            await FinishRecordingAsync(true, discard: _recordTarget == null);
            if (!IsDisposed)
            {
                Close();
            }
        }

        // A stream or a file that is not the recording cannot be opened while it records: it would have to stop the .ts file that plays.
        private bool BlockedByRecording(string path)
        {
            if (_recorder == null || string.Equals(path, _recorder.TsPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            MessageBox.Show(this, "A recording is running.\n\nClick Stop to save it first.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        // The folder of the last recording, else the Windows Videos folder (else Documents).
        private string RecordFolder()
        {
            string folder = AppSettings.Load().RecordFolder;
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                return folder;
            }
            folder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            return Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private static void RememberRecordFolder(string? folder)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                AppSettings settings = AppSettings.Load();   // from a fresh copy, so nothing else in the file is overwritten
                settings.RecordFolder = folder;
                settings.Save();
            }
        }

        // The name of the channel that plays, else the host of the address.
        private string StreamName(string url)
        {
            if (_channels != null && _channelIndex >= 0 && _channelIndex < _channels.Count && _channels[_channelIndex].Url == url)
            {
                return _channels[_channelIndex].Name;
            }
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.Host : "Recording";
        }

        private static string SafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            name = name.Trim().TrimEnd('.');
            return name.Length > 0 ? name : "Recording";
        }
    }
}
