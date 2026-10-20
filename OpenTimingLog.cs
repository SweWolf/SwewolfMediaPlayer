// OpenTimingLog.cs: a small debug log that shows where the time goes when a file is opened, kept in
// %AppData%\SweWolfSoftware\SwewolfMediaPlayer\open-timing.log. Every line has the time since the
// program started, so a slow start can be read from the gaps. The file is cut down when it gets big.
//
// History:
//   2026-10-19  First version: to find out why opening a video sometimes takes almost 30 seconds.
using System.Diagnostics;

namespace SwewolfMediaPlayer
{
    internal static class OpenTimingLog
    {
        private const long MaxBytes = 256 * 1024;
        private static readonly object Gate = new();
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "open-timing.log");
        private static bool _started;

        // Milliseconds since the process was created by Windows (includes the .NET runtime start-up).
        private static long SinceProcessStartMs()
        {
            try
            {
                return (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    if (!_started)
                    {
                        _started = true;
                        TrimIfBig();
                        File.AppendAllText(FilePath, $"{Environment.NewLine}==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} new run, process {Environment.ProcessId} ===={Environment.NewLine}");
                    }
                    File.AppendAllText(FilePath,
                        $"{DateTime.Now:HH:mm:ss.fff}  +{SinceProcessStartMs(),6} ms  [t{Environment.CurrentManagedThreadId}]  {message}{Environment.NewLine}");
                }
            }
            catch (Exception)
            {
                // A log must never break the player.
            }
        }

        private static void TrimIfBig()
        {
            try
            {
                FileInfo info = new(FilePath);
                if (info.Exists && info.Length > MaxBytes)
                {
                    string[] lines = File.ReadAllLines(FilePath);
                    File.WriteAllLines(FilePath, lines.Skip(lines.Length / 2));
                }
            }
            catch (Exception)
            {
            }
        }

        // For the lines that describe the file: its size and the drive it is on.
        public static string Describe(string path, bool isStream)
        {
            if (isStream)
            {
                return $"stream {path}";
            }
            try
            {
                FileInfo info = new(path);
                string drive = Path.GetPathRoot(path) ?? "?";
                string type = "?";
                try
                {
                    type = new DriveInfo(drive).DriveType.ToString();
                }
                catch (Exception)
                {
                }
                return $"file \"{path}\", {info.Length / (1024.0 * 1024.0):F1} MB, drive {drive} ({type})";
            }
            catch (Exception ex)
            {
                return $"file \"{path}\" ({ex.Message})";
            }
        }
    }
}
