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
            txtFile = new TextBox();
            btnBrowse = new Button();
            btnOpen = new Button();
            SuspendLayout();
            // 
            // txtFile
            // 
            txtFile.Font = new Font("Segoe UI", 12F);
            txtFile.Location = new Point(31, 38);
            txtFile.Name = "txtFile";
            txtFile.Size = new Size(627, 29);
            txtFile.TabIndex = 0;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(679, 38);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(34, 29);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // btnOpen
            // 
            btnOpen.Font = new Font("Segoe UI", 12F);
            btnOpen.Location = new Point(476, 91);
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new Size(182, 45);
            btnOpen.TabIndex = 2;
            btnOpen.Text = "Open";
            btnOpen.UseVisualStyleBackColor = true;
            btnOpen.Click += btnOpen_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 171);
            Controls.Add(btnOpen);
            Controls.Add(btnBrowse);
            Controls.Add(txtFile);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SweWolf Media Player";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtFile;
        private Button btnBrowse;
        private Button btnOpen;
    }
}
