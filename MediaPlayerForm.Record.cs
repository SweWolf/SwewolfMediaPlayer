// MediaPlayerForm.Record.cs: records the stream that plays (a live TV channel) to a file.
//
// The Record button (red circle, a red square while it records; right of Stop; only for streams) starts FFmpeg at once (StreamRecorder.cs), which copies the stream into a
// .ts file in the recording folder (the Windows Videos folder the first time, then the folder of the last recording). While it records, a
// recording starts at once, with no question. A few seconds later the player switches from the live stream to the .ts file, so you watch what is being stored; Pause only pauses what you see, the recording goes on. If the player
// reaches the end of the growing file (the recording was slow for a moment) it starts again where it was.
//
// Stop (or the Record button again) ends the recording: FFmpeg closes the .ts file, then a Save As dialog asks for the name of the MP4 file (the usual
// question when the file exists; the name suggested is the channel and the time). Cancel there asks "Discard the recording?": Yes moves the .ts file to the
// Recycle Bin, No goes back to the name. When the stream ends by itself, or the window is closing, nobody is asked and the suggested name is used.
// The .ts file is converted to the MP4 file and then deleted. If the conversion fails the .ts file is kept and its place is shown. If the stream ends by itself the recording is
// saved in the same way. Closing the window while it records asks first. While it records, other files and channels cannot be opened.
// A recording leaves no trace in the library (no read mark, no resume point). The seek bar and skip buttons are off while the .ts file plays.
//
// Some streams do not allow a second connection (an address with a token): FFmpeg is refused at once (404, 403). The quick check at the start sees
// that, and the player records through its own connection instead (RecordMethod, StartPlayerRecordingAsync): the stream is stopped and opened again
// with LibVLC writing the .ts file as it plays (sout duplicate). Then the live picture is what is shown (no pause, no seek bar, "REC" in the title
// and the time label); everything else is the same, the MP4 is made from the .ts file when the recording ends. An address that only works once
// cannot be opened again: the user is told so and the recording is given up.
//
// History:
//   2026-11-12  First version.
//   2026-11-12  Streams that refuse FFmpeg's second connection are recorded through the player's own connection.
//   2026-11-15  The name is asked when the recording ends (not when it starts); Cancel asks whether to discard, and a discarded recording goes to the Recycle Bin.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private IconButton? btnRecord;
        private StreamRecorder? _recorder;          // the recording that runs, null = none
        private string? _recordTarget;              // the MP4 file the user chose when the recording ended, null until then
        private string _recordSuggestion = "";      // the name suggested at the end (channel and time of the start) and used when nobody can be asked
        private bool _recordStarting;               // the recording is being set up (the quick check, the restart through the player)
        private bool _recorderEndedEarly;           // FFmpeg ended while the recording was still being set up
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
            btnRecord.Icon = _recorder != null ? IconKind.RecordStop : IconKind.Record;
            btnRecord.BackColor = _recorder != null ? Color.FromArgb(110, 30, 30) : btnStop.BackColor;
            toolTip.SetToolTip(btnRecord, _recorder != null ? "Stop Recording and Save" : "Record (saves the stream to a file)");
        }

        private async Task RecordClickAsync()
        {
            if (_recorder != null)
            {
                // The Record button toggles: it ends the recording and goes back to the live stream. (Stop saves it and stops everything.)
                // At once, so the click is seen; the message of the saved file replaces it later (it goes by itself after 2 seconds if nothing else has).
                if (!_recordFinishing)
                {
                    ShowVideoMessage(RecordingStoppedText);
                    HideStartedMessage(2000, RecordingStoppedText);
                }
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

            RecLog($"Record clicked: {url}, .ts file {ts}, method {RecordWith}");

            // At once, so the click is seen, and a short break in the picture is nothing wrong (the player may switch to the file or restart the stream).
            ShowVideoMessage(RecordingStartedText);
            StreamRecorder recorder;
            if (RecordWith == RecordMethod.PlayerOnly)
            {
                recorder = StreamRecorder.ForPlayer(url, ts);
            }
            else
            {
                recorder = new StreamRecorder(ffmpeg, url, url == _channelUserAgentFor ? _channelUserAgent : null, ts);
                StreamRecorder own = recorder;
                recorder.Exited += () => Post(() =>
                {
                    if (ReferenceEquals(_recorder, own))   // not the exit of a recorder that was given up (see the fallback below)
                    {
                        OnRecorderExited();
                    }
                });
            }

            // Starting FFmpeg takes half a second: not on the UI thread.
            string error = "";
            StreamRecorder starting = recorder;
            bool started = await Task.Run(() =>
            {
                bool ok = starting.Start(out string message);
                error = message;
                return ok;
            });
            if (!started)
            {
                RecLog($"FFmpeg could not be started: {error}");
                recorder.Dispose();
                HideStartedMessage(0);
                MessageBox.Show(this, $"The recording could not be started.\n\n{error}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _recorder = recorder;
            _recordTarget = null;
            _recordSuggestion = suggestion;
            _recordStarting = true;
            _recorderEndedEarly = false;
            _recordFinishing = false;
            UpdateRecordButton();

            if (!recorder.InPlayer)
            {
                // The quick check: FFmpeg opens its own connection to the stream. If the server refuses it (404, 403, a token that only allows one
                // connection: the player has that one) FFmpeg ends within a second or two with nothing. Then the player records instead.
                if (!await FfmpegGetsDataAsync(recorder))
                {
                    string why = recorder.LastMessage;
                    RecLog($"FFmpeg got no data from the stream: {why}");
                    _recorder = null;
                    _recordStarting = false;
                    recorder.Dispose();
                    StreamRecorder.TryDelete(ts);
                    if (RecordWith == RecordMethod.FfmpegOnly)
                    {
                        UpdateRecordButton();
                        HideStartedMessage(0);
                        MessageBox.Show(this, $"The stream cannot be recorded.\n\n{(why.Length > 0 ? why : "FFmpeg could not read it.")}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    recorder = StreamRecorder.ForPlayer(url, ts);
                    recorder.Start(out _);
                    _recorder = recorder;
                    _recorderEndedEarly = false;   // FFmpeg's refusal was just dealt with: its late "ended" notice must not stop the recording of the player
                    UpdateRecordButton();
                    ShowVideoMessage("The server does not allow a second connection.\nRecording through the player instead...");
                }
            }

            if (recorder.InPlayer && !await StartPlayerRecordingAsync(url, ts))
            {
                return;   // the stream could not be opened again: said, and the recording is given up
            }
            _recordStarting = false;
            RecLog(recorder.InPlayer ? "The player writes the .ts file" : "FFmpeg started");
            try
            {
                UnfinishedStore.Add(ts, StreamName(url), url, _channelListSource, recorder.StartedAt, suggestion);   // stays if the program dies (see UnfinishedRecordings.cs)
            }
            catch (Exception ex)
            {
                RecLog($"Could not write the unfinished entry: {ex.Message}");
            }
            RememberRecordFolder(folder);
            if (_recorderEndedEarly)
            {
                RecLog("FFmpeg ended while the recording was being set up");
                await FinishRecordingAsync(false);
                return;
            }

            // The message stays a little longer, then goes (it is the warning that the picture may break now). The name is asked when the recording ends.
            HideStartedMessage(3000);

            if (recorder.InPlayer)
            {
                return;   // the live picture is what plays (it is the one connection), so there is no switch to the .ts file
            }
            _recWatchTimer?.Dispose();
            _recWatchTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _recWatchTimer.Tick += (_, _) => CheckRecordingReady();
            _recWatchTimer.Start();
        }

        private const string RecordingStartedText = "Recording started\n(short interruptions may occur)";

        // Hides the "Recording started" message after a delay, unless another message has replaced it.
        private const string RecordingStoppedText = "Recording stopped";

        private void HideStartedMessage(int delayMs, string text = RecordingStartedText)
        {
            void Hide()
            {
                if (!IsDisposed && _loadingText == text)
                {
                    HideLoading();
                }
            }
            if (delayMs <= 0)
            {
                Hide();
                return;
            }
            _ = Task.Delay(delayMs).ContinueWith(_ => Post(Hide));
        }

        // How a stream is recorded (2026-11-12). Auto: FFmpeg opens a connection of its own to the stream, and the file can be watched and sought in while
        // it grows. Some servers allow only one connection at a time (a token in the address): FFmpeg is refused, and then the player records through the
        // connection it has itself: the stream is restarted with LibVLC writing the .ts file as it plays (no pause and no seek bar then). FfmpegOnly /
        // PlayerOnly use just one of the two. Change the value to switch; the environment variable SWEWOLF_RECORD_WITH (auto / ffmpeg / player) overrides it.
        private enum RecordMethod { Auto, FfmpegOnly, PlayerOnly }

        private static readonly RecordMethod RecordWith = Environment.GetEnvironmentVariable("SWEWOLF_RECORD_WITH")?.Trim().ToLowerInvariant() switch
        {
            "ffmpeg" => RecordMethod.FfmpegOnly,
            "player" => RecordMethod.PlayerOnly,
            _ => RecordMethod.Auto,
        };

        // The .ts file LibVLC writes while the stream plays (the stream address it is for is the one in _filePath): set while a recording through the player runs.
        private string? _soutPath;

        // True when FFmpeg got data from the stream (or is still connecting after a few seconds: that is no refusal), false when it ended with nothing.
        private async Task<bool> FfmpegGetsDataAsync(StreamRecorder recorder)
        {
            for (int i = 0; i < 30; i++)   // up to 6 seconds
            {
                if (RecSize(recorder.TsPath) >= 64_000)
                {
                    return true;
                }
                if (!recorder.IsRunning)
                {
                    return RecSize(recorder.TsPath) >= 64_000;
                }
                await Task.Delay(200);
            }
            return true;
        }

        // Recording through the player: the stream is stopped and opened again with the option that makes LibVLC write what it plays into the .ts file as
        // well (sout duplicate: one copy to the screen, one to the file). A server that allows one connection at a time lets the new connection in once the
        // old one is closed. Returns false (and gives the recording up, with a message) when the stream does not play again.
        private async Task<bool> StartPlayerRecordingAsync(string url, string ts)
        {
            _soutPath = ts;
            await OpenAnotherAsync(url, url, false);
            for (int i = 0; i < 60; i++)   // up to 12 seconds for the first data
            {
                if (RecSize(ts) >= 64_000)
                {
                    return true;
                }
                await Task.Delay(200);
                if (IsDisposed || _recorder == null)
                {
                    return false;
                }
            }

            RecLog("The player wrote no data in 12 seconds: the recording is given up");
            StreamRecorder? recorder = _recorder;
            _soutPath = null;
            if (recorder != null)
            {
                await FinishRecordingAsync(true, discard: true, resumeLive: false);
            }
            if (!IsDisposed)
            {
                MessageBox.Show(this, "The stream could not be recorded.\n\nIt did not play again when it was opened for recording. The address may have expired or may only work once.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return false;
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

        // The name of the MP4 file, asked when the recording has ended. Cancel in the Save As dialog is not "no": it asks whether to discard the recording,
        // and No (the default) goes back to the name. Yes returns null: the caller moves the .ts file to the Recycle Bin.
        private string? ChooseRecordingName(string folder, string suggestion) => RecordingNaming.Choose(this, AppName, folder, suggestion);

        private static string UniqueTarget(string folder, string name) => RecordingNaming.UniqueTarget(folder, name);

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
            if (_recordStarting)
            {
                _recorderEndedEarly = true;   // still being set up: it is dealt with when that is done
                return;
            }
            _ = FinishRecordingAsync(false);
        }

        // Ends the recording: the player lets go of the .ts file, FFmpeg closes it, and it becomes the MP4 file. discard: no file is made.
        // Whatever goes wrong, the recording is over afterwards (so Stop and Close never get stuck), and the .ts file is kept.
        // resumeLive: afterwards the live stream plays again (the message stays on the screen for a few seconds first), instead of the player being stopped.
        private async Task FinishRecordingAsync(bool byUser, bool discard = false, bool resumeLive = false, bool keepPlaying = false)
        {
            StreamRecorder? recorder = _recorder;
            if (recorder == null || _recordFinishing)
            {
                return;
            }

            try
            {
                await FinishRecordingCoreAsync(recorder, byUser, discard, resumeLive, keepPlaying);
            }
            catch (Exception ex)
            {
                RecLog($"Finishing the recording failed: {ex}");
                recorder.Dispose();
                _soutPath = null;
                _tsPlayedOut = null;
                _recorder = null;
                _recordTarget = null;
                _recordStarting = false;
                _recordFinishing = false;
                if (!IsDisposed)
                {
                    UpdateRecordButton();
                    MessageBox.Show(this, $"The recording could not be finished.\n\n{ex.Message}\n\nThe .ts file may still be there:\n{recorder.TsPath}", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Set while the .ts file plays on after the recording was ended: OnEndReached completes it when the file has played out.
        private TaskCompletionSource? _tsPlayedOut;

        // The player lets go of the .ts file. LibVLC is not waited for longer than 8 seconds: a stuck Stop must not keep the recording open.
        private async Task StopPlayerForRecordingAsync(MediaPlayer player)
        {
            _tuneAdvanceAtTicks = 0;
            EndPreview(false);
            Task stopping = StopPlayerTask(player);
            if (await Task.WhenAny(stopping, Task.Delay(8000)) != stopping)
            {
                RecLog("The player did not stop within 8 seconds; going on without it");
            }
        }

        private async Task FinishRecordingCoreAsync(StreamRecorder recorder, bool byUser, bool discard, bool resumeLive, bool keepPlaying)
        {
            _recordFinishing = true;
            _recWatchTimer?.Stop();
            _soutPath = null;   // the stream that plays again afterwards must not be written to the file
            RecLog($"Finishing the recording (by the user: {byUser}, discard: {discard}), the .ts file is {RecSize(recorder.TsPath):N0} bytes");
            string? target = null;
            bool recycle = false;   // the user threw the recording away: the .ts file goes to the Recycle Bin (discard = given up for a technical reason: deleted)
            string url = recorder.Url;
            string ts = recorder.TsPath;

            MediaPlayer? player = keepPlaying ? null : _player;   // keepPlaying: the live stream is on its own connection and goes on playing
            // Record clicked again: the .ts file goes on playing, as far as it reaches, while FFmpeg closes it and it is converted. The player is stopped
            // when the file has played out or the MP4 file is ready, whichever comes first. (Paused or not playing: stopped at once.)
            bool playOn = player != null && resumeLive && !discard && IsRecordingPlayback && player.IsPlaying;
            if (playOn)
            {
                _tsPlayedOut = new TaskCompletionSource();
                RecLog("The .ts file goes on playing while the recording is finished");
            }
            else if (player != null)
            {
                await StopPlayerForRecordingAsync(player);
            }

            if (!keepPlaying && !playOn)
            {
                ShowVideoMessage(discard ? "Canceling the recording..." : "Finishing the recording...");
            }
            await recorder.StopAsync();
            RecLog("FFmpeg has stopped");

            string message = "";
            long size = RecSize(ts);
            if (!discard && size > 0)
            {
                // The .ts file is complete now: this is the moment to ask for the name. Nobody is asked when the stream ended by itself (the user may be
                // away) or the window is gone: the suggested name is used then.
                if (byUser && !IsDisposed)
                {
                    HideLoading();
                    target = ChooseRecordingName(RecordFolder(), _recordSuggestion);
                    recycle = target == null;
                }
                else
                {
                    target = UniqueTarget(RecordFolder(), _recordSuggestion);
                }
                if (target != null)
                {
                    _recordTarget = target;
                    RememberRecordFolder(Path.GetDirectoryName(target));
                }
                RecLog(target == null ? "The user discarded the recording" : $"Name: {target}");
            }

            if (target == null || size == 0)
            {
                if (recycle)
                {
                    // The player may still be playing the .ts file: it must let go before the file can be moved.
                    if (playOn)
                    {
                        _tsPlayedOut = null;
                        if (player != null && ReferenceEquals(player, _player))
                        {
                            await StopPlayerForRecordingAsync(player);
                        }
                    }
                    message = StreamRecorder.TryRecycle(ts)
                        ? "The recording was discarded.\nThe file is in the Recycle Bin."
                        : $"The recording could not be moved to the Recycle Bin.\n\nThe .ts file was kept:\n{ts}";
                }
                else
                {
                    StreamRecorder.TryDelete(ts);
                    message = discard ? "The recording was canceled."
                        : "Nothing was recorded." + (recorder.LastMessage.Length > 0 ? $"\n\n{recorder.LastMessage}" : "");
                }
            }
            else
            {
                RecLog($"Converting {size:N0} bytes to {target}");
                Task<string?> converting = StreamRecorder.ConvertToMp4Async(FfmpegPath.Value ?? "ffmpeg", ts, target);
                if (playOn)
                {
                    Task playedOut = _tsPlayedOut!.Task;
                    Task first = await Task.WhenAny(converting, playedOut, Task.Delay(60000));   // the minute: a file that never ends must not keep it open
                    RecLog(first == converting ? "The MP4 file is ready before the .ts file has played out" : "The .ts file has played out (or the wait ended)");
                    _tsPlayedOut = null;
                    if (player != null && ReferenceEquals(player, _player))
                    {
                        await StopPlayerForRecordingAsync(player);
                    }
                }
                if (!converting.IsCompleted)
                {
                    ShowVideoMessage("Converting the recording to MP4...");
                }
                string? problem = await converting;
                RecLog(problem == null ? "Converted" : $"The conversion failed: {problem}");
                if (problem == null)
                {
                    StreamRecorder.TryDelete(ts);
                    RememberRecording(target, url, recorder);
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

            if (playOn && _tsPlayedOut != null)
            {
                _tsPlayedOut = null;   // nothing was converted: the player is stopped now
                if (player != null && ReferenceEquals(player, _player))
                {
                    await StopPlayerForRecordingAsync(player);
                }
            }

            recorder.Dispose();
            _recorder = null;
            _recordTarget = null;
            _recordStarting = false;
            _recordFinishing = false;
            UnfinishedStore.ForgetIfGone(ts);   // saved, discarded or nothing recorded; a .ts file that was kept stays in the Unfinished Recordings menu
            if (IsDisposed)
            {
                return;
            }

            if (keepPlaying)
            {
                UpdateRecordButton();   // nothing else changes: the picture was never interrupted
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

        // A recording that is saved goes into the list of My Recordings (RecordingStore), and the library remembers where the file came from: the
        // stream address as its host address and the channel list as its referrer, which the explorer carries along when it renames or moves the file
        // (that is how the list finds the file again, see RecordingStore.Locate). It never writes the library on a drive that has none yet.
        private void RememberRecording(string target, string url, StreamRecorder recorder)
        {
            try
            {
                long size = new FileInfo(target).Length;
                long duration = (long)(DateTime.Now - recorder.StartedAt).TotalMilliseconds;   // used when the file's own length cannot be read
                try
                {
                    using TagLib.File tags = TagLib.File.Create(target);
                    if (tags.Properties.Duration > TimeSpan.Zero)
                    {
                        duration = (long)tags.Properties.Duration.TotalMilliseconds;
                    }
                }
                catch (Exception)
                {
                }

                RecordingStore.Add(new RecordingEntry
                {
                    Path = target,
                    Channel = StreamName(url),
                    Url = url,
                    ListUrl = _channelListSource,
                    RecordedAt = recorder.StartedAt,
                    DurationMs = duration,
                    SizeBytes = size,
                });
                LibraryStore.SetItemInfo(target, url, _channelListSource, create: false);
                RecLog($"Remembered in My Recordings: {target}");
            }
            catch (Exception ex)
            {
                RecLog($"Could not remember the recording: {ex.Message}");   // the recording itself is saved: this is only the list
            }
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
            await FinishRecordingAsync(true);
            if (!IsDisposed)
            {
                Close();
            }
        }

        // A stream or a file that is not the recording cannot be opened while it records: it would have to stop the .ts file that plays.
        private bool BlockedByRecording(string path)
        {
            if (_recorder == null || string.Equals(path, _recorder.TsPath, StringComparison.OrdinalIgnoreCase)
                || (_recorder.InPlayer && string.Equals(path, _recorder.Url, StringComparison.OrdinalIgnoreCase)))   // the player opens its own stream again for the recording
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
