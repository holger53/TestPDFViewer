using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;  // ← WICHTIG: Für .Cast<Form>() und .ToList()
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    public partial class StartForm : Form
    {
        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        private readonly Color _panelBackColor = Color.FromArgb(160, 215, 232, 255);
        private readonly Color _btnBackColor   = Color.FromArgb(235, 235, 235);

        public StartForm()
        {
            InitializeComponent();

            if (IsInDesigner())
                return;

            _btnStartTags.Click -= BtnStartTags_Click;
            _btnStartTags.Click += BtnStartTags_Click;
            _btnCategories.Click -= BtnCategories_Click;
            _btnCategories.Click += BtnCategories_Click;
            _btnExit.Click -= BtnExit_Click;
            _btnExit.Click += BtnExit_Click;

            Apply3DStyle(_btnStartTags);
            Apply3DStyle(_btnCategories);
            Apply3DStyle(_btnExit);
            Apply3DStyle(_btnCompanyData);

            this.SizeChanged += RecenterButtonsPanel;
            
            // NEU: FormClosing-Event hinzufügen
            this.FormClosing += StartForm_FormClosing;
            
            TryLoadBackgroundImage();
        }

        // KORRIGIERT: Event-Handler zum Schließen aller Fenster
        private void StartForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Wenn die Anwendung beendet wird, zwinge alle Fenster zu schließen
            if (!e.Cancel)
            {
                // Hole alle offenen Fenster als Liste
                var openForms = Application.OpenForms.Cast<Form>().ToList();
                
                foreach (Form openForm in openForms)
                {
                    if (openForm != this && !openForm.IsDisposed)
                    {
                        try
                        {
                            // WICHTIG: Verhindere, dass das Fenster sein FormClosing-Event verarbeitet
                            // Setze eine Eigenschaft, die das Fenster als "wird geschlossen" markiert
                            openForm.Tag = "ForceClosing";
                            
                            // Schließe das Fenster
                            openForm.Close();
                            
                            // Falls Close() nicht funktioniert hat, forciere Dispose
                            if (!openForm.IsDisposed)
                            {
                                openForm.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Ignoriere Fehler beim Schließen einzelner Fenster
                            System.Diagnostics.Debug.WriteLine($"Fehler beim Schließen: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void RecenterButtonsPanel(object? sender, EventArgs e)
        {
            if (_buttonsPanel == null) return;
            _buttonsPanel.Location = new Point(
                (ClientSize.Width - _buttonsPanel.Width) / 2,
                ClientSize.Height - _buttonsPanel.Height - 30
            );
        }

        private void BtnStartTags_Click(object? sender, EventArgs e)
        {
            Hide();
            
            var mainForm = new MainForm();
            var categoriesForm = new CategoriesForm();

            bool isClosingForms = false;

            var settings = WindowPositionSettings.Load();

            mainForm.StartPosition = FormStartPosition.Manual;
            categoriesForm.StartPosition = FormStartPosition.Manual;

            var screenArea = Screen.PrimaryScreen!.WorkingArea;

            if (!settings.CategoriesFormLocation.HasValue || 
                !WindowPositionSettings.IsLocationValid(settings.CategoriesFormLocation.Value))
            {
                var categoriesWidth = 450;
                var spacing = 20;
                var totalWidth = categoriesWidth + spacing + mainForm.Width;
                var startX = Math.Max(10, (screenArea.Width - totalWidth) / 2);
                
                var categoriesX = startX;
                var mainFormX = categoriesX + categoriesWidth + spacing;
                var centerY = Math.Max(10, (screenArea.Height - mainForm.Height) / 2);
                
                categoriesForm.Location = new Point(categoriesX, centerY);
                categoriesForm.ClientSize = new Size(categoriesWidth, 600);
                mainForm.Location = new Point(mainFormX, centerY);
                
                settings.CategoriesFormLocation = categoriesForm.Location;
                settings.CategoriesFormSize = categoriesForm.ClientSize;
                settings.MainFormLocation = mainForm.Location;
                settings.MainFormSize = mainForm.ClientSize;
                settings.Save();
            }
            else
            {
                categoriesForm.Location = settings.CategoriesFormLocation.Value;
                if (settings.CategoriesFormSize.HasValue)
                    categoriesForm.ClientSize = settings.CategoriesFormSize.Value;
                
                if (settings.MainFormLocation.HasValue && 
                    WindowPositionSettings.IsLocationValid(settings.MainFormLocation.Value))
                {
                    mainForm.Location = settings.MainFormLocation.Value;
                }
                else
                {
                    mainForm.Location = new Point(
                        (screenArea.Width - mainForm.Width) / 2,
                        (screenArea.Height - mainForm.Height) / 2);
                }
                
                if (settings.MainFormSize.HasValue)
                    mainForm.ClientSize = settings.MainFormSize.Value;
            }

            categoriesForm.TagDoubleClicked += (s, tagItem) =>
            {
                mainForm?.PlaceTagFromCategory(tagItem);
                mainForm?.Activate();
            };
            
            categoriesForm.TagClicked += (s, tagItem) =>
            {
                mainForm?.PlaceTagFromCategory(tagItem);
                mainForm?.Activate();
            };

            // WICHTIG: Keine Owner-Beziehung!
            mainForm.Owner = null;
            categoriesForm.Owner = null;

            categoriesForm.Show();
            mainForm.Show();

            mainForm.TopMost = true;
            categoriesForm.TopMost = true;

            Application.DoEvents();
            mainForm.Activate();
            mainForm.BringToFront();
            
            mainForm.FormClosed += (s, ev) =>
            {
                if (!isClosingForms)
                {
                    isClosingForms = true;
                    categoriesForm.Close();
                    Show();
                }
            };
            
            categoriesForm.FormClosed += (s, ev) =>
            {
                if (!isClosingForms && !mainForm.IsDisposed)
                {
                    isClosingForms = true;
                    mainForm.Close();
                }
            };
        }

        private void BtnCategories_Click(object? sender, EventArgs e)
        {
            using var dlg = new CategoriesForm();
            dlg.ShowDialog(this);
        }

        private void BtnExit_Click(object? sender, EventArgs e) => Application.Exit();

        private void TryLoadBackgroundImage()
        {
            if (IsInDesigner())
                return;

            if (BackgroundImage != null)
            {
                BackgroundImageLayout = ImageLayout.Zoom;
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "Assets", "unbenannt.png"),
                Path.Combine(baseDir, "unbenannt.png"),
                Path.Combine(baseDir, "Assets", "start_bg.png"),
                Path.Combine(baseDir, "Assets", "start_bg.jpg"),
                Path.Combine(baseDir, "start_bg.png"),
                Path.Combine(baseDir, "start_bg.jpg")
            };

            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    BackgroundImage?.Dispose();
                    BackgroundImage = Image.FromFile(p);
                    BackgroundImageLayout = ImageLayout.Zoom;
                    return;
                }
            }

            BackColor = Color.WhiteSmoke;
        }

        private void Apply3DStyle(Button b)
        {
            bool pressed = false;

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.BackColor = _btnBackColor;
            b.ForeColor = Color.Black;

            b.MouseDown += (s, e) => { pressed = true; b.Invalidate(); };
            b.MouseUp   += (s, e) => { pressed = false; b.Invalidate(); };
            b.Leave     += (s, e) => { pressed = false; b.Invalidate(); };

            b.Paint += (s, e) =>
            {
                var rect = b.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                ControlPaint.DrawBorder3D(
                    e.Graphics,
                    rect,
                    pressed ? Border3DStyle.Sunken : Border3DStyle.Raised);
            };
        }

        private void BtnCompanyData_Click(object? sender, EventArgs e)
        {
            var companyData = LoadCompanyData();
            
            using var dlg = new CompanyDataDialog
            {
                CompanyData = companyData
            };
            
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                SaveCompanyData(dlg.CompanyData);
                MessageBox.Show("Firmendaten wurden erfolgreich gespeichert.", "Erfolg", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private CompanyData LoadCompanyData()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "companydata.json");
            
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<CompanyData>(json);
                    if (data != null)
                        return data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Firmendaten:\n{ex.Message}", 
                    "Ladefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            return new CompanyData();
        }

        private void SaveCompanyData(CompanyData data)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "companydata.json");
            
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                var json = System.Text.Json.JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Firmendaten:\n{ex.Message}", 
                    "Speicherfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}