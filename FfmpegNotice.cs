// FfmpegNotice.cs: a one-time message at start-up when FFmpeg is not installed. The player works without it,
// but the thumbnail previews on the seek bar and Save Frame at the very end of a video need it, and without
// a message nothing explains why they do nothing (those features themselves stay silent on purpose).
// It is shown once only (AppSettings.FfmpegNoticeShown): after that it is up to the user.

using System.Diagnostics;

namespace SwewolfMediaPlayer
{
    internal static class FfmpegNotice
    {
        private const string AppName = "SweWolf Media Player";
        private const string DownloadPage = "https://ffmpeg.org/download.html";

        public static void ShowOnce()
        {
            try
            {
                AppSettings settings = AppSettings.Load();
                if (settings.FfmpegNoticeShown || MediaPlayerForm.FindFfmpeg() != null)
                {
                    return;
                }

                // Remembered before the message is shown: a crash or a killed process must not make it come back every time.
                settings.FfmpegNoticeShown = true;
                settings.Save();

                DialogResult answer = MessageBox.Show(
                    "FFmpeg was not found on this computer.\n\n"
                    + "The player works without it, but these features need it:\n"
                    + "  • the thumbnail previews on the seek bar\n"
                    + "  • saving the very last frame of a video\n"
                    + "  • recording streams\n\n"
                    + "To use them, put ffmpeg.exe in the program folder or in a folder in the PATH.\n\n"
                    + "This message is shown only once. Do you want to open the FFmpeg download page now?",
                    AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                if (answer == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(DownloadPage) { UseShellExecute = true });
                }
            }
            catch (Exception)
            {
                // A notice is never worth an error.
            }
        }
    }
}
