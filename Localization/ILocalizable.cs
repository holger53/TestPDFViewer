namespace PdfiumOverlayTest.Localization
{
    /// <summary>
    /// Interface für Forms, die ihre UI bei Sprachänderungen aktualisieren können
    /// </summary>
    internal interface ILocalizable
    {
        /// <summary>
        /// Aktualisiert alle UI-Texte basierend auf der aktuellen Sprache
        /// </summary>
        void UpdateUI();
    }
}