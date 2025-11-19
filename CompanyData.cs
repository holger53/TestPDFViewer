using System.Drawing;
using System.Text.Json.Serialization;

namespace PdfiumOverlayTest
{
    public class CompanyData
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public override string ToString() => 
            string.IsNullOrWhiteSpace(CompanyName) ? "Keine Daten" : CompanyName;
    }
}