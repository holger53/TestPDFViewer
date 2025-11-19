using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfiumOverlayTest
{
    /// <summary>
    /// Repräsentiert eine erkannte Kontobewegung in der PDF
    /// </summary>
    public class TransactionPosition
    {
        /// <summary>
        /// Position auf der Seite (X, Y in Pixel bei RENDER_DPI)
        /// </summary>
        public int X { get; set; }
        public int Y { get; set; }
        
        /// <summary>
        /// Seitennummer (0-basiert)
        /// </summary>
        public int PageIndex { get; set; }
        
        /// <summary>
        /// Erkannter Text der Transaktion (optional für Debugging)
        /// </summary>
        public string? TransactionText { get; set; }
        
        /// <summary>
        /// Datum der Transaktion (falls erkannt)
        /// </summary>
        public DateTime? Date { get; set; }
        
        /// <summary>
        /// Betrag der Transaktion (falls erkannt)
        /// </summary>
        public decimal? Amount { get; set; }
    }
    
    /// <summary>
    /// Container für alle erkannten Transaktionen einer PDF
    /// </summary>
    public class TransactionData
    {
        /// <summary>
        /// Hash der PDF-Datei zur Identifikation
        /// </summary>
        public string PdfHash { get; set; } = string.Empty;
        
        /// <summary>
        /// Ursprünglicher Dateipfad
        /// </summary>
        public string OriginalFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Zeitpunkt der Analyse
        /// </summary>
        public DateTime AnalyzedAt { get; set; }
        
        /// <summary>
        /// Liste aller erkannten Transaktionen
        /// </summary>
        public List<TransactionPosition> Transactions { get; set; } = new List<TransactionPosition>();
        
        /// <summary>
        /// Speichert die Transaktionsdaten als JSON
        /// </summary>
        public void Save(string pdfFilePath)
        {
            try
            {
                string jsonPath = GetJsonPath(pdfFilePath);
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(jsonPath, json);
                
                System.Diagnostics.Debug.WriteLine($"Transaktionsdaten gespeichert: {jsonPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Transaktionsdaten: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Lädt die Transaktionsdaten aus JSON
        /// </summary>
        public static TransactionData? Load(string pdfFilePath)
        {
            try
            {
                string jsonPath = GetJsonPath(pdfFilePath);
                
                if (!File.Exists(jsonPath))
                    return null;
                
                var json = File.ReadAllText(jsonPath);
                var data = JsonSerializer.Deserialize<TransactionData>(json);
                
                System.Diagnostics.Debug.WriteLine($"Transaktionsdaten geladen: {jsonPath}");
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Transaktionsdaten: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Generiert den JSON-Dateipfad basierend auf der PDF
        /// </summary>
        private static string GetJsonPath(string pdfFilePath)
        {
            string directory = Path.GetDirectoryName(pdfFilePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(pdfFilePath);
            return Path.Combine(directory, $"{fileNameWithoutExt}_transactions.json");
        }
        
        /// <summary>
        /// Prüft ob Transaktionsdaten existieren
        /// </summary>
        public static bool Exists(string pdfFilePath)
        {
            return File.Exists(GetJsonPath(pdfFilePath));
        }
    }
}