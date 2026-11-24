// MediaPlayerForm.Trim.cs: the Trim/Remove/Split feature of MediaPlayerForm (the "Trim/Remove/Split" group box).
//
// The group box is hidden at start and is never remembered: the right-click menu on the control bar
// shows it. A drop-down list at its start chooses what to do:
//   Trim    keeps the part between "Start at" and "End at" (fades in and out).
//   Remove  cuts the part between "Remove From" and "Remove To" out and joins what is left (both times are
//           required; the fade out ends the part before the cut, the fade in starts the part after it).
//   Split   cuts the file at one or two times into two or three files (no fades).
// The user types the times (or takes the player's position with the "Now" buttons). "Preview" plays the
// result from where the seek bar is, "Quick Preview" only the places where something happens (a few seconds
// around each), both with the fades (in the volume only). In Split mode the preview pauses at every cut.
// "Start" uses the sibling program SplitMediaFiles: -i, -ss, -to, -fadein, -fadeout, and -remove or -split.
// Trim and Remove ask whether to replace the file or save a new one (-overwrite, waited for: exit code
// 0 = done, -1 = failed, -2 = canceled; the file is released meanwhile) or open SplitMediaFiles with the values
// filled in (the player does not wait). Split asks for "Split Now" (-run: SplitMediaFiles makes the files by
// itself next to the original) or "Open in SplitMediaFiles".
// The two small buttons jump to the previous / next time stamp typed in the boxes.
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
//   2026-11-22  Show Trim Features is hidden when SplitMediaFiles is not found (it is not released yet); the Locate / download dialog is gone.
//   2026-11-23  Trim/Remove/Split: a mode list, Remove (-remove) and Split (-split, Split Now = -run), the button is called Start,
//               Quick Preview (QuickPreviewSeconds around each edit, plus the fade), pauses at the cuts when splitting,
//               previous / next time stamp buttons. The group box has two rows.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private const string SplitMediaFilesExe = "SplitMediaFiles.exe";

        // How much of the picture before and after each edit "Quick Preview" plays, in addition to the fade.
        // A constant for now; meant to become a setting.
        private const double QuickPreviewSeconds = 5;

        private enum TrimMode { Trim, Remove, Split }

        // How SplitMediaFiles is started: its window opens with the values filled in; it replaces the file by itself
        // (-overwrite); or it does the work by itself and makes new files next to the original (-run).
        private enum SplitRun { Window, Overwrite, Automatic }

        // Not remembered between runs, on purpose.
        private bool _trimVisible;
        private bool _trimming;   // SplitMediaFiles is running: the file is not open
        private bool _splittingNow;   // a "Split Now" is running (the file stays open)

        // The preview plays a list of segments in turn (jumping from the end of one to the start of the next) and
        // may pause at marks (the cuts of a split) until the user presses Play.
        private readonly record struct PreviewSegment(long StartMs, long EndMs);

        private bool _previewing;
        private bool _previewQuick;
        private TrimMode _previewMode;
        private PreviewSegment[] _previewSegments = [];
        private int _previewSegment;
        private long[] _previewPauses = [];
        private int _previewPauseIndex;
        private bool _previewWaiting;   // paused at a mark: the preview goes on when the user plays
        private long _previewWaitingTicks;
        private long _previewMarkA;   // Trim: where the part starts; Remove: where the removed part starts (ms)
        private long _previewMarkB;   // Trim: where the part ends; Remove: where the removed part ends
        private double _previewFadeIn;
        private double _previewFadeOut;
        private long _previewStartedTicks;
        private long _previewSettleMs;   // the time to assume right after a jump, before the player reports it
        private System.Windows.Forms.Timer? _previewTimer;

        // What the user typed, checked and converted. Times are in seconds; the "has" flags say whether the user
        // filled them in (an empty field is not sent to SplitMediaFiles). Trim: start and end. Remove: both
        // given; the fade in starts the part after the cut, the fade out ends the part before it. Split: the
        // cut times that were given (a 0 is the same as nothing); no fades.
        private readonly record struct TrimRequest(
            TrimMode Mode, bool HasStart, double Start, bool HasEnd, double End, double FadeIn, double FadeOut);

        private TrimMode Mode => (TrimMode)Math.Max(cboTrimMode.SelectedIndex, 0);

        // ---- The mode ----

        private void InitTrimControls()
        {
            cboTrimMode.SelectedIndex = 0;
            ApplyTrimMode();
        }

        private void cboTrimMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyTrimMode();
        }

        // Names the boxes for the mode, hides what the mode does not use and lays the first row out again.
        private void ApplyTrimMode()
        {
            EndPreview(false);
            TrimMode mode = Mode;
            const string forms = "for example 1:23:45, 2:07, 90, 7.5 or 45%";

            switch (mode)
            {
                case TrimMode.Remove:
                    lblStartAt.Text = "Remove From";
                    lblEndAt.Text = "Remove To";
                    toolTip.SetToolTip(txtStartAt, $"Where the part that is removed starts, {forms}. The part before it is kept. Required.");
                    toolTip.SetToolTip(txtEndAt, $"Where the part that is removed ends, {forms}. The part after it is kept. Required.");
                    toolTip.SetToolTip(txtFadeIn, "Length of the fade in at the start of the part after the removed part, in seconds. 0 means no fade.");
                    toolTip.SetToolTip(txtFadeOut, "Length of the fade out at the end of the part before the removed part, in seconds. 0 means no fade.");
                    toolTip.SetToolTip(btnTrim, "Remove the part between Remove From and Remove To with SplitMediaFiles.");
                    toolTip.SetToolTip(btnPreview, "Play from the seek bar. The removed part is skipped, with the fades. The file is not changed.");
                    break;
                case TrimMode.Split:
                    lblStartAt.Text = "Split at";
                    lblEndAt.Text = "and at";
                    toolTip.SetToolTip(txtStartAt, $"Where to cut the file, {forms}. One time gives two files, two times give three. A 0 here is ignored.");
                    toolTip.SetToolTip(txtEndAt, "A second place to cut the file. Leave it empty to cut only once.");
                    toolTip.SetToolTip(btnTrim, "Split the file with SplitMediaFiles. The original file is kept.");
                    toolTip.SetToolTip(btnPreview, "Play from the seek bar. The player pauses at every cut; press Play to go on. The file is not changed.");
                    break;
                default:
                    lblStartAt.Text = "Start at";
                    lblEndAt.Text = "End at";
                    toolTip.SetToolTip(txtStartAt, $"Where the trimmed file starts, {forms}. Leave it empty to start at the beginning.");
                    toolTip.SetToolTip(txtEndAt, $"Where the trimmed file ends, {forms}. Leave it empty to end at the end of the file.");
                    toolTip.SetToolTip(txtFadeIn, "Length of the fade in at the start, in seconds. 0 means no fade.");
                    toolTip.SetToolTip(txtFadeOut, "Length of the fade out at the end, in seconds. 0 means no fade.");
                    toolTip.SetToolTip(btnTrim, "Trim the file with SplitMediaFiles. The original file is kept.");
                    toolTip.SetToolTip(btnPreview, "Play the part between Start at and End at, with the fades. The file is not changed.");
                    break;
            }

            bool fades = mode != TrimMode.Split;
            lblFadeIn.Visible = txtFadeIn.Visible = lblFadeOut.Visible = txtFadeOut.Visible = fades;
            LayoutTrimRow();
        }

        // The first row: the mode list, the two time boxes and the fades. The labels differ in width from mode to mode,
        // so everything after them follows. In Remove mode the fade out comes first, as in time.
        private void LayoutTrimRow()
        {
            int gap = LogicalToDeviceUnits(16);
            int space = LogicalToDeviceUnits(6);
            int x = cboTrimMode.Right + gap;
            x = PlaceTimeBox(lblStartAt, txtStartAt, btnStartNow, x, space) + gap;
            x = PlaceTimeBox(lblEndAt, txtEndAt, btnEndNow, x, space) + gap;

            if (Mode == TrimMode.Split)
            {
                return;
            }
            bool remove = Mode == TrimMode.Remove;
            Label firstLabel = remove ? lblFadeOut : lblFadeIn;
            TextBox firstBox = remove ? txtFadeOut : txtFadeIn;
            Label secondLabel = remove ? lblFadeIn : lblFadeOut;
            TextBox secondBox = remove ? txtFadeIn : txtFadeOut;
            x = PlaceFadeBox(firstLabel, firstBox, x, space) + gap;
            PlaceFadeBox(secondLabel, secondBox, x, space);

            // Tab goes in the order the boxes are shown.
            firstLabel.TabIndex = 7;
            firstBox.TabIndex = 8;
            secondLabel.TabIndex = 9;
            secondBox.TabIndex = 10;
        }

        private int PlaceTimeBox(Label label, TextBox box, Button now, int x, int space)
        {
            label.Left = x;
            box.Left = x + label.PreferredSize.Width + space;
            now.Left = box.Right + LogicalToDeviceUnits(4);
            return now.Right;
        }

        private static int PlaceFadeBox(Label label, TextBox box, int x, int space)
        {
            label.Left = x;
            box.Left = x + label.PreferredSize.Width + space;
            return box.Right;
        }

        // ---- Showing and hiding the group box ----

        // The "More Options" button opens the menu of the control bar (the same one as the right-click)
        // above itself, with its right edge at the right edge of the button.
        private void btnMore_Click(object? sender, EventArgs e)
        {
            menuControls.Show(btnMore.PointToScreen(new Point(btnMore.Width, 0)), ToolStripDropDownDirection.AboveLeft);
        }

        private void menuControls_Opening(object? sender, CancelEventArgs e)
        {
            if (_fullScreen)
            {
                e.Cancel = true;   // the Trim and Control Panel groups are not shown in full screen
                return;
            }

            // A stream has no file to trim, and neither has a file that cannot be searched (SID music).
            // An animated GIF is not trimmed: the item is hidden for it.
            // The item is hidden when SplitMediaFiles is not installed: until it is released, trimming is an unlisted feature.
            mnuShowTrim.Visible = !IsGifFile && FindSplitMediaFiles() != null;
            mnuShowTrim.Enabled = CanSeek && !IsDvd && !_trimming;
            mnuShowTrim.Checked = _trimVisible;
            mnuShowControlPanel.Checked = _panelVisible;
            if (_mnuShowStreamInfo != null)
            {
                _mnuShowStreamInfo.Checked = _streamInfoVisible;
            }
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
            if (visible == _trimVisible || visible && IsRecordingPlayback)   // not while a recording grows
            {
                return;
            }

            if (!visible)
            {
                EndPreview(false);
            }
            else if (cboTrimMode.SelectedIndex != 0)
            {
                cboTrimMode.SelectedIndex = 0;   // the box always opens on Trim
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
            int height = LogicalToDeviceUnits(200) + (_trimVisible ? grpTrim.Height : 0) + (_panelVisible ? grpControlPanel.Height : 0)
                + (_streamInfoVisible && _grpStreamInfo != null ? _grpStreamInfo.Height : 0);
            MinimumSize = SizeFromClientSize(new Size(width, height));
        }

        // ---- Start and end ("Now" buttons) and the time stamp buttons ----

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

            box.Text = FormatTime(CurrentTimeMs(), true);
        }

        // The same position that Save Frame uses.
        private long CurrentTimeMs()
        {
            if (_player == null)
            {
                return 0;
            }
            return _pendingSeekMs >= 0 ? _pendingSeekMs
                : IsAtEnd() ? _lengthMs
                : _paused && _stepTimeMs >= 0 ? _stepTimeMs
                : _player.Time;
        }

        private void btnPrevMark_Click(object? sender, EventArgs e)
        {
            GoToMark(-1);
        }

        private void btnNextMark_Click(object? sender, EventArgs e)
        {
            GoToMark(+1);
        }

        // Jumps to the time stamp before / after the position, among the times in the two boxes (those that
        // are filled in and valid). Does nothing when there is none in that direction.
        private void GoToMark(int direction)
        {
            if (_player == null || _lengthMs <= 0 || !CanSeek || _trimming)
            {
                return;
            }

            const long margin = 400;   // a mark we are standing on does not count
            long now = CurrentTimeMs();
            long? target = null;
            foreach (TextBox box in new[] { txtStartAt, txtEndAt })
            {
                if (!TryParseTime(box.Text, out double seconds))
                {
                    continue;
                }
                long mark = Math.Min((long)(seconds * 1000), _lengthMs);
                if (direction < 0 && mark < now - margin && (target == null || mark > target)
                    || direction > 0 && mark > now + margin && (target == null || mark < target))
                {
                    target = mark;
                }
            }

            if (target != null)
            {
                SeekTo(target.Value);
            }
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

        // Reads one time box. An empty box is fine (has = false). A time a few milliseconds beyond the end ("Now" at the
        // very end) is the end. On a problem it says so, focuses the box and returns false.
        private bool ReadTimeBox(TextBox box, Label label, double length, out bool has, out double value)
        {
            const string timeHelp = "Use a time such as 1:23:45, 2:07, 90, 7.5 or 45%.";
            has = box.Text.Trim().Length > 0;
            value = 0;
            if (!has)
            {
                return true;
            }
            if (!TryParseTime(box.Text, out value))
            {
                ShowTrimProblem($"The time \"{box.Text.Trim()}\" in \"{label.Text}\" is not valid. {timeHelp}", box);
                return false;
            }
            if (value > length && value - length < 0.5)
            {
                value = length;
            }
            if (value > length)
            {
                ShowTrimProblem($"The time in \"{label.Text}\" is after the end of the file ({FormatTime(_lengthMs, false)}).", box);
                return false;
            }
            return true;
        }

        private bool ReadFades(out double fadeIn, out double fadeOut)
        {
            fadeOut = 0;
            if (!TryParseSeconds(txtFadeIn.Text, out fadeIn))
            {
                ShowTrimProblem("The fade in must be a number of seconds, for example 2 or 3.5. Use 0 for no fade.", txtFadeIn);
                return false;
            }
            if (!TryParseSeconds(txtFadeOut.Text, out fadeOut))
            {
                ShowTrimProblem("The fade out must be a number of seconds, for example 2 or 3.5. Use 0 for no fade.", txtFadeOut);
                return false;
            }
            if (fadeIn < 0 || fadeOut < 0)
            {
                ShowTrimProblem("A fade cannot be negative. Use 0 for no fade.", fadeIn < 0 ? txtFadeIn : txtFadeOut);
                return false;
            }
            return true;
        }

        // Checks the fields for the mode. On a problem it says so, focuses the field and returns false.
        private bool TryReadTrim(out TrimRequest request)
        {
            request = default;
            TrimMode mode = Mode;

            if (_lengthMs <= 0)
            {
                MessageBox.Show(this, "The length of the file is not known yet. Wait until the file is playing.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            double length = _lengthMs / 1000.0;

            if (!ReadTimeBox(txtStartAt, lblStartAt, length, out bool hasStart, out double start)
                || !ReadTimeBox(txtEndAt, lblEndAt, length, out bool hasEnd, out double end))
            {
                return false;
            }

            switch (mode)
            {
                case TrimMode.Remove:
                {
                    if (!hasStart || !hasEnd)
                    {
                        ShowTrimProblem("Fill in both Remove From and Remove To.", hasStart ? txtEndAt : txtStartAt);
                        return false;
                    }
                    if (end <= start)
                    {
                        ShowTrimProblem("Remove To must be later than Remove From.", txtEndAt);
                        return false;
                    }
                    if (start < 0.05)
                    {
                        ShowTrimProblem("The part to remove starts at the very beginning. To remove the beginning of the file, use Trim.", txtStartAt);
                        return false;
                    }
                    if (end > length - 0.05)
                    {
                        ShowTrimProblem("The part to remove ends at the very end. To remove the end of the file, use Trim.", txtEndAt);
                        return false;
                    }
                    if (!ReadFades(out double fadeIn, out double fadeOut))
                    {
                        return false;
                    }
                    if (fadeOut > start)
                    {
                        ShowTrimProblem("The fade out is longer than the part before the removed part.", txtFadeOut);
                        return false;
                    }
                    if (fadeIn > length - end)
                    {
                        ShowTrimProblem("The fade in is longer than the part after the removed part.", txtFadeIn);
                        return false;
                    }
                    request = new TrimRequest(mode, true, start, true, end, fadeIn, fadeOut);
                    return true;
                }

                case TrimMode.Split:
                {
                    // A 0 is the same as an empty box: the first part always starts at 0.
                    hasStart &= start > 0;
                    hasEnd &= end > 0;
                    if (!hasStart && !hasEnd)
                    {
                        ShowTrimProblem("Fill in at least one time to split at.", txtStartAt);
                        return false;
                    }
                    if (hasStart && start >= length || hasEnd && end >= length)
                    {
                        ShowTrimProblem("A split time must be before the end of the file.", hasStart && start >= length ? txtStartAt : txtEndAt);
                        return false;
                    }
                    if (hasStart && hasEnd && end <= start)
                    {
                        ShowTrimProblem("The second time must be later than the first.", txtEndAt);
                        return false;
                    }
                    request = new TrimRequest(mode, hasStart, start, hasEnd, end, 0, 0);
                    return true;
                }

                default:
                {
                    double trimEnd = hasEnd ? end : length;
                    if (trimEnd <= start)
                    {
                        ShowTrimProblem("The end time must be later than the start time.", hasEnd ? txtEndAt : txtStartAt);
                        return false;
                    }
                    if (!ReadFades(out double fadeIn, out double fadeOut))
                    {
                        return false;
                    }
                    if (fadeIn + fadeOut > trimEnd - start)
                    {
                        ShowTrimProblem("The fades are longer than the part that is kept.", txtFadeIn);
                        return false;
                    }
                    request = new TrimRequest(mode, hasStart, start, hasEnd, trimEnd, fadeIn, fadeOut);
                    return true;
                }
            }
        }

        // ---- Preview ----

        private void btnPreview_Click(object? sender, EventArgs e)
        {
            StartPreview(false);
        }

        private void btnQuickPreview_Click(object? sender, EventArgs e)
        {
            StartPreview(true);
        }

        // The segments to play and the places to pause at, for the request. Quick: only the places where something
        // happens, QuickPreviewSeconds before and after, plus the fade. Otherwise the result from posMs on (Trim: all of
        // the part that is kept); a position near the end counts as the beginning.
        private (PreviewSegment[] Segments, long[] Pauses) BuildPreviewPlan(TrimRequest request, bool quick, long posMs)
        {
            long length = _lengthMs;
            long lead = (long)(QuickPreviewSeconds * 1000);
            long fadeIn = (long)(request.FadeIn * 1000);
            long fadeOut = (long)(request.FadeOut * 1000);
            long a = request.HasStart ? (long)(request.Start * 1000) : 0;
            long b = request.HasEnd ? (long)(request.End * 1000) : length;
            if (posMs >= length - 3000)
            {
                posMs = 0;
            }

            switch (request.Mode)
            {
                case TrimMode.Remove:
                    if (quick)
                    {
                        return ([new(Math.Max(0, a - fadeOut - lead), a), new(b, Math.Min(length, b + fadeIn + lead))], []);
                    }
                    if (posMs < a)
                    {
                        return ([new(posMs, a), new(b, length)], []);
                    }
                    return ([new(Math.Max(posMs, b), length)], []);

                case TrimMode.Split:
                {
                    List<long> cuts = new();
                    if (request.HasStart)
                    {
                        cuts.Add(a);
                    }
                    if (request.HasEnd)
                    {
                        cuts.Add(b);
                    }
                    if (!quick)
                    {
                        return ([new(posMs, length)], cuts.Where(c => c > posMs + 300).ToArray());
                    }
                    List<PreviewSegment> segments = new();
                    foreach (long cut in cuts)
                    {
                        long from = Math.Max(0, cut - lead);
                        long to = Math.Min(length, cut + lead);
                        if (segments.Count > 0 && from <= segments[^1].EndMs)
                        {
                            segments[^1] = segments[^1] with { EndMs = to };   // they run into each other
                        }
                        else
                        {
                            segments.Add(new(from, to));
                        }
                    }
                    return (segments.ToArray(), cuts.ToArray());
                }

                default:
                    if (!quick)
                    {
                        return ([new(a, b)], []);
                    }
                    {
                        PreviewSegment first = new(a, Math.Min(b, a + fadeIn + lead));
                        PreviewSegment last = new(Math.Max(a, b - fadeOut - lead), b);
                        return (last.StartMs <= first.EndMs ? [new(a, b)] : [first, last], []);
                    }
            }
        }

        private async void StartPreview(bool quick)
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

            (PreviewSegment[] segments, long[] pauses) = BuildPreviewPlan(request, quick, CurrentTimeMs());
            _previewMode = request.Mode;
            _previewQuick = quick;
            _previewSegments = segments;
            _previewSegment = 0;
            _previewPauses = pauses;
            _previewPauseIndex = 0;
            _previewWaiting = false;
            _previewMarkA = request.HasStart ? (long)(request.Start * 1000) : 0;
            _previewMarkB = request.HasEnd ? (long)(request.End * 1000) : _lengthMs;
            _previewFadeIn = request.FadeIn;
            _previewFadeOut = request.FadeOut;
            _previewing = true;
            (quick ? btnQuickPreview : btnPreview).Text = "Stop";
            (quick ? btnPreview : btnQuickPreview).Enabled = false;

            // An ended or stopped player has to be started again first.
            if (player.State is VLCState.Stopped or VLCState.Ended or VLCState.Error or VLCState.NothingSpecial)
            {
                await StopPlayerTask(player);
                _pendingSeekMs = -1;
                if (_player != player || !_previewing)
                {
                    return;
                }
                EnsureCleanMedia(player);
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
            JumpPreviewTo(player, segments[0].StartMs);
            player.SetPause(false);

            if (_previewTimer == null)
            {
                _previewTimer = new System.Windows.Forms.Timer { Interval = 30 };
                _previewTimer.Tick += (s, a) => PreviewTick();
            }
            _previewTimer.Start();
        }

        private void JumpPreviewTo(MediaPlayer player, long timeMs)
        {
            player.Time = timeMs;
            _stepTimeMs = -1;
            _previewSettleMs = timeMs;
            _previewStartedTicks = Environment.TickCount64;
        }

        // Follows the fades with the volume of the player, goes from one segment to the next, pauses at the marks
        // and stops at the end of the last segment. Stops also when the user pauses or stops the player.
        private void PreviewTick()
        {
            MediaPlayer? player = _player;
            if (player == null || !_previewing)
            {
                EndPreview(false);
                return;
            }

            // Paused at a mark: the preview goes on when the user plays.
            if (_previewWaiting)
            {
                if (Environment.TickCount64 - _previewWaitingTicks < 400 || !player.IsPlaying)
                {
                    return;
                }
                _previewWaiting = false;
            }

            // Right after a jump the player still reports the old time for a moment.
            bool settling = Environment.TickCount64 - _previewStartedTicks < 600;
            long timeMs = settling ? _previewSettleMs : player.Time;
            if (!settling && !player.IsPlaying)
            {
                EndPreview(false);
                return;
            }

            if (!settling)
            {
                PreviewSegment segment = _previewSegments[_previewSegment];
                if (timeMs >= segment.EndMs)
                {
                    if (_previewSegment + 1 >= _previewSegments.Length)
                    {
                        EndPreview(true);
                        return;
                    }
                    _previewSegment++;
                    JumpPreviewTo(player, _previewSegments[_previewSegment].StartMs);
                    return;
                }

                if (_previewPauseIndex < _previewPauses.Length && timeMs >= _previewPauses[_previewPauseIndex] - 30)
                {
                    _previewPauseIndex++;
                    _previewWaiting = true;
                    _previewWaitingTicks = Environment.TickCount64;
                    player.SetPause(true);
                    return;
                }
            }

            player.Volume = (int)Math.Round(trkVolume.Value * PreviewVolumeFactor(timeMs));
        }

        // 1 = full volume. Trim: fade in after the start, fade out before the end. Remove: fade out before the cut,
        // fade in after it. The fades are in the volume only.
        private double PreviewVolumeFactor(long timeMs)
        {
            double factor = 1;
            switch (_previewMode)
            {
                case TrimMode.Trim:
                    if (_previewFadeIn > 0)
                    {
                        factor = Math.Min(factor, Math.Clamp((timeMs - _previewMarkA) / 1000.0 / _previewFadeIn, 0, 1));
                    }
                    if (_previewFadeOut > 0)
                    {
                        factor = Math.Min(factor, Math.Clamp((_previewMarkB - timeMs) / 1000.0 / _previewFadeOut, 0, 1));
                    }
                    break;
                case TrimMode.Remove:
                    if (_previewFadeOut > 0 && timeMs <= _previewMarkA)
                    {
                        factor = Math.Min(factor, Math.Clamp((_previewMarkA - timeMs) / 1000.0 / _previewFadeOut, 0, 1));
                    }
                    if (_previewFadeIn > 0 && timeMs >= _previewMarkB)
                    {
                        factor = Math.Min(factor, Math.Clamp((timeMs - _previewMarkB) / 1000.0 / _previewFadeIn, 0, 1));
                    }
                    break;
            }
            return factor;
        }

        // Ends the preview and puts the volume back. With pause, the player is paused as well.
        private void EndPreview(bool pause)
        {
            if (!_previewing)
            {
                return;
            }

            _previewing = false;
            _previewWaiting = false;
            _previewTimer?.Stop();
            btnPreview.Text = "Preview";
            btnQuickPreview.Text = "Quick Preview";
            btnPreview.Enabled = btnQuickPreview.Enabled = true;

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

        // ---- Start: Trim, Remove and Split with SplitMediaFiles ----

        private async void btnTrim_Click(object? sender, EventArgs e)
        {
            MediaPlayer? player = _player;
            if (player == null || _trimming || _splittingNow || !CanSeek || _restoringEnd || !TryReadTrim(out TrimRequest request))
            {
                return;
            }

            if (!File.Exists(_filePath))
            {
                MessageBox.Show(this, $"The file \"{_filePath}\" does not exist.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (request.Mode == TrimMode.Trim && !request.HasStart && !request.HasEnd && request.FadeIn <= 0 && request.FadeOut <= 0)
            {
                ShowTrimProblem("There is nothing to trim. Fill in Start at, End at, or a fade.", txtStartAt);
                return;
            }

            string? exe = FindSplitMediaFiles();
            if (exe == null)
            {
                // Only if it was removed while the group was open.
                MessageBox.Show(this, "SplitMediaFiles was not found, so the file cannot be changed.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            const string replaceNote = "Replace the File: the file is changed in place. SplitMediaFiles keeps the original in its folder "
                + "for original files (\"_old\" next to the file, unless it is set otherwise there). The player lets go "
                + "of the file meanwhile and opens it again afterwards.\n\n"
                + "Save as a New File: SplitMediaFiles opens with these values filled in. You choose the name and "
                + "the folder there, and start it yourself. The original file is not changed.";
            string fileName = Path.GetFileName(_filePath);
            string time(double seconds) => FormatTime((long)(seconds * 1000), true);
            string fadeText(double seconds) => seconds > 0 ? FormatSeconds(seconds) + " s" : "none";

            // The question, for the mode.
            TaskDialogButton replaceButton = new("Replace the File");
            TaskDialogButton newFileButton = new("Save as a New File");
            TaskDialogButton splitNowButton = new("Split Now");
            TaskDialogButton openButton = new("Open in SplitMediaFiles");
            TaskDialogPage choicePage;
            if (request.Mode == TrimMode.Split)
            {
                int cuts = (request.HasStart ? 1 : 0) + (request.HasEnd ? 1 : 0);
                string cutTimes = string.Join(" and ", new[] { request.HasStart ? time(request.Start) : null, request.HasEnd ? time(request.End) : null }
                    .Where(t => t != null));
                string baseName = Path.GetFileNameWithoutExtension(_filePath);
                choicePage = new()
                {
                    Caption = AppName,
                    Heading = $"Split \"{fileName}\"?",
                    Text = $"Cut at: {cutTimes}\nThis makes {cuts + 1} files.\n\n"
                        + $"Split Now: SplitMediaFiles makes the files by itself, named \"{baseName} - part1\", \"{baseName} - part2\" and so on, "
                        + "in the same folder as the original. The original file is not changed, and no file is overwritten.\n\n"
                        + "Open in SplitMediaFiles: it opens with these times filled in. You choose the folder and the names there, and start it yourself.",
                    Icon = TaskDialogIcon.Information,
                    Buttons = { splitNowButton, openButton, TaskDialogButton.Cancel },
                    DefaultButton = openButton
                };
            }
            else
            {
                string summary = request.Mode == TrimMode.Remove
                    ? $"Remove from: {time(request.Start)}\nRemove to: {time(request.End)}\n"
                        + $"Fade out (end of the part before): {fadeText(request.FadeOut)}\n"
                        + $"Fade in (start of the part after): {fadeText(request.FadeIn)}\n\n"
                    : $"Start: {(request.HasStart ? time(request.Start) : "the beginning")}\n"
                        + $"End: {(request.HasEnd ? time(request.End) : "the end")}\n"
                        + $"Fade in: {fadeText(request.FadeIn)}\n"
                        + $"Fade out: {fadeText(request.FadeOut)}\n\n";
                choicePage = new()
                {
                    Caption = AppName,
                    Heading = request.Mode == TrimMode.Remove ? $"Remove a part of \"{fileName}\"?" : $"Trim \"{fileName}\"?",
                    Text = summary + replaceNote,
                    Icon = TaskDialogIcon.Information,
                    Buttons = { replaceButton, newFileButton, TaskDialogButton.Cancel },
                    DefaultButton = newFileButton   // the one that cannot lose anything
                };
            }

            TaskDialogButton choice = TaskDialog.ShowDialog(this, choicePage);
            if (choice == newFileButton || choice == openButton)
            {
                // The original is not touched, so the player keeps it open and does not wait.
                try
                {
                    StartSplitMediaFiles(exe, request, SplitRun.Window)?.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"SplitMediaFiles could not be started.\n\n{ex.Message}", AppName,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            if (choice == splitNowButton)
            {
                await SplitNowAsync(exe, request);
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
                exitCode = await Task.Run(() => RunSplitMediaFiles(exe, request, SplitRun.Overwrite));
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
                MessageBox.Show(this, request.Mode == TrimMode.Remove ? "The removal was canceled." : "The trim was canceled.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (exitCode != 0)
            {
                MessageBox.Show(this, $"{(request.Mode == TrimMode.Remove ? "The part could not be removed" : "The file could not be trimmed")} "
                    + $"(SplitMediaFiles exit code {exitCode}).\n\nThe reason is in the log file:\n{CommandLineLogPath}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string CommandLineLogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SplitMediaFiles", "Logs", "command-line.log");

        // "Split Now": SplitMediaFiles splits by itself (-run) and makes new files next to the original. It only reads
        // the file, so the player keeps playing and the user can go on meanwhile; the result is reported when it is done.
        private async Task SplitNowAsync(string exe, TrimRequest request)
        {
            _splittingNow = true;
            btnTrim.Enabled = false;
            int exitCode;
            string? startError = null;
            try
            {
                exitCode = await Task.Run(() => RunSplitMediaFiles(exe, request, SplitRun.Automatic));
            }
            catch (Exception ex)
            {
                exitCode = int.MinValue;
                startError = ex.Message;
            }

            _splittingNow = false;
            if (IsDisposed)
            {
                return;
            }
            btnTrim.Enabled = true;

            if (startError != null)
            {
                MessageBox.Show(this, $"SplitMediaFiles could not be started.\n\n{startError}", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (exitCode == 0)
            {
                int parts = (request.HasStart ? 1 : 0) + (request.HasEnd ? 1 : 0) + 1;
                MessageBox.Show(this, $"The file was split into {parts} files, next to the original.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, $"The file could not be split (SplitMediaFiles exit code {exitCode}).\n\nThe reason is in the log file:\n{CommandLineLogPath}",
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Starts SplitMediaFiles with the values and waits for it. Returns its exit code.
        private int RunSplitMediaFiles(string exe, TrimRequest request, SplitRun run)
        {
            using Process process = StartSplitMediaFiles(exe, request, run)
                ?? throw new InvalidOperationException("The process did not start.");
            process.WaitForExit();
            return process.ExitCode;
        }

        // Starts SplitMediaFiles with the values for the mode (see the brief SplitMediaFiles-command-line-brief.md).
        // Window: its window opens with the values filled in. Overwrite (-overwrite): it replaces the file by itself
        // (Trim and Remove only). Automatic (-run): it makes new files next to the original by itself.
        private Process? StartSplitMediaFiles(string exe, TrimRequest request, SplitRun run)
        {
            ProcessStartInfo info = new(exe)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? ""
            };
            info.ArgumentList.Add("-i");
            info.ArgumentList.Add(_filePath);

            if (request.Mode == TrimMode.Split)
            {
                // Semicolons between the times, so a point or a comma in a time does not matter. (No quoting is needed
                // with ArgumentList.)
                List<string> cuts = new();
                if (request.HasStart)
                {
                    cuts.Add(FormatSeconds(request.Start));
                }
                if (request.HasEnd)
                {
                    cuts.Add(FormatSeconds(request.End));
                }
                info.ArgumentList.Add("-split");
                info.ArgumentList.Add(string.Join(';', cuts));
            }
            else
            {
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
                if (request.Mode == TrimMode.Remove)
                {
                    info.ArgumentList.Add("-remove");
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
            }

            if (run == SplitRun.Overwrite)
            {
                info.ArgumentList.Add("-overwrite");
            }
            else if (run == SplitRun.Automatic)
            {
                info.ArgumentList.Add("-run");
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

        // Looks next to this program, in ..\SplitMediaFiles, and in the place named in SplitMediaFiles.txt (a way to
        // point to a copy somewhere else; nothing in the program writes that file). Null when it is nowhere: the
        // trim feature is hidden then (see the Controls menu), because SplitMediaFiles is not released yet.
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

            return null;
        }
    }
}
