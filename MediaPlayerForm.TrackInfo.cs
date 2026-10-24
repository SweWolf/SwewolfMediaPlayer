// MediaPlayerForm.TrackInfo.cs: the artist, title and album of a music file, in blue in the top left corner
// of the video area (as in TerraTool, the predecessor of this player). Only for audio files, never for video.
// The tags are read with TagLibSharp together with the cover art (ShowCoverArt in MediaPlayerForm.cs).
//
// History:
//   2026-10-23  First version. Always on; a setting to turn it off or customize it may come later.
//   2026-10-23  SID files show author, title and the name of the tune (MediaPlayerForm.TuneList.cs).

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private Label? _lblTrackInfo;

        // Shows the lines (artist, title, album; the empty ones are left out), or hides the label when there are none.
        private void ShowTrackInfo(params string?[] lines)
        {
            string text = string.Join("\r\n", lines.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
            if (text.Length == 0)
            {
                HideTrackInfo();
                return;
            }

            if (_lblTrackInfo == null)
            {
                _lblTrackInfo = new Label
                {
                    AutoSize = true,
                    BackColor = Color.Black,
                    ForeColor = Color.FromArgb(30, 144, 255),
                    Font = new Font("Arial", 14F, FontStyle.Bold),
                    Location = new Point(LogicalToDeviceUnits(10), LogicalToDeviceUnits(6)),
                    MaximumSize = new Size(LogicalToDeviceUnits(900), 0),
                    TabStop = false,
                    Visible = false,
                };
                Controls.Add(_lblTrackInfo);
            }

            _lblTrackInfo.Text = text;
            _lblTrackInfo.Visible = true;
            _lblTrackInfo.BringToFront();
        }

        private void HideTrackInfo()
        {
            if (_lblTrackInfo != null)
            {
                _lblTrackInfo.Visible = false;
            }
        }
    }
}
