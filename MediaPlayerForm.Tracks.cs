// MediaPlayerForm.Tracks.cs: the audio track and the subtitle track of a file.
//
// - What the user chooses in the right-click menu (audio, video, subtitles; or with the C key) is remembered for that file in the library (LibraryStore,
//   table TrackChoice) and used the next time it opens. A choice is only saved when it changes something: picking the track that
//   already plays saves nothing. What the program does by itself (the saved choice, the default) is never saved.
// - A file with no choice of its own follows the setting "Turn On Subtitles When a Video Opens" (Tools > Settings); LibVLC picks the audio track.
// - A track is saved as its place in the list and its name, and found again by the name first.
// - "Use Default Tracks" in the menu forgets the choice of the file and goes back to the defaults.
// - Nothing is saved for streams, SID music and files without a library path.
//
// The tracks are known once the file plays, so the timer (CheckTracks) applies the choice once per file.
//
// History:
//   2026-10-26  First version.
//   2026-10-27  Video track from the menu (ChooseVideo), remembered like the others (schema 10); it jumps to the same position so the new
//               picture starts at once. Use Default Tracks resets it too.
//   2026-10-27  Use Default Tracks also changes a subtitle track that is on: it goes to the track LibVLC chose, or else the first one.

using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private LibraryStore.TrackChoiceRow? _savedTracks;   // the choice of the file that plays, null = none
        private bool _audioApplied;      // the saved audio choice has been looked at for the file that plays
        private bool _subtitleApplied;   // the same for the subtitles (or the default)
        private bool _videoApplied;      // the same for the video track
        private int _defaultVideoId = -1;   // the video track LibVLC chose by itself
        private int _defaultAudioId = -1;   // the audio track LibVLC chose by itself
        private int _vlcSubtitleId = -1;    // the subtitle track LibVLC chose by itself (-1 = none, the file has no default track)
        private bool _vlcSubtitleSeen;
        private bool _showSubtitlesByDefault = true;

        private bool TracksSaveable => LibraryActive && !SidPluginSetup.IsSidFile(_filePath);

        // A new file starts: read its saved choice and the default.
        private void ResetTracks()
        {
            _audioApplied = false;
            _subtitleApplied = false;
            _videoApplied = false;
            _defaultVideoId = -1;
            _defaultAudioId = -1;
            _vlcSubtitleId = -1;
            _vlcSubtitleSeen = false;
            _showSubtitlesByDefault = AppSettings.Load().ShowSubtitles;
            _savedTracks = TracksSaveable ? LibraryStore.GetTrackChoice(_libraryPath) : null;
        }

        private static List<TrackDescription> RealTracks(IEnumerable<TrackDescription> tracks)
        {
            return tracks.Where(t => t.Id >= 0).ToList();   // id -1 is the "disabled" entry
        }

        // The id of the track that was saved as this place and name, or -1. The name is looked for first (the order of tracks can change).
        private static int FindTrackId(List<TrackDescription> tracks, int? index, string? name)
        {
            if (name != null)
            {
                var byName = tracks.Where(t => t.Name == name).ToList();
                if (byName.Count == 1)
                {
                    return byName[0].Id;
                }
                if (byName.Count > 1 && index is { } place && place >= 0 && place < tracks.Count && tracks[place].Name == name)
                {
                    return tracks[place].Id;
                }
            }
            return index is { } i && i >= 0 && i < tracks.Count ? tracks[i].Id : -1;
        }

        // Called by the timer: applies the saved choice, or the default, once to the file that plays.
        private void CheckTracks()
        {
            MediaPlayer? player = _player;
            if ((_audioApplied && _subtitleApplied && _videoApplied) || player == null || !player.IsPlaying)
            {
                return;
            }

            if (_isStream || SidPluginSetup.IsSidFile(_filePath))
            {
                _audioApplied = _subtitleApplied = _videoApplied = true;
                return;
            }

            long time = player.Time;
            if (!_audioApplied)
            {
                ApplyAudioChoice(player, time);
            }
            if (!_videoApplied)
            {
                ApplyVideoChoice(player, time);
            }
            if (!_subtitleApplied)
            {
                ApplySubtitleChoice(player, time);
            }
        }

        private void ApplyVideoChoice(MediaPlayer player, long time)
        {
            var tracks = RealTracks(player.VideoTrackDescription);
            if (tracks.Count == 0)
            {
                _videoApplied = time > 3000;
                return;
            }

            _videoApplied = true;
            _defaultVideoId = player.VideoTrack;
            if (_savedTracks is { HasVideo: true } saved)
            {
                int id = FindTrackId(tracks, saved.VideoIndex, saved.VideoName);
                if (id >= 0 && id != player.VideoTrack)
                {
                    SwitchVideoTrack(player, id);
                }
            }
        }

        private void ApplyAudioChoice(MediaPlayer player, long time)
        {
            var tracks = RealTracks(player.AudioTrackDescription);
            if (tracks.Count == 0)
            {
                _audioApplied = time > 3000;   // some files have none
                return;
            }

            _audioApplied = true;
            _defaultAudioId = player.AudioTrack;
            if (_savedTracks is { HasAudio: true } saved)
            {
                int id = FindTrackId(tracks, saved.AudioIndex, saved.AudioName);
                if (id >= 0 && id != player.AudioTrack)
                {
                    player.SetAudioTrack(id);
                }
            }
        }

        private void ApplySubtitleChoice(MediaPlayer player, long time)
        {
            if (!_vlcSubtitleSeen)
            {
                _vlcSubtitleSeen = true;
                _vlcSubtitleId = player.Spu;   // before the saved choice or the default is applied
            }

            bool show = _showSubtitlesByDefault;
            if (_savedTracks is { HasSubtitle: true } saved)
            {
                if (saved.SubtitleOff)
                {
                    show = false;
                }
                else
                {
                    var tracks = RealTracks(player.SpuDescription);
                    if (tracks.Count == 0 && time <= 3000)
                    {
                        return;   // the tracks are not known yet
                    }
                    int id = FindTrackId(tracks, saved.SubtitleIndex, saved.SubtitleName);
                    if (id >= 0)
                    {
                        if (player.Spu != id)
                        {
                            player.SetSpu(id);
                        }
                        _subtitleApplied = true;
                        return;
                    }
                    // The track is not in the file any more: the default decides.
                }
            }

            // LibVLC turns on a track by itself for some files (a default track); a file without one is given a moment.
            bool trackOn = player.Spu >= 0;
            if (show)
            {
                if (!trackOn && time > 1500)
                {
                    int id = FirstSubtitleToTurnOn(player);
                    if (id >= 0)
                    {
                        player.SetSpu(id);
                    }
                }
                _subtitleApplied = trackOn || time > 1500;
            }
            else
            {
                if (trackOn)
                {
                    _lastSubtitleTrack = player.Spu;
                    player.SetSpu(-1);
                }
                _subtitleApplied = trackOn || time > 3000;
            }
        }

        // The subtitle track to turn on (the C key, a new file with subtitles on by default): the one that was on before, or the track
        // LibVLC chose, or else the first one. -1 if the file has none.
        private int FirstSubtitleToTurnOn(MediaPlayer player)
        {
            var tracks = RealTracks(player.SpuDescription);
            if (_lastSubtitleTrack >= 0 && tracks.Any(t => t.Id == _lastSubtitleTrack))
            {
                return _lastSubtitleTrack;
            }
            if (_vlcSubtitleId >= 0 && tracks.Any(t => t.Id == _vlcSubtitleId))
            {
                return _vlcSubtitleId;
            }
            return tracks.Count > 0 ? tracks[0].Id : -1;
        }

        // ---- What the user chooses ----

        // An audio track from the menu. Choosing the track that plays changes nothing and saves nothing.
        private void ChooseAudio(int id)
        {
            MediaPlayer? player = _player;
            if (player == null || id == player.AudioTrack)
            {
                return;
            }

            player.SetAudioTrack(id);
            if (!TracksSaveable)
            {
                return;
            }

            var tracks = RealTracks(player.AudioTrackDescription);
            int index = tracks.FindIndex(t => t.Id == id);
            if (index >= 0)
            {
                LibraryStore.SetAudioChoice(_libraryPath, index, tracks[index].Name);
                _savedTracks = LibraryStore.GetTrackChoice(_libraryPath);
            }
        }

        // A video track (picture) from the menu. Choosing the track that plays changes nothing and saves nothing.
        private void ChooseVideo(int id)
        {
            MediaPlayer? player = _player;
            if (player == null || id == player.VideoTrack)
            {
                return;
            }

            SwitchVideoTrack(player, id);
            if (!TracksSaveable)
            {
                return;
            }

            var tracks = RealTracks(player.VideoTrackDescription);
            int index = tracks.FindIndex(t => t.Id == id);
            if (index >= 0)
            {
                LibraryStore.SetVideoChoice(_libraryPath, index, tracks[index].Name);
                _savedTracks = LibraryStore.GetTrackChoice(_libraryPath);
            }
        }

        // The new picture can only start at a key frame of its own stream, and until then the screen is black (a long wait in a file
        // with few key frames): jumping to where the video is now makes the player start the new stream from the key frame before it.
        private void SwitchVideoTrack(MediaPlayer player, int id)
        {
            long now = Math.Max(player.Time, 0);
            player.SetVideoTrack(id);
            if (CanSeek)
            {
                SeekTo(now);
            }
        }

        // A subtitle track from the menu or the C key; id -1 = no subtitles. The same track (or off when it is off) saves nothing.
        private void ChooseSubtitle(int id)
        {
            MediaPlayer? player = _player;
            int current = player?.Spu ?? -1;
            if (player == null || id == current || (id < 0 && current < 0))
            {
                return;
            }

            if (current >= 0)
            {
                _lastSubtitleTrack = current;
            }
            player.SetSpu(id < 0 ? -1 : id);
            if (!TracksSaveable)
            {
                return;
            }

            if (id < 0)
            {
                LibraryStore.SetSubtitleChoice(_libraryPath, true, 0, null);
            }
            else
            {
                var tracks = RealTracks(player.SpuDescription);
                int index = tracks.FindIndex(t => t.Id == id);
                if (index < 0)
                {
                    return;
                }
                LibraryStore.SetSubtitleChoice(_libraryPath, false, index, tracks[index].Name);
            }
            _savedTracks = LibraryStore.GetTrackChoice(_libraryPath);
        }

        // ---- Use Default Tracks ----

        private void AddDefaultTracksMenuItem()
        {
            ToolStripMenuItem item = new("Use Default Tracks") { Enabled = TracksSaveable && _savedTracks != null };
            item.ToolTipText = "Forget the audio and subtitle tracks chosen for this video.";
            item.Click += (_, _) => UseDefaultTracks();
            menuVideo.Items.Add(item);
        }

        private void UseDefaultTracks()
        {
            MediaPlayer? player = _player;
            if (player == null || !TracksSaveable)
            {
                return;
            }

            LibraryStore.ClearTrackChoice(_libraryPath);
            _savedTracks = null;

            // Back to what LibVLC chose for the audio and the video, and to the setting for the subtitles.
            if (_defaultAudioId >= 0 && player.AudioTrack != _defaultAudioId)
            {
                player.SetAudioTrack(_defaultAudioId);
            }
            if (_defaultVideoId >= 0 && player.VideoTrack != _defaultVideoId)
            {
                SwitchVideoTrack(player, _defaultVideoId);
            }
            _showSubtitlesByDefault = AppSettings.Load().ShowSubtitles;
            if (_showSubtitlesByDefault)
            {
                // The track LibVLC chose, or else the first one (what a new file gets): not the one that was on a moment ago.
                var tracks = RealTracks(player.SpuDescription);
                int id = tracks.Any(t => t.Id == _vlcSubtitleId) ? _vlcSubtitleId : tracks.Count > 0 ? tracks[0].Id : -1;
                if (id >= 0 && player.Spu != id)
                {
                    _lastSubtitleTrack = player.Spu >= 0 ? player.Spu : _lastSubtitleTrack;
                    player.SetSpu(id);
                }
            }
            else if (player.Spu >= 0)
            {
                _lastSubtitleTrack = player.Spu;
                player.SetSpu(-1);
            }
        }
    }
}
