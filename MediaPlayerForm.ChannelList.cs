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
            _channelIndex = -1;
            _channelListShown = true;
            EnsureChannelList();

            _channelGroups = entries.SelectMany(c => GroupsOf(c)).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g, StringComparer.CurrentCultureIgnoreCase).ToList();
            _cboChannelGroup!.BeginUpdate();
            _cboChannelGroup.Items.Clear();
            _cboChannelGroup.Items.Add("All Groups");
            foreach (string group in _channelGroups)
            {
                _cboChannelGroup.Items.Add(group);
            }
            _cboChannelGroup.SelectedIndex = 0;
            _cboChannelGroup.EndUpdate();
            _txtChannelFilter!.Clear();

            ApplyChannelFilter();
            _pnlChannels!.Visible = true;
            UpdateChannelPanelSide();
            UpdateRecordButton();   // the list itself is not recorded: nothing plays until a channel is chosen
            LayoutControlBar();
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
            string? group = _cboChannelGroup!.SelectedIndex > 0 ? _channelGroups[_cboChannelGroup.SelectedIndex - 1] : null;

            var rows = new List<object>();
            for (int i = 0; i < _channels.Count; i++)
            {
                M3uEntry channel = _channels[i];
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

            _lstChannels.BeginUpdate();
            _lstChannels.Items.Clear();
            _lstChannels.Items.AddRange(rows.ToArray());
            _lstChannels.EndUpdate();
            _lblChannelsHeader!.Text = rows.Count == _channels.Count ? $"Channels ({rows.Count:N0})" : $"Channels ({rows.Count:N0} of {_channels.Count:N0})";

            // Keep the channel that plays in view when it is in the list.
            int at = _channelIndex >= 0 ? rows.IndexOf(_channelIndex) : -1;
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
            _txtChannelFilter.TextChanged += (s, e) =>
            {
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
            toolTip.SetToolTip(_cboChannelGroup, "Show only the channels of one group.");

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

            _pnlChannels = new Panel
            {
                Dock = ListDock(),
                Width = LogicalToDeviceUnits(380),
                BackColor = back,
                Visible = false,
            };
            _pnlChannels.Controls.Add(_lstChannels);   // the filling control first, then the ones docked to the top (the last added is the top one)
            _pnlChannels.Controls.Add(filterRow);
            _pnlChannels.Controls.Add(_lblChannelsHeader);

            Controls.Add(_pnlChannels);
            Controls.SetChildIndex(_pnlChannels, Controls.GetChildIndex(grpControlPanel));
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
            var nameBounds = new Rectangle(left, e.Bounds.Top + 2, Math.Max(0, e.Bounds.Right - left - pad), half - 2);
            var groupBounds = new Rectangle(left, e.Bounds.Top + half, nameBounds.Width, half - 2);
            const TextFormatFlags flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            Color text = current ? Color.FromArgb(120, 190, 255) : Color.FromArgb(230, 230, 230);
            TextRenderer.DrawText(e.Graphics, channel.Name, e.Font, nameBounds, text, flags | TextFormatFlags.Bottom);
            TextRenderer.DrawText(e.Graphics, channel.Group.Replace(";", ", "), e.Font, groupBounds, Color.FromArgb(150, 150, 150), flags | TextFormatFlags.Top);
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

            // A stream that never starts (dead, blocked in this country) gives no error of its own: say so.
            await Task.Delay(15000);
            if (!IsDisposed && _channelIndex == index && _player != null && !_player.IsPlaying && _channelListShown)
            {
                ShowVideoMessage($"{channel.Name} does not seem to play.\nMany channels in such lists are offline or blocked. Try another one.");
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
