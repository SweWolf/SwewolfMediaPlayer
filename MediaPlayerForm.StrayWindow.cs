// MediaPlayerForm.StrayWindow.cs: a safeguard for the video window. LibVLC draws the picture inside the video area of this window
// (the Hwnd of the player). Sometimes, rarely, a channel opened in a window of its own instead: a separate "VLC (Direct3D11 output)"
// window that does not even close with its X. It was seen (2026-11-26) right after the log said "STUCK: MediaPlayer.Stop did not finish
// ... a new player takes over" (see StopPlayerTask), so the new player may not have got the video area. Two things are done:
//  - Before every Play the player is given the video area again (EnsureVideoWindow), and the log says if it had lost it.
//  - A few seconds after a stream starts we look for such a window of this program. If there is one it is written to the log, and the
//    stream is opened once more the way a click on the channel does (the old window goes away with its player).
//
// History:
//   2026-11-26  First version.

using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using System.Text;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);

        private int _strayChecks;   // how often a stray window made a restart for the file that is open now (reset when a channel is clicked)

        // Gives the player the video area again before it plays, if it does not have it (it should always have it).
        private void EnsureVideoWindow()
        {
            if (_player == null || !videoView.IsHandleCreated)
            {
                return;
            }
            try
            {
                if (_player.Hwnd != videoView.Handle)
                {
                    OpenTimingLog.Write("VIDEO WINDOW: the player had lost the video area, it is given again");
                    _player.Hwnd = videoView.Handle;
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // A player in a bad state: nothing more can be done here.
            }
        }

        // The title of a top-level window of this program that is a LibVLC video window of its own, or null.
        private static string? FindStrayVideoWindow()
        {
            string? found = null;
            uint me = (uint)Environment.ProcessId;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid != me || !IsWindowVisible(hWnd))
                {
                    return true;
                }
                var title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);
                if (title.ToString().StartsWith("VLC (", StringComparison.Ordinal))
                {
                    found = title.ToString();
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        // Called when a stream has been started: after 3 and 8 seconds, is there a video window of its own?
        private async void WatchForStrayVideoWindow(MediaPlayer started)
        {
            foreach (int wait in new[] { 3000, 5000 })
            {
                await Task.Delay(wait);
                if (IsDisposed || !ReferenceEquals(started, _player) || !_isStream)
                {
                    return;
                }
                string? stray = await Task.Run(FindStrayVideoWindow);
                if (stray == null)
                {
                    continue;
                }
                OpenTimingLog.Write($"VIDEO WINDOW: \"{stray}\" is open as a window of its own (restarts so far: {_strayChecks})");
                if (_strayChecks++ >= 1 || string.IsNullOrEmpty(_filePath) || PlayerBusy)
                {
                    return;   // once is enough: a second try would only repeat itself
                }
                await OpenAnotherAsync(_filePath, _libraryPath, false);
                return;
            }
        }
    }
}
