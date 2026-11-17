// MediaPlayerForm.Dvd.cs: keys for the menus of a video DVD (the mouse works in the picture itself, see EnableMouseInput in StartMedia).
//
// History:
//   2026-11-16  First version: Up / Down / Enter move in the disc menu, D opens the disc's main menu.
//   2026-11-17  Chapters only for a title whose chapters have different start times (not the menu pages), and jumps by chapter number.

using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        // Up / Down / Enter go to the disc menu (the keys have no other use), and D asks the disc for its main menu.
        // Left / Right keep seeking and frame stepping; in a menu the mouse is the way to go sideways.
        private bool HandleDvdKey(Keys keyData)
        {
            if (_player == null || ActiveControl is TextBoxBase { Visible: true })
            {
                return false;
            }
            switch (keyData)
            {
                case Keys.Up:
                    _player.Navigate(1);   // libvlc_navigate_up
                    return true;
                case Keys.Down:
                    _player.Navigate(2);   // ..._down
                    return true;
                case Keys.Enter:
                    _player.Navigate(0);   // ..._activate
                    return true;
                case Keys.D:
                    _player.Title = 0;   // title 0 is the disc's menu
                    return true;
                default:
                    return false;
            }
        }
        // The title and chapter count the DVD chapters were last read for: they are read again when either changes (menu to film and back).
        private int _dvdTitle = -2;
        private int _dvdChapterCount = -2;

        // A DVD has a list of "chapters" in every title, also in its menus, where they are the menu pages (Resume, Root, Title, ... all at 0:00)
        // and of no use. Only a title whose chapters start at different times (the film, an extra) gets the chapter buttons and the Chapters menu.
        private void CheckDvdChapters(MediaPlayer player)
        {
            if (!player.IsPlaying)
            {
                return;
            }
            int title = player.Title;
            int count = player.ChapterCount;
            if (title == _dvdTitle && count == _dvdChapterCount)
            {
                if (_chapters != null)
                {
                    UpdateChapterButtons(player.Time);
                }
                return;
            }
            _dvdTitle = title;
            _dvdChapterCount = count;
            ChapterDescription[]? chapters = count > 1 ? player.FullChapterDescriptions(-1) : null;
            bool usable = chapters is { Length: > 1 } && chapters.Any(c => c.TimeOffset > 0);
            _chapters = usable ? chapters : null;
            ShowChapterControls(usable);
            if (usable)
            {
                UpdateChapterButtons(player.Time);
            }
        }
    }
}
