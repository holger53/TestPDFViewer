using System;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    public class OverlayControl : Control
    {
        public Color FillColor { get; set; } = Color.Yellow;
        public string TagText { get; private set; } = string.Empty;
        public Color TagColor { get; private set; } = Color.FromArgb(220, 120, 0);

        public OverlayControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.SupportsTransparentBackColor | 
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Width = 200;
            Height = 60;
            
            // Wichtig: Verhindere, dass das Control den Fokus erhält
            SetStyle(ControlStyles.Selectable, false);
        }

        public void SetTag(string text, Color color)
        {
            TagText = text;
            TagColor = color;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Semi-transparent overlay rectangle
            using var brush = new SolidBrush(Color.FromArgb(140, FillColor));
            g.FillRectangle(brush, 0, 0, Width - 1, Height - 1);

            // Border
            using var pen = new Pen(Color.FromArgb(220, Color.Black), 2);
            g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);

            // Tag-Vorschau (nur zur Anzeige, wird NICHT eingebrannt hier)
            if (!string.IsNullOrEmpty(TagText))
            {
                var tagW = 40;
                var tagH = 20;
                var tagX = Width - tagW - 8;
                var tagY = 6;
                
                using var tagBrush = new SolidBrush(TagColor);
                g.FillRectangle(tagBrush, tagX, tagY, tagW, tagH);
                
                using var sf = new StringFormat 
                { 
                    Alignment = StringAlignment.Center, 
                    LineAlignment = StringAlignment.Center 
                };
                using var font = new Font("Arial", 9, FontStyle.Bold);
                g.DrawString(TagText, font, Brushes.White, 
                    new RectangleF(tagX, tagY, tagW, tagH), sf);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        // KRITISCH: Verhindert, dass das Overlay Tastenevents abfängt
        protected override bool IsInputKey(Keys keyData)
        {
            return false;
        }

        public override bool PreProcessMessage(ref Message msg)
        {
            // Leite alle Tastatur-Events an das Parent-Form weiter
            return false;
        }
    }
}