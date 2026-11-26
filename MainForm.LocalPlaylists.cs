// MainForm.LocalPlaylists.cs: IPTV > Local Playlists, the last item of the IPTV menu (after a separator). Its submenu lists the playlists
// (.m3u and .m3u8 files) in the folder chosen in Tools > Settings (Folder for Saved IPTV Playlists), in alphabetical order, without the extension.
// A click opens the playlist like the Open button does. The item is there only when that folder is set; it is greyed out when the folder
// holds no playlists (or cannot be read).
//
// The list is read when the program starts and again when Tools > Settings is closed with OK. A playlist that is saved in the folder
// later is not in the menu until then.
//
// History:
//   2026-11-26  First version.

namespace SwewolfMediaPlayer
{
    public partial class MainForm
    {
        private ToolStripSeparator? _localPlaylistsSeparator;
        private ToolStripMenuItem? _menuLocalPlaylists;

        private void RefreshLocalPlaylistsMenu()
        {
            if (_menuLocalPlaylists != null)
            {
                menuIptv.DropDownItems.Remove(_menuLocalPlaylists);
                _menuLocalPlaylists.Dispose();
                _menuLocalPlaylists = null;
            }
            if (_localPlaylistsSeparator != null)
            {
                menuIptv.DropDownItems.Remove(_localPlaylistsSeparator);
                _localPlaylistsSeparator.Dispose();
                _localPlaylistsSeparator = null;
            }

            string folder = AppSettings.Load().PlaylistFolder?.Trim().Trim('"') ?? "";
            if (folder.Length == 0)
            {
                return;
            }

            var files = new List<string>();
            try
            {
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    string ext = Path.GetExtension(file);
                    if (ext.Equals(".m3u", StringComparison.OrdinalIgnoreCase) || ext.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
                    {
                        files.Add(file);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // A folder that is gone or cannot be read: the item stays, greyed out.
            }
            files.Sort((a, b) => string.Compare(Path.GetFileNameWithoutExtension(a), Path.GetFileNameWithoutExtension(b), StringComparison.CurrentCultureIgnoreCase));

            _localPlaylistsSeparator = new ToolStripSeparator();
            _menuLocalPlaylists = new ToolStripMenuItem("Local Playlists")
            {
                ToolTipText = "The playlists in your folder for saved IPTV playlists (Tools > Settings).",
                Enabled = files.Count > 0,
            };
            foreach (string file in files)
            {
                string path = file;
                // "&" would make the next letter a shortcut key.
                var item = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(path).Replace("&", "&&")) { ToolTipText = path };
                item.Click += (_, _) => OpenMedia(path);
                _menuLocalPlaylists.DropDownItems.Add(item);
            }
            menuIptv.DropDownItems.Add(_localPlaylistsSeparator);
            menuIptv.DropDownItems.Add(_menuLocalPlaylists);
            MediaPlayerForm.StyleMenu(menuIptv.DropDown);
        }
    }
}
