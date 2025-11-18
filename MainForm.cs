using System;
using System.ComponentModel; // NEU
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PdfSharp.Pdf;           // NEU
using PdfSharp.Pdf.IO;        // NEU
using PdfSharp.Drawing;       // NEU

namespace PdfiumOverlayTest
{
    public partial class MainForm : Form
    {
        private IntPtr _pdfDocument = IntPtr.Zero;
        private string? _pdfFilePath;
        private int _currentPageIndex = 0;
        private int _pageCount = 0;
        private Bitmap? _renderedPageBitmap;
        private TransparentOverlayForm? _overlay;
        private List<TagOverlayForm> _tagOverlays = new List<TagOverlayForm>();
        private readonly Panel _pdfViewerPanel;
        private readonly PictureBox _pictureBox;
        private readonly Button _btnLoad;
        private readonly Button _btnBurn;
        private readonly Label _lblStatus;

        private const int RENDER_DPI = 150;

        private bool _autoOpenShownOnce = false; // NEU

        private readonly Dictionary<Keys, (string Text, Color Color)> _tagMap = new()
        {
            { Keys.M, ("M", Color.FromArgb(220, 120, 0)) },
            { Keys.P, ("P", Color.FromArgb(0, 160, 80)) },
            { Keys.R, ("R", Color.FromArgb(200, 40, 40)) },

            // Neue Tags hier ergänzen:
            { Keys.B, ("B", Color.FromArgb(0, 120, 220)) },
            { Keys.K, ("K", Color.FromArgb(80, 80, 80)) },
        };

        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public MainForm()
        {
            // PDFium nur zur Laufzeit initialisieren (Designer crasht sonst)
            if (!IsInDesigner())
                PDFium.FPDF_InitLibrary();

            Text = "Kürzel für PDF-Dateien";
            ClientSize = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            _pdfViewerPanel = new Panel
            {
                Location = new Point(10, 50),
                Size = new Size(880, 640),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGray,
                AutoScroll = true,
                TabStop = false
            };
            Controls.Add(_pdfViewerPanel);

            _pictureBox = new PictureBox
            {
                Location = new Point(0, 0),
                SizeMode = PictureBoxSizeMode.AutoSize,
                BackColor = Color.White,
                TabStop = false,
                Visible = false  // NEU: Initial unsichtbar
            };
            _pdfViewerPanel.Controls.Add(_pictureBox);

            _overlay = new TransparentOverlayForm
            {
                Visible = false  // Bleibt unsichtbar bis PDF geladen
            };
            // WICHTIG: Nicht Show() aufrufen beim Start!

            _btnLoad = new Button
            {
                Text = "PDF laden",
                Location = new Point(10, 10),
                Size = new Size(120, 30),
                TabStop = false
            };
            _btnLoad.Click += BtnLoad_Click;
            Apply3DStyle(_btnLoad);     // << neu
            Controls.Add(_btnLoad);

            _btnBurn = new Button
            {
                Text = "Einbrennen && Speichern",
                Location = new Point(140, 10),
                Size = new Size(160, 30),
                Enabled = false,
                TabStop = false
            };
            _btnBurn.Click += BtnBurn_Click;
            Apply3DStyle(_btnBurn);     // << neu
            Controls.Add(_btnBurn);

            _lblStatus = new Label
            {
                Location = new Point(310, 10),
                Size = new Size(580, 30),
                Text = "Keine PDF geladen. Shortcuts: ←↑→↓ bewegen, A/D Breite, W/S Höhe, M/P/R Tags",
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_lblStatus);

            LocationChanged += (s, e) => PositionOverlay();
            SizeChanged += (s, e) => PositionOverlay();

            _overlay.Hide(); // Explizit verstecken
        }

        private void PositionOverlay()
        {
            if (_overlay == null || !_overlay.Visible) return;

            var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);
            var pdfWidth = _pictureBox.Image?.Width ?? _pdfViewerPanel.ClientSize.Width;
            var overlayWidth = (int)(pdfWidth * 0.8);
            var overlayHeight = 60;
            var pdfHeight = _pictureBox.Image?.Height ?? _pdfViewerPanel.ClientSize.Height;
            var overlayX = panelScreenPos.X + (pdfWidth - overlayWidth) / 2;
            var overlayY = panelScreenPos.Y + (int)(pdfHeight * 0.1);

            _overlay.Size = new Size(overlayWidth, overlayHeight);
            _overlay.Location = new Point(overlayX, overlayY);
        }

        // Diese Methode ist bereits korrekt - Tags im Viewer bleiben klein (25x22)
        private void CreateTagAtCurrentPosition(string text, Color color)
        {
            if (_overlay == null || !_overlay.Visible) return;

            var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);
            var pdfWidth = _pictureBox.Image?.Width ?? 0;
            if (pdfWidth == 0) return;

            var tagX = panelScreenPos.X + pdfWidth - 80; // Abstand vom rechten Rand bleibt
            var tagY = _overlay.Top;

            var newTag = new TagOverlayForm();

            // 20% kleiner gegenüber 72x52 -> 58x42
            var w = 58;
            var h = 42;
            newTag.MinimumSize = new Size(w, h);
            newTag.MaximumSize = new Size(w, h);
            newTag.SetBounds(tagX, tagY, w, h);

            newTag.SetTag(text, color);
            newTag.Show(this);

            newTag.SetBounds(tagX, tagY, w, h);
            newTag.UpdateDisplay();

            _tagOverlays.Add(newTag);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_pdfDocument == IntPtr.Zero || _overlay == null || !_overlay.Visible)
                return base.ProcessCmdKey(ref msg, keyData);

            var hasShift = (keyData & Keys.Shift) == Keys.Shift;
            var baseKey = keyData & ~Keys.Shift;
            var step = hasShift ? 20 : 5;

            switch (baseKey)
            {
                case Keys.Left:
                    if (hasShift)
                    {
                        var newWidth = Math.Max(100, _overlay.Width - step * 2);
                        var widthDiff = _overlay.Width - newWidth;
                        _overlay.Left += widthDiff / 2;
                        _overlay.Width = newWidth;
                        _overlay.Invalidate();
                    }
                    else
                    {
                        _overlay.Left = Math.Max(_pdfViewerPanel.PointToScreen(Point.Empty).X, _overlay.Left - step);
                        _overlay.Invalidate();
                    }
                    return true;

                case Keys.Right:
                    if (hasShift)
                    {
                        var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);
                        var pdfWidth = _pictureBox.Image?.Width ?? _pdfViewerPanel.ClientSize.Width;
                        var maxWidth = pdfWidth - 20;

                        var newWidth = Math.Min(maxWidth, _overlay.Width + step * 2);
                        var widthDiff = newWidth - _overlay.Width;
                        var newLeft = _overlay.Left - widthDiff / 2;

                        newLeft = Math.Max(panelScreenPos.X + 10, newLeft);

                        _overlay.Left = newLeft;
                        _overlay.Width = newWidth;
                        _overlay.Invalidate();
                    }
                    else
                    {
                        var maxX = _pdfViewerPanel.PointToScreen(Point.Empty).X + _pictureBox.Width - _overlay.Width;
                        _overlay.Left = Math.Min(maxX, _overlay.Left + step);
                        _overlay.Invalidate();
                    }
                    return true;

                case Keys.Up:
                    _overlay.Top = Math.Max(_pdfViewerPanel.PointToScreen(Point.Empty).Y, _overlay.Top - step);
                    _overlay.Invalidate();
                    return true;

                case Keys.Down:
                    var maxY = _pdfViewerPanel.PointToScreen(Point.Empty).Y + _pictureBox.Height - _overlay.Height;
                    _overlay.Top = Math.Min(maxY, _overlay.Top + step);
                    _overlay.Invalidate();
                    return true;

                case Keys.PageDown:
                    if (_currentPageIndex < _pageCount - 1)
                    {
                        _currentPageIndex++;
                        RenderCurrentPage();
                    }
                    return true;

                case Keys.PageUp:
                    if (_currentPageIndex > 0)
                    {
                        _currentPageIndex--;
                        RenderCurrentPage();
                    }
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_pdfDocument == IntPtr.Zero || _overlay == null || !_overlay.Visible) return;

            // Generische Tag-Behandlung
            if (_tagMap.TryGetValue(e.KeyCode, out var tag))
            {
                _overlay.SetTag(tag.Text, tag.Color);
                CreateTagAtCurrentPosition(tag.Text, tag.Color);
                this.Focus();
                e.Handled = true;
                return;
            }

            var step = e.Shift ? 20 : 5;
            switch (e.KeyCode)
            {
                case Keys.A:
                    _overlay.Width = Math.Max(50, _overlay.Width - step);
                    _overlay.Invalidate();
                    e.Handled = true;
                    break;
                case Keys.D:
                    _overlay.Width += step;
                    _overlay.Invalidate();
                    e.Handled = true;
                    break;
                case Keys.W:
                    _overlay.Height = Math.Max(30, _overlay.Height - step);
                    _overlay.Invalidate();
                    e.Handled = true;
                    break;
                case Keys.S:
                    _overlay.Height += step;
                    _overlay.Invalidate();
                    e.Handled = true;
                    break;
                case Keys.Q:
                    _overlay.Left = Math.Max(_pdfViewerPanel.PointToScreen(Point.Empty).X, _overlay.Left - 1);
                    _overlay.Invalidate();
                    e.Handled = true;
                    break;
                case Keys.E:
                    _overlay.Left = Math.Min(_pdfViewerPanel.PointToScreen(Point.Empty).X + _pictureBox.Width - _overlay.Width, _overlay.Left + 1);
                    _overlay.Invalidate();
                    e.Handled = true;
                    break;
                // PageUp/PageDown usw. bleiben wie gehabt
            }
        }

        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "PDF-Dateien (*.pdf)|*.pdf",
                Title = "PDF-Datei öffnen"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                ClosePdfDocument();
                _pdfFilePath = ofd.FileName;

                if (!File.Exists(_pdfFilePath))
                {
                    MessageBox.Show($"Datei nicht gefunden:\n{_pdfFilePath}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] pdfBytes = File.ReadAllBytes(_pdfFilePath);
                GCHandle handle = GCHandle.Alloc(pdfBytes, GCHandleType.Pinned);

                try
                {
                    _pdfDocument = PDFium.FPDF_LoadMemDocument(handle.AddrOfPinnedObject(), pdfBytes.Length, null);
                }
                finally
                {
                    handle.Free();
                }

                if (_pdfDocument == IntPtr.Zero)
                {
                    var error = PDFium.FPDF_GetLastError();
                    string errorMsg = error switch
                    {
                        1 => "Unbekannter Fehler",
                        2 => "Datei nicht gefunden",
                        3 => "PDF beschädigt oder ungültiges Format",
                        4 => "Passwort erforderlich",
                        5 => "Falsches Passwort",
                        6 => "Ungültige Seitenzahl",
                        _ => $"Fehlercode: {error}"
                    };

                    MessageBox.Show($"Fehler beim Laden der PDF-Datei.\n\n{errorMsg}\n\nStelle sicher, dass:\n- Die Datei eine gültige PDF ist\n- Die Datei nicht verschlüsselt ist\n- pdfium.dll im Ausgabeordner liegt",
                        "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _pageCount = PDFium.FPDF_GetPageCount(_pdfDocument);
                _currentPageIndex = 0;

                if (!_overlay!.Visible)
                {
                    _overlay.Show(this);
                }
                _overlay.Visible = true;

                _btnBurn.Enabled = true;
                RenderCurrentPage();
                this.Focus();
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show($"pdfium.dll nicht gefunden!\n\nBitte platziere pdfium.dll in:\n{AppDomain.CurrentDomain.BaseDirectory}",
                    "DLL-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden:\n{ex.GetType().Name}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnShown(EventArgs e) // NEU
        {
            base.OnShown(e);
            if (IsInDesigner()) return;

            if (_autoOpenShownOnce) return;
            _autoOpenShownOnce = true;

            // nach dem Shown-Event asynchron öffnen, damit das Fenster bereits steht
            BeginInvoke(new Action(() =>
            {
                BtnLoad_Click(this, EventArgs.Empty);
                if (_pdfDocument == IntPtr.Zero) // Abbruch oder Fehler -> zurück ins Startfenster
                    Close();
            }));
        }

        private void RenderCurrentPage()
        {
            if (_pdfDocument == IntPtr.Zero) return;

            _renderedPageBitmap?.Dispose();

            var page = PDFium.FPDF_LoadPage(_pdfDocument, _currentPageIndex);
            if (page == IntPtr.Zero) return;

            try
            {
                var pageWidthPt = PDFium.FPDF_GetPageWidth(page);
                var pageHeightPt = PDFium.FPDF_GetPageHeight(page);

                var width = (int)(pageWidthPt * RENDER_DPI / 72.0);
                var height = (int)(pageHeightPt * RENDER_DPI / 72.0);

                _renderedPageBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                var bmpData = _renderedPageBitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);

                try
                {
                    var bitmap = PDFium.FPDFBitmap_CreateEx(
                        width, height, 4,
                        bmpData.Scan0, width * 4);

                    PDFium.FPDFBitmap_FillRect(bitmap, 0, 0, width, height, 0xFFFFFFFF);

                    PDFium.FPDF_RenderPageBitmap(
                        bitmap, page,
                        0, 0, width, height,
                        0, 0);

                    PDFium.FPDFBitmap_Destroy(bitmap);
                }
                finally
                {
                    _renderedPageBitmap.UnlockBits(bmpData);
                }

                _pictureBox.Image?.Dispose();
                _pictureBox.Image = new Bitmap(_renderedPageBitmap);
                _pictureBox.Size = _renderedPageBitmap.Size;
                _pictureBox.Visible = true; // NEU: Jetzt sichtbar machen

                var screenArea = Screen.PrimaryScreen!.WorkingArea;
                var formWidth = Math.Min(width + 40, screenArea.Width - 100);
                var formHeight = Math.Min(height + 110, screenArea.Height - 100);

                this.ClientSize = new Size(formWidth, formHeight);
                _pdfViewerPanel.Size = new Size(formWidth - 20, formHeight - 60);
                this.CenterToScreen();

                UpdateStatus();
                PositionOverlay();
            }
            finally
            {
                PDFium.FPDF_ClosePage(page);
            }
        }

        private void BtnBurn_Click(object? sender, EventArgs e)
        {
            if (_renderedPageBitmap == null || _pdfDocument == IntPtr.Zero || string.IsNullOrEmpty(_pdfFilePath) || _overlay == null) return;

            // Finde nächste verfügbare Nummer für _markiertX
            string baseFileName = Path.GetFileNameWithoutExtension(_pdfFilePath);
            string directory = Path.GetDirectoryName(_pdfFilePath) ?? "";
            int counter = 1;
            string suggestedFileName;

            do
            {
                suggestedFileName = $"{baseFileName}_markiert{counter}.pdf";
                counter++;
            } while (File.Exists(Path.Combine(directory, suggestedFileName)));

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF-Datei (*.pdf)|*.pdf",
                FileName = suggestedFileName,
                Title = "Markierte PDF speichern",
                InitialDirectory = directory
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Erstelle markiertes Bild
                using var resultBitmap = new Bitmap(_renderedPageBitmap);
                using (var g = Graphics.FromImage(resultBitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);

                    // Zeichne ALLE gesetzten Tags
                    foreach (var tag in _tagOverlays)
                    {
                        if (string.IsNullOrEmpty(tag.TagText)) continue;

                        // GEÄNDERT: Ca. 3x so groß wie im Viewer (75x66 statt 25x22)
                        var tagWidth = 300;
                        var tagHeight = 264;

                        var tagX = tag.Left - panelScreenPos.X;
                        var tagY = tag.Top - panelScreenPos.Y;

                        if (tagX < 0 || tagY < 0 || tagX > resultBitmap.Width || tagY > resultBitmap.Height)
                            continue;

                        using (var tagBrush = new SolidBrush(tag.TagColor))
                        {
                            g.FillRectangle(tagBrush, tagX, tagY, tagWidth, tagHeight);
                        }

                        using (var pen = new Pen(Color.Black, 3)) // Dickerer Rahmen
                        {
                            g.DrawRectangle(pen, tagX, tagY, tagWidth - 1, tagHeight - 1);
                        }

                        // GEÄNDERT: Größere Schrift für die PDF (24pt statt 8pt)
                        using (var font = new Font("Arial", 96, FontStyle.Bold)) // 24 * (300/75) ≈ 96
                        using (var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        })
                        {
                            g.DrawString(tag.TagText, font, Brushes.White,
                                new RectangleF(tagX, tagY, tagWidth, tagHeight), sf);
                        }
                    }
                }

                // Speichere als PDF mit PdfSharp
                SaveMarkedPageAsPdf(resultBitmap, sfd.FileName);

                MessageBox.Show($"Erfolgreich als PDF gespeichert:\n{sfd.FileName}",
                    "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}\n\n{ex.StackTrace}", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // NEU: Speichere markierte Seite als PDF
        private void SaveMarkedPageAsPdf(Bitmap markedImage, string outputPdfPath)
        {
            try
            {
                // Öffne Original-PDF
                PdfDocument inputDocument = PdfReader.Open(_pdfFilePath!, PdfDocumentOpenMode.Import);
                PdfDocument outputDocument = new PdfDocument();

                // Kopiere alle Seiten
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    if (i == _currentPageIndex)
                    {
                        // Erstelle neue Seite mit markiertem Bild
                        PdfPage page = outputDocument.AddPage();
                        
                        // Übernehme Größe von Original-Seite
                        PdfPage originalPage = inputDocument.Pages[i];
                        page.Width = XUnit.FromPoint(originalPage.Width.Point);
                        page.Height = XUnit.FromPoint(originalPage.Height.Point);

                        using (XGraphics gfx = XGraphics.FromPdfPage(page))
                        {
                            // Speichere Bild temporär
                            string tempImage = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
                            try
                            {
                                markedImage.Save(tempImage, ImageFormat.Png);
                                
                                // Zeichne Bild auf PDF-Seite
                                XImage image = XImage.FromFile(tempImage);
                                gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
                            }
                            finally
                            {
                                if (File.Exists(tempImage))
                                    File.Delete(tempImage);
                            }
                        }
                    }
                    else
                    {
                        // Kopiere unveränderte Seite
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }

                // Speichere PDF
                outputDocument.Save(outputPdfPath);
                outputDocument.Close();
                inputDocument.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim PDF-Export: {ex.Message}", ex);
            }
        }

        // NEU: Erstelle einseitige PDF aus Bitmap (Workaround da PDFium keine Bearbeitung unterstützt)
        private void CreateSinglePagePdf(Bitmap image, string pdfPath)
        {
            // Einfacher Workaround: Speichere als hochqualitativos PNG und
            // benenne es als PDF (für echte PDF-Erstellung würde man iTextSharp/PdfSharp brauchen)
            
            // Bessere Lösung: Nutze System.Drawing.Printing für PDF-Export
            string tempPng = pdfPath + ".temp.png";
            image.Save(tempPng, ImageFormat.Png);
            
            // Da PDFium keine PDF-Erstellung unterstützt, müsste hier eine andere Bibliothek verwendet werden
            // Für jetzt: Kopiere Original und zeige Warnung
            File.Copy(_pdfFilePath!, pdfPath, true);
            
            if (File.Exists(tempPng))
                File.Delete(tempPng);
            
            // TODO: Für echte PDF-Bearbeitung würde man benötigen:
            // - iTextSharp / iText7
            // - PdfSharp
            // - oder PDFBox (Java)
        }

        private void UpdateStatus()
        {
            if (_pdfDocument == IntPtr.Zero)
            {
                _lblStatus.Text = "Keine PDF geladen.";
                return;
            }

            _lblStatus.Text = $"Seite {_currentPageIndex + 1}/{_pageCount} | " +
                              $"DPI: {RENDER_DPI} | " +
                              $"Shortcuts: ←↑→↓ (bewegen), Shift+←→ (Breite), W/S (Höhe), M/P/R (Tags)";
        }

        private void ClosePdfDocument()
        {
            if (_pdfDocument != IntPtr.Zero)
            {
                PDFium.FPDF_CloseDocument(_pdfDocument);
                _pdfDocument = IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _overlay?.Close();
                _overlay?.Dispose();

                foreach (var tag in _tagOverlays)
                {
                    tag?.Close();
                    tag?.Dispose();
                }
                _tagOverlays.Clear();

                ClosePdfDocument();
                _renderedPageBitmap?.Dispose();
                _pictureBox.Image?.Dispose();
            }
            base.Dispose(disposing);
        }

        // Füge diese Methode in die Klasse MainForm ein (z.B. unter den anderen Methoden)
        private void Apply3DStyle(Button b)
        {
            bool pressed = false;

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;        // Rahmen zeichnen wir selbst
            b.UseVisualStyleBackColor = false;
            b.BackColor = Color.FromArgb(235, 235, 235);
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

    internal static class PDFium
    {
        private const string DllName = "pdfium.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_InitLibrary();

        [DllImport(DllName, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadDocument(string filePath, string? password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadMemDocument(IntPtr dataBuffer, int size, string? password);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint FPDF_GetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_CloseDocument(IntPtr document);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int FPDF_GetPageCount(IntPtr document);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDF_LoadPage(IntPtr document, int pageIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_ClosePage(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double FPDF_GetPageWidth(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double FPDF_GetPageHeight(IntPtr page);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FPDFBitmap_CreateEx(int width, int height, int format, IntPtr firstScan, int stride);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int startX, int startY, int sizeX, int sizeY, int rotate, int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FPDFBitmap_Destroy(IntPtr bitmap);
    }
}