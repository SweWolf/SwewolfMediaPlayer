// MediaPlayerForm.AspectRatio.cs: the shape of the picture (right-click menu: Aspect Ratio, and the A key).
//
// Some streams (and a few files) carry a picture that is stretched, for example 4:3 sent as 16:9. The user can then force a shape.
// Default is what the file or stream says, exactly as before; this is the only state in which nothing is changed in LibVLC.
// The choice is not remembered: every new file or channel starts with Default. (Opening the same file again for a restart, as the
// Visualization menu does, keeps it.) The A key steps through the list; not while typing in a text box.
//
// The timer (CheckAspectRatio) sets the choice on the player that plays, so it also holds after the player has been replaced
// (a new engine, a stuck player that was swapped), and for a stream that has not started to show a picture yet.
//
// History:
//   2026-11-27  First version.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        // Menu text and LibVLC value; "" = what the file says (LibVLC treats an empty text as none).
        private static readonly (string Text, string Value)[] AspectRatios =
        {
            ("Default", ""),
            ("16:9", "16:9"),
            ("4:3", "4:3"),
            ("16:10", "16:10"),
            ("21:9", "21:9"),
            ("5:4", "5:4"),
            ("1:1", "1:1"),
        };

        private int _aspectIndex;                    // 0 = Default
        private MediaPlayer? _aspectSetOn;           // the player that has _aspectSetIndex
        private int _aspectSetIndex;

        // A new file or channel: back to Default.
        private void ResetAspectRatio()
        {
            _aspectIndex = 0;
        }

        private bool AspectAvailable => _player != null && !IsAudioFile(_filePath);

        private void AddAspectRatioMenu()
        {
            if (!AspectAvailable)
            {
                return;
            }

            ToolStripMenuItem menu = new("Aspect Ratio");
            for (int i = 0; i < AspectRatios.Length; i++)
            {
                int index = i;
                menu.DropDownItems.Add(TrackItem(AspectRatios[i].Text, i == _aspectIndex, () => SetAspectRatio(index)));
            }
            menuVideo.Items.Add(menu);
        }

        private void SetAspectRatio(int index)
        {
            _aspectIndex = Math.Clamp(index, 0, AspectRatios.Length - 1);
            CheckAspectRatio();
        }

        // The A key: the next shape in the list, and back to Default after the last.
        private void NextAspectRatio()
        {
            SetAspectRatio((_aspectIndex + 1) % AspectRatios.Length);
        }

        // Called by the timer, and when the choice changes. Nothing is sent to LibVLC while the choice is Default and has always been.
        private void CheckAspectRatio()
        {
            MediaPlayer? player = _player;
            if (player == null)
            {
                return;
            }

            bool sameAsSet = ReferenceEquals(_aspectSetOn, player) && _aspectSetIndex == _aspectIndex;
            bool nothingToDo = _aspectIndex == 0 && !ReferenceEquals(_aspectSetOn, player) && _aspectSetOn == null;
            if (sameAsSet || nothingToDo)
            {
                return;
            }
            if (_aspectIndex != 0 && !player.IsPlaying && !_paused)
            {
                return;   // wait until there is a picture
            }

            player.AspectRatio = AspectRatios[_aspectIndex].Value;
            _aspectSetOn = player;
            _aspectSetIndex = _aspectIndex;
        }
    }
}
