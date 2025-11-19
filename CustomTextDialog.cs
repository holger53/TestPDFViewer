using System;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    public class CustomTextDialog : Form
    {
        private Label _lblPrompt;
        private TextBox _txtInput;
        private Label _lblCharCount;
        private ComboBox? _cmbHistory; // NEU: Nullable machen
        private CheckBox _chkRememberChoice; // NEU: "Auswahl merken"
        private Button _btnOk;
        private Button _btnCancel;

        public string CustomText => _txtInput.Text.Trim();
        public bool RememberChoice => _chkRememberChoice.Checked; // NEU
        public const int MaxCharacters = 50;

        private CustomTextHistory _history;

        public CustomTextDialog(bool isFirstTime = false, string? lastText = null)
        {
            _history = CustomTextHistory.Load();

            Text = "Freier Text eingeben";
            ClientSize = new Size(450, 220); // GEÄNDERT: Höher
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Prompt Label
            _lblPrompt = new Label
            {
                Text = isFirstTime 
                    ? "Bitte geben Sie Ihren Text ein (max. 50 Zeichen):" 
                    : "Möchten Sie den letzten Text wiederverwenden oder neuen Text eingeben?",
                Location = new Point(15, 15),
                Size = new Size(420, 30),
                AutoSize = false
            };
            Controls.Add(_lblPrompt);

            // NEU: Historie-Dropdown (nur wenn nicht erstes Mal)
            if (!isFirstTime && _history.RecentTexts.Count > 0)
            {
                _cmbHistory = new ComboBox
                {
                    Location = new Point(15, 50),
                    Width = 420,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                _cmbHistory.Items.Add("-- Neuen Text eingeben --");
                foreach (var text in _history.RecentTexts)
                {
                    _cmbHistory.Items.Add(text);
                }
                _cmbHistory.SelectedIndex = 0;
                _cmbHistory.SelectedIndexChanged += CmbHistory_SelectedIndexChanged;
                Controls.Add(_cmbHistory);
            }

            // TextBox mit Padding-Panel
            int textBoxY = isFirstTime || _history.RecentTexts.Count == 0 ? 50 : 85;
            var panel = new Panel
            {
                Location = new Point(15, textBoxY),
                Width = 420,
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Window
            };

            _txtInput = new TextBox
            {
                Location = new Point(3, 3),
                Width = 412,
                MaxLength = MaxCharacters,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F)
            };
            _txtInput.TextChanged += TxtInput_TextChanged;
            
            // Setze letzten Text wenn vorhanden
            if (!isFirstTime && !string.IsNullOrEmpty(lastText))
            {
                _txtInput.Text = lastText;
            }
            
            panel.Controls.Add(_txtInput);
            Controls.Add(panel);

            // Zeichenzähler
            _lblCharCount = new Label
            {
                Text = $"{_txtInput.Text.Length} / {MaxCharacters}",
                Location = new Point(15, textBoxY + 35),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(_lblCharCount);

            // NEU: "Auswahl merken" Checkbox
            _chkRememberChoice = new CheckBox
            {
                Text = "Diese Wahl merken (Text bei nächstem Mal automatisch verwenden)",
                Location = new Point(15, textBoxY + 60),
                Size = new Size(420, 20),
                Checked = _history.ReuseLastText
            };
            Controls.Add(_chkRememberChoice);

            // OK Button
            _btnOk = new Button
            {
                Text = "OK",
                Location = new Point(270, textBoxY + 90),
                Width = 80,
                DialogResult = DialogResult.OK
            };
            Controls.Add(_btnOk);

            // Cancel Button
            _btnCancel = new Button
            {
                Text = "Abbrechen",
                Location = new Point(355, textBoxY + 90),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        // NEU: History-Dropdown geändert
        private void CmbHistory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cmbHistory.SelectedIndex > 0) // Nicht "-- Neuen Text eingeben --"
            {
                _txtInput.Text = _cmbHistory.SelectedItem?.ToString() ?? "";
            }
            else
            {
                _txtInput.Clear();
            }
            _txtInput.Focus();
            _txtInput.SelectAll();
        }

        private void TxtInput_TextChanged(object? sender, EventArgs e)
        {
            int length = _txtInput.Text.Length;
            _lblCharCount.Text = $"{length} / {MaxCharacters}";
            
            if (length >= MaxCharacters)
            {
                _lblCharCount.ForeColor = Color.Red;
            }
            else if (length >= 40)
            {
                _lblCharCount.ForeColor = Color.Orange;
            }
            else
            {
                _lblCharCount.ForeColor = Color.Gray;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtInput.Focus();
            _txtInput.SelectAll();
        }
    }
}