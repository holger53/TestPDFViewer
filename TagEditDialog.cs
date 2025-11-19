using System;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest  // GEÄNDERT: Richtiger Namespace
{
    public class TagEditDialog : Form
    {
        private TextBox _txtCharacter;
        private TextBox _txtDescription;
        private Panel _colorPreview;
        private Button _btnSelectColor;
        private Button _btnOk;
        private Button _btnCancel;
        private Button _btnCategories;

        public string TagCharacter { get; set; } = string.Empty;
        public Color TagColor { get; set; } = Color.FromArgb(220, 120, 0);
        public string TagDescription { get; set; } = string.Empty;

        public TagEditDialog()
        {
            Text = "Tag bearbeiten";
            ClientSize = new Size(400, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Buchstabe
            var lblChar = new Label { Text = "Buchstabe:", Location = new Point(20, 20), AutoSize = true };
            _txtCharacter = new TextBox { Location = new Point(120, 17), Width = 50, MaxLength = 1 };
            Controls.Add(lblChar);
            Controls.Add(_txtCharacter);

            // Beschreibung
            var lblDesc = new Label { Text = "Beschreibung:", Location = new Point(20, 60), AutoSize = true };
            _txtDescription = new TextBox { Location = new Point(120, 57), Width = 250 };
            Controls.Add(lblDesc);
            Controls.Add(_txtDescription);

            // Farbe
            var lblColor = new Label { Text = "Farbe:", Location = new Point(20, 100), AutoSize = true };
            _colorPreview = new Panel { Location = new Point(120, 97), Size = new Size(50, 30), BorderStyle = BorderStyle.FixedSingle };
            _btnSelectColor = new Button { Text = "Auswählen", Location = new Point(180, 95), Width = 100 };
            _btnSelectColor.Click += (s, e) =>
            {
                using var dlg = new ColorDialog { Color = TagColor };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    TagColor = dlg.Color;
                    _colorPreview.BackColor = TagColor;
                }
            };
            Controls.Add(lblColor);
            Controls.Add(_colorPreview);
            Controls.Add(_btnSelectColor);

            // Buttons
            _btnOk = new Button { Text = "OK", Location = new Point(200, 160), Width = 80, DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Abbrechen", Location = new Point(290, 160), Width = 80, DialogResult = DialogResult.Cancel };
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            // Kategorien Button
            _btnCategories = new Button { Text = "Kategorien", Location = new Point(20, 160), Width = 80 };
            _btnCategories.Click += BtnCategories_Click;
            Controls.Add(_btnCategories);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            Load += (s, e) =>
            {
                _txtCharacter.Text = TagCharacter;
                _txtDescription.Text = TagDescription;
                _colorPreview.BackColor = TagColor;
            };

            _btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtCharacter.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen Buchstaben ein.", "Fehler", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }
                TagCharacter = _txtCharacter.Text.Trim().ToUpper();
                TagDescription = _txtDescription.Text.Trim();
            };
        }

        private void BtnCategories_Click(object? sender, EventArgs e)
        {
            using var dlg = new CategoriesForm();
            dlg.ShowDialog(this);
        }
    }
}