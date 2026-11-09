// MediaPlayerForm.EndCover.cs: hides the black gap when a video has ended.
//
// To show the last frame the player is stopped and started again behind the scenes (OnEndReached), and the
// video area is black meanwhile. So just before that, a copy of what the video area shows is put over it, and
// taken away again when the last picture is back.
//
// History:
//   2026-11-08  First version.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private PictureBox? _endCover;

        private void ShowEndCover()
        {
            HideEndCover();
            try
            {
                Control? parent = videoView.Parent;
                if (parent == null || !videoView.Visible || videoView.Width < 2 || videoView.Height < 2)
                {
                    return;
                }

                Bitmap copy = new(videoView.Width, videoView.Height);
                using (Graphics g = Graphics.FromImage(copy))
                {
                    g.CopyFromScreen(videoView.PointToScreen(Point.Empty), Point.Empty, videoView.Size);
                }

                _endCover = new PictureBox
                {
                    Image = copy,
                    SizeMode = PictureBoxSizeMode.Normal,
                    BackColor = Color.Black,
                    Dock = DockStyle.Fill,
                    TabStop = false,
                };
                parent.Controls.Add(_endCover);
                _endCover.BringToFront();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or ArgumentException or InvalidOperationException)
            {
                HideEndCover();   // no copy of the screen is available: the black gap stays, nothing else
            }
        }

        private void HideEndCover()
        {
            PictureBox? cover = _endCover;
            _endCover = null;
            if (cover != null)
            {
                cover.Parent?.Controls.Remove(cover);
                cover.Image?.Dispose();
                cover.Dispose();
            }
        }
    }
}
