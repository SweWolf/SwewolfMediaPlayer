// MediaPlayerForm.Tunes.cs: files with several tunes (SID music: one file can hold 11 songs).
// LibVLC lists them as "titles". When a file has more than one, two buttons next to the seek bar,
// Page Up / Page Down, the media keys and a Tune entry in the right-click menu switch between
// them, and the window title says which one plays ("Tune 3 / 11"). Each tune has its own length
// (SidSongLengths), which is blank when it is not known.
//
// History:
//   2026-10-16  First version.
//   2026-10-16  The next tune starts by itself 3 seconds after a tune has played to its known length.
//   2026-10-16  LayoutControlBar uses what the buttons were set to (_saveFrameShown, _tuneButtonsShown), not their Visible getter.
//   2026-10-21  The back button, Page Up and the previous-track key first go to the start of the file or tune when it has played 3 seconds or more.
//   2026-10-21  The same two buttons skip to the previous / next track while the explorer runs a playlist (MediaPlayerForm.Pipe.cs: TrackSkipping,
//               SkipTrack); they are shown for any file then, and enabled at both ends (the explorer decides what comes next).
//   2026-10-23  Tune names from HVSC's STIL in the window title and the Tune menu; a list of the tunes on the right (MediaPlayerForm.TuneList.cs).

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private IconButton? btnPrevTune;
        private IconButton? btnNextTune;

        private int _tuneCount;   // 0 = not known yet or the file has one tune
        private int _tuneIndex;   // the tune that plays, from 0 (the same number LibVLC uses for its title)
        private bool _tuneChecked;   // the number of tunes has been read from LibVLC
        private bool _tuneEndHandled;   // the end of this tune has been dealt with (so Play afterwards does not repeat it)
        private long _tuneAdvanceAtTicks;   // when the next tune starts (Environment.TickCount64), 0 = not waiting
        private long?[]? _tuneLengths;

        private void InitTuneControls()
        {
            btnPrevTune = NewTuneButton(IconKind.PreviousTune, "Previous Tune (Page Up)", () => NavButton(-1));
            btnNextTune = NewTuneButton(IconKind.NextTune, "Next Tune (Page Down)", () => NavButton(+1));
        }

        // The two buttons: tunes of a SID file, or tracks of the explorer's playlist.
        private void NavButton(int direction)
        {
            if (direction < 0 && RestartIfPlayedAWhile()) return;
            if (TrackSkipping) SkipTrack(direction);
            else ChangeTune(_tuneIndex + direction);
        }

        // The "back" button, Page Up and the previous-track media key: after more than 3 seconds of the file (or tune) they go back to its start,
        // and only within the first 3 seconds do they go to the previous file or tune. True if it went back to the start.
        private bool RestartIfPlayedAWhile()
        {
            if (_player == null || !CanSeek || _player.Time < 3000)
            {
                return false;
            }
            SeekTo(0);
            return true;
        }

        // Called when the playlist of the explorer starts or ends: the buttons change meaning (and show for any file while it runs).
        private void RefreshNavButtons()
        {
            if (btnPrevTune == null || btnNextTune == null) return;

            ShowTuneControls(_tuneCount > 1);
            toolTip.SetToolTip(btnPrevTune, TrackSkipping ? "Previous Track" : "Previous Tune (Page Up)");
            toolTip.SetToolTip(btnNextTune, TrackSkipping ? "Next Track" : "Next Tune (Page Down)");
            if (TrackSkipping)
            {
                btnPrevTune.Enabled = btnNextTune.Enabled = true;
            }
            else if (_tuneCount > 1)
            {
                ApplyTune();
            }
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
            _tuneEndHandled = false;
            _tuneAdvanceAtTicks = 0;
            _restoreTune = -1;
            _tuneLengths = null;
            ShowTuneControls(false);
            ResetSidInfo();
            Text = TitleBase();
        }

        private string TitleBase()
        {
            return _isStream && TryGetStreamUri(_filePath, out Uri? uri) ? uri!.Host : Path.GetFileName(_filePath);
        }

        // Called by the timer: reads how many tunes the file has once LibVLC knows it.
        private void CheckTunes()
        {
            CheckTuneEnd();
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
                if (_restoreTune >= 0)
                {
                    int tune = _restoreTune;
                    _restoreTune = -1;
                    ChangeTune(tune);   // after a restart (new visualization): back to the tune that played
                }
            }
        }

        private void ShowTuneControls(bool show)
        {
            show |= TrackSkipping;   // while the explorer runs a playlist the buttons skip tracks, for any file
            if (btnPrevTune == null || btnNextTune == null || _tuneButtonsShown == show)
            {
                return;
            }

            _tuneButtonsShown = show;
            btnPrevTune.Visible = btnNextTune.Visible = show;
            LayoutControlBar();
        }

        // The Save Frame button is only useful when there is a picture to save: a video, or an audio
        // file that shows its cover art. SID music and audio without a picture have none.
        private void UpdateSaveFrameButton()
        {
            _saveFrameShown = !IsAudioFile(_filePath) || picCover.Visible;
            btnSaveFrame.Visible = _saveFrameShown;
            LayoutControlBar();
        }

        // What the Visible property of these buttons was set to. Their Visible getter says false while the whole control bar
        // is hidden (full screen, the pointer idle), and then the seek bar was laid out under the Save Frame button.
        private bool _saveFrameShown = true;
        private bool _tuneButtonsShown;

        // The buttons left of the seek bar follow each other without gaps for hidden ones, and the
        // seek bar takes the rest (it keeps its right edge).
        private void LayoutControlBar()
        {
            if (btnPrevTune == null || btnNextTune == null)
            {
                return;
            }

            int gap = LogicalToDeviceUnits(6);
            int x = btnStop.Right;
            if (_saveFrameShown)
            {
                btnSaveFrame.Left = x + gap;
                x = btnSaveFrame.Right;
            }
            if (btnBookmark != null)   // right of the camera button
            {
                btnBookmark.Left = x + gap;
                x = btnBookmark.Right;
            }
            if (_tuneButtonsShown)
            {
                btnPrevTune.Left = x + gap;
                btnNextTune.Left = btnPrevTune.Right + gap;
                x = btnNextTune.Right;
            }

            int left = x + LogicalToDeviceUnits(8);
            trkSeek.SetBounds(left, trkSeek.Top, trkSeek.Right - left, trkSeek.Height);
        }

        // The length and the texts for the tune that plays now.
        private void ApplyTune()
        {
            _knownLengthMs = _tuneLengths != null && _tuneIndex < _tuneLengths.Length ? _tuneLengths[_tuneIndex] ?? 0 : 0;
            if (_tuneCount > 1)
            {
                Text = $"{TitleBase()} - Tune {_tuneIndex + 1} / {_tuneCount}{(TuneTitle(_tuneIndex) is { } name ? " - " + name : "")}";
                if (!TrackSkipping)
                {
                    btnPrevTune!.Enabled = _tuneIndex > 0;
                    btnNextTune!.Enabled = _tuneIndex < _tuneCount - 1;
                }
            }
            RefreshSidInfo();
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
            _tuneEndHandled = false;
            _tuneAdvanceAtTicks = 0;
            _stepTimeMs = -1;
            ApplyTune();
        }

        // A tune that has played to its known length stops (silence), and 3 seconds later the next one
        // starts. After the last tune the "When the Video Ends" option decides: Repeat starts again with
        // the first tune, Close Player closes the window, the others leave the player paused. A tune with
        // an unknown length plays on until the user chooses another.
        private void CheckTuneEnd()
        {
            MediaPlayer? player = _player;
            if (_tuneCount < 2 || player == null || _restoringEnd)
            {
                return;
            }

            if (_tuneAdvanceAtTicks != 0)
            {
                if (Environment.TickCount64 >= _tuneAdvanceAtTicks)
                {
                    _tuneAdvanceAtTicks = 0;
                    AdvanceTune(player);
                }
                return;
            }

            if (_tuneEndHandled || _knownLengthMs <= 0 || !player.IsPlaying || player.Time < _knownLengthMs)
            {
                return;
            }

            _tuneEndHandled = true;
            player.SetPause(true);
            _tuneAdvanceAtTicks = Environment.TickCount64 + 3000;
        }

        private void AdvanceTune(MediaPlayer player)
        {
            int next = _tuneIndex + 1;
            if (next >= _tuneCount)
            {
                if (ReportEnded()) return;   // the explorer's playlist goes on: it sends the next file
                if (EffectiveEndAction == EndAction.ClosePlayer)
                {
                    Close();
                    return;
                }
                if (EffectiveEndAction != EndAction.Repeat)
                {
                    return;   // stays paused at the end of the last tune
                }
                next = 0;
            }

            ChangeTune(next);
            player.SetPause(false);
        }

        private void AddTuneMenu()
        {
            if (_tuneCount < 2)
            {
                return;
            }

            ToolStripMenuItem tunes = new("Tune");
            bool listShown = _settings.ShowTuneList;
            tunes.DropDownItems.Add(TrackItem("Show Tune List", listShown, () => SetTuneListShown(!listShown)));
            tunes.DropDownItems.Add(new ToolStripSeparator());
            for (int i = 0; i < _tuneCount; i++)
            {
                int index = i;
                long? length = _tuneLengths != null && i < _tuneLengths.Length ? _tuneLengths[i] : null;
                string text = length is > 0 ? $"{TuneText(i)}   ({FormatTime(length.Value, false)})" : TuneText(i);
                tunes.DropDownItems.Add(TrackItem(text, index == _tuneIndex, () => ChangeTune(index)));
            }
            menuVideo.Items.Add(tunes);
        }
    }
}
