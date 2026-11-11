namespace SwewolfMediaPlayer
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new ToolTip(components);
            grpPlayer = new GroupBox();
            chkStartFullScreen = new CheckBox();
            chkSeekThumbnails = new CheckBox();
            chkShowSubtitles = new CheckBox();
            chkTrackMenus = new CheckBox();
            lblCoverSize = new Label();
            cboCoverSize = new ComboBox();
            lblButtonLook = new Label();
            cboButtonLook = new ComboBox();
            grpOpen = new GroupBox();
            lblOpenFolder = new Label();
            txtOpenFolder = new TextBox();
            btnBrowseOpen = new Button();
            lblOpenHint = new Label();
            grpResume = new GroupBox();
            lblResumeMode = new Label();
            cboResumeMode = new ComboBox();
            lblRewind = new Label();
            nudRewind = new NumericUpDown();
            lblSeconds = new Label();
            lblSaveResume = new Label();
            cboSaveResume = new ComboBox();
            lblAudioMinutes = new Label();
            nudAudioMinutes = new NumericUpDown();
            lblMinutes = new Label();
            grpFrames = new GroupBox();
            lblFramesFolder = new Label();
            txtFramesFolder = new TextBox();
            btnBrowseFrames = new Button();
            lblFramesHint = new Label();
            grpUpdate = new GroupBox();
            cboNewVersionCheck = new ComboBox();
            lblNewVersionCheck = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            grpPlayer.SuspendLayout();
            grpOpen.SuspendLayout();
            grpResume.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudRewind).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudAudioMinutes).BeginInit();
            grpFrames.SuspendLayout();
            grpUpdate.SuspendLayout();
            SuspendLayout();
            //
            // toolTip
            //
            toolTip.AutoPopDelay = 15000;
            toolTip.InitialDelay = 400;
            toolTip.ReshowDelay = 100;
            //
            // grpPlayer
            //
            grpPlayer.Controls.Add(chkStartFullScreen);
            grpPlayer.Controls.Add(chkSeekThumbnails);
            grpPlayer.Controls.Add(chkShowSubtitles);
            grpPlayer.Controls.Add(chkTrackMenus);
            grpPlayer.Controls.Add(lblCoverSize);
            grpPlayer.Controls.Add(cboCoverSize);
            grpPlayer.Controls.Add(lblButtonLook);
            grpPlayer.Controls.Add(cboButtonLook);
            grpPlayer.Location = new Point(12, 12);
            grpPlayer.Name = "grpPlayer";
            grpPlayer.Size = new Size(360, 238);
            grpPlayer.TabIndex = 0;
            grpPlayer.TabStop = false;
            grpPlayer.Text = "Player";
            //
            // chkStartFullScreen
            //
            chkStartFullScreen.AutoSize = true;
            chkStartFullScreen.Location = new Point(10, 24);
            chkStartFullScreen.Name = "chkStartFullScreen";
            chkStartFullScreen.Size = new Size(280, 19);
            chkStartFullScreen.TabIndex = 0;
            chkStartFullScreen.Text = "Start Player in &Full Screen Mode for Video Files";
            toolTip.SetToolTip(chkStartFullScreen, "Open every video file in full screen mode, whether it is opened\r\nfrom the main window or from the command line.\r\nAudio files never open in full screen mode.\r\nPress Esc to leave full screen mode.");
            chkStartFullScreen.UseVisualStyleBackColor = true;
            //
            // chkSeekThumbnails
            //
            chkSeekThumbnails.AutoSize = true;
            chkSeekThumbnails.Location = new Point(10, 50);
            chkSeekThumbnails.Name = "chkSeekThumbnails";
            chkSeekThumbnails.Size = new Size(240, 19);
            chkSeekThumbnails.TabIndex = 1;
            chkSeekThumbnails.Text = "Show &Thumbnail Previews on the Seek Bar";
            toolTip.SetToolTip(chkSeekThumbnails, "Show a small picture of the video at the pointer\r\nwhile you point at the seek bar. Video files only.\r\nIt needs FFmpeg (ffmpeg.exe in the program folder or in the PATH);\r\nwithout it nothing is shown.");
            chkSeekThumbnails.UseVisualStyleBackColor = true;
            //
            // chkShowSubtitles
            //
            chkShowSubtitles.AutoSize = true;
            chkShowSubtitles.Location = new Point(10, 76);
            chkShowSubtitles.Name = "chkShowSubtitles";
            chkShowSubtitles.Size = new Size(240, 19);
            chkShowSubtitles.TabIndex = 2;
            chkShowSubtitles.Text = "Turn On Subtitles When a &Video Opens";
            toolTip.SetToolTip(chkShowSubtitles, "Show the subtitles of a video the first time you open it.\r\nA choice you make in the right-click menu (or with the C key)\r\nis remembered for that video and used instead.");
            chkShowSubtitles.UseVisualStyleBackColor = true;
            //
            // chkTrackMenus
            //
            chkTrackMenus.AutoSize = true;
            chkTrackMenus.Location = new Point(10, 102);
            chkTrackMenus.Name = "chkTrackMenus";
            chkTrackMenus.Size = new Size(300, 19);
            chkTrackMenus.TabIndex = 3;
            chkTrackMenus.Text = "Show Track Menus Only When There Is a &Choice";
            toolTip.SetToolTip(chkTrackMenus, "In the right-click menu, show Audio Track and Video Track\r\nonly for a file that has more than one of them, and\r\nSubtitle Track only for a file that has at least one\r\n(subtitles can also be turned off there).\r\nWhen this is off, the three menus are always shown,\r\nand greyed out when the file has none.");
            chkTrackMenus.UseVisualStyleBackColor = true;
            //
            // lblCoverSize
            //
            lblCoverSize.AutoSize = true;
            lblCoverSize.Location = new Point(10, 132);
            lblCoverSize.Name = "lblCoverSize";
            lblCoverSize.TabIndex = 4;
            lblCoverSize.Text = "Cover Picture Si&ze for Music Files";
            //
            // cboCoverSize
            //
            cboCoverSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCoverSize.FormattingEnabled = true;
            cboCoverSize.Items.AddRange(new object[] { "Medium", "Maximum" });
            cboCoverSize.Location = new Point(10, 150);
            cboCoverSize.Name = "cboCoverSize";
            cboCoverSize.Size = new Size(218, 23);
            cboCoverSize.TabIndex = 5;
            toolTip.SetToolTip(cboCoverSize, "How large the cover picture of a music file is shown.\r\nMedium is 75 percent of the largest size that fits the window.\r\nMaximum fills the video area.");
            //
            // lblButtonLook
            //
            lblButtonLook.AutoSize = true;
            lblButtonLook.Location = new Point(10, 186);
            lblButtonLook.Name = "lblButtonLook";
            lblButtonLook.TabIndex = 6;
            lblButtonLook.Text = "&Look of the Previous and Next Buttons";
            //
            // cboButtonLook
            //
            cboButtonLook.DropDownStyle = ComboBoxStyle.DropDownList;
            cboButtonLook.FormattingEnabled = true;
            cboButtonLook.Items.AddRange(new object[] { "Modern", "Classic" });
            cboButtonLook.Location = new Point(10, 204);
            cboButtonLook.Name = "cboButtonLook";
            cboButtonLook.Size = new Size(218, 23);
            cboButtonLook.TabIndex = 7;
            toolTip.SetToolTip(cboButtonLook, "How the buttons for the previous and next tune or track look.\r\nModern has one triangle and a bar.\r\nClassic has two triangles and a bar.\r\nA player that is already open keeps its look until it is opened again.");
            //
            // grpOpen
            //
            grpOpen.Controls.Add(lblOpenFolder);
            grpOpen.Controls.Add(txtOpenFolder);
            grpOpen.Controls.Add(btnBrowseOpen);
            grpOpen.Controls.Add(lblOpenHint);
            grpOpen.Location = new Point(12, 256);
            grpOpen.Name = "grpOpen";
            grpOpen.Size = new Size(360, 116);
            grpOpen.TabIndex = 1;
            grpOpen.TabStop = false;
            grpOpen.Text = "Open a Media File";
            //
            // lblOpenFolder
            //
            lblOpenFolder.AutoSize = true;
            lblOpenFolder.Location = new Point(10, 28);
            lblOpenFolder.Name = "lblOpenFolder";
            lblOpenFolder.TabIndex = 0;
            lblOpenFolder.Text = "Default Start Fol&der for Browsing";
            //
            // txtOpenFolder
            //
            txtOpenFolder.Location = new Point(10, 46);
            txtOpenFolder.Name = "txtOpenFolder";
            txtOpenFolder.Size = new Size(260, 23);
            txtOpenFolder.TabIndex = 1;
            toolTip.SetToolTip(txtOpenFolder, "The folder that opens when you click Browse in the main window.\r\nThe Videos folder of Windows is the default.\r\nA folder that does not exist is skipped.\r\nWhile the program runs, the last folder you browsed in is used instead.\r\nLeave it blank to start in the last folder you used,\r\neven after the program has been closed.");
            //
            // btnBrowseOpen
            //
            btnBrowseOpen.Location = new Point(276, 45);
            btnBrowseOpen.Name = "btnBrowseOpen";
            btnBrowseOpen.Size = new Size(74, 25);
            btnBrowseOpen.TabIndex = 2;
            btnBrowseOpen.Text = "Bro&wse...";
            btnBrowseOpen.UseVisualStyleBackColor = true;
            btnBrowseOpen.Click += btnBrowseOpen_Click;
            //
            // lblOpenHint
            //
            lblOpenHint.ForeColor = SystemColors.GrayText;
            lblOpenHint.Location = new Point(10, 76);
            lblOpenHint.Name = "lblOpenHint";
            lblOpenHint.Size = new Size(340, 32);
            lblOpenHint.TabIndex = 3;
            lblOpenHint.Text = "Leave this blank to start in the last folder you used.";
            //
            // grpResume
            //
            grpResume.Controls.Add(lblResumeMode);
            grpResume.Controls.Add(cboResumeMode);
            grpResume.Controls.Add(lblRewind);
            grpResume.Controls.Add(nudRewind);
            grpResume.Controls.Add(lblSeconds);
            grpResume.Controls.Add(lblSaveResume);
            grpResume.Controls.Add(cboSaveResume);
            grpResume.Controls.Add(lblAudioMinutes);
            grpResume.Controls.Add(nudAudioMinutes);
            grpResume.Controls.Add(lblMinutes);
            grpResume.Location = new Point(12, 108);
            grpResume.Name = "grpResume";
            grpResume.Size = new Size(360, 224);
            grpResume.TabIndex = 2;
            grpResume.TabStop = false;
            grpResume.Text = "Resume";
            //
            // lblResumeMode
            //
            lblResumeMode.AutoSize = true;
            lblResumeMode.Location = new Point(10, 28);
            lblResumeMode.Name = "lblResumeMode";
            lblResumeMode.TabIndex = 0;
            lblResumeMode.Text = "&Resume";
            //
            // cboResumeMode
            //
            cboResumeMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cboResumeMode.FormattingEnabled = true;
            cboResumeMode.Items.AddRange(new object[] { "Yes", "Ask", "No" });
            cboResumeMode.Location = new Point(10, 46);
            cboResumeMode.Name = "cboResumeMode";
            cboResumeMode.Size = new Size(120, 23);
            cboResumeMode.TabIndex = 1;
            cboResumeMode.SelectedIndexChanged += cboResumeMode_SelectedIndexChanged;
            toolTip.SetToolTip(cboResumeMode, "What happens when you open a file you stopped in before.\r\nYes: continue from where you stopped, without asking.\r\nAsk: ask first.\r\nNo: always start from the beginning,\r\nand the player does not remember where you stopped.");
            //
            // lblRewind
            //
            lblRewind.AutoSize = true;
            lblRewind.Location = new Point(10, 92);
            lblRewind.Name = "lblRewind";
            lblRewind.TabIndex = 2;
            lblRewind.Text = "Move the Resume Point &Back by";
            //
            // nudRewind
            //
            nudRewind.Location = new Point(188, 88);
            nudRewind.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            nudRewind.Name = "nudRewind";
            nudRewind.Size = new Size(60, 23);
            nudRewind.TabIndex = 3;
            nudRewind.Value = new decimal(new int[] { 10, 0, 0, 0 });
            toolTip.SetToolTip(nudRewind, "The saved position is this many seconds before the point\r\nwhere you stopped, so you see a little of what came before.\r\n0 saves the exact point.\r\nExample: with 10 seconds, stopping at 2:00 saves 1:50.");
            //
            // lblSeconds
            //
            lblSeconds.AutoSize = true;
            lblSeconds.Location = new Point(254, 92);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.TabIndex = 4;
            lblSeconds.Text = "seconds";
            //
            // lblSaveResume
            //
            lblSaveResume.AutoSize = true;
            lblSaveResume.Location = new Point(10, 132);
            lblSaveResume.Name = "lblSaveResume";
            lblSaveResume.TabIndex = 5;
            lblSaveResume.Text = "&Save Resume Point for";
            //
            // cboSaveResume
            //
            cboSaveResume.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSaveResume.FormattingEnabled = true;
            cboSaveResume.Items.AddRange(new object[] { "Video and Audio Files", "Video Files and Long Audio Files" });
            cboSaveResume.Location = new Point(10, 150);
            cboSaveResume.Name = "cboSaveResume";
            cboSaveResume.Size = new Size(340, 23);
            cboSaveResume.TabIndex = 6;
            cboSaveResume.SelectedIndexChanged += cboSaveResume_SelectedIndexChanged;
            toolTip.SetToolTip(cboSaveResume, "The player remembers where you stopped.\r\nIt starts after the file has played for 60 seconds.\r\nThe position is kept in the library database\r\nthat SweWolf File Explorer uses too.");
            //
            // lblAudioMinutes
            //
            lblAudioMinutes.AutoSize = true;
            lblAudioMinutes.Location = new Point(10, 192);
            lblAudioMinutes.Name = "lblAudioMinutes";
            lblAudioMinutes.TabIndex = 7;
            lblAudioMinutes.Text = "Long Audio Files Are &at Least";
            //
            // nudAudioMinutes
            //
            nudAudioMinutes.Location = new Point(188, 188);
            nudAudioMinutes.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            nudAudioMinutes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudAudioMinutes.Name = "nudAudioMinutes";
            nudAudioMinutes.Size = new Size(60, 23);
            nudAudioMinutes.TabIndex = 8;
            nudAudioMinutes.Value = new decimal(new int[] { 10, 0, 0, 0 });
            toolTip.SetToolTip(nudAudioMinutes, "Audio files of this length or longer get a resume point\r\n(for example podcasts and audio books).\r\nShorter ones do not.");
            //
            // lblMinutes
            //
            lblMinutes.AutoSize = true;
            lblMinutes.Location = new Point(254, 192);
            lblMinutes.Name = "lblMinutes";
            lblMinutes.TabIndex = 9;
            lblMinutes.Text = "minutes";
            //
            // grpFrames
            //
            grpFrames.Controls.Add(lblFramesFolder);
            grpFrames.Controls.Add(txtFramesFolder);
            grpFrames.Controls.Add(btnBrowseFrames);
            grpFrames.Controls.Add(lblFramesHint);
            grpFrames.Location = new Point(12, 334);
            grpFrames.Name = "grpFrames";
            grpFrames.Size = new Size(360, 116);
            grpFrames.TabIndex = 3;
            grpFrames.TabStop = false;
            grpFrames.Text = "Saved Frames";
            //
            // lblFramesFolder
            //
            lblFramesFolder.AutoSize = true;
            lblFramesFolder.Location = new Point(10, 28);
            lblFramesFolder.Name = "lblFramesFolder";
            lblFramesFolder.TabIndex = 0;
            lblFramesFolder.Text = "Default &Folder for Saved Frames";
            //
            // txtFramesFolder
            //
            txtFramesFolder.Location = new Point(10, 46);
            txtFramesFolder.Name = "txtFramesFolder";
            txtFramesFolder.Size = new Size(260, 23);
            txtFramesFolder.TabIndex = 1;
            toolTip.SetToolTip(txtFramesFolder, "The folder where the Save Frame dialog starts.\r\nThe Pictures folder of Windows is the default.\r\nLeave it blank to start in the folder of the video file.\r\nA folder you choose in the dialog is used for that player window until it is closed.");
            //
            // btnBrowseFrames
            //
            btnBrowseFrames.Location = new Point(276, 45);
            btnBrowseFrames.Name = "btnBrowseFrames";
            btnBrowseFrames.Size = new Size(74, 25);
            btnBrowseFrames.TabIndex = 2;
            btnBrowseFrames.Text = "&Browse...";
            btnBrowseFrames.UseVisualStyleBackColor = true;
            btnBrowseFrames.Click += btnBrowseFrames_Click;
            //
            // lblFramesHint
            //
            lblFramesHint.ForeColor = SystemColors.GrayText;
            lblFramesHint.Location = new Point(10, 76);
            lblFramesHint.Name = "lblFramesHint";
            lblFramesHint.Size = new Size(340, 32);
            lblFramesHint.TabIndex = 3;
            lblFramesHint.Text = "Leave this blank to start in the same folder as the video file.";
            //
            // grpUpdate
            //
            grpUpdate.Controls.Add(cboNewVersionCheck);
            grpUpdate.Controls.Add(lblNewVersionCheck);
            grpUpdate.Location = new Point(12, 456);
            grpUpdate.Name = "grpUpdate";
            grpUpdate.Size = new Size(360, 90);
            grpUpdate.TabIndex = 4;
            grpUpdate.TabStop = false;
            grpUpdate.Text = "Updates";
            //
            // cboNewVersionCheck
            //
            cboNewVersionCheck.DropDownStyle = ComboBoxStyle.DropDownList;
            cboNewVersionCheck.FormattingEnabled = true;
            cboNewVersionCheck.Items.AddRange(new object[] { "Yes", "No" });
            cboNewVersionCheck.Location = new Point(10, 46);
            cboNewVersionCheck.Name = "cboNewVersionCheck";
            cboNewVersionCheck.Size = new Size(218, 23);
            cboNewVersionCheck.TabIndex = 1;
            //
            // lblNewVersionCheck
            //
            lblNewVersionCheck.AutoSize = true;
            lblNewVersionCheck.Location = new Point(10, 28);
            lblNewVersionCheck.Name = "lblNewVersionCheck";
            lblNewVersionCheck.TabIndex = 0;
            lblNewVersionCheck.Text = "Check for &New Version at Startup";
            //
            // btnOK
            //
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(216, 442);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 27);
            btnOK.TabIndex = 5;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            //
            // btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(297, 442);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 27);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            //
            // SettingsForm
            //
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(384, 481);
            ControlBox = false;
            Controls.Add(grpPlayer);
            Controls.Add(grpOpen);
            Controls.Add(grpResume);
            Controls.Add(grpFrames);
            Controls.Add(grpUpdate);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            Load += SettingsForm_Load;
            grpPlayer.ResumeLayout(false);
            grpPlayer.PerformLayout();
            grpOpen.ResumeLayout(false);
            grpOpen.PerformLayout();
            grpResume.ResumeLayout(false);
            grpResume.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudRewind).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAudioMinutes).EndInit();
            grpFrames.ResumeLayout(false);
            grpFrames.PerformLayout();
            grpUpdate.ResumeLayout(false);
            grpUpdate.PerformLayout();
            ResumeLayout(false);
        }

        private ToolTip toolTip;
        private GroupBox grpPlayer;
        private CheckBox chkStartFullScreen;
        private CheckBox chkSeekThumbnails;
        private CheckBox chkShowSubtitles;
        private CheckBox chkTrackMenus;
        private Label lblCoverSize;
        private ComboBox cboCoverSize;
        private Label lblButtonLook;
        private ComboBox cboButtonLook;
        private GroupBox grpOpen;
        private Label lblOpenFolder;
        private TextBox txtOpenFolder;
        private Button btnBrowseOpen;
        private Label lblOpenHint;
        private GroupBox grpResume;
        private Label lblResumeMode;
        private ComboBox cboResumeMode;
        private Label lblRewind;
        private NumericUpDown nudRewind;
        private Label lblSeconds;
        private Label lblSaveResume;
        private ComboBox cboSaveResume;
        private Label lblAudioMinutes;
        private NumericUpDown nudAudioMinutes;
        private Label lblMinutes;
        private GroupBox grpFrames;
        private Label lblFramesFolder;
        private TextBox txtFramesFolder;
        private Button btnBrowseFrames;
        private Label lblFramesHint;
        private GroupBox grpUpdate;
        private ComboBox cboNewVersionCheck;
        private Label lblNewVersionCheck;
        private Button btnOK;
        private Button btnCancel;
    }
}
