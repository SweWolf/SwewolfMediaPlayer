// MediaPlayerForm.FloatingTitle.cs: a title bar with a close button at the top edge in full screen mode (like the one of PotPlayer).
//
// It shows the window title (the file name, or the channel) and an X that closes the player, so a full screen window can be closed with the mouse. It floats over the
// top edge of the video in the same way as the control bar (MediaPlayerForm.FloatingBar.cs), and comes and goes in the same way: it appears when the
// pointer moves into the top strip of the video, and goes away when the pointer has left it for a moment or has been still for CursorHideDelayMs.
//
// History:
//   2026-11-22  First version.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private Panel? _pnlTitleBar;
        private Label? _lblTitleBar;
        private bool _titleShown;
        private long _titleActiveTicks;

        private void EnsureTitleBar()
        {
            if (_pnlTitleBar != null)
            {
                return;
            }

            Color dark = Color.FromArgb(32, 32, 32), light = Color.FromArgb(230, 230, 230);
            _lblTitleBar = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = light,
                BackColor = dark,
                AutoEllipsis = true,
                UseMnemonic = false,   // a file name with & is not an underline
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(LogicalToDeviceUnits(10), 0, 0, 0),
            };
            var close = new CloseXButton { Dock = DockStyle.Right, Width = LogicalToDeviceUnits(46) };
            close.Click += (s, e) => Close();
            toolTip.SetToolTip(close, "Close Player");

            _pnlTitleBar = new Panel { Height = LogicalToDeviceUnits(32), BackColor = dark, Visible = false };
            _pnlTitleBar.Controls.Add(_lblTitleBar);   // the filling control first, then the one docked to the right
            _pnlTitleBar.Controls.Add(close);
            Controls.Add(_pnlTitleBar);
        }

        private void SetFloatingTitle(bool fullScreen)
        {
            _titleShown = false;
            if (fullScreen)
            {
                EnsureTitleBar();
            }
            if (_pnlTitleBar != null)
            {
                _pnlTitleBar.Visible = false;
            }
        }

        private void PlaceFloatingTitle()
        {
            _pnlTitleBar?.SetBounds(videoView.Left, videoView.Top, videoView.Width, _pnlTitleBar.Height);
        }

        private void SetTitleShown(bool shown)
        {
            if (_pnlTitleBar == null || shown == _titleShown)
            {
                return;
            }

            _titleShown = shown;
            if (shown)
            {
                _lblTitleBar!.Text = Text;
                PlaceFloatingTitle();
                _pnlTitleBar.BringToFront();
            }
            _pnlTitleBar.Visible = shown;
            if (!shown)
            {
                toolTip.Active = false;   // a tooltip of the X that has just gone must not stay on the screen
                toolTip.Active = true;
            }
        }

        // Where the title bar is (or would be) on the screen: the top strip of the video.
        private Rectangle TitleScreenArea()
        {
            Rectangle video = videoView.RectangleToScreen(videoView.ClientRectangle);
            return new Rectangle(video.Left, video.Top, video.Width, _pnlTitleBar?.Height ?? LogicalToDeviceUnits(32));
        }

        private bool IsOverFloatingTitle(Point screenPoint)
        {
            return _fullScreen && _titleShown && TitleScreenArea().Contains(screenPoint);
        }

        // Called by the 30 ms timer (through UpdateFloatingBar) with the pointer state.
        private void UpdateFloatingTitle(Point position, bool moved, bool buttonDown, long now)
        {
            bool over = TitleScreenArea().Contains(position);
            if (over && (moved || buttonDown))
            {
                _titleActiveTicks = now;
                SetTitleShown(true);
                return;
            }

            if (!_titleShown)
            {
                return;
            }

            if (menuVideo.Visible || (buttonDown && over))
            {
                _titleActiveTicks = now;
            }
            else if (over ? now - _titleActiveTicks >= CursorHideDelayMs : now - _titleActiveTicks >= BarLeaveGraceMs)
            {
                SetTitleShown(false);
            }
        }
    }
}
