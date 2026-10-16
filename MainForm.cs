using System.Diagnostics;

namespace SwewolfMediaPlayer
{
    public partial class MainForm : Form
    {
        internal const string AppName = "SweWolf Media Player";

        private string? _updateReleasePageUrl;

        // initialPath: a file given on the command line that was not found, shown for correcting.
        public MainForm(string? initialPath = null)
        {
            InitializeComponent();
            Icon = Program.LoadIcon();
            if (!string.IsNullOrEmpty(initialPath))
            {
                txtFile.Text = initialPath;
            }
            UpdateOpenButton();

            // Start loading the video engine now, so the first video opens without a wait.
            _ = MediaPlayerForm.PreloadAsync();
        }

        private void menuCreateShortcut_Click(object? sender, EventArgs e)
        {
            using var form = new CreateShortcutForm(Application.ExecutablePath);
            form.ShowDialog(this);
        }

        private void menuSettings_Click(object? sender, EventArgs e)
        {
            using var form = new SettingsForm();
            form.ShowDialog(this);
        }

        private void menuAbout_Click(object? sender, EventArgs e)
        {
            using var form = new AboutForm();
            form.ShowDialog(this);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (AppSettings.Load().CheckForUpdatesOnStartup)
            {
                await CheckForUpdatesAsync();
            }
        }

        // Runs in the background and fails silently: the menu item only appears if a newer version exists.
        private async Task CheckForUpdatesAsync()
        {
            var fvi = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
            var currentVersion = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart);
            var result = await GitHubUpdateChecker.CheckAsync("SweWolf", "SwewolfMediaPlayer", currentVersion);
            if (result is { IsUpdateAvailable: true } && !IsDisposed)
            {
                _updateReleasePageUrl = result.ReleasePageUrl;
                menuNewVersion.Visible = true;
            }
        }

        private void menuNewVersion_Click(object? sender, EventArgs e)
        {
            if (_updateReleasePageUrl != null)
            {
                Process.Start(new ProcessStartInfo(_updateReleasePageUrl) { UseShellExecute = true });
            }
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
                Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.sid|All Files|*.*"
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

        // A file dragged from Windows Explorer can be dropped anywhere in the window (the form and
        // the text box both accept it); it only fills in the path.
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
