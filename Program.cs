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
            ApplicationConfiguration.Initialize();

            // The app always uses the dark theme (there is no setting for it yet).
#pragma warning disable WFO5001   // SetColorMode is still marked experimental in .NET 10
            Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001

            // A file or web address on the command line (e.g. "Open with", or a file dropped on the
            // EXE) opens the player at once and skips the main window; the program ends when the
            // player is closed. A file that does not exist is shown in the main window instead.
            string? path = args.Length > 0 ? args[0].Trim().Trim('"') : null;
            if (!string.IsNullOrEmpty(path))
            {
                if (MediaPlayerForm.TryGetStreamUri(path, out _) || File.Exists(path))
                {
                    Application.Run(new MediaPlayerForm(Path.GetFullPath(path)) { Icon = LoadIcon() });
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