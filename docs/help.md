# SweWolf Media Player Help

SweWolf Media Player plays video, music, DVDs, audio CDs, playlists and live TV channels. It
remembers where you stopped, lets you step through a video one frame at a time, and saves any
frame as a picture.

## Contents

- [Getting Started](#getting-started)
- [The Main Window](#the-main-window)
- [The Player Window](#the-player-window)
  - [The Control Bar](#the-control-bar)
  - [The Right-Click Menu](#the-right-click-menu)
  - [Full Screen](#full-screen)
  - [When the Media File Ends](#when-the-media-file-ends)
- [Frame Stepping and Saving Frames](#frame-stepping-and-saving-frames)
- [Resuming Where You Stopped](#resuming-where-you-stopped)
- [Audio, Video and Subtitle Tracks](#audio-video-and-subtitle-tracks)
- [Chapters](#chapters)
- [Playback Speed](#playback-speed)
- [Music](#music)
- [Playlists](#playlists)
- [DVDs and Audio CDs](#dvds-and-audio-cds)
- [Live TV Channels (IPTV)](#live-tv-channels-iptv)
  - [The Channel List](#the-channel-list)
  - [Check Channels](#check-channels)
  - [Recording a Channel](#recording-a-channel)
  - [My Recordings and Unfinished Recordings](#my-recordings-and-unfinished-recordings)
- [Bookmarks](#bookmarks)
- [Settings](#settings)
- [Create Shortcut](#create-shortcut)
- [Command Line](#command-line)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Where the Program Keeps Its Files](#where-the-program-keeps-its-files)
- [Troubleshooting](#troubleshooting)

## Getting Started

### What You Need

- Windows 10 or Windows 11.
- **FFmpeg** is optional. Download it from [ffmpeg.org](https://ffmpeg.org/download.html) and
  either put `ffmpeg.exe` in the same folder as the player, or install it so that it is on the
  system PATH. Without FFmpeg the player still plays everything, but these features do not work:
  the thumbnail previews on the seek bar, saving the very last frame of a video, and recording a
  live TV channel. The player tells you once, the first time it starts, if FFmpeg is missing.
- **VLC media player** is optional, and only needed for SID music (Commodore 64). See
  [Music](#music).

### Your First Video

1. Click **Browse...** and choose a video or music file, or drag a file into the main window.
2. Click **Open**. The player window opens and the file starts playing.

You can also start a file from Windows with **Open with**, or from SweWolf File Explorer.

## The Main Window

The main window is where you choose what to play.

- **Media File**: the file or web address to play. Type or paste it, click **Browse...**, or drag
  a file here. A web address can be a stream, or a list of channels (`.m3u` or `.m3u8`).
- **Open**: opens the player window and plays it.

The menus:

- **Tools > Settings**: the program's options. See [Settings](#settings).
- **Tools > Create Shortcut...**: see [Create Shortcut](#create-shortcut).
- **Media > Open DVD/CD**: plays the DVD or audio CD in the drive. See
  [DVDs and Audio CDs](#dvds-and-audio-cds).
- **Media > Recent Media Files**: the last nine files you opened, with **Clear List** (it asks
  first). Only the list is cleared, not the files.
- **IPTV**: the public live TV channel list. See [Live TV Channels (IPTV)](#live-tv-channels-iptv).
- **My Recordings** and **Unfinished Recordings**: they appear when there is something to show.
  See [My Recordings and Unfinished Recordings](#my-recordings-and-unfinished-recordings).
- **Help**: this help (F1) and **About**.
- **New Version Available**: appears at the right end of the menu bar when a newer version has
  been released. Click it to open the download page.

## The Player Window

The player window shows the picture on top and the control bar at the bottom. For music it shows
the cover picture instead of a video.

Double-click the video to switch to full screen and back. The mouse pointer disappears over the
video when you have not moved it for a moment.

### The Control Bar

From left to right, the buttons you will normally use:

- **Play/Pause** (Space): plays, or pauses. While a video is paused you can step frame by frame.
- **Stop**: stops and goes back to the beginning.
- **Skip Back 1 Minute** and **Skip Ahead 1 Minute**: shown for files you can jump around in.
- **Previous** and **Next**: shown for files with several tunes, for a playlist, and for the
  playlist of SweWolf File Explorer (Page Up and Page Down).
- **Record**: shown for live TV channels. See [Recording a Channel](#recording-a-channel).
- **Save Frame** (S): saves the picture on screen as a PNG file.
- **Add Bookmark** (Ctrl+D): see [Bookmarks](#bookmarks).
- **Mute** (M) and the **volume** slider.
- The **seek bar**, with the time. Click or drag it to jump. Point at it to see a small picture of
  that moment (needs FFmpeg; turn it off in the settings).
- The **speed box**: see [Playback Speed](#playback-speed).
- **More Options**: the same menu as the right-click menu.

### The Right-Click Menu

Right-click the video or the control bar. Which items you see depends on the file:

- **Audio Track**, **Video Track**, **Subtitle Track** and **Use Default Tracks**. See
  [Audio, Video and Subtitle Tracks](#audio-video-and-subtitle-tracks).
- **Chapters**, for files that have them.
- **Tune**, for files with several tunes.
- **Quality**, for live channels that offer several qualities.
- **Visualization**, for music: moving graphics instead of the cover picture. Press **Esc** to turn
  it off. It is not remembered: a new player window starts without one.
- **Control Panel**: shows the **Control Panel** under the control bar.

Right-click the **Save Frame** button for **Set as Preview Picture...**, **Save Without Asking
for a Folder** and **Open Folder**.

### Full Screen

Press **F** (or **Alt+Enter**, or double-click the video). Move the mouse to the bottom edge to
show the control bar, and to the top edge to show the title bar with a close button. Press **Esc**
to leave full screen.

In **Tools > Settings** you can choose to open every video in full screen. Music never opens in
full screen.

### When the Media File Ends

The **Control Panel** has a drop-down, **When the Media File Ends** (for a playlist it says **When
the Playlist Ends**). The choices are:

- **Show the Last Frame**: the video stays on its last picture. This is the default.
- **Show a Black Screen**.
- **Repeat**: starts again from the beginning (for a playlist, from the first item).
- **Close Player**: closes the window.

The choice is remembered. A player that was started with a file (for example from SweWolf File
Explorer) always closes at the end, and does not save that choice.

## Frame Stepping and Saving Frames

While a video is **paused**, press **Right** to go one frame forward and **Left** to go one frame
back.

**Save Frame** (S, or the camera button) saves the picture you see as a PNG file. A Save As window
asks for the file name; it starts in the folder you chose in **Tools > Settings** (the Windows
Pictures folder by default). Right-click the button and choose **Save Without Asking for a
Folder** to skip the question and save with a ready-made name.

At the very end of a video, **Save Frame** saves the real last frame of the file. This needs
FFmpeg.

**Set as Preview Picture...** (right-click the Save Frame button) saves the frame as a PNG file
named after the video with `-poster` added (for example `My Video-poster.png`), in the same
folder as the video, as a preview picture for it.

## Resuming Where You Stopped

The player remembers where you stopped in every video, and offers to continue the next time you
open it. A video counts as watched after it has played for a minute or to its end, so SweWolf File
Explorer can mark it too. The positions are kept in the library file that SweWolf File Explorer
shares.

In **Tools > Settings > Resume**:

- **Resume Video Files**: **Yes** continues at once, **Ask** asks first (and chooses Yes after 10
  seconds if you do not touch the mouse or the keyboard), **No** always starts from the beginning
  and remembers nothing.
- **Move the Resume Point Back by**: the saved position is this many seconds before where you
  stopped, so you see a little of what came before. Use 0 for the exact point.
- **Resume Audio Files**: also remember songs, podcasts and audio books, but only files that are
  at least the number of minutes given in **Only if the File Is at Least**. **When Opening an
  Audio File** works like **Resume Video Files**.

## Audio, Video and Subtitle Tracks

Many files hold several audio languages, video angles or subtitles. Choose them in the
right-click menu. Press **C** to turn the subtitles on or off.

The player **remembers your choice for each file**, and uses it the next time the file opens.
**Use Default Tracks** forgets the choice for that file.

In **Tools > Settings > Player**:

- **Turn On Subtitles When a Video Opens**: shows the subtitles of a video the first time you
  open it. Your own choice for a video is used instead.
- **Show Track Menus Only When There Is a Choice**: hides the track menus for files that have
  nothing to choose between. When it is off, the three menus are always shown and are greyed out
  when the file has none.

## Chapters

For a file that has chapters, press **N** for the next chapter and **P** for the previous one, or
use the **Chapters** menu. The menu does not appear for files without chapters.

## Playback Speed

The speed box left of the time shows the speed. Click its arrows or use the mouse wheel over it
to step through the speeds, or press **+** and **-** (also **]** and **[**). **Backspace** goes
back to normal speed.

The speed is not remembered: every new file starts at normal speed. The box is off for files you
cannot jump in, such as live streams and SID music.

## Music

Music files show their cover picture, taken from the file's tags or from a picture in the same
folder. In **Tools > Settings > Player**, **Cover Picture Size for Music Files** chooses
**Medium** or **Maximum**.

Files with several tunes (SID music, Commodore 64) get **Previous** and **Next** buttons
(Page Up and Page Down), the **Tune** menu, and a list of the tunes beside the picture. When a
tune has played to its known length, the next one starts by itself after 3 seconds. In
**Tools > Settings**, **Side of the Tune and Channel Lists** puts the list on the left or on the
right, and **Look of the Previous and Next Buttons** chooses a modern or a classic look.

At the bottom of the tune list (and of a playlist, see [Playlists](#playlists)) is the check box
**Continuous Play**. It is on whenever a player window opens (it is not a setting). Turn it off if
you only want to hear the tune you chose: when it has ended, the player stops instead of going on
to the next one.

**SID music needs VLC media player.** The first time you open a SID file, the player copies the
SID plugin from your own VLC installation. If VLC is not installed, the player tells you so.
Install [VLC](https://www.videolan.org/vlc/) and open the file again.

## Playlists

Open an `.m3u` or `.m3u8` playlist of files on your disks in the **Media File** box, like any
other file. The items play one after the other, and the list is shown beside the picture. Click a
row to jump to it. Press **F8** to hide or show the list.

If an item cannot be played because it is missing or damaged, a message in the video area says
which file and why, counts down, and then the next item starts. **Skip Now** and the **Next**
button do not wait. In **Tools > Settings**, **Seconds to Show a Playlist Error** sets the
countdown (10 seconds by default). If every item in the list fails, the list is not repeated.

The check box **Continuous Play** at the bottom of the list is on whenever a player window opens
(it is not a setting). Turn it off to play only the item you chose: when it has ended, the player
does not go on to the next one. It is not shown for a list of live channels.

At the end of the list, **When the Playlist Ends** decides what happens (see
[When the Media File Ends](#when-the-media-file-ends)).

## DVDs and Audio CDs

**Media > Open DVD/CD** plays the DVD in the drive, with its menus, or the audio CD from the first
track to the last. You can also give the player a DVD drive or folder, such as `E:\`.

- With a **DVD**, the mouse works in the picture. Up, Down and Enter move in the disc menu, and
  **D** opens the main menu of the disc.
- With an **audio CD**, the artist, title, album and cover picture are looked up from MusicBrainz
  and the Cover Art Archive. Only the disc's fingerprint is sent. What was found is kept on your
  computer, so a disc you have played before is known at once, also without internet.

## Live TV Channels (IPTV)

**IPTV > Open Playlist** opens the public channel list from
[iptv-org](https://github.com/iptv-org/iptv), a free list of TV channels on the internet. The
channels are streamed from their owners. Some of them stop working or change over time.

### The Channel List

The list is shown beside the picture (press **F8** to hide or show it). Each row has the logo of
the channel, its name and its group. Click a row to play it.

- The **text box** filters by name. Every word you type must be in the name.
- The **drop-down** narrows the list to one group. It also holds **Favorites**, **Recently
  Watched** (the last 10 channels that really played) and **Hidden Channels**.
- Right-click a channel for **Add to Favorites**, **Hide Channel** and **Copy Stream Address**. A
  gold star marks a favorite.
- Point at a row to see when the channel last worked, or since when it does not.
- After a channel has failed three times in a row, the player offers to hide it.
- Choose **Quality** in the right-click menu if a channel offers several. **Auto** is the default;
  the others are **Highest**, **Lowest**, or a fixed top height such as 720p.

**IPTV > Reset Channel List...** forgets the channels that the list owner has removed from the
public list but that were kept here, and loads the list again as it is on the web now. It asks
first.

### Check Channels

**IPTV > Check Channels** goes through the whole list in the background and asks every channel
whether it answers. The result is shown when you point at a channel. The list has over 11,000
channels, so it can take a long time. You can stop it at any moment and continue later.

The next run skips the channels that already have a result from the last 24 hours (a check box
asks for all of them). Hidden channels, channels marked "[Geo-blocked]" and addresses that are not
`http` or `https` are skipped. If many channels in a row fail, the internet connection is tested,
and the check stops when it is down, so nothing is wrongly marked as failing.

After a run, **Hide Long-Failing Channels** hides the channels that have failed for 14 days or
more. **Check the Hidden Channels Instead** asks only the hidden channels, and afterwards offers
to bring back the ones that answer.

### Recording a Channel

Click the red **Record** button (it turns into a red square while recording). FFmpeg starts at
once and stores the stream in a file. A few seconds later you watch what is being stored; **Pause**
only pauses what you see, the recording goes on. Short interruptions may occur.

Click the button again, or **Stop**, to end the recording. A Save As window asks for the name of
the MP4 file (the channel and the time are suggested). If you cancel, the player asks whether to
discard the recording. Recording needs FFmpeg. The quality cannot be changed while recording.

### My Recordings and Unfinished Recordings

**My Recordings** appears in the main window after your first recording. It lists the latest nine
recordings, newest first, and **All Recordings**, a window with the whole list. There you can
**Play**, **Open Folder**, **Find File** (if the file was moved or renamed), **Copy Source
Address** and **Remove from List** (the file stays).

**Unfinished Recordings** appears if the player or the computer stopped while recording. The
recording up to that moment is kept. Choose **Recover** to turn it into an MP4 file, **Play** to
watch it as it is, or **Discard** (asks first; the file goes to the Recycle Bin).

## Bookmarks

**Add Bookmark** (Ctrl+D) saves a bookmark to the current spot in the video, in the Bookmarks
folder of SweWolf File Explorer. It shows at the top of the explorer's folder tree, and opens the
video at that point.

## Settings

**Tools > Settings** has these groups.

### Player

- **Start Player in Full Screen Mode for Video Files**: every video opens in full screen. Music
  never does.
- **Show Thumbnail Previews on the Seek Bar**: needs FFmpeg.
- **Turn On Subtitles When a Video Opens**, **Show Track Menus Only When There Is a Choice**:
  see [Audio, Video and Subtitle Tracks](#audio-video-and-subtitle-tracks).
- **Cover Picture Size for Music Files**, **Look of the Previous and Next Buttons**,
  **Side of the Tune and Channel Lists**: see [Music](#music). A player that is already open keeps
  its old look until it is opened again.
- **Seconds to Show a Playlist Error**: see [Playlists](#playlists).

### Open a Media File

- **Default Start Folder for Browsing**: the folder where **Browse...** starts. The Windows
  Videos folder is the default. Leave it blank to start in the last folder you used, also after
  the program has been closed.

### Resume

See [Resuming Where You Stopped](#resuming-where-you-stopped).

### Saved Frames

- **Default Folder for Saved Frames**: where the Save Frame window starts. The Windows Pictures
  folder is the default. Leave it blank to start in the folder of the video.

### Updates

- **Check for New Version at Startup**: the program asks GitHub whether a newer version has been
  released. Only the version number is compared; nothing about you is sent.

## Create Shortcut

**Tools > Create Shortcut...** adds a shortcut to the program on your **Desktop**, in the **Start
Menu (Programs)**, or in the **Send To Menu** of Windows. Choose whether it is for the current user
only or for all users (this may need administrator privileges; a Send To shortcut is always for the
current user). The shortcut points to the program that is running now.

## Command Line

Give the program a file or a web address, for example through **Open with** or a shortcut:

```
SwewolfMediaPlayer.exe "C:\Videos\My Video.mp4"
```

The player opens at once, skips the main window, plays the file and closes when it has ended. If a
player window is already open, the file is handed to that one. A file that does not exist is
reported.

## Keyboard Shortcuts

| Key | What it does |
|-----|--------------|
| F1 | Open this help |
| Space, K, Pause | Play or pause |
| Left / Right (paused) | One frame back / forward |
| S | Save the current frame |
| F, Alt+Enter | Full screen on or off |
| Esc | Leave full screen, close a panel, or close the player |
| X | Close the player |
| M | Mute or unmute |
| C | Subtitles on or off |
| N / P | Next / previous chapter |
| + / - (or ] / [) | Faster / slower |
| Backspace | Normal speed |
| Page Up / Page Down | Previous / next tune, track or playlist item |
| F8 | Show or hide the channel list or the playlist |
| Ctrl+D | Add a bookmark |

A key that types a letter, such as M or X, does not act while you are typing in a text box.

## Where the Program Keeps Its Files

- **Settings, the library, recordings and logs**:
  `%AppData%\SweWolfSoftware\SwewolfMediaPlayer\`. The library file, which remembers the files you
  have opened, your resume points and your track choices, is shared with SweWolf File Explorer.
- **Caches**, such as the information about an audio CD, which the program can recreate by itself:
  `%LocalAppData%\SweWolfSoftware\`.

## Troubleshooting

**The seek bar shows no picture when I point at it.** The previews need FFmpeg, and only work for
video files. See [What You Need](#what-you-need).

**Save Frame does not save the last picture of a video.** This needs FFmpeg too.

**A SID file will not play.** SID music needs the SID plugin of VLC media player. Install VLC and
open the file again.

**A channel does not play.** Channels on the internet come and go. Try another quality, or use
**IPTV > Check Channels** to find the ones that work. A channel that has failed for a long time can
be hidden.

**A file does not play at all.** The player uses the VLC engine, which plays nearly everything. If
a file is damaged, it will not play in any player.

**I hear no sound.** Look at the **Mute** button and the volume slider. If the file has several
audio tracks, try another one in the right-click menu.

**I do not see the resume question.** The setting **Resume Video Files** may be **Yes** or **No**.
Files that are shorter than a minute never get a resume point.
