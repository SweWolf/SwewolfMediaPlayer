// StreamRecorder.cs: records a live stream into a .ts file with FFmpeg, and converts the .ts file to MP4 afterwards.
//
// The same way as "Enable Watching While Downloading" in FFmpegAssistant: FFmpeg copies the streams (-c copy, no re-encoding) into
// an MPEG-TS file, which can be played while it grows and never gets damaged when the program stops it (an MP4 needs a proper end).
// FFmpeg is stopped by sending it "q" on its standard input, so it finishes the packet it is writing and closes the file cleanly; if it
// does not stop within 15 seconds it is killed. Subtitle and data streams are left out: they cannot go into the MP4 later.
//
// Self-contained: to reuse it in another app, change the namespace.
//
// History:
//   2026-11-12  First version.
//   2026-11-12  FFmpeg runs in a Windows job object that ends it with this program (before, a killed player left FFmpeg recording on its own).

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SwewolfMediaPlayer
{
    internal sealed class StreamRecorder : IDisposable
    {
        private readonly string _ffmpeg;
        private readonly string _userAgent;
        private readonly List<string> _lastLines = new();   // the last lines FFmpeg wrote, for the error message
        private Process? _process;

        public string Url { get; }
        public string TsPath { get; }
        public DateTime StartedAt { get; private set; }

        // Raised (on a worker thread) when FFmpeg ends by itself: the stream ended or broke, or FFmpeg could not start the recording.
        public event Action? Exited;

        // True when the player itself writes the .ts file (LibVLC, from the connection it plays through) and FFmpeg is not involved until the file is
        // converted: for streams that do not allow a second connection. Start and Stop then only keep the time and the state.
        public bool InPlayer { get; }
        private bool _playerRunning;

        public bool IsRunning => InPlayer ? _playerRunning : _process is { HasExited: false };

        public StreamRecorder(string ffmpegPath, string url, string? userAgent, string tsPath)
        {
            _ffmpeg = ffmpegPath;
            Url = url;
            _userAgent = userAgent ?? "";
            TsPath = tsPath;
        }

        private StreamRecorder(string url, string tsPath)
        {
            _ffmpeg = "";
            _userAgent = "";
            Url = url;
            TsPath = tsPath;
            InPlayer = true;
        }

        public static StreamRecorder ForPlayer(string url, string tsPath) => new(url, tsPath);

        // The last thing FFmpeg said (for a message when nothing was recorded).
        public string LastMessage
        {
            get
            {
                lock (_lastLines)
                {
                    return _lastLines.Count > 0 ? _lastLines[^1] : "";
                }
            }
        }

        public bool Start(out string error)
        {
            error = "";
            if (InPlayer)
            {
                StartedAt = DateTime.Now;
                _playerRunning = true;
                return true;
            }
            try
            {
                var info = new ProcessStartInfo(_ffmpeg)
                {
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                foreach (string a in new[] { "-hide_banner", "-loglevel", "warning", "-nostats", "-y" })
                {
                    info.ArgumentList.Add(a);
                }
                if (_userAgent.Length > 0)
                {
                    info.ArgumentList.Add("-user_agent");
                    info.ArgumentList.Add(_userAgent);
                }
                // A short break in the connection should not end the recording (only for http: other protocols do not know the options).
                if (Uri.TryCreate(Url, UriKind.Absolute, out Uri? uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    foreach (string a in new[] { "-reconnect", "1", "-reconnect_streamed", "1", "-reconnect_delay_max", "5" })
                    {
                        info.ArgumentList.Add(a);
                    }
                }
                foreach (string a in new[] { "-i", Url, "-sn", "-dn", "-c", "copy", "-f", "mpegts", TsPath })
                {
                    info.ArgumentList.Add(a);
                }

                _process = new Process { StartInfo = info, EnableRaisingEvents = true };
                DataReceivedEventHandler remember = (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        lock (_lastLines)
                        {
                            _lastLines.Add(e.Data.Trim());
                            if (_lastLines.Count > 20)
                            {
                                _lastLines.RemoveAt(0);
                            }
                        }
                    }
                };
                _process.ErrorDataReceived += remember;
                _process.OutputDataReceived += remember;
                _process.Exited += (_, _) => Exited?.Invoke();
                _process.Start();
                KillOnCloseJob.Add(_process);   // FFmpeg must not go on recording when this program is closed or killed
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
                StartedAt = DateTime.Now;
                return true;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
            {
                error = ex.Message;
                return false;
            }
        }

        // Asks FFmpeg to finish ("q") and waits until it has closed the file.
        public async Task StopAsync()
        {
            _playerRunning = false;
            Process? process = _process;
            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.StandardInput.Write("q");
                    process.StandardInput.Flush();
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                // It has ended already.
            }

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);   // a stuck connection: the .ts file is still playable up to the last packet
                    await process.WaitForExitAsync();
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                }
            }
        }

        // Copies the .ts file into an MP4 (no re-encoding). Returns null when it worked, otherwise what went wrong.
        public static async Task<string?> ConvertToMp4Async(string ffmpegPath, string tsPath, string mp4Path)
        {
            var info = new ProcessStartInfo(ffmpegPath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (string a in new[] { "-hide_banner", "-loglevel", "error", "-y", "-i", tsPath, "-c", "copy", "-movflags", "+faststart", mp4Path })
            {
                info.ArgumentList.Add(a);
            }

            try
            {
                using var process = Process.Start(info)!;
                Task<string> errors = process.StandardError.ReadToEndAsync();
                _ = process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                string text = (await errors).Trim();
                if (process.ExitCode == 0 && File.Exists(mp4Path) && new FileInfo(mp4Path).Length > 0)
                {
                    return null;
                }
                TryDelete(mp4Path);   // a half-written MP4 is of no use; the .ts file is kept
                return text.Length > 0 ? text.Split('\n')[^1].Trim() : $"FFmpeg ended with code {process.ExitCode}.";
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
            {
                TryDelete(mp4Path);
                return ex.Message;
            }
        }

        // Moves a file to the Recycle Bin (so it can be restored). False when that did not work: the file is then still there.
        public static bool TryRecycle(string path)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                return !File.Exists(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException)
            {
                return false;
            }
        }

        public static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        public void Dispose()
        {
            _process?.Dispose();
            _process = null;
        }
    }
}

namespace SwewolfMediaPlayer
{
    // A Windows job object with "kill on close": every process in it ends when this program ends, however that happens (closed, crashed, killed
    // in the Task Manager). Without it a recording FFmpeg went on writing to the disk after a stuck player was closed.
    internal static class KillOnCloseJob
    {
        private static IntPtr _job;
        private static bool _failed;

        public static void Add(Process process)
        {
            try
            {
                if (_job == IntPtr.Zero && !_failed)
                {
                    _job = CreateJobObject(IntPtr.Zero, null);
                    var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                    info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
                    int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
                    IntPtr buffer = Marshal.AllocHGlobal(size);
                    try
                    {
                        Marshal.StructureToPtr(info, buffer, false);
                        if (_job == IntPtr.Zero || !SetInformationJobObject(_job, JobObjectExtendedLimitInformation, buffer, (uint)size))
                        {
                            _failed = true;
                            _job = IntPtr.Zero;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                if (_job != IntPtr.Zero)
                {
                    AssignProcessToJobObject(_job, process.Handle);   // if it fails, nothing is lost but this safety net
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or DllNotFoundException)
            {
            }
        }

        private const int JobObjectExtendedLimitInformation = 9;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr attributes, string? name);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(IntPtr job, int infoType, IntPtr info, uint size);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
