// SettingsForm.cs: Tools > Settings. Modeled on MP3toMP4's SettingsForm; the values live in AppSettings.
//
// History:
//   2026-10-15  First version: Start Player in Full Screen Mode, Check for New Version at Startup.
//   2026-10-16  The full screen option is for video files only. Added Show Thumbnail Previews on the Seek Bar.

namespace SwewolfMediaPlayer
{
    internal partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            Icon = Program.LoadIcon();

            // The Designer sets ClientSize before ControlBox = false / FixedDialog, and changing
            // those keeps the window size, so the client area ends up ~33 px too short and the
            // bottom-anchored OK/Cancel slide up over the last group. Size it from the layout instead.
            ClientSize = new Size(grpUpdate.Right + 12, grpUpdate.Bottom + 12 + btnOK.Height + 12);
        }

        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            AppSettings settings = AppSettings.Load();
            chkStartFullScreen.Checked = settings.StartFullScreenForVideo;
            chkSeekThumbnails.Checked = settings.ShowSeekBarThumbnails;
            cboNewVersionCheck.SelectedItem = settings.CheckForUpdatesOnStartup ? "Yes" : "No";
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            // Load again, so nothing else in the file (e.g. When the Video Ends) is overwritten.
            AppSettings settings = AppSettings.Load();
            settings.StartFullScreenForVideo = chkStartFullScreen.Checked;
            settings.ShowSeekBarThumbnails = chkSeekThumbnails.Checked;
            settings.CheckForUpdatesOnStartup = cboNewVersionCheck.SelectedItem?.ToString() != "No";
            settings.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
