// MediaPlayerForm.Library.cs: the shared library of SweWolf File Explorer and SweWolf Media Player (LibraryStore.cs).
//
// - A video file counts as read (the explorer then shows it as read) after it has played for 60 seconds, or to its end.
//   An audio file counts as read as soon as it plays.
//   A file that is 60 seconds long or shorter counts as read as soon as it starts to play (it has no resume point).
// - From then on the position is saved about every 10 seconds and when the video is paused or the window closes, so the
//   next time the player offers to resume. The setting "Save Resume Point" says which files get a position.
// - The file is remembered under _libraryPath: the path of the file, or, for a file the explorer unpacked from an archive
//   to play it, the path inside the archive (--library-path on the command line).
//
// Played time is real time while the video plays: paused time and dragging the seek bar do not count, and jumping ahead
// adds nothing.
//
// History:
//   2026-10-16  First version.
//   2026-10-16  A file of 60 seconds or less is read as soon as it plays; no position is saved for it.
//   2026-10-16  Resume = No also stops saving positions (read marks are still made).
//   2026-10-16  The saved position is ResumeRewindSeconds before the point where the user stopped.
//   2026-10-16  The setting Resume (Yes / Ask / No) says what happens with a saved position; Save Resume Point has no "none" any more.
//   2026-10-16  An audio file is read as soon as it plays; the 60 seconds are for video files (an MP4 is a video file).
//               The resume position of an audio file is still saved only after 60 seconds of playing.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        // The rule: this much playing makes a file read, and starts the saving of the position. To be refined.
        private const long ReadAfterMs = 60_000;
        private const long SavePositionEveryMs = 10_000;
        private const long MinResumeMs = 10_000;   // a position nearer the start than this is not worth offering
        private const int ResumeCountdownSeconds = 10;   // the question "Do you want to resume?" answers Yes by itself after this

        private string _libraryPath = "";   // what the file is called in the library
        private long _playedMs;             // played in this session
        private long _lastTrackTicks;       // 0 = the clock is not running
        private bool _markedRead;
        private bool _savingPosition;       // the 60 seconds are over: the position follows the playback
        private long _lastSaveTicks;
        private long _resumeAtMs = -1;      // chosen when the file was opened, used when its length is known
        private long _resumeStoredLengthMs;
        private bool _resumeIgnoreStoredLength;   // the start time was asked for by the explorer (Demo Play): it does not belong to a saved length

        private bool LibraryActive => !_isStream && !IsDvd && _libraryPath.Length > 0;   // a DVD is not remembered: its position is not a time in a file

        // Whether this file gets a resume point, by the setting. An audio file needs its length (known once it plays).
        private bool PositionWanted()
        {
            if (!LibraryActive || SidPluginSetup.IsSidFile(_filePath) || ResumeMode(_filePath) == 2)
            {
                return false;   // Resume = No: nothing is saved
            }
            if (_lengthMs > 0 && _lengthMs <= ReadAfterMs)
            {
                return false;   // a short clip is watched in one go: there is nothing to resume
            }
            return !IsAudioFile(_filePath)
                || (_lengthMs > 0 && _lengthMs >= _settings.ResumeAudioMinMinutes * 60_000L);
        }

        // What happens with a saved position of this file: 0 = Yes, 1 = Ask, 2 = No (nothing is saved or offered). Video and audio files
        // have their own settings. Audio files only resume when Resume Audio Files is on (and then only the long ones, see
        // PositionWanted). A track of an audio CD never does: every disc has an E:\Track02.cda, so the path says nothing about which disc.
        private int ResumeMode(string path)
        {
            if (!IsAudioFile(path))
            {
                return _settings.ResumeWhenOpening;
            }
            return _settings.ResumeAudioFiles && !Path.GetExtension(path).Equals(".cda", StringComparison.OrdinalIgnoreCase)
                ? Math.Clamp(_settings.ResumeAudioMode, 0, 1)
                : 2;
        }

        // A new file (or the same one again) starts: forget what was counted, and offer to continue where the user stopped.
        // Called by StartMedia before the file plays.
        private void StartLibrary(bool alwaysFromStart = false)
        {
            _playedMs = 0;
            _lastTrackTicks = 0;
            _markedRead = false;
            _savingPosition = false;
            _lastSaveTicks = 0;
            _resumeAtMs = -1;
            _resumeIgnoreStoredLength = false;

            // The explorer's Demo Play: start at this time, whatever was saved (no question).
            long startAt = _startAtOnceMs;
            _startAtOnceMs = 0;
            if (startAt > 0)
            {
                _noResumeOnce = false;
                _resumeAtMs = startAt;
                _resumeIgnoreStoredLength = true;
                return;
            }

            // The explorer's playlist (MediaPlayerForm.Pipe.cs) does not want a saved position, or a question about one, for this file.
            if (_noResumeOnce)
            {
                _noResumeOnce = false;
                return;
            }

            // The quick setting "Always Play from Start" (Media menu of the main window): no offer, but the position is saved as usual.
            if (alwaysFromStart)
            {
                return;
            }

            if (!LibraryActive || ResumeMode(_filePath) == 2 || SidPluginSetup.IsSidFile(_filePath))
            {
                return;
            }

            (long Position, long Length)? saved = LibraryStore.GetPosition(_libraryPath);
            if (saved == null)
            {
                return;
            }

            // Not near the start, and not at the end: there the video was basically finished.
            (long position, long length) = saved.Value;
            if (position < MinResumeMs || length <= 0 || position > length - Math.Max(30_000, length / 20))
            {
                return;
            }

            // An audio file that is not to be resumed by the setting (or is too short) is not offered, even if a position was saved earlier.
            if (IsAudioFile(_filePath) && (length < _settings.ResumeAudioMinMinutes * 60_000L))
            {
                return;
            }

            // Yes: continue without asking. Ask: ask first; Yes is chosen by itself after 10 seconds if the user does nothing.
            bool resume = ResumeMode(_filePath) == 0
                || CountdownMessageBox.AskYesNo(this,
                    $"Do you want to resume from {FormatTime(position, false)}?\n\nChoose No to start from the beginning.", AppName, ResumeCountdownSeconds);
            if (resume)
            {
                _resumeAtMs = position;
                _resumeStoredLengthMs = length;
            }
        }

        // Jumps to the position the user chose to resume from, once LibVLC knows the length and can seek.
        // The saved length must match: a file that changed (trimmed, replaced) has another time line.
        private void ApplyResume()
        {
            if (_resumeAtMs < 0 || _lengthMs <= 0 || !_vlcSeekable || _player == null)
            {
                return;
            }

            long target = _resumeAtMs;
            _resumeAtMs = -1;
            if (_resumeIgnoreStoredLength)
            {
                _resumeIgnoreStoredLength = false;
                if (target < _lengthMs - 1000)   // a file shorter than the start time plays from the beginning
                {
                    SeekTo(target);
                }
                return;
            }
            if (Math.Abs(_lengthMs - _resumeStoredLengthMs) < 2000)
            {
                SeekTo(target);
            }
        }

        // Called by the 30 ms timer: counts the time the file really plays, and acts on it.
        private void TrackLibrary()
        {
            if (!LibraryActive || _player == null || _trimming || _previewing || _restoringEnd)
            {
                _lastTrackTicks = 0;
                return;
            }

            long now = Environment.TickCount64;
            bool counting = _player.IsPlaying && !_paused && !_seeking;
            if (counting && _lastTrackTicks != 0)
            {
                _playedMs += Math.Min(now - _lastTrackTicks, 250);   // a stalled timer must not count as playing
            }
            _lastTrackTicks = counting ? now : 0;

            // An audio file (music, SID) is read as soon as it plays: the 60 seconds are for video, which is often started and
            // left. A video file (an MP4 with a still picture too) follows the rule below. The resume position still waits
            // for the 60 seconds (_savingPosition).
            if (!_markedRead && counting && IsAudioFile(_filePath))
            {
                _markedRead = true;
                LibraryStore.SetRead(new[] { _libraryPath }, true);
            }

            // A file of 60 seconds or less is read as soon as it plays; a longer one after 60 seconds of playing.
            bool shortFile = _lengthMs > 0 && _lengthMs <= ReadAfterMs;
            if (!_savingPosition && (_playedMs >= ReadAfterMs || (shortFile && counting)))
            {
                _savingPosition = true;
                MarkReadNow();
            }
            if (counting && _savingPosition && now - _lastSaveTicks >= SavePositionEveryMs)
            {
                SavePosition(false);
            }
        }

        private void MarkReadNow()
        {
            if (_markedRead)
            {
                return;
            }
            _markedRead = true;

            if (PositionWanted())
            {
                SavePosition(false);   // saving a position marks the file read
            }
            else
            {
                LibraryStore.SetRead(new[] { _libraryPath }, true);
            }
        }

        // Saves where the video is now. Not while the first 60 seconds are not over. Off the UI thread, except when the window closes.
        private void SavePosition(bool wait)
        {
            _lastSaveTicks = Environment.TickCount64;
            if (!_savingPosition || _player == null || !PositionWanted() || _lengthMs <= 0)
            {
                return;
            }

            string path = _libraryPath;
            // A few seconds earlier than where the user stopped (never before the start), to see a little of what came before.
            long position = Math.Max(0, _player.Time - Math.Max(0, _settings.ResumeRewindSeconds) * 1000L);
            long length = _lengthMs;
            if (wait)
            {
                LibraryStore.SetPosition(path, position, length);
            }
            else
            {
                _ = Task.Run(() => LibraryStore.SetPosition(path, position, length));
            }
        }

        // The video played to its end: it counts as read if it was watched (a short clip too), and it starts from the
        // beginning next time.
        private void LibraryOnEnded()
        {
            if (!LibraryActive || _previewing || _trimming)
            {
                return;
            }

            if (!_markedRead && _lengthMs > 0 && _playedMs >= Math.Min(ReadAfterMs, _lengthMs / 2))
            {
                _markedRead = true;
                LibraryStore.SetRead(new[] { _libraryPath }, true);
            }

            if (_markedRead)
            {
                string path = _libraryPath;
                _ = Task.Run(() => LibraryStore.ClearPosition(path));
            }

            // A second round (repeat, or Play after the end) must be watched for 60 seconds before it saves again.
            _playedMs = 0;
            _savingPosition = false;
            _lastTrackTicks = 0;
        }

        // The file was replaced by a trim: the old position does not fit any more.
        private void LibraryFileReplaced()
        {
            if (LibraryActive)
            {
                LibraryStore.ClearPosition(_libraryPath);
            }
        }
    }
}
