// MediaPlayerForm.TuneList.cs: a list of all the tunes of a SID file on the right side of the window (like the
// playlist of PotPlayer), shown while a file with several tunes plays. A click on a row plays that tune. A tune
// with a name in HVSC's STIL (SidTuneTitles.cs) shows the name, the others "Tune N". The same texts (author, title,
// the name of the tune that plays) are shown in the top left corner (MediaPlayerForm.TrackInfo.cs). The list can be
// hidden from the Tune menu (right-click); that choice is remembered (AppSettings.ShowTuneList).
//
// History:
//   2026-10-23  First version.
//   2026-11-14  The top left texts: title, author and released (file order), without empty or "???" ones, each with its label.
//   2026-11-22  A double click on a tune plays it, also when the player is stopped or paused.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private Panel? _pnlTuneList;
        private ListBox? _lstTunes;
        private Dictionary<int, SidTuneInfo>? _tuneInfos;   // STIL texts by tune number (from 1)
        private SidHeader? _sidHeader;

        // The STIL name of tune number index (from 0), or null.
        private string? TuneTitle(int index)
        {
            if (_tuneInfos == null)
            {
                return null;
            }
            // A file with one tune can have its entry without a "(#1)" line (stored as tune 0).
            bool found = _tuneInfos.TryGetValue(index + 1, out SidTuneInfo? info) || (_tuneCount <= 1 && _tuneInfos.TryGetValue(0, out info));
            return found ? info!.Title ?? info.Name : null;
        }

        // "3. My Bonnie is Over the Ocean", or "4. Tune 4" when the tune has no name.
        private string TuneText(int index)
        {
            return $"{index + 1}. {TuneTitle(index) ?? $"Tune {index + 1}"}";
        }

        // Called when a SID file opens: reads the header and the STIL entry without blocking the window.
        private async Task LoadSidInfoAsync(string path)
        {
            var (header, infos) = await Task.Run(() => (SidTuneTitles.ReadHeader(path), SidTuneTitles.FindTunes(path)));
            if (IsDisposed || path != _filePath)
            {
                return;
            }

            _sidHeader = header;
            _tuneInfos = infos;
            RefreshSidInfo();
        }

        private static string? Labeled(string label, string? text)
        {
            return text == null ? null : $"{label}: {text}";
        }

        // A SID file says "unknown" with question marks ("<?>", "???"): such a text, and an empty one, is not shown.
        private static string? KnownSidText(string? text)
        {
            return string.IsNullOrWhiteSpace(text) || text.All(c => c is '?' or '<' or '>' || char.IsWhiteSpace(c)) ? null : text.Trim();
        }

        // Forget the texts of the previous file (called by ResetTunes).
        private void ResetSidInfo()
        {
            _tuneInfos = null;
            _sidHeader = null;
            RefreshSidInfo();
        }

        // Brings the list and the top left texts up to date with the tunes, their lengths and their names.
        private void RefreshSidInfo()
        {
            bool sid = !_isStream && SidPluginSetup.IsSidFile(_filePath);

            if (sid && _sidHeader != null)
            {
                string? line = _tuneCount > 1 ? TuneTitle(_tuneIndex) ?? $"Tune {_tuneIndex + 1}" : TuneTitle(0);
                // The header's texts in the order of the file (title, author, released), the tune's own name last. Empty texts and "unknown" ones
                // ("<?>", "???") are left out.
                ShowTrackInfo(Labeled("Title", KnownSidText(_sidHeader.Title)), Labeled("Artist", KnownSidText(_sidHeader.Author)),
                    Labeled("Copyright", KnownSidText(_sidHeader.Released)), Labeled("Tune", KnownSidText(line)));
            }
            else if (sid)
            {
                HideTrackInfo();
            }

            bool show = sid && _tuneCount > 1 && _settings.ShowTuneList;
            if (!show)
            {
                if (_pnlTuneList != null)
                {
                    _pnlTuneList.Visible = false;
                }
                return;
            }

            EnsureTuneList();
            ListBox list = _lstTunes!;
            if (list.Items.Count != _tuneCount)
            {
                list.BeginUpdate();
                list.Items.Clear();
                for (int i = 0; i < _tuneCount; i++)
                {
                    list.Items.Add(i);
                }
                list.EndUpdate();
            }

            // Keep the tune that plays in view.
            int visible = Math.Max(1, list.ClientSize.Height / list.ItemHeight);
            if (_tuneIndex < list.TopIndex || _tuneIndex >= list.TopIndex + visible)
            {
                list.TopIndex = Math.Max(0, _tuneIndex - visible / 2);
            }
            _pnlTuneList!.Visible = true;
            list.Invalidate();
        }

        private void EnsureTuneList()
        {
            if (_pnlTuneList != null)
            {
                return;
            }

            _lstTunes = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(48, 48, 48),
                ForeColor = Color.FromArgb(230, 230, 230),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = LogicalToDeviceUnits(24),
                SelectionMode = SelectionMode.None,   // the row that plays is drawn by DrawTuneRow, not selected
                IntegralHeight = false,
                TabStop = false,
            };
            _lstTunes.DrawItem += DrawTuneRow;
            _lstTunes.MouseClick += (s, e) =>
            {
                int index = _lstTunes.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    ChangeTune(index);
                }
            };

            // A double click means play: it plays that tune also when the player is stopped (the last tune has ended) or paused.
            _lstTunes.MouseDoubleClick += (s, e) =>
            {
                int index = _lstTunes.IndexFromPoint(e.Location);
                if (index >= 0 && e.Button == MouseButtons.Left && _player != null)
                {
                    ChangeTune(index);
                    if (!_player.IsPlaying)
                    {
                        TogglePlayPause();
                    }
                }
            };

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = LogicalToDeviceUnits(30),
                BackColor = Color.FromArgb(32, 32, 32),
                ForeColor = Color.FromArgb(230, 230, 230),
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(LogicalToDeviceUnits(8), 0, 0, 0),
                Text = "Tunes",
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _pnlTuneList = new Panel
            {
                Dock = ListDock(),
                Width = LogicalToDeviceUnits(340),
                BackColor = Color.FromArgb(48, 48, 48),
                Visible = false,
            };
            _pnlTuneList.Controls.Add(_lstTunes);   // the filling control first, then the one docked to the top
            _pnlTuneList.Controls.Add(header);
            _pnlTuneList.Controls.Add(NewContinuousBox());   // Continuous Play, at the bottom (MediaPlayerForm.Continuous.cs)

            // Docked after the control bar and the group boxes (so they keep the full width), before the video.
            Controls.Add(_pnlTuneList);
            Controls.SetChildIndex(_pnlTuneList, Controls.GetChildIndex(grpControlPanel));
        }

        private void DrawTuneRow(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            int index = e.Index;
            bool current = index == _tuneIndex;
            using (var back = new SolidBrush(current ? Color.FromArgb(24, 62, 110) : _lstTunes!.BackColor))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
            }

            Color text = current ? Color.FromArgb(120, 190, 255) : TuneTitle(index) != null ? Color.FromArgb(230, 230, 230) : Color.FromArgb(160, 160, 160);
            long? length = _tuneLengths != null && index < _tuneLengths.Length ? _tuneLengths[index] : null;
            string time = length is > 0 ? FormatTime(length.Value, false) : "";
            int pad = LogicalToDeviceUnits(8);
            Size timeSize = TextRenderer.MeasureText(e.Graphics, time, e.Font);
            var timeBounds = new Rectangle(e.Bounds.Right - timeSize.Width - pad, e.Bounds.Top, timeSize.Width, e.Bounds.Height);
            var textBounds = new Rectangle(e.Bounds.Left + pad, e.Bounds.Top, Math.Max(0, timeBounds.Left - e.Bounds.Left - pad * 2), e.Bounds.Height);
            const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(e.Graphics, TuneText(index), e.Font, textBounds, text, flags);
            TextRenderer.DrawText(e.Graphics, time, e.Font, timeBounds, current ? text : Color.FromArgb(160, 160, 160), TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        private void SetTuneListShown(bool shown)
        {
            _settings.ShowTuneList = shown;
            _settings.Save();
            RefreshSidInfo();
        }
    }
}
