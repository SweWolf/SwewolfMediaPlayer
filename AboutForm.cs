// AboutForm.cs: Help > About. Shows the version, a link to the project and, when GitHub can be
// reached, whether a newer version has been released. Modeled on MP3toMP4's AboutForm.
//
// History:
//   2026-10-13  First version.

using System.Diagnostics;

namespace SwewolfMediaPlayer
{
    public partial class AboutForm : Form
    {
        private const string GitHubUrl = "https://github.com/SweWolf/SwewolfMediaPlayer";
        private readonly Version _currentVersion;

        public AboutForm()
        {
            InitializeComponent();
            Icon = Program.LoadIcon();

            var fvi = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
            _currentVersion = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart);
            lblVersion.Text = $"Version {fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}";
            lnkGitHub.Text = GitHubUrl;

            if (Icon != null)
            {
                using var icon = new Icon(Icon, 48, 48);
                picIcon.Image = icon.ToBitmap();
            }
            Shown += AboutForm_Shown;
        }

        private async void AboutForm_Shown(object? sender, EventArgs e)
        {
            var result = await GitHubUpdateChecker.CheckAsync("SweWolf", "SwewolfMediaPlayer", _currentVersion);
            if (result == null || IsDisposed)
            {
                return;   // no network, or the window is already closed
            }

            if (result.IsUpdateAvailable)
            {
                lblUpdateStatus.Text = $"Version {result.LatestVersion} is available";
                lblUpdateStatus.ForeColor = Color.FromArgb(255, 210, 80);
            }
            else
            {
                lblUpdateStatus.Text = "This is the latest version";
                lblUpdateStatus.ForeColor = Color.FromArgb(120, 210, 120);
            }
        }

        private void lnkGitHub_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = GitHubUrl, UseShellExecute = true });
        }
    }
}
