// MediaPlayerForm.ControlPanel.cs: the "Control Panel" group box of MediaPlayerForm.
//
// Like the Trim group box it is hidden at start (that is not remembered, the options in it are):
// the right-click menu on the control bar shows it. It holds player options; for now only what happens when a video has
// played to its end (used by OnEndReached in MediaPlayerForm.cs).
//
// History:
//   2026-10-13  First version: "When the Video Ends" (last frame, black screen, repeat).
//   2026-10-13  "When the Video Ends" is remembered between runs (AppSettings.cs).
//   2026-10-15  New option "Close Player": the window closes when the video ends.
//   2026-10-23  While the Trim group box is open, the end action is always the last frame (EffectiveEndAction).
//   2026-10-26  A small X in the upper right corner closes the group box (CloseXButton.cs); the Trim group box has one too.
//   2026-10-26  "Show Subtitles" check box, remembered between runs (AppSettings.ShowSubtitles, on by default); the C key switches it.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private enum EndAction
        {
            LastFrame,
            BlackScreen,
            Repeat,
            ClosePlayer,
        }

        private bool _panelVisible;
        private EndAction _whenEnds = EndAction.LastFrame;
        private readonly AppSettings _settings = AppSettings.Load();

        // While the Trim group box is open the player never closes, repeats or goes black at the end:
        // it stays on the last frame, so the user can go on trimming. The chosen option is kept as it was.
        private EndAction EffectiveEndAction => _trimVisible ? EndAction.LastFrame : _whenEnds;

        private void InitControlPanel()
        {
            cboWhenEnds.Items.AddRange(new object[] { "Show the Last Frame", "Show a Black Screen", "Repeat", "Close Player" });
            _whenEnds = Enum.IsDefined((EndAction)_settings.WhenVideoEnds) ? (EndAction)_settings.WhenVideoEnds : EndAction.LastFrame;
            cboWhenEnds.SelectedIndex = (int)_whenEnds;
            chkShowSubtitles.Checked = _settings.ShowSubtitles;
            _subtitleSettingLoaded = true;

            picCover.SizeChanged += (_, _) => ApplyCoverSize();
            picCover.VisibleChanged += (_, _) => ApplyCoverSize();
            ApplyCoverSize();
        }

        private void cboWhenEnds_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboWhenEnds.SelectedIndex >= 0)
            {
                _whenEnds = (EndAction)cboWhenEnds.SelectedIndex;
                // Save from a fresh copy, so a change made in the Settings window meanwhile is not overwritten.
                AppSettings settings = AppSettings.Load();
                settings.WhenVideoEnds = cboWhenEnds.SelectedIndex;
                settings.Save();
            }
        }

        // ---- Show Subtitles ----
        // The check box is remembered between runs. It is applied to every file once its subtitle tracks are known
        // (CheckSubtitles, by the timer): checked = a track is on (the one that was on, or else the first), unchecked =
        // no track. The C key switches it like a click. The Subtitle Track menu only changes the file that plays.

        private bool _subtitleSettingLoaded;   // the check box has its saved value (changes before that are not saved)
        private bool _subtitlesApplied;   // the setting has been applied to the file that plays

        private void chkShowSubtitles_CheckedChanged(object? sender, EventArgs e)
        {
            if (!_subtitleSettingLoaded)
            {
                return;
            }

            // Save from a fresh copy, so a change made in the Settings window meanwhile is not overwritten.
            AppSettings settings = AppSettings.Load();
            settings.ShowSubtitles = chkShowSubtitles.Checked;
            settings.Save();
            ApplySubtitles(chkShowSubtitles.Checked);
            _subtitlesApplied = true;
        }

        // Switches the subtitles of the file that plays on or off.
        private void ApplySubtitles(bool show)
        {
            MediaPlayer? player = _player;
            if (player == null)
            {
                return;
            }

            if (!show)
            {
                if (player.Spu >= 0)
                {
                    _lastSubtitleTrack = player.Spu;
                    player.SetSpu(-1);
                }
                return;
            }

            if (player.Spu >= 0)
            {
                return;
            }
            var tracks = player.SpuDescription;
            if (_lastSubtitleTrack >= 0 && tracks.Any(t => t.Id == _lastSubtitleTrack))
            {
                player.SetSpu(_lastSubtitleTrack);
            }
            else
            {
                // Track id -1 is the "disabled" entry in the list.
                var first = tracks.FirstOrDefault(t => t.Id >= 0);
                if (first.Id >= 0)
                {
                    player.SetSpu(first.Id);
                }
            }
        }

        // Called by the timer: applies the setting once to each file, when it plays and LibVLC has chosen its tracks.
        private void CheckSubtitles()
        {
            MediaPlayer? player = _player;
            if (_subtitlesApplied || player == null || !player.IsPlaying)
            {
                return;
            }

            if (_isStream || SidPluginSetup.IsSidFile(_filePath))
            {
                _subtitlesApplied = true;
                return;
            }

            // LibVLC turns on a track by itself for some files (a default track); a file without one is given a moment.
            bool trackOn = player.Spu >= 0;
            if (chkShowSubtitles.Checked)
            {
                if (!trackOn && player.Time > 1500)
                {
                    ApplySubtitles(true);
                }
                _subtitlesApplied = trackOn || player.Time > 1500;
            }
            else
            {
                if (trackOn)
                {
                    ApplySubtitles(false);
                }
                _subtitlesApplied = trackOn || player.Time > 3000;
            }
        }

        // The C key: switches the check box (which applies and saves it). When the track was changed with the menu so that
        // the check box no longer says what is shown, the key does what the picture says.
        private void SetShowSubtitles(bool show)
        {
            if (chkShowSubtitles.Checked != show)
            {
                chkShowSubtitles.Checked = show;
            }
            else
            {
                ApplySubtitles(show);
            }
        }

        private void mnuShowControlPanel_Click(object? sender, EventArgs e)
        {
            SetPanelVisible(!_panelVisible);
        }

        private void btnCloseControlPanel_Click(object? sender, EventArgs e)
        {
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (visible == _panelVisible)
            {
                return;
            }

            _panelVisible = visible;
            grpControlPanel.Visible = visible && !_fullScreen;
            UpdateMinimumSize();

            // Keep the video as large as it was: the window grows and shrinks with the group box.
            if (!_fullScreen && WindowState == FormWindowState.Normal)
            {
                int height = Height + (visible ? grpControlPanel.Height : -grpControlPanel.Height);
                Height = Math.Min(height, Screen.FromControl(this).WorkingArea.Height);
            }
        }
    }
}
