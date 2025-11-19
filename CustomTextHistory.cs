using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PdfiumOverlayTest
{
    /// <summary>
    /// Verwaltet die Historie der zuletzt verwendeten freien Texte
    /// </summary>
    public class CustomTextHistory
    {
        private static readonly string HistoryFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "customtext_history.json");

        public List<string> RecentTexts { get; set; } = new List<string>();
        public string? LastUsedText { get; set; }
        public bool ReuseLastText { get; set; } = false;
        public DateTime? LastUsedDate { get; set; }

        /// <summary>
        /// Lädt die Historie aus der JSON-Datei
        /// </summary>
        public static CustomTextHistory Load()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    var json = File.ReadAllText(HistoryFilePath);
                    var history = JsonSerializer.Deserialize<CustomTextHistory>(json);
                    return history ?? new CustomTextHistory();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der CustomText-Historie: {ex.Message}");
            }

            return new CustomTextHistory();
        }

        /// <summary>
        /// Speichert die Historie in der JSON-Datei
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der CustomText-Historie: {ex.Message}");
            }
        }

        /// <summary>
        /// Fügt einen neuen Text zur Historie hinzu
        /// </summary>
        public void AddText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Entferne Text wenn bereits vorhanden
            RecentTexts.Remove(text);
            
            // Füge am Anfang hinzu
            RecentTexts.Insert(0, text);
            
            // Behalte nur die letzten 10 Texte
            if (RecentTexts.Count > 10)
            {
                RecentTexts.RemoveRange(10, RecentTexts.Count - 10);
            }

            LastUsedText = text;
            LastUsedDate = DateTime.Now;
            Save();
        }

        /// <summary>
        /// Setzt die Wiederverwendungs-Einstellung
        /// </summary>
        public void SetReusePreference(bool reuse, string? text = null)
        {
            ReuseLastText = reuse;
            if (!string.IsNullOrEmpty(text))
            {
                LastUsedText = text;
            }
            Save();
        }

        /// <summary>
        /// Löscht die komplette Historie
        /// </summary>
        public void Clear()
        {
            RecentTexts.Clear();
            LastUsedText = null;
            ReuseLastText = false;
            LastUsedDate = null;
            Save();
        }
    }
}