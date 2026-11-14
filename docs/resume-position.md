# Resume Position and Read Marks

Status: **built 2026-10-16** (`MediaPlayerForm.Library.cs`, `LibraryStore.cs`). This replaces the first specification
(a `positions.json` file, a hash of the file as the key, a 90 second rule): the player now uses the **library database
shared with SweWolf File Explorer**. The format and the rules for the files are in the explorer's `docs/library-db.md`
(that copy is the master).

## What the player does

- A video or song counts as **read** after it has **played for 60 seconds** (`ReadAfterMs`, to be refined), or when it plays
  to its end (when about half of it was played). A file that is **60 seconds long or shorter counts as read as soon as it
  starts to play**, and gets no resume point. The explorer then shows the file as read.
- From the 60 seconds on, the **position** is saved every ~10 seconds while playing, and when the video is paused or the
  window closes. The next time the file is opened the player offers to resume, as the setting Resume says (Yes: at once,
  Ask: with the question "Do you want to resume from 1:30?", No: never).
- **Played time** is real time while the file plays. Paused time and dragging the seek bar do not count, and jumping ahead
  adds nothing. It starts over each time a file is opened (also after a video ended and starts again).
- **Before the 60 seconds nothing is written**, and a saved position is left alone: opening a video for a quick look must not
  overwrite the point where the user really stopped.
- A saved position is **not offered** when it is less than 10 seconds in, or in the last 30 seconds (or 5 %) of the file,
  or when the saved length differs from the file's length (the file was changed).
- A saved position is **cleared** when the video plays to its end (the file stays read) and when a trim replaces the file.
- No position is saved while a trim runs or a trim preview plays, for streams (web addresses) and for SID music.

## Settings (Tools > Settings > Resume)

Video and audio files have **separate settings** (changed 2026-11-13; before, one Resume choice and a "Save Resume Point for" list covered both).

**Resume Video Files** (what happens when a video with a saved position is opened):

| Choice | Behavior |
|---|---|
| Yes | Continue from where you stopped, without asking. |
| **Ask** (default) | "Do you want to resume from 1:30?" Yes continues, No starts from the beginning. |
| No | Always start from the beginning, and nothing is saved for video files. |

**Resume Audio Files** (checkbox, **off by default**: nobody wants to resume a song; it is for audio books and podcasts). When it is checked:

- **When Opening an Audio File:** Yes (continue without asking) or **Ask** (the default).
- **Only if the File Is at Least** N minutes (default 10): shorter audio files never get a position.
- A track of an audio CD (`.cda`) never resumes: every disc has an `E:\Track02.cda`, so the path does not say which disc it was.

**The question "Do you want to resume?" has a countdown** (`CountdownMessageBox.cs`, 10 seconds): the Yes button shows "Yes [10]" and is pressed by itself when the time is up. Moving the mouse (more than 2 pixels), clicking or pressing a key stops the countdown, and the question waits as an ordinary message box. This is for someone who is away from the computer.

**Move the Resume Point Back by** N seconds (default 10, 0 or more): the saved position is N seconds before the point where
the user stopped (never before the start), so there is a little of what came before. Stopping at 2:00 saves 1:50. The row is hidden when nothing is saved (video = No and audio unchecked). Read marks are made in every case.

## Where the file is remembered

By the path of the file. When the explorer has unpacked the file from an archive to a temporary folder to play it, it passes
`--library-path "D:\Videos\Trip.zip\Day 1\clip.mp4"` on the command line: the player plays the temporary file and remembers it
under that path. Moving or renaming a file outside the apps loses its history (matching moved files is a later idea).

## Not done

- The explorer's percent-watched column (position divided by length, both are in the database).
- Add the feature to a README and `docs/help.md` when those exist.
