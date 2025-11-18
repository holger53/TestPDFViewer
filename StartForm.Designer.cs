using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    partial class StartForm
    {
        // WICHTIG: Felder im Designer-Teil deklarieren
        private Panel _buttonsPanel;
        private Button _btnStartTags;
        private Button _btnCategories;
        private Button _btnExit;

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form-Eigenschaften (sichtbar im Designer)
            this.Text = "Kürzel für PDF-Dateien";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ClientSize = new Size(1200, 800);
            this.BackgroundImageLayout = ImageLayout.Zoom; // Designeransicht nicht strecken

            // Buttons-Panel
            _buttonsPanel = new Panel
            {
                Size = new Size(1000, 80),
                BackColor = Color.FromArgb(160, 215, 232, 255), // Pastell-Blau
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _buttonsPanel.Location = new Point(
                (this.ClientSize.Width - _buttonsPanel.Width) / 2,
                this.ClientSize.Height - _buttonsPanel.Height - 30
            );
            this.Controls.Add(_buttonsPanel);

            // gemeinsame Maße/Abstände
            var btnSize = new Size(280, 48);
            int spacing = 60;
            int top = 16;
            int left1 = 30;
            int left2 = left1 + btnSize.Width + spacing; // 370
            int left3 = left2 + btnSize.Width + spacing; // 710

            // Buttons
            _btnStartTags = new Button
            {
                Text = "Tag setzen (PDF öffnen)",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = btnSize,
                Location = new Point(left1, top),
                BackColor = Color.FromArgb(235, 235, 235),
                FlatStyle = FlatStyle.Flat
            };
            _btnStartTags.Click += BtnStartTags_Click;
            _buttonsPanel.Controls.Add(_btnStartTags);

            _btnCategories = new Button
            {
                Text = "Kategorien (Tags) verwalten",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = btnSize,
                Location = new Point(left2, top),
                BackColor = Color.FromArgb(235, 235, 235),
                FlatStyle = FlatStyle.Flat
            };
            _btnCategories.Click += BtnCategories_Click;
            _buttonsPanel.Controls.Add(_btnCategories);

            _btnExit = new Button
            {
                Text = "Beenden",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = btnSize,
                Location = new Point(left3, top),
                BackColor = Color.FromArgb(235, 235, 235),
                FlatStyle = FlatStyle.Flat
            };
            _btnExit.Click += BtnExit_Click;
            _buttonsPanel.Controls.Add(_btnExit);

            this.ResumeLayout(false);
        }
    }
}