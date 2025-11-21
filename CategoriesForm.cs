using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;  // ← HINZUFÜGEN für .Any() und .ToList()
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DrawingColor = System.Drawing.Color;
using WordColor = DocumentFormat.OpenXml.Wordprocessing.Color;
using PdfiumOverlayTest.Localization;


namespace PdfiumOverlayTest
{
    public partial class CategoriesForm : Form
    {
        // JSON-Serialisierbare Version von TagItem
        public class TagItem
        {
            public string Character { get; set; } = string.Empty;
            
            [JsonConverter(typeof(ColorJsonConverter))]
            public DrawingColor Color { get; set; }
            
            public string Description { get; set; } = string.Empty;
            
            // NEU: Flag für "Freier Text"-Kürzel
            public bool IsCustomText { get; set; } = false;

            public override string ToString() => $"{Character} - {Description}";
        }

        // Custom JSON Converter für System.Drawing.Color
        private class ColorJsonConverter : JsonConverter<DrawingColor>  // GEÄNDERT
        {
            public override DrawingColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var argb = reader.GetInt32();
                return DrawingColor.FromArgb(argb);  // GEÄNDERT
            }

            public override void Write(Utf8JsonWriter writer, DrawingColor value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.ToArgb());
            }
        }

        private List<TagItem> _tags = new List<TagItem>();
        private ContextMenuStrip? _contextMenu;
        private Label? _lblClickInfo; // NEU HINZUFÜGEN

        // Pfad zur Tags-Datei
        private static string TagsFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "tags.json");
        
        // Event für Doppelklick - wird von MainForm/StartForm abonniert
        public event EventHandler<TagItem>? TagDoubleClicked;
        public event EventHandler<TagItem>? TagClicked; // NEU: Single-Click Event

        private static bool IsInDesigner() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        public CategoriesForm()
        {
            InitializeComponent();
            
            if (IsInDesigner())
                return;
            
            // NEU: Aktiviere freie Positionierung
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.Manual;
            
            // NEU: Optimale Größe
            this.ClientSize = new Size(450, 600);
            this.MinimumSize = new Size(400, 500);
            this.MaximumSize = new Size(500, 800);

            Apply3DStyle(_btnAdd);
            Apply3DStyle(_btnEdit);
            Apply3DStyle(_btnDelete);
            Apply3DStyle(_btnClose);

            CreateContextMenu();
            LoadTags();
            RefreshList();
            
            // NEU: Lade gespeicherte Position
            LoadWindowPosition();
            
            // NEU: Speichere Position beim Verschieben/Schließen
            this.LocationChanged += CategoriesForm_LocationChanged;
            this.FormClosing += CategoriesForm_FormClosing;
            
            // NEU: Click-Event für ListBox
            _lstTags.Click += LstTags_Click;
            _lstTags.DoubleClick += LstTags_DoubleClick;

            // NEU: Info-Label erstellen
            _lblClickInfo = new Label
            {
                Text = "💡 Klick = Tag setzen | Doppelklick = Tag setzen",
                Location = new Point(10, this.ClientSize.Height - 30),
                Size = new Size(this.ClientSize.Width - 20, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = DrawingColor.Gray, // ← Explizit qualifiziert
                Font = new System.Drawing.Font("Segoe UI", 8F, FontStyle.Italic), // ← Explizit qualifiziert
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_lblClickInfo);
        }

        // NEU: Position laden
        private void LoadWindowPosition()
        {
            var settings = WindowPositionSettings.Load();
            
            if (settings.CategoriesFormLocation.HasValue && 
                WindowPositionSettings.IsLocationValid(settings.CategoriesFormLocation.Value))
            {
                this.Location = settings.CategoriesFormLocation.Value;
            }
            
            if (settings.CategoriesFormSize.HasValue)
            {
                this.Size = settings.CategoriesFormSize.Value;
            }
        }

        // NEU: Position speichern bei Änderung
        private void CategoriesForm_LocationChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                SaveWindowPosition();
            }
        }

        // NEU: Position speichern beim Schließen
        private void CategoriesForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveWindowPosition();
        }

        // NEU: Position speichern
        private void SaveWindowPosition()
        {
            if (this.WindowState != FormWindowState.Normal)
                return;

            var settings = WindowPositionSettings.Load();
            settings.CategoriesFormLocation = this.Location;
            settings.CategoriesFormSize = this.Size;
            settings.Save();
        }

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            
            // Menüpunkt "Bearbeiten"
            var editMenuItem = new ToolStripMenuItem("Bearbeiten", null, ContextMenu_Edit);
            editMenuItem.ShortcutKeys = Keys.F2;
            
            // Menüpunkt "Löschen"
            var deleteMenuItem = new ToolStripMenuItem("Löschen", null, ContextMenu_Delete);
            deleteMenuItem.ShortcutKeys = Keys.Delete;
            
            // Separator 1
            var separator1 = new ToolStripSeparator();
            
            // Menüpunkt "Neuer Tag"
            var addMenuItem = new ToolStripMenuItem("Neuer Tag hinzufügen", null, ContextMenu_Add);
            addMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            
            // NEU: Menüpunkt "Freier Text hinzufügen"
            var addCustomTextMenuItem = new ToolStripMenuItem("\"Freier Text\" Kürzel hinzufügen", null, ContextMenu_AddCustomText);
            addCustomTextMenuItem.ShortcutKeys = Keys.Control | Keys.T;

            // NEU: Menüpunkt "Historie verwalten"
            var manageHistoryMenuItem = new ToolStripMenuItem("Freier Text - Historie verwalten", null, ContextMenu_ManageHistory);
            manageHistoryMenuItem.ShortcutKeys = Keys.Control | Keys.H;

            // Separator 2
            var separator2 = new ToolStripSeparator();
            
            // Menüpunkt "Als Word-Datei exportieren"
            var exportWordMenuItem = new ToolStripMenuItem("Kürzelerklärung als Word-Datei erstellen", null, ContextMenu_ExportWord);
            exportWordMenuItem.ShortcutKeys = Keys.Control | Keys.E;

            // NEU: Menüpunkt "Position zurücksetzen"
            var separator3 = new ToolStripSeparator();
            var resetPositionMenuItem = new ToolStripMenuItem("Fensterposition zurücksetzen", null, ContextMenu_ResetPosition);
            
            _contextMenu.Items.Add(editMenuItem);
            _contextMenu.Items.Add(deleteMenuItem);
            _contextMenu.Items.Add(separator1);
            _contextMenu.Items.Add(addMenuItem);
            _contextMenu.Items.Add(addCustomTextMenuItem);  // NEU
            _contextMenu.Items.Add(manageHistoryMenuItem); // NEU
            _contextMenu.Items.Add(separator2);
            _contextMenu.Items.Add(exportWordMenuItem);
            _contextMenu.Items.Add(separator3);
            _contextMenu.Items.Add(resetPositionMenuItem); // NEU

            // ContextMenu mit ListBox verbinden
            _lstTags.ContextMenuStrip = _contextMenu;
            
            // Event: ContextMenu wird geöffnet
            _contextMenu.Opening += ContextMenu_Opening;
        }

        // Ersetzen Sie ContextMenu_Opening (Zeile 127-135):

        private void ContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            // Deaktiviere "Bearbeiten" und "Löschen" wenn nichts ausgewählt ist
            bool hasSelection = _lstTags.SelectedIndex >= 0;

            // NEU: Prüfe ob "Freier Text" bereits hinzugefügt wurde
            bool hasCustomText = _tags.Any(t => t.IsCustomText);

            if (_contextMenu != null)
            {
                _contextMenu.Items[0].Enabled = hasSelection; // Bearbeiten
                _contextMenu.Items[1].Enabled = hasSelection; // Löschen
                // Items[2] = Separator
                // Items[3] = Neuer Tag hinzufügen
                _contextMenu.Items[4].Enabled = !hasCustomText; // "Freier Text" Kürzel hinzufügen
                _contextMenu.Items[5].Enabled = hasCustomText;  // Historie verwalten (nur wenn Kürzel existiert)
                                                                // Items[6] = Separator
                                                                // Items[7] = Word-Export

                // Tooltips setzen
                if (hasCustomText)
                {
                    ((ToolStripMenuItem)_contextMenu.Items[4]).ToolTipText = "\"Freier Text\" wurde bereits hinzugefügt";
                }
                else
                {
                    ((ToolStripMenuItem)_contextMenu.Items[5]).ToolTipText = "Fügen Sie zuerst das \"Freier Text\"-Kürzel hinzu";
                }
            }
        }

        private void ContextMenu_Edit(object? sender, EventArgs e)
        {
            BtnEdit_Click(sender, e);
        }

        private void ContextMenu_Delete(object? sender, EventArgs e)
        {
            BtnDelete_Click(sender, e);
        }

        private void ContextMenu_Add(object? sender, EventArgs e)
        {
            BtnAdd_Click(sender, e);
        }

        // NEU: Fügt das spezielle "Freier Text"-Kürzel hinzu
        private void ContextMenu_AddCustomText(object? sender, EventArgs e)
        {
            // Prüfe ob bereits vorhanden
            if (_tags.Any(t => t.IsCustomText))
            {
                MessageBox.Show("Das \"Freier Text\"-Kürzel wurde bereits hinzugefügt.", 
                    "Bereits vorhanden", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }
            
            // Erstelle das spezielle Kürzel
            var customTextTag = new TagItem
            {
                Character = "freier Text", // GEÄNDERT: Vollständiger Text statt "T"
                Color = DrawingColor.FromArgb(200, 200, 200), // Hellgrau
                Description = "Freie Texteingabe beim Kürzelsetzen - max 50 Zeichen", // GEÄNDERT
                IsCustomText = true
            };
            
            _tags.Add(customTextTag);
            RefreshList();
            SaveTags();
            
            MessageBox.Show(
                "Das \"Freier Text\"-Kürzel wurde hinzugefügt.\n\n" +
                "Beim Taggen können Sie bis zu 50 Zeichen eigenen Text eingeben.\n" +
                "Dieses Kürzel kann mehrfach verwendet werden.\n\n" +
                "Nach dem ersten Setzen werden Sie gefragt, ob Sie den Text\n" +
                "beim nächsten Mal wiederverwenden möchten.",
                "Freier Text hinzugefügt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Fügen Sie nach der ContextMenu_AddCustomText-Methode (Zeile 188) ein:

        // NEU: Handler für Historie-Verwaltung
        private void ContextMenu_ManageHistory(object? sender, EventArgs e)
        {
            if (!_tags.Any(t => t.IsCustomText))
            {
                MessageBox.Show(
                    "Das \"Freier Text\"-Kürzel ist noch nicht vorhanden.\n\n" +
                    "Fügen Sie es zuerst hinzu: Rechtsklick → \"Freier Text\" Kürzel hinzufügen",
                    "Kürzel fehlt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var dialog = new CustomTextHistoryDialog();
            dialog.ShowDialog(this);
        }

        // NEU: Export nach Word
        private void ContextMenu_ExportWord(object? sender, EventArgs e)
        {
            if (_tags.Count == 0)
            {
                MessageBox.Show("Es sind keine Tags zum Exportieren vorhanden.", "Keine Tags", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Word-Dokument (*.docx)|*.docx",
                FileName = "Kürzelerklärung.docx",
                Title = "Kürzelerklärung speichern"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    CreateWordDocument(sfd.FileName);
                    
                    var result = MessageBox.Show(
                        $"Kürzelerklärung wurde erfolgreich erstellt:\n{sfd.FileName}\n\nMöchten Sie die Datei jetzt öffnen?",
                        "Export erfolgreich", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Erstellen der Word-Datei:\n{ex.Message}", 
                        "Export-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // NEU: Word-Dokument erstellen (AKTUALISIERT - mit farbigem Feld ohne Text)
        private void CreateWordDocument(string filePath)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Kopfzeile (Header) erstellen - FÜR ALLE SEITEN
                HeaderPart headerPart = mainPart.AddNewPart<HeaderPart>();
                string headerPartId = mainPart.GetIdOfPart(headerPart);
                GenerateHeader(headerPart, mainPart);

                // Fußzeile (Footer) erstellen - FÜR ALLE SEITEN
                FooterPart footerPart = mainPart.AddNewPart<FooterPart>();
                string footerPartId = mainPart.GetIdOfPart(footerPart);
                GenerateFooter(footerPart);

                // Überschrift hinzufügen
                Paragraph titleParagraph = body.AppendChild(new Paragraph());
                Run titleRun = titleParagraph.AppendChild(new Run());
                titleRun.AppendChild(new Text("Kürzelerklärung für PDF-Datei"));
                
                RunProperties titleRunProperties = titleRun.AppendChild(new RunProperties());
                titleRunProperties.AppendChild(new Bold());
                titleRunProperties.AppendChild(new FontSize() { Val = "32" });
                
                ParagraphProperties titleParagraphProperties = titleParagraph.AppendChild(new ParagraphProperties());
                titleParagraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
                titleParagraphProperties.AppendChild(new SpacingBetweenLines() { After = "400" });

                // Tabelle erstellen
                Table table = new Table();

                TableProperties tableProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    ),
                    new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
                );
                table.AppendChild(tableProperties);

                // Kopfzeile der Tabelle
                TableRow headerRow = new TableRow();
                TableRowProperties headerRowProperties = new TableRowProperties();
                headerRowProperties.Append(new TableRowHeight() { Val = 600, HeightType = HeightRuleValues.AtLeast }); // NEU: HeightType
                headerRow.Append(headerRowProperties);
                
                headerRow.Append(
                    CreateTableCell("Kürzel", true, true, true, "28"),      // GEÄNDERT: auch fett
                    CreateTableCell("Farbe", true, true, true, "28"),       // GEÄNDERT: auch fett
                    CreateTableCell("Beschreibung", true, true, true, "28") // GEÄNDERT: auch fett
                );
                table.AppendChild(headerRow);

                // Datenzeilen hinzufügen
                bool isFirstRow = true;
                foreach (var tag in _tags)
                {
                    if (!isFirstRow)
                    {
                        TableRow emptyRow = new TableRow();
                        TableRowProperties emptyRowProperties = new TableRowProperties();
                        emptyRowProperties.Append(new TableRowHeight() { Val = 200, HeightType = HeightRuleValues.Exact }); // NEU: HeightType
                        emptyRow.Append(emptyRowProperties);

                        emptyRow.AppendChild(CreateTableCell("", false, false, false, "22"));
                        emptyRow.AppendChild(CreateTableCell("", false, false, false, "22"));
                        emptyRow.AppendChild(CreateTableCell("", false, false, false, "22"));

                        table.AppendChild(emptyRow);
                    }
                    isFirstRow = false;

                    TableRow dataRow = new TableRow();
                    TableRowProperties dataRowProperties = new TableRowProperties();
                    dataRowProperties.Append(new TableRowHeight() { Val = 600, HeightType = HeightRuleValues.AtLeast }); // GEÄNDERT: Höher + AtLeast
                    dataRow.Append(dataRowProperties);
                    
                    // Kürzel-Spalte: Zeige "freier Text" für CustomText
                    string displayCharacter = tag.IsCustomText ? "freier Text" : tag.Character;
                    dataRow.AppendChild(CreateTableCell(displayCharacter, false, true, true, "32")); // 16pt, fett, zentriert

                    // Farbspalte: Auch "freier Text" anzeigen
                    string colorCharacter = tag.IsCustomText ? "freier Text" : tag.Character;
                    dataRow.AppendChild(CreateColorCellWithCharacter(colorCharacter, tag.Color));

                    // Beschreibung: GRÖßER, FETT, mit LINKEM ABSTAND - vertikale Zentrierung ist bereits vorhanden
                    // NEU: Bei "Freier Text" Hinweis hinzufügen
                    string description = tag.IsCustomText 
                        ? tag.Description + " (variabel beim Taggen)" 
                        : tag.Description;
                    
                    dataRow.AppendChild(CreateTableCell(description, false, false, true, "28")); // 14pt, fett, links mit Abstand
                    
                    table.AppendChild(dataRow);
                }

                body.AppendChild(table);

                // KORRIGIERT: SectionProperties für ALLE Seiten
                SectionProperties sectionProperties = new SectionProperties();
                
                // Header-Referenz für DEFAULT (alle Seiten)
                HeaderReference headerReference = new HeaderReference() 
                { 
                    Type = HeaderFooterValues.Default,  // ✅ Alle Seiten
                    Id = headerPartId 
                };
                
                // Footer-Referenz für DEFAULT (alle Seiten)
                FooterReference footerReference = new FooterReference() 
                { 
                    Type = HeaderFooterValues.Default, 
                    Id = footerPartId 
                };
                
                sectionProperties.Append(headerReference);
                sectionProperties.Append(footerReference);

                // SectionProperties am Ende des Body hinzufügen
                body.Append(sectionProperties);

                mainPart.Document.Save();
            }
        }

        // NEU: Kopfzeile erstellen
        private void GenerateHeader(HeaderPart headerPart, MainDocumentPart mainPart)
        {
            var companyData = LoadCompanyData();
            
            Header header = new Header();
            
            // Zeile 1: Firmenname - Arial 18pt (professionell)
            Paragraph companyParagraph = new Paragraph();
            ParagraphProperties companyParaProps = new ParagraphProperties();
            companyParaProps.Append(new Justification() { Val = JustificationValues.Center });
            companyParagraph.Append(companyParaProps);
            
            Run companyRun = new Run();
            RunProperties companyRunProps = new RunProperties();
            companyRunProps.Append(new Bold());
            companyRunProps.Append(new FontSize() { Val = "36" });  // GEÄNDERT: 18pt (war 14pt)
            companyRunProps.Append(new RunFonts() { Ascii = "Arial" });  // NEU: Arial
            companyRunProps.Append(new WordColor() { Val = "2E75B6" }); // Blau
            companyRun.Append(companyRunProps);
            companyRun.Append(new Text(string.IsNullOrWhiteSpace(companyData.CompanyName) ? "Kürzelerklärung" : companyData.CompanyName));
            companyParagraph.Append(companyRun);
            header.Append(companyParagraph);
            
            // Zeile 2: Konto, Kontoart, Jahr - Calibri 11pt
            Paragraph infoParagraph = new Paragraph();
            ParagraphProperties infoParaProps = new ParagraphProperties();
            infoParaProps.Append(new Justification() { Val = JustificationValues.Center });
            infoParaProps.Append(new SpacingBetweenLines() { After = "100" });
            infoParagraph.Append(infoParaProps);
            
            Run infoRun = new Run();
            RunProperties infoRunProps = new RunProperties();
            infoRunProps.Append(new FontSize() { Val = "22" });  // GEÄNDERT: 11pt (war 10pt)
            infoRunProps.Append(new RunFonts() { Ascii = "Calibri" });  // NEU: Calibri
            infoRunProps.Append(new WordColor() { Val = "808080" });
            infoRun.Append(infoRunProps);
            
            string infoText = $"{companyData.AccountNumber} | {companyData.AccountType} | {companyData.Year}";
            if (string.IsNullOrWhiteSpace(companyData.AccountNumber) && 
                string.IsNullOrWhiteSpace(companyData.AccountType) && 
                string.IsNullOrWhiteSpace(companyData.Year))
            {
                infoText = DateTime.Now.ToString("dd.MM.yyyy");
            }
            
            infoRun.Append(new Text(infoText));
            infoParagraph.Append(infoRun);
            header.Append(infoParagraph);
            
            // Trennlinie (unverändert)
            Paragraph lineParagraph = new Paragraph();
            ParagraphProperties lineParaProps = new ParagraphProperties();
            ParagraphBorders borders = new ParagraphBorders();
            borders.Append(new BottomBorder() 
            { 
                Val = BorderValues.Single, 
                Size = 12, 
                Color = "2E75B6" 
            });
            lineParaProps.Append(borders);
            lineParaProps.Append(new SpacingBetweenLines() { After = "200" });
            lineParagraph.Append(lineParaProps);
            header.Append(lineParagraph);
            
            headerPart.Header = header;
        }

        // AKTUALISIERTE Fußzeile mit neuen Schriftarten und -größen
        private void GenerateFooter(FooterPart footerPart)
        {
            var companyData = LoadCompanyData();
            
            Footer footer = new Footer();
            Table footerTable = new Table();
            
            TableProperties tableProperties = new TableProperties();
            tableProperties.Append(new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct });
            
            TableBorders tableBorders = new TableBorders();
            tableBorders.Append(new TopBorder() { Val = BorderValues.Single, Size = 4, Color = "808080" });
            tableBorders.Append(new BottomBorder() { Val = BorderValues.None });
            tableBorders.Append(new LeftBorder() { Val = BorderValues.None });
            tableBorders.Append(new RightBorder() { Val = BorderValues.None });
            tableBorders.Append(new InsideHorizontalBorder() { Val = BorderValues.None });
            tableBorders.Append(new InsideVerticalBorder() { Val = BorderValues.None });
            tableProperties.Append(tableBorders);
            
            footerTable.Append(tableProperties);
            TableRow footerRow = new TableRow();

            // Linke Spalte - E-Mail (Calibri 10pt)
            TableCell leftCell = new TableCell();
            Paragraph leftParagraph = new Paragraph();
            Run leftRun = new Run();
            RunProperties leftRunProperties = new RunProperties();
            leftRunProperties.Append(new FontSize() { Val = "20" });  // GEÄNDERT: 10pt (war 9pt)
            leftRunProperties.Append(new RunFonts() { Ascii = "Calibri" });  // NEU: Calibri
            leftRunProperties.Append(new WordColor() { Val = "808080" });
            leftRun.Append(leftRunProperties);
            leftRun.Append(new Text(string.IsNullOrWhiteSpace(companyData.Email) ? 
                $"Erstellt: {DateTime.Now:dd.MM.yyyy HH:mm}" : 
                $"E-Mail: {companyData.Email}"));
            leftParagraph.Append(leftRun);
            leftCell.Append(leftParagraph);
            footerRow.Append(leftCell);

            // Mittlere Spalte - Seitenzahl (Calibri 10pt)
            TableCell middleCell = new TableCell();
            Paragraph middleParagraph = new Paragraph();
            ParagraphProperties middleParaProps = new ParagraphProperties();
            middleParaProps.Append(new Justification() { Val = JustificationValues.Center });
            middleParagraph.Append(middleParaProps);
            
            Run middleRun = new Run();
            RunProperties middleRunProperties = new RunProperties();
            middleRunProperties.Append(new FontSize() { Val = "20" });  // GEÄNDERT: 10pt
            middleRunProperties.Append(new RunFonts() { Ascii = "Calibri" });  // NEU: Calibri
            middleRunProperties.Append(new WordColor() { Val = "808080" });
            middleRun.Append(middleRunProperties);
            middleRun.Append(new Text("Seite "));
            
            // Seitenzahl-Feld
            Run pageNumRun = new Run();
            RunProperties pageNumRunProps = new RunProperties();
            pageNumRunProps.Append(new FontSize() { Val = "20" });  // GEÄNDERT: 10pt
            pageNumRunProps.Append(new RunFonts() { Ascii = "Calibri" });  // NEU
            pageNumRunProps.Append(new WordColor() { Val = "808080" });
            pageNumRun.Append(pageNumRunProps);
            pageNumRun.Append(new FieldChar() { FieldCharType = FieldCharValues.Begin });
            
            Run pageNumInstrRun = new Run();
            RunProperties pageNumInstrRunProps = new RunProperties();
            pageNumInstrRunProps.Append(new FontSize() { Val = "20" });  // GEÄNDERT
            pageNumInstrRunProps.Append(new RunFonts() { Ascii = "Calibri" });  // NEU
            pageNumInstrRun.Append(pageNumInstrRunProps);
            pageNumInstrRun.Append(new FieldCode(" PAGE "));
            
            Run pageNumEndRun = new Run();
            pageNumEndRun.Append(new FieldChar() { FieldCharType = FieldCharValues.End });
            
            middleParagraph.Append(middleRun);
            middleParagraph.Append(pageNumRun);
            middleParagraph.Append(pageNumInstrRun);
            middleParagraph.Append(pageNumEndRun);
            middleCell.Append(middleParagraph);
            footerRow.Append(middleCell);

            // rechte Spalte - Telefon (Calibri 10pt)
            TableCell rightCell = new TableCell();
            Paragraph rightParagraph = new Paragraph();
            ParagraphProperties rightParaProps = new ParagraphProperties();
            rightParaProps.Append(new Justification() { Val = JustificationValues.Right });
            rightParagraph.Append(rightParaProps);
            
            Run rightRun = new Run();
            RunProperties rightRunProperties = new RunProperties();
            rightRunProperties.Append(new FontSize() { Val = "20" });  // GEÄNDERT: 10pt
            rightRunProperties.Append(new RunFonts() { Ascii = "Calibri" });  // NEU: Calibri
            rightRunProperties.Append(new WordColor() { Val = "808080" });
            rightRun.Append(rightRunProperties);
            rightRun.Append(new Text(string.IsNullOrWhiteSpace(companyData.Phone) ? 
                $"© {DateTime.Now.Year}" : 
                $"Tel: {companyData.Phone}"));
            rightParagraph.Append(rightRun);
            rightCell.Append(rightParagraph);
            footerRow.Append(rightCell);

            footerTable.Append(footerRow);
            footer.Append(footerTable);
            
            footerPart.Footer = footer;
        }

        // NEU: Spezial-Kopfzeile für die erste Seite
        private void GenerateFirstPageHeader(HeaderPart headerPart)
        {
            Header header = new Header();
            Paragraph paragraph = new Paragraph();
            Run run = new Run();
            run.Append(new Text("Erste Seite - Spezial-Header"));
            paragraph.Append(run);
            header.Append(paragraph);
            headerPart.Header = header;
        }

        // ERWEITERTE Hilfsmethode: Tabellenzelle erstellen mit mehr Optionen
        private TableCell CreateTableCell(string text, bool isHeader, bool isCentered, bool isBold = false, string fontSize = "22")
        {
            TableCell cell = new TableCell();

            // Zellen-Eigenschaften
            TableCellProperties cellProperties = new TableCellProperties();
            cellProperties.Append(new TableCellWidth() { Type = TableWidthUnitValues.Auto });
            cellProperties.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
            
            if (isHeader)
            {
                cellProperties.Append(new Shading()
                {
                    Val = ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = "D0D0D0"
                });
            }
            
            cell.Append(cellProperties);

            // Text-Paragraph
            Paragraph paragraph = new Paragraph();
            
            // Paragraph-Eigenschaften
            ParagraphProperties paragraphProperties = new ParagraphProperties();
            if (isCentered)
            {
                paragraphProperties.Append(new Justification() { Val = JustificationValues.Center });
            }
            else
            {
                // NEU: Einzug für linksbündigen Text (Beschreibung)
                paragraphProperties.Append(new Indentation() { Left = "100" }); // Linker Abstand
            }
            paragraph.Append(paragraphProperties);
            
            Run run = new Run();
            run.Append(new Text(text));

            // Formatierung
            RunProperties runProperties = new RunProperties();
            if (isHeader)
            {
                runProperties.Append(new Bold());
                runProperties.Append(new FontSize() { Val = "28" }); // GEÄNDERT: 14pt statt 11pt (größer)
            }
            else
            {
                if (isBold)
                {
                    runProperties.Append(new Bold());
                }
                runProperties.Append(new FontSize() { Val = fontSize });
            }
            run.PrependChild(runProperties);

            paragraph.Append(run);
            cell.Append(paragraph);

            return cell;
        }

        // Hilfsmethode: Color zu Hex-String
        private string ColorToHex(DrawingColor color)  // GEÄNDERT: DrawingColor
        {
            return $"{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void LoadTags()
        {
            try
            {
                if (File.Exists(TagsFilePath))
                {
                    var json = File.ReadAllText(TagsFilePath);
                    var loadedTags = JsonSerializer.Deserialize<List<TagItem>>(json);
                    
                    if (loadedTags != null && loadedTags.Count > 0)
                    {
                        _tags = loadedTags;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Tags:\n{ex.Message}\n\nStandard-Tags werden geladen.", 
                    "Ladefehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            LoadDefaultTags();
        }

        private void SaveTags()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                var json = JsonSerializer.Serialize(_tags, options);
                File.WriteAllText(TagsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Tags:\n{ex.Message}", 
                    "Speicherfehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDefaultTags()
        {
            _tags.Clear();
            _tags.Add(new TagItem { Character = "M", Color = DrawingColor.FromArgb(220, 120, 0), Description = "Wichtig" });  // GEÄNDERT
            _tags.Add(new TagItem { Character = "P", Color = DrawingColor.FromArgb(0, 160, 80), Description = "Persönlich" });
            _tags.Add(new TagItem { Character = "R", Color = DrawingColor.FromArgb(200, 40, 40), Description = "Rechnung" });
            _tags.Add(new TagItem { Character = "B", Color = DrawingColor.FromArgb(0, 120, 220), Description = "Bank" });
            _tags.Add(new TagItem { Character = "K", Color = DrawingColor.FromArgb(80, 80, 80), Description = "Kontoauszug" });
            
            SaveTags();
        }

        private void RefreshList()
        {
            _lstTags.Items.Clear();
            foreach (var tag in _tags)
            {
                _lstTags.Items.Add(tag);
            }
        }

        private void LstTags_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _tags.Count)
                return;

            var tag = _tags[e.Index];
            
            e.DrawBackground();

            // Zeichne farbiges Rechteck mit Text
            var tagRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 5, 100, 40); // GEÄNDERT: Breiter für "freier Text"
            using (var brush = new SolidBrush(tag.Color))
            {
                e.Graphics.FillRectangle(brush, tagRect);
            }
            using (var pen = new Pen(DrawingColor.Black, 2))
            {
                e.Graphics.DrawRectangle(pen, tagRect);
            }

            // Zeichne Text im Rechteck
            if (tag.IsCustomText)
            {
                // GEÄNDERT: "freier Text" in kleinerer Schrift
                using (var font = new System.Drawing.Font("Arial", 9F, FontStyle.Bold))
                using (var sf = new StringFormat 
                { 
                    Alignment = StringAlignment.Center, 
                    LineAlignment = StringAlignment.Center 
                })
                {
                    e.Graphics.DrawString("freier Text", font, Brushes.White, tagRect, sf);
                }
            }
            else
            {
                // Normale Tags: Großer einzelner Buchstabe
                using (var font = new System.Drawing.Font("Arial", 20, FontStyle.Bold))
                using (var sf = new StringFormat 
                { 
                    Alignment = StringAlignment.Center, 
                    LineAlignment = StringAlignment.Center 
                })
                {
                    e.Graphics.DrawString(tag.Character, font, Brushes.White, tagRect, sf);
                }
            }

            // Zeichne Beschreibung rechts daneben
            var textRect = new Rectangle(e.Bounds.X + 120, e.Bounds.Y, e.Bounds.Width - 130, e.Bounds.Height); // GEÄNDERT: Angepasst
    
            float descFontSize = tag.IsCustomText ? 9F : 12F;
            FontStyle descFontStyle = tag.IsCustomText ? FontStyle.Regular : FontStyle.Bold;
            
            using (var font = new System.Drawing.Font("Segoe UI", descFontSize, descFontStyle))
            using (var sf = new StringFormat 
            { 
                Alignment = StringAlignment.Near, 
                LineAlignment = StringAlignment.Center 
            })
            {
                var textColor = (e.State & DrawItemState.Selected) != 0 
                    ? SystemColors.HighlightText 
                    : SystemColors.ControlText;
                using (var brush = new SolidBrush(textColor))
                {
                    // GEÄNDERT: Zeige "freier Text - Beschreibung" oder "Buchstabe - Beschreibung"
                    string displayText = tag.IsCustomText 
                        ? $"{tag.Character} - {tag.Description}" 
                        : $"{tag.Character} - {tag.Description}";
                    
                    e.Graphics.DrawString(displayText, font, brush, textRect, sf);
                }
            }

            e.DrawFocusRectangle();
        }

        private void LstTags_Click(object? sender, EventArgs e)
        {
            if (_lstTags.SelectedIndex < 0 || _lstTags.SelectedIndex >= _tags.Count)
                return;

            var selectedTag = _tags[_lstTags.SelectedIndex];
            
            // Visuelles Feedback
            var originalBackColor = _lstTags.BackColor;
            _lstTags.BackColor = DrawingColor.LightYellow; // ← Explizit qualifiziert
            
            // Feuere das Click-Event
            TagClicked?.Invoke(this, selectedTag);
            
            // Zurücksetzen nach kurzer Verzögerung
            var timer = new System.Windows.Forms.Timer { Interval = 200 };
            timer.Tick += (s, ev) =>
            {
                _lstTags.BackColor = originalBackColor;
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void LstTags_DoubleClick(object? sender, EventArgs e)
        {
            if (_lstTags.SelectedIndex < 0 || _lstTags.SelectedIndex >= _tags.Count)
                return;

            var selectedTag = _tags[_lstTags.SelectedIndex];
        
            // Übergebe das komplette TagItem statt nur Character und Color
            TagDoubleClicked?.Invoke(this, selectedTag);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var dlg = new TagEditDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _tags.Add(new TagItem 
                { 
                    Character = dlg.TagCharacter, 
                    Color = dlg.TagColor, 
                    Description = dlg.TagDescription 
                });
                RefreshList();
                SaveTags();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_lstTags.SelectedIndex < 0 || _lstTags.SelectedIndex >= _tags.Count)
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Tag aus.", "Hinweis", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tag = _tags[_lstTags.SelectedIndex];
        
            // NEU: Verhindere Bearbeitung von "Freier Text"
            if (tag.IsCustomText)
            {
                MessageBox.Show("Das \"Freier Text\"-Kürzel kann nicht bearbeitet werden.", 
                    "Nicht editierbar", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }
        
            using var dlg = new TagEditDialog
            {
                TagCharacter = tag.Character,
                TagColor = tag.Color,
                TagDescription = tag.Description
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                tag.Character = dlg.TagCharacter;
                tag.Color = dlg.TagColor;
                tag.Description = dlg.TagDescription;
                RefreshList();
                SaveTags();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_lstTags.SelectedIndex < 0 || _lstTags.SelectedIndex >= _tags.Count)
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Tag aus.", "Hinweis", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var tag = _tags[_lstTags.SelectedIndex];
        
            // NEU: Warnung beim Löschen von "Freier Text"
            if (tag.IsCustomText)
            {
                var result = MessageBox.Show(
                    "Möchten Sie das \"Freier Text\"-Kürzel wirklich löschen?\n\n" +
                    "Sie können es über das Kontextmenü wieder hinzufügen.", 
                    "Freier Text löschen", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
            
                if (result != DialogResult.Yes)
                    return;
            }
            else
            {
                var result = MessageBox.Show($"Möchten Sie den Tag '{tag.Character} - {tag.Description}' wirklich löschen?", 
                    "Löschen bestätigen", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;
            }

            _tags.RemoveAt(_lstTags.SelectedIndex);
            RefreshList();
            SaveTags();
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void Apply3DStyle(Button b)
        {
            bool pressed = false;

            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.BackColor = DrawingColor.FromArgb(235, 235, 235);
            b.ForeColor = DrawingColor.Black;

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

        private CompanyData LoadCompanyData()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "companydata.json");
        
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<CompanyData>(json);
                    if (data != null)
                        return data;
                }
            }
            catch
            {
                // Fehler ignorieren, Standarddaten zurückgeben
            }
        
            return new CompanyData();
        }

        private TableCell CreateColorCellWithCharacter(string character, DrawingColor color)
        {
            TableCell cell = new TableCell();

            TableCellProperties cellProperties = new TableCellProperties();
            cellProperties.Append(new TableCellWidth() { Width = "800", Type = TableWidthUnitValues.Dxa });
            cellProperties.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });

            cellProperties.Append(new Shading()
            {
                Val = ShadingPatternValues.Clear,
                Color = "auto",
                Fill = ColorToHex(color)
            });

            cell.Append(cellProperties);

            Paragraph paragraph = new Paragraph();
            ParagraphProperties paragraphProperties = new ParagraphProperties();
            paragraphProperties.Append(new Justification() { Val = JustificationValues.Center });
            paragraph.Append(paragraphProperties);

            Run run = new Run();
            run.Append(new Text(character));

            RunProperties runProperties = new RunProperties();
            runProperties.Append(new Bold());
            runProperties.Append(new FontSize() { Val = "32" });
            runProperties.Append(new RunFonts() { Ascii = "Arial" });
            runProperties.Append(new WordColor() { Val = "FFFFFF" });
            run.PrependChild(runProperties);

            paragraph.Append(run);
            cell.Append(paragraph);

            return cell;
        }

        // NEU: Position zurücksetzen
        private void ContextMenu_ResetPosition(object? sender, EventArgs e)
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

        public void UpdateUI()
        {
            // CategoriesForm: Die UI-Elemente haben keine lokalisierbaren Texte
            // Die Buttons verwenden Icons oder sind selbsterklärend
            // Falls in Zukunft lokalisierbare Texte hinzugefügt werden, hier einfügen
        }
    } // Ende CategoriesForm
} // Ende Namespace
    


