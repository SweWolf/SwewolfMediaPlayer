namespace SwewolfMediaPlayer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            OpenTimingLog.Write($"Main: started, arguments: {string.Join(" ", args)}");
            ApplicationConfiguration.Initialize();

            // For testing a copy of the player without touching the real library file.
            string? testDb = Environment.GetEnvironmentVariable("SWEWOLF_LIBRARY_TEST_DB");
            if (!string.IsNullOrEmpty(testDb))
            {
                LibraryStore.MainDbFileOverride = testDb;
            }

            // A damaged library file is replaced by LibraryStore (the newest good backup, or a new file): say so.
            LibraryStore.Repaired += (movedTo, whatNow) => Task.Run(() =>
                MessageBox.Show($"The file that remembers which files you have opened was damaged. It was moved to:\n{movedTo}\n\n{whatNow}",
                    "SweWolf Media Player", MessageBoxButtons.OK, MessageBoxIcon.Warning));

            // One backup of the main library file a day (the first start of the day), the last 7 are kept.
            Task.Run(() => LibraryStore.BackUpMainFile());

            // The app always uses the dark theme (there is no setting for it yet).
#pragma warning disable WFO5001   // SetColorMode is still marked experimental in .NET 10
            Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001

            // A file or web address on the command line (e.g. "Open with", or a file dropped on the
            // EXE) opens the player at once and skips the main window; the program ends when the
            // player is closed. A file that does not exist is shown in the main window instead.
            //
            // --library-path "<path>" says what the file is called in the library (read marks, resume points): the
            // explorer passes it when it has unpacked the file from an archive to play it.
            string? path = null;
            string? libraryPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--library-path", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    libraryPath = args[++i].Trim().Trim('"');
                }
                else if (path == null)
                {
                    path = args[i].Trim().Trim('"');
                }
            }
            if (!string.IsNullOrEmpty(path))
            {
                if (MediaPlayerForm.TryGetStreamUri(path, out _) || File.Exists(path))
                {
                    // Load the video engine while the window is being made (the main window does the same).
                    _ = MediaPlayerForm.PreloadAsync();
                    Application.Run(new MediaPlayerForm(Path.GetFullPath(path), string.IsNullOrEmpty(libraryPath) ? null : libraryPath) { Icon = LoadIcon() });
                    return;
                }
                MessageBox.Show($"The file \"{path}\" does not exist.", "SweWolf Media Player",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Application.Run(new MainForm(path));
        }

        // The icon is built into the program, so it also works when only the EXE is shipped.
        internal static Icon? LoadIcon()
        {
            using Stream? stream = typeof(Program).Assembly.GetManifestResourceStream("SwewolfMediaPlayer.ico");
            return stream == null ? null : new Icon(stream);
        }
    }
}