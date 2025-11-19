using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    partial class StartForm
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // FELDER
        private Panel _buttonsPanel;
        private Button _btnStartTags;
        private Button _btnCategories;
        private Button _btnCompanyData;  // NEU: Firmendaten-Button
        private Button _btnExit;

        private void InitializeComponent()
        {
            _buttonsPanel = new Panel();
            _btnStartTags = new Button();
            _btnCategories = new Button();
            _btnCompanyData = new Button();  // NEU
            _btnExit = new Button();
            _buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _buttonsPanel
            // 
            _buttonsPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _buttonsPanel.BackColor = Color.FromArgb(160, 215, 232, 255);
            _buttonsPanel.Controls.Add(_btnStartTags);
            _buttonsPanel.Controls.Add(_btnCategories);
            _buttonsPanel.Controls.Add(_btnCompanyData);  // NEU: Button zum Panel hinzufügen
            _buttonsPanel.Controls.Add(_btnExit);
            _buttonsPanel.Location = new Point(78, 664);
            _buttonsPanel.Name = "_buttonsPanel";
            _buttonsPanel.Size = new Size(1000, 140);  // GEÄNDERT: Höher für 2 Zeilen
            _buttonsPanel.TabIndex = 0;
            // 
            // _btnStartTags (ERSTE ZEILE)
            // 
            _btnStartTags.BackColor = Color.FromArgb(235, 235, 235);
            _btnStartTags.FlatStyle = FlatStyle.Flat;
            _btnStartTags.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnStartTags.Location = new Point(30, 16);
            _btnStartTags.Name = "_btnStartTags";
            _btnStartTags.Size = new Size(280, 48);
            _btnStartTags.TabIndex = 0;
            _btnStartTags.Text = "Tag setzen (PDF öffnen)";
            _btnStartTags.UseVisualStyleBackColor = false;
            _btnStartTags.Click += BtnStartTags_Click;
            // 
            // _btnCategories (ERSTE ZEILE)
            // 
            _btnCategories.BackColor = Color.FromArgb(235, 235, 235);
            _btnCategories.FlatStyle = FlatStyle.Flat;
            _btnCategories.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnCategories.Location = new Point(360, 16);
            _btnCategories.Name = "_btnCategories";
            _btnCategories.Size = new Size(280, 48);
            _btnCategories.TabIndex = 1;
            _btnCategories.Text = "Kategorien (Tags) verwalten";
            _btnCategories.UseVisualStyleBackColor = false;
            _btnCategories.Click += BtnCategories_Click;
            // 
            // _btnCompanyData (ZWEITE ZEILE - NEU)
            // 
            _btnCompanyData.BackColor = Color.FromArgb(235, 235, 235);
            _btnCompanyData.FlatStyle = FlatStyle.Flat;
            _btnCompanyData.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnCompanyData.Location = new Point(30, 76);  // KORRIGIERT: Zweite Zeile
            _btnCompanyData.Name = "_btnCompanyData";
            _btnCompanyData.Size = new Size(610, 48);  // KORRIGIERT: Breiter (über 2 Spalten)
            _btnCompanyData.TabIndex = 2;
            _btnCompanyData.Text = "Firmen-/Personendaten";
            _btnCompanyData.UseVisualStyleBackColor = false;
            _btnCompanyData.Click += BtnCompanyData_Click;
            // 
            // _btnExit (ERSTE ZEILE)
            // 
            _btnExit.BackColor = Color.FromArgb(235, 235, 235);
            _btnExit.FlatStyle = FlatStyle.Flat;
            _btnExit.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnExit.Location = new Point(690, 16);
            _btnExit.Name = "_btnExit";
            _btnExit.Size = new Size(280, 48);
            _btnExit.TabIndex = 3;
            _btnExit.Text = "Beenden";
            _btnExit.UseVisualStyleBackColor = false;
            _btnExit.Click += BtnExit_Click;
            // 
            // StartForm
            // 
            BackgroundImageLayout = ImageLayout.Zoom;
            ClientSize = new Size(1200, 800);
            Controls.Add(_buttonsPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            KeyPreview = true;
            MaximizeBox = false;
            Name = "StartForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Kürzel für PDF-Dateien";
            _buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}