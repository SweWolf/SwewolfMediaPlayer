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

**Resume** (what happens when a file with a saved position is opened):

| Choice | Behavior |
|---|---|
| Yes | Continue from where you stopped, without asking. |
| **Ask** (default) | "Do you want to resume from 1:30?" Yes continues, No starts from the beginning. |
| No | Always start from the beginning, and nothing is saved (the other settings of the group are hidden). |

**Move the Resume Point Back by** N seconds (default 10, 0 or more): the saved position is N seconds before the point where
the user stopped (never before the start), so there is a little of what came before. Stopping at 2:00 saves 1:50.

**Save Resume Point for** (which files get a position):

| Choice | Saves a position for |
|---|---|
| Video and Audio Files | every video and audio file |
| **Video Files and Long Audio Files** (default) | every video file, and audio files of at least N minutes (default 10: podcasts, audio books) |

The box for N minutes is only shown for "Video Files and Long Audio Files". With Resume = No the other settings are hidden. Read marks are made in every case.

## Where the file is remembered

By the path of the file. When the explorer has unpacked the file from an archive to a temporary folder to play it, it passes
`--library-path "D:\Videos\Trip.zip\Day 1\clip.mp4"` on the command line: the player plays the temporary file and remembers it
under that path. Moving or renaming a file outside the apps loses its history (matching moved files is a later idea).

## Not done

- The explorer's percent-watched column (position divided by length, both are in the database).
- Add the feature to a README and `docs/help.md` when those exist.
