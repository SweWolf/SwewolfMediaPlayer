// MediaPlayerForm.StreamInfo.cs: the "Stream Info" group box of MediaPlayerForm (a third one, beside Trim and Control Panel).
//
// A debug view for now: it shows everything the player knows about what plays (the channel list entry, the tracks and their codecs, the
// quality choices of an adaptive stream, the metadata and the statistics of LibVLC), refreshed once a second while the box is open, so a
// quality that changes during playback shows up. It is opened from the right-click menu of the control bar (like Control Panel).
// Nothing is remembered. It may be removed, or become a smaller info display, once it is known which of the values are interesting.
//
// History:
//   2026-11-13  First version.

using System.Runtime.InteropServices;
using System.Text;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int EM_GETFIRSTVISIBLELINE = 0xCE;
        private const int EM_LINESCROLL = 0xB6;

        private GroupBox? _grpStreamInfo;
        private TextBox? _txtStreamInfo;
        private System.Windows.Forms.Timer? _streamInfoTimer;
        private ToolStripMenuItem? _mnuShowStreamInfo;
        private bool _streamInfoVisible;

        private void InitStreamInfo()
        {
            _mnuShowStreamInfo = new ToolStripMenuItem("Stream Info");
            _mnuShowStreamInfo.Click += (_, _) => SetStreamInfoVisible(!_streamInfoVisible);
            menuControls.Items.Add(_mnuShowStreamInfo);
        }

        private void EnsureStreamInfo()
        {
            if (_grpStreamInfo != null)
            {
                return;
            }

            var close = new CloseXButton { Anchor = AnchorStyles.Top | AnchorStyles.Right, Size = btnCloseControlPanel.Size };
            close.Click += (_, _) => SetStreamInfoVisible(false);
            toolTip.SetToolTip(close, "Close Stream Info");

            _txtStreamInfo = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 24, 24),
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Consolas", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };

            _grpStreamInfo = new GroupBox
            {
                Text = "Stream Info",
                Dock = DockStyle.Bottom,
                Height = LogicalToDeviceUnits(190),
                TabStop = false,
                Visible = false,
                ContextMenuStrip = menuControls,
            };
            _grpStreamInfo.Controls.Add(_txtStreamInfo);
            _grpStreamInfo.Controls.Add(close);
            int gap = LogicalToDeviceUnits(8);
            _txtStreamInfo.SetBounds(gap, LogicalToDeviceUnits(20), _grpStreamInfo.ClientSize.Width - 2 * gap, _grpStreamInfo.ClientSize.Height - LogicalToDeviceUnits(20) - gap);
            close.Location = new Point(_grpStreamInfo.ClientSize.Width - close.Width - 4, LogicalToDeviceUnits(9));
            close.BringToFront();

            Controls.Add(_grpStreamInfo);
            Controls.SetChildIndex(_grpStreamInfo, Controls.GetChildIndex(grpControlPanel) + 1);   // beside the other two group boxes, under the video

            _streamInfoTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _streamInfoTimer.Tick += (_, _) => RefreshStreamInfo();
        }

        private void SetStreamInfoVisible(bool visible)
        {
            if (visible == _streamInfoVisible)
            {
                return;
            }

            EnsureStreamInfo();
            _streamInfoVisible = visible;
            _grpStreamInfo!.Visible = visible && !_fullScreen;
            UpdateMinimumSize();
            if (visible)
            {
                RefreshStreamInfo();
                _streamInfoTimer!.Start();
            }
            else
            {
                _streamInfoTimer?.Stop();
            }

            // Keep the video as large as it was: the window grows and shrinks with the group box.
            if (!_fullScreen && WindowState == FormWindowState.Normal)
            {
                int height = Height + (visible ? _grpStreamInfo.Height : -_grpStreamInfo.Height);
                Height = Math.Min(height, Screen.FromControl(this).WorkingArea.Height);
            }
        }

        private static string FourCc(uint code)
        {
            string text = Encoding.ASCII.GetString(BitConverter.GetBytes(code)).TrimEnd('\0', ' ');
            return text.Length > 0 && text.All(c => c >= 32 && c < 127) ? text : "0x" + code.ToString("X");
        }

        private void RefreshStreamInfo()
        {
            if (_txtStreamInfo == null || !_streamInfoVisible || IsDisposed || PlayerBusy)
            {
                return;
            }

            string text;
            try
            {
                text = BuildStreamInfo();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                text = "The information could not be read: " + ex.Message;
            }
            if (text == _txtStreamInfo.Text.Replace("\r\n", "\n"))
            {
                return;
            }

            // Keep the place in the text (and a selection): a new text would scroll to the top.
            int first = (int)SendMessage(_txtStreamInfo.Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero);
            int start = _txtStreamInfo.SelectionStart, length = _txtStreamInfo.SelectionLength;
            _txtStreamInfo.Text = text.Replace("\n", "\r\n");
            _txtStreamInfo.Select(Math.Min(start, _txtStreamInfo.TextLength), Math.Min(length, Math.Max(0, _txtStreamInfo.TextLength - start)));
            SendMessage(_txtStreamInfo.Handle, EM_LINESCROLL, IntPtr.Zero, (IntPtr)first);
        }

        private string BuildStreamInfo()
        {
            var sb = new StringBuilder();
            MediaPlayer? player = _player;

            // Where it comes from.
            sb.AppendLine("== Source ==");
            if (_channels != null && _channelIndex >= 0 && _channelIndex < _channels.Count)
            {
                M3uEntry channel = _channels[_channelIndex];
                sb.AppendLine($"Channel:      {channel.Name}");
                sb.AppendLine($"Group:        {channel.Group}");
                sb.AppendLine($"Logo:         {channel.LogoUrl}");
                sb.AppendLine($"User agent:   {channel.UserAgent}");
                sb.AppendLine($"List:         {_channelListSource}");
            }
            sb.AppendLine($"{(_isStream ? "Address:      " : "File:         ")}{_filePath}");
            if (IsRecordingPlayback)
            {
                sb.AppendLine("Recording:    this is the file that is being recorded");
            }
            else if (_recorder != null)
            {
                sb.AppendLine("Recording:    in the player (the player itself writes the file)");
            }

            if (player == null)
            {
                return sb.ToString().Replace("\r\n", "\n");
            }

            // The state of the player.
            sb.AppendLine();
            sb.AppendLine("== Player ==");
            sb.AppendLine($"State:        {player.State}, seekable: {player.IsSeekable}");
            sb.AppendLine($"Time/Length:  {player.Time / 1000.0:F1} s / {(player.Length > 0 ? (player.Length / 1000.0).ToString("F1") + " s" : "unknown")}");
            sb.AppendLine($"Speed:        {player.Rate:F2}x    Volume: {player.Volume}%");
            uint w = 0, h = 0;
            player.Size(0, ref w, ref h);
            sb.AppendLine($"Video output: {w}x{h}, frame rate {player.Fps:F2} fps");

            Media? media = player.Media;
            if (media == null)
            {
                return sb.ToString().Replace("\r\n", "\n");
            }

            // The tracks that the stream contains.
            sb.AppendLine();
            sb.AppendLine("== Tracks (what the stream contains) ==");
            MediaTrack[] tracks = media.Tracks;
            if (tracks.Length == 0)
            {
                sb.AppendLine("(none known yet)");
            }
            foreach (MediaTrack t in tracks)
            {
                string kind = t.TrackType.ToString();
                string detail = "";
                if (t.TrackType == TrackType.Video)
                {
                    VideoTrack v = t.Data.Video;
                    double fps = v.FrameRateDen > 0 ? (double)v.FrameRateNum / v.FrameRateDen : 0;
                    detail = $"{v.Width}x{v.Height}, {fps:F2} fps, pixel aspect {v.SarNum}:{v.SarDen}";
                }
                else if (t.TrackType == TrackType.Audio)
                {
                    AudioTrack a = t.Data.Audio;
                    detail = $"{a.Channels} channels, {a.Rate} Hz";
                }
                string bitrate = t.Bitrate > 0 ? $", {t.Bitrate / 1000} kb/s" : "";
                string lang = string.IsNullOrEmpty(t.Language) ? "" : $", language {t.Language}";
                string desc = string.IsNullOrEmpty(t.Description) ? "" : $", \"{t.Description}\"";
                sb.AppendLine($"#{t.Id,-4} {kind,-9} {FourCc(t.Codec)}{(t.OriginalFourcc != t.Codec ? " (stream: " + FourCc(t.OriginalFourcc) + ")" : "")}  profile {t.Profile}, level {t.Level}  {detail}{bitrate}{lang}{desc}");
            }

            // What the user can choose; for an adaptive (HLS) stream the video list is the list of qualities.
            AppendChoices(sb, "Video choices (for an HLS stream: the qualities)", player.VideoTrackDescription, player.VideoTrack);
            AppendChoices(sb, "Audio choices", player.AudioTrackDescription, player.AudioTrack);
            AppendChoices(sb, "Subtitle choices", player.SpuDescription, player.Spu);

            // What the stream says about itself.
            sb.AppendLine();
            sb.AppendLine("== Metadata ==");
            bool any = false;
            foreach (MetadataType type in new[] { MetadataType.Title, MetadataType.NowPlaying, MetadataType.Publisher, MetadataType.ShowName,
                MetadataType.Genre, MetadataType.Language, MetadataType.Description, MetadataType.URL, MetadataType.Copyright,
                MetadataType.EncodedBy, MetadataType.Date, MetadataType.Artist, MetadataType.Album })
            {
                string? value = media.Meta(type);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    sb.AppendLine($"{type,-12}  {value}");
                    any = true;
                }
            }
            if (!any)
            {
                sb.AppendLine("(the stream sends none)");
            }

            // The numbers of LibVLC, which change all the time.
            sb.AppendLine();
            sb.AppendLine("== Statistics ==");
            MediaStats s = media.Statistics;
            sb.AppendLine($"Input:        {s.ReadBytes / 1024.0 / 1024.0:F1} MB read, {s.InputBitrate * 8000:F0} kb/s");
            sb.AppendLine($"Demux:        {s.DemuxReadBytes / 1024.0 / 1024.0:F1} MB, {s.DemuxBitrate * 8000:F0} kb/s, corrupted {s.DemuxCorrupted}, discontinuities {s.DemuxDiscontinuity}");
            sb.AppendLine($"Video:        decoded {s.DecodedVideo}, shown {s.DisplayedPictures}, lost {s.LostPictures}");
            sb.AppendLine($"Audio:        decoded {s.DecodedAudio}, played {s.PlayedAudioBuffers}, lost {s.LostAudioBuffers}");
            return sb.ToString().Replace("\r\n", "\n");
        }

        private static void AppendChoices(StringBuilder sb, string title, IEnumerable<TrackDescription> choices, int current)
        {
            sb.AppendLine();
            sb.AppendLine($"== {title} ==");
            bool any = false;
            foreach (TrackDescription c in choices)
            {
                sb.AppendLine($"{(c.Id == current ? "*" : " ")} #{c.Id,-4} {c.Name}");
                any = true;
            }
            if (!any)
            {
                sb.AppendLine("(none)");
            }
        }
    }
}
