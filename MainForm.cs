namespace SwewolfMediaPlayer
{
    public partial class MainForm : Form
    {
        private const string AppName = "SweWolf Media Player";

        public MainForm()
        {
            InitializeComponent();
            UpdateOpenButton();
        }

        private void txtFile_TextChanged(object? sender, EventArgs e)
        {
            UpdateOpenButton();
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
            }
        }

        private void btnOpen_Click(object? sender, EventArgs e)
        {
            string path = txtFile.Text.Trim().Trim('"');

            if (!File.Exists(path))
            {
                MessageBox.Show(this, $"The file \"{path}\" does not exist.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFile.Focus();
                return;
            }

            new MediaPlayerForm(path).Show(this);
        }
    }
}
