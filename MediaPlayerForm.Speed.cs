// MediaPlayerForm.Speed.cs: the playback speed (the SpeedBox in the control bar, left of the time).
//
// The box shows the speed and steps through a fixed list with its arrows (up = faster), the wheel, and the keys
// + / - (also ] / [ on keyboards that have them without a modifier) and Backspace (back to normal speed).
// The speed is not remembered: every new file starts at normal speed (ResetSpeed in StartMedia). It is off
// for files the user cannot jump in (streams, SID music), and the trim preview starts at normal speed.
//
// LibVLC may refuse a rate until the file plays, so the timer (CheckSpeed) sets it again until it holds.
//
// History:
//   2026-10-26  First version.

using System.Globalization;
using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private static readonly double[] SpeedSteps = { 0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4 };
        private const int NormalSpeedIndex = 3;

        private SpeedBox? speedBox;
        private int _speedIndex = NormalSpeedIndex;

        private void InitSpeedBox()
        {
            speedBox = new SpeedBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Name = "speedBox",
                Size = new Size(64, btnStop.Height),
                Top = btnStop.Top,
            };
            speedBox.Left = lblTime.Left - 6 - speedBox.Width;
            toolTip.SetToolTip(speedBox, "Playback Speed (+ and - keys, Backspace for normal speed)");
            speedBox.Step += (_, direction) => StepSpeed(direction);
            speedBox.ResetRequested += (_, _) => ResetSpeed();
            pnlControls.Controls.Add(speedBox);
            UpdateSpeedBox();   // ApplySeekState switches it on and off
        }

        private void StepSpeed(int direction)
        {
            int index = Math.Clamp(_speedIndex + direction, 0, SpeedSteps.Length - 1);
            if (index == _speedIndex)
            {
                return;
            }

            _speedIndex = index;
            UpdateSpeedBox();
            _player?.SetRate((float)SpeedSteps[_speedIndex]);
        }

        // Back to normal speed (a new file, the trim preview, Backspace).
        private void ResetSpeed()
        {
            _speedIndex = NormalSpeedIndex;
            UpdateSpeedBox();
            _player?.SetRate(1f);
        }

        private void UpdateSpeedBox()
        {
            if (speedBox == null)
            {
                return;
            }

            speedBox.ValueText = SpeedSteps[_speedIndex].ToString("0.##", CultureInfo.InvariantCulture) + "x";
            speedBox.Highlight = _speedIndex != NormalSpeedIndex;
        }

        // Called by the timer: the rate of the player is the one in the box.
        private void CheckSpeed()
        {
            MediaPlayer? player = _player;
            if (player == null || !player.IsPlaying)
            {
                return;
            }

            float wanted = (float)SpeedSteps[_speedIndex];
            if (Math.Abs(player.Rate - wanted) > 0.01f)
            {
                player.SetRate(wanted);
            }
        }

        private bool SpeedAvailable => speedBox is { Enabled: true };
    }
}
