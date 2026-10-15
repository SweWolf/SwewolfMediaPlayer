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
            menuTools = new ToolStripMenuItem();
            menuCreateShortcut = new ToolStripMenuItem();
            menuSettings = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuAbout = new ToolStripMenuItem();
            menuNewVersion = new ToolStripMenuItem();
            lblFile = new Label();
            txtFile = new TextBox();
            btnBrowse = new Button();
            btnOpen = new Button();
            menuStrip.SuspendLayout();
            SuspendLayout();
            //
            // menuStrip
            //
            menuStrip.Items.AddRange(new ToolStripItem[] { menuTools, menuHelp, menuNewVersion });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(754, 24);
            menuStrip.TabIndex = 4;
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
            // menuHelp
            //
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuAbout });
            menuHelp.Name = "menuHelp";
            menuHelp.Size = new Size(44, 20);
            menuHelp.Text = "Help";
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
        private ToolStripMenuItem menuTools;
        private ToolStripMenuItem menuCreateShortcut;
        private ToolStripMenuItem menuSettings;
        private ToolStripMenuItem menuNewVersion;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuAbout;
    }
}
