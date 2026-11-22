# SweWolf Media Player

A Windows media player for video, music, DVDs, audio CDs, playlists and live TV channels. It is simple on purpose: it plays what you give it, remembers where you stopped, and lets you step through a video frame by frame and save any frame as a picture.

It is built on the VLC engine (LibVLC), so it plays nearly every format. It works on its own and together with its sibling application, SweWolf File Explorer.

---

## Features

- **Plays almost anything**: video, music, animated GIFs, DVDs (with their menus), audio CDs, and web streams
- **Frame stepping**: while a video is paused, **Left** and **Right** move one frame back or forward
- **Save Frame**: **S** (or the camera button) saves the picture on screen as an image file, also the very last frame of a video. Choose a default folder in the settings, or save without being asked
- **Seek bar thumbnails**: point at the seek bar and a small picture of that moment pops up (needs FFmpeg)
- **Resume**: video files continue where you stopped (you can choose to be asked first, or to just continue). Audio files, such as audio books and podcasts, can do the same when they are long enough
- **Playback speed**: from slow motion to fast, with a box next to the time, the `+` and `-` keys, or the mouse wheel. **Backspace** goes back to normal speed. Every new file starts at normal speed
- **Chapters**: **N** and **P** jump to the next and previous chapter of files that have them
- **Audio, video and subtitle tracks**: pick them in the right-click menu or press **C** to turn subtitles on or off. Your choice is remembered for each file
- **Playlists**: open an `.m3u` or `.m3u8` playlist of files on your disks. Double-click a row to jump to it. A file that cannot be played is skipped with a short message
- **Live TV channels (IPTV)**: **IPTV > Open** loads the public channel list from [iptv-org](https://github.com/iptv-org/iptv). Filter the list, mark favorites, hide channels you don't want, choose the picture quality, and use **Check Channels** to find out which channels answer
- **Record a stream**: the red button records the channel you watch and saves it as an MP4 file when you stop (needs FFmpeg). Recordings are listed under **My Recordings**, and **Unfinished Recordings** helps after a crash
- **DVD and audio CD**: **Media > Open DVD/CD** plays the disc in the drive. For an audio CD, the artist, title, album and cover come from MusicBrainz
- **Music with several tunes**: Commodore 64 music (SID files) with tune buttons, the tune lengths, and a list of tunes
- **Visualizations**: moving graphics while music plays (right-click menu)
- **Cover pictures** for music files, taken from the tags or from a picture in the folder
- **Full screen** (**F** or **Alt+Enter**): a control bar and a title bar appear when you move the mouse
- **Bookmarks**: **Ctrl+D** adds a bookmark at the current spot, to be found again in SweWolf File Explorer
- **Recent Media Files**: the last nine files, in the **Media** menu
- **Settings** (**Tools > Settings**): full screen for video, thumbnails, subtitles, resume, the look of the buttons, the side of the lists, the saved-frames folder, the start folder for browsing, and checking for a new version at startup
- **Create Shortcut** (**Tools > Create Shortcut...**): add the program to your Desktop, Start Menu or Send To menu
- **Drag and drop**: drop a file on the window, or use **Open with**
- **Help** (**Help > SweWolf Media Player Help**, F1): a complete user guide

---

## Keyboard Shortcuts

| Key | What it does |
|-----|--------------|
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
| Page Up / Page Down | Previous / next tune or track |
| F8 | Show or hide the channel list or the playlist |
| Ctrl+D | Add a bookmark |

---

## Requirements

- **Windows 10 / 11**
- **.NET 10 Desktop Runtime** ([download](https://dotnet.microsoft.com/download)), unless you use the standalone EXE
- **FFmpeg** (optional): place `ffmpeg.exe` next to the application, or install it so that it is on the system PATH ([download FFmpeg](https://ffmpeg.org/download.html)). Without it the player still plays everything, but the seek bar thumbnails, saving the very last frame of a video and recording a stream do not work. The player tells you once, at the first start, if FFmpeg is missing
- **VLC media player** (optional): only for SID music files. The player copies the SID plugin from your own VLC installation the first time you open such a file. Nothing from VLC is shipped with the player

---

## Usage

1. Start the program and choose a file in the **Media File** box (**Browse...**, or drag one in), then click **Open**. The player window opens
2. Right-click the control bar or the video for the other options, such as tracks, quality and visualizations

For all the details, press **F1** in the application or see [docs/help.md](docs/help.md).

### Command Line

You can pass a file or a web address as an argument, for example through **Open with** or a shortcut:

```
SwewolfMediaPlayer.exe "C:\Videos\My Video.mp4"
```

The player opens at once, plays the file and closes when it has ended. If a player is already running, the file is handed to that one instead.

---

## Where the Program Keeps Its Files

- Settings, the library of files you have opened (resume points and track choices), recordings, and logs are in `%AppData%\SweWolfSoftware\SwewolfMediaPlayer\`
- The library file is shared with SweWolf File Explorer, so a video you have started in one program is known to the other
- Caches, such as the information about an audio CD, are in `%LocalAppData%\SweWolfSoftware\`

---

## Built With

- [.NET 10 / Windows Forms](https://dotnet.microsoft.com/)
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) and [LibVLC](https://www.videolan.org/vlc/libvlc.html): the video engine
- [FFmpeg](https://ffmpeg.org/): thumbnails, the last frame and stream recording
- [TagLibSharp](https://github.com/mono/taglib-sharp): music tags and cover pictures
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/): the library of opened files
