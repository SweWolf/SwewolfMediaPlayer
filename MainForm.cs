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
            RefreshRecordingsMenu();   // there only when a recording has been made
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

        // The public channel list of the iptv-org project. For now it is the one list in the menu. It opens like an address typed in the
        // box and opened, but the box is left as it is (the list is kept on disk and checked for changes, see M3uPlaylist.cs).
        private const string IptvPlaylistUrl = "https://iptv-org.github.io/iptv/index.m3u";

        private void menuOpenPlaylist_Click(object? sender, EventArgs e)
        {
            OpenMedia(IptvPlaylistUrl);
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

        // The folder last browsed in this run of the program (until it is closed, the dialog starts here).
        private string? _browsedFolder;

        // The folder when the text names a full path that exists; otherwise null (blank, relative, or not there).
        private static string? ExistingFolder(string? text)
        {
            string folder = (text ?? "").Trim().Trim('"');
            return folder.Length > 0 && Path.IsPathRooted(folder) && Directory.Exists(folder) ? folder : null;
        }

        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select a Media File",
                Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mp3;*.wav;*.flac;*.m4a;*.ogg;*.sid;*.gif;*.m3u;*.m3u8|All Files|*.*"
            };

            // Where the dialog starts: the folder of the file in the box, or the folder typed there; else the folder last browsed in this run; else the
            // default folder of the settings (when it exists); else the last folder used in an earlier run.
            AppSettings settings = AppSettings.Load();
            string typed = txtFile.Text.Trim().Trim('"');
            if (File.Exists(typed))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(typed));
                dialog.FileName = Path.GetFileName(typed);
            }
            else if (ExistingFolder(typed) is { } typedFolder)
            {
                dialog.InitialDirectory = typedFolder;   // a folder typed in the box (with or without a closing backslash)
            }
            else if (_browsedFolder != null && Directory.Exists(_browsedFolder))
            {
                dialog.InitialDirectory = _browsedFolder;
            }
            else if (ExistingFolder(settings.MediaStartFolder) is { } startFolder)
            {
                dialog.InitialDirectory = startFolder;
            }
            else if (ExistingFolder(settings.LastMediaFolder) is { } lastFolder)
            {
                dialog.InitialDirectory = lastFolder;
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _browsedFolder = Path.GetDirectoryName(dialog.FileName);
                if (_browsedFolder != null && !string.Equals(settings.LastMediaFolder, _browsedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    AppSettings latest = AppSettings.Load();   // from a fresh copy, so nothing else in the file is overwritten
                    latest.LastMediaFolder = _browsedFolder;
                    latest.Save();
                }
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
            OpenMedia(txtFile.Text.Trim().Trim('"'));
        }

        // Opens a file or web address in a player window. Used by Open and by the IPTV menu (which leaves the box alone).
        private void OpenMedia(string path)
        {
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
