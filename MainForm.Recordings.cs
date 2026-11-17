// MainForm.Recordings.cs: the My Recordings menu of the main window (between IPTV and Help).
//
// It is not there until the first recording has been made. It lists the 9 latest recordings, the newest on top (as the recent-file lists of Windows
// do), numbered 1 to 9 so that the number key opens one; then All Recordings (the window with the whole list, RecordingsForm) and Clear List (asks
// first, and says that only the list is cleared). A recording that is not at its path any more is looked for through the library; if it is not found
// the user is asked whether to take it out of the list. The menu is looked at again whenever the window is activated, so a recording made in a
// player window shows when the user comes back.
//
// History:
//   2026-11-12  First version.
//   2026-11-15  The Unfinished Recordings menu (recordings left behind by a crash).

namespace SwewolfMediaPlayer
{
    public partial class MainForm
    {
        private RecordingsForm? _recordingsForm;

        private void RefreshRecordingsMenu()
        {
            menuMyRecordings.Visible = RecordingStore.Any();
            RefreshUnfinishedMenu();
        }

        // Unfinished Recordings (to the right of My Recordings): only there when a recording was left behind (UnfinishedRecordings.cs). One item for each, with
        // Recover, Play and Discard in its submenu.
        private void RefreshUnfinishedMenu()
        {
            List<UnfinishedEntry> pending = UnfinishedStore.Pending();
            menuUnfinished.Visible = pending.Count > 0;
            menuUnfinished.DropDownItems.Clear();
            foreach (UnfinishedEntry entry in pending)
            {
                UnfinishedEntry captured = entry;
                string size = entry.SizeBytes >= 1_000_000_000 ? $"{entry.SizeBytes / 1e9:0.0} GB" : $"{entry.SizeBytes / 1e6:0} MB";
                string name = entry.Channel.Length > 0 ? entry.Channel : "Recording";
                var item = new ToolStripMenuItem($"{name.Replace("&", "&&")}, {entry.StartedAt:g} ({size})") { ToolTipText = entry.TsPath };
                item.DropDownItems.Add(new ToolStripMenuItem("Recover", null, async (_, _) =>
                {
                    await UnfinishedActions.RecoverAsync(this, AppName, captured);
                    RefreshRecordingsMenu();
                }) { ToolTipText = "Save it as an MP4 file" });
                item.DropDownItems.Add(new ToolStripMenuItem("Play", null, (_, _) => OpenUnfinished(captured)) { ToolTipText = "Play the recording as it is" });
                item.DropDownItems.Add(new ToolStripMenuItem("Discard", null, (_, _) =>
                {
                    UnfinishedActions.Discard(this, AppName, captured);
                    RefreshRecordingsMenu();
                }) { ToolTipText = "Move it to the Recycle Bin" });
                menuUnfinished.DropDownItems.Add(item);
            }
        }

        private void OpenUnfinished(UnfinishedEntry entry)
        {
            if (File.Exists(entry.TsPath))
            {
                OpenMedia(entry.TsPath);
            }
            else
            {
                RefreshRecordingsMenu();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            RefreshRecordingsMenu();
        }

        private void menuMyRecordings_DropDownOpening(object? sender, EventArgs e)
        {
            ToolStripItemCollection items = menuMyRecordings.DropDownItems;
            items.Clear();

            int number = 1;
            foreach (RecordingEntry entry in RecordingStore.All().Take(9))
            {
                RecordingEntry captured = entry;
                var item = new ToolStripMenuItem($"&{number}. {entry.Name.Replace("&", "&&")}") { ToolTipText = entry.Path };   // "1. Name": the number key opens it
                item.Click += (_, _) => OpenRecording(captured);
                items.Add(item);
                number++;
            }

            items.Add(new ToolStripSeparator());
            items.Add(new ToolStripMenuItem("All Recordings", null, (_, _) => ShowAllRecordings()));
            items.Add(new ToolStripMenuItem("Clear List", null, (_, _) => ClearRecordingList()));
        }

        private void OpenRecording(RecordingEntry entry)
        {
            string? path = RecordingStore.Locate(entry);
            if (path != null)
            {
                OpenMedia(path);
                return;
            }

            DialogResult remove = MessageBox.Show(this,
                $"The recording was not found:\n{entry.Path}\n\nIt may have been moved, renamed or deleted.\n\nDo you want to remove it from the list?",
                AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (remove == DialogResult.Yes)
            {
                RecordingStore.Remove(entry.Id);
                RefreshRecordingsMenu();
            }
        }

        private void ShowAllRecordings()
        {
            if (_recordingsForm == null || _recordingsForm.IsDisposed)
            {
                _recordingsForm = new RecordingsForm(OpenMedia);
                _recordingsForm.Show(this);
            }
            else
            {
                _recordingsForm.Activate();
            }
        }

        private void ClearRecordingList()
        {
            DialogResult answer = MessageBox.Show(this,
                "Do you want to clear the list of recordings?\n\nThis only clears the list. Your recording files are not deleted and stay where they are.",
                AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes)
            {
                RecordingStore.Clear();
                RefreshRecordingsMenu();
            }
        }
    }
}
