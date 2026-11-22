// CheckChannelsForm.cs: IPTV > Check Channels. Goes through the channels of the public IPTV list in the background, asks each one whether it answers
// (ChannelProbe.cs) and remembers the result in the channel history (ChannelHistory.cs): the tooltip of a row in the channel list then says when the
// channel last worked, or since when it does not.
//
// It can take a long time (the list has over 11,000 channels), so it can be stopped at any moment and continued later: every result is saved at once
// (every few seconds), and the next run skips the channels that already have a result from the last 24 hours (the check box asks for all of them).
// Skipped as well: hidden channels, channels the list names "[Geo-blocked]" (they only fail when the computer is in the wrong country) and addresses
// that are not http or https. If many channels in a row fail, the internet connection is tested (InternetCheck.cs): without it the check stops and
// nothing is recorded as failing. After a run the channels that failed can be hidden at once.
//
// History:
//   2026-11-18  First version.
//   2026-11-22  Text no longer cut off (the labels are measured). "Hide Long-Failing Channels": hides the channels whose run of failures began 14 days
//               or more ago (no success since), from the history.
//   2026-11-19  The check box "Check the Hidden Channels Instead" asks only the hidden channels (of this list), and afterwards the button offers a list of
//               the ones that answer, to bring back (UnhideForm: all are ticked, Select All / Select None).

using System.Collections.Concurrent;

namespace SwewolfMediaPlayer
{
    internal sealed class CheckChannelsForm : Form
    {
        private const int MaxParallel = 24;
        private static readonly TimeSpan Fresh = TimeSpan.FromHours(24);
        private static readonly TimeSpan LongFailing = TimeSpan.FromDays(14);   // no answer for this long: the channel is counted as gone

        private readonly string _listUrl;
        private readonly Label _lblStatus = new();
        private readonly ProgressBar _bar = new();
        private readonly CheckBox _chkAgain = new(), _chkHidden = new();
        private bool _hiddenRun;   // the run that is going or was done asked the hidden channels
        private readonly ConcurrentDictionary<string, string> _workedHidden = new(StringComparer.OrdinalIgnoreCase);   // address -> name
        private readonly Button _btnStart = new(), _btnStop = new(), _btnHide = new(), _btnLong = new(), _btnClose = new();
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 500 };

        private CancellationTokenSource? _stop;
        private bool _running;
        private int _todo, _done, _worked, _failed, _unknown, _skipped, _fresh, _consecutiveFailures;
        private bool _offline;
        private string _note = "";
        private int _ticks;
        private readonly ConcurrentBag<string> _failedUrls = new();
        private DateTime _lastOnlineCheck = DateTime.MinValue;

        public CheckChannelsForm(string listUrl)
        {
            _listUrl = listUrl;
            Text = "Check Channels";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.Dpi;

            var info = new Label
            {
                AutoSize = false,
                Text = "Asks the channels of the public IPTV list, one after the other in the background, whether they answer, and remembers the result " +
                       "(point at a channel in the list to see it). It takes a while: you can stop it and continue later.\n\n" +
                       "Skipped: hidden channels, channels named [Geo-blocked], and channels checked in the last 24 hours.",
                Dock = DockStyle.Top,
                Padding = new Padding(12, 12, 12, 0),
            };
            const int width = 660;
            // The labels are as high as their text needs (a fixed height cut the last words off).
            int textWidth = width - 24;
            int Measure(string text) => TextRenderer.MeasureText(text, Font, new Size(textWidth, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
            info.Height = Measure(info.Text) + 12 + 6;
            _lblStatus.AutoSize = false;
            _lblStatus.Dock = DockStyle.Top;
            _lblStatus.Height = 4 * Font.Height + 6 + 6;   // a note, and the lines of the counts
            _lblStatus.Padding = new Padding(12, 6, 12, 0);
            _lblStatus.Text = "Ready.";
            _bar.Dock = DockStyle.Top;
            _bar.Height = 22;
            _bar.Style = ProgressBarStyle.Continuous;
            var barHost = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(12, 6, 12, 6) };
            barHost.Controls.Add(_bar);
            _bar.Dock = DockStyle.Fill;

            _chkAgain.Text = "Check Again Channels Checked in the Last 24 Hours";
            _chkAgain.AutoSize = true;
            _chkAgain.Location = new Point(12, 6);
            _chkHidden.Text = "Check the Hidden Channels Instead";
            _chkHidden.AutoSize = true;
            _chkHidden.Location = new Point(12, 32);
            var checkHost = new Panel { Dock = DockStyle.Top, Height = 58 };
            checkHost.Controls.Add(_chkAgain);
            checkHost.Controls.Add(_chkHidden);
            _chkHidden.CheckedChanged += (_, _) =>
            {
                _chkAgain.Enabled = !_chkHidden.Checked;
                if (!_btnHide.Enabled)
                {
                    _btnHide.Text = _chkHidden.Checked ? "Bring Back Channels That Work" : "Hide Channels That Failed";
                }
            };
            new ToolTip().SetToolTip(_chkHidden, "Asks only the channels you have hidden. Afterwards you can bring back the ones that answer.");

            _btnStart.Text = "Start";
            _btnStop.Text = "Stop";
            _btnHide.Text = "Hide Channels That Failed";
            _btnLong.Text = "Hide Long-Failing Channels";
            new ToolTip().SetToolTip(_btnLong, "Hides the channels that have not answered for 14 days or more: the checks and your own clicks have not found them working since " +
                "the day they started to fail, and that was at least 14 days ago.");
            _btnClose.Text = "Close";
            foreach (Button b in new[] { _btnStart, _btnStop, _btnHide, _btnLong, _btnClose })
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.MinimumSize = new Size(80, 28);
                b.Margin = new Padding(0, 0, 8, 0);
            }
            _btnStop.Enabled = false;
            _btnHide.Enabled = false;
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12, 6, 12, 6), WrapContents = false };
            buttons.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnHide, _btnLong, _btnClose });

            // The last one added is the top one.
            Controls.Add(buttons);
            Controls.Add(checkHost);
            Controls.Add(barHost);
            Controls.Add(_lblStatus);
            Controls.Add(info);
            ClientSize = new Size(width, info.Height + _lblStatus.Height + 34 + 58 + 52);

            _btnStart.Click += async (_, _) => await RunAsync();
            _btnStop.Click += (_, _) => _stop?.Cancel();
            _btnHide.Click += (_, _) =>
            {
                if (_hiddenRun)
                {
                    BringBackWorking();
                }
                else
                {
                    HideFailed();
                }
            };
            _btnLong.Click += async (_, _) => await HideLongFailingAsync();
            _btnClose.Click += (_, _) => Close();
            AcceptButton = _btnStart;
            CancelButton = _btnClose;
            _timer.Tick += (_, _) => UpdateStatus();
        }

        // Centered over the player window, with the final size (CenterParent alone works from the size before the layout is done).
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Owner != null)
            {
                Rectangle area = Screen.FromControl(Owner).WorkingArea;
                int x = Owner.Left + (Owner.Width - Width) / 2;
                int y = Owner.Top + (Owner.Height - Height) / 2;
                Location = new Point(Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - Width)),
                    Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - Height)));
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _stop?.Cancel();
            _timer.Stop();
            ChannelHistory.Flush();
            base.OnFormClosing(e);
        }

        private static bool IsGeoBlocked(M3uEntry e) => e.Name.Contains("Geo-blocked", StringComparison.OrdinalIgnoreCase);

        private async Task RunAsync()
        {
            if (_running)
            {
                return;
            }
            _btnStart.Enabled = false;
            _lblStatus.Text = "Loading the channel list...";
            string? text = await M3uPlaylist.LoadTextAsync(_listUrl);
            if (IsDisposed)
            {
                return;
            }
            if (text == null)
            {
                _lblStatus.Text = "The channel list could not be loaded.";
                _btnStart.Enabled = true;
                return;
            }
            if (!await InternetCheck.IsOnlineAsync())
            {
                _lblStatus.Text = "The computer seems to be offline. Check the internet connection and try again.";
                _btnStart.Enabled = true;
                return;
            }
            if (IsDisposed)
            {
                return;
            }

            // Which channels to ask.
            HashSet<string> hidden = HiddenChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var todo = new List<M3uEntry>();
            _skipped = _fresh = _unknown = 0;
            _hiddenRun = _chkHidden.Checked;
            _workedHidden.Clear();
            foreach (M3uEntry entry in M3uPlaylist.Parse(text))
            {
                if (!seen.Add(entry.Url))
                {
                    continue;
                }
                if (_hiddenRun)
                {
                    // Only the hidden channels, whatever was checked recently (the list is short).
                    if (hidden.Contains(entry.Url) && (entry.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || entry.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) && !IsGeoBlocked(entry))
                    {
                        todo.Add(entry);
                    }
                    continue;
                }
                if (!entry.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !entry.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _unknown++;
                }
                else if (hidden.Contains(entry.Url) || IsGeoBlocked(entry))
                {
                    _skipped++;
                }
                else if (!_chkAgain.Checked && HasRecentResult(entry.Url))
                {
                    _fresh++;
                }
                else
                {
                    todo.Add(entry);
                }
            }

            _todo = todo.Count;
            _done = _worked = _failed = _consecutiveFailures = 0;
            _offline = false;
            _note = "";
            _failedUrls.Clear();
            _bar.Maximum = Math.Max(1, _todo);
            _bar.Value = 0;
            _btnHide.Enabled = false;
            _btnHide.Text = _hiddenRun ? "Bring Back Channels That Work" : "Hide Channels That Failed";
            if (_todo == 0 && _hiddenRun)
            {
                _lblStatus.Text = "None of the hidden channels can be checked here: there are none in this list, or they are geo-blocked.";
                _btnStart.Enabled = true;
                return;
            }
            if (_todo == 0)
            {
                _lblStatus.Text = $"Nothing to check: {_fresh:N0} channels were checked in the last 24 hours, {_skipped:N0} are hidden or geo-blocked.";
                _btnStart.Enabled = true;
                return;
            }

            _running = true;
            _stop = new CancellationTokenSource();
            _btnStop.Enabled = true;
            _btnLong.Enabled = false;
            _chkAgain.Enabled = false;
            _chkHidden.Enabled = false;
            _timer.Start();
            UpdateStatus();
            try
            {
                await System.Threading.Tasks.Parallel.ForEachAsync(todo, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel, CancellationToken = _stop.Token },
                    async (entry, ct) => await CheckOneAsync(entry, ct));
            }
            catch (OperationCanceledException)
            {
                // Stop was clicked (or the connection was lost): what is done is saved.
            }

            _timer.Stop();
            ChannelHistory.Flush();
            _running = false;
            bool finished = _done >= _todo && !_offline;
            _note = _offline ? "Stopped: the internet connection was lost. Nothing was recorded for the channels that failed in the meantime."
                : finished ? "Done." : "Stopped. Start continues with the channels that are not checked yet.";
            if (IsDisposed)
            {
                return;
            }
            _btnStop.Enabled = false;
            _btnStart.Enabled = true;
            _btnLong.Enabled = true;
            _chkAgain.Enabled = !_chkHidden.Checked;
            _chkHidden.Enabled = true;
            _btnHide.Enabled = _hiddenRun ? !_workedHidden.IsEmpty : !_failedUrls.IsEmpty;
            UpdateStatus();
        }

        private static bool HasRecentResult(string url)
        {
            ChannelHistoryEntry? h = ChannelHistory.Get(url);
            if (h == null)
            {
                return false;
            }
            DateTime last = new[] { h.LastWorked ?? DateTime.MinValue, h.LastFailed ?? DateTime.MinValue }.Max();
            return DateTime.Now - last < Fresh;
        }

        private async ValueTask CheckOneAsync(M3uEntry entry, CancellationToken ct)
        {
            bool? works = await ChannelProbe.WorksAsync(entry, ct);
            if (works == null)
            {
                Interlocked.Increment(ref _unknown);
                Interlocked.Increment(ref _done);
                return;
            }
            if (works == true)
            {
                Interlocked.Exchange(ref _consecutiveFailures, 0);
                ChannelHistory.RecordWorked(entry.Url, save: false);
                if (_hiddenRun)
                {
                    _workedHidden[entry.Url] = entry.Name;
                }
                Interlocked.Increment(ref _worked);
                Interlocked.Increment(ref _done);
                return;
            }

            // Many failures in a row: is it the connection? Then nothing is recorded and the check stops.
            if (Interlocked.Increment(ref _consecutiveFailures) >= 6 && !await StillOnlineAsync())
            {
                _offline = true;
                _stop?.Cancel();
                ct.ThrowIfCancellationRequested();
                return;
            }
            ChannelHistory.RecordFailed(entry.Url, save: false);
            if (!_hiddenRun)
            {
                _failedUrls.Add(entry.Url);
            }
            Interlocked.Increment(ref _failed);
            Interlocked.Increment(ref _done);
        }

        // The connection is tested at most every 10 seconds, however many channels fail meanwhile.
        private readonly SemaphoreSlim _onlineGate = new(1);
        private bool _lastOnline = true;

        private async Task<bool> StillOnlineAsync()
        {
            await _onlineGate.WaitAsync();
            try
            {
                if (DateTime.Now - _lastOnlineCheck > TimeSpan.FromSeconds(10))
                {
                    _lastOnline = await InternetCheck.IsOnlineAsync();
                    _lastOnlineCheck = DateTime.Now;
                }
                return _lastOnline;
            }
            finally
            {
                _onlineGate.Release();
            }
        }

        // Updates the text and the bar; the history is saved every few seconds.
        private void UpdateStatus()
        {
            _bar.Value = Math.Min(_bar.Maximum, _done);
            string line = $"Checked {_done:N0} of {_todo:N0}: {_worked:N0} answer, {_failed:N0} do not." +
                (_hiddenRun ? "\nOnly the hidden channels are checked."
                    : $"\nSkipped: {_fresh:N0} checked recently, {_skipped:N0} hidden or geo-blocked, {_unknown:N0} not checkable.");
            _lblStatus.Text = _note.Length > 0 ? _note + "\n" + line : line;
            if (_running && ++_ticks % 10 == 0)
            {
                ChannelHistory.Flush();
            }
        }

        // The hidden channels that answered: a list to choose from (all ticked), the ticked ones come back in the channel list.
        private void BringBackWorking()
        {
            var items = _workedHidden.OrderBy(p => p.Value, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (items.Count == 0)
            {
                return;
            }
            using var dialog = new UnhideForm(items.Select(p => (p.Key, p.Value)).ToList()) { Icon = Icon };
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Chosen.Count == 0)
            {
                return;
            }
            HiddenChannels.RemoveMany(dialog.Chosen);
            foreach (string url in dialog.Chosen)
            {
                _workedHidden.TryRemove(url, out _);
            }
            _btnHide.Enabled = !_workedHidden.IsEmpty;
            _note = $"{dialog.Chosen.Count:N0} channels were brought back (open channel lists show the change the next time they are opened).";
            UpdateStatus();
        }

        // The list of hidden channels that work again.
        private sealed class UnhideForm : Form
        {
            private readonly CheckedListBox _list = new() { CheckOnClick = true, Dock = DockStyle.Fill, IntegralHeight = false };
            private readonly List<string> _urls;

            public List<string> Chosen => _list.CheckedIndices.Cast<int>().Select(i => _urls[i]).ToList();

            public UnhideForm(List<(string Url, string Name)> items)
            {
                Text = "Bring Back Channels";
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimizeBox = false;
                MaximizeBox = false;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.CenterParent;
                AutoScaleMode = AutoScaleMode.Dpi;
                ClientSize = new Size(460, 420);
                MinimumSize = Size;

                _urls = items.Select(i => i.Url).ToList();
                foreach ((string url, string name) in items)
                {
                    _list.Items.Add(name, true);
                }

                var info = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 44,
                    Padding = new Padding(12, 10, 12, 0),
                    Text = $"{items.Count:N0} hidden channels answer again. Tick the ones to bring back.",
                };
                var all = new Button { Text = "Select All", AutoSize = true, MinimumSize = new Size(80, 28) };
                var none = new Button { Text = "Select None", AutoSize = true, MinimumSize = new Size(80, 28) };
                var ok = new Button { Text = "Bring Back", AutoSize = true, MinimumSize = new Size(80, 28), DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(80, 28), DialogResult = DialogResult.Cancel };
                all.Click += (_, _) => SetAll(true);
                none.Click += (_, _) => SetAll(false);
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(12, 8, 12, 4) };
                buttons.Controls.AddRange(new Control[] { all, none, ok, cancel });
                var listHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
                listHost.Controls.Add(_list);

                Controls.Add(listHost);
                Controls.Add(buttons);
                Controls.Add(info);
                AcceptButton = ok;
                CancelButton = cancel;
            }

            private void SetAll(bool on)
            {
                for (int i = 0; i < _list.Items.Count; i++)
                {
                    _list.SetItemChecked(i, on);
                }
            }
        }

        // The channels (of this list) whose run of failures began 14 days or more ago: nothing has found them working since. The history is what knows.
        private async Task HideLongFailingAsync()
        {
            if (_running)
            {
                return;
            }
            _btnLong.Enabled = false;
            string? text = await M3uPlaylist.LoadTextAsync(_listUrl);
            if (IsDisposed)
            {
                return;
            }
            _btnLong.Enabled = true;
            if (text == null)
            {
                _note = "The channel list could not be loaded.";
                UpdateStatus();
                return;
            }

            HashSet<string> hidden = HiddenChannels.All().ToHashSet(StringComparer.OrdinalIgnoreCase);
            DateTime cutoff = DateTime.Now - LongFailing;
            List<string> urls = M3uPlaylist.Parse(text)
                .Where(e => !hidden.Contains(e.Url) && !IsGeoBlocked(e) && ChannelHistory.Get(e.Url)?.FailingSince is DateTime since && since <= cutoff)
                .Select(e => e.Url).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (urls.Count == 0)
            {
                _note = "No channel has been failing for 14 days or more yet. Check the channels now and then: a channel counts when it started to fail 14 days ago or earlier.";
                UpdateStatus();
                return;
            }
            if (MessageBox.Show(this, $"Hide the {urls.Count:N0} channels that have not answered for 14 days or more?\n\n" +
                "You can find them again in the Hidden Channels group (open channel lists show the change the next time they are opened).",
                MainForm.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }
            HiddenChannels.AddMany(urls);
            _note = $"{urls.Count:N0} channels were hidden.";
            UpdateStatus();
        }

        private void HideFailed()
        {
            List<string> urls = _failedUrls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (urls.Count == 0)
            {
                return;
            }
            if (MessageBox.Show(this, $"Hide the {urls.Count:N0} channels that did not answer?\n\nSome of them may still work in the player. " +
                "You can find them again in the Hidden Channels group (open channel lists show the change the next time they are opened).",
                MainForm.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }
            HiddenChannels.AddMany(urls);
            _btnHide.Enabled = false;
            _note = $"{urls.Count:N0} channels were hidden.";
            UpdateStatus();
        }
    }
}
