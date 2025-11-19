using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    public class TransparentOverlayForm : Form
    {
        public Color FillColor { get; set; } = Color.FromArgb(100, 255, 255, 0);
        public string TagText { get; private set; } = string.Empty;
        public Color TagColor { get; private set; } = Color.FromArgb(220, 120, 0);

        // GEÄNDERT: Diese als Properties öffentlich machen
        private string _currentText = string.Empty;
        private Color _currentColor = Color.Empty;

        // NEU: Öffentliche Properties hinzufügen
        public string CurrentText => _currentText;
        public Color CurrentColor => _currentColor;

        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public TransparentOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (IsInDesigner()) return;
            UpdateLayeredWindow();
        }

        public void SetTag(string text, Color color)
        {
            _currentText = text;
            _currentColor = color;
            TagText = text;  // NEU: Auch TagText setzen
            TagColor = color; // NEU: Auch TagColor setzen

            // NEU: Automatische Breiteanpassung für längere Texte
            if (text.Length > 5)
            {
                // Berechne benötigte Breite (ca. 15 Pixel pro Zeichen)
                int neededWidth = Math.Max(200, text.Length * 15);
                this.Width = Math.Min(neededWidth, 600); // Max 600px
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsInDesigner())
            {
                e.Graphics.Clear(Color.Transparent);
                using var brush = new SolidBrush(Color.FromArgb(120, FillColor.R, FillColor.G, FillColor.B));
                e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
                using var pen = new Pen(Color.FromArgb(200, Color.Black), 3);
                e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
                return;
            }
            base.OnPaint(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (IsInDesigner()) return;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (IsInDesigner()) return;
            UpdateLayeredWindow();
        }

        private void UpdateLayeredWindow()
        {
            if (IsInDesigner()) return;
            if (Width <= 0 || Height <= 0 || !IsHandleCreated)
                return;

            using (var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias; // NEU

                    using (var brush = new SolidBrush(Color.FromArgb(120, FillColor.R, FillColor.G, FillColor.B)))
                        g.FillRectangle(brush, 0, 0, Width, Height);

                    using (var pen = new Pen(Color.FromArgb(200, Color.Black), 3))
                        g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);

                    // NEU: Zeichne Text wenn vorhanden
                    if (!string.IsNullOrEmpty(_currentText))
                    {
                        // Wähle Schriftgröße basierend auf Textlänge
                        float fontSize = _currentText.Length > 5 ? 14F : 18F; // Kleinere Schrift für lange Texte

                        using (var font = new Font("Arial", fontSize, FontStyle.Bold))
                        using (var textBrush = new SolidBrush(Color.FromArgb(220, Color.White))) // Leicht transparent
                        using (var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        })
                        {
                            g.DrawString(_currentText, font, textBrush,
                                new RectangleF(0, 0, Width, Height), sf);
                        }
                    }
                }
                SetBitmap(bitmap, 255);
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

                UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, ULW_ALPHA);
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

        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const int ULW_ALPHA = 0x02;

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }
        [StructLayout(LayoutKind.Sequential)] private struct SIZE { public int cx; public int cy; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct BLENDFUNCTION { public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat; }

        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll", SetLastError = true)] private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
    }
}