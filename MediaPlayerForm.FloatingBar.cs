// MediaPlayerForm.FloatingBar.cs: the control bar in full screen mode.
//
// In full screen the bar (pnlControls) is not docked: it floats over the bottom edge of the video, so the picture keeps its size. It appears when the
// pointer moves into the bottom strip of the video (as high as the bar) and goes away when the pointer has left it for a moment, or has been still for
// CursorHideDelayMs. It stays while a button is held down over it or a slider is dragged (the pointer may leave the bar then), and while the
// right-click menu is open. A normal control can float over LibVLC's video window (tested 2026-11-21 in a throwaway app: no flicker, the
// video is unharmed when the bar goes away again). The pointer moves are watched by the 30 ms timer (CheckCursorIdle), as the video window does not pass them on.
//
// History:
//   2026-11-21  First version.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private const int BarLeaveGraceMs = 600;   // how long the bar stays after the pointer has left it

        private bool _barShown;                    // the floating bar is on the screen (full screen only)
        private long _barActiveTicks;              // the last time the pointer moved or was pressed over the bar
        private int _barChildIndex;                // where the bar sat in the z-order while docked

        // Full screen on: the bar leaves the docked layout and waits, hidden. Full screen off: it is docked at the bottom again.
        private void SetFloatingBar(bool fullScreen)
        {
            if (fullScreen)
            {
                _barChildIndex = Controls.GetChildIndex(pnlControls);
                pnlControls.Visible = false;
                pnlControls.Dock = DockStyle.None;
                btnMore.Visible = false;   // its menu (Trim, Control Panel) does not apply in full screen
                videoView.SizeChanged += VideoViewResized;
                videoView.LocationChanged += VideoViewResized;
                _barShown = false;
            }
            else
            {
                videoView.SizeChanged -= VideoViewResized;
                videoView.LocationChanged -= VideoViewResized;
                _barShown = false;
                pnlControls.Dock = DockStyle.Bottom;
                Controls.SetChildIndex(pnlControls, Math.Min(_barChildIndex, Controls.Count - 1));
                btnMore.Visible = true;
                pnlControls.Visible = true;
            }
        }

        private void VideoViewResized(object? sender, EventArgs e)
        {
            if (_fullScreen)
            {
                PlaceFloatingBar();
            }
        }

        // The bar sits on the bottom edge of the video area (not under a channel list at its side).
        private void PlaceFloatingBar()
        {
            pnlControls.SetBounds(videoView.Left, videoView.Bottom - pnlControls.Height, videoView.Width, pnlControls.Height);
        }

        private void SetBarShown(bool shown)
        {
            if (shown == _barShown)
            {
                return;
            }

            _barShown = shown;
            if (shown)
            {
                PlaceFloatingBar();
                pnlControls.BringToFront();
            }
            pnlControls.Visible = shown;
            if (!shown)
            {
                toolTip.Active = false;   // a tooltip of a button that has just gone must not stay on the screen
                toolTip.Active = true;
            }
        }

        // Where the bar is (or would be) on the screen: the bottom strip of the video.
        private Rectangle BarScreenArea()
        {
            Rectangle video = videoView.RectangleToScreen(videoView.ClientRectangle);
            return new Rectangle(video.Left, video.Bottom - pnlControls.Height, video.Width, pnlControls.Height);
        }

        // True when the pointer is over the visible floating bar (so that a click or the hidden pointer there is not treated as one on the video).
        private bool IsOverFloatingBar(Point screenPoint)
        {
            return _fullScreen && _barShown && BarScreenArea().Contains(screenPoint);
        }

        // Called by the 30 ms timer (CheckCursorIdle) with the pointer state.
        private void UpdateFloatingBar(Point position, bool moved, bool buttonDown, long now)
        {
            if (!_fullScreen)
            {
                return;
            }

            bool over = BarScreenArea().Contains(position);
            bool holding = menuVideo.Visible || (buttonDown && pnlControls.ContainsFocus && _barShown);
            if (over && (moved || buttonDown))
            {
                _barActiveTicks = now;
                SetBarShown(true);
                return;
            }

            if (!_barShown)
            {
                return;
            }

            if (holding)
            {
                _barActiveTicks = now;
            }
            else if (over ? now - _barActiveTicks >= CursorHideDelayMs : now - _barActiveTicks >= BarLeaveGraceMs)
            {
                SetBarShown(false);
            }
        }
    }
}
