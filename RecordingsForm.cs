// RecordingsForm.cs: the All Recordings window (My Recordings > All Recordings): a grid of every recording the player has made, newest first.
//
// Play (or a double-click) plays the file in a player window, Open Folder shows it in Windows Explorer, Find File points a recording that cannot be
// found at the file it is now, Copy Source Address copies the stream address, Remove from List takes it out of the list (the file stays). A
// recording that is not at its path is looked for through the library in the background (RecordingStore.Locate), so one that was renamed or
// moved there shows up with its new place. The window is not modal and reads the list again when it is activated.
//
// History:
//   2026-11-12  First version.

using System.ComponentModel;
using System.Diagnostics;

namespace SwewolfMediaPlayer
{
    internal sealed class RecordingsForm : Form
    {
        private const int ColName = 0, ColChannel = 1, ColRecorded = 2, ColLength = 3, ColSize = 4, ColStatus = 5, ColFolder = 6, ColSource = 7;

        private readonly Action<string> _play;
        private readonly DataGridView _grid = new();
        private readonly Button _btnPlay = new(), _btnFolder = new(), _btnFind = new(), _btnCopy = new(), _btnRemove = new(), _btnClose = new();
        private readonly ToolTip _tips = new();
        private bool _loaded;

        public RecordingsForm(Action<string> play)
        {
            _play = play;
            Text = "All Recordings";
            Icon = Program.LoadIcon();
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1040, 480);

            Color back = Color.FromArgb(32, 32, 32), alt = Color.FromArgb(38, 38, 38), text = Color.FromArgb(230, 230, 230);
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.MultiSelect = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.BorderStyle = BorderStyle.None;
            _grid.BackgroundColor = back;
            _grid.GridColor = Color.FromArgb(60, 60, 60);
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(48, 48, 48), ForeColor = text, SelectionBackColor = Color.FromArgb(48, 48, 48) };
            _grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = back, ForeColor = text, SelectionBackColor = Color.FromArgb(24, 62, 110), SelectionForeColor = Color.White };
            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = alt, ForeColor = text, SelectionBackColor = Color.FromArgb(24, 62, 110), SelectionForeColor = Color.White };
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            AddColumn("Name", 200);
            AddColumn("Channel", 150);
            AddColumn("Recorded", 120);
            AddColumn("Length", 70);
            AddColumn("Size", 80);
            AddColumn("Status", 80);
            AddColumn("Folder", 200);
            AddColumn("Source Address", 200);
            _grid.Columns[ColFolder].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns[ColLength].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns[ColSize].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.SortCompare += SortCompare;
            _grid.SelectionChanged += (_, _) => UpdateButtons();
            _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) PlaySelected(); };
            _grid.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    RemoveSelected();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    PlaySelected();
                    e.Handled = true;
                }
            };

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(43, 43, 43), Padding = new Padding(10, 10, 10, 10) };
            var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false };
            SetupButton(_btnPlay, "Play", "Play the recording in a player window (or double-click it).", PlaySelected);
            SetupButton(_btnFolder, "Open Folder", "Show the file in Windows Explorer.", OpenFolder);
            SetupButton(_btnFind, "Find File...", "The file is not where it was recorded: choose where it is now.", FindFile);
            SetupButton(_btnCopy, "Copy Source Address", "Copy the address of the stream that was recorded.", CopySource);
            SetupButton(_btnRemove, "Remove from List", "Take the recording out of this list. The file itself is not deleted.", RemoveSelected);
            foreach (Button b in new[] { _btnPlay, _btnFolder, _btnFind, _btnCopy, _btnRemove })
            {
                left.Controls.Add(b);
            }
            SetupButton(_btnClose, "Close", "", Close);
            _btnClose.Dock = DockStyle.Right;
            bottom.Controls.Add(left);
            bottom.Controls.Add(_btnClose);

            Controls.Add(_grid);
            Controls.Add(bottom);
            CancelButton = _btnClose;
        }

        private void AddColumn(string title, int width)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = title, Width = width, SortMode = DataGridViewColumnSortMode.Automatic });
        }

        private void SetupButton(Button button, string text, string tip, Action click)
        {
            button.Text = text;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Padding = new Padding(8, 2, 8, 2);
            button.UseVisualStyleBackColor = true;
            button.Margin = new Padding(0, 0, 8, 0);
            button.Click += (_, _) => click();
            if (tip.Length > 0)
            {
                _tips.SetToolTip(button, tip);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MinimumSize = Size;   // the designer-style rule of the apps: the size it is made with is the smallest
            Reload();
            _loaded = true;
        }

        // A recording made (or removed) while the window was in the background shows when it comes to the front.
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (_loaded)
            {
                Reload();
            }
        }

        private RecordingEntry? Selected => _grid.CurrentRow?.Tag as RecordingEntry;

        private void Reload()
        {
            Guid? keep = Selected?.Id;
            DataGridViewColumn? sortColumn = _grid.SortedColumn;
            ListSortDirection? direction = _grid.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending;

            List<RecordingEntry> entries = RecordingStore.All();
            _grid.Rows.Clear();
            var missing = new List<DataGridViewRow>();
            foreach (RecordingEntry entry in entries)
            {
                bool there = File.Exists(entry.Path);
                int index = _grid.Rows.Add(entry.Name, entry.Channel, entry.RecordedAt.ToString("yyyy-MM-dd HH:mm"), FormatLength(entry.DurationMs),
                    FormatSize(entry.SizeBytes), there ? "OK" : "Looking...", Path.GetDirectoryName(entry.Path) ?? "", entry.Url ?? "");
                DataGridViewRow row = _grid.Rows[index];
                row.Tag = entry;
                if (!there)
                {
                    missing.Add(row);
                }
            }

            if (sortColumn != null)
            {
                _grid.Sort(sortColumn, direction ?? ListSortDirection.Ascending);
            }
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (keep != null && (row.Tag as RecordingEntry)?.Id == keep)
                {
                    _grid.CurrentCell = row.Cells[ColName];
                    break;
                }
            }
            UpdateButtons();

            if (missing.Count > 0)
            {
                _ = LookForMissingAsync(missing.Select(r => (RecordingEntry)r.Tag!).ToList());
            }
        }

        // Asks the library where recordings went that are not at their path (renamed or moved by the explorer), without holding up the window.
        private async Task LookForMissingAsync(List<RecordingEntry> entries)
        {
            var found = await Task.Run(() => entries.Select(e => (Entry: e, Path: RecordingStore.Locate(e))).ToList());
            if (IsDisposed)
            {
                return;
            }

            foreach (var (entry, path) in found)
            {
                DataGridViewRow? row = _grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => (r.Tag as RecordingEntry)?.Id == entry.Id);
                if (row == null)
                {
                    continue;
                }
                row.Cells[ColStatus].Value = path != null ? "Moved" : "Not found";
                if (path != null)
                {
                    row.Cells[ColName].Value = entry.Name;
                    row.Cells[ColFolder].Value = Path.GetDirectoryName(path);
                    row.Cells[ColSize].Value = FormatSize(entry.SizeBytes);
                }
                else
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                }
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            RecordingEntry? entry = Selected;
            bool any = entry != null;
            string? status = _grid.CurrentRow?.Cells[ColStatus].Value as string;
            bool missing = status == "Not found";
            _btnPlay.Enabled = any && !missing;
            _btnFolder.Enabled = any;
            _btnFind.Enabled = any && missing;
            _btnCopy.Enabled = any && !string.IsNullOrEmpty(entry?.Url);
            _btnRemove.Enabled = any;
        }

        private void PlaySelected()
        {
            RecordingEntry? entry = Selected;
            if (entry == null)
            {
                return;
            }
            string? path = RecordingStore.Locate(entry);
            if (path == null)
            {
                MessageBox.Show(this, $"The recording was not found:\n{entry.Path}\n\nIt may have been moved, renamed or deleted. Use Find File to point at it.", MainForm.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _play(path);
        }

        private void OpenFolder()
        {
            RecordingEntry? entry = Selected;
            if (entry == null)
            {
                return;
            }
            string? path = RecordingStore.Locate(entry);
            string? folder = Path.GetDirectoryName(path ?? entry.Path);
            if (path != null)
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            else if (folder != null && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show(this, "The folder of the recording was not found either.", MainForm.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void FindFile()
        {
            RecordingEntry? entry = Selected;
            if (entry == null)
            {
                return;
            }
            using var dialog = new OpenFileDialog
            {
                Title = "Find the Recording",
                Filter = "Video Files|*.mp4;*.mkv;*.ts;*.mov;*.avi;*.webm|All Files|*.*",
                FileName = Path.GetFileName(entry.Path),
            };
            string? folder = Path.GetDirectoryName(entry.Path);
            if (folder != null && Directory.Exists(folder))
            {
                dialog.InitialDirectory = folder;
            }
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                RecordingStore.SetPath(entry.Id, dialog.FileName, new FileInfo(dialog.FileName).Length);
                Reload();
            }
        }

        private void CopySource()
        {
            string? url = Selected?.Url;
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    Clipboard.SetText(url);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // Another program has the clipboard just now: nothing was copied.
                }
            }
        }

        private void RemoveSelected()
        {
            RecordingEntry? entry = Selected;
            if (entry == null)
            {
                return;
            }
            RecordingStore.Remove(entry.Id);
            Reload();
        }

        // Length and size are text in the grid: sort them by the numbers behind them, and the date by the date.
        private void SortCompare(object? sender, DataGridViewSortCompareEventArgs e)
        {
            var a = _grid.Rows[e.RowIndex1].Tag as RecordingEntry;
            var b = _grid.Rows[e.RowIndex2].Tag as RecordingEntry;
            if (a == null || b == null)
            {
                return;
            }
            switch (e.Column.Index)
            {
                case ColRecorded: e.SortResult = a.RecordedAt.CompareTo(b.RecordedAt); break;
                case ColLength: e.SortResult = a.DurationMs.CompareTo(b.DurationMs); break;
                case ColSize: e.SortResult = a.SizeBytes.CompareTo(b.SizeBytes); break;
                default: e.SortResult = string.Compare(e.CellValue1?.ToString(), e.CellValue2?.ToString(), StringComparison.CurrentCultureIgnoreCase); break;
            }
            e.Handled = true;
        }

        private static string FormatLength(long ms)
        {
            if (ms <= 0)
            {
                return "";
            }
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "";
            }
            return bytes >= 1024L * 1024 * 1024 ? $"{bytes / (1024.0 * 1024 * 1024):0.0} GB" : $"{bytes / (1024.0 * 1024):0.0} MB";
        }
    }
}
