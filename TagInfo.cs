using System.Drawing;

namespace PdfiumOverlayTest
{
    public class TagInfo
    {
        public string Text { get; set; } = string.Empty;
        public Color Color { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsCustomText { get; set; }
    }
}