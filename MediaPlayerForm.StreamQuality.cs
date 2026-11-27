// MediaPlayerForm.StreamQuality.cs: the picture size of the stream that plays (the label left of "LIVE", in the control bar).
//
// A channel with several qualities (HLS) shows what is used right now, for example "Auto 720p" (LibVLC chose it) or "1080p" (the
// user chose Highest, Lowest or a height in the Quality menu). The size is the one of the picture LibVLC shows, read by the timer, so
// it follows a switch that LibVLC makes by itself. Only streams show it; a file, or a stream that has no picture yet (or only sound),
// shows nothing. It takes the place of the speed box, which is off for streams.
//
// History:
//   2026-11-27  First version.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private Label? lblStreamQuality;

        private void InitStreamQuality()
        {
            lblStreamQuality = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = false,
                ForeColor = lblTime.ForeColor,
                Font = lblTime.Font,
                Name = "lblStreamQuality",
                Size = new Size(LogicalToDeviceUnits(90), lblTime.Height),
                Top = lblTime.Top,
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false,
            };
            lblStreamQuality.Left = lblTime.Left - LogicalToDeviceUnits(6) - lblStreamQuality.Width;
            lblStreamQuality.ContextMenuStrip = menuControls;
            toolTip.SetToolTip(lblStreamQuality, "The picture size that plays now. Auto: LibVLC chose it from the speed of the connection (right-click the video for the Quality menu).");
            pnlControls.Controls.Add(lblStreamQuality);
        }

        // Called by the timer.
        private void CheckStreamQuality()
        {
            if (lblStreamQuality == null)
            {
                return;
            }

            string text = "";
            MediaPlayer? player = _player;
            if (_isStream && player != null && player.IsPlaying)
            {
                uint width = 0, height = 0;
                if (player.Size(0, ref width, ref height) && height > 0)
                {
                    text = (_qualityMode == QualityMode.Auto && _variants.Count > 1 ? "Auto " : "") + height + "p";
                }
            }

            if (lblStreamQuality.Text != text)
            {
                lblStreamQuality.Text = text;
            }
            lblStreamQuality.Visible = text.Length > 0;
        }
    }
}
