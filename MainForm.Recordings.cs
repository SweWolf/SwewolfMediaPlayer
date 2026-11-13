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

namespace SwewolfMediaPlayer
{
    public partial class MainForm
    {
        private RecordingsForm? _recordingsForm;

        private void RefreshRecordingsMenu()
        {
            menuMyRecordings.Visible = RecordingStore.Any();
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
                var item = new ToolStripMenuItem($"&{number} {entry.Name.Replace("&", "&&")}") { ToolTipText = entry.Path };
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
