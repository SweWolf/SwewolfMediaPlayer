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
            OpenTimingLog.Write($"Main: started, program folder: {AppContext.BaseDirectory}, arguments: {string.Join(" ", args)}");
            // --warm-up (from the explorer, a few seconds after it starts): load the video engine once, so that
            // the antivirus program has scanned its files and Windows has them cached when a real video is opened,
            // then end. No window, no pipe, no library file; if a player is running already there is nothing to do.
            if (args.Length == 1 && args[0].Equals("--warm-up", StringComparison.OrdinalIgnoreCase))
            {
                WarmUp();
                return;
            }

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
            long startAtMs = 0;       // --start-at <ms> / --no-resume: from the explorer's Demo Play and Intro Play
            bool noResume = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--no-resume", StringComparison.OrdinalIgnoreCase))
                {
                    noResume = true;
                    continue;
                }
                if (args[i].Equals("--start-at", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    long.TryParse(args[++i], out startAtMs);
                    continue;
                }

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
                    // One player window per user: a second start with a file hands the file to the first one (through the pipe,
                    // PlayerPipe.cs) and ends. If no player answers, this one goes on as a player of its own.
                    using var oneWindow = new Mutex(true, PlayerPipe.MutexName, out bool first);
                    if (!first && PlayerPipe.TrySend(new PipeCommand("play", Path.GetFullPath(path),
                            string.IsNullOrEmpty(libraryPath) ? null : libraryPath, StartMs: startAtMs, NoResume: noResume, Front: true)))
                    {
                        OpenTimingLog.Write("Main: the file was handed to the player that is running");
                        return;
                    }

                    // Load the video engine while the window is being made (the main window does the same).
                    _ = MediaPlayerForm.PreloadAsync();
                    var window = new MediaPlayerForm(Path.GetFullPath(path), string.IsNullOrEmpty(libraryPath) ? null : libraryPath) { Icon = LoadIcon() };
                    window.SetFirstFileOptions(startAtMs, noResume);
                    PlayerPipe? pipe = first ? new PlayerPipe() : null;
                    window.AttachPipe(pipe);
                    pipe?.Start();
                    Application.Run(window);
                    pipe?.Dispose();
                    return;
                }
                MessageBox.Show($"The file \"{path}\" does not exist.", "SweWolf Media Player",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Application.Run(new MainForm(path));
        }

        private static void WarmUp()
        {
            try
            {
                // The same mutex as a real player: when one runs, its files are loaded already.
                using var oneWindow = new Mutex(true, PlayerPipe.MutexName, out bool first);
                if (!first)
                {
                    OpenTimingLog.Write("Warm-up: a player is running already, nothing to do");
                    return;
                }
                var watch = System.Diagnostics.Stopwatch.StartNew();
                // At most 2 minutes: a stuck load must never leave a hidden process behind.
                bool done = MediaPlayerForm.PreloadAsync().Wait(TimeSpan.FromMinutes(2));
                OpenTimingLog.Write(done ? $"Warm-up: done after {watch.ElapsedMilliseconds} ms" : "Warm-up: gave up after 2 minutes");
            }
            catch (Exception ex)
            {
                OpenTimingLog.Write($"Warm-up: failed: {ex.Message}");
            }
            // The process ends here: Main returns and the engine is dropped with it (nothing was ever shown).
        }

        // The icon is built into the program, so it also works when only the EXE is shipped.
        internal static Icon? LoadIcon()
        {
            using Stream? stream = typeof(Program).Assembly.GetManifestResourceStream("SwewolfMediaPlayer.ico");
            return stream == null ? null : new Icon(stream);
        }
    }
}