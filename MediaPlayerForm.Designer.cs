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
            menuVideo = new ContextMenuStrip(components);
            videoView = new LibVLCSharp.WinForms.VideoView();
            pnlControls = new Panel();
            btnPlayPause = new IconButton();
            btnStop = new IconButton();
            btnSaveFrame = new IconButton();
            trkSeek = new JumpTrackBar();
            lblTime = new Label();
            btnMute = new IconButton();
            trkVolume = new JumpTrackBar();
            ((System.ComponentModel.ISupportInitialize)videoView).BeginInit();
            pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkSeek).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkVolume).BeginInit();
            SuspendLayout();
            //
            // menuVideo
            //
            menuVideo.Name = "menuVideo";
            menuVideo.Opening += menuVideo_Opening;
            //
            // pnlControls
            //
            pnlControls.BackColor = Color.FromArgb(32, 32, 32);
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
            // btnPlayPause
            //
            btnPlayPause.BackColor = Color.FromArgb(48, 48, 48);
            btnPlayPause.ForeColor = Color.FromArgb(230, 230, 230);
            btnPlayPause.Icon = IconKind.Play;
            btnPlayPause.Location = new Point(12, 12);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(40, 32);
            btnPlayPause.TabIndex = 0;
            btnPlayPause.Click += btnPlayPause_Click;
            //
            // btnStop
            //
            btnStop.BackColor = Color.FromArgb(48, 48, 48);
            btnStop.ForeColor = Color.FromArgb(230, 230, 230);
            btnStop.Icon = IconKind.Stop;
            btnStop.Location = new Point(58, 12);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(40, 32);
            btnStop.TabIndex = 1;
            btnStop.Click += btnStop_Click;
            //
            // btnSaveFrame
            //
            btnSaveFrame.BackColor = Color.FromArgb(48, 48, 48);
            btnSaveFrame.ForeColor = Color.FromArgb(230, 230, 230);
            btnSaveFrame.Icon = IconKind.Camera;
            btnSaveFrame.Location = new Point(104, 12);
            btnSaveFrame.Name = "btnSaveFrame";
            btnSaveFrame.Size = new Size(40, 32);
            btnSaveFrame.TabIndex = 2;
            btnSaveFrame.Click += btnSaveFrame_Click;
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
            lblTime.ForeColor = Color.FromArgb(230, 230, 230);
            lblTime.Location = new Point(844, 12);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(160, 32);
            lblTime.TabIndex = 4;
            lblTime.Text = "0:00 / 0:00";
            lblTime.TextAlign = ContentAlignment.MiddleRight;
            //
            // btnMute
            //
            btnMute.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMute.BackColor = Color.FromArgb(48, 48, 48);
            btnMute.ForeColor = Color.FromArgb(230, 230, 230);
            btnMute.Icon = IconKind.Volume;
            btnMute.Location = new Point(1010, 12);
            btnMute.Name = "btnMute";
            btnMute.Size = new Size(40, 32);
            btnMute.TabIndex = 5;
            btnMute.Click += btnMute_Click;
            //
            // toolTip
            //
            toolTip.SetToolTip(btnPlayPause, "Play (Space)");
            toolTip.SetToolTip(btnStop, "Stop");
            toolTip.SetToolTip(btnSaveFrame, "Save Frame as PNG (S)");
            toolTip.SetToolTip(btnMute, "Mute");
            toolTip.SetToolTip(trkVolume, "Volume: 100%");
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
            trkVolume.Value = 100;
            trkVolume.Scroll += trkVolume_Scroll;
            //
            // videoView
            //
            videoView.BackColor = Color.Black;
            videoView.Dock = DockStyle.Fill;
            videoView.Location = new Point(0, 0);
            videoView.MediaPlayer = null;
            videoView.Name = "videoView";
            videoView.Size = new Size(1178, 698);
            videoView.TabIndex = 0;
            //
            // MediaPlayerForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(32, 32, 32);
            ClientSize = new Size(1178, 698);
            Controls.Add(videoView);
            Controls.Add(pnlControls);
            ForeColor = Color.FromArgb(230, 230, 230);
            Name = "MediaPlayerForm";
            Text = "Media Player";
            ((System.ComponentModel.ISupportInitialize)videoView).EndInit();
            pnlControls.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)trkSeek).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkVolume).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private LibVLCSharp.WinForms.VideoView videoView;
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
    }
}
