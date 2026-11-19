// ChannelProbe.cs: finds out, without playing it, whether a channel of an IPTV list answers (used by Check Channels).
//
// It asks the stream address like the player does (the channel's own user agent, else the one of VLC; no referrer, as the player sends none) and
// decides from what comes back:
//   - an HLS stream (#EXTM3U): the master list is followed to the first quality, and then the first piece of video of that quality is fetched;
//     a channel works when that piece (at least a few bytes) arrives;
//   - another stream (MPEG-TS and the like): it works when about a kilobyte of data arrives.
// A web page (text/html) instead of a stream, an error status, a timeout (12 seconds for the whole check) or an empty answer means it does not work.
// Only http and https addresses can be checked (the others, rtmp and the like, give "unknown": null).
// It does not decode anything, so "works" means "delivers data", which is a good but not a perfect sign.
//
// History:
//   2026-11-18  First version.

using System.Text;

namespace SwewolfMediaPlayer
{
    internal static class ChannelProbe
    {
        private const string DefaultAgent = "VLC/3.0.21 LibVLC/3.0.21";
        private static readonly TimeSpan Limit = TimeSpan.FromSeconds(12);

        private static readonly HttpClient Http = new(new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxConnectionsPerServer = 4,
            ConnectTimeout = TimeSpan.FromSeconds(8),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        })
        { Timeout = Timeout.InfiniteTimeSpan };

        // true: it delivers data. false: it does not. null: it cannot be checked this way. Throws OperationCanceledException when stop is canceled.
        public static async Task<bool?> WorksAsync(M3uEntry channel, CancellationToken stop)
        {
            if (!Uri.TryCreate(channel.Url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return null;
            }

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(stop);
            limit.CancelAfter(Limit);
            try
            {
                return await RunAsync(uri, channel.UserAgent ?? DefaultAgent, limit.Token);
            }
            catch (OperationCanceledException) when (!stop.IsCancellationRequested)
            {
                return false;   // the time limit
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return false;   // whatever else goes wrong with one odd channel (a bad address in its playlist, a broken connection): it does not work
            }
        }

        private static async Task<bool> RunAsync(Uri uri, string agent, CancellationToken ct)
        {
            (byte[]? data, Uri finalUri) = await FetchAsync(uri, agent, ct);
            if (data == null || data.Length == 0)
            {
                return false;
            }
            if (!StartsWithPlaylist(data))
            {
                return data.Length >= 512;   // a plain stream: some data came
            }

            Uri current = finalUri;
            string text = Encoding.UTF8.GetString(data);
            for (int level = 0; level < 3; level++)
            {
                string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string? next = null;
                bool master = text.Contains("#EXT-X-STREAM-INF", StringComparison.Ordinal);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith('#'))
                    {
                        continue;
                    }
                    next = line;   // the first address: the first quality of a master list, else the first piece of video
                    break;
                }
                if (next == null || !Uri.TryCreate(current, next, out Uri? target) || (target.Scheme != "http" && target.Scheme != "https"))
                {
                    return false;
                }

                if (!master)
                {
                    (byte[]? piece, _) = await FetchAsync(target, agent, ct, firstPiece: true);
                    return piece != null && piece.Length > 0;
                }

                (byte[]? list, Uri listUri) = await FetchAsync(target, agent, ct);
                if (list == null || !StartsWithPlaylist(list))
                {
                    return false;
                }
                current = listUri;
                text = Encoding.UTF8.GetString(list);
            }
            return false;
        }

        private static bool StartsWithPlaylist(byte[] data)
        {
            return data.Length >= 7 && Encoding.ASCII.GetString(data, 0, 7) == "#EXTM3U";
        }

        // The start of an answer: a playlist is read to its end (at most 256 KB), anything else only for its first kilobyte, then the connection is closed.
        // Returns null data for an error status or a web page.
        private static async Task<(byte[]? Data, Uri Final)> FetchAsync(Uri uri, string agent, CancellationToken ct, bool firstPiece = false)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("User-Agent", agent);
            using HttpResponseMessage response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            Uri final = response.RequestMessage?.RequestUri ?? uri;
            if (!response.IsSuccessStatusCode)
            {
                return (null, final);
            }
            string? type = response.Content.Headers.ContentType?.MediaType;
            if (type != null && type.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            {
                return (null, final);
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            while (memory.Length < 262144)
            {
                int n = await stream.ReadAsync(buffer, ct);
                if (n == 0)
                {
                    break;
                }
                memory.Write(buffer, 0, n);
                bool playlist = memory.Length >= 7 && Encoding.ASCII.GetString(memory.GetBuffer(), 0, 7) == "#EXTM3U";
                if (!playlist && memory.Length >= (firstPiece ? 188 : 1024))
                {
                    break;
                }
            }
            return (memory.ToArray(), final);
        }
    }
}
