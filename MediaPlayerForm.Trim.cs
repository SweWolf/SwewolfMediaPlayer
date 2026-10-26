// MediaPlayerForm.Trim.cs: the trim feature of MediaPlayerForm (the "Trim" group box).
//
// The group box is hidden at start and is never remembered: the right-click menu on the control bar
// shows it. The user types a start and end time (or takes the player's position with the "Now"
// buttons) and fade lengths. "Preview" plays that part with the fades (in the volume only), and
// "Trim" asks whether to replace the file or save a new one, and uses the sibling program
// SplitMediaFiles either way (arguments -i, -ss, -to, -fadein, -fadeout). To replace, it adds
// -overwrite and waits for it (exit code 0 = done, -1 = failed, -2 = canceled), and the file is
// released meanwhile. For a new file SplitMediaFiles opens its window with the values filled in
// and the player does not wait.
//
// To reuse it: copy this file together with the three files named in MediaPlayerForm.cs.
//
// History:
//   2026-10-11  First version.
//   2026-10-12  Removed the development path; when SplitMediaFiles is not found, the user can locate
//               it or open its download page.
//   2026-10-12  The Now buttons use the position the seek bar was dragged to while stopped.
//   2026-10-12  Trim asks: Replace the File (-overwrite, as before) or Save as a New File (SplitMediaFiles
//               opens with the values filled in and the player does not wait).
//   2026-10-26  A small X (CloseXButton.cs) in the upper right corner closes the group box; Preview and Trim moved 28 px to the left.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private const string SplitMediaFilesExe = "SplitMediaFiles.exe";
        private const string SplitMediaFilesDownloadUrl = "https://github.com/SweWolf/SplitMediaFiles/releases/latest";

        // Not remembered between runs, on purpose.
        private bool _trimVisible;
        private bool _trimming;   // SplitMediaFiles is running: the file is not open

        private bool _previewing;
        private long _previewStartMs;
        private long _previewEndMs;
        private double _previewFadeIn;
        private double _previewFadeOut;
        private long _previewStartedTicks;
        private System.Windows.Forms.Timer? _previewTimer;

        // What the user typed, checked and converted. Start and end are in seconds; the "has"
        // flags say whether the user filled them in (an empty field is not sent to SplitMediaFiles).
        private readonly record struct TrimRequest(
            bool HasStart, double Start, bool HasEnd, double End, double FadeIn, double FadeOut);

        // ---- Showing and hiding the group box ----

        // The "More Options" button opens the menu of the control bar (the same one as the right-click)
        // above itself, with its right edge at the right edge of the button.
        private void btnMore_Click(object? sender, EventArgs e)
        {
            menuControls.Show(btnMore.PointToScreen(new Point(btnMore.Width, 0)), ToolStripDropDownDirection.AboveLeft);
        }

        private void menuControls_Opening(object? sender, CancelEventArgs e)
        {
            // A stream has no file to trim, and neither has a file that cannot be searched (SID music).
            mnuShowTrim.Enabled = CanSeek && !_trimming;
            mnuShowTrim.Checked = _trimVisible;
            mnuShowControlPanel.Checked = _panelVisible;
        }

        private void mnuShowTrim_Click(object? sender, EventArgs e)
        {
            SetTrimVisible(!_trimVisible);
        }

        private void btnCloseTrim_Click(object? sender, EventArgs e)
        {
            SetTrimVisible(false);
        }

        private void SetTrimVisible(bool visible)
        {
            if (visible == _trimVisible)
            {
                return;
            }

            if (!visible)
            {
                EndPreview(false);
            }
            _trimVisible = visible;
            grpTrim.Visible = visible && !_fullScreen;
            UpdateMinimumSize();

            // Keep the video as large as it was: the window grows and shrinks with the group box.
            if (!_fullScreen && WindowState == FormWindowState.Normal)
            {
                int height = Height + (visible ? grpTrim.Height : -grpTrim.Height);
                Height = Math.Min(height, Screen.FromControl(this).WorkingArea.Height);
            }
        }

        // The window may be made much smaller than the designer size, but not so small that the
        // control bar (and the trim group box, when it is shown) no longer fits.
        private void UpdateMinimumSize()
        {
            int width = LogicalToDeviceUnits(_trimVisible ? 880 : 840);
            int height = LogicalToDeviceUnits(200) + (_trimVisible ? grpTrim.Height : 0) + (_panelVisible ? grpControlPanel.Height : 0);
            MinimumSize = SizeFromClientSize(new Size(width, height));
        }

        // ---- Start and end ("Now" buttons) ----

        private void btnStartNow_Click(object? sender, EventArgs e)
        {
            TakeCurrentTime(txtStartAt);
        }

        private void btnEndNow_Click(object? sender, EventArgs e)
        {
            TakeCurrentTime(txtEndAt);
        }

        private void TakeCurrentTime(TextBox box)
        {
            if (_player == null)
            {
                return;
            }

            // The same position that Save Frame uses.
            long timeMs = _pendingSeekMs >= 0 ? _pendingSeekMs
                : IsAtEnd() ? _lengthMs
                : _paused && _stepTimeMs >= 0 ? _stepTimeMs
                : _player.Time;
            box.Text = FormatTime(timeMs, true);
        }

        // ---- Reading and checking the fields ----

        // Accepts what SplitMediaFiles accepts: 1:23:45, 2:07, 90, 7.5 (a point or a comma) or 45%.
        private bool TryParseTime(string text, out double seconds)
        {
            seconds = 0;
            text = text.Trim().Replace(',', '.');
            if (text.Length == 0)
            {
                return false;
            }

            if (text.EndsWith('%'))
            {
                if (_lengthMs <= 0
                    || !double.TryParse(text[..^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double percent)
                    || percent > 100)
                {
                    return false;
                }
                seconds = _lengthMs / 1000.0 * percent / 100;
                return true;
            }

            string[] parts = text.Split(':');
            if (parts.Length > 3)
            {
                return false;
            }

            double total = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                // Only the last part (the seconds) may have a fraction.
                NumberStyles style = i == parts.Length - 1 ? NumberStyles.AllowDecimalPoint : NumberStyles.None;
                if (!double.TryParse(parts[i], style, CultureInfo.InvariantCulture, out double value))
                {
                    return false;
                }
                total = total * 60 + value;
            }
            seconds = total;
            return true;
        }

        private static bool TryParseSeconds(string text, out double seconds)
        {
            text = text.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out seconds);
        }

        private void ShowTrimProblem(string message, Control field)
        {
            MessageBox.Show(this, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            field.Focus();
        }

        // Checks the fields. On a problem it says so, focuses the field and returns false.
        private bool TryReadTrim(out TrimRequest request)
        {
            request = default;
            const string timeHelp = "Use a time such as 1:23:45, 2:07, 90, 7.5 or 45%.";

            if (_lengthMs <= 0)
            {
                MessageBox.Show(this, "The length of the file is not known yet. Wait until the file is playing.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            double length = _lengthMs / 1000.0;

            bool hasStart = txtStartAt.Text.Trim().Length > 0;
            double start = 0;
            if (hasStart && !TryParseTime(txtStartAt.Text, out start))
            {
                ShowTrimProblem($"The start time \"{txtStartAt.Text.Trim()}\" is not valid. {timeHelp}", txtStartAt);
                return false;
            }

            bool hasEnd = txtEndAt.Text.Trim().Length > 0;
            double end = length;
            if (hasEnd && !TryParseTime(txtEndAt.Text, out end))
            {
                ShowTrimProblem($"The end time \"{txtEndAt.Text.Trim()}\" is not valid. {timeHelp}", txtEndAt);
                return false;
            }
            // "Now" at the very end can be a few milliseconds beyond the length.
            if (end > length && end - length < 0.5)
            {
                end = length;
            }
            if (end > length)
            {
                ShowTrimProblem($"The end time is after the end of the file ({FormatTime(_lengthMs, false)}).", txtEndAt);
                return false;
            }
            if (end <= start)
            {
                ShowTrimProblem("The end time must be later than the start time.", hasEnd ? txtEndAt : txtStartAt);
                return false;
            }

            double fadeIn = 0;
            if (!TryParseSeconds(txtFadeIn.Text, out fadeIn))
            {
                ShowTrimProblem("The fade in must be a number of seconds, for example 2 or 3.5. Use 0 for no fade.", txtFadeIn);
                return false;
            }
            double fadeOut = 0;
            if (!TryParseSeconds(txtFadeOut.Text, out fadeOut))
            {
                ShowTrimProblem("The fade out must be a number of seconds, for example 2 or 3.5. Use 0 for no fade.", txtFadeOut);
                return false;
            }
            if (fadeIn + fadeOut > end - start)
            {
                ShowTrimProblem("The fades are longer than the part that is kept.", txtFadeIn);
                return false;
            }

            request = new TrimRequest(hasStart, start, hasEnd, end, fadeIn, fadeOut);
            return true;
        }

        // ---- Preview ----

        private async void btnPreview_Click(object? sender, EventArgs e)
        {
            if (_previewing)
            {
                EndPreview(true);
                return;
            }

            MediaPlayer? player = _player;
            if (player == null || _trimming || _restoringEnd || !TryReadTrim(out TrimRequest request))
            {
                return;
            }

            _previewStartMs = (long)(request.Start * 1000);
            _previewEndMs = (long)(request.End * 1000);
            _previewFadeIn = request.FadeIn;
            _previewFadeOut = request.FadeOut;
            _previewing = true;
            btnPreview.Text = "Stop";

            // An ended or stopped player has to be started again first.
            if (player.State is VLCState.Stopped or VLCState.Ended or VLCState.Error or VLCState.NothingSpecial)
            {
                await Task.Run(() => player.Stop());
                _pendingSeekMs = -1;
                if (_player != player || !_previewing)
                {
                    return;
                }
                player.Play();
                for (int i = 0; i < 60 && !player.IsPlaying; i++)
                {
                    await Task.Delay(50);
                }
                if (_player != player || !_previewing)
                {
                    return;
                }
            }

            ResetSpeed();   // the preview is timed for normal speed
            player.Time = _previewStartMs;
            _stepTimeMs = -1;
            player.SetPause(false);

            _previewStartedTicks = Environment.TickCount64;
            if (_previewTimer == null)
            {
                _previewTimer = new System.Windows.Forms.Timer { Interval = 30 };
                _previewTimer.Tick += (s, a) => PreviewTick();
            }
            _previewTimer.Start();
        }

        // Follows the fades with the volume of the player, and stops at the end time. Stops also
        // when the user pauses or stops the player.
        private void PreviewTick()
        {
            MediaPlayer? player = _player;
            if (player == null || !_previewing)
            {
                EndPreview(false);
                return;
            }

            // Right after the jump to the start the player still reports the old time for a moment.
            bool settling = Environment.TickCount64 - _previewStartedTicks < 600;
            long timeMs = settling ? _previewStartMs : player.Time;
            if (!settling && (timeMs >= _previewEndMs || !player.IsPlaying))
            {
                EndPreview(timeMs >= _previewEndMs);
                return;
            }

            double factor = 1;
            if (_previewFadeIn > 0)
            {
                factor = Math.Min(factor, Math.Clamp((timeMs - _previewStartMs) / 1000.0 / _previewFadeIn, 0, 1));
            }
            if (_previewFadeOut > 0)
            {
                factor = Math.Min(factor, Math.Clamp((_previewEndMs - timeMs) / 1000.0 / _previewFadeOut, 0, 1));
            }
            player.Volume = (int)Math.Round(trkVolume.Value * factor);
        }

        // Ends the preview and puts the volume back. With pause, the player is paused as well.
        private void EndPreview(bool pause)
        {
            if (!_previewing)
            {
                return;
            }

            _previewing = false;
            _previewTimer?.Stop();
            btnPreview.Text = "Preview";

            MediaPlayer? player = _player;
            if (player != null)
            {
                player.Volume = trkVolume.Value;
                if (pause && player.IsPlaying)
                {
                    player.SetPause(true);
                }
            }
        }

        // ---- Trim with SplitMediaFiles ----

        private async void btnTrim_Click(object? sender, EventArgs e)
        {
            MediaPlayer? player = _player;
            if (player == null || _trimming || !CanSeek || _restoringEnd || !TryReadTrim(out TrimRequest request))
            {
                return;
            }

            if (!File.Exists(_filePath))
            {
                MessageBox.Show(this, $"The file \"{_filePath}\" does not exist.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!request.HasStart && !request.HasEnd && request.FadeIn <= 0 && request.FadeOut <= 0)
            {
                ShowTrimProblem("There is nothing to trim. Fill in Start at, End at, or a fade.", txtStartAt);
                return;
            }

            string? exe = FindSplitMediaFiles();
            if (exe == null)
            {
                return;   // the user did not pick the program
            }

            string summary = $"Start: {(request.HasStart ? FormatTime((long)(request.Start * 1000), true) : "the beginning")}\n"
                + $"End: {(request.HasEnd ? FormatTime((long)(request.End * 1000), true) : "the end")}\n"
                + $"Fade in: {(request.FadeIn > 0 ? FormatSeconds(request.FadeIn) + " s" : "none")}\n"
                + $"Fade out: {(request.FadeOut > 0 ? FormatSeconds(request.FadeOut) + " s" : "none")}\n\n"
                + "Replace the File: the file is trimmed in place. SplitMediaFiles keeps the original in its folder "
                + "for original files (\"_old\" next to the file, unless it is set otherwise there). The player lets go "
                + "of the file meanwhile and opens it again afterwards.\n\n"
                + "Save as a New File: SplitMediaFiles opens with these values filled in. You choose the name and "
                + "the folder there, and start it yourself. The original file is not changed.";
            TaskDialogButton replaceButton = new("Replace the File");
            TaskDialogButton newFileButton = new("Save as a New File");
            TaskDialogPage choicePage = new()
            {
                Caption = AppName,
                Heading = $"Trim \"{Path.GetFileName(_filePath)}\"?",
                Text = summary,
                Icon = TaskDialogIcon.Information,
                Buttons = { replaceButton, newFileButton, TaskDialogButton.Cancel },
                DefaultButton = newFileButton   // the one that cannot lose anything
            };
            TaskDialogButton choice = TaskDialog.ShowDialog(this, choicePage);
            if (choice == newFileButton)
            {
                // The original is not touched, so the player keeps it open and does not wait.
                try
                {
                    StartSplitMediaFiles(exe, request, false)?.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"SplitMediaFiles could not be started.\n\n{ex.Message}", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            if (choice != replaceButton)
            {
                return;
            }

            // SplitMediaFiles replaces the file, so the player must not have it open.
            EndPreview(false);
            _trimming = true;
            pnlControls.Enabled = false;
            grpTrim.Enabled = false;
            Media? media = _media;
            _media = null;
            await Task.Run(() =>
            {
                player.Stop();
                media?.Dispose();
            });

            int exitCode;
            string? startError = null;
            try
            {
                exitCode = await Task.Run(() => RunSplitMediaFiles(exe, request));
            }
            catch (Exception ex)
            {
                exitCode = int.MinValue;
                startError = ex.Message;
            }

            if (IsDisposed)
            {
                return;
            }

            _trimming = false;
            _pendingSeekMs = -1;
            pnlControls.Enabled = true;
            grpTrim.Enabled = true;
            if (exitCode == 0)
            {
                // The new file is shorter: the old positions mean nothing now.
                txtStartAt.Clear();
                txtEndAt.Clear();
                _frameMs = 0;
                LibraryFileReplaced();
            }
            StartMedia();
            Activate();

            if (startError != null)
            {
                MessageBox.Show(this, $"SplitMediaFiles could not be started.\n\n{startError}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (exitCode == -2)
            {
                MessageBox.Show(this, "The trim was canceled.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (exitCode != 0)
            {
                string log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SweWolfSoftware", "SplitMediaFiles", "Logs", "command-line.log");
                MessageBox.Show(this, $"The file could not be trimmed (SplitMediaFiles exit code {exitCode}).\n\nThe reason is in the log file:\n{log}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Starts SplitMediaFiles in its automatic mode, trimming the file in place, and waits for it.
        // Returns its exit code.
        private int RunSplitMediaFiles(string exe, TrimRequest request)
        {
            using Process process = StartSplitMediaFiles(exe, request, true)
                ?? throw new InvalidOperationException("The process did not start.");
            process.WaitForExit();
            return process.ExitCode;
        }

        // Starts SplitMediaFiles with the trim values. With overwrite (-overwrite) it works by itself
        // and replaces the file; without, its window opens with the values filled in.
        private Process? StartSplitMediaFiles(string exe, TrimRequest request, bool overwrite)
        {
            ProcessStartInfo info = new(exe)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? ""
            };
            info.ArgumentList.Add("-i");
            info.ArgumentList.Add(_filePath);
            if (request.HasStart)
            {
                info.ArgumentList.Add("-ss");
                info.ArgumentList.Add(FormatSeconds(request.Start));
            }
            if (request.HasEnd)
            {
                info.ArgumentList.Add("-to");
                info.ArgumentList.Add(FormatSeconds(request.End));
            }
            if (request.FadeIn > 0)
            {
                info.ArgumentList.Add("-fadein");
                info.ArgumentList.Add(FormatSeconds(request.FadeIn));
            }
            if (request.FadeOut > 0)
            {
                info.ArgumentList.Add("-fadeout");
                info.ArgumentList.Add(FormatSeconds(request.FadeOut));
            }
            if (overwrite)
            {
                info.ArgumentList.Add("-overwrite");
            }

            return Process.Start(info);
        }

        private static string FormatSeconds(double seconds)
        {
            return seconds.ToString("0.###", CultureInfo.InvariantCulture);
        }

        // ---- Finding SplitMediaFiles.exe ----

        private static string SavedPathFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "SplitMediaFiles.txt");

        // Looks next to this program, in ..\SplitMediaFiles, and in the place saved last time. If it is
        // nowhere, the user can point to it (the answer is saved) or open the download page.
        private string? FindSplitMediaFiles()
        {
            string baseDir = AppContext.BaseDirectory;
            List<string> candidates = new() { Path.Combine(baseDir, SplitMediaFilesExe) };

            // One folder up, in a folder named after the program (every program in its own folder).
            candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "SplitMediaFiles", SplitMediaFilesExe)));

            try
            {
                if (File.Exists(SavedPathFile))
                {
                    candidates.Add(File.ReadAllText(SavedPathFile).Trim());
                }
            }
            catch (IOException)
            {
                // No saved path: ask below.
            }

            string? found = candidates.FirstOrDefault(File.Exists);
            if (found != null)
            {
                return found;
            }

            // Not installed, or somewhere else: let the user choose.
            TaskDialogButton locate = new("Locate SplitMediaFiles");
            TaskDialogButton download = new("Open the Download Page");
            TaskDialogPage page = new()
            {
                Caption = AppName,
                Heading = "SplitMediaFiles was not found.",
                Text = "Trimming needs the program SplitMediaFiles. If it is installed, show where it is. "
                    + "If not, download it first (the download page needs a GitHub login while the project is private).",
                Icon = TaskDialogIcon.Warning,
                Buttons = { locate, download, TaskDialogButton.Cancel },
                DefaultButton = locate
            };
            TaskDialogButton answer = TaskDialog.ShowDialog(this, page);
            if (answer == download)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(SplitMediaFilesDownloadUrl) { UseShellExecute = true });
                }
                catch (Win32Exception)
                {
                    // No browser to open: nothing more to do.
                }
                return null;
            }
            if (answer != locate)
            {
                return null;
            }

            using OpenFileDialog dialog = new()
            {
                Title = "Locate SplitMediaFiles",
                Filter = $"SplitMediaFiles|{SplitMediaFilesExe}",
                FileName = SplitMediaFilesExe
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return null;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavedPathFile)!);
                File.WriteAllText(SavedPathFile, dialog.FileName);
            }
            catch (IOException)
            {
                // It only means asking again next time.
            }
            return dialog.FileName;
        }
    }
}
