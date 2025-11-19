using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    /// <summary>
    /// Speichert und lädt Fensterpositionen
    /// </summary>
    public class WindowPositionSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "window_positions.json");

        public int? CategoriesFormX { get; set; }
        public int? CategoriesFormY { get; set; }
        public int? CategoriesFormWidth { get; set; }
        public int? CategoriesFormHeight { get; set; }
        
        public int? MainFormX { get; set; }
        public int? MainFormY { get; set; }
        public int? MainFormWidth { get; set; }
        public int? MainFormHeight { get; set; }

        [JsonIgnore]
        public Point? CategoriesFormLocation
        {
            get => (CategoriesFormX.HasValue && CategoriesFormY.HasValue) 
                ? new Point(CategoriesFormX.Value, CategoriesFormY.Value) 
                : null;
            set
            {
                if (value.HasValue)
                {
                    CategoriesFormX = value.Value.X;
                    CategoriesFormY = value.Value.Y;
                }
                else
                {
                    CategoriesFormX = null;
                    CategoriesFormY = null;
                }
            }
        }

        [JsonIgnore]
        public Size? CategoriesFormSize
        {
            get => (CategoriesFormWidth.HasValue && CategoriesFormHeight.HasValue)
                ? new Size(CategoriesFormWidth.Value, CategoriesFormHeight.Value)
                : null;
            set
            {
                if (value.HasValue)
                {
                    CategoriesFormWidth = value.Value.Width;
                    CategoriesFormHeight = value.Value.Height;
                }
                else
                {
                    CategoriesFormWidth = null;
                    CategoriesFormHeight = null;
                }
            }
        }

        [JsonIgnore]
        public Point? MainFormLocation
        {
            get => (MainFormX.HasValue && MainFormY.HasValue)
                ? new Point(MainFormX.Value, MainFormY.Value)
                : null;
            set
            {
                if (value.HasValue)
                {
                    MainFormX = value.Value.X;
                    MainFormY = value.Value.Y;
                }
                else
                {
                    MainFormX = null;
                    MainFormY = null;
                }
            }
        }

        [JsonIgnore]
        public Size? MainFormSize
        {
            get => (MainFormWidth.HasValue && MainFormHeight.HasValue)
                ? new Size(MainFormWidth.Value, MainFormHeight.Value)
                : null;
            set
            {
                if (value.HasValue)
                {
                    MainFormWidth = value.Value.Width;
                    MainFormHeight = value.Value.Height;
                }
                else
                {
                    MainFormWidth = null;
                    MainFormHeight = null;
                }
            }
        }

        public static WindowPositionSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<WindowPositionSettings>(json);
                    return settings ?? new WindowPositionSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Fenster-Positionen: {ex.Message}");
            }

            return new WindowPositionSettings();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
                
                System.Diagnostics.Debug.WriteLine($"Fenster-Positionen gespeichert: {json}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Fenster-Positionen: {ex.Message}");
            }
        }

        public static bool IsLocationValid(Point location)
        {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(location))
                    return true;
            }
            return false;
        }
    }
}