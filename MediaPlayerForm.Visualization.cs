// MediaPlayerForm.Visualization.cs: moving graphics (bars, waves, patterns) in the video area while an audio
// file plays. LibVLC makes them; the right-click menu has a Visualization entry to pick one, and the choice is
// not remembered: every window starts without one. Only audio files get the menu entry; video files never show it.
//
// LibVLC 3 reads the visualization (--audio-visual) only when its engine is created, not per file. A window
// that shows one therefore has an engine of its own (_ownLibVlc, a few seconds to start); the other windows
// share the one engine of the program. Changing the choice makes a new engine and player and starts the file
// again at the same place (and the same tune). While a visualization is on, the cover art of an audio file is
// not shown, as it would cover it.
//
// History:
//   2026-10-16  First version.
//   2026-10-16  Not remembered between windows: a window always starts without a visualization. Escape turns it off.

using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        // Key (saved in the settings), menu text, LibVLC module (--audio-visual) and effect (--effect-list, for "visual").
        private static readonly (string Key, string Text, string Module, string? Effect)[] Visualizations =
        {
            ("spectrum", "Spectrum", "visual", "spectrum"),
            ("scope", "Scope", "visual", "scope"),
            ("spectrometer", "Spectrometer", "visual", "spectrometer"),
            ("vumeter", "VU Meter", "visual", "vuMeter"),
            ("goom", "Goom", "goom", null),
            ("projectm", "projectM", "projectm", null),
            ("glspectrum", "3D Spectrum", "glspectrum", null),
        };

        private string _visualization = "none";   // the key of the one that is on in this window
        private LibVLC? _ownLibVlc;   // this window's own engine, only while a visualization is on
        private LibVLC? _activeLib;   // the engine of the player (the shared one or _ownLibVlc)
        private int _restoreTune = -1;   // after a new engine: the tune to go back to (SID files)

        // The visualization that is on for this file, or null (also for anything but an audio file).
        private (string Key, string Text, string Module, string? Effect)? ActiveVisualization()
        {
            if (!IsAudioFile(_filePath))
            {
                return null;
            }
            foreach (var visualization in Visualizations)
            {
                if (visualization.Key == _visualization)
                {
                    return visualization;
                }
            }
            return null;
        }

        // A new engine with the visualization, or null if it could not be made (the shared engine is used then).
        private Task<LibVLC?> CreateOwnEngineAsync()
        {
            var visualization = ActiveVisualization();
            if (visualization == null)
            {
                return Task.FromResult<LibVLC?>(null);
            }

            // They draw in a picture of a fixed size (800 x 500 as a start): make it as big as the video area.
            int width = Math.Max(videoView.Width, 320);
            int height = Math.Max(videoView.Height, 200);
            var options = new List<string> { "--audio-visual=" + visualization.Value.Module };
            if (visualization.Value.Effect != null)
            {
                options.Add("--effect-list=" + visualization.Value.Effect);
                options.Add("--effect-width=" + width);
                options.Add("--effect-height=" + height);
            }
            else if (visualization.Value.Module == "projectm")
            {
                options.Add("--projectm-width=" + width);
                options.Add("--projectm-height=" + height);
            }
            return Task.Run(() =>
            {
                try
                {
                    return (LibVLC?)new LibVLC(options.ToArray());
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        private void DisposeOwnEngine()
        {
            LibVLC? engine = _ownLibVlc;
            _ownLibVlc = null;
            if (engine != null)
            {
                Task.Run(engine.Dispose);
            }
        }

        private void AddVisualizationMenu()
        {
            if (!IsAudioFile(_filePath))
            {
                return;
            }

            ToolStripMenuItem menu = new("Visualization");
            menu.DropDownItems.Add(TrackItem("Off", ActiveVisualization() == null, () => SetVisualization("none")));
            foreach (var visualization in Visualizations)
            {
                string key = visualization.Key;
                menu.DropDownItems.Add(TrackItem(visualization.Text, key == _visualization, () => SetVisualization(key)));
            }
            menuVideo.Items.Add(menu);
        }

        private void SetVisualization(string key)
        {
            if (key == _visualization)
            {
                return;
            }

            _visualization = key;
            SwitchEngineAsync();
        }

        // Releases the player and its engine, makes new ones (with or without the visualization) and starts the
        // file again where it was.
        private async void SwitchEngineAsync()
        {
            MediaPlayer? player = _player;
            if (player == null || _trimming || _restoringEnd)
            {
                return;
            }

            long position = player.Time;
            bool seekable = CanSeek && _lengthMs > 0 && position > 0;
            int tune = _tuneCount > 1 ? _tuneIndex : -1;

            Media? media = _media;
            LibVLC? oldEngine = _ownLibVlc;
            _player = null;
            _media = null;
            _ownLibVlc = null;
            videoView.MediaPlayer = null;
            await Task.Run(() =>
            {
                player.Stop();
                player.Dispose();
                media?.Dispose();
                oldEngine?.Dispose();
            });
            if (IsDisposed)
            {
                return;
            }

            LibVLC engine = _libVlc!;
            if (ActiveVisualization() != null)
            {
                engine = _ownLibVlc = await CreateOwnEngineAsync() ?? engine;
                if (IsDisposed)
                {
                    DisposeOwnEngine();
                    return;
                }
            }

            AttachPlayer(engine);
            _player!.Volume = trkVolume.Value;
            StartMedia(true);
            if (seekable)
            {
                _resumeAtMs = position;   // ApplyResume jumps there once LibVLC knows the length
                _resumeStoredLengthMs = _lengthMs;
            }
            _restoreTune = tune;
        }
    }
}
