// MediaPlayerForm.ChannelList.cs: a list of channels (or streams) from an M3U file or web address, shown beside the video like the
// playlist of PotPlayer. Each row has the logo of the channel, its name and its group; a click plays the channel. A text box filters by
// name (every word must be in the name) and a drop-down narrows the list to one group, since such lists have thousands of channels.
//
// The list is opened by giving the player an .m3u / .m3u8 address or file (main window, command line, drop). An HLS stream is an .m3u8
// file too: it is only a channel list when the content says so (M3uPlaylist.LooksLikeChannelList); anything else plays as before.
// The list is on the right of the window; Tools > Settings can put it (and the tune list) on the left (AppSettings.ListSide).
// A downloaded list is kept on disk and checked for changes, the logos too (M3uPlaylist.cs).
//
// History:
//   2026-11-12  First version.
//   2026-11-13  "Recently Watched" in the group drop-down: the last 10 channels that really played, newest first (RecentChannels.cs).
//   2026-11-18  Channel history (ChannelHistory.cs): a tooltip on each row says when the channel last worked or since when it fails, and after
//               three failures in a row (the first at least a day ago) the user is offered to hide the channel.
//   2026-11-16  Hide Channel / Unhide Channel (right-click): hidden channels are left out of every view and are found in the last group,
//               "Hidden Channels", which only exists while at least one channel is hidden (HiddenChannels.cs).
//   2026-11-13  "Favorites" group (right-click: Add to Favorites / Remove from Favorites), before Recently Watched (FavoriteChannels.cs).
//   2026-11-13  A gold star on the rows of the favorites (ShowFavoriteStars, a setting later).
//   2026-11-19  F8 hides / shows the list; the X says "Hide the Channel List" or "Hide the Playlist".
//   2026-11-26  A list in a file of the user's (streams, or streams and files) is a "Playlist": the group drop-down only when there are two or more groups, Hide Channel only for this window,
//               "When the Media File Ends" = Close Player goes on with the next item. Favorites keep the whole channel, so any channel can be one.
//   2026-11-19  A .m3u / .m3u8 of local files is a playlist, not a channel list: the same panel without filter, group and favorites (MediaPlayerForm.Playlist.cs).

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private Panel? _pnlChannels;
        private CheckBox? _continuousBoxChannels;
        private ListBox? _lstChannels;
        private TextBox? _txtChannelFilter;
        private ComboBox? _cboChannelGroup;
        private Label? _lblChannelsHeader;
        private System.Windows.Forms.Timer? _channelFilterTimer;
        private M3uPlaylist.LogoCache? _logos;
        private List<M3uEntry>? _channels;
        private List<string> _channelGroups = new();   // the groups in the drop-down, after "All Groups"
        private int _channelIndex = -1;                // the entry that plays
        private string? _channelUserAgentFor;          // the stream address that needs _channelUserAgent
        private string? _channelUserAgent;
        private bool _channelListShown = true;
        // A gold star on the rows of the favorites. A setting for this may come later: it will replace this property.
        private bool ShowFavoriteStars => true;
        private HashSet<string> _favoriteUrls = new(StringComparer.OrdinalIgnoreCase);   // FavoriteChannels in memory, for drawing the rows
        private HashSet<string> _hiddenUrls = new(StringComparer.OrdinalIgnoreCase);   // HiddenChannels in memory
        private const string HiddenGroupName = "Hidden Channels";   // the last item of the group drop-down, there only while a channel is hidden
        private const string FavoritesGroupName = "Favorites";   // first item after "All Groups": the channels the user marked
        private const string RecentGroupName = "Recently Watched";   // second item of the group drop-down: the last 10 channels that played
        private int _baseCount;                        // the entries of the list itself; the favorites of other lists come after them in _channels
        private bool _userListMode;                   // the channels come from a file of the user's (shown as a playlist, see ShowChannelList)
        private string _channelListName = "";          // the file name of that list, for the panel header
        private Panel? _channelGroupGap;
        private string? _channelListSource;            // the web address of the list the channels came from (null for a file); kept with a recording

        // Dock side of the lists: right by default, left when the user chose so.
        private DockStyle ListDock() => _settings.ListSide == 1 ? DockStyle.Left : DockStyle.Right;

        // Called when a file or address is opened. Returns true when it was a channel list: it is shown and nothing plays until the user
        // clicks a channel. False: the caller plays it as an ordinary file or stream.
        private async Task<bool> TryOpenChannelListAsync(string pathOrUrl)
        {
            if (!M3uPlaylist.MayBeList(pathOrUrl))
            {
                return false;
            }

            ShowVideoMessage("Loading the channel list...");
            string? text = await M3uPlaylist.LoadTextAsync(pathOrUrl);
            if (IsDisposed)
            {
                return true;
            }
            if (text == null || text.Contains("#EXT-X-", StringComparison.Ordinal))
            {
                HideLoading();
                return false;   // an HLS stream, or a file that cannot be read: the player tries it as it is
            }

            List<M3uEntry> entries = await Task.Run(() => M3uPlaylist.Parse(text));
            if (IsDisposed)
            {
                return true;
            }

            // A list of files on a disk (the explorer's playlist) plays item after item. It does not need #EXTINF lines.
            // A list on the web is never one: its entries that are not addresses (placeholders such as "[NO PUBLIC STREAM]") are left out.
            bool fromWeb = Uri.TryCreate(pathOrUrl.Trim(), UriKind.Absolute, out Uri? listUri) && (listUri.Scheme == "http" || listUri.Scheme == "https");
            entries = M3uPlaylist.WithoutPlaceholders(entries, filesAllowed: !fromWeb);
            bool isFileList = M3uPlaylist.IsFilePlaylist(entries);
            if (!isFileList && !M3uPlaylist.LooksLikeChannelList(text))
            {
                HideLoading();
                return false;
            }
            if (entries.Count == 0)
            {
                HideLoading();
                MessageBox.Show(this, "The list does not contain any channels.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            bool mixed = isFileList && entries.Any(e => M3uPlaylist.IsAddress(e.Url));   // files and streams in one list
            if (isFileList && !(mixed && !fromWeb))
            {
                HideLoading();
                _channelListSource = null;
                Text = Path.GetFileName(pathOrUrl);
                ShowPlaylist(M3uPlaylist.ResolvePaths(entries, pathOrUrl), Path.GetFileNameWithoutExtension(pathOrUrl));
                return true;
            }

            _channelListSource = fromWeb ? pathOrUrl.Trim() : null;
            // A list of streams (or streams and files) in a file of the user's: a playlist like the IPTV list, without groups.
            ShowChannelList(isFileList ? M3uPlaylist.ResolvePaths(entries, pathOrUrl) : entries, userList: !fromWeb, name: Path.GetFileNameWithoutExtension(pathOrUrl));
            Text =Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri? uri) && uri.Scheme.StartsWith("http") ? uri.Host : Path.GetFileName(pathOrUrl);
            ShowVideoMessage("Choose a channel in the list.");
            return true;
        }

        // A message in the video area that does not claim that something is slow (unlike ShowLoading's hint after 5 seconds).
        private void ShowVideoMessage(string text)
        {
            ShowLoading(text);
            _slowLoadTimer?.Stop();
            _slowLoadTimer?.Dispose();
            _slowLoadTimer = null;
            _slowLoadHint = false;
        }

        // userList: the channels come from a file on the user's disk. The panel then says "Playlist", has no Hidden Channels group, and Hide
        // Channel only hides the row until the file is opened again (nothing is saved). Groups and favorites work as in every list.
        // The entries after _baseCount are favorites from other lists: only the Favorites group shows them.
        private void ShowChannelList(List<M3uEntry> entries, bool userList = false, string name = "")
        {
            LeavePlaylistMode();
            _userListMode = userList;
            _channelListName = name;
            _baseCount = entries.Count;
            var have = entries.Select(e => e.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<M3uEntry> extras = FavoriteChannels.Entries().Where(f => !have.Contains(f.Url)).ToList();
            if (extras.Count > 0)
            {
                entries = entries.Concat(extras).ToList();
            }
            _channels = entries;
            _favoriteUrls = FavoriteChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            _hiddenUrls = userList ? new(StringComparer.OrdinalIgnoreCase) : HiddenChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            _channelIndex = -1;
            _channelListShown = true;
            EnsureChannelList();
            UpdateListToggleTexts();

            _channelGroups = entries.SelectMany(c => GroupsOf(c)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g, StringComparer.CurrentCultureIgnoreCase).ToList();
            // The group drop-down is there when the list has more than one group.
            _cboChannelGroup!.Visible = _channelGroups.Count > 1;
            _channelGroupGap!.Visible = _channelGroups.Count > 1;
            _cboChannelGroup.BeginUpdate();
            _cboChannelGroup.Items.Clear();
            _cboChannelGroup.Items.Add("All Groups");
            _cboChannelGroup.Items.Add(FavoritesGroupName);
            _cboChannelGroup.Items.Add(RecentGroupName);
            foreach (string group in _channelGroups)
            {
                _cboChannelGroup.Items.Add(group);
            }
            UpdateHiddenGroupItem();
            _cboChannelGroup.SelectedIndex = 0;
            _cboChannelGroup.EndUpdate();
            _txtChannelFilter!.Clear();

            ApplyChannelFilter();
            _pnlChannels!.Visible = true;
            UpdateChannelPanelSide();
            UpdateRecordButton();   // the list itself is not recorded: nothing plays until a channel is chosen
            LayoutControlBar();
        }

        // The index of "Hidden Channels" in the drop-down (the last item), when it is there.
        private int HiddenGroupIndex => 3 + _channelGroups.Count;

        private bool IsHidden(M3uEntry channel) => _hiddenUrls.Contains(channel.Url);

        // "Hidden Channels" is shown only while this list has a hidden channel. When the last one is unhidden while the group is open, the list goes back to All Groups.
        private void UpdateHiddenGroupItem()
        {
            if (_cboChannelGroup == null || _channels == null || _userListMode)
            {
                return;
            }

            bool any = _channels.Any(IsHidden);
            bool there = _cboChannelGroup.Items.Count > HiddenGroupIndex;
            if (any && !there)
            {
                _cboChannelGroup.Items.Add(HiddenGroupName);
            }
            else if (!any && there)
            {
                bool open = _cboChannelGroup.SelectedIndex == HiddenGroupIndex;
                _cboChannelGroup.Items.RemoveAt(HiddenGroupIndex);
                if (open)
                {
                    _cboChannelGroup.SelectedIndex = 0;
                }
            }
        }

        // A channel can be in several groups: "Entertainment;Family;General".
        private static string[] GroupsOf(M3uEntry channel)
        {
            return channel.Group.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private void HideChannelList()
        {
            LeavePlaylistMode();
            _userListMode = false;
            _channels = null;
            _channelListSource = null;
            _channelIndex = -1;
            if (_pnlChannels != null)
            {
                _pnlChannels.Visible = false;
                _lstChannels!.Items.Clear();
            }
        }

        private void SetChannelListShown(bool shown)
        {
            _channelListShown = shown;
            if (_pnlChannels != null && _channels != null)
            {
                _pnlChannels.Visible = shown;
            }
        }

        private void UpdateChannelPanelSide()
        {
            if (_pnlChannels != null)
            {
                _pnlChannels.Dock = ListDock();
            }
        }

        // Rebuilds the rows from the filter text and the group.
        private void ApplyChannelFilter()
        {
            if (_channels == null || _lstChannels == null)
            {
                return;
            }

            if (_playlistMode)
            {
                var all = new List<object>(_channels.Count);
                for (int i = 0; i < _channels.Count; i++)
                {
                    all.Add(i);
                }
                _lstChannels.BeginUpdate();
                _lstChannels.Items.Clear();
                _lstChannels.Items.AddRange(all.ToArray());
                _lstChannels.EndUpdate();
                _lblChannelsHeader!.Text = $"Playlist ({all.Count:N0})" + (_playlistName.Length > 0 ? " - " + _playlistName : "");
                return;
            }

            string[] words = _txtChannelFilter!.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int selected = _cboChannelGroup!.SelectedIndex;
            bool favorites = selected == 1;
            bool recent = selected == 2;
            bool hiddenView = selected == HiddenGroupIndex && _cboChannelGroup.Items.Count > HiddenGroupIndex;
            string? group = selected > 2 && !hiddenView ? _channelGroups[selected - 3] : null;

            var rows = new List<object>();
            if (hiddenView)
            {
                for (int i = 0; i < _baseCount; i++)
                {
                    if (IsHidden(_channels[i]) && (words.Length == 0 || words.All(w => _channels[i].Name.Contains(w, StringComparison.CurrentCultureIgnoreCase))))
                    {
                        rows.Add(i);
                    }
                }
            }
            else if (favorites)
            {
                // In the order of the list, then the favorites of other lists (they come after the entries of this one).
                HashSet<string> urls = FavoriteChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < _channels.Count; i++)
                {
                    if (urls.Contains(_channels[i].Url) && !IsHidden(_channels[i]) && (words.Length == 0 || words.All(w => _channels[i].Name.Contains(w, StringComparison.CurrentCultureIgnoreCase))))
                    {
                        rows.Add(i);
                    }
                }
            }
            else if (recent)
            {
                // The most recently watched first (not in the order of the list); only those that this list has.
                foreach (string url in RecentChannels.All())
                {
                    int i = _channels.FindIndex(0, _baseCount, c => string.Equals(c.Url, url, StringComparison.OrdinalIgnoreCase));
                    if (i >= 0 && !IsHidden(_channels[i]) && (words.Length == 0 || words.All(w => _channels[i].Name.Contains(w, StringComparison.CurrentCultureIgnoreCase))))
                    {
                        rows.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _baseCount; i++)
                {
                    M3uEntry channel = _channels[i];
                    if (IsHidden(channel))
                    {
                        continue;
                    }
                    if (group != null && !GroupsOf(channel).Contains(group, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (words.Length > 0 && !words.All(w => channel.Name.Contains(w, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        continue;
                    }
                    rows.Add(i);   // the row holds the number of the entry
                }
            }

            _lstChannels.BeginUpdate();
            _lstChannels.Items.Clear();
            _lstChannels.Items.AddRange(rows.ToArray());
            _lstChannels.EndUpdate();
            var own = _channels.Take(_baseCount);   // the favorites of other lists are not part of this list's count
            int total = Math.Max(rows.Count, hiddenView ? own.Count(IsHidden) : own.Count(c => !IsHidden(c)));   // the hidden ones are not counted in the other views
            string title = _userListMode ? "Playlist" : "Channels";
            string count = rows.Count == total ? $"{rows.Count:N0}" : $"{rows.Count:N0} of {total:N0}";
            _lblChannelsHeader!.Text = $"{title} ({count})" + (_userListMode && _channelListName.Length > 0 ? " - " + _channelListName : "");

            // Keep the channel that plays in view when it is in the list (not in the recent list: the newest is on top).
            int at = _channelIndex >= 0 && !recent ? rows.IndexOf(_channelIndex) : -1;
            if (at >= 0)
            {
                _lstChannels.TopIndex = Math.Max(0, at - Math.Max(1, _lstChannels.ClientSize.Height / _lstChannels.ItemHeight) / 2);
            }
        }

        private void EnsureChannelList()
        {
            if (_pnlChannels != null)
            {
                return;
            }

            Color back = Color.FromArgb(48, 48, 48), dark = Color.FromArgb(32, 32, 32), light = Color.FromArgb(230, 230, 230);

            _logos = new M3uPlaylist.LogoCache(LogicalToDeviceUnits(64), LogicalToDeviceUnits(40), () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    try
                    {
                        BeginInvoke(() => _lstChannels?.Invalidate());   // a logo has arrived: draw the rows again
                    }
                    catch (InvalidOperationException)
                    {
                        // The window closed meanwhile.
                    }
                }
            });

            _lstChannels = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = back,
                ForeColor = light,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = LogicalToDeviceUnits(52),
                SelectionMode = SelectionMode.None,   // the row that plays is drawn by DrawChannelRow
                IntegralHeight = false,
                TabStop = false,
            };
            _lstChannels.DrawItem += DrawChannelRow;
            _lstChannels.MouseClick += (s, e) =>
            {
                int row = _lstChannels.IndexFromPoint(e.Location);
                if (row >= 0 && e.Button == MouseButtons.Left)
                {
                    // A channel plays; in a playlist one click jumps to that item (the list itself is not changed).
                    _ = _playlistMode ? PlayPlaylistItemAsync((int)_lstChannels.Items[row]) : PlayChannelAsync((int)_lstChannels.Items[row]);
                }
            };
            _lstChannels.ContextMenuStrip = CreateChannelMenu();

            _txtChannelFilter = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = light,
                BorderStyle = BorderStyle.FixedSingle,
            };
            TextBoxStyle.Apply(_txtChannelFilter);
            _channelFilterTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _channelFilterTimer.Tick += (s, e) =>
            {
                _channelFilterTimer.Stop();
                ApplyChannelFilter();
            };
            // A × at the right end of the box clears it (shown only while there is text). The text keeps clear of it (right margin).
            Color boxBack = _txtChannelFilter.BackColor, clearIdle = Color.FromArgb(160, 160, 160), clearHot = Color.White;
            var clear = new Label
            {
                Dock = DockStyle.Right,
                Width = LogicalToDeviceUnits(22),
                ForeColor = clearIdle,
                BackColor = boxBack,
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Visible = false,
            };
            // The × is drawn as two lines exactly in the middle (the character of a font sits below the middle of its line).
            clear.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                float half = LogicalToDeviceUnits(4);
                float cx = clear.ClientSize.Width / 2f, cy = clear.ClientSize.Height / 2f - 0.8f;   // measured: it sat 0.8 px below the middle of the box
                using var pen = new Pen(clear.ForeColor, Math.Max(1.5f, LogicalToDeviceUnits(2) * 0.8f)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
                e.Graphics.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                e.Graphics.DrawLine(pen, cx - half, cy + half, cx + half, cy - half);
            };
            clear.MouseEnter += (s, e) => clear.ForeColor = clearHot;
            clear.MouseLeave += (s, e) => clear.ForeColor = clearIdle;
            clear.Click += (s, e) =>
            {
                _txtChannelFilter!.Clear();
                _txtChannelFilter.Focus();
            };
            toolTip.SetToolTip(clear, "Clear the filter (Esc)");
            _txtChannelFilter.Controls.Add(clear);
            TextBoxStyle.ApplyRightMargin(_txtChannelFilter, 22);
            _txtChannelFilter.TextChanged += (s, e) =>
            {
                clear.Visible = _txtChannelFilter.TextLength > 0;
                _channelFilterTimer.Stop();   // wait until the typing pauses
                _channelFilterTimer.Start();
            };

            _cboChannelGroup = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = LogicalToDeviceUnits(155),   // about 5 characters wider than before (120); the filter box takes the rest, so their total is the same
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = light,
                MaxDropDownItems = 20,
            };
            _cboChannelGroup.SelectedIndexChanged += (s, e) => ApplyChannelFilter();
            toolTip.SetToolTip(_txtChannelFilter, "Type a part of a channel name to show only the channels that match.\nSeveral words must all be in the name.");
            toolTip.SetToolTip(_cboChannelGroup, "Show only the channels of one group, your favorites, or the last 10 channels you watched.");

            var filterRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(34),
                BackColor = dark,
                Padding = new Padding(LogicalToDeviceUnits(6), LogicalToDeviceUnits(6), LogicalToDeviceUnits(6), LogicalToDeviceUnits(4)),
            };
            _channelFilterRow = filterRow;
            filterRow.Controls.Add(_txtChannelFilter);   // the filling control first, then the one docked to the right
            var gap = new Panel { Dock = DockStyle.Right, Width = LogicalToDeviceUnits(6), BackColor = dark };
            _channelGroupGap = gap;
            filterRow.Controls.Add(gap);
            filterRow.Controls.Add(_cboChannelGroup);

            _lblChannelsHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(30),
                BackColor = dark,
                ForeColor = light,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(LogicalToDeviceUnits(8), 0, LogicalToDeviceUnits(36), 0),   // keeps clear of the X
                AutoEllipsis = true,
                Text = "Channels",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            // A small X in the upper right corner closes the list (Show Channel List in the right-click menu of the video opens it again); red when the pointer is over it,
            // like the one of the Control Panel and Trim group boxes (CloseXButton.cs, the same as in the explorer).
            var closeList = new CloseXButton { Size = new Size(LogicalToDeviceUnits(22), LogicalToDeviceUnits(20)) };
            void PlaceClose() => closeList.Location = new Point(_lblChannelsHeader.ClientSize.Width - closeList.Width - LogicalToDeviceUnits(6), (_lblChannelsHeader.ClientSize.Height - closeList.Height) / 2);
            _lblChannelsHeader.Controls.Add(closeList);
            _lblChannelsHeader.SizeChanged += (s, e) => PlaceClose();
            PlaceClose();
            closeList.Click += (s, e) => SetChannelListShown(false);
            _closeListButton = closeList;
            UpdateListToggleTexts();

            _pnlChannels = new Panel
            {
                Dock = ListDock(),
                Width = LogicalToDeviceUnits(380),
                BackColor = back,
                Visible = false,
            };
            _lstChannels.MouseMove += (s, e) => UpdateChannelTip(e.Location);
            _lstChannels.MouseLeave += (s, e) => _channelTipRow = -1;
            _pnlChannels.Controls.Add(_lstChannels);   // the filling control first, then the ones docked to the top (the last added is the top one)
            _pnlChannels.Controls.Add(filterRow);
            _pnlChannels.Controls.Add(_lblChannelsHeader);
            _continuousBoxChannels = NewContinuousBox();   // Continuous Play, at the bottom: only for a playlist, not for a list of channels
            _continuousBoxChannels.Visible = _playlistMode;
            _pnlChannels.Controls.Add(_continuousBoxChannels);

            Controls.Add(_pnlChannels);
            Controls.SetChildIndex(_pnlChannels, Controls.GetChildIndex(grpControlPanel));
        }

        // A filled five-pointed star, drawn as a polygon so that it does not depend on a font.
        private static void DrawStar(Graphics g, Rectangle box)
        {
            var points = new PointF[10];
            float cx = box.Left + box.Width / 2f, cy = box.Top + box.Height / 2f + box.Height * 0.04f;
            float outer = box.Width / 2f, inner = outer * 0.42f;
            for (int i = 0; i < 10; i++)
            {
                double angle = -Math.PI / 2 + i * Math.PI / 5;
                float r = i % 2 == 0 ? outer : inner;
                points[i] = new PointF(cx + (float)(r * Math.Cos(angle)), cy + (float)(r * Math.Sin(angle)));
            }
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(Color.FromArgb(255, 200, 40)))
            {
                g.FillPolygon(brush, points);
            }
            g.SmoothingMode = oldMode;
        }

        private void DrawChannelRow(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || _channels == null || _logos == null)
            {
                return;
            }

            int index = (int)_lstChannels!.Items[e.Index];
            if (index >= _channels.Count)
            {
                return;
            }
            M3uEntry channel = _channels[index];
            bool current = index == _channelIndex;
            using (var back = new SolidBrush(current ? Color.FromArgb(24, 62, 110) : _lstChannels.BackColor))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
            }

            // The logo box: dark, with the logo centered, or the first letter while there is no logo (yet).
            int pad = LogicalToDeviceUnits(8);
            int boxW = LogicalToDeviceUnits(64), boxH = LogicalToDeviceUnits(40);
            var box = new Rectangle(e.Bounds.Left + pad, e.Bounds.Top + (e.Bounds.Height - boxH) / 2, boxW, boxH);
            using (var boxBrush = new SolidBrush(Color.FromArgb(24, 24, 24)))
            {
                e.Graphics.FillRectangle(boxBrush, box);
            }
            Bitmap? logo = channel.LogoUrl != null ? _logos.Get(channel.LogoUrl, out _) : null;
            if (logo != null)
            {
                e.Graphics.DrawImage(logo, box.Left + (box.Width - logo.Width) / 2, box.Top + (box.Height - logo.Height) / 2, logo.Width, logo.Height);
            }
            else
            {
                string letter = _playlistMode ? (index + 1).ToString() : channel.Name.Length > 0 ? char.ToUpperInvariant(channel.Name[0]).ToString() : "?";
                TextRenderer.DrawText(e.Graphics, letter, e.Font, box, Color.FromArgb(110, 110, 110),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            // Name above, group below in grey.
            int left = box.Right + pad;
            int half = e.Bounds.Height / 2;
            int textRight = e.Bounds.Right - pad;
            if (ShowFavoriteStars && !_playlistMode && _favoriteUrls.Contains(channel.Url))
            {
                int starSize = LogicalToDeviceUnits(14);
                var starBox = new Rectangle(e.Bounds.Right - pad - starSize, e.Bounds.Top + (e.Bounds.Height - starSize) / 2, starSize, starSize);
                DrawStar(e.Graphics, starBox);
                textRight = starBox.Left - pad / 2;
            }
            var nameBounds = new Rectangle(left, e.Bounds.Top + 2, Math.Max(0, textRight - left), half - 2);
            var groupBounds = new Rectangle(left, e.Bounds.Top + half, nameBounds.Width, half - 2);
            const TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            Color text = current ? Color.FromArgb(120, 190, 255) : Color.FromArgb(230, 230, 230);
            TextRenderer.DrawText(e.Graphics, channel.Name, e.Font, nameBounds, text, flags | TextFormatFlags.Bottom);
            TextRenderer.DrawText(e.Graphics, channel.Group.Replace(";", ", "), e.Font, groupBounds, Color.FromArgb(150, 150, 150), flags | TextFormatFlags.Top);
        }

        // The right-click menu of a channel row: Copy Stream Address puts the address of the stream (the https link) on the clipboard.
        private ContextMenuStrip CreateChannelMenu()
        {
            string? url = null;   // the address of the row the menu was opened on
            M3uEntry? entry = null;   // and its channel (a favorite keeps all of it)
            var menu = new ContextMenuStrip();
            var copy = new ToolStripMenuItem("Copy Stream Address");
            copy.Click += (_, _) =>
            {
                if (url == null)
                {
                    return;
                }
                try
                {
                    Clipboard.SetText(url);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // Another program has the clipboard just now: nothing was copied.
                }
            };
            var favorite = new ToolStripMenuItem("Add to Favorites");
            favorite.Click += (_, _) =>
            {
                if (url == null || entry == null)
                {
                    return;
                }
                if (FavoriteChannels.Contains(url))
                {
                    FavoriteChannels.Remove(url);
                }
                else
                {
                    FavoriteChannels.Add(entry!);
                }
                _favoriteUrls = FavoriteChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (_cboChannelGroup?.SelectedIndex == 1)
                {
                    ApplyChannelFilter();   // a channel removed from the Favorites list leaves it at once
                }
                _lstChannels?.Invalidate();
            };
            var hide = new ToolStripMenuItem("Hide Channel");
            hide.Click += (_, _) =>
            {
                if (url != null)
                {
                    ToggleChannelHidden(url);
                }
            };
            menu.Items.Add(favorite);
            menu.Items.Add(hide);
            menu.Items.Add(copy);
            StyleMenu(menu);
            menu.Opening += (_, e) =>
            {
                // Only over a row: which one is found from where the pointer is.
                int row = _lstChannels!.IndexFromPoint(_lstChannels.PointToClient(Cursor.Position));
                int index = row >= 0 ? (int)_lstChannels.Items[row] : -1;
                if (_channels == null || index < 0 || index >= _channels.Count)
                {
                    e.Cancel = true;
                    return;
                }
                entry = _channels[index];
                url = entry.Url;
                copy.ToolTipText = url;
                favorite.Text = FavoriteChannels.Contains(url) ? "Remove from Favorites" : "Add to Favorites";
                bool hidden = !_userListMode && HiddenChannels.Contains(url);
                hide.Text = hidden ? "Unhide Channel" : "Hide Channel";
                favorite.Visible = !hidden && !_playlistMode;   // a hidden channel is not shown in Favorites
                hide.Visible = !_playlistMode;
                copy.Text = _playlistMode ? "Copy File Path" : "Copy Stream Address";
            };
            return menu;
        }

        // Hide Channel / Unhide Channel of the right-click menu, and the Delete key.
        private void ToggleChannelHidden(string url)
        {
            if (_userListMode)
            {
                // Only for this window: the row goes until the file is opened again; nothing is saved.
                _hiddenUrls.Add(url);
                ApplyChannelFilter();
                return;
            }
            SetChannelHidden(url, !HiddenChannels.Contains(url));
        }

        // Keys for the channel list (or playlist) while it is shown, when no text box, drop-down or volume slider has the focus:
        //   Up / Down  change channel (the previous / next row of the list, no wrap-around), like a click on it.
        //   Scroll Lock on: Up / Down only scroll the list by a row, nothing changes channel (like Calc and Excel).
        //   Delete     the same as right-click > Hide Channel (or Unhide Channel) on the channel that plays; not in a file playlist.
        private bool HandleChannelListKey(Keys keyData)
        {
            if (_lstChannels == null || _channels == null || !_channelListShown || ActiveControl is TextBoxBase or ComboBox || ActiveControl == trkVolume)
            {
                return false;
            }

            if (keyData is Keys.Up or Keys.Down)
            {
                int step = keyData == Keys.Down ? 1 : -1;
                if (Control.IsKeyLocked(Keys.Scroll))
                {
                    _lstChannels.TopIndex = Math.Clamp(_lstChannels.TopIndex + step, 0, Math.Max(0, _lstChannels.Items.Count - 1));
                    return true;
                }

                int count = _lstChannels.Items.Count;
                if (count == 0)
                {
                    return true;
                }
                // The row of the channel that plays. When it is not in the list (hidden, filtered away), the next row after its place.
                int row = -1;
                for (int i = 0; i < count; i++)
                {
                    if ((int)_lstChannels.Items[i] == _channelIndex)
                    {
                        row = i;
                        break;
                    }
                }
                int target;
                if (row >= 0)
                {
                    target = Math.Clamp(row + step, 0, count - 1);
                }
                else
                {
                    target = step > 0 ? 0 : count - 1;
                    for (int i = 0; i < count; i++)
                    {
                        int at = (int)_lstChannels.Items[i];
                        if (step > 0 && at > _channelIndex)
                        {
                            target = i;
                            break;
                        }
                        if (step < 0 && at < _channelIndex)
                        {
                            target = i;   // the last one before it
                        }
                    }
                }
                if (target == row)
                {
                    return true;
                }

                int index = (int)_lstChannels.Items[target];
                int visible = Math.Max(1, _lstChannels.ClientSize.Height / _lstChannels.ItemHeight);
                if (target < _lstChannels.TopIndex)
                {
                    _lstChannels.TopIndex = target;
                }
                else if (target >= _lstChannels.TopIndex + visible)
                {
                    _lstChannels.TopIndex = target - visible + 1;
                }
                _ = _playlistMode ? PlayPlaylistItemAsync(index) : PlayChannelAsync(index);
                return true;
            }

            if (keyData == Keys.Delete && !_playlistMode && _channelIndex >= 0 && _channelIndex < _channels.Count)
            {
                ToggleChannelHidden(_channels[_channelIndex].Url);
                return true;
            }
            return false;
        }

        // Hides a channel (or shows it again). A channel that plays goes on playing: only the list changes.
        private void SetChannelHidden(string url, bool hide)
        {
            if (hide)
            {
                HiddenChannels.Add(url);
                FavoriteChannels.Remove(url);   // a hidden channel is not a favorite any more
                _favoriteUrls = FavoriteChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                HiddenChannels.Remove(url);
            }
            _hiddenUrls = HiddenChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            UpdateHiddenGroupItem();
            ApplyChannelFilter();
        }

        // The tooltip of a row: when the channel last worked, or since when it fails.
        private static string ChannelHistoryText(string url)
        {
            ChannelHistoryEntry? h = ChannelHistory.Get(url);
            if (h == null)
            {
                return "";
            }
            var lines = new List<string>();
            if (h.FailingSince != null)
            {
                lines.Add($"Not working since {h.FailingSince:yyyy-MM-dd HH:mm} ({h.FailCount} {(h.FailCount == 1 ? "failure" : "failures")} in a row)");
            }
            if (h.LastWorked != null)
            {
                lines.Add($"Last worked {h.LastWorked:yyyy-MM-dd HH:mm}");
            }
            return string.Join("\n", lines);
        }

        private int _channelTipRow = -1;

        private void UpdateChannelTip(Point at)
        {
            if (_lstChannels == null || _channels == null)
            {
                return;
            }
            int row = _lstChannels.IndexFromPoint(at);
            if (row == _channelTipRow)
            {
                return;
            }
            _channelTipRow = row;
            string text = row >= 0 && (int)_lstChannels.Items[row] is int i && i < _channels.Count ? (_playlistMode ? _channels[i].Url : ChannelHistoryText(_channels[i].Url)) : "";
            toolTip.Hide(_lstChannels);
            toolTip.SetToolTip(_lstChannels, text.Length > 0 ? text : null);
        }

        private async Task PlayChannelAsync(int index)
        {
            if (_channels == null || index < 0 || index >= _channels.Count || _player == null || BlockedByRecording(_channels[index].Url))
            {
                return;
            }

            M3uEntry channel = _channels[index];
            _strayChecks = 0;   // a click on a channel: the safeguard for a stray video window may restart it once (MediaPlayerForm.StrayWindow.cs)
            _channelIndex = index;
            _channelUserAgentFor = channel.Url;
            _channelUserAgent = channel.UserAgent;
            _lstChannels?.Invalidate();
            bool isFile = !M3uPlaylist.IsAddress(channel.Url);   // a playlist of the user's may hold files too
            if (isFile && ProblemWithItem(channel.Url) is string problem)
            {
                if (!PlayerBusy)
                {
                    _ = StopPlayerTask(_player);
                }
                ShowVideoMessage($"{channel.Name} cannot be played.\n{problem}");
                return;
            }
            ShowVideoMessage($"Opening {channel.Name}...");

            await OpenAnotherAsync(channel.Url, channel.Url, false);
            if (isFile)
            {
                return;   // a file is not watched like a stream: it plays, or LibVLC reports an error
            }

            // A stream that never starts (dead, blocked in this country) gives no error of its own. Only a channel that really plays
            // (two checks in a row, half a second apart) goes on the Recently Watched list; after 15 seconds without that we say so.
            int playing = 0;
            for (int waited = 0; waited < 15000 && playing < 2; waited += 500)
            {
                await Task.Delay(500);
                if (IsDisposed || _channelIndex != index || _player == null)
                {
                    return;
                }
                playing = _player.IsPlaying ? playing + 1 : 0;
            }
            if (playing >= 2)
            {
                RecentChannels.Add(channel.Url);
                ChannelHistory.RecordWorked(channel.Url);
                if (_cboChannelGroup?.SelectedIndex == 2)
                {
                    ApplyChannelFilter();
                }
                return;
            }
            if (!IsDisposed && _channelIndex == index && _player != null && !_player.IsPlaying && _channelListShown)
            {
                // Offline: the computer is the problem, not the channel, so nothing is recorded.
                if (!await InternetCheck.IsOnlineAsync())
                {
                    if (!IsDisposed && _channelIndex == index)
                    {
                        ShowVideoMessage($"{channel.Name} does not seem to play.\nThe computer seems to be offline.");
                    }
                    return;
                }
                if (IsDisposed || _channelIndex != index)
                {
                    return;
                }

                ChannelHistoryEntry history = ChannelHistory.RecordFailed(channel.Url);
                ShowVideoMessage($"{channel.Name} does not seem to play.\nMany channels in such lists are offline or blocked. Try another one.");
                _lstChannels?.Invalidate();

                // A channel that keeps failing (three times in a row, the first at least a day ago) can be hidden. "No" asks again at the next failure.
                if (!_userListMode && history.FailCount >= 3 && history.FailingSince != null && DateTime.Now - history.FailingSince.Value >= TimeSpan.FromDays(1)
                    && !HiddenChannels.Contains(channel.Url))
                {
                    DialogResult answer = MessageBox.Show(this,
                        $"{channel.Name} has failed {history.FailCount} times in a row since {history.FailingSince:yyyy-MM-dd}.\n\nHide this channel?\nYou can find it again in the Hidden Channels group.",
                        AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    if (answer == DialogResult.Yes && !IsDisposed)
                    {
                        SetChannelHidden(channel.Url, true);
                    }
                }
            }
        }

        // A file of a playlist of the user's ended (not a stream: a channel that drops is a failure). "When the Media File Ends" decides:
        // Close Player goes on with the next item that is not hidden, and closes the window after the last one; Repeat, Show the Last Frame and
        // Show a Black Screen are done by the ordinary code, so false is returned for them. True: handled here.
        private bool UserListItemEnded()
        {
            if (!_userListMode || _channels == null || _previewing || _isStream || EffectiveEndAction != EndAction.ClosePlayer)
            {
                return false;
            }
            for (int i = _channelIndex + 1; i < _baseCount; i++)
            {
                if (!IsHidden(_channels[i]))
                {
                    _ = PlayChannelAsync(i);
                    return true;
                }
            }
            BeginInvoke(Close);   // the last item: now the window closes (not from inside the LibVLC event)
            return true;
        }

        // While the filter box has the focus the keys are for typing: Space, letters and the like must not reach the player's shortcuts.
        // Escape clears the text, and when it is empty moves the focus away from the box.
        private bool HandleChannelFilterKey(Keys keyData, out bool handled)
        {
            handled = false;
            if (_txtChannelFilter == null || ActiveControl != _txtChannelFilter)
            {
                return false;
            }
            if (keyData == Keys.Escape)
            {
                if (_txtChannelFilter.TextLength > 0)
                {
                    _txtChannelFilter.Clear();
                }
                else
                {
                    ActiveControl = null;
                }
                handled = true;
            }
            return true;   // the caller passes the key on to the text box
        }

        private void DisposeChannelList()
        {
            _channelFilterTimer?.Dispose();
            _channelFilterTimer = null;
            _logos?.Dispose();
            _logos = null;
        }

        private void AddChannelMenu()
        {
            if (_channels == null)
            {
                return;
            }
            bool shown = _channelListShown;
            ToolStripMenuItem toggle = TrackItem(_playlistMode || _userListMode ? "Show Playlist" : "Show Channel List", shown, () => SetChannelListShown(!shown));
            toggle.ShortcutKeyDisplayString = "F8";
            menuVideo.Items.Add(toggle);
        }
    }
}
