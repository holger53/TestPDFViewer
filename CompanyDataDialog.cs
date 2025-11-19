using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;  // GEÄNDERT: Von Newtonsoft.Json zu System.Text.Json

namespace PdfiumOverlayTest
{
    public class CompanyDataDialog : Form
    {
        private TextBox _txtCompanyName;
        private TextBox _txtAddress;
        private TextBox _txtPhone;
        private TextBox _txtEmail;
        private TextBox _txtAccountNumber;
        private TextBox _txtAccountType;
        private TextBox _txtYear;
        private TextBox _txtNotes;
        private Button _btnOk;
        private Button _btnCancel;

        public CompanyData CompanyData { get; set; } = new CompanyData();

        public CompanyDataDialog()
        {
            Text = "Firmen-/Personendaten bearbeiten";
            ClientSize = new Size(520, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Padding = new Padding(10);

            int labelWidth = 130;
            int textBoxLeft = labelWidth + 20;
            int textBoxWidth = 350;
            int rowHeight = 45;
            int startY = 20;

            // Firmenname/Person
            AddLabelAndTextBox("Firma/Name:", ref _txtCompanyName, startY, labelWidth, textBoxLeft, textBoxWidth);

            // Anschrift
            AddLabelAndTextBox("Anschrift:", ref _txtAddress, startY + rowHeight, labelWidth, textBoxLeft, textBoxWidth);

            // Telefon
            AddLabelAndTextBox("Telefonnummer:", ref _txtPhone, startY + rowHeight * 2, labelWidth, textBoxLeft, textBoxWidth);

            // Email
            AddLabelAndTextBox("E-Mail:", ref _txtEmail, startY + rowHeight * 3, labelWidth, textBoxLeft, textBoxWidth);

            // Kontonummer
            AddLabelAndTextBox("Konto:", ref _txtAccountNumber, startY + rowHeight * 4, labelWidth, textBoxLeft, textBoxWidth);

            // Kontoart
            AddLabelAndTextBox("Kontoart:", ref _txtAccountType, startY + rowHeight * 5, labelWidth, textBoxLeft, textBoxWidth);

            // Jahr
            AddLabelAndTextBox("Jahr:", ref _txtYear, startY + rowHeight * 6, labelWidth, textBoxLeft, textBoxWidth);
            _txtYear.MaxLength = 4;
            _txtYear.Text = DateTime.Now.Year.ToString();

            // Bemerkungen
            var lblNotes = new Label 
            { 
                Text = "Bemerkungen:", 
                Location = new Point(20, startY + rowHeight * 7 + 3),
                Width = labelWidth,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(lblNotes);

            _txtNotes = new TextBox 
            { 
                Location = new Point(textBoxLeft, startY + rowHeight * 7), 
                Width = textBoxWidth,
                Height = 70,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_txtNotes);

            // Buttons
            _btnOk = new Button 
            { 
                Text = "OK", 
                Location = new Point(310, 445),
                Width = 90,
                DialogResult = DialogResult.OK 
            };
            _btnCancel = new Button 
            { 
                Text = "Abbrechen", 
                Location = new Point(410, 445),
                Width = 90,
                DialogResult = DialogResult.Cancel 
            };
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Load += CompanyDataDialog_Load;
            _btnOk.Click += BtnOk_Click;
        }

        private void AddLabelAndTextBox(string labelText, ref TextBox textBox, int y, int labelWidth, int textBoxLeft, int textBoxWidth)
        {
            var label = new Label 
            { 
                Text = labelText, 
                Location = new Point(20, y + 3),
                Width = labelWidth,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(label);

            // Panel als Container für TextBox (ermöglicht Padding)
            var panel = new Panel
            {
                Location = new Point(textBoxLeft, y),
                Width = textBoxWidth,
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.SystemColors.Window
            };

            textBox = new TextBox 
            { 
                Location = new Point(3, 3),
                Width = textBoxWidth - 8,
                BorderStyle = BorderStyle.None,
                BackColor = System.Drawing.SystemColors.Window
            };
            
            panel.Controls.Add(textBox);
            Controls.Add(panel);
        }

        private void CompanyDataDialog_Load(object? sender, EventArgs e)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "companydata.json");
            
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    // GEÄNDERT: Verwende System.Text.Json statt Newtonsoft
                    var data = JsonSerializer.Deserialize<CompanyData>(json);
                    if (data != null)
                    {
                        CompanyData = data;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Firmendaten:\n{ex.Message}", 
                    "Ladefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _txtCompanyName.Text = CompanyData.CompanyName;
            _txtAddress.Text = CompanyData.Address;
            _txtPhone.Text = CompanyData.Phone;
            _txtEmail.Text = CompanyData.Email;
            _txtAccountNumber.Text = CompanyData.AccountNumber;
            _txtAccountType.Text = CompanyData.AccountType;
            _txtYear.Text = string.IsNullOrWhiteSpace(CompanyData.Year) ? DateTime.Now.Year.ToString() : CompanyData.Year;
            _txtNotes.Text = CompanyData.Notes;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            CompanyData.CompanyName = _txtCompanyName.Text.Trim();
            CompanyData.Address = _txtAddress.Text.Trim();
            CompanyData.Phone = _txtPhone.Text.Trim();
            CompanyData.Email = _txtEmail.Text.Trim();
            CompanyData.AccountNumber = _txtAccountNumber.Text.Trim();
            CompanyData.AccountType = _txtAccountType.Text.Trim();
            CompanyData.Year = _txtYear.Text.Trim();
            CompanyData.Notes = _txtNotes.Text.Trim();

            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "companydata.json");
                
                // GEÄNDERT: Verwende System.Text.Json statt Newtonsoft
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                var json = JsonSerializer.Serialize(CompanyData, options);
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