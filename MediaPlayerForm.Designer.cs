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
            videoView = new LibVLCSharp.WinForms.VideoView();
            pnlControls = new Panel();
            btnPlayPause = new Button();
            btnStop = new Button();
            btnSaveFrame = new Button();
            trkSeek = new TrackBar();
            lblTime = new Label();
            btnMute = new Button();
            trkVolume = new TrackBar();
            ((System.ComponentModel.ISupportInitialize)videoView).BeginInit();
            pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkSeek).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkVolume).BeginInit();
            SuspendLayout();
            //
            // pnlControls
            //
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
            btnPlayPause.Location = new Point(12, 12);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(80, 32);
            btnPlayPause.TabIndex = 0;
            btnPlayPause.Text = "Pause";
            btnPlayPause.UseVisualStyleBackColor = true;
            btnPlayPause.Click += btnPlayPause_Click;
            //
            // btnStop
            //
            btnStop.Location = new Point(98, 12);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(70, 32);
            btnStop.TabIndex = 1;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            //
            // btnSaveFrame
            //
            btnSaveFrame.Location = new Point(174, 12);
            btnSaveFrame.Name = "btnSaveFrame";
            btnSaveFrame.Size = new Size(96, 32);
            btnSaveFrame.TabIndex = 2;
            btnSaveFrame.Text = "Save Frame";
            btnSaveFrame.UseVisualStyleBackColor = true;
            btnSaveFrame.Click += btnSaveFrame_Click;
            //
            // trkSeek
            //
            trkSeek.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trkSeek.AutoSize = false;
            trkSeek.Location = new Point(278, 12);
            trkSeek.Maximum = 1000;
            trkSeek.Name = "trkSeek";
            trkSeek.Size = new Size(530, 32);
            trkSeek.TabIndex = 3;
            trkSeek.TickStyle = TickStyle.None;
            trkSeek.Scroll += trkSeek_Scroll;
            trkSeek.MouseDown += trkSeek_MouseDown;
            trkSeek.MouseUp += trkSeek_MouseUp;
            //
            // lblTime
            //
            lblTime.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTime.Location = new Point(814, 12);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(160, 32);
            lblTime.TabIndex = 4;
            lblTime.Text = "0:00 / 0:00";
            lblTime.TextAlign = ContentAlignment.MiddleRight;            //
            // btnMute
            //
            btnMute.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMute.Location = new Point(980, 12);
            btnMute.Name = "btnMute";
            btnMute.Size = new Size(70, 32);
            btnMute.TabIndex = 5;
            btnMute.Text = "Mute";
            btnMute.UseVisualStyleBackColor = true;
            btnMute.Click += btnMute_Click;
            //
            // trkVolume
            //
            trkVolume.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            trkVolume.AutoSize = false;
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
            ClientSize = new Size(1178, 698);
            Controls.Add(videoView);
            Controls.Add(pnlControls);
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
        private Panel pnlControls;
        private Button btnPlayPause;
        private Button btnStop;
        private Button btnSaveFrame;
        private TrackBar trkSeek;
        private Label lblTime;
        private Button btnMute;
        private TrackBar trkVolume;
    }
}