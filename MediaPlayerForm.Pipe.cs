// MediaPlayerForm.Pipe.cs: the player takes commands from SweWolf File Explorer through PlayerPipe.cs.
//
// - "play": plays another file in this window (the same as dropping a file on it). With "playlist" the explorer runs a playlist: the end of a file
//   is then only reported ("ended") and the explorer sends the next one, so the end actions of the Control Panel (close, repeat) are not used.
//   Resume: only when the explorer does not say noResume (the first file of a list the user opened); the list itself always starts from the beginning.
// - "pause", "resume", "front" (bring the window to the front), "release" (the explorer's playlist is over: the Control Panel decides again), "close".
// - Events: "ended" (a file played to its end), "closed" (the window closes), "error".
//
// History:
//   2026-10-20  First version.

using System.Runtime.InteropServices;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private PlayerPipe? _pipe;
        private bool _controlled;                         // a playlist of the explorer is running
        private bool _noResumeOnce;                       // the next StartLibrary does not offer or use a saved position
        private PipeCommand? _pendingPlay;                // a play command that came before the video engine was ready

        internal void AttachPipe(PlayerPipe? pipe)
        {
            _pipe = pipe;
            if (pipe == null) return;

            pipe.CommandReceived += command =>
            {
                if (IsDisposed) return;
                try { BeginInvoke(() => HandlePipeCommand(command)); }
                catch (InvalidOperationException)
                {
                    // The window has no handle yet: the command waits (only the last play counts) and is done when the engine is ready.
                    if (command.Cmd == "play") _pendingPlay = command;
                }
            };
            pipe.ClientDisconnected += () =>
            {
                try { if (!IsDisposed) BeginInvoke(() => _controlled = false); }
                catch (InvalidOperationException) { }
            };
        }

        /// <summary>Called when the engine is ready: a play command that arrived earlier is done now.</summary>
        private void ProcessPendingPipeCommands()
        {
            var pending = _pendingPlay;
            _pendingPlay = null;
            if (pending != null) HandlePipeCommand(pending);
        }

        private void HandlePipeCommand(PipeCommand command)
        {
            switch (command.Cmd)
            {
                case "play":
                    _ = PlayFromPipeAsync(command);
                    break;

                case "pause":
                    if (_player != null && !_paused && !_restoringEnd) TogglePlayPause();
                    break;

                case "resume":
                    if (_player != null && _paused && !_restoringEnd) TogglePlayPause();
                    break;

                case "release":
                    _controlled = false;
                    break;

                case "control":   // the explorer started this player with a file and now runs a playlist: the end of the file is only reported
                    _controlled = true;
                    break;

                case "front":
                    BringToFrontNow();
                    break;

                case "close":
                    Close();
                    break;
            }
        }

        private async Task PlayFromPipeAsync(PipeCommand command)
        {
            string? path = command.Path;
            if (string.IsNullOrEmpty(path) || !(TryGetStreamUri(path, out _) || File.Exists(path)))
            {
                _pipe?.Send(new { @event = "error", path, message = "The file was not found." });
                return;
            }

            if (_player == null)
            {
                _pendingPlay = command;   // the engine is still loading
                return;
            }

            _controlled = command.Playlist;
            _noResumeOnce = command.NoResume;   // the explorer says: a file that follows another one in a playlist starts from the beginning
            await OpenAnotherAsync(path, command.LibraryPath ?? path);
            if (command.Front) BringToFrontNow();
        }

        /// <summary>
        /// Brings the window to the front, also over the program that has the focus (the explorer): Windows allows a program to take the
        /// focus only in some cases, so the window is made topmost for a moment as well.
        /// </summary>
        private void BringToFrontNow()
        {
            if (IsDisposed) return;
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            bool wasTop = TopMost;
            TopMost = true;
            TopMost = wasTop;
            Activate();
            SetForegroundWindow(Handle);
        }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>A file played to its end: the explorer is told. True if the explorer runs a playlist (the end actions are not used then).</summary>
        private bool ReportEnded()
        {
            _pipe?.Send(new { @event = "ended", path = _filePath });
            return _controlled && _pipe != null;
        }

        private void ReportClosed()
        {
            _pipe?.Send(new { @event = "closed", path = _filePath });
        }
    }
}
