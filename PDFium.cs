using System;
using System.Runtime.InteropServices;

namespace PdfiumOverlayTest
{
    public static class PDFium
    {
        private const string DllName = "pdfium.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_InitLibrary();

        [DllImport(DllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadDocument(string filePath, string? password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadMemDocument(IntPtr dataBuffer, int size, string? password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FPDF_GetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_CloseDocument(IntPtr document);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDF_GetPageCount(IntPtr document);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadPage(IntPtr document, int pageIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_ClosePage(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double FPDF_GetPageWidth(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double FPDF_GetPageHeight(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDFBitmap_CreateEx(int width, int height, int format, IntPtr firstScan, int stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int startX, int startY, int sizeX, int sizeY, int rotate, int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFBitmap_Destroy(IntPtr bitmap);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDFText_LoadPage(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFText_ClosePage(IntPtr text_page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDFText_CountChars(IntPtr text_page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FPDFText_GetUnicode(IntPtr text_page, int index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool FPDFText_GetCharBox(
            IntPtr text_page,
            int index,
            out double left,
            out double right,
            out double bottom,
            out double top);
    }
}