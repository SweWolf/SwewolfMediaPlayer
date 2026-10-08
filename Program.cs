namespace SwewolfMediaPlayer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // The app always uses the dark theme (there is no setting for it yet).
#pragma warning disable WFO5001   // SetColorMode is still marked experimental in .NET 10
            Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001
            Application.Run(new MainForm());
        }
    }
}