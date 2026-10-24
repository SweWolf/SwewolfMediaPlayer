// SidTuneTitles.cs: the texts of a SID (Commodore 64 music) file. Two sources:
//   - The file's own header: one title, author and "released" line for the whole file (ReadHeader).
//   - HVSC's STIL (DOCUMENTS\STIL.txt, the "SID Tune Information List"): a title, artist and comment for
//     single tunes, mostly for notable ones (covers and the like), so a file with 11 tunes may have names for
//     only some of them (FindTunes). The user's own HVSC collection is needed: STIL.txt is looked for in a
//     DOCUMENTS folder above the .sid file, and an entry is found by the file's path inside the collection
//     (e.g. /GAMES/A-F/Alice_in_Wonderland.sid), not by checksum. Nothing is downloaded.
// A file without an entry, a missing STIL.txt or any problem gives null / nothing: the caller shows "Tune N".
//
// STIL.txt is Latin-1 with CRLF. An entry is the path line, then (optionally) general lines, then one block per
// tune that starts with "(#3)". Fields (right-aligned labels): TITLE, ARTIST, NAME, AUTHOR, COMMENT; a long value
// goes on in the lines after it, indented. Entries are separated by an empty line.
//
// Self-contained: to reuse it in another app, copy this file and change the namespace.
//
// History:
//   2026-10-23  First version.

using System.Text;
using System.Text.RegularExpressions;

namespace SwewolfMediaPlayer
{
    // The STIL texts of one tune (null for what the entry does not say).
    internal sealed record SidTuneInfo(string? Title, string? Artist, string? Name, string? Comment);

    // The header of a SID file.
    internal sealed record SidHeader(string Title, string Author, string Released);

    internal static class SidTuneTitles
    {
        private static readonly Regex TuneLine = new(@"^\(#(\d+)\)\s*$", RegexOptions.Compiled);
        private static readonly Regex FieldLine = new(@"^\s*(TITLE|ARTIST|NAME|AUTHOR|COMMENT):\s?(.*)$", RegexOptions.Compiled);

        private static readonly object CacheLock = new();
        private static string? _cachedPath;
        private static DateTime _cachedStamp;
        private static string? _cachedText;

        // The title, author and released line from the file header, or null if it is not a PSID / RSID file.
        public static SidHeader? ReadHeader(string sidFile)
        {
            try
            {
                using FileStream file = File.OpenRead(sidFile);
                byte[] data = new byte[0x76];
                int read = file.Read(data, 0, data.Length);
                if (read < data.Length || data[1] != 'S' || data[2] != 'I' || data[3] != 'D' || (data[0] != 'P' && data[0] != 'R'))
                {
                    return null;
                }
                return new SidHeader(Field(data, 0x16), Field(data, 0x36), Field(data, 0x56));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Field(byte[] data, int offset)
        {
            int end = Array.IndexOf(data, (byte)0, offset, 32);
            return Encoding.Latin1.GetString(data, offset, (end < 0 ? offset + 32 : end) - offset).Trim();
        }

        // The STIL texts by tune number (from 1; 0 holds lines for the whole file), or null when there is no STIL.txt
        // above the file or it has no entry for it.
        public static Dictionary<int, SidTuneInfo>? FindTunes(string sidFile)
        {
            try
            {
                string full = Path.GetFullPath(sidFile);
                string? folder = Path.GetDirectoryName(full);
                for (int level = 0; folder != null && level < 12; level++)
                {
                    string stil = Path.Combine(folder, "DOCUMENTS", "STIL.txt");
                    if (File.Exists(stil))
                    {
                        string key = "/" + Path.GetRelativePath(folder, full).Replace('\\', '/');
                        return ParseEntry(LoadText(stil), key);
                    }
                    folder = Path.GetDirectoryName(folder);
                }
            }
            catch (Exception)
            {
                // A damaged or locked file is not worth a message: the tunes are just not named.
            }
            return null;
        }

        // The text is kept for the next file (it is several MB), and read again if STIL.txt has changed.
        private static string LoadText(string path)
        {
            lock (CacheLock)
            {
                DateTime stamp = File.GetLastWriteTimeUtc(path);
                if (_cachedText == null || _cachedPath != path || _cachedStamp != stamp)
                {
                    _cachedText = Encoding.Latin1.GetString(File.ReadAllBytes(path));
                    _cachedPath = path;
                    _cachedStamp = stamp;
                }
                return _cachedText;
            }
        }

        private static Dictionary<int, SidTuneInfo>? ParseEntry(string text, string key)
        {
            int at = text.IndexOf("\n" + key + "\r", StringComparison.OrdinalIgnoreCase);
            if (at < 0)
            {
                return null;
            }

            int start = text.IndexOf('\n', at + 1) + 1;
            int end = text.IndexOf("\r\n\r\n", start, StringComparison.Ordinal);
            string entry = text[start..(end < 0 ? text.Length : end)];

            var fields = new Dictionary<int, Dictionary<string, string>>();
            int tune = 0;
            string? current = null;   // the field the following indented lines belong to; null after a repeated field
            foreach (string raw in entry.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                Match tuneMatch = TuneLine.Match(line);
                if (tuneMatch.Success)
                {
                    tune = int.Parse(tuneMatch.Groups[1].Value);
                    current = null;
                    continue;
                }

                Match field = FieldLine.Match(line);
                if (field.Success)
                {
                    if (!fields.TryGetValue(tune, out var values))
                    {
                        fields[tune] = values = new Dictionary<string, string>();
                    }
                    string label = field.Groups[1].Value;
                    // Only the first TITLE, ARTIST... of a tune is used (a tune can have several, for its parts).
                    if (values.ContainsKey(label))
                    {
                        current = null;
                    }
                    else
                    {
                        values[label] = field.Groups[2].Value.Trim();
                        current = label;
                    }
                }
                else if (current != null && line.Length > 0 && char.IsWhiteSpace(line[0]))
                {
                    fields[tune][current] += " " + line.Trim();
                }
            }

            return fields.ToDictionary(pair => pair.Key, pair => new SidTuneInfo(
                Get(pair.Value, "TITLE"), Get(pair.Value, "ARTIST"), Get(pair.Value, "NAME"), Get(pair.Value, "COMMENT")));
        }

        private static string? Get(Dictionary<string, string> values, string label)
        {
            return values.TryGetValue(label, out string? value) && value.Length > 0 ? value : null;
        }
    }
}
