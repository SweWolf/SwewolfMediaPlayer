namespace SwewolfMediaPlayer
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlHeader = new Panel();
            picIcon = new PictureBox();
            lblAppName = new Label();
            lblVersion = new Label();
            lblUpdateStatus = new Label();
            lblDescription = new Label();
            lblCopyright = new Label();
            lnkGitHub = new LinkLabel();
            lblBuiltWithHeader = new Label();
            lblBuiltWith = new Label();
            btnClose = new Button();
            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
            SuspendLayout();
            //
            // pnlHeader
            //
            pnlHeader.BackColor = Color.FromArgb(15, 52, 96);
            pnlHeader.Controls.Add(picIcon);
            pnlHeader.Controls.Add(lblAppName);
            pnlHeader.Controls.Add(lblVersion);
            pnlHeader.Controls.Add(lblUpdateStatus);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(440, 88);
            pnlHeader.TabIndex = 0;
            //
            // picIcon
            //
            picIcon.Location = new Point(20, 16);
            picIcon.Name = "picIcon";
            picIcon.Size = new Size(52, 52);
            picIcon.SizeMode = PictureBoxSizeMode.Zoom;
            picIcon.TabIndex = 0;
            picIcon.TabStop = false;
            //
            // lblAppName
            //
            lblAppName.AutoSize = true;
            lblAppName.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            lblAppName.ForeColor = Color.White;
            lblAppName.Location = new Point(84, 16);
            lblAppName.Name = "lblAppName";
            lblAppName.TabIndex = 1;
            lblAppName.Text = "SweWolf Media Player";
            //
            // lblVersion
            //
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 9F);
            lblVersion.ForeColor = Color.FromArgb(160, 195, 225);
            lblVersion.Location = new Point(86, 54);
            lblVersion.Name = "lblVersion";
            lblVersion.TabIndex = 2;
            lblVersion.Text = "Version 1.0.0";
            //
            // lblUpdateStatus
            //
            lblUpdateStatus.AutoSize = true;
            lblUpdateStatus.Font = new Font("Segoe UI", 8.5F);
            lblUpdateStatus.ForeColor = Color.FromArgb(160, 195, 225);
            lblUpdateStatus.Location = new Point(86, 70);
            lblUpdateStatus.Name = "lblUpdateStatus";
            lblUpdateStatus.TabIndex = 3;
            //
            // lblDescription
            //
            lblDescription.Location = new Point(20, 106);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(400, 36);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "Plays video and audio files, steps through them frame by frame, saves frames and trims files.";
            //
            // lblCopyright
            //
            lblCopyright.AutoSize = true;
            lblCopyright.ForeColor = SystemColors.GrayText;
            lblCopyright.Location = new Point(20, 150);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.TabIndex = 2;
            lblCopyright.Text = "SweWolf Software";
            //
            // lnkGitHub
            //
            lnkGitHub.AutoSize = true;
            lnkGitHub.LinkColor = Color.FromArgb(120, 170, 255);
            lnkGitHub.Location = new Point(20, 172);
            lnkGitHub.Name = "lnkGitHub";
            lnkGitHub.TabIndex = 3;
            lnkGitHub.TabStop = true;
            lnkGitHub.Text = "https://github.com/SweWolf/SwewolfMediaPlayer";
            lnkGitHub.LinkClicked += lnkGitHub_LinkClicked;
            //
            // lblBuiltWithHeader
            //
            lblBuiltWithHeader.AutoSize = true;
            lblBuiltWithHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBuiltWithHeader.Location = new Point(20, 204);
            lblBuiltWithHeader.Name = "lblBuiltWithHeader";
            lblBuiltWithHeader.TabIndex = 4;
            lblBuiltWithHeader.Text = "Built With";
            //
            // lblBuiltWith
            //
            lblBuiltWith.Location = new Point(20, 224);
            lblBuiltWith.Name = "lblBuiltWith";
            lblBuiltWith.Size = new Size(400, 52);
            lblBuiltWith.TabIndex = 5;
            lblBuiltWith.Text = "• .NET 10 / Windows Forms\r\n• LibVLC / LibVLCSharp (playback)\r\n• TagLibSharp (cover art)";
            //
            // btnClose
            //
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Location = new Point(340, 292);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 28);
            btnClose.TabIndex = 6;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            //
            // AboutForm
            //
            AcceptButton = btnClose;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnClose;
            ClientSize = new Size(440, 336);
            Controls.Add(btnClose);
            Controls.Add(lblBuiltWith);
            Controls.Add(lblBuiltWithHeader);
            Controls.Add(lnkGitHub);
            Controls.Add(lblCopyright);
            Controls.Add(lblDescription);
            Controls.Add(pnlHeader);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "About SweWolf Media Player";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Panel pnlHeader;
        private PictureBox picIcon;
        private Label lblAppName;
        private Label lblVersion;
        private Label lblUpdateStatus;
        private Label lblDescription;
        private Label lblCopyright;
        private LinkLabel lnkGitHub;
        private Label lblBuiltWithHeader;
        private Label lblBuiltWith;
        private Button btnClose;
    }
}
