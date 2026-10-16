// SidSongLengths.cs: finds how long a SID (Commodore 64 music) tune plays, from HVSC's Songlengths
// database (Songlengths.md5, a text file: "<md5 of the .sid file>=<length of tune 1> <length of tune 2> ...").
// A SID file does not say how long it plays, and the length cannot be calculated from it.
//
// Where the database comes from, in this order:
//   1. The user's own HVSC collection: a DOCUMENTS\Songlengths.md5 in a folder above the .sid file.
//   2. A shared copy, %AppData%\SweWolfSoftware\Shared\SidSongLengths\Songlengths.md5. Apps that
//      use this file all share it, so it is downloaded once per computer. It is downloaded the
//      first time a SID file is played (nothing is shipped with the program: the license terms of
//      HVSC's data are not clear) and refreshed in the background when it is older than 90 days.
// A tune that is not in the database, or any problem (no internet, a damaged file), gives null:
// the caller shows no length. Problems are only written to log.txt in the shared folder.
//
// Self-contained: to reuse it in another app, copy this file and change the namespace (the log
// lines use the program's name). Keep the shared folder name as it is, so the apps share the file.
//
// History:
//   2026-10-16  First version: local HVSC first, then the shared copy (downloaded on first use).

using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace SwewolfMediaPlayer
{
    internal static class SidSongLengths
    {
        private const string DownloadUrl = "https://hvsc.c64.org/download/C64Music/DOCUMENTS/Songlengths.md5";
        private const string FileName = "Songlengths.md5";
        private const int MaxAgeDays = 90;
        private const int MaxSidBytes = 1024 * 1024;   // a real SID file is far smaller than this

        private static Task<bool>? _download;   // one download attempt per run, whatever the result

        // %AppData%\SweWolfSoftware\Shared\SidSongLengths
        public static string SharedFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "Shared", "SidSongLengths");

        private static string SharedFile => Path.Combine(SharedFolder, FileName);

        // The length in milliseconds of the tune that starts first in the file, or null if it is
        // not known. Waits for the first download if there is no database yet, so call it without
        // blocking the window.
        public static async Task<long?> FindLengthMsAsync(string sidFile)
        {
            try
            {
                byte[]? data = await Task.Run(() => ReadSidFile(sidFile));
                if (data == null || !TryReadHeader(data, out int startSong))
                {
                    return null;
                }
                string hash = Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();

                string? database = FindLocalDatabase(sidFile);
                if (database == null)
                {
                    if (!File.Exists(SharedFile))
                    {
                        if (!await DownloadOnceAsync())
                        {
                            return null;
                        }
                    }
                    else if (DateTime.UtcNow - File.GetLastWriteTimeUtc(SharedFile) > TimeSpan.FromDays(MaxAgeDays))
                    {
                        _ = DownloadOnceAsync();   // refresh in the background, use the old file now
                    }
                    database = SharedFile;
                }

                return await Task.Run(() => Lookup(database, hash, startSong));
            }
            catch (Exception ex)
            {
                Log("Length lookup failed: " + ex.Message);
                return null;
            }
        }

        private static byte[]? ReadSidFile(string path)
        {
            var info = new FileInfo(path);
            return info.Exists && info.Length <= MaxSidBytes ? File.ReadAllBytes(path) : null;
        }

        // A PSID or RSID header: the number of the tune that starts first is at offset 0x10.
        private static bool TryReadHeader(byte[] data, out int startSong)
        {
            startSong = 1;
            if (data.Length < 0x12 || data[1] != 'S' || data[2] != 'I' || data[3] != 'D' || (data[0] != 'P' && data[0] != 'R'))
            {
                return false;
            }
            startSong = Math.Max((data[0x10] << 8) | data[0x11], 1);
            return true;
        }

        // The collection's own DOCUMENTS folder, found by looking in each folder above the file.
        private static string? FindLocalDatabase(string sidFile)
        {
            string? folder = Path.GetDirectoryName(Path.GetFullPath(sidFile));
            for (int level = 0; folder != null && level < 12; level++)
            {
                string candidate = Path.Combine(folder, "DOCUMENTS", FileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                folder = Path.GetDirectoryName(folder);
            }
            return null;
        }

        private static long? Lookup(string databasePath, string hash, int startSong)
        {
            string text = Encoding.Latin1.GetString(File.ReadAllBytes(databasePath));
            int at = text.IndexOf("\n" + hash + "=", StringComparison.Ordinal);
            if (at < 0)
            {
                return null;
            }

            int start = at + 1 + hash.Length + 1;
            int end = text.IndexOfAny(new[] { '\r', '\n' }, start);
            string[] lengths = text[start..(end < 0 ? text.Length : end)].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return startSong <= lengths.Length ? ParseLength(lengths[startSong - 1]) : null;
        }

        // "3:21", "4:33.108" or "1:02:03".
        private static long? ParseLength(string text)
        {
            string[] parts = text.Split(':');
            int hours = 0;
            if (parts.Length < 2 || parts.Length > 3
                || !double.TryParse(parts[^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds)
                || !int.TryParse(parts[^2], out int minutes)
                || (parts.Length == 3 && !int.TryParse(parts[0], out hours)))
            {
                return null;
            }
            return (long)Math.Round(((hours * 60L + minutes) * 60 + seconds) * 1000);
        }

        private static Task<bool> DownloadOnceAsync() => _download ??= Task.Run(DownloadAsync);

        // Downloads to a temporary file and moves it into place when it is complete and looks right,
        // so another app (or a crash) never leaves a half file behind.
        private static async Task<bool> DownloadAsync()
        {
            string temp = Path.Combine(SharedFolder, FileName + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                Directory.CreateDirectory(SharedFolder);
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("SweWolfSoftware-SidSongLengths/1.0");
                using HttpResponseMessage response = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using (FileStream file = File.Create(temp))
                {
                    await response.Content.CopyToAsync(file);
                }

                if (!LooksLikeDatabase(temp))
                {
                    throw new InvalidDataException("The downloaded file is not a song length database.");
                }
                File.Move(temp, SharedFile, true);
                Log("Downloaded " + FileName + ".");
                return true;
            }
            catch (Exception ex)
            {
                Log("Download failed: " + ex.Message);
                return false;
            }
            finally
            {
                try
                {
                    File.Delete(temp);
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool LooksLikeDatabase(string path)
        {
            using var reader = new StreamReader(path, Encoding.Latin1);
            return new FileInfo(path).Length > 100_000 && reader.ReadLine()?.Trim() == "[Database]";
        }

        private static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(SharedFolder);
                string log = Path.Combine(SharedFolder, "log.txt");
                if (File.Exists(log) && new FileInfo(log).Length > 100_000)
                {
                    File.Delete(log);
                }
                string app = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
                File.AppendAllText(log, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {app}: {message}{Environment.NewLine}");
            }
            catch (Exception)
            {
                // A log that cannot be written is not worth a message either.
            }
        }
    }
}
