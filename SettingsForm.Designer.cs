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
            grpUpdate = new GroupBox();
            cboNewVersionCheck = new ComboBox();
            lblNewVersionCheck = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            grpPlayer.SuspendLayout();
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
            grpPlayer.Location = new Point(12, 12);
            grpPlayer.Name = "grpPlayer";
            grpPlayer.Size = new Size(360, 58);
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
            // grpUpdate
            //
            grpUpdate.Controls.Add(cboNewVersionCheck);
            grpUpdate.Controls.Add(lblNewVersionCheck);
            grpUpdate.Location = new Point(12, 82);
            grpUpdate.Name = "grpUpdate";
            grpUpdate.Size = new Size(360, 90);
            grpUpdate.TabIndex = 1;
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
            btnOK.Location = new Point(216, 190);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 27);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            //
            // btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(297, 190);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 27);
            btnCancel.TabIndex = 3;
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
            ClientSize = new Size(384, 229);
            ControlBox = false;
            Controls.Add(grpPlayer);
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
            grpUpdate.ResumeLayout(false);
            grpUpdate.PerformLayout();
            ResumeLayout(false);
        }

        private ToolTip toolTip;
        private GroupBox grpPlayer;
        private CheckBox chkStartFullScreen;
        private GroupBox grpUpdate;
        private ComboBox cboNewVersionCheck;
        private Label lblNewVersionCheck;
        private Button btnOK;
        private Button btnCancel;
    }
}
