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
//   2026-10-21  The play command's StartMs starts a file at that time (Demo Play); --start-at and --no-resume do the same for a file on the command line.
//   2026-10-21  While the explorer runs a playlist the two buttons next to the seek bar (the tune buttons of SID files) skip tracks: they send "skip"
//   2026-11-19  TrackSkipping is also true while a playlist of this player runs; SkipTrack then moves in that playlist (the explorer's playlist is unchanged).
//               (direction next / previous) and the explorer decides what plays; Page Up / Page Down (without tunes) and the media keys do the same.

using System.Runtime.InteropServices;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private PlayerPipe? _pipe;
        private bool _controlled;                         // a playlist of the explorer is running
        private bool _quickSettingsOnce;                  // the next StartMedia follows the quick settings (see UseQuickSettingsOnce)
        private bool _startPausedPending;                 // the file stops on its first picture as soon as it plays
        private bool _noResumeOnce;                       // the next StartLibrary does not offer or use a saved position
        private long _startAtOnceMs;                      // the next StartLibrary starts the file at this time (the explorer's Demo Play)
        private PipeCommand? _pendingPlay;                // a play command that came before the video engine was ready

        /// <summary>The first file of a player the explorer started (command line --start-at / --no-resume).</summary>
        internal void SetFirstFileOptions(long startMs, bool noResume)
        {
            _startAtOnceMs = startMs;
            _noResumeOnce = noResume;
        }

        // The two quick settings of the main window's Media menu (Play Media Files Automatically, Always Play from Start) apply to the next file
        // that is opened by hand: from that menu, or dropped on this window. A file that arrives with arguments or through the pipe (the explorer)
        // never uses them: it always plays and always asks about a saved position.
        internal void UseQuickSettingsOnce()
        {
            _quickSettingsOnce = true;
        }

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
                try { if (!IsDisposed) BeginInvoke(() => SetControlled(false)); }
                catch (InvalidOperationException) { }
            };
        }

        /// <summary>True while the explorer runs a playlist in this window: the two buttons skip tracks instead of tunes.</summary>
        internal bool TrackSkipping => (_controlled && _pipe != null) || _playlistMode;

        private void SetControlled(bool value)
        {
            if (_controlled == value) return;
            _controlled = value;
            RefreshNavButtons();
        }

        /// <summary>A button, Page Up / Page Down or a media key: the explorer is asked for the previous or next track (it decides what plays).</summary>
        private void SkipTrack(int direction)
        {
            if (_playlistMode)
            {
                PlaylistSkip(direction);   // the playlist of this player (MediaPlayerForm.Playlist.cs)
                return;
            }
            _pipe?.Send(new { @event = "skip", direction = direction > 0 ? "next" : "previous" });
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
                    SetControlled(false);
                    break;

                case "control":   // the explorer started this player with a file and now runs a playlist: the end of the file is only reported
                    SetControlled(true);
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

            UseCloseWhenEnds();   // a file handed over by another program: close at the end, not saved
            SetControlled(command.Playlist);
            _noResumeOnce = command.NoResume;   // the explorer says: a file that follows another one in a playlist starts from the beginning
            _startAtOnceMs = command.StartMs;   // Demo Play
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
            // SW_RESTORE gives a minimized window back in the state it had before (a maximized window stays maximized);
            // WindowState = Normal would lose that.
            if (WindowState == FormWindowState.Minimized)
            {
                ShowWindow(Handle, 9);
                // Make sure it is the state it had before it was minimized (maximized stays maximized).
                if (WindowState != _lastShownState && WindowState != FormWindowState.Minimized) WindowState = _lastShownState;
            }
            bool wasTop = TopMost;
            TopMost = true;
            TopMost = wasTop;
            Activate();
            SetForegroundWindow(Handle);
        }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmd);

        /// <summary>The last state the window had while it was not minimized (Normal or Maximized): what Show Media Player brings it back to.</summary>
        private FormWindowState _lastShownState = FormWindowState.Normal;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState != FormWindowState.Minimized) _lastShownState = WindowState;
        }

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
