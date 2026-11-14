// CountdownMessageBox.cs: a Yes / No question whose Yes button is pressed by itself after a number of seconds, so a question
// like "Do you want to resume?" does not hold up someone who is away from the computer (as in TeraTool). The Yes button shows
// the seconds left ("Yes [10]"). As soon as the user moves the mouse or touches the keyboard the countdown stops and the question
// waits as an ordinary message box. Self-contained: copy the file to another app and change the namespace (the sibling
// File Explorer has the same idea in its ChoiceDialog, WPF).
//
// History:
//   2026-11-13  First version, for the resume question of the Media Player (10 seconds, Yes).

namespace SwewolfMediaPlayer
{
    internal sealed class CountdownMessageBox : Form, IMessageFilter
    {
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_LBUTTONDOWN = 0x0201;

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
        private readonly Button _yes = new();
        private readonly Button _no = new();
        private int _remaining;
        private bool _counting;
        private Point _startMouse;
        private bool _ready;

        private CountdownMessageBox(string text, string caption, int seconds)
        {
            Text = caption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            PictureBox icon = new() { Image = SystemIcons.Question.ToBitmap(), SizeMode = PictureBoxSizeMode.AutoSize, Location = new Point(16, 16) };
            Label label = new() { Text = text, AutoSize = true, MaximumSize = new Size(380, 0), Location = new Point(64, 20), UseMnemonic = false };
            Controls.Add(icon);
            Controls.Add(label);

            _yes.Text = "Yes";
            _yes.DialogResult = DialogResult.Yes;
            _no.Text = "No";
            _no.DialogResult = DialogResult.No;
            _yes.Size = _no.Size = new Size(110, 28);   // wide enough for "Yes [10]"
            int bottom = Math.Max(icon.Bottom, label.Bottom) + 20;
            _no.Location = new Point(Math.Max(label.Right, 300) - _no.Width + 12, bottom);
            _yes.Location = new Point(_no.Left - _yes.Width - 8, bottom);
            Controls.Add(_yes);
            Controls.Add(_no);
            AcceptButton = _yes;
            CancelButton = _no;
            ClientSize = new Size(_no.Right + 16, _no.Bottom + 16);

            _remaining = seconds;
            _counting = seconds > 0;
            _timer.Tick += (_, _) =>
            {
                if (--_remaining <= 0)
                {
                    _timer.Stop();
                    _yes.PerformClick();
                    return;
                }
                ShowSeconds();
            };
        }

        private void ShowSeconds() => _yes.Text = _counting ? $"Yes [{_remaining}]" : "Yes";

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _startMouse = Cursor.Position;
            _ready = true;
            Application.AddMessageFilter(this);
            _yes.Focus();
            if (_counting)
            {
                ShowSeconds();
                _timer.Start();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _timer.Stop();
            base.OnFormClosed(e);
        }

        // The user is here: stop the countdown (a mouse that only rests under the box does not count).
        bool IMessageFilter.PreFilterMessage(ref Message m)
        {
            if (!_ready || !_counting)
            {
                return false;
            }

            bool active = m.Msg == WM_KEYDOWN || m.Msg == WM_LBUTTONDOWN;
            if (m.Msg == WM_MOUSEMOVE)
            {
                Point now = Cursor.Position;
                active = Math.Abs(now.X - _startMouse.X) > 2 || Math.Abs(now.Y - _startMouse.Y) > 2;
            }
            if (active && FromChildHandle(m.HWnd) is { } control && control.FindForm() == this)
            {
                _counting = false;
                _timer.Stop();
                ShowSeconds();
            }
            return false;
        }

        // True for Yes (also when the countdown ran out), false for No or when the box is closed.
        public static bool AskYesNo(IWin32Window owner, string text, string caption, int seconds)
        {
            using CountdownMessageBox box = new(text, caption, seconds);
            return box.ShowDialog(owner) == DialogResult.Yes;
        }
    }
}
