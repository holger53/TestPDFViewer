using System;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    /// <summary>
    /// Dialog zur Verwaltung der "Freier Text"-Historie
    /// </summary>
    public class CustomTextHistoryDialog : Form
    {
        private ListBox _lstHistory;
        private Button _btnEdit;
        private Button _btnDelete;
        private Button _btnClear;
        private Button _btnClose;
        private Label _lblInfo;

        private CustomTextHistory _history;

        public CustomTextHistoryDialog()
        {
            _history = CustomTextHistory.Load();

            Text = "Freier Text - Historie verwalten";
            ClientSize = new Size(500, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Info-Label
            _lblInfo = new Label
            {
                Text = "Gespeicherte Texte (max. 10):",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            Controls.Add(_lblInfo);

            // ListBox für Historie
            _lstHistory = new ListBox
            {
                Location = new Point(15, 40),
                Size = new Size(470, 280),
                Font = new Font("Segoe UI", 9F)
            };
            _lstHistory.DoubleClick += LstHistory_DoubleClick;
            Controls.Add(_lstHistory);

            // Button: Bearbeiten
            _btnEdit = new Button
            {
                Text = "Bearbeiten",
                Location = new Point(15, 330),
                Width = 100
            };
            _btnEdit.Click += BtnEdit_Click;
            Controls.Add(_btnEdit);

            // Button: Löschen
            _btnDelete = new Button
            {
                Text = "Löschen",
                Location = new Point(120, 330),
                Width = 100
            };
            _btnDelete.Click += BtnDelete_Click;
            Controls.Add(_btnDelete);

            // Button: Alle löschen
            _btnClear = new Button
            {
                Text = "Alle löschen",
                Location = new Point(225, 330),
                Width = 100
            };
            _btnClear.Click += BtnClear_Click;
            Controls.Add(_btnClear);

            // Button: Schließen
            _btnClose = new Button
            {
                Text = "Schließen",
                Location = new Point(385, 330),
                Width = 100
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            LoadHistory();
        }

        private void LoadHistory()
        {
            _lstHistory.Items.Clear();
            
            if (_history.RecentTexts.Count == 0)
            {
                _lstHistory.Items.Add("(Keine Einträge vorhanden)");
                _btnEdit.Enabled = false;
                _btnDelete.Enabled = false;
                _btnClear.Enabled = false;
                return;
            }

            foreach (var text in _history.RecentTexts)
            {
                _lstHistory.Items.Add(text);
            }

            _btnEdit.Enabled = true;
            _btnDelete.Enabled = true;
            _btnClear.Enabled = true;

            // Zeige Info über letzten verwendeten Text
            if (!string.IsNullOrEmpty(_history.LastUsedText))
            {
                _lblInfo.Text = $"Gespeicherte Texte (max. 10) - Zuletzt verwendet: \"{_history.LastUsedText}\"";
            }
        }

        private void LstHistory_DoubleClick(object? sender, EventArgs e)
        {
            if (_lstHistory.SelectedIndex >= 0 && 
                _lstHistory.SelectedIndex < _history.RecentTexts.Count)
            {
                BtnEdit_Click(sender, e);
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_lstHistory.SelectedIndex < 0 || 
                _lstHistory.SelectedIndex >= _history.RecentTexts.Count)
            {
                MessageBox.Show("Bitte wählen Sie einen Eintrag aus.", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string oldText = _history.RecentTexts[_lstHistory.SelectedIndex];

            using var dialog = new CustomTextDialog(true, oldText);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                string newText = dialog.CustomText;
                
                if (string.IsNullOrWhiteSpace(newText))
                    return;

                if (newText.Length > CustomTextDialog.MaxCharacters)
                    newText = newText.Substring(0, CustomTextDialog.MaxCharacters);

                newText = newText.Trim();

                // Entferne alten Text
                _history.RecentTexts.RemoveAt(_lstHistory.SelectedIndex);
                
                // Füge neuen Text hinzu
                _history.AddText(newText);
                
                LoadHistory();
                
                MessageBox.Show("Text wurde aktualisiert.", "Erfolg",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_lstHistory.SelectedIndex < 0 || 
                _lstHistory.SelectedIndex >= _history.RecentTexts.Count)
            {
                MessageBox.Show("Bitte wählen Sie einen Eintrag aus.", "Hinweis",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string textToDelete = _history.RecentTexts[_lstHistory.SelectedIndex];
            
            var result = MessageBox.Show(
                $"Möchten Sie diesen Eintrag löschen?\n\n\"{textToDelete}\"",
                "Löschen bestätigen",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _history.RecentTexts.RemoveAt(_lstHistory.SelectedIndex);
                _history.Save();
                LoadHistory();
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            if (_history.RecentTexts.Count == 0)
                return;

            var result = MessageBox.Show(
                $"Möchten Sie wirklich ALLE {_history.RecentTexts.Count} Einträge löschen?\n\n" +
                "Diese Aktion kann nicht rückgängig gemacht werden!",
                "Alle löschen?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _history.Clear();
                LoadHistory();
                
                MessageBox.Show("Alle Einträge wurden gelöscht.", "Erfolg",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}