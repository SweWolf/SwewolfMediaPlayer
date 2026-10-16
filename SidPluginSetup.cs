// SidPluginSetup.cs: SID (Commodore 64 music) support for LibVLC.
//
// The LibVLC package leaves out the SID plugin (libsid_plugin.dll, it contains GPL code), but a
// normal VLC installation has it. This class copies the plugin from the user's own VLC into a
// per-user folder, %AppData%\SweWolfSoftware\SwewolfMediaPlayer\Plugins\demux, which LibVLC reads
// through the VLC_PLUGIN_PATH variable (LibVLC 3 has no command-line option for it). Nothing from
// VLC is shipped with the program.
//
// Self-contained: to reuse it in another app, change the namespace and AppFolder.
//
// History:
//   2026-10-16  First version: finds VLC, checks the version and the bitness, copies the plugin.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SwewolfMediaPlayer
{
    internal static class SidPluginSetup
    {
        private const string AppFolder = "SwewolfMediaPlayer";
        private const string PluginFileName = "libsid_plugin.dll";

        // The folder LibVLC is told to search in addition to its own plugin folder.
        public static string PluginRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SweWolfSoftware", AppFolder, "Plugins");

        private static string PluginFolder => Path.Combine(PluginRoot, "demux");
        private static string PluginPath => Path.Combine(PluginFolder, PluginFileName);

        public static bool IsInstalled => File.Exists(PluginPath);

        public static bool IsSidFile(string path) => path.EndsWith(".sid", StringComparison.OrdinalIgnoreCase);

        // Must be called before the first LibVLC instance is created. LibVLC reads the variable
        // from the C runtime's copy of the environment, which Environment.SetEnvironmentVariable
        // does not change, so it is set through the C runtime as well.
        public static void ConfigurePluginPath()
        {
            try
            {
                Directory.CreateDirectory(PluginFolder);
                string? existing = Environment.GetEnvironmentVariable("VLC_PLUGIN_PATH");
                string value = string.IsNullOrEmpty(existing) ? PluginRoot : PluginRoot + ";" + existing;
                Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", value);
                TryPutEnv(() => UcrtPutEnv("VLC_PLUGIN_PATH", value));
                TryPutEnv(() => MsvcrtPutEnv("VLC_PLUGIN_PATH", value));
            }
            catch (Exception)
            {
                // Without the folder only SID files are affected, and they report the problem.
            }
        }

        private static void TryPutEnv(Action put)
        {
            try
            {
                put();
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
            }
        }

        [DllImport("ucrtbase.dll", EntryPoint = "_putenv_s", CharSet = CharSet.Ansi)]
        private static extern int UcrtPutEnv(string name, string value);

        [DllImport("msvcrt.dll", EntryPoint = "_putenv_s", CharSet = CharSet.Ansi)]
        private static extern int MsvcrtPutEnv(string name, string value);

        // Copies the plugin from VLC. Returns null when it worked, otherwise the reason it did not.
        // libVlcVersion is LibVLC's version text, e.g. "3.0.24 Vetinari": the plugin must come from
        // the same version.
        public static string? TryInstall(string libVlcVersion)
        {
            if (RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.X86))
            {
                return "This version of the player cannot copy the plugin on this type of computer.";
            }

            string? vlcFolder = FindVlcFolder();
            if (vlcFolder == null)
            {
                return "VLC media player was not found on this computer.";
            }

            string source = Path.Combine(vlcFolder, "plugins", "demux", PluginFileName);
            if (!File.Exists(source))
            {
                return $"The file {PluginFileName} was not found in your VLC installation (\"{vlcFolder}\").";
            }

            string wanted = new string(libVlcVersion.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
            string? found = ReadVersion(Path.Combine(vlcFolder, "libvlc.dll"));
            if (found == null || found != wanted)
            {
                return $"Your VLC is version {found ?? "unknown"}, but the plugin must come from version {wanted}.";
            }

            try
            {
                Directory.CreateDirectory(PluginFolder);
                string temp = PluginPath + ".tmp";
                File.Copy(source, temp, true);
                File.Move(temp, PluginPath, true);
                return null;
            }
            catch (Exception ex)
            {
                return $"The plugin could not be copied: {ex.Message}";
            }
        }

        // The VLC folder with the same bitness as this process (a 32-bit plugin does not load into
        // a 64-bit process), or null.
        private static string? FindVlcFolder()
        {
            var candidates = new List<string>();
            RegistryView view = Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32;
            foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                try
                {
                    using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\VideoLAN\VLC");
                    if (key?.GetValue("InstallDir") is string dir)
                    {
                        candidates.Add(dir);
                    }
                }
                catch (Exception)
                {
                }
            }

            string programFiles = Environment.GetFolderPath(Environment.Is64BitProcess
                ? Environment.SpecialFolder.ProgramFiles
                : Environment.SpecialFolder.ProgramFilesX86);
            candidates.Add(Path.Combine(programFiles, "VideoLAN", "VLC"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "VideoLAN", "VLC"));

            return candidates.FirstOrDefault(dir => File.Exists(Path.Combine(dir, "libvlc.dll")));
        }

        private static string? ReadVersion(string file)
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
                return $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}";
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Shown when the plugin could not be copied: what to do by hand.
        public static string ManualInstructions(string reason)
        {
            return $"SID files need a plugin from VLC media player, and it could not be set up automatically.\n\n{reason}\n\n" +
                "To fix it by hand:\n" +
                "1. Install VLC media player (the same version as the player uses, from videolan.org).\n" +
                $"2. Copy {PluginFileName} from VLC's folder plugins\\demux to:\n    {PluginFolder}\n" +
                "3. Open the file again.\n\n" +
                "Open that folder now?";
        }

        public static void OpenPluginFolder()
        {
            try
            {
                Directory.CreateDirectory(PluginFolder);
                Process.Start(new ProcessStartInfo(PluginFolder) { UseShellExecute = true });
            }
            catch (Exception)
            {
            }
        }
    }
}
