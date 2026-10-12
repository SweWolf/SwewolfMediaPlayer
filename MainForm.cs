namespace SwewolfMediaPlayer
{
    public partial class MainForm : Form
    {
        private const string AppName = "SweWolf Media Player";

        public MainForm()
        {
            InitializeComponent();
            // The icon is built into the program, so it also works when only the EXE is shipped.
            using Stream? iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("SwewolfMediaPlayer.ico");
            if (iconStream != null)
            {
                Icon = new Icon(iconStream);
            }
            UpdateOpenButton();

            // Start loading the video engine now, so the first video opens without a wait.
            _ = MediaPlayerForm.PreloadAsync();
        }

        private void txtFile_TextChanged(object? sender, EventArgs e)
        {
            UpdateOpenButton();

            // Start loading the video engine now, so the first video opens without a wait.
            _ = MediaPlayerForm.PreloadAsync();
        }

        private void UpdateOpenButton()
        {
            btnOpen.Enabled = txtFile.Text.Trim().Length > 0;
        }

        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select a Media File",
                Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mp3;*.wav;*.flac;*.m4a;*.ogg|All Files|*.*"
            };

            if (File.Exists(txtFile.Text))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(txtFile.Text));
                dialog.FileName = Path.GetFileName(txtFile.Text);
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                txtFile.Text = dialog.FileName;
                // The user has chosen the file, so there is no need to click Open as well.
                btnOpen.PerformClick();
            }
        }

        // The text box accepts a file dragged from Windows Explorer; it only fills in the path.
        private void txtFile_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void txtFile_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            {
                txtFile.Text = files[0];
                txtFile.SelectionStart = txtFile.TextLength;
            }
        }

        private void btnOpen_Click(object? sender, EventArgs e)
        {
            string path = txtFile.Text.Trim().Trim('"');

            // A web address (a stream) is not checked: only files must exist.
            if (!MediaPlayerForm.TryGetStreamUri(path, out _) && !File.Exists(path))
            {
                MessageBox.Show(this, $"The file \"{path}\" does not exist.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFile.Focus();
                return;
            }

            new MediaPlayerForm(path) { Icon = Icon }.Show(this);
        }
    }
}
