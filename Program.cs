using System;
using System.Windows.Forms;
using PdfiumOverlayTest.Localization;

namespace PdfiumOverlayTest
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Gespeicherte Sprache laden
            LocalizationHelper.LoadSavedLanguage();
            
            ApplicationConfiguration.Initialize();
            Application.Run(new StartForm());
        }
    }
}