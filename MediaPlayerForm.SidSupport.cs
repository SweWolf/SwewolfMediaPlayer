// MediaPlayerForm.SidSupport.cs: sets up SID (Commodore 64 music) playback the first time a .sid
// file is opened. The plugin comes from the user's own VLC (see SidPluginSetup.cs). While it is
// copied and LibVLC is started again a small window with a moving bar shows that the program is
// working and not frozen.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        // Makes sure LibVLC can play the current file. Returns false when it cannot (the reason has
        // been shown). canReload is false when this window already has a player: LibVLC can then
        // not be restarted, and the file has to be opened again once the plugin is in place.
        private async Task<bool> EnsureSidSupportAsync(bool canReload)
        {
            if (_isStream || !SidPluginSetup.IsSidFile(_filePath) || SidPluginSetup.IsInstalled)
            {
                return true;
            }

            // A restart of LibVLC would stop the other windows too.
            bool othersPlaying = Application.OpenForms.OfType<MediaPlayerForm>().Any(f => f != this && f._player != null);
            string version = _libVlc?.Version ?? "";

            using var progress = new SidSetupProgressForm();
            progress.Show(this);
            string? error;
            try
            {
                error = await Task.Run(() => SidPluginSetup.TryInstall(version));
                if (error == null && canReload && !othersPlaying)
                {
                    await ReloadLibVlcAsync();
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                progress.Close();
            }

            if (IsDisposed)
            {
                return false;
            }

            if (error != null)
            {
                DialogResult answer = MessageBox.Show(this, SidPluginSetup.ManualInstructions(error), AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer == DialogResult.Yes)
                {
                    SidPluginSetup.OpenPluginFolder();
                }
                return false;
            }

            if (!canReload || othersPlaying)
            {
                MessageBox.Show(this, "SID support is now set up. Close all player windows and open the file again.", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        // LibVLC reads its plugin list once, when it starts, so it is started again after the
        // plugin was copied. The old instance has to be disposed first: while it is alive a new one
        // shares its plugin list.
        private static Task ReloadLibVlcAsync()
        {
            return _loadTask = Task.Run(() =>
            {
                LibVLC? old = _libVlc;
                _libVlc = null;
                old?.Dispose();
                _libVlc = new LibVLC();
            });
        }
    }

    // A small window with a moving bar, shown while the SID plugin is set up.
    internal sealed class SidSetupProgressForm : Form
    {
        public SidSetupProgressForm()
        {
            Text = MainForm.AppName;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
            };
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Setting up SID support. This takes a few seconds.",
                Margin = new Padding(0, 0, 0, 12),
            });
            layout.Controls.Add(new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
            });
            Controls.Add(layout);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
    }
}
