// ExactFrames.cs: pictures of a video with their exact times, decoded by FFmpeg, for stepping frame by frame while paused.
//
// LibVLC cannot do this: a seek while paused lands up to a few frames away from the time asked for, its own "next frame" step now and then
// skips a frame, and it never says which frame is on the screen (measured 2026-11-24 with a test video that has the frame number drawn on it).
// FFmpeg decodes a short window of frames (here as small JPEG pictures, kept in memory) and its "showinfo" filter says the exact presentation
// time of each one, so the player can show frame N+1 or N-1 of a known frame, and say which time it is: the time FFmpeg uses when it cuts.
//
// The same decoding is used to find out which frame LibVLC is showing right now: a small grey picture of what LibVLC shows (a snapshot) is
// compared with the same kind of picture of every frame in the window (FindClosest).
//
// Self-contained: to reuse it in another app, change the namespace. Needs ffmpeg.exe (the caller looks for it).
//
// History:
//   2026-11-26  First version.

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SwewolfMediaPlayer
{
    internal sealed class ExactFrame
    {
        public ExactFrame(double ptsMs, byte[] jpeg)
        {
            PtsMs = ptsMs;
            Jpeg = jpeg;
        }

        public double PtsMs { get; }
        public byte[] Jpeg { get; }
        private byte[]? _signature;

        // A small grey picture (32 x 18), to compare frames with a snapshot of the player.
        public byte[] Signature => _signature ??= ExactFrames.SignatureOf(Jpeg);
    }

    internal static class ExactFrames
    {
        private const int SignatureWidth = 32;
        private const int SignatureHeight = 18;

        // The frames from fromSec to toSec (seconds from the start of the file), scaled to width x height, in the order they are shown.
        // null when FFmpeg failed or was stopped. Takes a fraction of a second for a short window of a 1080p video; call it on another thread.
        public static List<ExactFrame>? Load(string ffmpeg, string file, double fromSec, double toSec, int width, int height, CancellationToken cancel)
        {
            fromSec = Math.Max(fromSec, 0);
            // FFmpeg before 5.1 has no -fps_mode (it was -vsync): try the new one first.
            return Run(ffmpeg, file, fromSec, toSec, width, height, "-fps_mode", "passthrough", cancel)
                ?? Run(ffmpeg, file, fromSec, toSec, width, height, "-vsync", "0", cancel);
        }

        private static List<ExactFrame>? Run(string ffmpeg, string file, double fromSec, double toSec, int width, int height,
            string syncOption, string syncValue, CancellationToken cancel)
        {
            ProcessStartInfo info = new(ffmpeg)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            string seconds(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
            foreach (string argument in new[] { "-hide_banner", "-nostats", "-ss", seconds(fromSec), "-t", seconds(Math.Max(toSec - fromSec, 0.05)),
                "-i", file, "-an", "-sn", "-vf", $"scale={width}:{height}:flags=bicubic,setsar=1,showinfo", syncOption, syncValue,
                "-f", "image2pipe", "-c:v", "mjpeg", "-q:v", "2", "-pix_fmt", "yuvj420p", "-" })
            {
                info.ArgumentList.Add(argument);
            }

            using Process? process = Process.Start(info);
            if (process == null)
            {
                return null;
            }
            using CancellationTokenRegistration stop = cancel.Register(() =>
            {
                try
                {
                    process.Kill(true);
                }
                catch (InvalidOperationException)
                {
                    // It ended just now.
                }
            });

            MemoryStream pictures = new();
            Task copy = process.StandardOutput.BaseStream.CopyToAsync(pictures);
            Task<string> log = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                try
                {
                    process.Kill(true);
                }
                catch (InvalidOperationException)
                {
                    // It ended just now.
                }
                return null;
            }
            copy.Wait();
            if (cancel.IsCancellationRequested || process.ExitCode != 0)
            {
                return null;
            }

            List<double> times = new();
            foreach (Match match in Regex.Matches(log.Result, @"showinfo.*?pts_time:(?<t>-?\d+(\.\d+)?)"))
            {
                times.Add(fromSec + double.Parse(match.Groups["t"].Value, CultureInfo.InvariantCulture));
            }

            List<byte[]> jpegs = SplitJpegs(pictures.GetBuffer(), (int)pictures.Length);
            int count = Math.Min(times.Count, jpegs.Count);
            List<ExactFrame> frames = new(count);
            for (int i = 0; i < count; i++)
            {
                frames.Add(new ExactFrame(times[i] * 1000, jpegs[i]));
            }
            return frames;
        }

        // The pictures come one after the other. A JPEG ends with FF D9, and that pair cannot be inside the picture data (a FF there is always
        // followed by 00), so it marks the end; the next one starts with FF D8.
        private static List<byte[]> SplitJpegs(byte[] data, int length)
        {
            List<byte[]> result = new();
            int start = 0;
            for (int i = 0; i + 1 < length; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xD9 && (i + 3 >= length || (data[i + 2] == 0xFF && data[i + 3] == 0xD8)))
                {
                    int end = i + 2;
                    byte[] jpeg = new byte[end - start];
                    Buffer.BlockCopy(data, start, jpeg, 0, jpeg.Length);
                    result.Add(jpeg);
                    start = end;
                    i = end - 1;
                }
            }
            return result;
        }

        // A grey picture of 32 x 18 values, the same for any picture of the same view whatever its size.
        public static byte[] SignatureOf(byte[] jpeg)
        {
            using MemoryStream stream = new(jpeg);
            return SignatureOf(stream);
        }

        public static byte[] SignatureOf(Stream picture)
        {
            using Image source = Image.FromStream(picture);
            using Bitmap small = new(SignatureWidth, SignatureHeight, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, SignatureWidth, SignatureHeight));
            }

            BitmapData data = small.LockBits(new Rectangle(0, 0, SignatureWidth, SignatureHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                byte[] grey = new byte[SignatureWidth * SignatureHeight];
                byte[] row = new byte[Math.Abs(data.Stride)];
                for (int y = 0; y < SignatureHeight; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                    for (int x = 0; x < SignatureWidth; x++)
                    {
                        grey[y * SignatureWidth + x] = (byte)((row[x * 3] + row[x * 3 + 1] + row[x * 3 + 2]) / 3);
                    }
                }
                return grey;
            }
            finally
            {
                small.UnlockBits(data);
            }
        }

        // The mean difference between two signatures.
        public static double Difference(byte[] a, byte[] b)
        {
            long sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += Math.Abs(a[i] - b[i]);
            }
            return sum / (double)a.Length;
        }

        // Which frame of the window looks like the picture of the player (its signature)? The frames that are as close as the closest one
        // (within 0.7 grey levels: a still picture gives many) compete, and the one nearest in time to the estimate wins. -1 for an empty window.
        public static int FindClosest(List<ExactFrame> frames, byte[] snapshotSignature, double estimateMs)
        {
            if (frames.Count == 0)
            {
                return -1;
            }

            double[] differences = frames.Select(f => Difference(f.Signature, snapshotSignature)).ToArray();
            double best = differences.Min();
            int chosen = -1;
            for (int i = 0; i < frames.Count; i++)
            {
                if (differences[i] <= best + 0.7 && (chosen < 0 || Math.Abs(frames[i].PtsMs - estimateMs) < Math.Abs(frames[chosen].PtsMs - estimateMs)))
                {
                    chosen = i;
                }
            }
            return chosen;
        }
    }
}
