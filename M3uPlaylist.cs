// M3uPlaylist.cs: reads an M3U list of streams (like the IPTV lists with thousands of channels), keeps a copy of a downloaded
// list on disk and checks if it has changed, and fetches and keeps the channel logos.
//
//  - Parse: #EXTINF lines give the name, the logo (tvg-logo), the group (group-title) and a user agent; a #EXTVLCOPT:http-user-agent line
//    or an http-user-agent attribute gives the user agent the stream needs.
//  - LooksLikeChannelList: an HLS stream is also an .m3u8 file. A list of channels has #EXTINF lines and no #EXT-X tags.
//  - LoadAsync: a web address is downloaded with a conditional request (ETag / Last-Modified) against the copy in
//    %LocalAppData%\SweWolfSoftware\SwewolfMediaPlayer\Playlists, so an unchanged list is not downloaded again. Without a connection the
//    copy is used.
//  - LogoCache: logos are downloaded when a row comes into view, kept in ...\LogoCache, and checked again (If-Modified-Since) when
//    they are a week old. A logo that cannot be loaded shows no picture.
//
// Self-contained: to reuse it in another app, change the namespace and AppFolder.
//
// History:
//   2026-11-12  First version.

using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SwewolfMediaPlayer
{
    internal sealed record M3uEntry(string Name, string Url, string Group, string? LogoUrl, string? UserAgent);

    internal static class M3uPlaylist
    {
        private const string AppFolder = "SwewolfMediaPlayer";
        internal static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SweWolfMediaPlayer");
            return client;
        }

        private static string LocalFolder(string sub) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SweWolfSoftware", AppFolder, sub);

        private static string HashName(string text)
        {
            return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        }

        // True for an address or file name that may be a list (an HLS stream ends in .m3u8 as well, so the content decides).
        public static bool MayBeList(string pathOrUrl)
        {
            string text = pathOrUrl.Trim();
            if (Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                text = uri.AbsolutePath;
            }
            string ext = Path.GetExtension(text);
            return ext.Equals(".m3u", StringComparison.OrdinalIgnoreCase) || ext.Equals(".m3u8", StringComparison.OrdinalIgnoreCase);
        }

        // A list of channels: #EXTINF entries, each followed by an address, and none of the #EXT-X tags of an HLS stream.
        public static bool LooksLikeChannelList(string text)
        {
            return text.Contains("#EXTINF", StringComparison.Ordinal) && !text.Contains("#EXT-X-", StringComparison.Ordinal);
        }

        private static readonly Regex Attribute = new("([A-Za-z0-9_-]+)=\"([^\"]*)\"", RegexOptions.Compiled);

        public static List<M3uEntry> Parse(string text)
        {
            var list = new List<M3uEntry>();
            string? name = null, group = null, logo = null, agent = null;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
                {
                    group = logo = agent = null;
                    foreach (Match m in Attribute.Matches(line))
                    {
                        string value = m.Groups[2].Value.Trim();
                        switch (m.Groups[1].Value.ToLowerInvariant())
                        {
                            case "group-title": group = value; break;
                            case "tvg-logo": logo = value.Length > 0 ? value : null; break;
                            case "http-user-agent": agent = value.Length > 0 ? value : null; break;
                        }
                    }
                    // The name follows the last comma outside the quotes: remove the attributes first.
                    string rest = Attribute.Replace(line, "");
                    int comma = rest.LastIndexOf(',');
                    name = comma >= 0 ? rest[(comma + 1)..].Trim() : null;
                }
                else if (line.StartsWith("#EXTVLCOPT:http-user-agent=", StringComparison.OrdinalIgnoreCase))
                {
                    agent = line["#EXTVLCOPT:http-user-agent=".Length..].Trim();
                }
                else if (line.StartsWith('#'))
                {
                    continue;
                }
                else
                {
                    // An address: ends the entry. Without an #EXTINF line the file name or the address is the name.
                    string label = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(line.Split('?')[0]) : name;
                    list.Add(new M3uEntry(label.Length > 0 ? label : line, line, string.IsNullOrWhiteSpace(group) ? "" : group!, logo, agent));
                    name = group = logo = agent = null;
                }
            }
            return list;
        }

        // Reads a local file or downloads a web address (with the disk copy and the change check). Returns null when neither works.
        public static async Task<string?> LoadTextAsync(string pathOrUrl)
        {
            string text = pathOrUrl.Trim();
            if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                try
                {
                    return await File.ReadAllTextAsync(text);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return null;
                }
            }

            string folder = LocalFolder("Playlists");
            string copy = Path.Combine(folder, HashName(text) + ".m3u");
            string info = copy + ".json";
            string? cached = File.Exists(copy) ? SafeRead(copy) : null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                if (cached != null && File.Exists(info))
                {
                    CacheInfo? saved = JsonSerializer.Deserialize<CacheInfo>(SafeRead(info) ?? "");
                    if (!string.IsNullOrEmpty(saved?.ETag))
                    {
                        request.Headers.TryAddWithoutValidation("If-None-Match", saved.ETag);
                    }
                    if (!string.IsNullOrEmpty(saved?.LastModified))
                    {
                        request.Headers.TryAddWithoutValidation("If-Modified-Since", saved.LastModified);
                    }
                }
                using HttpResponseMessage response = await Http.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.NotModified && cached != null)
                {
                    return cached;
                }
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();
                try
                {
                    Directory.CreateDirectory(folder);
                    File.WriteAllText(copy, body);
                    File.WriteAllText(info, JsonSerializer.Serialize(new CacheInfo
                    {
                        ETag = response.Headers.ETag?.ToString(),
                        LastModified = response.Content.Headers.LastModified?.ToString("R"),
                    }));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // The copy is only a speed-up.
                }
                return body;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                return cached;   // no connection: the last copy, if there is one
            }
        }

        private static string? SafeRead(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private sealed class CacheInfo
        {
            public string? ETag { get; set; }
            public string? LastModified { get; set; }
        }

        // Fetches logos on background threads, a few at a time, and keeps small copies in memory for the rows in view.
        public sealed class LogoCache : IDisposable
        {
            private const int MaxInMemory = 500;
            private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);
            private readonly Dictionary<string, Bitmap?> _images = new();   // null: could not be loaded, do not try again in this run
            private readonly HashSet<string> _pending = new();
            private readonly SemaphoreSlim _gate = new(6);
            private readonly CancellationTokenSource _stop = new();
            private readonly int _width, _height;
            private readonly Action _onLoaded;   // called on a worker thread: the list posts its own redraw

            public LogoCache(int width, int height, Action onLoaded)
            {
                _width = width;
                _height = height;
                _onLoaded = onLoaded;
            }

            // The logo when it is ready; otherwise starts to fetch it and returns null.
            public Bitmap? Get(string url, out bool failed)
            {
                failed = false;
                lock (_images)
                {
                    if (_images.TryGetValue(url, out Bitmap? bitmap))
                    {
                        failed = bitmap == null;
                        return bitmap;
                    }
                    if (_pending.Add(url))
                    {
                        _ = Task.Run(() => FetchAsync(url));
                    }
                }
                return null;
            }

            private async Task FetchAsync(string url)
            {
                Bitmap? result = null;
                try
                {
                    await _gate.WaitAsync(_stop.Token);
                    try
                    {
                        byte[]? data = await ReadOrDownloadAsync(url);
                        if (data != null)
                        {
                            result = Shrink(data);
                        }
                    }
                    finally
                    {
                        _gate.Release();
                    }
                }
                catch (Exception)
                {
                    // A logo that fails shows no picture; nothing else depends on it.
                }
                if (_stop.IsCancellationRequested)
                {
                    result?.Dispose();
                    return;
                }
                lock (_images)
                {
                    if (_images.Count >= MaxInMemory)
                    {
                        // Not disposed: the list may be drawing one of them right now; the garbage collector frees them.
                        _images.Clear();   // simple and rare: rows in view are fetched again from the disk copy
                    }
                    _images[url] = result;
                    _pending.Remove(url);
                }
                _onLoaded();
            }

            private async Task<byte[]?> ReadOrDownloadAsync(string url)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    return null;
                }
                string file = Path.Combine(LocalFolder("LogoCache"), HashName(url) + ".img");
                bool have = File.Exists(file);
                bool fresh = have && DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < MaxAge;
                if (fresh)
                {
                    return await File.ReadAllBytesAsync(file, _stop.Token);
                }
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    if (have)
                    {
                        request.Headers.IfModifiedSince = new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero);
                    }
                    using HttpResponseMessage response = await Http.SendAsync(request, _stop.Token);
                    if (response.StatusCode == HttpStatusCode.NotModified && have)
                    {
                        File.SetLastWriteTimeUtc(file, DateTime.UtcNow);   // checked just now: unchanged
                        return await File.ReadAllBytesAsync(file, _stop.Token);
                    }
                    response.EnsureSuccessStatusCode();
                    byte[] data = await response.Content.ReadAsByteArrayAsync(_stop.Token);
                    if (data.Length > 0 && data.Length < 4_000_000)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                        await File.WriteAllBytesAsync(file, data, _stop.Token);
                    }
                    return data;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
                {
                    return have ? await File.ReadAllBytesAsync(file, _stop.Token) : null;   // offline: the old copy
                }
            }

            // A copy that fits the logo box, so that 500 of them in memory stay small. Formats GDI+ cannot read (SVG, WebP) give null.
            private Bitmap? Shrink(byte[] data)
            {
                try
                {
                    using var stream = new MemoryStream(data);
                    using Image source = Image.FromStream(stream);
                    double scale = Math.Min((double)_width / source.Width, (double)_height / source.Height);
                    int w = Math.Max(1, (int)(source.Width * scale)), h = Math.Max(1, (int)(source.Height * scale));
                    var bitmap = new Bitmap(w, h);
                    using Graphics g = Graphics.FromImage(bitmap);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, 0, 0, w, h);
                    return bitmap;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public void Dispose()
            {
                _stop.Cancel();
                lock (_images)
                {
                    foreach (Bitmap? bitmap in _images.Values)
                    {
                        bitmap?.Dispose();
                    }
                    _images.Clear();
                }
            }
        }
    }
}
