// MediaPlayerForm.Cd.cs: the artist, title, album and cover picture of an audio CD track, shown like the tags of a music file (ShowTrackInfo, picCover).
//
// Everything about a disc comes from CdDiscInfo.cs, a file shared with SweWolf File Explorer (identical in both repos but for the namespace; the
// History list in its header says what changed): MusicBrainz and the Cover Art Archive are asked by the disc's fingerprint, only that id goes out, and
// what was found is kept in %LocalAppData%\SweWolfSoftware\CdInfo, a folder both programs use. A disc opened in the File Explorer first is
// therefore known here at once (and the other way round); information that is a day old is looked at again, silently, while the old one is shown,
// and the screen changes only if MusicBrainz says something different (CdDiscInfo.Updated). Offline, a kept disc still shows its titles and cover.
//
// History:
//   2026-11-13  First version: titles from LibVLC's own lookup (CDDB), kept per disc in SwewolfMediaPlayer\CdInfo.
//   2026-11-13  Cover picture from MusicBrainz + the Cover Art Archive.
//   2026-11-14  Shared with the File Explorer: titles and cover come from the shared CdDiscInfo (MusicBrainz only, shared cache, daily silent refresh);
//               LibVLC's CDDB lookup and this program's own CdInfo folder are no longer used.

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private bool _cdInfoHooked;

        // Called when a CD track has started: what is known is shown at once (the kept information, else "Track N"); the lookup of a new or
        // old disc runs in the background and CdDiscInfo.Updated brings what it found.
        private void ShowCdTrackInfo(string path)
        {
            HookCdInfo();
            if (!TryGetCdTrack(path, out _, out int track))
            {
                return;
            }
            ShowCdTrackText(path, track);
            ShowCdCover(path);
        }

        private void ShowCdTrackText(string path, int track)
        {
            CdTrackInfo? info = CdDiscInfo.InfoOf(path);   // starts the lookup when the disc is new or its information is old
            if (info != null)
            {
                ShowTrackInfo(info.Artist, info.Title, info.Album);
            }
            else
            {
                ShowTrackInfo("Audio CD", $"Track {track}");
            }
        }

        // The cover picture of the disc, when one is kept.
        private void ShowCdCover(string path)
        {
            string? discId = CdDiscInfo.DiscId(path);
            if (discId == null || CdDiscInfo.CoverFile(discId) is not { } file || IsDisposed || _filePath != path || ActiveVisualization() != null)
            {
                return;
            }

            try
            {
                using MemoryStream stream = new(File.ReadAllBytes(file));
                using Image loaded = Image.FromStream(stream);
                Image? old = picCover.Image;
                picCover.Image = new Bitmap(loaded);
                picCover.Visible = true;
                old?.Dispose();
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
            {
                // A damaged file: it is fetched again after it is deleted (the next lookup of an incomplete disc).
                try { File.Delete(file); } catch (IOException) { }
            }
        }

        // The shared lookup found something new (a new disc, or a change on MusicBrainz): the track playing now shows it, silently.
        private void HookCdInfo()
        {
            if (_cdInfoHooked)
            {
                return;
            }
            _cdInfoHooked = true;
            CdDiscInfo.Log ??= OpenTimingLog.Write;
            CdDiscInfo.Updated += discId =>
            {
                try
                {
                    BeginInvoke(() => OnCdInfoUpdated(discId));
                }
                catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
                {
                    // The window is gone.
                }
            };
        }

        private void OnCdInfoUpdated(string discId)
        {
            if (IsDisposed || _isStream || string.IsNullOrEmpty(_filePath)
                || !Path.GetExtension(_filePath).Equals(".cda", StringComparison.OrdinalIgnoreCase)
                || CdDiscInfo.DiscId(_filePath) != discId
                || !TryGetCdTrack(_filePath, out _, out int track))
            {
                return;
            }
            ShowCdTrackText(_filePath, track);
            ShowCdCover(_filePath);
        }
    }
}
