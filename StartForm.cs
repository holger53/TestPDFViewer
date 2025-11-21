using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PdfiumOverlayTest.Localization;
using PdfiumOverlayTest.Properties;

namespace PdfiumOverlayTest
{
    public partial class StartForm : Form, ILocalizable
    {
        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        private readonly Color _panelBackColor = Color.FromArgb(160, 215, 232, 255);
        private readonly Color _btnBackColor   = Color.FromArgb(235, 235, 235);

        private CategoriesForm? _openCategoriesForm = null;
        
        // NEU: Flaggen-PictureBoxes
        private PictureBox _flagDE = null!;
        private PictureBox _flagEN = null!;
        private PictureBox _flagES = null!;
        private PictureBox _flagFR = null!;

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
            _btnCompanyData.Click -= BtnCompanyData_Click;
            _btnCompanyData.Click += BtnCompanyData_Click;

            Apply3DStyle(_btnStartTags);
            Apply3DStyle(_btnCategories);
            Apply3DStyle(_btnExit);
            Apply3DStyle(_btnCompanyData);

            this.SizeChanged += RecenterButtonsPanel;
            this.FormClosing += StartForm_FormClosing;
            
            // NEU: Flaggen erstellen
            CreateLanguageFlags();
            
            // NEU: UI aktualisieren
            UpdateUI();
            
            TryLoadBackgroundImage();
        }

        // NEU: Flaggen erstellen
        private void CreateLanguageFlags()
        {
            int flagSize = 32;
            int flagSpacing = 5;
            int topMargin = 10;
            int rightMargin = 10;
            
            // Französische Flagge (ganz links)
            _flagFR = CreateFlag(flagSize, 3, topMargin, rightMargin, flagSpacing, "fr-FR");
            _flagFR.Image = CreateFrenchFlag(flagSize, flagSize);
            _flagFR.Paint += (s, e) => DrawFlagBorder(e.Graphics, _flagFR);
            
            // Spanische Flagge
            _flagES = CreateFlag(flagSize, 2, topMargin, rightMargin, flagSpacing, "es-ES");
            _flagES.Image = CreateSpanishFlag(flagSize, flagSize);
            _flagES.Paint += (s, e) => DrawFlagBorder(e.Graphics, _flagES);
            
            // Englische Flagge
            _flagEN = CreateFlag(flagSize, 1, topMargin, rightMargin, flagSpacing, "en-US");
            _flagEN.Image = CreateUKFlag(flagSize, flagSize);
            _flagEN.Paint += (s, e) => DrawFlagBorder(e.Graphics, _flagEN);
            
            // Deutsche Flagge (ganz rechts)
            _flagDE = CreateFlag(flagSize, 0, topMargin, rightMargin, flagSpacing, "de-DE");
            _flagDE.Image = CreateGermanFlag(flagSize, flagSize);
            _flagDE.Paint += (s, e) => DrawFlagBorder(e.Graphics, _flagDE);
            
            Controls.Add(_flagFR);
            Controls.Add(_flagES);
            Controls.Add(_flagEN);
            Controls.Add(_flagDE);
            
            // Im Vordergrund
            _flagFR.BringToFront();
            _flagES.BringToFront();
            _flagEN.BringToFront();
            _flagDE.BringToFront();
            
            UpdateFlagSelection();
        }

        private PictureBox CreateFlag(int size, int position, int topMargin, int rightMargin, int spacing, string culture)
        {
            var flag = new PictureBox
            {
                Size = new Size(size, size),
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Tag = culture
            };
            
            flag.Location = new Point(
                ClientSize.Width - rightMargin - (size + spacing) * (position + 1) + spacing,
                topMargin
            );
            
            flag.Click += (s, e) => ChangeLanguage(culture);
            flag.MouseEnter += (s, e) => flag.BackColor = Color.FromArgb(50, Color.White);
            flag.MouseLeave += (s, e) => 
            {
                if (LocalizationHelper.GetCurrentLanguage() != culture)
                    flag.BackColor = Color.Transparent;
            };
            
            return flag;
        }

        // NEU: Rahmen um ausgewählte Flagge
        private void DrawFlagBorder(Graphics g, PictureBox flag)
        {
            if (flag.Tag?.ToString() == LocalizationHelper.GetCurrentLanguage())
            {
                using (var pen = new Pen(Color.Gold, 3))
                {
                    g.DrawRectangle(pen, 0, 0, flag.Width - 1, flag.Height - 1);
                }
            }
        }

        // NEU: Flaggen-Auswahl aktualisieren
        private void UpdateFlagSelection()
        {
            if (_flagDE == null || _flagEN == null || _flagES == null || _flagFR == null)
                return;
                
            var currentLang = LocalizationHelper.GetCurrentLanguage();
            
            _flagDE.BackColor = currentLang == "de-DE" ? Color.FromArgb(80, Color.Gold) : Color.Transparent;
            _flagEN.BackColor = currentLang == "en-US" ? Color.FromArgb(80, Color.Gold) : Color.Transparent;
            _flagES.BackColor = currentLang == "es-ES" ? Color.FromArgb(80, Color.Gold) : Color.Transparent;
            _flagFR.BackColor = currentLang == "fr-FR" ? Color.FromArgb(80, Color.Gold) : Color.Transparent;
            
            _flagDE.Invalidate();
            _flagEN.Invalidate();
            _flagES.Invalidate();
            _flagFR.Invalidate();
        }

        // NEU: Sprache wechseln
        private void ChangeLanguage(string culture)
        {
            LocalizationHelper.SetLanguage(culture);
            UpdateUI();
            UpdateFlagSelection();
            
            MessageBox.Show(
                Strings.Message_LanguageChanged,
                Strings.Message_Success,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // NEU: UI-Texte aktualisieren
        public void UpdateUI()
        {
            this.Text = Strings.StartForm_Title;
            _btnStartTags.Text = Strings.Button_SetTags;
            _btnCategories.Text = Strings.Button_ManageCategories;
            _btnCompanyData.Text = Strings.Button_CompanyData;
            _btnExit.Text = Strings.Button_Exit;
        }

        // NEU: Deutsche Flagge erstellen
        private Bitmap CreateGermanFlag(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                int stripeHeight = height / 3;
                g.FillRectangle(Brushes.Black, 0, 0, width, stripeHeight);
                g.FillRectangle(Brushes.Red, 0, stripeHeight, width, stripeHeight);
                using (var goldBrush = new SolidBrush(Color.FromArgb(255, 206, 0)))
                {
                    g.FillRectangle(goldBrush, 0, stripeHeight * 2, width, height - stripeHeight * 2);
                }
            }
            return bmp;
        }

        // NEU: UK Flagge erstellen
        private Bitmap CreateUKFlag(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                using (var blueBrush = new SolidBrush(Color.FromArgb(1, 33, 105)))
                {
                    g.FillRectangle(blueBrush, 0, 0, width, height);
                }
                
                using (var whitePen = new Pen(Color.White, Math.Max(2, width / 10)))
                {
                    g.DrawLine(whitePen, width / 2, 0, width / 2, height);
                    g.DrawLine(whitePen, 0, height / 2, width, height / 2);
                }
                
                using (var redPen = new Pen(Color.FromArgb(200, 16, 46), Math.Max(1, width / 16)))
                {
                    g.DrawLine(redPen, width / 2, 0, width / 2, height);
                    g.DrawLine(redPen, 0, height / 2, width, height / 2);
                }
            }
            return bmp;
        }

        // NEU: Spanische Flagge erstellen
        private Bitmap CreateSpanishFlag(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                int stripeHeight = height / 4;
                
                // Rot oben
                using (var redBrush = new SolidBrush(Color.FromArgb(198, 11, 30)))
                {
                    g.FillRectangle(redBrush, 0, 0, width, stripeHeight);
                }
                
                // Gelb Mitte (doppelt so breit)
                using (var yellowBrush = new SolidBrush(Color.FromArgb(255, 196, 0)))
                {
                    g.FillRectangle(yellowBrush, 0, stripeHeight, width, stripeHeight * 2);
                }
                
                // Rot unten
                using (var redBrush = new SolidBrush(Color.FromArgb(198, 11, 30)))
                {
                    g.FillRectangle(redBrush, 0, stripeHeight * 3, width, height - stripeHeight * 3);
                }
            }
            return bmp;
        }

        // NEU: Französische Flagge erstellen
        private Bitmap CreateFrenchFlag(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                int stripeWidth = width / 3;
                
                // Blau links
                using (var blueBrush = new SolidBrush(Color.FromArgb(0, 85, 164)))
                {
                    g.FillRectangle(blueBrush, 0, 0, stripeWidth, height);
                }
                
                // Weiß Mitte
                g.FillRectangle(Brushes.White, stripeWidth, 0, stripeWidth, height);
                
                // Rot rechts
                using (var redBrush = new SolidBrush(Color.FromArgb(239, 65, 53)))
                {
                    g.FillRectangle(redBrush, stripeWidth * 2, 0, width - stripeWidth * 2, height);
                }
            }
            return bmp;
        }

        private void StartForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_openCategoriesForm != null && !_openCategoriesForm.IsDisposed)
            {
                _openCategoriesForm.Close();
                _openCategoriesForm = null;
            }

            if (!e.Cancel)
            {
                var openForms = Application.OpenForms.Cast<Form>().Where(f => f != this).ToList();
                
                foreach (Form openForm in openForms)
                {
                    if (!openForm.IsDisposed)
                    {
                        try
                        {
                            openForm.Tag = "ForceClosing";
                            openForm.Close();
                            
                            if (!openForm.IsDisposed)
                            {
                                openForm.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fehler beim Schließen von {openForm.Name}: {ex.Message}");
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
            
            // NEU: Flaggen neu positionieren
            if (_flagDE != null && _flagEN != null && _flagES != null && _flagFR != null)
            {
                int flagSize = 32;
                int flagSpacing = 5;
                int topMargin = 10;
                int rightMargin = 10;
                
                _flagDE.Location = new Point(ClientSize.Width - rightMargin - flagSize, topMargin);
                _flagEN.Location = new Point(ClientSize.Width - rightMargin - (flagSize + flagSpacing) * 2 + flagSpacing, topMargin);
                _flagES.Location = new Point(ClientSize.Width - rightMargin - (flagSize + flagSpacing) * 3 + flagSpacing * 2, topMargin);
                _flagFR.Location = new Point(ClientSize.Width - rightMargin - (flagSize + flagSpacing) * 4 + flagSpacing * 3, topMargin);
            }
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
            // Wenn schon ein Kategorien-Fenster offen ist, aktiviere es
            if (_openCategoriesForm != null && !_openCategoriesForm.IsDisposed)
            {
                _openCategoriesForm.Activate();
                _openCategoriesForm.BringToFront();
                return;
            }
            
            // Erstelle neues Kategorien-Fenster
            _openCategoriesForm = new CategoriesForm();
            
            // Event-Handler für das Schließen
            _openCategoriesForm.FormClosed += (s, ev) =>
            {
                _openCategoriesForm = null;
                this.Activate();
            };
            
            // WICHTIG: Show() statt ShowDialog() - damit StartForm nicht blockiert wird!
            _openCategoriesForm.Show(this);
        }

        private void BtnExit_Click(object? sender, EventArgs e)
        {
            // Schließe das StartForm - dadurch wird StartForm_FormClosing ausgelöst,
            // welches dann alle anderen Fenster sauber schließt
            this.Close();
        }

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
            
            // CompanyDataDialog bleibt modal - das ist hier in Ordnung
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                SaveCompanyData(dlg.CompanyData);
                MessageBox.Show(Strings.Message_CompanyDataSaved, Strings.Message_Success, 
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


