// FrameThumbnailer.cs: small preview pictures of a video file, taken with FFmpeg.
//
// FFmpeg writes one JPEG picture to its standard output, so it goes straight into memory and
// nothing is saved on disk. Any problem (no FFmpeg, a file FFmpeg cannot read, a canceled
// request) gives null: a missing preview is not worth an error message.
//
// Self-contained: to reuse it in another app, change the namespace.
//
// History:
//   2026-10-16  First version: nearest-keyframe pictures (fast), cached by time bucket.

using System.Diagnostics;

namespace SwewolfMediaPlayer
{
    internal sealed class FrameThumbnailer : IDisposable
    {
        private const int MaxCached = 400;

        private readonly string _ffmpeg;
        private readonly string _file;
        private readonly int _width;
        private readonly long _bucketMs;
        private readonly Dictionary<long, Image> _cache = new();
        private readonly CancellationTokenSource _disposed = new();

        // width is the picture width in pixels; lengthMs sets how close two requests must be to
        // share a picture (the seek bar has only a few hundred distinct positions anyway).
        public FrameThumbnailer(string ffmpegPath, string filePath, int width, long lengthMs)
        {
            _ffmpeg = ffmpegPath;
            _file = filePath;
            _width = Math.Max(width, 32);
            _bucketMs = Math.Max(1000, lengthMs / 300);
        }

        // The cached picture for this time, or null. The picture belongs to the thumbnailer.
        public Image? TryGetCached(long timeMs)
        {
            _cache.TryGetValue(timeMs / _bucketMs, out Image? image);
            return image;
        }

        // Takes the picture at (or just before) this time. null when it failed or was canceled.
        public async Task<Image?> GetAsync(long timeMs, CancellationToken cancel)
        {
            long key = timeMs / _bucketMs;
            if (_cache.TryGetValue(key, out Image? cached))
            {
                return cached;
            }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancel, _disposed.Token);
                byte[]? bytes = await Task.Run(() => RunFfmpegAsync(key * _bucketMs, linked.Token), linked.Token);
                if (bytes == null || bytes.Length == 0 || linked.IsCancellationRequested)
                {
                    return null;
                }

                // A Bitmap made from a stream needs the stream for its whole life: copy it.
                using var stream = new MemoryStream(bytes);
                using var decoded = Image.FromStream(stream);
                var image = new Bitmap(decoded);

                if (_cache.Count >= MaxCached)
                {
                    _cache.Clear();
                }
                _cache[key] = image;
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // -ss before -i makes FFmpeg jump to the keyframe before the time and decode only that
        // picture, which is much faster than an exact frame.
        private async Task<byte[]?> RunFfmpegAsync(long timeMs, CancellationToken cancel)
        {
            var info = new ProcessStartInfo(_ffmpeg)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            string seconds = (timeMs / 1000.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            foreach (string argument in new[]
            {
                "-nostdin", "-loglevel", "quiet", "-noaccurate_seek", "-ss", seconds, "-i", _file,
                "-an", "-sn", "-frames:v", "1", "-vf", $"scale={_width}:-2", "-q:v", "4", "-f", "image2pipe", "-c:v", "mjpeg", "pipe:1"
            })
            {
                info.ArgumentList.Add(argument);
            }

            using Process? process = Process.Start(info);
            if (process == null)
            {
                return null;
            }

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            timeout.CancelAfter(15000);
            using CancellationTokenRegistration kill = timeout.Token.Register(() =>
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                }
            });

            using var output = new MemoryStream();
            _ = process.StandardError.ReadToEndAsync();
            await process.StandardOutput.BaseStream.CopyToAsync(output, CancellationToken.None);
            await process.WaitForExitAsync(CancellationToken.None);
            return timeout.IsCancellationRequested || process.ExitCode != 0 ? null : output.ToArray();
        }

        public void Dispose()
        {
            _disposed.Cancel();
            foreach (Image image in _cache.Values)
            {
                image.Dispose();
            }
            _cache.Clear();
        }
    }
}
