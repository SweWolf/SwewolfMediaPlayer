// UnfinishedRecordings.cs: recordings that were not finished (the program or the computer died while it recorded), and what the user can do with them.
//
// When a recording starts, the player writes a small entry (UnfinishedStore) with the .ts file, the channel, the stream address and the start time. The
// entry is removed when the recording is over and the .ts file is gone (saved as MP4, discarded, or nothing recorded). If the program or the computer
// dies, the entry and the .ts file stay (a .ts file is playable and convertible up to the last packet that was written). The main window shows them in
// its Unfinished Recordings menu: Recover (the name is asked, then the .ts file becomes an MP4 file and goes into My Recordings), Play (the .ts file as
// it is) and Discard (asks first; the .ts file goes to the Recycle Bin). An entry whose .ts file is still being written by a running copy of the player
// (another window, or this one) is not unfinished and is not listed. An entry whose .ts file is gone is dropped from the list.
//
// The list is kept in %AppData%\SweWolfSoftware\SwewolfMediaPlayer\unfinished.json (the user would miss it).
//
// History:
//   2026-11-15  First version.

using System.Diagnostics;
using System.Text.Json;

namespace SwewolfMediaPlayer
{
    internal sealed class UnfinishedEntry
    {
        public string TsPath { get; set; } = "";
        public string Channel { get; set; } = "";
        public string? Url { get; set; }
        public string? ListUrl { get; set; }
        public DateTime StartedAt { get; set; }
        public string Suggestion { get; set; } = "";
        public int ProcessId { get; set; }          // the copy of the player that wrote the .ts file ...
        public long ProcessStartTicks { get; set; } // ... and when it started (a process id can be used again)

        public long SizeBytes => File.Exists(TsPath) ? new FileInfo(TsPath).Length : 0;
    }

    internal static class UnfinishedStore
    {
        private static readonly object Gate = new();
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", "SwewolfMediaPlayer", "unfinished.json");

        // Written when a recording has started.
        public static void Add(string tsPath, string channel, string? url, string? listUrl, DateTime startedAt, string suggestion)
        {
            lock (Gate)
            {
                List<UnfinishedEntry> list = Load();
                list.RemoveAll(e => SamePath(e.TsPath, tsPath));
                Process me = Process.GetCurrentProcess();
                list.Add(new UnfinishedEntry
                {
                    TsPath = tsPath,
                    Channel = channel,
                    Url = url,
                    ListUrl = listUrl,
                    StartedAt = startedAt,
                    Suggestion = suggestion,
                    ProcessId = me.Id,
                    ProcessStartTicks = me.StartTime.ToUniversalTime().Ticks,
                });
                Save(list);
            }
        }

        public static void Remove(string tsPath)
        {
            lock (Gate)
            {
                List<UnfinishedEntry> list = Load();
                if (list.RemoveAll(e => SamePath(e.TsPath, tsPath)) > 0)
                {
                    Save(list);
                }
            }
        }

        // Called when a recording is over: the entry goes when the .ts file is gone (a file that was kept, for example after a failed conversion, stays listed).
        public static void ForgetIfGone(string tsPath)
        {
            if (!File.Exists(tsPath))
            {
                Remove(tsPath);
            }
        }

        // The recordings that were left unfinished, the newest first. Entries whose .ts file is gone are dropped.
        public static List<UnfinishedEntry> Pending()
        {
            lock (Gate)
            {
                List<UnfinishedEntry> list = Load();
                int before = list.Count;
                list.RemoveAll(e => !File.Exists(e.TsPath));
                if (list.Count != before)
                {
                    Save(list);
                }
                return list.Where(e => !IsWrittenNow(e)).OrderByDescending(e => e.StartedAt).ToList();
            }
        }

        // True when the copy of the player that started the recording is still running (it is recording, or finishing the file).
        private static bool IsWrittenNow(UnfinishedEntry entry)
        {
            try
            {
                using Process process = Process.GetProcessById(entry.ProcessId);
                return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == entry.ProcessStartTicks;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private static bool SamePath(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static List<UnfinishedEntry> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonSerializer.Deserialize<List<UnfinishedEntry>>(File.ReadAllText(FilePath)) ?? new List<UnfinishedEntry>();
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
            }
            return new List<UnfinishedEntry>();
        }

        private static void Save(List<UnfinishedEntry> list)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(list, Json));
                File.Move(temp, FilePath, overwrite: true);   // never a half-written list
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    // The questions about the name of a recording, the same in the player (when a recording ends) and in the main window (Recover).
    internal static class RecordingNaming
    {
        // The usual Save As question: the dialog asks by itself what to do with a name that exists (replace it, or choose another name).
        public static string? Ask(IWin32Window owner, string folder, string suggestion)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Recording As",
                Filter = "MP4 Video|*.mp4",
                DefaultExt = "mp4",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = suggestion,
                InitialDirectory = folder,
            };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }

        // Cancel in the Save As dialog is not "no": it asks whether to discard the recording, and No (the default) goes back to the name.
        // Yes returns null: the caller moves the .ts file to the Recycle Bin.
        public static string? Choose(IWin32Window owner, string caption, string folder, string suggestion)
        {
            while (true)
            {
                string? target = Ask(owner, folder, suggestion);
                if (target != null)
                {
                    return target;
                }
                if (ConfirmDiscard(owner, caption))
                {
                    return null;
                }
            }
        }

        public static bool ConfirmDiscard(IWin32Window owner, string caption)
        {
            return MessageBox.Show(owner,
                "Do you want to discard the recording?\n\n" +
                "Yes: Discard the recording. The file is moved to the Recycle Bin, so you can still get it back from there.\n\n" +
                "No: Keep the recording. You go back and enter a file name for it.",
                caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        // A file name in the folder that is not taken: "name.mp4", else "name (2).mp4" and so on.
        public static string UniqueTarget(string folder, string name)
        {
            string path = Path.Combine(folder, name + ".mp4");
            for (int n = 2; File.Exists(path); n++)
            {
                path = Path.Combine(folder, $"{name} ({n}).mp4");
            }
            return path;
        }
    }

    // What the Unfinished Recordings menu does with an entry.
    internal static class UnfinishedActions
    {
        // Asks for a name, makes the MP4 file from the .ts file, and puts it into My Recordings. Cancel asks whether to discard (see RecordingNaming.Choose).
        public static async Task RecoverAsync(IWin32Window owner, string caption, UnfinishedEntry entry)
        {
            string? ffmpeg = MediaPlayerForm.FindFfmpeg();
            if (ffmpeg == null)
            {
                MessageBox.Show(owner, "FFmpeg was not found, so the recording cannot be converted.\n\nPut ffmpeg.exe in the program folder or in a folder in the PATH.", caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string folder = Path.GetDirectoryName(entry.TsPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            string suggestion = entry.Suggestion.Length > 0 ? entry.Suggestion : $"Recording {entry.StartedAt:yyyy-MM-dd HH-mm}";
            string? target = RecordingNaming.Choose(owner, caption, folder, suggestion);
            if (target == null)
            {
                MoveToRecycleBin(owner, caption, entry);
                return;
            }

            string? problem;
            Form? form = owner as Form;
            bool wasWaiting = form?.UseWaitCursor ?? false;
            if (form != null)
            {
                form.UseWaitCursor = true;
            }
            try
            {
                problem = await StreamRecorder.ConvertToMp4Async(ffmpeg, entry.TsPath, target);
            }
            finally
            {
                if (form != null && !form.IsDisposed)
                {
                    form.UseWaitCursor = wasWaiting;
                }
            }

            if (problem != null)
            {
                MessageBox.Show(owner, $"The recording could not be converted to MP4.\n\n{problem}\n\nThe .ts file was kept:\n{entry.TsPath}", caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StreamRecorder.TryDelete(entry.TsPath);
            UnfinishedStore.Remove(entry.TsPath);
            Remember(target, entry);
            MessageBox.Show(owner, $"The recording was saved:\n{target}", caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Asks first, then moves the .ts file to the Recycle Bin.
        public static void Discard(IWin32Window owner, string caption, UnfinishedEntry entry)
        {
            if (RecordingNaming.ConfirmDiscard(owner, caption))
            {
                MoveToRecycleBin(owner, caption, entry);
            }
        }

        private static void MoveToRecycleBin(IWin32Window owner, string caption, UnfinishedEntry entry)
        {
            if (StreamRecorder.TryRecycle(entry.TsPath))
            {
                UnfinishedStore.Remove(entry.TsPath);
            }
            else
            {
                MessageBox.Show(owner, $"The recording could not be moved to the Recycle Bin.\n\nThe .ts file was kept:\n{entry.TsPath}", caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // The same as MediaPlayerForm.RememberRecording, for a recording that is recovered.
        private static void Remember(string target, UnfinishedEntry entry)
        {
            try
            {
                long duration = 0;
                try
                {
                    using TagLib.File tags = TagLib.File.Create(target);
                    duration = (long)tags.Properties.Duration.TotalMilliseconds;
                }
                catch (Exception)
                {
                }

                RecordingStore.Add(new RecordingEntry
                {
                    Path = target,
                    Channel = entry.Channel,
                    Url = entry.Url,
                    ListUrl = entry.ListUrl,
                    RecordedAt = entry.StartedAt,
                    DurationMs = duration,
                    SizeBytes = new FileInfo(target).Length,
                });
                if (entry.Url != null)
                {
                    LibraryStore.SetItemInfo(target, entry.Url, entry.ListUrl, create: false);
                }
            }
            catch (Exception)
            {
                // The recording itself is saved: this is only the list.
            }
        }
    }
}
