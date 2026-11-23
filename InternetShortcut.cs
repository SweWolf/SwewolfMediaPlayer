// InternetShortcut.cs: a Windows Internet shortcut (a .url file, such as the bookmarks of SweWolf File Explorer make for a stream) holds a web address
// in a line "URL=...". The player opens the address, not the little file. A shortcut with no readable address is left alone (TryResolve says no).
//
// Self-contained: to reuse it in another app, change the namespace.
//
// History:
//   2026-11-22  First version: a .url file opens like the web address in it (main window, command line).

using System.Text.RegularExpressions;

namespace SwewolfMediaPlayer
{
    internal static class InternetShortcut
    {
        private static readonly Regex UrlLine = new(@"^\s*URL\s*=\s*(.+?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static bool IsShortcutFile(string path) =>
            path.EndsWith(".url", StringComparison.OrdinalIgnoreCase);

        // True, with the address, for an existing .url file that has a "URL=" line.
        public static bool TryResolve(string path, out string url)
        {
            url = "";
            try
            {
                if (!IsShortcutFile(path) || !File.Exists(path))
                {
                    return false;
                }

                // The address is plain ASCII, so the code page of the file does not matter.
                Match match = UrlLine.Match(File.ReadAllText(path));
                if (!match.Success)
                {
                    return false;
                }
                url = match.Groups[1].Value;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
