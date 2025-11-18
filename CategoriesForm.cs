using System;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    // Platzhalter-Dialog zum späteren Ausbau (Kategorien/Tags verwalten)
    public class CategoriesForm : Form
    {
        private readonly Button _btnClose;

        public CategoriesForm()
        {
            Text = "Kategorien verwalten";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(600, 400);

            var lblInfo = new Label
            {
                Text = "Hier können später Kategorien (Tags) angelegt, bearbeitet und gelöscht werden.",
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 60,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblInfo);

            _btnClose = new Button
            {
                Text = "Schließen",
                Size = new Size(120, 36),
                Location = new Point((ClientSize.Width - 120) / 2, ClientSize.Height - 60),
                Anchor = AnchorStyles.Bottom
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);
        }
    }
}