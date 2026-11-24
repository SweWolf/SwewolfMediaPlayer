using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SwewolfMediaPlayer
{
    // Animated WebP. LibVLC 3.0 shows a WebP as one still picture, so FFmpeg turns the animation into a GIF (a palette of its own
    // for every frame), which is cached and played instead of the file, the way a GIF is played (pause, seek and frame stepping
    // work for GIF; they froze for a video made from the WebP).
    // Transparent parts become white. Without FFmpeg, or when the conversion fails, null is returned and the file is
    // opened as before, silently.
    // History: 2026-11-24 first version (an MP4; paused seeks froze), then a GIF.
    internal static class WebpAnimation
    {
        private static string CacheFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "WebpCache");

        /// <summary>True when the file is a WebP that holds an animation (VP8X chunk with the animation flag), by content, not extension.</summary>
        internal static bool IsAnimated(string path)
        {
            try
            {
                byte[] b = new byte[30];
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Read(b, 0, b.Length) < b.Length)
                {
                    return false;
                }
                return b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' && b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P'
                    && b[12] == 'V' && b[13] == 'P' && b[14] == '8' && b[15] == 'X' && (b[20] & 0x02) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // LibVLC freezes on pause + seek in a clip shorter than a few seconds (found with a 0.84 s animation: the picture moved once or
        // twice and then stayed), so a short animation is repeated inside the GIF until it is about this long.
        private const int MinMs = 4000;

        // Frames and total play time of an animated WebP, from its ANMF chunks (the duration is 3 bytes at offset 12 of the chunk data).
        private static (int Frames, long Ms) Measure(string path)
        {
            int frames = 0;
            long ms = 0;
            try
            {
                byte[] b = File.ReadAllBytes(path);
                int pos = 12;
                while (pos + 8 <= b.Length)
                {
                    long size = BitConverter.ToUInt32(b, pos + 4);
                    if (b[pos] == 'A' && b[pos + 1] == 'N' && b[pos + 2] == 'M' && b[pos + 3] == 'F' && pos + 8 + 16 <= b.Length)
                    {
                        frames++;
                        ms += Math.Max(b[pos + 8 + 12] | (b[pos + 8 + 13] << 8) | (b[pos + 8 + 14] << 16), 10);
                    }
                    pos += (int)(8 + size + (size & 1));
                }
            }
            catch (Exception)
            {
                // Not measured: the animation is not repeated.
            }
            return (frames, ms);
        }

        /// <summary>The cached GIF for an animated WebP (made when needed), or null.</summary>
        internal static string? GetVideo(string path)
        {
            string? ffmpeg = MediaPlayerForm.FindFfmpeg();
            if (ffmpeg == null || !IsAnimated(path))
            {
                return null;
            }
            try
            {
                FileInfo info = new(path);
                string key = $"v3|{info.FullName.ToLowerInvariant()}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
                string name = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(key)))[..16] + ".gif";
                string target = Path.Combine(CacheFolder, name);
                if (File.Exists(target))
                {
                    return target;
                }
                Directory.CreateDirectory(CacheFolder);
                string temp = target + ".tmp.gif";

                ProcessStartInfo psi = new(ffmpeg)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };
                (int frames, long totalMs) = Measure(path);
                int repeats = frames > 0 && totalMs > 0 && totalMs < MinMs ? (int)Math.Min((MinMs + totalMs - 1) / totalMs, 100) : 1;
                string loop = repeats > 1 ? $"loop=loop={repeats - 1}:size={frames}:start=0," : "";
                foreach (string a in new[] { "-hide_banner", "-v", "error", "-y", "-i", path, "-vf",
                    loop + "split[a][b];[a]drawbox=c=white:t=fill[bg];[bg][b]overlay=format=auto,split[s0][s1];[s0]palettegen=stats_mode=single[p];[s1][p]paletteuse=new=1",
                    "-fps_mode", "vfr", "-loop", "0", temp })
                {
                    psi.ArgumentList.Add(a);
                }
                using Process? p = Process.Start(psi);
                if (p == null)
                {
                    return null;
                }
                p.ErrorDataReceived += (_, _) => { };
                p.BeginErrorReadLine();
                if (!p.WaitForExit(60000))
                {
                    try { p.Kill(true); } catch (Exception) { }
                    TryDelete(temp);
                    return null;
                }
                if (p.ExitCode != 0 || !File.Exists(temp))
                {
                    TryDelete(temp);
                    return null;
                }
                File.Move(temp, target, true);
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void TryDelete(string file)
        {
            try { File.Delete(file); } catch (Exception) { }
        }
    }
}
