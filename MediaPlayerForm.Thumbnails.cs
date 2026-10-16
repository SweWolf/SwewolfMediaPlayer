// MediaPlayerForm.Thumbnails.cs: the picture that pops up above the seek bar while the pointer
// is on it (setting: Show Thumbnail Previews on the Seek Bar). The pictures come from
// FrameThumbnailer (FFmpeg, in memory). Without FFmpeg, for audio files and streams, or if
// anything fails, nothing is shown and nothing is reported.

using System.Drawing.Drawing2D;

namespace SwewolfMediaPlayer
{
    public partial class MediaPlayerForm
    {
        // ffmpeg.exe is searched for once per run.
        private static readonly Lazy<string?> FfmpegPath = new(FindFfmpeg);

        private FrameThumbnailer? _thumbnailer;
        private ThumbnailPopup? _thumbPopup;
        private CancellationTokenSource? _thumbCts;
        private System.Windows.Forms.Timer? _thumbTimer;
        private long _thumbTimeMs;
        private int _thumbX;
        private int _thumbFailures;   // in a row: after three the file gets no more previews
        private bool _thumbHover;

        private bool ThumbnailsWanted =>
            _settings.ShowSeekBarThumbnails && !_isStream && _lengthMs > 0 && !IsAudioFile(_filePath)
            && _thumbFailures < 3 && FfmpegPath.Value != null;

        // The file changed (or was trimmed): forget everything about the old one.
        private void ResetThumbnails()
        {
            HideThumbnail();
            _thumbnailer?.Dispose();
            _thumbnailer = null;
            _thumbFailures = 0;
        }

        private void HideThumbnail()
        {
            _thumbHover = false;
            _thumbTimer?.Stop();
            _thumbCts?.Cancel();
            _thumbPopup?.Hide();
        }

        private void DisposeThumbnails()
        {
            ResetThumbnails();
            _thumbTimer?.Dispose();
            _thumbTimer = null;
            _thumbPopup?.Dispose();
            _thumbPopup = null;
        }

        private void trkSeek_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!ThumbnailsWanted || !trkSeek.Enabled)
            {
                HideThumbnail();
                return;
            }

            _thumbHover = true;
            _thumbX = e.X;
            _thumbTimeMs = (long)(trkSeek.RatioAt(e.X) * _lengthMs);

            Image? cached = _thumbnailer?.TryGetCached(_thumbTimeMs);
            if (cached != null)
            {
                _thumbTimer?.Stop();
                _thumbCts?.Cancel();
                ShowThumbnail(cached);
                return;
            }

            // Wait until the pointer rests for a moment, so a fast sweep does not start FFmpeg
            // for every pixel.
            if (_thumbTimer == null)
            {
                _thumbTimer = new System.Windows.Forms.Timer { Interval = 100 };
                _thumbTimer.Tick += async (s, a) => await LoadThumbnailAsync();
            }
            _thumbTimer.Stop();
            _thumbTimer.Start();
            if (_thumbPopup?.Visible == true)
            {
                PlaceThumbnail();   // the old picture follows the pointer until the new one is ready
            }
        }

        private void trkSeek_MouseLeave(object? sender, EventArgs e)
        {
            // While the thumb is dragged the pointer may leave the bar: keep the picture then.
            if (MouseButtons == MouseButtons.None)
            {
                HideThumbnail();
            }
        }

        private async Task LoadThumbnailAsync()
        {
            _thumbTimer?.Stop();
            string? ffmpeg = FfmpegPath.Value;
            if (!_thumbHover || ffmpeg == null || IsDisposed)
            {
                return;
            }

            FrameThumbnailer thumbnailer = _thumbnailer ??= new FrameThumbnailer(ffmpeg, _filePath, LogicalToDeviceUnits(240), _lengthMs);
            _thumbCts?.Cancel();
            var cts = _thumbCts = new CancellationTokenSource();
            long timeMs = _thumbTimeMs;

            Image? image = await thumbnailer.GetAsync(timeMs, cts.Token);
            if (cts.IsCancellationRequested || IsDisposed || thumbnailer != _thumbnailer)
            {
                return;
            }
            if (image == null)
            {
                _thumbFailures++;
                return;
            }

            _thumbFailures = 0;
            if (_thumbHover)
            {
                _thumbTimeMs = timeMs;
                ShowThumbnail(image);
            }
        }

        private void ShowThumbnail(Image image)
        {
            _thumbPopup ??= new ThumbnailPopup();
            _thumbPopup.SetPicture(image, FormatThumbnailTime(_thumbTimeMs));
            PlaceThumbnail();
            if (!_thumbPopup.Visible)
            {
                _thumbPopup.Show(this);
            }
        }

        // Centered over the pointer, just above the seek bar, and inside the screen.
        private void PlaceThumbnail()
        {
            if (_thumbPopup == null)
            {
                return;
            }

            Point onScreen = trkSeek.PointToScreen(new Point(_thumbX, 0));
            Rectangle screen = Screen.FromControl(this).Bounds;
            int x = Math.Clamp(onScreen.X - _thumbPopup.Width / 2, screen.Left, Math.Max(screen.Left, screen.Right - _thumbPopup.Width));
            int y = onScreen.Y - _thumbPopup.Height - LogicalToDeviceUnits(8);
            _thumbPopup.Location = new Point(x, Math.Max(y, screen.Top));
        }

        private static string FormatThumbnailTime(long ms)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}";
        }
    }

    // The small window with the picture and the time. It never takes the focus and the mouse
    // goes through it, so the seek bar below keeps working.
    internal sealed class ThumbnailPopup : Form
    {
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WM_NCHITTEST = 0x84;
        private const int HTTRANSPARENT = -1;

        private Image? _image;
        private string _time = "";

        public ThumbnailPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            DoubleBuffered = true;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        public void SetPicture(Image image, string time)
        {
            _image = image;
            _time = time;
            Size size = image.Size + new Size(2, 2);
            if (ClientSize != size)
            {
                ClientSize = size;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (_image != null)
            {
                g.DrawImage(_image, 1, 1, _image.Width, _image.Height);
            }

            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            SizeF text = g.MeasureString(_time, font);
            var pill = new RectangleF((Width - text.Width) / 2 - 6, Height - text.Height - 10, text.Width + 12, text.Height + 2);
            using (var back = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            {
                g.FillRectangle(back, pill);
            }
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.DrawString(_time, font, Brushes.White, pill.X + 6, pill.Y + 1);

            using var border = new Pen(Color.FromArgb(120, 120, 120));
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }
    }
}
