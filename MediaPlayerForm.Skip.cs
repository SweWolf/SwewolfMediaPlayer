// MediaPlayerForm.Skip.cs: the two skip buttons right of Stop (rewind and fast forward, two triangles each).
//
// They are always shown. They jump SkipSeconds back or ahead in the file, never past its ends: going back stops at the
// start (it never goes to the previous track), going ahead stops at the end, and what happens at the end (next track,
// repeat, ...) is then what always happens there. Off (greyed out) when the file cannot be sought in (see CanSeek).
//
// History:
//   2026-11-08  First version: a fixed 1 minute. To be made configurable later.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private const int SkipSeconds = 60;

        private IconButton? btnSkipBack;
        private IconButton? btnSkipForward;

        private void InitSkipButtons()
        {
            btnSkipBack = NewTuneButton(IconKind.Rewind, "Skip Back 1 Minute", () => Skip(-SkipSeconds));
            btnSkipForward = NewTuneButton(IconKind.FastForward, "Skip Ahead 1 Minute", () => Skip(+SkipSeconds));
            btnSkipBack.Visible = btnSkipForward.Visible = true;
            btnSkipBack.Enabled = btnSkipForward.Enabled = false;   // until a file that can be sought in is open
        }

        private void Skip(int seconds)
        {
            if (_player == null || !CanSeek || _lengthMs <= 0)
            {
                return;
            }

            // The same position that Save Frame uses.
            long now = _pendingSeekMs >= 0 ? _pendingSeekMs
                : IsAtEnd() ? _lengthMs
                : _paused && _stepTimeMs >= 0 ? _stepTimeMs
                : _player.Time;

            long target = now + seconds * 1000L;
            if (target < 0)
            {
                target = 0;
            }
            else if (target >= _lengthMs)
            {
                // To the end: a moment before it, so a playing file reaches its end the normal way.
                target = Math.Max(0, _lengthMs - 250);
            }
            SeekTo(target);
        }
    }
}
