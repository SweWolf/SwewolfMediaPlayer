// CreateShortcutForm.cs: Tools > Create Shortcut. Makes shortcuts to the running EXE on the
// desktop, in the Start menu and/or in the Send To menu (Send To is handy here: the player opens
// the file it is sent). Copied from MP3toMP4 (namespace and message box caption changed).
//
// History:
//   2026-10-13  Copied from MP3toMP4; US spelling and Title Case in the labels.

namespace SwewolfMediaPlayer
{
    public partial class CreateShortcutForm : Form
    {
        private readonly string _exePath;
        private readonly string _appName;

        public CreateShortcutForm(string exePath)
        {
            InitializeComponent();
            _exePath = exePath;
            _appName = Application.ProductName ?? Path.GetFileNameWithoutExtension(exePath);
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            if (!chkDesktop.Checked && !chkStartMenu.Checked && !chkSendTo.Checked)
            {
                MessageBox.Show(this, "Please select at least one location.",
                    MainForm.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool allUsers = rdoAllUsers.Checked;
            var created = new List<string>();
            var failed = new List<string>();

            if (chkDesktop.Checked)
            {
                string folder = allUsers
                    ? Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                    : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                TryCreate(Path.Combine(folder, $"{_appName}.lnk"), created, failed);
            }

            if (chkStartMenu.Checked)
            {
                string baseFolder = allUsers
                    ? Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
                    : Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                string programsFolder = Path.Combine(baseFolder, "Programs");
                TryCreate(Path.Combine(programsFolder, $"{_appName}.lnk"), created, failed);
            }

            if (chkSendTo.Checked)
            {
                string sendToFolder = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                TryCreate(Path.Combine(sendToFolder, $"{_appName}.lnk"), created, failed);
            }

            if (failed.Count > 0)
            {
                string msg = "Could not create the following shortcut(s):\n\n" + string.Join("\n", failed);
                if (allUsers)
                {
                    msg += "\n\nCreating shortcuts for all users requires administrator privileges.";
                }
                MessageBox.Show(this, msg, MainForm.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (created.Count > 0)
            {
                MessageBox.Show(this, "Shortcut(s) created successfully:\n\n" + string.Join("\n", created),
                    MainForm.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void TryCreate(string shortcutPath, List<string> created, List<string> failed)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
                Type shellType = Type.GetTypeFromProgID("WScript.Shell")!;
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic lnk = shell.CreateShortcut(shortcutPath);
                lnk.TargetPath = _exePath;
                lnk.WorkingDirectory = Path.GetDirectoryName(_exePath);
                lnk.Description = _appName;
                lnk.IconLocation = _exePath;
                lnk.Save();
                created.Add(shortcutPath);
            }
            catch (Exception ex)
            {
                failed.Add($"{shortcutPath}\n  ({ex.Message})");
            }
        }
    }
}
