// MediaPlayerForm.ControlPanel.cs: the "Control Panel" group box of MediaPlayerForm.
//
// Like the Trim group box it is hidden at start (that is not remembered, the options in it are):
// the right-click menu on the control bar shows it. It holds player options; for now only what happens when a video has
// played to its end (used by OnEndReached in MediaPlayerForm.cs).
//
// History:
//   2026-10-13  First version: "When the Video Ends" (last frame, black screen, repeat).
//   2026-10-13  "When the Video Ends" is remembered between runs (AppSettings.cs).
//   2026-10-15  New option "Close Player": the window closes when the video ends.
//   2026-10-23  While the Trim group box is open, the end action is always the last frame (EffectiveEndAction).
//   2026-10-26  A small X in the upper right corner closes the group box (CloseXButton.cs); the Trim group box has one too.
//   2026-11-18  A player started with a file argument uses "Close Player" without saving it (UseCloseWhenEnds).
//   2026-11-18  A file handed over to a running player through the pipe does the same (PlayFromPipeAsync).
//   2026-10-26  A "Show Subtitles" check box was added and removed again the next day: the default is in Tools > Settings, the choice per file is
//               remembered in the library (MediaPlayerForm.Tracks.cs).
//   2026-11-19  The label reads "When the Media File Ends", or "When the Playlist Ends" while a playlist is open (UpdateEndLabel).
//   2026-11-22  The drop-down follows the right edge of its label (PositionEndCombo): it overlapped the longer label.
//   2026-11-26  "Frame Stepping when Paused" (Normal (VLC) / Exact (FFmpeg)), not remembered: MediaPlayerForm.ExactSteps.cs.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private enum EndAction
        {
            LastFrame,
            BlackScreen,
            Repeat,
            ClosePlayer,
        }

        private bool _panelVisible;
        private EndAction _whenEnds = EndAction.LastFrame;
        private readonly AppSettings _settings = AppSettings.Load();

        // While the Trim group box is open the player never closes, repeats or goes black at the end:
        // it stays on the last frame, so the user can go on trimming. The chosen option is kept as it was.
        private EndAction EffectiveEndAction => _trimVisible ? EndAction.LastFrame : _whenEnds;

        // The drop-down sits a little right of its label, whatever the label says ("When the Media File Ends" / "When the Playlist Ends")
        // and however wide that is on this screen: the label is as wide as its text, so the drop-down follows its right edge.
        private void PositionEndCombo()
        {
            cboWhenEnds.Left = lblWhenEnds.Right + LogicalToDeviceUnits(8);
            PositionFrameStepping();
        }

        private void InitControlPanel()
        {
            lblWhenEnds.SizeChanged += (_, _) => PositionEndCombo();
            PositionEndCombo();
            cboWhenEnds.Items.AddRange(new object[] { "Show the Last Frame", "Show a Black Screen", "Repeat", "Close Player" });
            _whenEnds = Enum.IsDefined((EndAction)_settings.WhenVideoEnds) ? (EndAction)_settings.WhenVideoEnds : EndAction.LastFrame;
            cboWhenEnds.SelectedIndex = (int)_whenEnds;

            picCover.SizeChanged += (_, _) => ApplyCoverSize();
            picCover.VisibleChanged += (_, _) => ApplyCoverSize();
            ApplyCoverSize();
        }

        private bool _settingEndActionByCode;   // true while the program (not the user) sets the drop-down list: nothing is saved then

        // A player started with a file on the command line (another program, e.g. the File Explorer, plays a file or a playlist)
        // always closes when the file ends, so it is never stuck on a repeat or on the last frame. This is not saved:
        // only a choice the user makes in the Control Panel is.
        public void UseCloseWhenEnds()
        {
            _settingEndActionByCode = true;
            try
            {
                _whenEnds = EndAction.ClosePlayer;
                cboWhenEnds.SelectedIndex = (int)EndAction.ClosePlayer;
            }
            finally
            {
                _settingEndActionByCode = false;
            }
        }

        private void cboWhenEnds_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_settingEndActionByCode)
            {
                return;
            }
            if (cboWhenEnds.SelectedIndex >= 0)
            {
                _whenEnds = (EndAction)cboWhenEnds.SelectedIndex;
                // Save from a fresh copy, so a change made in the Settings window meanwhile is not overwritten.
                AppSettings settings = AppSettings.Load();
                settings.WhenVideoEnds = cboWhenEnds.SelectedIndex;
                settings.Save();
            }
        }

        private void mnuShowControlPanel_Click(object? sender, EventArgs e)
        {
            SetPanelVisible(!_panelVisible);
        }

        private void btnCloseControlPanel_Click(object? sender, EventArgs e)
        {
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (visible == _panelVisible)
            {
                return;
            }

            _panelVisible = visible;
            grpControlPanel.Visible = visible && !_fullScreen;
            UpdateMinimumSize();

            // Keep the video as large as it was: the window grows and shrinks with the group box.
            if (!_fullScreen && WindowState == FormWindowState.Normal)
            {
                int height = Height + (visible ? grpControlPanel.Height : -grpControlPanel.Height);
                Height = Math.Min(height, Screen.FromControl(this).WorkingArea.Height);
            }
        }
    }
}
