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

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private Panel? _pnlChannels;
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
            if (text == null || !M3uPlaylist.LooksLikeChannelList(text))
            {
                HideLoading();
                return false;   // an HLS stream, or a file that cannot be read: the player tries it as it is
            }

            List<M3uEntry> entries = await Task.Run(() => M3uPlaylist.Parse(text));
            if (IsDisposed)
            {
                return true;
            }
            if (entries.Count == 0)
            {
                HideLoading();
                MessageBox.Show(this, "The list does not contain any channels.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            _channelListSource = Uri.TryCreate(pathOrUrl.Trim(), UriKind.Absolute, out Uri? listUri) && (listUri.Scheme == "http" || listUri.Scheme == "https") ? pathOrUrl.Trim() : null;
            ShowChannelList(entries);
            Text = Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri? uri) && uri.Scheme.StartsWith("http") ? uri.Host : Path.GetFileName(pathOrUrl);
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

        private void ShowChannelList(List<M3uEntry> entries)
        {
            _channels = entries;
            _favoriteUrls = FavoriteChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            _hiddenUrls = HiddenChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            _channelIndex = -1;
            _channelListShown = true;
            EnsureChannelList();

            _channelGroups = entries.SelectMany(c => GroupsOf(c)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g, StringComparer.CurrentCultureIgnoreCase).ToList();
            _cboChannelGroup!.BeginUpdate();
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
            if (_cboChannelGroup == null || _channels == null)
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

            string[] words = _txtChannelFilter!.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int selected = _cboChannelGroup!.SelectedIndex;
            bool favorites = selected == 1;
            bool recent = selected == 2;
            bool hiddenView = selected == HiddenGroupIndex && _cboChannelGroup.Items.Count > HiddenGroupIndex;
            string? group = selected > 2 && !hiddenView ? _channelGroups[selected - 3] : null;

            var rows = new List<object>();
            if (hiddenView)
            {
                for (int i = 0; i < _channels.Count; i++)
                {
                    if (IsHidden(_channels[i]) && (words.Length == 0 || words.All(w => _channels[i].Name.Contains(w, StringComparison.CurrentCultureIgnoreCase))))
                    {
                        rows.Add(i);
                    }
                }
            }
            else if (favorites)
            {
                // In the order of the list; only those that this list has.
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
                    int i = _channels.FindIndex(c => string.Equals(c.Url, url, StringComparison.OrdinalIgnoreCase));
                    if (i >= 0 && !IsHidden(_channels[i]) && (words.Length == 0 || words.All(w => _channels[i].Name.Contains(w, StringComparison.CurrentCultureIgnoreCase))))
                    {
                        rows.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _channels.Count; i++)
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
            int total = hiddenView ? _channels.Count(IsHidden) : _channels.Count(c => !IsHidden(c));   // the hidden ones are not counted in the other views
            _lblChannelsHeader!.Text = rows.Count == total ? $"Channels ({rows.Count:N0})" : $"Channels ({rows.Count:N0} of {total:N0})";

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
                    _ = PlayChannelAsync((int)_lstChannels.Items[row]);
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
                Width = LogicalToDeviceUnits(120),
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
            filterRow.Controls.Add(_txtChannelFilter);   // the filling control first, then the one docked to the right
            var gap = new Panel { Dock = DockStyle.Right, Width = LogicalToDeviceUnits(6), BackColor = dark };
            filterRow.Controls.Add(gap);
            filterRow.Controls.Add(_cboChannelGroup);

            _lblChannelsHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(30),
                BackColor = dark,
                ForeColor = light,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(LogicalToDeviceUnits(8), 0, 0, 0),
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
            toolTip.SetToolTip(closeList, "Close the Channel List");

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
                string letter = channel.Name.Length > 0 ? char.ToUpperInvariant(channel.Name[0]).ToString() : "?";
                TextRenderer.DrawText(e.Graphics, letter, e.Font, box, Color.FromArgb(110, 110, 110),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            // Name above, group below in grey.
            int left = box.Right + pad;
            int half = e.Bounds.Height / 2;
            int textRight = e.Bounds.Right - pad;
            if (ShowFavoriteStars && _favoriteUrls.Contains(channel.Url))
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
                if (url == null)
                {
                    return;
                }
                if (FavoriteChannels.Contains(url))
                {
                    FavoriteChannels.Remove(url);
                }
                else
                {
                    FavoriteChannels.Add(url);
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
                if (url == null)
                {
                    return;
                }
                SetChannelHidden(url, !HiddenChannels.Contains(url));
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
                url = _channels[index].Url;
                copy.ToolTipText = url;
                favorite.Text = FavoriteChannels.Contains(url) ? "Remove from Favorites" : "Add to Favorites";
                bool hidden = HiddenChannels.Contains(url);
                hide.Text = hidden ? "Unhide Channel" : "Hide Channel";
                favorite.Visible = !hidden;   // a hidden channel is not shown in Favorites
            };
            return menu;
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
            string text = row >= 0 && (int)_lstChannels.Items[row] is int i && i < _channels.Count ? ChannelHistoryText(_channels[i].Url) : "";
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
            _channelIndex = index;
            _channelUserAgentFor = channel.Url;
            _channelUserAgent = channel.UserAgent;
            _lstChannels?.Invalidate();
            ShowVideoMessage($"Opening {channel.Name}...");

            await OpenAnotherAsync(channel.Url, channel.Url, false);

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
                if (history.FailCount >= 3 && history.FailingSince != null && DateTime.Now - history.FailingSince.Value >= TimeSpan.FromDays(1)
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
            menuVideo.Items.Add(TrackItem("Show Channel List", shown, () => SetChannelListShown(!shown)));
        }
    }
}
