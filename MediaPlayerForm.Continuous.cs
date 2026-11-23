// MediaPlayerForm.Continuous.cs: the "Continuous Play" check box at the bottom of the tune list (SID music) and of a playlist.
//
// On (always the case when a window opens, it is not a setting): when a tune or a playlist item has ended, the next one starts by itself.
// Off: only the one the user chose is played; at its end the player stops (a tune stays paused at its end, a playlist item ends like a
// single file does, with the choice in the Control Panel). Next, Previous and a click or double click in the list work as always.
// It is not shown for a list of channels (IPTV): a live channel has no end.
//
// History:
//   2026-11-22  First version.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private bool _continuousPlay = true;
        private readonly List<CheckBox> _continuousBoxes = new();

        // One check box for a list panel; all of them show the same value.
        private CheckBox NewContinuousBox()
        {
            var box = new CheckBox
            {
                Dock = DockStyle.Bottom,
                Height = LogicalToDeviceUnits(32),
                Text = "Continuous Play",
                Checked = _continuousPlay,
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(230, 230, 230),
                Padding = new Padding(LogicalToDeviceUnits(8), 0, 0, 0),
                TabStop = false,
                UseMnemonic = false,
            };
            toolTip.SetToolTip(box, "On: the next tune or item starts by itself when one has ended.\r\nOff: only the one you chose is played.");
            box.CheckedChanged += (s, e) =>
            {
                _continuousPlay = box.Checked;
                foreach (CheckBox other in _continuousBoxes)
                {
                    if (other.Checked != _continuousPlay)
                    {
                        other.Checked = _continuousPlay;
                    }
                }
            };
            _continuousBoxes.Add(box);
            return box;
        }
    }
}
