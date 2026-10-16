// MediaPlayerForm.Tunes.cs: files with several tunes (SID music: one file can hold 11 songs).
// LibVLC lists them as "titles". When a file has more than one, two buttons next to the seek bar,
// Page Up / Page Down, the media keys and a Tune entry in the right-click menu switch between
// them, and the window title says which one plays ("Tune 3 / 11"). Each tune has its own length
// (SidSongLengths), which is blank when it is not known.
//
// History:
//   2026-10-16  First version.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private IconButton? btnPrevTune;
        private IconButton? btnNextTune;
        private int _seekLeftWithoutTunes;   // where the seek bar starts when the tune buttons are hidden

        private int _tuneCount;   // 0 = not known yet or the file has one tune
        private int _tuneIndex;   // the tune that plays, from 0 (the same number LibVLC uses for its title)
        private bool _tuneChecked;   // the number of tunes has been read from LibVLC
        private long?[]? _tuneLengths;

        private void InitTuneControls()
        {
            btnPrevTune = NewTuneButton(IconKind.PreviousTune, "Previous Tune (Page Up)", () => ChangeTune(_tuneIndex - 1));
            btnNextTune = NewTuneButton(IconKind.NextTune, "Next Tune (Page Down)", () => ChangeTune(_tuneIndex + 1));
            _seekLeftWithoutTunes = trkSeek.Left;
        }

        private IconButton NewTuneButton(IconKind icon, string tip, Action click)
        {
            var button = new IconButton
            {
                BackColor = Color.FromArgb(48, 48, 48),
                ForeColor = Color.FromArgb(230, 230, 230),
                Icon = icon,
                Size = btnStop.Size,
                Top = btnStop.Top,
                TabStop = false,
                Visible = false,
            };
            toolTip.SetToolTip(button, tip);
            button.Click += (s, a) => click();
            pnlControls.Controls.Add(button);
            return button;
        }

        // Forget the tunes of the previous file (called whenever a file is opened).
        private void ResetTunes()
        {
            _tuneCount = 0;
            _tuneIndex = 0;
            _tuneChecked = false;
            _tuneLengths = null;
            ShowTuneControls(false);
            Text = TitleBase();
        }

        private string TitleBase()
        {
            return _isStream && TryGetStreamUri(_filePath, out Uri? uri) ? uri!.Host : Path.GetFileName(_filePath);
        }

        // Called by the timer: reads how many tunes the file has once LibVLC knows it.
        private void CheckTunes()
        {
            if (_tuneChecked || _player == null || _isStream || !_player.IsPlaying || !SidPluginSetup.IsSidFile(_filePath))
            {
                return;
            }

            int count = _player.TitleCount;
            if (count <= 0)
            {
                return;
            }

            _tuneChecked = true;
            if (count > 1)
            {
                _tuneCount = count;
                // LibVLC calls the tune that starts first "title 0" even when the file starts on another one.
                _tuneIndex = Math.Clamp(SidSongLengths.ReadStartSong(_filePath) - 1, 0, count - 1);
                ShowTuneControls(true);
                ApplyTune();
            }
        }

        private void ShowTuneControls(bool show)
        {
            if (btnPrevTune == null || btnNextTune == null || btnPrevTune.Visible == show)
            {
                return;
            }

            int gap = LogicalToDeviceUnits(6);
            btnPrevTune.Left = btnSaveFrame.Right + gap;
            btnNextTune.Left = btnPrevTune.Right + gap;
            btnPrevTune.Visible = btnNextTune.Visible = show;

            // The seek bar keeps its right edge and gives up the room for the buttons.
            int left = show ? btnNextTune.Right + LogicalToDeviceUnits(8) : _seekLeftWithoutTunes;
            trkSeek.SetBounds(left, trkSeek.Top, trkSeek.Right - left, trkSeek.Height);
        }

        // The length and the texts for the tune that plays now.
        private void ApplyTune()
        {
            _knownLengthMs = _tuneLengths != null && _tuneIndex < _tuneLengths.Length ? _tuneLengths[_tuneIndex] ?? 0 : 0;
            if (_tuneCount > 1)
            {
                Text = $"{TitleBase()} - Tune {_tuneIndex + 1} / {_tuneCount}";
                btnPrevTune!.Enabled = _tuneIndex > 0;
                btnNextTune!.Enabled = _tuneIndex < _tuneCount - 1;
            }
            UpdateTime(_player?.Time ?? 0);
        }

        // Starts tune number index (from 0).
        private void ChangeTune(int index)
        {
            MediaPlayer? player = _player;
            if (player == null || _tuneCount < 2 || index < 0 || index >= _tuneCount || index == _tuneIndex)
            {
                return;
            }

            player.Title = index;
            _tuneIndex = index;
            _stepTimeMs = -1;
            ApplyTune();
        }

        private void AddTuneMenu()
        {
            if (_tuneCount < 2)
            {
                return;
            }

            ToolStripMenuItem tunes = new("Tune");
            for (int i = 0; i < _tuneCount; i++)
            {
                int index = i;
                long? length = _tuneLengths != null && i < _tuneLengths.Length ? _tuneLengths[i] : null;
                string text = length is > 0 ? $"Tune {i + 1}   ({FormatTime(length.Value, false)})" : $"Tune {i + 1}";
                tunes.DropDownItems.Add(TrackItem(text, index == _tuneIndex, () => ChangeTune(index)));
            }
            menuVideo.Items.Add(tunes);
        }
    }
}
