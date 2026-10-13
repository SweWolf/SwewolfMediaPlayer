# Resume Position (specification)

Status: **planned, not implemented.** Specified 2026-10-13.

When the user closes a video and opens it again, the player offers to continue where they stopped.

## Setting

A drop-down in the Control Panel, **When Opening a Video I Have Watched Before** (final wording to be
decided), remembered in `settings.json` like "When the Video Ends":

| Option | Behavior |
|---|---|
| **Always Ask** (default) | A dialog: "Resume from 12:34, or start from the beginning?" Resume is the default button. |
| **Always Resume** | Plays from the saved position without asking. |
| **Never Resume** | Always plays from the beginning, and **no positions are saved** (a real off switch). |

## Storage

- One file for all videos: `%AppData%\SweWolfSoftware\SwewolfMediaPlayer\positions.json`, next to
  `settings.json`. Nothing is written to the folders that hold the videos.
- JSON to start with (code like `AppSettings.cs`, readable by hand). Keep the most recent ~500 entries and
  drop the oldest. If it ever gets slow or large, switch to SQLite: only the load and save code changes.
- Per video: position (ms), length (ms), last played (date), last known path (information only).
- A missing or damaged file means "no saved positions". Never show an error for it.

## Identifying a video

The key is a small hash of the file, not its path, so renaming the file or its folder, or moving it, does not
lose the position: the file size plus the first and last 64 KB of the content. The last known path is only
stored as information. A web address (stream) uses the address as the key. A live stream has nothing to
resume, so streams are not saved.

## When a position is saved

Only after the video has **played for 90 seconds in this session** (a constant in the code).

- The 90 seconds is time the video was actually playing. Paused time does not count, and neither does
  dragging the seek bar: jumping ahead 10 minutes adds nothing.
- It carries on through pause and resume (60 s, a pause, then 40 s makes 100 s), and starts over each time
  a video is opened. (`_playingSinceTicks` in `MediaPlayerForm.cs` is a starting point.)
- **Before the 90 seconds:** nothing is written, and an existing saved position is left alone. Opening a
  video for a quick look must not overwrite the point where the user really stopped.
- **After the 90 seconds:** the position is saved every ~10 seconds while playing, and on pause, stop and
  close (saving only on close loses it if the app crashes). From then on it follows the playback position,
  wherever the user has dragged to.
- No position is saved while a trim runs (the file is being replaced), during a trim preview, or when the
  setting is Never Resume.

## When a saved position is removed

- The video **played to its end** (also if shorter than 90 seconds, so a short clip never keeps a stale
  point). It then starts from the beginning next time.
- The file was **replaced by a trim** (the length changed, so the old time no longer fits).
- The oldest entries, when there are more than ~500.

## When a saved position is offered or used

Only if the video was watched long enough to have one, and the position is more than ~10 seconds in and not
within the last ~5% (or last 30 seconds), because then the video was basically finished. Otherwise the video
just plays from the beginning. If the stored length differs from the file's real length, ignore the position.

## Open questions

- Short videos (under 90 s) never get a position. Fine for clips, but a "90 s or 10% of the length,
  whichever is smaller" rule would cover them too. Keep the plain 90 s unless it turns out to bother.
- Exact wording of the setting and of the dialog (US spelling, Title Case for the labels).
- Add the feature to the README and `docs/help.md` when it is built.
