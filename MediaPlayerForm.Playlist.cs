// MediaPlayerForm.Playlist.cs: a playlist file (.m3u / .m3u8 whose entries are files on a disk, like the ones the SweWolf File Explorer
// saves) opened in the player. The items play one after the other; the list is shown in the same panel as the channel list.
//
//  - The list is static in this version: no reordering. Clicking a row jumps to that item.
//  - When an item ends the next one starts. After the last one "When the Playlist Ends" decides (the same drop-down as "When the Media
//    File Ends" for a single file): Repeat starts again with item 1, Close Player closes the window, the others stay on the last item.
//  - An item that cannot be played (missing, damaged) is skipped, but not silently: a message in the video area tells which file and why,
//    counts down (Settings: PlaylistNoticeSeconds, 10 by default) and then the next item starts. Skip Now, the Next button, Page Down and
//    the media key do not wait; Previous goes back to the previous item at once; Stop is a regular stop and ends the countdown.
//  - If every item fails in one round, nothing is repeated.
//  - The explorer's own playlist (played through the pipe, file by file) is not this: it knows the list, the player does not.
//
// History:
//   2026-11-19  First version.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private bool _playlistMode;          // the panel shows a playlist (and not a list of channels)
        private int _playlistFailsInRow;     // items in a row that could not be played; reset when one plays
        private Panel? _pnlNotice;
        private Label? _lblNotice;
        private Button? _btnNoticeSkip;
        private System.Windows.Forms.Timer? _noticeTimer;
        private int _noticeRemaining;
        private bool _noticeShown;
        private Panel? _channelFilterRow;    // the filter box and group drop-down above the rows: not used for a playlist

        // Shown when a playlist file is opened. Nothing plays by itself: the first item starts here.
        // The playlist of an audio CD: the rows show "Artist - Title" as soon as the shared lookup knows it (it may answer a little after the start),
        // and the header the name of the disc. Called when the playlist opens and when the lookup has something new (OnCdInfoUpdated).
        private void RefreshCdNames()
        {
            if (!_playlistMode || _channels == null || _channels.Count == 0
                || !_channels.All(c => Path.GetExtension(c.Url).Equals(".cda", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            HookCdInfo();
            bool changed = false;
            for (int i = 0; i < _channels.Count; i++)
            {
                CdTrackInfo? info = CdDiscInfo.InfoOf(_channels[i].Url);
                if (info == null)
                {
                    continue;
                }
                string name = string.IsNullOrWhiteSpace(info.Artist) ? info.Title!.Trim() : $"{info.Artist.Trim()} - {info.Title!.Trim()}";
                if (name != _channels[i].Name)
                {
                    _channels[i] = _channels[i] with { Name = name };
                    changed = true;
                }
            }
            string? disc = CdDiscInfo.DriveTitle(Path.GetPathRoot(_channels[0].Url) ?? "");
            if (!string.IsNullOrEmpty(disc) && disc != _playlistName)
            {
                _playlistName = disc;
                changed = true;
            }
            if (changed)
            {
                ApplyChannelFilter();   // the header; the rows are drawn again
                _lstChannels?.Invalidate();
            }
        }

        private CloseXButton? _closeListButton;   // the X in the header of the panel

        // The X says what it hides: the playlist or the channel list (F8 does the same).
        private void UpdateListToggleTexts()
        {
            if (_closeListButton != null)
            {
                toolTip.SetToolTip(_closeListButton, _playlistMode || _userListMode ? "Hide the Playlist (F8)" : "Hide the Channel List (F8)");
            }
        }

        private string _playlistName = "";   // the file name of the playlist without its extension, shown in the panel header

        private void ShowPlaylist(List<M3uEntry> entries, string name)
        {
            _playlistName = name;
            _userListMode = false;
            CancelPlaylistNotice();
            _playlistMode = true;
            _playlistFailsInRow = 0;
            _channels = entries;
            _channelIndex = -1;
            _channelListShown = true;
            EnsureChannelList();
            UpdateListToggleTexts();
            _channelFilterRow!.Visible = false;
            ApplyChannelFilter();
            _pnlChannels!.Visible = true;
            UpdateChannelPanelSide();
            UpdateRecordButton();
            UpdateEndLabel();
            RefreshNavButtons();
            LayoutControlBar();
            RefreshCdNames();
            _ = PlayPlaylistItemAsync(0);
        }

        // Called when the list goes away (another file is opened, or the list is replaced).
        private void LeavePlaylistMode()
        {
            if (!_playlistMode)
            {
                return;
            }
            CancelPlaylistNotice();
            _playlistMode = false;
            if (_channelFilterRow != null)
            {
                _channelFilterRow.Visible = true;
            }
            UpdateEndLabel();
            UpdateListToggleTexts();
            RefreshNavButtons();
        }

        // The drop-down of the Control Panel is about the file, or about the whole playlist.
        private void UpdateEndLabel()
        {
            lblWhenEnds.Text = _playlistMode ? "When the Playlist Ends" : "When the Media File Ends";
            if (_continuousBoxChannels != null)
            {
                _continuousBoxChannels.Visible = _playlistMode;
            }
        }

        private async Task PlayPlaylistItemAsync(int index)
        {
            if (!_playlistMode || _channels == null || index < 0 || index >= _channels.Count || _player == null || IsDisposed)
            {
                return;
            }

            CancelPlaylistNotice();
            M3uEntry item = _channels[index];
            _channelIndex = index;
            _playlistItemPlayed = false;
            _lstChannels?.Invalidate();
            if (_lstChannels != null && _lstChannels.ClientSize.Height > 0)
            {
                int rows = Math.Max(1, _lstChannels.ClientSize.Height / _lstChannels.ItemHeight);
                if (index < _lstChannels.TopIndex || index >= _lstChannels.TopIndex + rows)
                {
                    _lstChannels.TopIndex = Math.Max(0, index - rows / 2);   // the item that plays stays in view
                }
            }
            RefreshNavButtons();
            if (BlockedByRecording(item.Url))
            {
                return;
            }

            string? problem = ProblemWithItem(item.Url);
            if (problem != null)
            {
                OnPlaylistItemFailed(index, problem);
                return;
            }
            await OpenAnotherAsync(item.Url, item.Url, false);
        }

        // Why a file cannot be played, found before LibVLC is asked; null when it looks fine.
        private static string? ProblemWithItem(string path)
        {
            if (M3uPlaylist.IsAddress(path) || DvdSupport.IsDvdPath(path))
            {
                return null;
            }
            try
            {
                if (Directory.Exists(path))
                {
                    return null;
                }
                if (!File.Exists(path))
                {
                    string? root = Path.GetPathRoot(path);
                    return !string.IsNullOrEmpty(root) && !Directory.Exists(root)
                        ? $"The drive or network location {root} is not available."
                        : "The file was not found.";
                }
                return new FileInfo(path).Length == 0 ? "The file is empty." : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return "The file cannot be read: " + ex.Message;
            }
        }

        // The item ended by itself (or its last tune did). True if the playlist took over: the next item starts, or the list repeats or closes.
        // False: the end of the last item with "Show the Last Frame" or "Show a Black Screen", which the ordinary code does.
        private bool PlaylistItemEnded()
        {
            if (!_playlistMode || _channels == null || _previewing)
            {
                return false;
            }
            // A damaged file often does not raise an error: LibVLC just ends it without ever having played. That is a failure, not an end.
            if (!_playlistItemPlayed && _channelIndex >= 0 && _channelIndex < _channels.Count && !M3uPlaylist.IsAddress(_channels[_channelIndex].Url))
            {
                OnPlaylistItemFailed(_channelIndex, "The file could not be played. It may be damaged or in a format the player does not support.");
                return true;
            }
            if (!_continuousPlay)
            {
                return false;   // Continuous Play is off: this item was all that was wanted, it ends like a single file
            }
            return PlaylistGoNext(_channelIndex);
        }

        private bool _playlistItemPlayed;   // LibVLC reported a time above 0 for the item that plays

        // From item `index` (it ended or could not be played) to the next one; after the last one the end action decides.
        private bool PlaylistGoNext(int index)
        {
            if (_channels == null)
            {
                return false;
            }
            if (index + 1 < _channels.Count)
            {
                _ = PlayPlaylistItemAsync(index + 1);
                return true;
            }
            switch (EffectiveEndAction)
            {
                case EndAction.ClosePlayer:
                    BeginInvoke(Close);   // not from inside the LibVLC event
                    return true;
                case EndAction.Repeat:
                    if (_playlistFailsInRow >= _channels.Count)
                    {
                        return false;   // nothing in the list plays: repeating would never stop
                    }
                    _ = PlayPlaylistItemAsync(0);
                    return true;
                default:
                    return false;
            }
        }

        // The Next and Previous buttons, Page Up / Page Down and the media keys (SkipTrack): at once, also during the countdown.
        private void PlaylistSkip(int direction)
        {
            if (_channels == null)
            {
                return;
            }
            int target = _channelIndex + direction;
            if (target >= _channels.Count)
            {
                return;   // the last item: there is no next one (the button is greyed out)
            }
            _ = PlayPlaylistItemAsync(Math.Max(0, target));   // Previous on the first item plays it again
        }

        // LibVLC could not play the item that plays (EncounteredError), or the file is missing.
        private void OnPlaylistItemFailed(int index, string reason)
        {
            if (!_playlistMode || _channels == null || index < 0 || index >= _channels.Count || (_noticeShown && index == _channelIndex && _noticeTimer != null))
            {
                return;
            }

            _channelIndex = index;
            _playlistFailsInRow++;
            HideLoading();
            var player = _player;
            if (player != null && !PlayerBusy)
            {
                _ = StopPlayerTask(player);
            }
            ShowStopped();

            M3uEntry item = _channels[index];
            string name = Path.GetFileName(item.Url);
            if (string.IsNullOrEmpty(name))
            {
                name = item.Url;
            }
            bool allFailed = _playlistFailsInRow >= _channels.Count;
            string head = $"This item cannot be played:\n{name}\n\n{reason}";
            if (allFailed)
            {
                ShowPlaylistNotice(head + "\n\nNone of the items in this playlist could be played.", 0);
                return;
            }
            ShowPlaylistNotice(head, Math.Clamp(_settings.PlaylistNoticeSeconds, 1, 60));
        }

        private string NoticeFooter(int seconds)
        {
            string what;
            if (_channels != null && _channelIndex + 1 < _channels.Count)
            {
                what = "The next item starts";
            }
            else
            {
                what = EffectiveEndAction switch
                {
                    EndAction.Repeat => "The playlist starts again",
                    EndAction.ClosePlayer => "The player closes",
                    _ => "This message closes",
                };
            }
            return $"{what} in {seconds} {(seconds == 1 ? "second" : "seconds")}.";
        }

        // seconds = 0: the message stays until the user does something (nothing more to play).
        private void ShowPlaylistNotice(string text, int seconds)
        {
            EnsureNotice();
            _noticeShown = true;
            _noticeBase = text;
            _noticeRemaining = seconds;
            _btnNoticeSkip!.Visible = seconds > 0 && _channels != null && _channelIndex + 1 < _channels.Count;
            ApplyNoticeText();
            _pnlNotice!.Visible = true;
            _pnlNotice.BringToFront();
            PositionNotice();
            _noticeTimer!.Stop();
            if (seconds > 0)
            {
                _noticeTimer.Start();
            }
        }

        private string _noticeBase = "";

        private void ApplyNoticeText()
        {
            _lblNotice!.Text = _noticeRemaining > 0 ? _noticeBase + "\n\n" + NoticeFooter(_noticeRemaining) : _noticeBase;
            PositionNotice();
        }

        private void EnsureNotice()
        {
            if (_pnlNotice != null)
            {
                return;
            }

            Color light = Color.FromArgb(230, 230, 230);
            _lblNotice = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(LogicalToDeviceUnits(520), 0),
                ForeColor = light,
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(10)),
            };
            _btnNoticeSkip = new Button
            {
                Text = "Skip Now",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = light,
                TabStop = false,
                Margin = new Padding(0),
            };
            toolTip.SetToolTip(_btnNoticeSkip, "Start the next item now (the Next button does the same).");
            _btnNoticeSkip.Click += (_, _) => PlaylistSkip(1);

            _pnlNotice = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(LogicalToDeviceUnits(24), LogicalToDeviceUnits(16), LogicalToDeviceUnits(24), LogicalToDeviceUnits(16)),
                Visible = false,
            };
            _pnlNotice.Controls.Add(_lblNotice);
            _pnlNotice.Controls.Add(_btnNoticeSkip);
            Controls.Add(_pnlNotice);
            videoView.SizeChanged += (_, _) => PositionNotice();

            _noticeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _noticeTimer.Tick += (_, _) =>
            {
                _noticeRemaining--;
                if (_noticeRemaining > 0)
                {
                    ApplyNoticeText();
                    return;
                }
                _noticeTimer.Stop();
                int index = _channelIndex;
                CancelPlaylistNotice();
                if (!PlaylistGoNext(index))
                {
                    ShowStopped();   // the end of the list with an end action that stays where it is
                }
            };
        }

        private void PositionNotice()
        {
            if (_pnlNotice == null || !_pnlNotice.Visible && !_noticeShown)
            {
                return;
            }
            Rectangle area = videoView.Bounds;
            Size size = _pnlNotice.GetPreferredSize(Size.Empty);
            _pnlNotice.Location = new Point(area.Left + Math.Max(0, (area.Width - size.Width) / 2), area.Top + Math.Max(0, (area.Height - size.Height) / 2));
        }

        // Stop, another item, another file, or the list closed: the message and its countdown end.
        private void CancelPlaylistNotice()
        {
            _noticeTimer?.Stop();
            _noticeShown = false;
            if (_pnlNotice != null)
            {
                _pnlNotice.Visible = false;
            }
        }
    }
}
