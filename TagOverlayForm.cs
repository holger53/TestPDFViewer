using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    public class TagOverlayForm : Form
    {
        public string TagText { get; private set; } = string.Empty;
        public Color TagColor { get; private set; } = Color.FromArgb(220, 120, 0);

        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public TagOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.None;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);

            // Designer-Vorschau: einfache Fläche statt Layered-Window
            if (IsInDesigner())
            {
                BackColor = Color.FromArgb(220, 120, 0);
                ForeColor = Color.White;
            }

            // Aktuelle Größe (Breite x Höhe) – läuft in Designer ebenfalls
            Size = new Size(58, 42);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                if (!IsInDesigner())
                    cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        public void SetTag(string text, Color color)
        {
            TagText = text;
            TagColor = color;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (IsInDesigner()) return;

            if (!string.IsNullOrEmpty(TagText))
            {
                UpdateLayeredWindow();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (IsInDesigner()) return;

            if (IsHandleCreated)
                UpdateLayeredWindow();
        }

        public void UpdateDisplay()
        {
            if (!string.IsNullOrEmpty(TagText) && IsHandleCreated && !IsInDesigner())
            {
                UpdateLayeredWindow();
            }
        }

        private void UpdateLayeredWindow()
        {
            if (IsInDesigner()) return;
            if (Width <= 0 || Height <= 0 || !IsHandleCreated) return;

            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    using (var tagBrush = new SolidBrush(TagColor))
                        g.FillRectangle(tagBrush, 0, 0, Width, Height);

                    using (var pen = new Pen(Color.Black, 2))
                        g.DrawRectangle(pen, 0, 0, Width - 2, Height - 2);

                    if (!string.IsNullOrEmpty(TagText))
                    {
                        using var font = new Font("Arial", 23, FontStyle.Bold);
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(TagText, font, Brushes.White, new RectangleF(0, 0, Width, Height), sf);
                    }
                }

                SetBitmap(bmp, 255);
            }
            finally
            {
                bmp.Dispose();
            }
        }

        private void SetBitmap(Bitmap bitmap, byte opacity)
        {
            if (IsInDesigner()) return;
            if (!IsHandleCreated || bitmap == null)
                return;

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hOldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                hOldBitmap = SelectObject(memDc, hBitmap);

                SIZE size = new SIZE { cx = bitmap.Width, cy = bitmap.Height };
                POINT pointSource = new POINT { x = 0, y = 0 };
                POINT topPos = new POINT { x = Left, y = Top };
                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = opacity,
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size,
                    memDc, ref pointSource, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, hOldBitmap);
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsInDesigner())
            {
                e.Graphics.Clear(Color.FromArgb(32, Color.SteelBlue));
                using var pen = new Pen(Color.SteelBlue, 2);
                e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2);
                return;
            }
            base.OnPaint(e);
        }

        // P/Invoke
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const int ULW_ALPHA = 0x02;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx; public int cy; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(
            IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
    }
}