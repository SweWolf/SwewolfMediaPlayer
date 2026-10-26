// SpeedBox.cs: the playback speed box of the control bar: the speed as text ("1x", "1.5x") with an up and a
// down arrow stacked on its right, like a spin box. It draws everything itself, so it keeps the dark theme
// (the arrows of a Windows spin box stay light). It never takes the focus.
//
// It only tells what the user asked for (Step: +1 up, -1 down; ResetRequested: double click on the text);
// the list of speeds and the player are the form's business (MediaPlayerForm.Speed.cs).
//
// History:
//   2026-10-26  First version.

using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SwewolfMediaPlayer
{
    internal sealed class SpeedBox : Control
    {
        private enum Part { None, Text, Up, Down }

        private static readonly Color Line = Color.FromArgb(70, 70, 70);

        private string _valueText = "1x";
        private bool _highlight;
        private Part _hot = Part.None;
        private Part _down = Part.None;

        public SpeedBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            BackColor = Color.FromArgb(48, 48, 48);
            ForeColor = Color.FromArgb(230, 230, 230);
        }

        // +1 = the up arrow (faster), -1 = the down arrow (slower).
        public event EventHandler<int>? Step;

        // A double click on the text: back to the normal speed.
        public event EventHandler? ResetRequested;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ValueText
        {
            get => _valueText;
            set { _valueText = value; Invalidate(); }
        }

        // The speed is not the normal one: the text is drawn in the accent color of the seek bar.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Highlight
        {
            get => _highlight;
            set { _highlight = value; Invalidate(); }
        }

        private int ArrowWidth => (int)(Height * 0.56f);

        private Part PartAt(Point p)
        {
            if (p.X < 0 || p.Y < 0 || p.X >= Width || p.Y >= Height)
            {
                return Part.None;
            }
            if (p.X < Width - ArrowWidth)
            {
                return Part.Text;
            }
            return p.Y < Height / 2 ? Part.Up : Part.Down;
        }

        private Rectangle RectOf(Part part)
        {
            int arrow = ArrowWidth;
            return part switch
            {
                Part.Up => new Rectangle(Width - arrow, 0, arrow, Height / 2),
                Part.Down => new Rectangle(Width - arrow, Height / 2, arrow, Height - Height / 2),
                _ => new Rectangle(0, 0, Width - arrow, Height),
            };
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Part part = Enabled ? PartAt(e.Location) : Part.None;
            if (part != _hot)
            {
                _hot = part;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hot = _down = Part.None;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Enabled && e.Button == MouseButtons.Left)
            {
                Part part = PartAt(e.Location);
                _down = part;
                Invalidate();
                if (part == Part.Up)
                {
                    Step?.Invoke(this, +1);
                }
                else if (part == Part.Down)
                {
                    Step?.Invoke(this, -1);
                }
                else if (part == Part.Text && e.Clicks == 2)
                {
                    ResetRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _down = Part.None;
            Invalidate();
            base.OnMouseUp(e);
        }

        // The wheel steps like the arrows: up = faster.
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Enabled && e.Delta != 0)
            {
                Step?.Invoke(this, e.Delta > 0 ? +1 : -1);
            }
            base.OnMouseWheel(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            _hot = _down = Part.None;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fore = !Enabled ? Color.FromArgb(110, 110, 110) : ForeColor;
            Color textColor = !Enabled ? fore : _highlight ? Color.FromArgb(90, 165, 255) : ForeColor;

            Rectangle text = RectOf(Part.Text);
            TextRenderer.DrawText(g, _valueText, Font, text, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            foreach (Part part in new[] { Part.Up, Part.Down })
            {
                Rectangle r = RectOf(part);
                if (Enabled && (_down == part || _hot == part))
                {
                    using SolidBrush hover = new(_down == part ? Lighten(BackColor, 0.25f) : Lighten(BackColor, 0.12f));
                    g.FillRectangle(hover, r);
                }

                // A small triangle in the middle of its half.
                float w = Math.Max(5f, r.Width * 0.38f);
                float h = w * 0.55f;
                float cx = r.X + r.Width / 2f;
                float cy = r.Y + r.Height / 2f;
                PointF[] triangle = part == Part.Up
                    ? new[] { new PointF(cx - w / 2, cy + h / 2), new PointF(cx + w / 2, cy + h / 2), new PointF(cx, cy - h / 2) }
                    : new[] { new PointF(cx - w / 2, cy - h / 2), new PointF(cx + w / 2, cy - h / 2), new PointF(cx, cy + h / 2) };
                using SolidBrush brush = new(fore);
                g.FillPolygon(brush, triangle);
            }

            using Pen line = new(Line);
            g.DrawLine(line, Width - ArrowWidth, 2, Width - ArrowWidth, Height - 3);
        }

        private static Color Lighten(Color c, float amount)
        {
            return Color.FromArgb(c.R + (int)((255 - c.R) * amount), c.G + (int)((255 - c.G) * amount), c.B + (int)((255 - c.B) * amount));
        }
    }
}
