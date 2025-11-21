using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;  // ← HINZUFÜGEN für .ToList()
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using PdfiumOverlayTest.Localization;

namespace PdfiumOverlayTest
{
    public partial class MainForm : Form, ILocalizable
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

        private bool _autoOpenShownOnce = false;
        private bool _isUpdatingPosition = false;

        // NEU: Transaktionsdaten
        private TransactionData? _transactionData;
        private int _currentTransactionIndex = -1;

        private readonly Dictionary<Keys, (string Text, Color Color)> _tagMap = new()
        {
            { Keys.M, ("M", Color.FromArgb(220, 120, 0)) },
            { Keys.P, ("P", Color.FromArgb(0, 160, 80)) },
            { Keys.R, ("R", Color.FromArgb(200, 40, 40)) },
            { Keys.B, ("B", Color.FromArgb(0, 120, 220)) },
            { Keys.K, ("K", Color.FromArgb(80, 80, 80)) },
        };

        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public MainForm()
        {
            if (!IsInDesigner())
            {
                // Prüfe ob pdfium.dll verfügbar ist
                if (!CheckPdfiumAvailability())
                {
                    // Zeige Fehlermeldung und schließe das Formular
                    this.Load += (s, e) => this.Close();
                    return;
                }

                try
                {
                    PDFium.FPDF_InitLibrary();
                }
                catch (DllNotFoundException ex)
                {
                    ShowPdfiumError(ex);
                    this.Load += (s, e) => this.Close();
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Fehler beim Initialisieren der PDF-Bibliothek:\n\n{ex.Message}",
                        "Initialisierungsfehler",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    this.Load += (s, e) => this.Close();
                    return;
                }
            }

            Text = "Kürzel für PDF-Dateien";
            ClientSize = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(800, 600);

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
                Visible = false
            };
            _pdfViewerPanel.Controls.Add(_pictureBox);

            _overlay = new TransparentOverlayForm
            {
                Visible = false
            };

            _btnLoad = new Button
            {
                Text = "PDF laden",
                Location = new Point(10, 10),
                Size = new Size(120, 30),
                TabStop = false
            };
            _btnLoad.Click += BtnLoad_Click;
            Apply3DStyle(_btnLoad);
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
            Apply3DStyle(_btnBurn);
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

            _overlay.Hide();

            LoadWindowPosition();

            this.LocationChanged += MainForm_LocationChanged;
            this.SizeChanged += MainForm_SizeChanged;
            this.FormClosing += MainForm_FormClosing;
            this.KeyDown += MainForm_EmergencyExit; // Registriere den Notfall-Exit Handler

            CreateMainFormContextMenu();

            _pdfViewerPanel.SizeChanged += (s, e) => PositionOverlay();

            // WICHTIG: MainForm immer im Vordergrund halten
            this.TopMost = false; // Wird in StartForm gesetzt
        }

        /// <summary>
        /// Prüft ob pdfium.dll verfügbar ist
        /// </summary>
        private static bool CheckPdfiumAvailability()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] possiblePaths = new[]
            {
                Path.Combine(baseDir, "pdfium.dll"),
                Path.Combine(baseDir, "runtimes", "win-x64", "native", "pdfium.dll"),
                Path.Combine(baseDir, "runtimes", "win-x86", "native", "pdfium.dll")
            };

            bool found = possiblePaths.Any(File.Exists);

            if (!found)
            {
                ShowPdfiumError(null);
            }

            return found;
        }

        /// <summary>
        /// Zeigt eine hilfreiche Fehlermeldung an, wenn pdfium.dll fehlt
        /// </summary>
        private static void ShowPdfiumError(Exception? ex)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string architecture = Environment.Is64BitProcess ? "x64" : "x86";

            string message = $"Die erforderliche Bibliothek 'pdfium.dll' wurde nicht gefunden.\n\n" +
                           $"Diese DLL wird für die PDF-Verarbeitung benötigt.\n\n" +
                           $"Bitte führen Sie folgende Schritte aus:\n\n" +
                           $"1. Laden Sie pdfium.dll herunter:\n" +
                           $"   https://github.com/bblanchon/pdfium-binaries/releases/latest\n\n" +
                           $"2. Wählen Sie die Datei:\n" +
                           $"   pdfium-windows-{architecture}.tgz\n\n" +
                           $"3. Extrahieren Sie die Datei und kopieren Sie 'pdfium.dll' nach:\n" +
                           $"   {baseDir}\n\n" +
                           $"4. Starten Sie die Anwendung neu.\n\n" +
                           $"Aktuelles Verzeichnis: {baseDir}";

            if (ex != null)
            {
                message += $"\n\nTechnische Details:\n{ex.Message}";
            }

            MessageBox.Show(
                message,
                "PDFium-Bibliothek fehlt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            // Optional: Link in Zwischenablage kopieren
            try
            {
                Clipboard.SetText("https://github.com/bblanchon/pdfium-binaries/releases/latest");
            }
            catch
            {
                // Ignoriere Fehler beim Kopieren in die Zwischenablage
            }
        }

        private ContextMenuStrip? _mainFormContextMenu;

        private void CreateMainFormContextMenu()
        {
            _mainFormContextMenu = new ContextMenuStrip();

            var resetPositionMenuItem = new ToolStripMenuItem("Fensterposition zurücksetzen", null, MainForm_ResetPosition);
            resetPositionMenuItem.ShortcutKeys = Keys.Control | Keys.R;

            // NEU: Beenden-Menüpunkt hinzufügen
            var exitMenuItem = new ToolStripMenuItem("Anwendung beenden", null, MainForm_Exit);
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;

            _mainFormContextMenu.Items.Add(resetPositionMenuItem);
            _mainFormContextMenu.Items.Add(new ToolStripSeparator()); // Trennlinie
            _mainFormContextMenu.Items.Add(exitMenuItem); // NEU

            this.ContextMenuStrip = _mainFormContextMenu;
        }

        // NEU: Methode zum Beenden der Anwendung (nach MainForm_ResetPosition, Zeile 175):
        private void MainForm_Exit(object? sender, EventArgs e)
        {
            // Schließe das Formular statt Application.Exit()
            this.Close();
        }

        private void MainForm_ResetPosition(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie die gespeicherten Fensterpositionen zurücksetzen?\n\n" +
                "Die Fenster werden beim nächsten Start wieder in der Standard-Position angezeigt.",
                "Position zurücksetzen",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var settings = new WindowPositionSettings();
                settings.Save();

                MessageBox.Show("Fensterpositionen wurden zurückgesetzt.", "Erfolg",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadWindowPosition()
        {
            var settings = WindowPositionSettings.Load();

            if (settings.MainFormLocation.HasValue &&
                WindowPositionSettings.IsLocationValid(settings.MainFormLocation.Value))
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = settings.MainFormLocation.Value;
            }

            if (settings.MainFormSize.HasValue)
            {
                this.Size = settings.MainFormSize.Value;
            }
        }

        private void MainForm_LocationChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingPosition) return;

            _isUpdatingPosition = true;
            try
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    SaveWindowPosition();
                }
                PositionOverlay();
            }
            finally
            {
                _isUpdatingPosition = false;
            }
        }

        private void MainForm_SizeChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingPosition) return;

            _isUpdatingPosition = true;
            try
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    SaveWindowPosition();
                }
                PositionOverlay();
            }
            finally
            {
                _isUpdatingPosition = false;
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // NEU: Prüfe ob das Fenster zwangsweise geschlossen wird
            if (this.Tag?.ToString() == "ForceClosing")
            {
                SaveWindowPosition();
                return;
            }

            // Wenn der Benutzer das MainForm schließt, frage nach und schließe alle Fenster
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "Anwendung beenden?",
                    "Beenden",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true; // NICHT beenden
                    return;
                }
                
                // Bei "Ja" → Schließe alle Fenster durch das normale Schließen
                // Kein Application.Exit() - das verursacht InvalidOperationException
            }

            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
            if (this.WindowState != FormWindowState.Normal)
                return;

            var settings = WindowPositionSettings.Load();
            settings.MainFormLocation = this.Location;
            settings.MainFormSize = this.ClientSize;
            settings.Save();
        }

        private void PositionOverlay()
        {
            if (_overlay == null || !_overlay.Visible)
            {
                System.Diagnostics.Debug.WriteLine("PositionOverlay: Overlay nicht sichtbar oder null");
                return;
            }

            try
            {
                var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);
                var pdfWidth = _pictureBox.Image?.Width ?? _pdfViewerPanel.ClientSize.Width;
                var overlayWidth = (int)(pdfWidth * 0.8);
                var overlayHeight = 60;
                var pdfHeight = _pictureBox.Image?.Height ?? _pdfViewerPanel.ClientSize.Height;

                overlayWidth = Math.Min(overlayWidth, _pdfViewerPanel.ClientSize.Width - 40);

                var overlayX = panelScreenPos.X + (pdfWidth - overlayWidth) / 2;
                var overlayY = panelScreenPos.Y + (int)(pdfHeight * 0.1);

                bool needsUpdate = false;

                if (_overlay.Size != new Size(overlayWidth, overlayHeight))
                {
                    _overlay.Size = new Size(overlayWidth, overlayHeight);
                    needsUpdate = true;
                }

                if (_overlay.Location != new Point(overlayX, overlayY))
                {
                    _overlay.Location = new Point(overlayX, overlayY);
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    System.Diagnostics.Debug.WriteLine($"Overlay repositioniert: X={overlayX}, Y={overlayY}");
                    RepositionTagsToPanel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler in PositionOverlay: {ex.Message}");
            }
        }

        private void CreateTagAtCurrentPosition(string text, Color color)
        {
            if (_overlay == null || !_overlay.Visible) return;

            var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);
            var pdfWidth = _pictureBox.Image?.Width ?? 0;
            if (pdfWidth == 0) return;

            var tagX = panelScreenPos.X + pdfWidth - 80;
            var tagY = _overlay.Top;

            var newTag = new TagOverlayForm();

            int w, h;
            if (text.Length > 5)
            {
                w = Math.Min(200, text.Length * 12);
                h = 50;
            }
            else
            {
                w = 58;
                h = 42;
            }

            newTag.MinimumSize = new Size(w, h);
            newTag.MaximumSize = new Size(w, h);
            newTag.SetBounds(tagX, tagY, w, h);

            newTag.SetTag(text, color);
            newTag.Show(this);

            newTag.SetBounds(tagX, tagY, w, h);
            newTag.UpdateDisplay();

            _tagOverlays.Add(newTag);
        }

        private void RepositionTagsToPanel()
        {
            if (_tagOverlays.Count == 0 || _pictureBox.Image == null)
                return;

            var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);
            var pdfWidth = _pictureBox.Image.Width;

            foreach (var tag in _tagOverlays.ToList())
            {
                if (tag == null || tag.IsDisposed)
                    continue;

                try
                {
                    var tagX = panelScreenPos.X + pdfWidth - 80;
                    var relativeY = tag.Top - _overlay!.Top;
                    var tagY = _overlay.Top + relativeY;

                    tag.Location = new Point(tagX, tagY);
                }
                catch (ObjectDisposedException)
                {
                    continue;
                }
            }
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
                case Keys.V:
                    if (_transactionData != null && _transactionData.Transactions.Count > 0)
                    {
                        _currentTransactionIndex++;
                        if (_currentTransactionIndex >= _transactionData.Transactions.Count)
                            _currentTransactionIndex = 0;

                        JumpToTransaction(_currentTransactionIndex);
                        return true;
                    }
                    break;

                case Keys.Z:
                    if (_transactionData != null && _transactionData.Transactions.Count > 0)
                    {
                        _currentTransactionIndex--;
                        if (_currentTransactionIndex < 0)
                            _currentTransactionIndex = _transactionData.Transactions.Count - 1;

                        JumpToTransaction(_currentTransactionIndex);
                        return true;
                    }
                    break;

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

                    MessageBox.Show($"Fehler beim Laden der PDF-Datei.\n\n{errorMsg}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _pageCount = PDFium.FPDF_GetPageCount(_pdfDocument);
                _currentPageIndex = 0;

                LoadOrAnalyzeTransactions();

                if (!_overlay!.Visible)
                {
                    _overlay.Show(this);
                }
                _overlay.Visible = true;

                _btnBurn.Enabled = true;
                RenderCurrentPage();

                if (_transactionData != null && _transactionData.Transactions.Count > 0)
                {
                    _currentTransactionIndex = 0;
                    JumpToTransaction(_currentTransactionIndex);
                }

                this.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden:\n{ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrAnalyzeTransactions()
        {
            if (string.IsNullOrEmpty(_pdfFilePath))
                return;

            _transactionData = TransactionData.Load(_pdfFilePath);

            if (_transactionData == null)
            {
                var result = MessageBox.Show(
                    "Diese PDF wurde noch nicht analysiert.\n\n" +
                    "Möchten Sie jetzt die Kontobewegungen automatisch erkennen lassen?\n\n" +
                    "Dies kann einige Sekunden dauern.",
                    "PDF analysieren",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    AnalyzeTransactions();
                }
            }
        }

        private void AnalyzeTransactions()
        {
            if (_pdfDocument == IntPtr.Zero || string.IsNullOrEmpty(_pdfFilePath))
                return;

            try
            {
                var progressForm = new Form
                {
                    Text = "Analysiere PDF...",
                    Size = new Size(400, 100),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var label = new Label
                {
                    Text = "Bitte warten, PDF wird analysiert...",
                    AutoSize = true,
                    Location = new Point(20, 30)
                };
                progressForm.Controls.Add(label);

                progressForm.Show(this);
                Application.DoEvents();

                _transactionData = TransactionParser.ParsePdf(_pdfFilePath, _pdfDocument, _pageCount);
                _transactionData.Save(_pdfFilePath);

                progressForm.Close();

                MessageBox.Show(
                    $"Analyse abgeschlossen!\n\n" +
                    $"{_transactionData.Transactions.Count} Kontobewegungen wurden erkannt.\n\n" +
                    $"Verwenden Sie:\n" +
                    $"• V = Vorwärts zur nächsten Position\n" +
                    $"• Z = Zurück zur vorherigen Position",
                    "Analyse erfolgreich",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Analyse:\n{ex.Message}", "Analysefehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void JumpToTransaction(int transactionIndex)
        {
            if (_transactionData == null ||
                transactionIndex < 0 ||
                transactionIndex >= _transactionData.Transactions.Count ||
                _overlay == null)
                return;

            var transaction = _transactionData.Transactions[transactionIndex];

            if (transaction.PageIndex != _currentPageIndex)
            {
                _currentPageIndex = transaction.PageIndex;
                RenderCurrentPage();
            }

            var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);

            _overlay.Left = panelScreenPos.X + transaction.X;
            _overlay.Top = panelScreenPos.Y + transaction.Y;

            _overlay.Invalidate();

            UpdateStatus();
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
                _pictureBox.Visible = true;

                var screenArea = Screen.PrimaryScreen!.WorkingArea;
                var formWidth = Math.Min(width + 40, screenArea.Width - 100);
                var formHeight = Math.Min(height + 110, screenArea.Height - 100);

                this.ClientSize = new Size(formWidth, formHeight);
                _pdfViewerPanel.Size = new Size(formWidth - 20, formHeight - 60);

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
                using var resultBitmap = new Bitmap(_renderedPageBitmap);
                using (var g = Graphics.FromImage(resultBitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    var panelScreenPos = _pdfViewerPanel.PointToScreen(Point.Empty);

                    // Zeichne das Haupt-Overlay (gelbes Rechteck)
                    if (_overlay.Visible)
                    {
                        var overlayX = _overlay.Left - panelScreenPos.X;
                        var overlayY = _overlay.Top - panelScreenPos.Y;
                        var overlayWidth = _overlay.Width;
                        var overlayHeight = _overlay.Height;

                        // Prüfe ob das Overlay im sichtbaren Bereich liegt
                        if (overlayX >= -overlayWidth && overlayY >= -overlayHeight && 
                            overlayX < resultBitmap.Width && overlayY < resultBitmap.Height)
                        {
                            // Clip-Bereich berechnen
                            int drawX = Math.Max(0, overlayX);
                            int drawY = Math.Max(0, overlayY);
                            int drawWidth = Math.Min(overlayWidth, resultBitmap.Width - drawX);
                            int drawHeight = Math.Min(overlayHeight, resultBitmap.Height - drawY);

                            if (drawWidth > 0 && drawHeight > 0)
                            {
                                // Zeichne das halbtransparente gelbe Rechteck
                                using (var overlayBrush = new SolidBrush(Color.FromArgb(120, _overlay.FillColor)))
                                {
                                    g.FillRectangle(overlayBrush, overlayX, overlayY, overlayWidth, overlayHeight);
                                }

                                // Zeichne den schwarzen Rahmen
                                using (var pen = new Pen(Color.FromArgb(200, Color.Black), 3))
                                {
                                    g.DrawRectangle(pen, overlayX, overlayY, overlayWidth - 1, overlayHeight - 1);
                                }

                                // Zeichne den Text im Overlay (falls vorhanden)
                                // KORRIGIERT: Verwende CurrentText statt TagText
                                if (!string.IsNullOrEmpty(_overlay.CurrentText))
                                {
                                    float fontSize = _overlay.CurrentText.Length > 5 ? 14F : 18F;
                                    
                                    using (var font = new Font("Arial", fontSize, FontStyle.Bold))
                                    using (var textBrush = new SolidBrush(Color.FromArgb(220, Color.White)))
                                    using (var sf = new StringFormat
                                    {
                                        Alignment = StringAlignment.Center,
                                        LineAlignment = StringAlignment.Center
                                    })
                                    {
                                        g.DrawString(_overlay.CurrentText, font, textBrush,
                                            new RectangleF(overlayX, overlayY, overlayWidth, overlayHeight), sf);
                                    }
                                }
                            }
                        }
                    }

                    // GEÄNDERT: ToList() verwenden um eine Kopie zu erstellen
                    foreach (var tag in _tagOverlays.ToList() )
                    {
                        if (string.IsNullOrEmpty(tag.TagText)) continue;

                        int tagWidth, tagHeight;
                        float fontSize;

                        if (tag.TagText.Length > 5)
                        {
                            tagWidth = Math.Min(800, tag.TagText.Length * 40);
                            tagHeight = 200;
                            fontSize = 40;
                        }
                        else if (tag.TagText.Length > 1)
                        {
                            tagWidth = 300;
                            tagHeight = 264;
                            fontSize = 96;
                        }
                        else
                        {
                            tagWidth = 300;
                            tagHeight = 264;
                            fontSize = 96;
                        }

                        var tagX = tag.Left - panelScreenPos.X;
                        var tagY = tag.Top - panelScreenPos.Y;

                        if (tagX < -tagWidth || tagY < -tagHeight || 
                            tagX > resultBitmap.Width || tagY > resultBitmap.Height)
                            continue;

                        using (var tagBrush = new SolidBrush(tag.TagColor))
                        {
                            g.FillRectangle(tagBrush, tagX, tagY, tagWidth, tagHeight);
                        }

                        using (var pen = new Pen(Color.Black, 3))
                        {
                            g.DrawRectangle(pen, tagX, tagY, tagWidth - 1, tagHeight - 1);
                        }

                        using (var font = new Font("Arial", fontSize, FontStyle.Bold))
                        using (var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter
                        })
                        {
                            g.DrawString(tag.TagText, font, Brushes.White,
                                new RectangleF(tagX, tagY, tagWidth, tagHeight), sf);
                        }
                    }
                }

                SaveMarkedPageAsPdf(resultBitmap, sfd.FileName);

                MessageBox.Show($"Erfolgreich als PDF gespeichert:\n{sfd.FileName}",
                    "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveMarkedPageAsPdf(Bitmap markedImage, string outputPdfPath)
        {
            try
            {
                PdfDocument inputDocument = PdfReader.Open(_pdfFilePath!, PdfDocumentOpenMode.Import);
                PdfDocument outputDocument = new PdfDocument();

                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    if (i == _currentPageIndex)
                    {
                        PdfPage page = outputDocument.AddPage();

                        PdfPage originalPage = inputDocument.Pages[i];
                        page.Width = XUnit.FromPoint(originalPage.Width.Point);
                        page.Height = XUnit.FromPoint(originalPage.Height.Point);

                        using (XGraphics gfx = XGraphics.FromPdfPage(page))
                        {
                            string tempImage = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
                            try
                            {
                                markedImage.Save(tempImage, ImageFormat.Png);

                                XImage image = XImage.FromFile(tempImage);
                                gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
                                image.Dispose();
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
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }

                outputDocument.Save(outputPdfPath);
                outputDocument.Close();
                inputDocument.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim PDF-Export: {ex.Message}", ex);
            }
        }

        private void UpdateStatus()
        {
            if (_pdfDocument == IntPtr.Zero)
            {
                _lblStatus.Text = "Keine PDF geladen.";
                return;
            }

            string transactionInfo = "";
            if (_transactionData != null && _transactionData.Transactions.Count > 0)
            {
                transactionInfo = $" | Position {_currentTransactionIndex + 1}/{_transactionData.Transactions.Count} | V=Vor Z=Zurück";
            }

            _lblStatus.Text = $"Seite {_currentPageIndex + 1}/{_pageCount}{transactionInfo} | " +
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

        private void Apply3DStyle(Button b)
        {
            bool pressed = false;

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.BackColor = Color.FromArgb(235, 235, 235);
            b.ForeColor = Color.Black;

            b.MouseDown += (s, e) => { pressed = true; b.Invalidate(); };
            b.MouseUp += (s, e) => { pressed = false; b.Invalidate(); };
            b.Leave += (s, e) => { pressed = false; b.Invalidate(); };

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

        private CustomTextHistory _customTextHistory = CustomTextHistory.Load();
        private bool _hasUsedCustomTextBefore = false;

        public void PlaceTagFromCategory(CategoriesForm.TagItem tagItem)
        {
            if (_overlay == null || !_overlay.Visible)
            {
                MessageBox.Show("Bitte laden Sie zuerst ein PDF.", "Kein PDF geladen",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string character = tagItem.Character;
            Color color = tagItem.Color;

            if (tagItem.IsCustomText)
            {
                string? customText = null;
                bool isFirstTime = !_hasUsedCustomTextBefore;

                if (!isFirstTime && _customTextHistory.ReuseLastText && !string.IsNullOrEmpty(_customTextHistory.LastUsedText))
                {
                    var result = MessageBox.Show(
                        $"Möchten Sie den zuletzt verwendeten Text wiederverwenden?\n\n" +
                        $"Letzter Text: \"{_customTextHistory.LastUsedText}\"\n\n" +
                        $"Ja = Text wiederverwenden\n" +
                        $"Nein = Neuen Text eingeben\n" +
                        $"Abbrechen = Vorgang abbrechen",
                        "Text wiederverwenden?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel)
                    {
                        this.Focus();
                        return;
                    }
                    else if (result == DialogResult.Yes)
                    {
                        customText = _customTextHistory.LastUsedText;
                    }
                }

                if (customText == null)
                {
                    using var dialog = new CustomTextDialog(isFirstTime, _customTextHistory.LastUsedText);

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        customText = dialog.CustomText;

                        if (string.IsNullOrWhiteSpace(customText))
                        {
                            MessageBox.Show("Bitte geben Sie einen Text ein.", "Kein Text",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            this.Focus();
                            return;
                        }

                        if (customText.Length > CustomTextDialog.MaxCharacters)
                        {
                            customText = customText.Substring(0, CustomTextDialog.MaxCharacters);
                        }

                        customText = customText.Trim();

                        if (string.IsNullOrEmpty(customText))
                        {
                            MessageBox.Show("Der Text darf nicht nur aus Leerzeichen bestehen.",
                                "Ungültiger Text", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            this.Focus();
                            return;
                        }

                        _customTextHistory.AddText(customText);
                        _customTextHistory.SetReusePreference(dialog.RememberChoice, customText);
                        _hasUsedCustomTextBefore = true;
                    }
                    else
                    {
                        this.Focus();
                        return;
                    }
                }

                character = customText!;
            }

            _overlay.SetTag(character, color);
            CreateTagAtCurrentPosition(character, color);
            this.Focus();
        }

        // NOTFALL-BEENDEN: Strg + Shift + Q
        private void MainForm_EmergencyExit(object? sender, KeyEventArgs e)
        {
            // Notfall-Beenden mit Strg + Shift + Q
            if (e.Control && e.Shift && e.KeyCode == Keys.Q)
            {
                this.Close();
                e.Handled = true;
            }
            
            // Oder: Fenster in den Vordergrund bringen mit Strg + Shift + F
            if (e.Control && e.Shift && e.KeyCode == Keys.F)
            {
                this.BringToFront();
                this.Activate();
                this.Focus();
                e.Handled = true;
            }
        }

        public void UpdateUI()
        {
            // MainForm: Keine lokalisierbaren UI-Elemente momentan
            // Buttons und Labels verwenden hartcodierte deutsche Texte
        }
    }
}

