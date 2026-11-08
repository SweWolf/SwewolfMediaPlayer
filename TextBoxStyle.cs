// TextBoxStyle.cs: the standard look and behavior of the single-line text boxes in the SweWolf apps.
//
//  - A little space between the left edge and the text (about half a character; without it the text touches the border).
//  - Arriving with the keyboard (Tab, Shift+Tab, a mnemonic, or code calling Focus) selects all the text, as if the
//    user pressed Ctrl+A, with the caret at the end. Clicking with the mouse does not: the click sets the caret.
//
// Self-contained: to reuse it in another app, copy the file and change the namespace. The margin idea comes from
// FFmpegAssistant's Form1 (EM_SETMARGINS with a fixed 5 px); this version scales with the display and sets the
// margin again if Windows re-creates the text box's window (the margin is lost then).
//
// History:
//   2026-11-08  First version (SweWolf Media Player: the four boxes of the Trim group): margin and select all on keyboard focus.

using System.Runtime.InteropServices;

namespace SwewolfMediaPlayer
{
    internal static class TextBoxStyle
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int EM_SETMARGINS = 0xD3;
        private const int EC_LEFTMARGIN = 0x1;

        // Both rules. marginDip: the space in device independent pixels at 96 dpi (4 is about half a character in the default font).
        public static void Apply(TextBox box, int marginDip = 4)
        {
            ApplyMargin(box, marginDip);
            SelectAllOnKeyboardFocus(box);
        }

        public static void ApplyMargin(TextBox box, int marginDip = 4)
        {
            void Set()
            {
                int pixels = box.LogicalToDeviceUnits(marginDip);
                SendMessage(box.Handle, EM_SETMARGINS, EC_LEFTMARGIN, pixels);
            }

            box.HandleCreated += (_, _) => Set();
            box.DpiChangedAfterParent += (_, _) => Set();
            if (box.IsHandleCreated)
            {
                Set();
            }
        }

        // With no mouse button down the focus came from the keyboard or from code; a click must keep its own caret position.
        public static void SelectAllOnKeyboardFocus(TextBox box)
        {
            box.Enter += (_, _) =>
            {
                if (Control.MouseButtons == MouseButtons.None)
                {
                    box.SelectAll();
                }
            };
        }
    }
}
