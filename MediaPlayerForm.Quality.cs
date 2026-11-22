// MediaPlayerForm.Quality.cs: the quality of an HLS channel (right-click menu: Quality).
//
// Many channels offer one picture in several qualities. LibVLC picks one by itself from the download speed (Auto), which is also what
// this player does unless the user chooses otherwise. The menu offers:
//   Auto      - LibVLC decides (the default)
//   Highest   - always the best quality
//   Lowest    - always the lowest quality (for a slow connection)
//   720p ...  - the best quality up to that height (a cap; if nothing is that low, the lowest)
// The qualities are read from the channel's master playlist (EXT-X-STREAM-INF), fetched in the background when the stream is opened.
// The menu is only shown for a stream that offers a choice (two or more qualities).
//
// LibVLC 3 only takes these as options of the Media (adaptive-logic, adaptive-maxheight), so a choice reopens the channel, like a click
// on it in the list. The choice stays for this window: the next channel is played with it too (a cap works for any channel).
// A stream that is recorded through the player is not reopened (the menu is disabled).
//
// Tested 2026-11-19 with a headless LibVLC 3.0.24 test: lowest, highest and a cap each gave the expected picture size on a channel with three qualities;
// a cap below every quality gave the lowest. LibVLC's own track list of an HLS stream is incomplete (a quality was missing), so it is not used.
//
// History:
//   2026-11-19  First version.
//   2026-11-22  The same lookup also checks for DRM keys (Widevine, PlayReady, FairPlay) and says so: only the sound plays, the picture is black.

using System.Globalization;
using LibVLCSharp.Shared;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private enum QualityMode { Auto, Highest, Lowest, MaxHeight }

        private record StreamVariant(int Height, int Bandwidth);

        private static readonly HttpClient _variantClient = new() { Timeout = TimeSpan.FromSeconds(8) };

        private QualityMode _qualityMode = QualityMode.Auto;
        private int _qualityHeight;                       // with QualityMode.MaxHeight
        private string? _variantsFor;                     // the stream address the list below belongs to (or is being fetched for)
        private List<StreamVariant> _variants = new();    // the qualities of that stream, highest first; empty = no choice

        // Called when a Media is made for a stream: starts reading the qualities of a new stream address.
        private void StartVariantsLookup(string url)
        {
            if (string.Equals(url, _variantsFor, StringComparison.Ordinal))
            {
                return;
            }

            _variantsFor = url;
            _variants = new List<StreamVariant>();
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return;
            }

            string? userAgent = url == _channelUserAgentFor && !string.IsNullOrEmpty(_channelUserAgent) ? _channelUserAgent : null;
            _ = LoadVariantsAsync(url, uri, userAgent);
        }

        private async Task LoadVariantsAsync(string url, Uri uri, string? userAgent)
        {
            List<StreamVariant> found = new();
            bool drm = false;
            try
            {
                string? text = await GetPlaylistTextAsync(uri, userAgent);
                if (text != null)
                {
                    found = ParseVariants(text);
                    drm = HasDrmKey(text);
                    if (!drm && text.Contains("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
                    {
                        // The keys are usually in the playlist of a quality, not in the master playlist: look at the first one.
                        string? variantLine = text.Split('\n').Select(l => l.Trim())
                            .SkipWhile(l => !l.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                            .Skip(1).FirstOrDefault(l => l.Length > 0 && !l.StartsWith('#'));
                        if (variantLine != null && Uri.TryCreate(uri, variantLine, out Uri? variantUri))
                        {
                            string? variantText = await GetPlaylistTextAsync(variantUri, userAgent);
                            drm = variantText != null && HasDrmKey(variantText);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                // No list: the menu is not shown, the stream plays as before.
            }

            if (!IsDisposed && string.Equals(_variantsFor, url, StringComparison.Ordinal))
            {
                _variants = found;
                if (drm)
                {
                    BeginInvoke(() => MessageBox.Show(this,
                        "This stream is protected by DRM (copy protection). The player can play its sound, but not decode its picture, so the screen stays black.\n\n"
                        + "To watch it, use the website or app of the service that offers it.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information));
                }
            }
        }

        private static async Task<string?> GetPlaylistTextAsync(Uri uri, string? userAgent)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent ?? "VLC/3.0.24 LibVLC/3.0.24");
            using HttpResponseMessage response = await _variantClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode && response.Content.Headers.ContentLength is null or < 1_000_000
                ? await response.Content.ReadAsStringAsync()
                : null;
        }

        // True when a playlist has an EXT-X-KEY for a DRM system (Widevine, PlayReady, FairPlay). A plain AES-128 key is not DRM: LibVLC can use it.
        private static bool HasDrmKey(string playlist)
        {
            foreach (string raw in playlist.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("#EXT-X-KEY:", StringComparison.OrdinalIgnoreCase)
                    && (line.Contains("edef8ba9-79d6-4ace-a3c8-27dcd51d21ed", StringComparison.OrdinalIgnoreCase)   // Widevine
                        || line.Contains("com.microsoft.playready", StringComparison.OrdinalIgnoreCase)
                        || line.Contains("com.apple.streamingkeydelivery", StringComparison.OrdinalIgnoreCase)))   // FairPlay
                {
                    return true;
                }
            }
            return false;
        }

        // The qualities of a master playlist, one per height (the highest bandwidth of a height), the highest first. Fewer than two = no choice.
        private static List<StreamVariant> ParseVariants(string playlist)
        {
            Dictionary<int, int> byHeight = new();
            int count = 0;
            foreach (string raw in playlist.Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                count++;
                int bandwidth = AttributeNumber(line, "BANDWIDTH=");
                int height = 0;
                int r = line.IndexOf("RESOLUTION=", StringComparison.OrdinalIgnoreCase);
                if (r >= 0)
                {
                    string value = line[(r + 11)..];
                    int x = value.IndexOfAny(new[] { 'x', 'X' });
                    int end = value.IndexOf(',');
                    if (x >= 0 && int.TryParse(end >= 0 ? value[(x + 1)..end] : value[(x + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out int h))
                    {
                        height = h;
                    }
                }
                if (height > 0)
                {
                    byHeight[height] = Math.Max(byHeight.GetValueOrDefault(height), bandwidth);
                }
            }

            // Only a list with a real choice counts (an audio-only variant has no resolution and is not a choice).
            return count >= 2 && byHeight.Count >= 2
                ? byHeight.OrderByDescending(p => p.Key).Select(p => new StreamVariant(p.Key, p.Value)).ToList()
                : new List<StreamVariant>();
        }

        private static int AttributeNumber(string line, string name)
        {
            int i = line.IndexOf(name, StringComparison.OrdinalIgnoreCase);
            // Not AVERAGE-BANDWIDTH: it must be the attribute itself.
            while (i > 0 && line[i - 1] == '-')
            {
                i = line.IndexOf(name, i + 1, StringComparison.OrdinalIgnoreCase);
            }
            if (i < 0)
            {
                return 0;
            }
            string rest = line[(i + name.Length)..];
            int end = rest.IndexOf(',');
            return int.TryParse(end >= 0 ? rest[..end] : rest, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : 0;
        }

        // The options that make LibVLC play the chosen quality; added to the Media of a stream (CreateMedia). Other kinds of streams ignore them.
        private void AddQualityOptions(Media media)
        {
            switch (_qualityMode)
            {
                case QualityMode.Highest:
                    media.AddOption(":adaptive-logic=highest");
                    break;
                case QualityMode.Lowest:
                    media.AddOption(":adaptive-logic=lowest");
                    break;
                case QualityMode.MaxHeight:
                    media.AddOption(":adaptive-logic=highest");
                    media.AddOption(string.Create(CultureInfo.InvariantCulture, $":adaptive-maxheight={_qualityHeight}"));
                    break;
            }
        }

        private void AddQualityMenu()
        {
            if (!_isStream || _variants.Count < 2 || !string.Equals(_variantsFor, _filePath, StringComparison.Ordinal))
            {
                return;
            }

            ToolStripMenuItem menu = new("Quality") { Enabled = _recorder == null };
            if (_recorder != null)
            {
                menu.ToolTipText = "The quality cannot be changed while a recording is made.";
            }
            menu.DropDownItems.Add(QualityItem("Auto", "LibVLC chooses the quality from the speed of the connection.", _qualityMode == QualityMode.Auto, QualityMode.Auto, 0));
            menu.DropDownItems.Add(QualityItem("Highest", "Always the best quality.", _qualityMode == QualityMode.Highest, QualityMode.Highest, 0));
            menu.DropDownItems.Add(QualityItem("Lowest", "Always the lowest quality, for a slow connection.", _qualityMode == QualityMode.Lowest, QualityMode.Lowest, 0));
            menu.DropDownItems.Add(new ToolStripSeparator());

            // A cap is checked on the height that is the best one under it.
            int capped = _qualityMode == QualityMode.MaxHeight
                ? _variants.Where(v => v.Height <= _qualityHeight).Select(v => v.Height).DefaultIfEmpty(_variants[^1].Height).Max()
                : -1;
            foreach (StreamVariant v in _variants)
            {
                string mbit = v.Bandwidth > 0 ? $" ({v.Bandwidth / 1_000_000.0:0.0} Mbit/s)" : "";
                menu.DropDownItems.Add(QualityItem($"{v.Height}p{mbit}", $"The best quality up to {v.Height}p.", v.Height == capped, QualityMode.MaxHeight, v.Height));
            }
            menuVideo.Items.Add(menu);
        }

        private ToolStripMenuItem QualityItem(string text, string tip, bool isCurrent, QualityMode mode, int height)
        {
            ToolStripMenuItem item = new(text) { Checked = isCurrent, ToolTipText = tip };
            item.Click += (_, _) => ChooseQuality(mode, height);
            return item;
        }

        private async void ChooseQuality(QualityMode mode, int height)
        {
            if (mode == _qualityMode && (mode != QualityMode.MaxHeight || height == _qualityHeight))
            {
                return;
            }

            _qualityMode = mode;
            _qualityHeight = height;
            if (_player != null && _isStream && !string.IsNullOrEmpty(_filePath) && _recorder == null)
            {
                OpenTimingLog.Write($"Quality changed to {mode} {height}: the stream is opened again");
                await OpenAnotherAsync(_filePath, _libraryPath, false);
            }
        }
    }
}
