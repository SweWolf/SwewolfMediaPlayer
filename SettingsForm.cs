// SettingsForm.cs: Tools > Settings. Modeled on MP3toMP4's SettingsForm; the values live in AppSettings.
//
// History:
//   2026-10-15  First version: Start Player in Full Screen Mode, Check for New Version at Startup.
//   2026-10-16  The full screen option is for video files only. Added Show Thumbnail Previews on the Seek Bar.
//   2026-10-16  Resume group: Save Resume Point for (video and audio / video and long audio) and the length in minutes.
//   2026-10-16  Resume (Yes / Ask / No) is its own drop-down; the length box is hidden, not disabled, when it does not apply.
//   2026-10-16  Move the Resume Point Back by N seconds (default 10).
//   2026-10-16  Resume = No hides the other settings of the group.
//   2026-11-13  "Save Resume Point for" is the checkbox Resume Audio Files (off by default) with the minimum length in minutes.
//   2026-10-23  Cover Picture Size for Music Files (Medium / Maximum) in the Player group.
//   2026-10-27  Show Track Menus Only When There Is a Choice in the Player group.
//   2026-11-08  Look of the Previous and Next Buttons (Modern / Classic) in the Player group.
//   2026-11-10  Saved Frames group: Default Folder for Saved Frames (text box, Browse, grey hint; blank = the folder of the video).
//   2026-11-10  Open a Media File group: Default Start Folder for Browsing (text box, Browse, grey hint; blank = the last folder used).
//   2026-11-12  Side of the Tune and Channel Lists (Right / Left) in the Player group.
//   2026-10-26  "Turn On Subtitles When a Video Opens" (the default for videos with no choice of their own) in the Player group.

namespace SwewolfMediaPlayer
{
    internal partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            Icon = Program.LoadIcon();
            TextBoxStyle.Apply(txtFramesFolder);
            TextBoxStyle.Apply(txtOpenFolder);

            ApplyLayout();
        }

        // Browse for the folder; it starts where the box points now, when that folder exists.
        private void btnBrowseFrames_Click(object? sender, EventArgs e)
        {
            BrowseFolder(txtFramesFolder, "Choose the default folder for saved frames.");
        }

        private void btnBrowseOpen_Click(object? sender, EventArgs e)
        {
            BrowseFolder(txtOpenFolder, "Choose the folder where browsing for a media file starts.");
        }

        private void BrowseFolder(TextBox box, string description)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                InitialDirectory = Directory.Exists(box.Text.Trim()) ? box.Text.Trim() : ""
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                box.Text = dialog.SelectedPath;
            }
        }

        // The Designer sets ClientSize before ControlBox = false / FixedDialog, and changing
        // those keeps the window size, so the client area ends up ~33 px too short and the
        // bottom-anchored OK/Cancel slide up over the last group. Size it from the layout instead.
        // Also used when the Resume group grows or shrinks (the length box is only there for "Video Files and Long Audio Files").
        private void ApplyLayout()
        {
            // Video and audio files have their own settings. The rewind row is only there when something is saved at all;
            // the audio rows only when Resume Audio Files is checked. Everything below moves up as rows disappear.
            bool videoOn = cboResumeMode.SelectedIndex != 2;
            bool audioOn = chkResumeAudio.Checked;
            bool rewindOn = videoOn || audioOn;
            lblRewind.Visible = nudRewind.Visible = lblSeconds.Visible = rewindOn;
            lblAudioMode.Visible = cboAudioMode.Visible = audioOn;
            lblAudioMinutes.Visible = nudAudioMinutes.Visible = lblMinutes.Visible = audioOn;

            int y = 92;
            if (rewindOn)
            {
                lblRewind.Top = lblSeconds.Top = y;
                nudRewind.Top = y - 4;
                y += 38;
            }
            chkResumeAudio.Top = y;
            y += 30;
            if (audioOn)
            {
                lblAudioMode.Top = y + 4;
                cboAudioMode.Top = y;
                y += 30;
                lblAudioMinutes.Top = lblMinutes.Top = y + 4;
                nudAudioMinutes.Top = y;
                y += 30;
            }

            grpOpen.Top = grpPlayer.Bottom + 12;
            grpResume.Top = grpOpen.Bottom + 12;
            grpResume.Height = y + 6;
            grpFrames.Top = grpResume.Bottom + 12;
            grpUpdate.Top = grpFrames.Bottom + 12;

            ClientSize = new Size(grpUpdate.Right + 12, grpUpdate.Bottom + 12 + btnOK.Height + 12);
            btnOK.Top = btnCancel.Top = ClientSize.Height - 12 - btnOK.Height;
        }

        private void SettingsForm_Load(object? sender, EventArgs e)
        {
            AppSettings settings = AppSettings.Load();
            chkStartFullScreen.Checked = settings.StartFullScreenForVideo;
            chkSeekThumbnails.Checked = settings.ShowSeekBarThumbnails;
            chkChapterMarks.Checked = settings.ShowChapterMarks;
            chkShowSubtitles.Checked = settings.ShowSubtitles;
            chkTrackMenus.Checked = settings.TrackMenusOnlyWithChoice;
            cboNewVersionCheck.SelectedItem = settings.CheckForUpdatesOnStartup ? "Yes" : "No";
            txtFramesFolder.Text = settings.SaveFramesFolder ?? "";
            txtOpenFolder.Text = settings.MediaStartFolder ?? "";
            cboCoverSize.SelectedIndex = Math.Clamp(settings.CoverPictureSize, 0, 1);
            cboButtonLook.SelectedIndex = settings.ClassicTrackButtons ? 1 : 0;
            cboListSide.SelectedIndex = Math.Clamp(settings.ListSide, 0, 1);
            nudNoticeSeconds.Value = Math.Clamp(settings.PlaylistNoticeSeconds, (int)nudNoticeSeconds.Minimum, (int)nudNoticeSeconds.Maximum);
            cboResumeMode.SelectedIndex = Math.Clamp(settings.ResumeWhenOpening, 0, 2);
            chkResumeAudio.Checked = settings.ResumeAudioFiles;
            cboAudioMode.SelectedIndex = Math.Clamp(settings.ResumeAudioMode, 0, 1);
            nudRewind.Value = Math.Clamp(settings.ResumeRewindSeconds, (int)nudRewind.Minimum, (int)nudRewind.Maximum);
            nudAudioMinutes.Value = Math.Clamp(settings.ResumeAudioMinMinutes, (int)nudAudioMinutes.Minimum, (int)nudAudioMinutes.Maximum);
        }

        // The number of minutes only matters for "Resume Audio Files": otherwise it is not shown.
        private void chkResumeAudio_CheckedChanged(object? sender, EventArgs e)
        {
            ApplyLayout();
        }

        private void cboResumeMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyLayout();
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            // Load again, so nothing else in the file (e.g. When the Video Ends) is overwritten.
            AppSettings settings = AppSettings.Load();
            settings.StartFullScreenForVideo = chkStartFullScreen.Checked;
            settings.ShowSeekBarThumbnails = chkSeekThumbnails.Checked;
            settings.ShowChapterMarks = chkChapterMarks.Checked;
            settings.ShowSubtitles = chkShowSubtitles.Checked;
            settings.TrackMenusOnlyWithChoice = chkTrackMenus.Checked;
            settings.CheckForUpdatesOnStartup = cboNewVersionCheck.SelectedItem?.ToString() != "No";
            settings.SaveFramesFolder = txtFramesFolder.Text.Trim().Trim('"');
            settings.MediaStartFolder = txtOpenFolder.Text.Trim().Trim('"');
            settings.CoverPictureSize = Math.Max(0, cboCoverSize.SelectedIndex);
            settings.ClassicTrackButtons = cboButtonLook.SelectedIndex == 1;
            settings.ListSide = Math.Max(0, cboListSide.SelectedIndex);
            settings.PlaylistNoticeSeconds = (int)nudNoticeSeconds.Value;
            settings.ResumeWhenOpening = Math.Max(0, cboResumeMode.SelectedIndex);
            settings.ResumeAudioFiles = chkResumeAudio.Checked;
            settings.ResumeAudioMode = Math.Max(0, cboAudioMode.SelectedIndex);
            settings.ResumeRewindSeconds = (int)nudRewind.Value;
            settings.ResumeAudioMinMinutes = (int)nudAudioMinutes.Value;
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
