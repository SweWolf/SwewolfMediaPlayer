// MediaPlayerForm.Chapters.cs: chapters (MKV, MP4, DVD rips), as VLC has them.
// A file that has at least two chapters gets two buttons right of the tune buttons (previous / next chapter),
// the keys P and N, and a Chapters entry in the right-click menu. A file without chapters has none of them:
// the buttons are hidden and the keys do nothing.
//
// LibVLC lists the chapters of the current title (FullChapterDescriptions) once the file plays. The jumps are
// ordinary seeks to the start time of a chapter (SeekTo), so they work while paused too.
//
// History:
//   2026-10-25  First version.

using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private IconButton? btnPrevChapter;
        private IconButton? btnNextChapter;
        private bool _chapterButtonsShown;

        private ChapterDescription[]? _chapters;   // null = the file has no chapters (or they are not known yet)
        private bool _chaptersChecked;   // LibVLC has been asked, or it was given up on

        private void InitChapterControls()
        {
            btnPrevChapter = NewTuneButton(IconKind.PreviousChapter, "Previous Chapter (P)", () => GoToChapter(-1));
            btnNextChapter = NewTuneButton(IconKind.NextChapter, "Next Chapter (N)", () => GoToChapter(+1));
        }

        // Forget the chapters of the previous file (called whenever a file is opened).
        private void ResetChapters()
        {
            _chapters = null;
            _chaptersChecked = false;
            _dvdTitle = _dvdChapterCount = -2;   // (a DVD reads its chapters again)
            ShowChapterControls(false);
        }

        // Called by the timer: reads the chapters once LibVLC knows them, and keeps the buttons up to date.
        private void CheckChapters()
        {
            MediaPlayer? player = _player;
            if (player == null || _isStream)
            {
                return;
            }

            if (IsDvd)
            {
                CheckDvdChapters(player);
                return;
            }

            if (_chapters != null)
            {
                UpdateChapterButtons(player.Time);
                return;
            }

            if (_chaptersChecked || !player.IsPlaying)
            {
                return;
            }

            if (SidPluginSetup.IsSidFile(_filePath))
            {
                _chaptersChecked = true;
                return;
            }

            if (player.ChapterCount > 1)
            {
                ChapterDescription[] chapters = player.FullChapterDescriptions(-1);
                _chaptersChecked = true;
                if (chapters is { Length: > 1 })
                {
                    _chapters = chapters;
                    ShowChapterControls(true);
                    UpdateChapterButtons(player.Time);
                }
            }
            else if (player.Time > 5000)
            {
                _chaptersChecked = true;   // it has played for a while without any: there are none
            }
        }

        private void ShowChapterControls(bool show)
        {
            if (btnPrevChapter == null || btnNextChapter == null || _chapterButtonsShown == show)
            {
                return;
            }

            _chapterButtonsShown = show;
            btnPrevChapter.Visible = btnNextChapter.Visible = show;
            LayoutControlBar();
        }

        // The chapter that plays at timeMs (the last one that has started).
        private int ChapterAt(long timeMs)
        {
            int index = 0;
            for (int i = 0; i < _chapters!.Length; i++)
            {
                if (_chapters[i].TimeOffset <= timeMs)
                {
                    index = i;
                }
            }
            return index;
        }

        // Previous is always possible (it goes back to the start of the chapter); next stops at the last chapter.
        private void UpdateChapterButtons(long timeMs)
        {
            if (btnNextChapter != null && _chapters != null)
            {
                btnNextChapter.Enabled = ChapterAt(timeMs) < _chapters.Length - 1;
            }
        }

        private bool ChaptersAvailable => _chapters != null && CanSeek;

        // direction -1: previous chapter, but after 3 seconds into a chapter it goes back to the start of that chapter
        // (like the previous button of the tunes); +1: next chapter.
        private void GoToChapter(int direction)
        {
            if (_player == null || !ChaptersAvailable)
            {
                return;
            }

            long now = Math.Max(_player.Time, 0);
            int current = ChapterAt(now);
            int target = current + direction;
            if (direction < 0 && now - _chapters![current].TimeOffset > 3000)
            {
                target = current;
            }
            if (target < 0 || target >= _chapters!.Length)
            {
                return;
            }

            JumpToChapter(target);
        }

        // A DVD jumps by its own chapter numbers; a file seeks to the start time of the chapter.
        private void JumpToChapter(int index)
        {
            if (IsDvd && _player != null)
            {
                _player.Chapter = index;
            }
            else
            {
                SeekTo(_chapters![index].TimeOffset);
            }
        }

        private void AddChapterMenu()
        {
            if (!ChaptersAvailable)
            {
                return;
            }

            long now = Math.Max(_player?.Time ?? 0, 0);
            int current = ChapterAt(now);
            ToolStripMenuItem menu = new("Chapters");
            for (int i = 0; i < _chapters!.Length; i++)
            {
                int index = i;
                string name = _chapters[i].Name is { } chapterName && !string.IsNullOrWhiteSpace(chapterName) ? chapterName : $"Chapter {i + 1}";
                string text = $"{name}   ({FormatTime(_chapters[i].TimeOffset, false)})";
                menu.DropDownItems.Add(TrackItem(text, index == current, () => JumpToChapter(index)));
            }
            menuVideo.Items.Add(menu);
        }
    }
}
