// CloseXButton.cs: the small X in the upper right corner of the Control Panel and Trim group boxes.
// It draws its own X, dark like the other buttons, and turns red while the pointer is over it.
// It never takes the focus (TabStop is false), so it does not change where the keyboard goes.
//
// History:
//   2026-10-26  First version.

using System.Drawing.Drawing2D;

namespace SwewolfMediaPlayer
{
    internal sealed class CloseXButton : Button
    {
        private static readonly Color HoverBack = Color.FromArgb(200, 50, 50);
        private static readonly Color DownBack = Color.FromArgb(160, 35, 35);

        private bool _hot;
        private bool _down;

        public CloseXButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.FromArgb(48, 48, 48);
            ForeColor = Color.FromArgb(230, 230, 230);
            TabStop = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override bool ShowFocusCues => false;

        protected override void OnMouseEnter(EventArgs e) { _hot = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hot = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool hot = Enabled && _hot;
            g.Clear(!hot ? BackColor : _down ? DownBack : HoverBack);

            Color fore = !Enabled ? Color.FromArgb(110, 110, 110) : hot ? Color.White : ForeColor;
            float size = Math.Min(Width, Height) * 0.42f;
            float x = (Width - size) / 2;
            float y = (Height - size) / 2;
            using Pen pen = new(fore, Math.Max(1.5f, size * 0.16f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, x, y, x + size, y + size);
            g.DrawLine(pen, x + size, y, x, y + size);
        }
    }
}
