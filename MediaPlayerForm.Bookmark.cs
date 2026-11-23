// MediaPlayerForm.Bookmark.cs: the Add Bookmark button (right of the camera button) and Ctrl+D.
//
// A bookmark is an ordinary Windows shortcut in the explorer's Bookmarks folder, %AppData%\SweWolfSoftware\SwewolfFileExplorer\Bookmarks
// (the explorer shows it at the top of its folder tree and reads that folder again when its window comes to the front). The point where it
// starts is written in the shortcut's Comment in plain words, "Start time: 0:12:34.500" (hours:minutes:seconds.thousandths): the explorer's
// BookmarkStore.FormatStartTime / ParseStartTime read and write the same text (change both together).
//
// History:
//   2026-10-21  First version. A question first (the file, a checkbox "Start Point 0:00:53", unchecked, as TeraTool had it).
//   2026-10-23  SID files: no Start Point checkbox (a SID file cannot be jumped in), the question is shorter.
//   2026-11-11  Streams can be bookmarked too: an Internet shortcut (.url) in the same folder, no start point.
//   2026-11-22  Without SweWolf File Explorer, the first bookmark is followed by a one-time message about where it is and what the explorer adds.

using System.Runtime.InteropServices;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        private IconButton? btnBookmark;

        private void InitBookmarkButton()
        {
            btnBookmark = new IconButton
            {
                BackColor = btnSaveFrame.BackColor,
                FlatStyle = FlatStyle.Flat,
                ForeColor = btnSaveFrame.ForeColor,
                Icon = IconKind.Bookmark,
                Name = "btnBookmark",
                Size = btnSaveFrame.Size,
                Top = btnSaveFrame.Top,
                Left = btnSaveFrame.Right + 6,
                TabStop = false,
                UseVisualStyleBackColor = false,
            };
            toolTip.SetToolTip(btnBookmark, "Add Bookmark at This Point (Ctrl+D)");
            btnBookmark.Click += (_, _) => AddBookmark();
            pnlControls.Controls.Add(btnBookmark);
            LayoutControlBar();
        }

        // Asks (as TeraTool did: the file, and a checkbox "Start Point 0:00:53", unchecked) and makes the shortcut.
        // Without the start point: a bookmark of the file, named like the file. With it: named "<file> @ 12m34s", so several start
        // points of one file can be kept, with the start point in the Comment.
        private void AddBookmark()
        {
            if (_player == null || _restoringEnd || IsRecordingPlayback)   // the .ts file of a recording is temporary
            {
                return;
            }

            if (_isStream)
            {
                AddStreamBookmark();
                return;
            }

            if (IsDvd)
            {
                return;   // (no bookmark of a DVD yet)
            }

            if (!File.Exists(_filePath))
            {
                return;
            }

            string file = Path.GetFullPath(_filePath);
            long ms = Math.Max(_player.Time, 0);   // the point when the key was pressed, not when the question is answered
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            string clock = t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";

            // SID music has no start point (it cannot be jumped in): the question has no checkbox.
            if (!AskBookmark(file, SidPluginSetup.IsSidFile(_filePath) ? null : $"Start Point {clock}", out bool useStart))
            {
                return;
            }

            string name = Path.GetFileNameWithoutExtension(file);
            foreach (char bad in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(bad.ToString(), "");
            }
            name = name.Trim().Trim('.').Trim();
            if (name.Length == 0)
            {
                name = "Bookmark";
            }

            string when = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h{t.Minutes:00}m{t.Seconds:00}s" : $"{t.Minutes}m{t.Seconds:00}s";
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SweWolfSoftware", "SwewolfFileExplorer", "Bookmarks");

            try
            {
                Directory.CreateDirectory(folder);
                string lnk = Path.Combine(folder, useStart ? $"{name} @ {when}.lnk" : $"{name}.lnk");
                if (File.Exists(lnk))
                {
                    ShowTitleMessage(useStart ? "That start point is bookmarked already" : "This file is bookmarked already");
                    return;
                }

                Type type = Type.GetTypeFromProgID("WScript.Shell")!;
                dynamic shell = Activator.CreateInstance(type)!;
                dynamic link = shell.CreateShortcut(lnk);
                link.TargetPath = file;
                link.WorkingDirectory = Path.GetDirectoryName(file) ?? "";
                if (useStart)
                {
                    link.Description = $"Start time: {(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds:000}";
                }
                link.Save();
                Marshal.FinalReleaseComObject(link);
                Marshal.FinalReleaseComObject(shell);

                ShowTitleMessage(useStart ? $"Bookmark added at {clock}" : "Bookmark added");
                ExplainBookmarksOnce();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"The bookmark could not be made. {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // A stream (a TV channel, a radio station) is bookmarked as an Internet shortcut (.url), the same kind of file the explorer makes
        // for a web address (BookmarkStore.AddUrl: [InternetShortcut], ANSI code page). There is no start point: a stream has no position.
        private void AddStreamBookmark()
        {
            if (!TryGetStreamUri(_filePath, out Uri? uri) || uri == null)
            {
                return;
            }

            string url = uri.AbsoluteUri;
            if (!AskBookmark(url, null, out _))
            {
                return;
            }

            string last = uri.Segments.Length > 1 ? uri.Segments[^1].Trim('/') : "";
            string name = uri.Host + (last.Length > 0 ? " - " + last : "");
            foreach (char bad in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(bad.ToString(), "");
            }
            name = name.Trim().Trim('.').Trim();
            if (name.Length == 0)
            {
                name = "Stream";
            }

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SweWolfSoftware", "SwewolfFileExplorer", "Bookmarks");
            string path = Path.Combine(folder, name + ".url");
            string temp = path + ".part" + Environment.ProcessId;
            try
            {
                Directory.CreateDirectory(folder);
                if (File.Exists(path))
                {
                    ShowTitleMessage("This stream is bookmarked already");
                    return;
                }

                // Windows reads an Internet shortcut in the ANSI code page. Written under another name and moved into place,
                // so that nobody sees the finished name as an empty file.
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var ansi = System.Text.Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                File.WriteAllBytes(temp, ansi.GetBytes("[InternetShortcut]\r\nURL=" + url + "\r\n"));
                File.Move(temp, path);
                ShowTitleMessage("Bookmark added");
                ExplainBookmarksOnce();
            }
            catch (Exception ex)
            {
                try { File.Delete(temp); } catch { }
                MessageBox.Show(this, $"The bookmark could not be made. {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // The bookmarks are seen and opened (at their start point) in SweWolf File Explorer. Without it the player has nowhere to show them, so
        // the first time a bookmark is made on a computer where it cannot be found, the user is told where the bookmark is and what the
        // explorer adds, with the link to its page (the same message, and the Yes / No, as for the other features that need a sibling).
        // Only once (AppSettings.BookmarkExplorerNoticeShown); a bookmark is made all the same.
        private void ExplainBookmarksOnce()
        {
            try
            {
                AppSettings settings = AppSettings.Load();
                if (settings.BookmarkExplorerNoticeShown || FindFileExplorer() != null)
                {
                    return;
                }
                settings.BookmarkExplorerNoticeShown = true;
                settings.Save();

                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SweWolfSoftware", "SwewolfFileExplorer", "Bookmarks");
                DialogResult answer = MessageBox.Show(this,
                    $"The bookmark was saved in this folder:\n{folder}\n\n"
                    + "SweWolf File Explorer shows your bookmarks and opens a video at the point you saved. It was not found on this computer. "
                    + "In another file explorer, a bookmark of a video opens the video from the beginning.\n\n"
                    + "This message is shown only once. Do you want to open the SweWolf File Explorer page on GitHub now?",
                    AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                if (answer == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/SweWolf/SwewolfFileExplorer/releases") { UseShellExecute = true });
                }
            }
            catch (Exception)
            {
                // A notice is never worth an error.
            }
        }

        // The window title says something for a moment.
        private void ShowTitleMessage(string text)
        {
            Text = text;
            var restore = new System.Windows.Forms.Timer { Interval = 2500 };
            restore.Tick += (_, _) =>
            {
                restore.Stop();
                restore.Dispose();
                Text = TitleBase();
            };
            restore.Start();
        }

        // startPointText null: no checkbox, and the buttons move up to close the gap.
        private bool AskBookmark(string file, string? startPointText, out bool useStart)
        {
            int buttonsTop = startPointText == null ? 68 : 106;
            using var dialog = new Form
            {
                Text = "Add Bookmark",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(520, buttonsTop + 44),
                BackColor = Color.FromArgb(43, 43, 43),
                ForeColor = Color.FromArgb(230, 230, 230),
            };
            var label = new Label { Text = file, AutoEllipsis = true, Left = 14, Top = 14, Width = 492, Height = 40 };
            var check = new CheckBox { Text = startPointText, Visible = startPointText != null, Left = 14, Top = 68, Width = 492, Checked = false };
            var ok = new Button
            {
                Text = "OK", DialogResult = DialogResult.OK, Left = 14, Top = buttonsTop, Width = 240, Height = 30,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60),
            };
            var cancel = new Button
            {
                Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 266, Top = buttonsTop, Width = 240, Height = 30,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60),
            };
            dialog.Controls.AddRange(new Control[] { label, check, ok, cancel });
            dialog.AcceptButton = ok;
            dialog.CancelButton = cancel;

            bool accepted = dialog.ShowDialog(this) == DialogResult.OK;
            useStart = accepted && check.Checked;
            return accepted;
        }
    }
}
