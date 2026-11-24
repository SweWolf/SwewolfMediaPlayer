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
            btnMore = new IconButton();
            trkVolume = new JumpTrackBar();
            txtStartAt = new TextBox();
            btnStartNow = new Button();
            txtEndAt = new TextBox();
            btnEndNow = new Button();
            txtFadeIn = new TextBox();
            txtFadeOut = new TextBox();
            btnPreview = new Button();
            btnTrim = new Button();
            cboTrimMode = new ComboBox();
            btnQuickPreview = new Button();
            btnPrevMark = new IconButton();
            btnNextMark = new IconButton();
            btnCloseTrim = new CloseXButton();
            btnCloseControlPanel = new CloseXButton();
            menuVideo = new ContextMenuStrip(components);
            videoView = new LibVLCSharp.WinForms.VideoView();
            picCover = new CoverBox();
            pnlControls = new Panel();
            trkSeek = new JumpTrackBar();
            lblTime = new Label();
            menuControls = new ContextMenuStrip(components);
            menuSaveFrame = new ContextMenuStrip(components);
            mnuSetPreviewPicture = new ToolStripMenuItem();
            sepSaveFrame = new ToolStripSeparator();
            mnuSaveWithoutAsking = new ToolStripMenuItem();
            sepOpenFolder = new ToolStripSeparator();
            mnuOpenFolder = new ToolStripMenuItem();
            mnuShowTrim = new ToolStripMenuItem();
            mnuShowControlPanel = new ToolStripMenuItem();
            grpControlPanel = new GroupBox();
            lblWhenEnds = new Label();
            cboWhenEnds = new ComboBox();
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
            menuSaveFrame.SuspendLayout();
            grpTrim.SuspendLayout();
            grpControlPanel.SuspendLayout();
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
            btnSaveFrame.ContextMenuStrip = menuSaveFrame;
            // 
            // btnMute
            // 
            btnMute.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMute.BackColor = Color.FromArgb(48, 48, 48);
            btnMute.FlatStyle = FlatStyle.Flat;
            btnMute.ForeColor = Color.FromArgb(230, 230, 230);
            btnMute.Icon = IconKind.Volume;
            btnMute.Location = new Point(964, 12);
            btnMute.Name = "btnMute";
            btnMute.Size = new Size(40, 32);
            btnMute.TabIndex = 5;
            toolTip.SetToolTip(btnMute, "Mute");
            btnMute.UseVisualStyleBackColor = false;
            btnMute.Click += btnMute_Click;
            // 
            // btnMore
            // 
            btnMore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMore.BackColor = Color.FromArgb(48, 48, 48);
            btnMore.FlatStyle = FlatStyle.Flat;
            btnMore.ForeColor = Color.FromArgb(230, 230, 230);
            btnMore.Icon = IconKind.More;
            btnMore.Location = new Point(1126, 12);
            btnMore.Name = "btnMore";
            btnMore.Size = new Size(40, 32);
            btnMore.TabIndex = 7;
            toolTip.SetToolTip(btnMore, "More Options");
            btnMore.UseVisualStyleBackColor = false;
            btnMore.Click += btnMore_Click;
            // 
            // trkVolume
            // 
            trkVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            trkVolume.AutoSize = false;
            trkVolume.BackColor = Color.FromArgb(32, 32, 32);
            trkVolume.LargeChange = 10;
            trkVolume.Location = new Point(1010, 12);
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
            txtStartAt.TabIndex = 2;
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
            btnStartNow.TabIndex = 3;
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
            txtEndAt.TabIndex = 5;
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
            btnEndNow.TabIndex = 6;
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
            txtFadeIn.TabIndex = 8;
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
            txtFadeOut.TabIndex = 10;
            txtFadeOut.Text = "0";
            toolTip.SetToolTip(txtFadeOut, "Length of the fade out at the end, in seconds. 0 means no fade.");
            // 
            // btnPreview
            // 
            btnPreview.BackColor = Color.FromArgb(48, 48, 48);
            btnPreview.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnPreview.FlatStyle = FlatStyle.Flat;
            btnPreview.ForeColor = Color.FromArgb(230, 230, 230);
            btnPreview.Location = new Point(210, 60);
            btnPreview.Name = "btnPreview";
            btnPreview.Size = new Size(80, 25);
            btnPreview.TabIndex = 14;
            btnPreview.Text = "Preview";
            toolTip.SetToolTip(btnPreview, "Play the result with the fades. The file is not changed.");
            btnPreview.UseVisualStyleBackColor = false;
            btnPreview.Click += btnPreview_Click;
            //
            // btnQuickPreview
            //
            btnQuickPreview.BackColor = Color.FromArgb(48, 48, 48);
            btnQuickPreview.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnQuickPreview.FlatStyle = FlatStyle.Flat;
            btnQuickPreview.ForeColor = Color.FromArgb(230, 230, 230);
            btnQuickPreview.Location = new Point(96, 60);
            btnQuickPreview.Name = "btnQuickPreview";
            btnQuickPreview.Size = new Size(108, 25);
            btnQuickPreview.TabIndex = 13;
            btnQuickPreview.Text = "Quick Preview";
            toolTip.SetToolTip(btnQuickPreview, "Play only the places where something happens, a few seconds each. The file is not changed.");
            btnQuickPreview.UseVisualStyleBackColor = false;
            btnQuickPreview.Click += btnQuickPreview_Click;
            //
            // btnPrevMark
            //
            btnPrevMark.BackColor = Color.FromArgb(48, 48, 48);
            btnPrevMark.FlatStyle = FlatStyle.Flat;
            btnPrevMark.ForeColor = Color.FromArgb(230, 230, 230);
            btnPrevMark.Icon = IconKind.PreviousChapter;
            btnPrevMark.Location = new Point(12, 60);
            btnPrevMark.Name = "btnPrevMark";
            btnPrevMark.Size = new Size(36, 25);
            btnPrevMark.TabIndex = 11;
            toolTip.SetToolTip(btnPrevMark, "Go to the previous time stamp in the boxes above");
            btnPrevMark.UseVisualStyleBackColor = false;
            btnPrevMark.Click += btnPrevMark_Click;
            //
            // btnNextMark
            //
            btnNextMark.BackColor = Color.FromArgb(48, 48, 48);
            btnNextMark.FlatStyle = FlatStyle.Flat;
            btnNextMark.ForeColor = Color.FromArgb(230, 230, 230);
            btnNextMark.Icon = IconKind.NextChapter;
            btnNextMark.Location = new Point(54, 60);
            btnNextMark.Name = "btnNextMark";
            btnNextMark.Size = new Size(36, 25);
            btnNextMark.TabIndex = 12;
            toolTip.SetToolTip(btnNextMark, "Go to the next time stamp in the boxes above");
            btnNextMark.UseVisualStyleBackColor = false;
            btnNextMark.Click += btnNextMark_Click;
            //
            // cboTrimMode
            //
            cboTrimMode.BackColor = Color.FromArgb(48, 48, 48);
            cboTrimMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTrimMode.FlatStyle = FlatStyle.Flat;
            cboTrimMode.ForeColor = Color.FromArgb(230, 230, 230);
            cboTrimMode.Items.AddRange(new object[] { "Trim", "Remove", "Split" });
            cboTrimMode.Location = new Point(12, 25);
            cboTrimMode.Name = "cboTrimMode";
            cboTrimMode.Size = new Size(84, 23);
            cboTrimMode.TabIndex = 0;
            toolTip.SetToolTip(cboTrimMode, "Trim keeps one part of the file. Remove cuts a part out of the middle. Split cuts the file into several files.");
            cboTrimMode.SelectedIndexChanged += cboTrimMode_SelectedIndexChanged;
            //
            // btnTrim
            //
            btnTrim.BackColor = Color.FromArgb(48, 48, 48);
            btnTrim.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            btnTrim.FlatStyle = FlatStyle.Flat;
            btnTrim.ForeColor = Color.FromArgb(230, 230, 230);
            btnTrim.Location = new Point(312, 60);
            btnTrim.Name = "btnTrim";
            btnTrim.Size = new Size(74, 25);
            btnTrim.TabIndex = 15;
            btnTrim.Text = "Start";
            toolTip.SetToolTip(btnTrim, "Trim the file with SplitMediaFiles. The original file is kept.");
            btnTrim.UseVisualStyleBackColor = false;
            btnTrim.Click += btnTrim_Click;
            //
            // btnCloseTrim
            //
            btnCloseTrim.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCloseTrim.Location = new Point(1154, 9);
            btnCloseTrim.Name = "btnCloseTrim";
            btnCloseTrim.Size = new Size(22, 20);
            btnCloseTrim.TabIndex = 16;
            toolTip.SetToolTip(btnCloseTrim, "Close Trim");
            btnCloseTrim.UseVisualStyleBackColor = false;
            btnCloseTrim.Click += btnCloseTrim_Click;
            //
            // btnCloseControlPanel
            //
            btnCloseControlPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCloseControlPanel.Location = new Point(1154, 9);
            btnCloseControlPanel.Name = "btnCloseControlPanel";
            btnCloseControlPanel.Size = new Size(22, 20);
            btnCloseControlPanel.TabIndex = 2;
            toolTip.SetToolTip(btnCloseControlPanel, "Close Control Panel");
            btnCloseControlPanel.UseVisualStyleBackColor = false;
            btnCloseControlPanel.Click += btnCloseControlPanel_Click;
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
            pnlControls.Controls.Add(btnMore);
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
            trkSeek.Size = new Size(608, 32);
            trkSeek.TabIndex = 3;
            trkSeek.TickStyle = TickStyle.None;
            trkSeek.Scroll += trkSeek_Scroll;
            trkSeek.MouseDown += trkSeek_MouseDown;
            trkSeek.MouseUp += trkSeek_MouseUp;
            trkSeek.MouseMove += trkSeek_MouseMove;
            trkSeek.MouseLeave += trkSeek_MouseLeave;
            // 
            // lblTime
            // 
            lblTime.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTime.ContextMenuStrip = menuControls;
            lblTime.ForeColor = Color.FromArgb(230, 230, 230);
            lblTime.Location = new Point(838, 12);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(120, 32);
            lblTime.TabIndex = 4;
            lblTime.Text = "0:00 / 0:00";
            lblTime.TextAlign = ContentAlignment.MiddleRight;
            // 
            // menuControls
            // 
            menuControls.Items.AddRange(new ToolStripItem[] { mnuShowTrim, mnuShowControlPanel });
            menuControls.Name = "menuControls";
            menuControls.Size = new Size(178, 26);
            menuControls.Opening += menuControls_Opening;
            //
            // menuSaveFrame (right-click on the camera button)
            //
            menuSaveFrame.Items.AddRange(new ToolStripItem[] { mnuSetPreviewPicture, sepSaveFrame, mnuSaveWithoutAsking, sepOpenFolder, mnuOpenFolder });
            menuSaveFrame.Name = "menuSaveFrame";
            menuSaveFrame.Size = new Size(210, 26);
            menuSaveFrame.Opening += menuSaveFrame_Opening;
            //
            // mnuSetPreviewPicture
            //
            mnuSetPreviewPicture.Name = "mnuSetPreviewPicture";
            mnuSetPreviewPicture.Size = new Size(209, 22);
            mnuSetPreviewPicture.Text = "Set as Preview Picture...";
            mnuSetPreviewPicture.Click += mnuSetPreviewPicture_Click;
            //
            // sepSaveFrame
            //
            sepSaveFrame.Name = "sepSaveFrame";
            sepSaveFrame.Size = new Size(206, 6);
            //
            // mnuSaveWithoutAsking
            //
            mnuSaveWithoutAsking.Name = "mnuSaveWithoutAsking";
            mnuSaveWithoutAsking.Size = new Size(209, 22);
            mnuSaveWithoutAsking.Text = "Save Without Asking for a Folder";
            mnuSaveWithoutAsking.Click += mnuSaveWithoutAsking_Click;
            //
            // sepOpenFolder
            //
            sepOpenFolder.Name = "sepOpenFolder";
            sepOpenFolder.Size = new Size(206, 6);
            //
            // mnuOpenFolder
            //
            mnuOpenFolder.Name = "mnuOpenFolder";
            mnuOpenFolder.Size = new Size(209, 22);
            mnuOpenFolder.Text = "Open Folder";
            mnuOpenFolder.Click += mnuOpenFolder_Click;
            //
            // mnuShowTrim
            // 
            mnuShowTrim.Name = "mnuShowTrim";
            mnuShowTrim.Size = new Size(177, 22);
            mnuShowTrim.Text = "Show Trim/Remove/Split";
            mnuShowTrim.Click += mnuShowTrim_Click;
            // 
            // mnuShowControlPanel
            // 
            mnuShowControlPanel.Name = "mnuShowControlPanel";
            mnuShowControlPanel.Size = new Size(177, 22);
            mnuShowControlPanel.Text = "Control Panel";
            mnuShowControlPanel.Click += mnuShowControlPanel_Click;
            // 
            // grpControlPanel
            // 
            grpControlPanel.ContextMenuStrip = menuControls;
            grpControlPanel.Controls.Add(lblWhenEnds);
            grpControlPanel.Controls.Add(cboWhenEnds);
            grpControlPanel.Controls.Add(btnCloseControlPanel);
            grpControlPanel.Dock = DockStyle.Bottom;
            grpControlPanel.Location = new Point(0, 518);
            grpControlPanel.Name = "grpControlPanel";
            grpControlPanel.Size = new Size(1178, 62);
            grpControlPanel.TabIndex = 3;
            grpControlPanel.TabStop = false;
            grpControlPanel.Text = "Control Panel";
            grpControlPanel.Visible = false;
            // 
            // lblWhenEnds
            // 
            lblWhenEnds.AutoSize = true;
            lblWhenEnds.Location = new Point(12, 28);
            lblWhenEnds.Name = "lblWhenEnds";
            lblWhenEnds.Size = new Size(106, 15);
            lblWhenEnds.TabIndex = 0;
            lblWhenEnds.Text = "When the Media File Ends";
            // 
            // cboWhenEnds
            // 
            cboWhenEnds.BackColor = Color.FromArgb(48, 48, 48);
            cboWhenEnds.DropDownStyle = ComboBoxStyle.DropDownList;
            cboWhenEnds.FlatStyle = FlatStyle.Flat;
            cboWhenEnds.ForeColor = Color.FromArgb(230, 230, 230);
            cboWhenEnds.Location = new Point(150, 25);
            cboWhenEnds.Name = "cboWhenEnds";
            cboWhenEnds.Size = new Size(170, 23);
            cboWhenEnds.TabIndex = 1;
            toolTip.SetToolTip(cboWhenEnds, "What the player does when a media file has played to its end.\r\nFor a playlist: what happens after the last item.");
            cboWhenEnds.SelectedIndexChanged += cboWhenEnds_SelectedIndexChanged;
            // 
            // grpTrim
            // 
            grpTrim.ContextMenuStrip = menuControls;
            grpTrim.Controls.Add(cboTrimMode);
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
            grpTrim.Controls.Add(btnPrevMark);
            grpTrim.Controls.Add(btnNextMark);
            grpTrim.Controls.Add(btnQuickPreview);
            grpTrim.Controls.Add(btnPreview);
            grpTrim.Controls.Add(btnTrim);
            grpTrim.Controls.Add(btnCloseTrim);
            grpTrim.Dock = DockStyle.Bottom;
            grpTrim.Location = new Point(0, 580);
            grpTrim.Name = "grpTrim";
            grpTrim.Size = new Size(1178, 96);
            grpTrim.TabIndex = 2;
            grpTrim.TabStop = false;
            grpTrim.Text = "Trim/Remove/Split";
            grpTrim.Visible = false;
            // 
            // lblStartAt
            // 
            lblStartAt.AutoSize = true;
            lblStartAt.Location = new Point(12, 28);
            lblStartAt.Name = "lblStartAt";
            lblStartAt.Size = new Size(44, 15);
            lblStartAt.TabIndex = 1;
            lblStartAt.Text = "Start at";
            // 
            // lblEndAt
            // 
            lblEndAt.AutoSize = true;
            lblEndAt.Location = new Point(216, 28);
            lblEndAt.Name = "lblEndAt";
            lblEndAt.Size = new Size(40, 15);
            lblEndAt.TabIndex = 4;
            lblEndAt.Text = "End at";
            // 
            // lblFadeIn
            // 
            lblFadeIn.AutoSize = true;
            lblFadeIn.Location = new Point(416, 28);
            lblFadeIn.Name = "lblFadeIn";
            lblFadeIn.Size = new Size(61, 15);
            lblFadeIn.TabIndex = 7;
            lblFadeIn.Text = "Fade in (s)";
            // 
            // lblFadeOut
            // 
            lblFadeOut.AutoSize = true;
            lblFadeOut.Location = new Point(544, 28);
            lblFadeOut.Name = "lblFadeOut";
            lblFadeOut.Size = new Size(69, 15);
            lblFadeOut.TabIndex = 9;
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
            Controls.Add(grpControlPanel);
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
            menuSaveFrame.ResumeLayout(false);
            grpTrim.ResumeLayout(false);
            grpTrim.PerformLayout();
            grpControlPanel.ResumeLayout(false);
            grpControlPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private LibVLCSharp.WinForms.VideoView videoView;
        private CoverBox picCover;
        private ToolTip toolTip;
        private ContextMenuStrip menuVideo;
        private Panel pnlControls;
        private IconButton btnPlayPause;
        private IconButton btnStop;
        private IconButton btnSaveFrame;
        private JumpTrackBar trkSeek;
        private Label lblTime;
        private IconButton btnMute;
        private IconButton btnMore;
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
        private Button btnTrim;   // the Start button (named when it only trimmed)
        private ComboBox cboTrimMode;
        private Button btnQuickPreview;
        private IconButton btnPrevMark;
        private IconButton btnNextMark;
        private CloseXButton btnCloseTrim;
        private CloseXButton btnCloseControlPanel;
        private ContextMenuStrip menuControls;
        private ContextMenuStrip menuSaveFrame;
        private ToolStripMenuItem mnuSetPreviewPicture;
        private ToolStripSeparator sepSaveFrame;
        private ToolStripMenuItem mnuSaveWithoutAsking;
        private ToolStripSeparator sepOpenFolder;
        private ToolStripMenuItem mnuOpenFolder;
        private ToolStripMenuItem mnuShowTrim;
        private ToolStripMenuItem mnuShowControlPanel;
        private GroupBox grpControlPanel;
        private Label lblWhenEnds;
        private ComboBox cboWhenEnds;
    }
}
