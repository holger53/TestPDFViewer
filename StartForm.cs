using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    public partial class StartForm : Form
    {
        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        // Farben
        private readonly Color _panelBackColor = Color.FromArgb(160, 215, 232, 255); // Pastell-Blau
        private readonly Color _btnBackColor   = Color.FromArgb(235, 235, 235);      // leichtes Grau

        public StartForm()
        {
            InitializeComponent();

            if (IsInDesigner())
                return;

            // Event-Handler (werden in Designer bereits gesetzt, aber zur Sicherheit)
            _btnStartTags.Click -= BtnStartTags_Click;    // doppelte Handler vermeiden
            _btnStartTags.Click += BtnStartTags_Click;
            _btnCategories.Click -= BtnCategories_Click;
            _btnCategories.Click += BtnCategories_Click;
            _btnExit.Click -= BtnExit_Click;
            _btnExit.Click += BtnExit_Click;

            Apply3DStyle(_btnStartTags);
            Apply3DStyle(_btnCategories);
            Apply3DStyle(_btnExit);

            this.SizeChanged += RecenterButtonsPanel;
            TryLoadBackgroundImage();
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
            try
            {
                using var main = new MainForm();
                main.ShowDialog(this);
            }
            finally
            {
                Show();
            }
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
    }
}