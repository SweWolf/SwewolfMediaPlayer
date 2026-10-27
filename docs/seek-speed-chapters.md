# Skip, Speed and Chapters: Plan

Plan for more playback controls in SweWolf Media Player. Written 2026-10-25, updated 2026-10-26.
Only the chapters part is built so far. Skip and speed are still to do.

## Status

| Part | Status |
| --- | --- |
| Chapters (buttons, keys P and N, menu) | **Done**, committed 2026-10-25 (`8c7cbf6`) |
| Skip back / forward (10 s, 30 s) | To do |
| Playback speed (box with arrows, keys + - Backspace) | **Done**, 2026-10-26 (`SpeedBox.cs`, `MediaPlayerForm.Speed.cs`) |
| Show Subtitles check box in the Control Panel | Built 2026-10-26, **removed 2026-10-27**: replaced by the default in Tools > Settings and the tracks per file (below) |
| Audio and subtitle track remembered per file | **Done**, 2026-10-27 (`MediaPlayerForm.Tracks.cs`, library table `TrackChoice`) |
| Settings for the skip lengths | To do |
| `docs/help.md` shortcut section | To do (there is no help file in this repo yet) |

Playing backwards is **not** planned. LibVLC has no reverse playback, and the user said it is not important.

## What LibVLC can and cannot do

- **Speed:** `MediaPlayer.SetRate(float)`. 0.25x to 4x works well, and the audio stays pitch-corrected.
- **Reverse:** not supported (no negative rates).
- **Frame step:** `NextFrame()` works forward only. Stepping back is a seek to an earlier time. This is already
  handled by the frame stepping described in the project memory.
- **Chapters:** `ChapterCount`, `FullChapterDescriptions(-1)`. The chapters are only known once the file plays.

## Done: chapters

Code: `MediaPlayerForm.Chapters.cs` (plus the hooks in `MediaPlayerForm.cs` and `MediaPlayerForm.Tunes.cs`).

- A file with **two or more** chapters gets two buttons right of the tune buttons, the keys **P** (previous) and
  **N** (next), and a **Chapters** entry in the right-click menu (name and start time, current one checked).
- A file **without** chapters has none of these: buttons hidden, menu entry missing, keys do nothing (as in VLC).
- Previous: more than 3 seconds into a chapter it goes back to the start of that chapter, otherwise to the previous one.
- Next is disabled in the last chapter. The jumps are ordinary seeks, so they work while paused too.
- Icons: chapters have a bar and **one** triangle (like VLC). Previous / next **tune and track** have a bar and
  **two** triangles, so the two pairs differ when both are visible.
- Not available for streams and SID files. The keys are ignored while trimming and while typing in a text box.
- Tested with a clip of `From Dusk Till Dawn (1996).mkv` (26 chapters). Which of the user's files have chapters:
  `ffprobe -v error -show_chapters -of json "<file>"`. MKV files and DVD / Blu-ray rips often have them,
  AVI, WMV and MPEG-TS do not.

## To do: skip back / forward

- **Left / Right while playing:** jump 10 s. (While paused they still step one frame.)
- **Ctrl+Left / Ctrl+Right:** jump 30 s. **Shift+Left / Shift+Right:** jump 5 s (optional).
- Clamp at 0 and at the end of the file. Only when `CanSeek` is true (not streams, not SID).
- Use `SeekTo` (it also handles a paused or stopped player and updates the time label).
- A short on-screen message ("+30 s", "-10 s") so a key press gives feedback. Needs a small overlay, as there is none yet.
- The jump lengths become settings later (Tools > Settings), defaults 10 s and 30 s.
- Open: the exact lengths and keys. The proposal above is the VLC / YouTube convention; the user has not decided.
- Check: PageUp / PageDown are taken (tune and track skipping), and Left / Right are used for frame stepping while paused.

## Done: playback speed (built 2026-10-26)

What was built: the box sits left of the time label (the seek bar is 30 px shorter); arrows, mouse wheel and the keys
`+` / `-` (also `]` / `[`) step through the list, Backspace or a double click on the text goes back to 1x; a speed other than 1x
is shown in blue. The box is off for streams and SID files. Every new file and the trim preview start at 1x.
The `[ ] \` keys of the original plan need AltGr on a Swedish keyboard, hence `+`, `-` and Backspace.
Tested: at 4x the clock advanced about 14 s in 3.3 s. Not built: a Speed submenu in the right-click menu.

Original plan (kept for the details):

- **Steps:** 0.25x, 0.5x, 0.75x, 1x, 1.25x, 1.5x, 2x, 3x, 4x.
- **Keys:** `]` faster, `[` slower, `\` back to 1x (the mpv convention). `+` and `-` are an alternative.
- **Control (decided 2026-10-26):** not a slider (the exact speeds are hard to read from it) and not in the Control Panel.
  It goes in the **main control bar**, with the other controls for what plays now: a small box that shows the speed
  ("1x", "1.5x") and an up / down arrow pair on its right, stacked like a spin box. Each click moves one step in the
  list above. Up = faster, down = slower. Keep the list ordered so that the arrows go the way people expect.
  - WinForms `DomainUpDown` has exactly this behavior (list of text items, arrows). Its arrows are drawn by Windows
    and stay light in the dark theme, so a self-drawn control like `IconButton` / `CloseXButton` is probably needed.
  - Open: where it sits in the bar (near the time label?) and whether it is hidden when the file cannot use it
    (SID files).
- **Menu:** a **Speed** submenu in the right-click menu with a check mark on the current rate (no "..." on it).
- **Decided:** the speed is **not remembered**. It resets to 1x whenever a new file starts. So no "Remember Playback Speed"
  setting. Reset it in the new-file path (`StartMedia`, next to `ResetTunes` and `ResetChapters`).
- **Trim preview:** reset to 1x while previewing, or at least check that the preview timing is right at other speeds.
- **SID files:** the plugin probably ignores the rate. Disable the Speed menu there; verify with a test.
- Show the current speed on screen when it is not 1x (for example "1.5x"), so a forgotten slow speed is noticed.

## Suggested order for what is left

1. Skip ±10 / ±30 s with the on-screen message.
2. Speed (keys, menu, reset rule, trim and SID checks).
3. Settings for the skip lengths.
4. Help file and README (see the release checklist in `D:\Dev\CLAUDE.md`).

## Open questions

1. Skip keys and lengths: 10 s plain, 30 s with Ctrl?
2. Should there be buttons for skip and speed in the control bar, or only keys and menu? (Chapters got buttons.)
3. Should the previous chapter rule (3 seconds) be the same everywhere? Currently yes.

## Testing notes

- Test with a separate scratch copy of the player (never the user's running one), built with `-o` to a scratch folder.
  The first start of a fresh copy is slow (antivirus scan), so wait about 30 seconds before sending keys.
- A file played before shows the resume prompt: use a new copy of the file.
- Announce by voice before and after a test that takes over the screen (see the global CLAUDE.md).
- Build the Release version after every change.

## Tracks per file (built 2026-10-27)

- **Setting:** Tools > Settings > Player > "Turn On Subtitles When a Video Opens" (on by default). It only decides for a video that has no choice of its own.
- **Remembered:** an audio or subtitle track chosen in the right-click menu (and the C key for subtitles) is saved for that file in the
  library database, table `TrackChoice` (schema 9; columns AudioIndex, AudioName, SubtitleOff, SubtitleIndex, SubtitleName). It is used
  the next time the file opens. A track is found again by its name first, then by its place in the list.
- **Not saved:** what the program does by itself, and a choice that changes nothing (the track that already plays). Nothing for streams, SID
  music or files without a library path.
- **Menu:** "Use Default Tracks" forgets the choice of the file (enabled only when there is one) and goes back to the default.
- **Explorer:** `LibraryStore.cs` is shared; the explorer's copy is the master and needs the same change (new table, `Forget` and `Move` carry
  the rows), and `docs/library-db.md` there should list the table. Not done: the explorer's repo was not touched.
- Tested with a clip that has two audio and two subtitle tracks: the choice was saved, used again after a restart, and "Use Default Tracks"
  removed it. Not tested: an older explorer opening a database with schema 9.
