namespace SwewolfMediaPlayer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip = new ToolTip(components);
            menuStrip = new MenuStrip();
            menuMedia = new ToolStripMenuItem();
            menuOpenDvd = new ToolStripMenuItem();
            menuRecent = new ToolStripMenuItem();
            menuSepQuick = new ToolStripSeparator();
            menuPlayAutomatically = new ToolStripMenuItem();
            menuAlwaysFromStart = new ToolStripMenuItem();
            menuTools = new ToolStripMenuItem();
            menuCreateShortcut = new ToolStripMenuItem();
            menuSettings = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuAbout = new ToolStripMenuItem();
            menuViewHelp = new ToolStripMenuItem();
            menuHelpSeparator = new ToolStripSeparator();
            menuNewVersion = new ToolStripMenuItem();
            menuIptv = new ToolStripMenuItem();
            menuOpenPlaylist = new ToolStripMenuItem();
            menuCheckChannels = new ToolStripMenuItem();
            menuResetList = new ToolStripMenuItem();
            menuMyRecordings = new ToolStripMenuItem();
            menuUnfinished = new ToolStripMenuItem();
            lblFile = new Label();
            txtFile = new TextBox();
            btnBrowse = new Button();
            btnOpen = new Button();
            menuStrip.SuspendLayout();
            SuspendLayout();
            //
            // menuStrip
            //
            menuStrip.Items.AddRange(new ToolStripItem[] { menuTools, menuMedia, menuIptv, menuMyRecordings, menuUnfinished, menuHelp, menuNewVersion });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(754, 24);
            menuStrip.TabIndex = 4;
            //
            // menuMedia
            //
            menuMedia.DropDownItems.AddRange(new ToolStripItem[] { menuOpenDvd, menuRecent, menuSepQuick, menuPlayAutomatically, menuAlwaysFromStart });
            menuMedia.Name = "menuMedia";
            menuMedia.Size = new Size(52, 20);
            menuMedia.Text = "Media";
            //
            // menuOpenDvd
            //
            menuOpenDvd.Name = "menuOpenDvd";
            menuOpenDvd.Size = new Size(180, 22);
            menuOpenDvd.Text = "Open DVD/CD";
            menuOpenDvd.ToolTipText = "Play the DVD in the drive, with its menus, or the audio CD from the first track to the last.";
            menuOpenDvd.Click += menuOpenDvd_Click;
            //
            // menuRecent (its items are made in code: MainForm.RefreshRecentMenu)
            //
            menuRecent.Name = "menuRecent";
            menuRecent.Size = new Size(180, 22);
            menuRecent.Text = "Recent Media Files";
            menuRecent.ToolTipText = "The media files you opened most recently.";
            //
            // menuSepQuick
            //
            menuSepQuick.Name = "menuSepQuick";
            menuSepQuick.Size = new Size(177, 6);
            //
            // menuPlayAutomatically
            //
            menuPlayAutomatically.CheckOnClick = true;
            menuPlayAutomatically.Name = "menuPlayAutomatically";
            menuPlayAutomatically.Size = new Size(180, 22);
            menuPlayAutomatically.Text = "Play Media Files Automatically";
            menuPlayAutomatically.ToolTipText = "A file you open here starts to play by itself. Without the check mark it stops on its first picture until you press Play (handy for trimming).\r\nThis setting does not apply when a file is started from another program or with command-line arguments. Such a file always plays, unless you add --start-paused.";
            menuPlayAutomatically.Click += menuPlayAutomatically_Click;
            //
            // menuAlwaysFromStart
            //
            menuAlwaysFromStart.CheckOnClick = true;
            menuAlwaysFromStart.Name = "menuAlwaysFromStart";
            menuAlwaysFromStart.Size = new Size(180, 22);
            menuAlwaysFromStart.Text = "Always Play from Start";
            menuAlwaysFromStart.ToolTipText = "A file you open here always starts from the beginning, without the question about continuing where you stopped. Where you stop is still remembered. A file started from another program, or with arguments, still asks.";
            menuAlwaysFromStart.Click += menuAlwaysFromStart_Click;
            //
            // menuTools
            //
            menuTools.DropDownItems.AddRange(new ToolStripItem[] { menuCreateShortcut, menuSettings });
            menuTools.Name = "menuTools";
            menuTools.Size = new Size(47, 20);
            menuTools.Text = "Tools";
            //
            // menuCreateShortcut
            //
            menuCreateShortcut.Name = "menuCreateShortcut";
            menuCreateShortcut.Size = new Size(180, 22);
            menuCreateShortcut.Text = "Create Shortcut...";
            menuCreateShortcut.Click += menuCreateShortcut_Click;
            //
            // menuSettings
            //
            menuSettings.Name = "menuSettings";
            menuSettings.Size = new Size(180, 22);
            menuSettings.Text = "Settings";
            menuSettings.Click += menuSettings_Click;
            //
            // menuIptv
            //
            menuIptv.DropDownItems.AddRange(new ToolStripItem[] { menuOpenPlaylist, menuCheckChannels, menuResetList });
            menuIptv.Name = "menuIptv";
            menuIptv.Size = new Size(41, 20);
            menuIptv.Text = "IPTV";
            //
            // menuOpenPlaylist
            //
            menuOpenPlaylist.Name = "menuOpenPlaylist";
            menuOpenPlaylist.Size = new Size(180, 22);
            menuOpenPlaylist.Text = "Open Playlist";
            menuOpenPlaylist.ToolTipText = "Open the public IPTV channel list (iptv-org).";
            menuOpenPlaylist.Click += menuOpenPlaylist_Click;
            //
            // menuCheckChannels
            //
            menuCheckChannels.Name = "menuCheckChannels";
            menuCheckChannels.Size = new Size(180, 22);
            menuCheckChannels.Text = "Check Channels";
            menuCheckChannels.ToolTipText = "Find out which channels of the public IPTV list answer, in the background. The result is shown when you point at a channel.";
            menuCheckChannels.Click += menuCheckChannels_Click;
            //
            // menuResetList
            //
            menuResetList.Name = "menuResetList";
            menuResetList.Size = new Size(180, 22);
            menuResetList.Text = "Reset Channel List...";
            menuResetList.ToolTipText = "Forget the channels that were removed from the public IPTV list and load the list again, as it is on the web now.";
            menuResetList.Click += menuResetList_Click;
            //
            // menuMyRecordings
            //
            menuMyRecordings.Name = "menuMyRecordings";
            menuMyRecordings.Size = new Size(95, 20);
            menuMyRecordings.Text = "My Recordings";
            menuMyRecordings.Visible = false;
            menuMyRecordings.DropDownOpening += menuMyRecordings_DropDownOpening;
            // 
            // menuUnfinished
            // 
            menuUnfinished.Name = "menuUnfinished";
            menuUnfinished.Size = new Size(130, 20);
            menuUnfinished.Text = "Unfinished Recordings";
            menuUnfinished.Visible = false;
            //
            // menuHelp
            //
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuViewHelp, menuHelpSeparator, menuAbout });
            menuHelp.Name = "menuHelp";
            menuHelp.Size = new Size(44, 20);
            menuHelp.Text = "Help";
            //
            // menuViewHelp
            //
            menuViewHelp.Name = "menuViewHelp";
            menuViewHelp.ShortcutKeys = Keys.F1;
            menuViewHelp.Size = new Size(180, 22);
            menuViewHelp.Text = "SweWolf Media Player Help";
            menuViewHelp.Click += menuViewHelp_Click;
            //
            // menuHelpSeparator
            //
            menuHelpSeparator.Name = "menuHelpSeparator";
            menuHelpSeparator.Size = new Size(177, 6);
            //
            // menuAbout
            //
            menuAbout.Name = "menuAbout";
            menuAbout.Size = new Size(180, 22);
            menuAbout.Text = "About";
            menuAbout.Click += menuAbout_Click;
            //
            // menuNewVersion
            //
            menuNewVersion.Alignment = ToolStripItemAlignment.Right;
            menuNewVersion.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            menuNewVersion.ForeColor = Color.FromArgb(120, 210, 120);
            menuNewVersion.Name = "menuNewVersion";
            menuNewVersion.Size = new Size(142, 20);
            menuNewVersion.Text = "New Version Available";
            menuNewVersion.Visible = false;
            menuNewVersion.Click += menuNewVersion_Click;
            //
            // lblFile
            //
            lblFile.AutoSize = true;
            lblFile.Location = new Point(12, 43);
            lblFile.Name = "lblFile";
            lblFile.Size = new Size(64, 15);
            lblFile.TabIndex = 0;
            lblFile.Text = "&Media File";
            //
            // txtFile
            //
            txtFile.AllowDrop = true;
            txtFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFile.Font = new Font("Segoe UI", 12F);
            txtFile.Location = new Point(12, 64);
            txtFile.Name = "txtFile";
            txtFile.Size = new Size(673, 29);
            txtFile.TabIndex = 1;
            txtFile.TextChanged += txtFile_TextChanged;
            txtFile.DragEnter += txtFile_DragEnter;
            txtFile.DragDrop += txtFile_DragDrop;
            // 
            // btnBrowse
            // 
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowse.Location = new Point(706, 64);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(34, 29);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "...";
            toolTip.SetToolTip(btnBrowse, "Browse for a media file");
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // btnOpen
            // 
            btnOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpen.Font = new Font("Segoe UI", 12F);
            btnOpen.Location = new Point(503, 117);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(182, 45);
            btnOpen.TabIndex = 3;
            btnOpen.Text = "Open";
            btnOpen.UseVisualStyleBackColor = true;
            btnOpen.Click += btnOpen_Click;
            // 
            // MainForm
            // 
            AcceptButton = btnOpen;
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(754, 195);
            Controls.Add(btnOpen);
            Controls.Add(btnBrowse);
            Controls.Add(txtFile);
            Controls.Add(lblFile);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SweWolf Media Player";
            DragEnter += txtFile_DragEnter;
            DragDrop += txtFile_DragDrop;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolTip toolTip;
        private Label lblFile;
        private TextBox txtFile;
        private Button btnBrowse;
        private Button btnOpen;
        private MenuStrip menuStrip;
        private ToolStripMenuItem menuMedia;
        private ToolStripMenuItem menuRecent;
        private ToolStripSeparator menuSepQuick;
        private ToolStripMenuItem menuPlayAutomatically;
        private ToolStripMenuItem menuAlwaysFromStart;
        private ToolStripMenuItem menuOpenDvd;
        private ToolStripMenuItem menuTools;
        private ToolStripMenuItem menuCreateShortcut;
        private ToolStripMenuItem menuSettings;
        private ToolStripMenuItem menuNewVersion;
        private ToolStripMenuItem menuIptv;
        private ToolStripMenuItem menuOpenPlaylist;
        private ToolStripMenuItem menuCheckChannels;
        private ToolStripMenuItem menuResetList;
        private ToolStripMenuItem menuMyRecordings;
        private ToolStripMenuItem menuUnfinished;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuAbout;
        private ToolStripMenuItem menuViewHelp;
        private ToolStripSeparator menuHelpSeparator;
    }
}
