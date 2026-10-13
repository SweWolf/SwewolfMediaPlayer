// MediaPlayerForm.ControlPanel.cs: the "Control Panel" group box of MediaPlayerForm.
//
// Like the Trim group box it is hidden at start (that is not remembered, the options in it are):
// the right-click menu on the control bar shows it. It holds player options; for now only what happens when a video has
// played to its end (used by OnEndReached in MediaPlayerForm.cs).
//
// History:
//   2026-10-13  First version: "When the Video Ends" (last frame, black screen, repeat).
//   2026-10-13  "When the Video Ends" is remembered between runs (AppSettings.cs).

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private enum EndAction
        {
            LastFrame,
            BlackScreen,
            Repeat,
        }

        private bool _panelVisible;
        private EndAction _whenEnds = EndAction.LastFrame;
        private readonly AppSettings _settings = AppSettings.Load();

        private void InitControlPanel()
        {
            cboWhenEnds.Items.AddRange(new object[] { "Show the Last Frame", "Show a Black Screen", "Repeat" });
            _whenEnds = Enum.IsDefined((EndAction)_settings.WhenVideoEnds) ? (EndAction)_settings.WhenVideoEnds : EndAction.LastFrame;
            cboWhenEnds.SelectedIndex = (int)_whenEnds;
        }

        private void cboWhenEnds_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboWhenEnds.SelectedIndex >= 0)
            {
                _whenEnds = (EndAction)cboWhenEnds.SelectedIndex;
                _settings.WhenVideoEnds = cboWhenEnds.SelectedIndex;
                _settings.Save();
            }
        }

        private void mnuShowControlPanel_Click(object? sender, EventArgs e)
        {
            SetPanelVisible(!_panelVisible);
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
