// MediaPlayerForm.TrackInfo.cs: the artist, title and album of a music file, in blue in the top left corner
// of the video area (as in TerraTool, the predecessor of this player). Only for audio files, never for video.
// The tags are read with TagLibSharp together with the cover art (ShowCoverArt in MediaPlayerForm.cs).
//
// History:
//   2026-10-23  First version. Always on; a setting to turn it off or customize it may come later.
//   2026-10-23  Cover picture size, Medium (75 percent) or Maximum (ApplyCoverSize; AppSettings.CoverPictureSize).
//   2026-11-13  UseMnemonic off: an "&" in a title or album name (Heaven & Hell) was swallowed.
//   2026-10-23  SID files show author, title and the name of the tune (MediaPlayerForm.TuneList.cs).

namespace SwewolfMediaPlayer
{
    // A PictureBox that draws its picture as large as fits inside its Padding, keeping the proportions (the standard
    // PictureBox ignores the padding). Not clever about animation: a cover is a still picture.
    internal sealed class CoverBox : PictureBox
    {
        public CoverBox()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (Image == null)
            {
                return;
            }

            Rectangle area = new(Padding.Left, Padding.Top, Math.Max(1, ClientSize.Width - Padding.Horizontal), Math.Max(1, ClientSize.Height - Padding.Vertical));
            double scale = Math.Min((double)area.Width / Image.Width, (double)area.Height / Image.Height);
            int w = Math.Max(1, (int)(Image.Width * scale));
            int h = Math.Max(1, (int)(Image.Height * scale));
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Image, area.Left + (area.Width - w) / 2, area.Top + (area.Height - h) / 2, w, h);
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            Invalidate();
        }
    }

    public partial class MediaPlayerForm
    {
        private Label? _lblTrackInfo;

        private const double MediumCoverFraction = 0.75;   // measured in TeraTool: the picture was about 75 percent of the video area

        // The cover picture of a music file is shown at its largest size (Maximum) or at 75 percent of it (Medium, the default;
        // Settings > Player). CoverBox draws the picture inside its padding, so Medium is a padding of an eighth on every side.
        private void ApplyCoverSize()
        {
            if (_settings.CoverPictureSize == 1)
            {
                picCover.Padding = Padding.Empty;
                return;
            }

            int x = (int)(picCover.ClientSize.Width * (1 - MediumCoverFraction) / 2);
            int y = (int)(picCover.ClientSize.Height * (1 - MediumCoverFraction) / 2);
            picCover.Padding = new Padding(x, y, x, y);
        }

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
                    UseMnemonic = false,   // "Heaven & Hell" must not lose its &
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
