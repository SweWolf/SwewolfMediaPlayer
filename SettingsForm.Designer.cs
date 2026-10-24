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
            lblCoverSize = new Label();
            cboCoverSize = new ComboBox();
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
            grpUpdate = new GroupBox();
            cboNewVersionCheck = new ComboBox();
            lblNewVersionCheck = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            grpPlayer.SuspendLayout();
            grpResume.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudRewind).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudAudioMinutes).BeginInit();
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
            grpPlayer.Controls.Add(lblCoverSize);
            grpPlayer.Controls.Add(cboCoverSize);
            grpPlayer.Location = new Point(12, 12);
            grpPlayer.Name = "grpPlayer";
            grpPlayer.Size = new Size(360, 132);
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
            toolTip.SetToolTip(chkStartFullScreen, "Open every video file in full screen mode, whether it is opened from the main window or from the command line. Audio files never open in full screen mode. Press Esc to leave full screen mode.");
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
            toolTip.SetToolTip(chkSeekThumbnails, "Show a small picture of the video at the pointer while you point at the seek bar. Video files only. It needs FFmpeg (ffmpeg.exe in the program folder or in the PATH); without it nothing is shown.");
            chkSeekThumbnails.UseVisualStyleBackColor = true;
            //
            // lblCoverSize
            //
            lblCoverSize.AutoSize = true;
            lblCoverSize.Location = new Point(10, 80);
            lblCoverSize.Name = "lblCoverSize";
            lblCoverSize.TabIndex = 2;
            lblCoverSize.Text = "Cover Picture Si&ze for Music Files";
            //
            // cboCoverSize
            //
            cboCoverSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCoverSize.FormattingEnabled = true;
            cboCoverSize.Items.AddRange(new object[] { "Medium", "Maximum" });
            cboCoverSize.Location = new Point(10, 98);
            cboCoverSize.Name = "cboCoverSize";
            cboCoverSize.Size = new Size(218, 23);
            cboCoverSize.TabIndex = 3;
            toolTip.SetToolTip(cboCoverSize, "How large the cover picture of a music file is shown: Medium is 75 percent of the largest size that fits the window, Maximum fills the video area.");
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
            grpResume.TabIndex = 1;
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
            toolTip.SetToolTip(cboResumeMode, "What happens when you open a file you stopped in before. Yes: continue from where you stopped, without asking. Ask: ask first. No: always start from the beginning, and the player does not remember where you stopped.");
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
            toolTip.SetToolTip(nudRewind, "The saved position is this many seconds before the point where you stopped, so you see a little of what came before. 0 saves the exact point. Example: with 10 seconds, stopping at 2:00 saves 1:50.");
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
            toolTip.SetToolTip(cboSaveResume, "The player remembers where you stopped. It starts after the file has played for 60 seconds. The position is kept in the library database that SweWolf File Explorer uses too.");
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
            toolTip.SetToolTip(nudAudioMinutes, "Audio files of this length or longer get a resume point (for example podcasts and audio books). Shorter ones do not.");
            //
            // lblMinutes
            //
            lblMinutes.AutoSize = true;
            lblMinutes.Location = new Point(254, 192);
            lblMinutes.Name = "lblMinutes";
            lblMinutes.TabIndex = 9;
            lblMinutes.Text = "minutes";
            //
            // grpUpdate
            //
            grpUpdate.Controls.Add(cboNewVersionCheck);
            grpUpdate.Controls.Add(lblNewVersionCheck);
            grpUpdate.Location = new Point(12, 334);
            grpUpdate.Name = "grpUpdate";
            grpUpdate.Size = new Size(360, 90);
            grpUpdate.TabIndex = 2;
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
            btnOK.TabIndex = 3;
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
            btnCancel.TabIndex = 4;
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
            Controls.Add(grpResume);
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
            grpResume.ResumeLayout(false);
            grpResume.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudRewind).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAudioMinutes).EndInit();
            grpUpdate.ResumeLayout(false);
            grpUpdate.PerformLayout();
            ResumeLayout(false);
        }

        private ToolTip toolTip;
        private GroupBox grpPlayer;
        private CheckBox chkStartFullScreen;
        private CheckBox chkSeekThumbnails;
        private Label lblCoverSize;
        private ComboBox cboCoverSize;
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
        private GroupBox grpUpdate;
        private ComboBox cboNewVersionCheck;
        private Label lblNewVersionCheck;
        private Button btnOK;
        private Button btnCancel;
    }
}
