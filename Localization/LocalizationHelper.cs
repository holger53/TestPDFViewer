using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PdfiumOverlayTest.Properties;

namespace PdfiumOverlayTest.Localization
{
    internal static class LocalizationHelper
    {
        public static event EventHandler? LanguageChanged;

        public static string GetCurrentLanguage()
        {
            return Strings.Culture?.Name ?? Thread.CurrentThread.CurrentUICulture.Name;
        }

        public static void SetLanguage(string cultureName)
        {
            ChangeLanguage(cultureName);
        }

        public static void ChangeLanguage(string cultureName)
        {
            var culture = new CultureInfo(cultureName);
            Strings.Culture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            SaveLanguage(cultureName);

            // Event auslösen
            LanguageChanged?.Invoke(null, EventArgs.Empty);

            // Alle offenen Forms aktualisieren
            UpdateAllForms();
        }

        private static void UpdateAllForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form is ILocalizable localizable)
                {
                    localizable.UpdateUI();
                }
            }
        }

        public static void SaveLanguage(string cultureName)
        {
            Settings.Default.Language = cultureName;
            Settings.Default.Save();
        }

        public static void LoadSavedLanguage()
        {
            string? savedLanguage = Settings.Default.Language;
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                var culture = new CultureInfo(savedLanguage);
                Strings.Culture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }
    }
}