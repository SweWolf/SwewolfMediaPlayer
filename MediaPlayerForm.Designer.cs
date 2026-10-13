namespace SwewolfMediaPlayer
{
    partial class MediaPlayerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new ToolTip(components);
            btnPlayPause = new IconButton();
            btnStop = new IconButton();
            btnSaveFrame = new IconButton();
            btnMute = new IconButton();
            trkVolume = new JumpTrackBar();
            txtStartAt = new TextBox();
            btnStartNow = new Button();
            txtEndAt = new TextBox();
            btnEndNow = new Button();
            txtFadeIn = new TextBox();
            txtFadeOut = new TextBox();
            btnPreview = new Button();
            btnTrim = new Button();
            menuVideo = new ContextMenuStrip(components);
            videoView = new LibVLCSharp.WinForms.VideoView();
            picCover = new PictureBox();
            pnlControls = new Panel();
            trkSeek = new JumpTrackBar();
            lblTime = new Label();
            menuControls = new ContextMenuStrip(components);
            mnuShowTrim = new ToolStripMenuItem();
            grpTrim = new GroupBox();
            lblStartAt = new Label();
            lblEndAt = new Label();
            lblFadeIn = new Label();
            lblFadeOut = new Label();
            (trkVolume).BeginInit();
            ((System.ComponentModel.ISupportInitialize)videoView).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picCover).BeginInit();
            pnlControls.SuspendLayout();
            (trkSeek).BeginInit();
            menuControls.SuspendLayout();
            grpTrim.SuspendLayout();
            SuspendLayout();
            // 
            // btnPlayPause
            // 
            btnPlayPause.BackColor = Color.FromArgb(48, 48, 48);
            btnPlayPause.FlatStyle = FlatStyle.Flat;
            btnPlayPause.ForeColor = Color.FromArgb(230, 230, 230);
            btnPlayPause.Icon = IconKind.Play;
            btnPlayPause.Location = new Point(12, 12);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(40, 32);
            btnPlayPause.TabIndex = 0;
            toolTip.SetToolTip(btnPlayPause, "Play (Space)");
            btnPlayPause.UseVisualStyleBackColor = false;
            btnPlayPause.Click += btnPlayPause_Click;
            // 
            // btnStop
            // 
            btnStop.BackColor = Color.FromArgb(48, 48, 48);
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.ForeColor = Color.FromArgb(230, 230, 230);
            btnStop.Icon = IconKind.Stop;
            btnStop.Location = new Point(58, 12);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(40, 32);
            btnStop.TabIndex = 1;
            toolTip.SetToolTip(btnStop, "Stop");
            btnStop.UseVisualStyleBackColor = false;
            btnStop.Click += btnStop_Click;
            // 
            // btnSaveFrame
            // 
            btnSaveFrame.BackColor = Color.FromArgb(48, 48, 48);
            btnSaveFrame.FlatStyle = FlatStyle.Flat;
            btnSaveFrame.ForeColor = Color.FromArgb(230, 230, 230);
            btnSaveFrame.Icon = IconKind.Camera;
            btnSaveFrame.Location = new Point(104, 12);
            btnSaveFrame.Name = "btnSaveFrame";
            btnSaveFrame.Size = new Size(40, 32);
            btnSaveFrame.TabIndex = 2;
            toolTip.SetToolTip(btnSaveFrame, "Save Frame as PNG (S)");
            btnSaveFrame.UseVisualStyleBackColor = false;
            btnSaveFrame.Click += btnSaveFrame_Click;
            // 
            // btnMute
            // 
            btnMute.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMute.BackColor = Color.FromArgb(48, 48, 48);
            btnMute.FlatStyle = FlatStyle.Flat;
            btnMute.ForeColor = Color.FromArgb(230, 230, 230);
            btnMute.Icon = IconKind.Volume;
            btnMute.Location = new Point(1010, 12);
            btnMute.Name = "btnMute";
            btnMute.Size = new Size(40, 32);
            btnMute.TabIndex = 5;
            toolTip.SetToolTip(btnMute, "Mute");
            btnMute.UseVisualStyleBackColor = false;
            btnMute.Click += btnMute_Click;
            // 
            // trkVolume
            // 
            trkVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            trkVolume.AutoSize = false;
            trkVolume.BackColor = Color.FromArgb(32, 32, 32);
            trkVolume.LargeChange = 10;
            trkVolume.Location = new Point(1056, 12);
            trkVolume.Maximum = 100;
            trkVolume.Name = "trkVolume";
            trkVolume.Size = new Size(110, 32);
            trkVolume.SmallChange = 5;
            trkVolume.TabIndex = 6;
            trkVolume.TickStyle = TickStyle.None;
            toolTip.SetToolTip(trkVolume, "Volume: 100%");
            trkVolume.Value = 100;
            trkVolume.Scroll += trkVolume_Scroll;
            // 
            // txtStartAt
            // 
            txtStartAt.BackColor = Color.FromArgb(48, 48, 48);
            txtStartAt.BorderStyle = BorderStyle.FixedSingle;
            txtStartAt.ForeColor = Color.FromArgb(230, 230, 230);
            txtStartAt.Location = new Point(66, 25);
            txtStartAt.Name = "txtStartAt";
            txtStartAt.Size = new Size(80, 23);
            txtStartAt.TabIndex = 1;
            toolTip.SetToolTip(txtStartAt, "Where the trimmed file starts, for example 1:23:45, 2:07, 90, 7.5 or 45%. Leave it empty to start at the beginning.");
            // 
            // btnStartNow
            // 
            btnStartNow.BackColor = Color.FromArgb(48, 48, 48);
            btnStartNow.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnStartNow.FlatStyle = FlatStyle.Flat;
            btnStartNow.ForeColor = Color.FromArgb(230, 230, 230);
            btnStartNow.Location = new Point(150, 24);
            btnStartNow.Name = "btnStartNow";
            btnStartNow.Size = new Size(46, 25);
            btnStartNow.TabIndex = 2;
            btnStartNow.Text = "Now";
            toolTip.SetToolTip(btnStartNow, "Use the current position of the player as the start");
            btnStartNow.UseVisualStyleBackColor = false;
            btnStartNow.Click += btnStartNow_Click;
            // 
            // txtEndAt
            // 
            txtEndAt.BackColor = Color.FromArgb(48, 48, 48);
            txtEndAt.BorderStyle = BorderStyle.FixedSingle;
            txtEndAt.ForeColor = Color.FromArgb(230, 230, 230);
            txtEndAt.Location = new Point(262, 25);
            txtEndAt.Name = "txtEndAt";
            txtEndAt.Size = new Size(80, 23);
            txtEndAt.TabIndex = 4;
            toolTip.SetToolTip(txtEndAt, "Where the trimmed file ends, for example 1:23:45, 2:07, 90, 7.5 or 45%. Leave it empty to end at the end of the file.");
            // 
            // btnEndNow
            // 
            btnEndNow.BackColor = Color.FromArgb(48, 48, 48);
            btnEndNow.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnEndNow.FlatStyle = FlatStyle.Flat;
            btnEndNow.ForeColor = Color.FromArgb(230, 230, 230);
            btnEndNow.Location = new Point(346, 24);
            btnEndNow.Name = "btnEndNow";
            btnEndNow.Size = new Size(46, 25);
            btnEndNow.TabIndex = 5;
            btnEndNow.Text = "Now";
            toolTip.SetToolTip(btnEndNow, "Use the current position of the player as the end");
            btnEndNow.UseVisualStyleBackColor = false;
            btnEndNow.Click += btnEndNow_Click;
            // 
            // txtFadeIn
            // 
            txtFadeIn.BackColor = Color.FromArgb(48, 48, 48);
            txtFadeIn.BorderStyle = BorderStyle.FixedSingle;
            txtFadeIn.ForeColor = Color.FromArgb(230, 230, 230);
            txtFadeIn.Location = new Point(484, 25);
            txtFadeIn.Name = "txtFadeIn";
            txtFadeIn.Size = new Size(44, 23);
            txtFadeIn.TabIndex = 7;
            txtFadeIn.Text = "0";
            toolTip.SetToolTip(txtFadeIn, "Length of the fade in at the start, in seconds. 0 means no fade.");
            // 
            // txtFadeOut
            // 
            txtFadeOut.BackColor = Color.FromArgb(48, 48, 48);
            txtFadeOut.BorderStyle = BorderStyle.FixedSingle;
            txtFadeOut.ForeColor = Color.FromArgb(230, 230, 230);
            txtFadeOut.Location = new Point(620, 25);
            txtFadeOut.Name = "txtFadeOut";
            txtFadeOut.Size = new Size(44, 23);
            txtFadeOut.TabIndex = 9;
            txtFadeOut.Text = "0";
            toolTip.SetToolTip(txtFadeOut, "Length of the fade out at the end, in seconds. 0 means no fade.");
            // 
            // btnPreview
            // 
            btnPreview.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPreview.BackColor = Color.FromArgb(48, 48, 48);
            btnPreview.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnPreview.FlatStyle = FlatStyle.Flat;
            btnPreview.ForeColor = Color.FromArgb(230, 230, 230);
            btnPreview.Location = new Point(1006, 24);
            btnPreview.Name = "btnPreview";
            btnPreview.Size = new Size(80, 25);
            btnPreview.TabIndex = 10;
            btnPreview.Text = "Preview";
            toolTip.SetToolTip(btnPreview, "Play the part between Start at and End at, with the fades. The file is not changed.");
            btnPreview.UseVisualStyleBackColor = false;
            btnPreview.Click += btnPreview_Click;
            // 
            // btnTrim
            // 
            btnTrim.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTrim.BackColor = Color.FromArgb(48, 48, 48);
            btnTrim.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnTrim.FlatStyle = FlatStyle.Flat;
            btnTrim.ForeColor = Color.FromArgb(230, 230, 230);
            btnTrim.Location = new Point(1092, 24);
            btnTrim.Name = "btnTrim";
            btnTrim.Size = new Size(74, 25);
            btnTrim.TabIndex = 11;
            btnTrim.Text = "Trim";
            toolTip.SetToolTip(btnTrim, "Trim the file with SplitMediaFiles. The original file is kept.");
            btnTrim.UseVisualStyleBackColor = false;
            btnTrim.Click += btnTrim_Click;
            // 
            // menuVideo
            // 
            menuVideo.Name = "menuVideo";
            menuVideo.Size = new Size(61, 4);
            menuVideo.Opening += menuVideo_Opening;
            // 
            // videoView
            // 
            videoView.BackColor = Color.Black;
            videoView.Dock = DockStyle.Fill;
            videoView.Location = new Point(0, 0);
            videoView.MediaPlayer = null;
            videoView.Name = "videoView";
            videoView.Size = new Size(1178, 580);
            videoView.TabIndex = 0;
            // 
            // picCover
            // 
            picCover.BackColor = Color.Black;
            picCover.Dock = DockStyle.Fill;
            picCover.Location = new Point(0, 0);
            picCover.Name = "picCover";
            picCover.Size = new Size(1178, 580);
            picCover.SizeMode = PictureBoxSizeMode.Zoom;
            picCover.TabIndex = 3;
            picCover.TabStop = false;
            picCover.Visible = false;
            // 
            // pnlControls
            // 
            pnlControls.BackColor = Color.FromArgb(32, 32, 32);
            pnlControls.ContextMenuStrip = menuControls;
            pnlControls.Controls.Add(btnPlayPause);
            pnlControls.Controls.Add(btnStop);
            pnlControls.Controls.Add(btnSaveFrame);
            pnlControls.Controls.Add(trkSeek);
            pnlControls.Controls.Add(lblTime);
            pnlControls.Controls.Add(btnMute);
            pnlControls.Controls.Add(trkVolume);
            pnlControls.Dock = DockStyle.Bottom;
            pnlControls.Location = new Point(0, 642);
            pnlControls.Name = "pnlControls";
            pnlControls.Size = new Size(1178, 56);
            pnlControls.TabIndex = 1;
            // 
            // trkSeek
            // 
            trkSeek.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trkSeek.AutoSize = false;
            trkSeek.BackColor = Color.FromArgb(32, 32, 32);
            trkSeek.Location = new Point(152, 12);
            trkSeek.Maximum = 1000;
            trkSeek.Name = "trkSeek";
            trkSeek.Size = new Size(684, 32);
            trkSeek.TabIndex = 3;
            trkSeek.TickStyle = TickStyle.None;
            trkSeek.Scroll += trkSeek_Scroll;
            trkSeek.MouseDown += trkSeek_MouseDown;
            trkSeek.MouseUp += trkSeek_MouseUp;
            // 
            // lblTime
            // 
            lblTime.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTime.ContextMenuStrip = menuControls;
            lblTime.ForeColor = Color.FromArgb(230, 230, 230);
            lblTime.Location = new Point(844, 12);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(160, 32);
            lblTime.TabIndex = 4;
            lblTime.Text = "0:00 / 0:00";
            lblTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // menuControls
            // 
            menuControls.Items.AddRange(new ToolStripItem[] { mnuShowTrim });
            menuControls.Name = "menuControls";
            menuControls.Size = new Size(178, 26);
            menuControls.Opening += menuControls_Opening;
            // 
            // mnuShowTrim
            // 
            mnuShowTrim.Name = "mnuShowTrim";
            mnuShowTrim.Size = new Size(177, 22);
            mnuShowTrim.Text = "Show Trim Features";
            mnuShowTrim.Click += mnuShowTrim_Click;
            // 
            // grpTrim
            // 
            grpTrim.ContextMenuStrip = menuControls;
            grpTrim.Controls.Add(lblStartAt);
            grpTrim.Controls.Add(txtStartAt);
            grpTrim.Controls.Add(btnStartNow);
            grpTrim.Controls.Add(lblEndAt);
            grpTrim.Controls.Add(txtEndAt);
            grpTrim.Controls.Add(btnEndNow);
            grpTrim.Controls.Add(lblFadeIn);
            grpTrim.Controls.Add(txtFadeIn);
            grpTrim.Controls.Add(lblFadeOut);
            grpTrim.Controls.Add(txtFadeOut);
            grpTrim.Controls.Add(btnPreview);
            grpTrim.Controls.Add(btnTrim);
            grpTrim.Dock = DockStyle.Bottom;
            grpTrim.Location = new Point(0, 580);
            grpTrim.Name = "grpTrim";
            grpTrim.Size = new Size(1178, 62);
            grpTrim.TabIndex = 2;
            grpTrim.TabStop = false;
            grpTrim.Text = "Trim";
            grpTrim.Visible = false;
            // 
            // lblStartAt
            // 
            lblStartAt.AutoSize = true;
            lblStartAt.Location = new Point(12, 28);
            lblStartAt.Name = "lblStartAt";
            lblStartAt.Size = new Size(44, 15);
            lblStartAt.TabIndex = 0;
            lblStartAt.Text = "Start at";
            // 
            // lblEndAt
            // 
            lblEndAt.AutoSize = true;
            lblEndAt.Location = new Point(216, 28);
            lblEndAt.Name = "lblEndAt";
            lblEndAt.Size = new Size(40, 15);
            lblEndAt.TabIndex = 3;
            lblEndAt.Text = "End at";
            // 
            // lblFadeIn
            // 
            lblFadeIn.AutoSize = true;
            lblFadeIn.Location = new Point(416, 28);
            lblFadeIn.Name = "lblFadeIn";
            lblFadeIn.Size = new Size(61, 15);
            lblFadeIn.TabIndex = 6;
            lblFadeIn.Text = "Fade in (s)";
            // 
            // lblFadeOut
            // 
            lblFadeOut.AutoSize = true;
            lblFadeOut.Location = new Point(544, 28);
            lblFadeOut.Name = "lblFadeOut";
            lblFadeOut.Size = new Size(69, 15);
            lblFadeOut.TabIndex = 8;
            lblFadeOut.Text = "Fade out (s)";
            // 
            // MediaPlayerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AllowDrop = true;
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(1178, 698);
            Controls.Add(picCover);
            Controls.Add(videoView);
            Controls.Add(grpTrim);
            Controls.Add(pnlControls);
            ForeColor = Color.FromArgb(230, 230, 230);
            Name = "MediaPlayerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Media Player";
            DragEnter += MediaPlayerForm_DragEnter;
            DragDrop += MediaPlayerForm_DragDrop;
            WindowState = FormWindowState.Maximized;
            (trkVolume).EndInit();
            ((System.ComponentModel.ISupportInitialize)videoView).EndInit();
            ((System.ComponentModel.ISupportInitialize)picCover).EndInit();
            pnlControls.ResumeLayout(false);
            (trkSeek).EndInit();
            menuControls.ResumeLayout(false);
            grpTrim.ResumeLayout(false);
            grpTrim.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private LibVLCSharp.WinForms.VideoView videoView;
        private PictureBox picCover;
        private ToolTip toolTip;
        private ContextMenuStrip menuVideo;
        private Panel pnlControls;
        private IconButton btnPlayPause;
        private IconButton btnStop;
        private IconButton btnSaveFrame;
        private JumpTrackBar trkSeek;
        private Label lblTime;
        private IconButton btnMute;
        private JumpTrackBar trkVolume;
        private GroupBox grpTrim;
        private Label lblStartAt;
        private TextBox txtStartAt;
        private Button btnStartNow;
        private Label lblEndAt;
        private TextBox txtEndAt;
        private Button btnEndNow;
        private Label lblFadeIn;
        private TextBox txtFadeIn;
        private Label lblFadeOut;
        private TextBox txtFadeOut;
        private Button btnPreview;
        private Button btnTrim;
        private ContextMenuStrip menuControls;
        private ToolStripMenuItem mnuShowTrim;
    }
}
