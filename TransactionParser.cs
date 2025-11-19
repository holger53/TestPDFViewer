using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace PdfiumOverlayTest
{
    public class TransactionParser
    {
        private const int RENDER_DPI = 150;
        
        public static TransactionData ParsePdf(string pdfFilePath, IntPtr pdfDocument, int pageCount)
        {
            var transactionData = new TransactionData
            {
                PdfHash = CalculateFileHash(pdfFilePath),
                OriginalFilePath = pdfFilePath,
                AnalyzedAt = DateTime.Now
            };
            
            System.Diagnostics.Debug.WriteLine($"Starte PDF-Analyse: {pdfFilePath}");
            
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var pageTransactions = ParsePage(pdfDocument, pageIndex);
                transactionData.Transactions.AddRange(pageTransactions);
                
                System.Diagnostics.Debug.WriteLine($"Seite {pageIndex + 1}/{pageCount}: {pageTransactions.Count} Transaktionen gefunden");
            }
            
            System.Diagnostics.Debug.WriteLine($"PDF-Analyse abgeschlossen: {transactionData.Transactions.Count} Transaktionen insgesamt");
            
            return transactionData;
        }
        
        private static List<TransactionPosition> ParsePage(IntPtr pdfDocument, int pageIndex)
        {
            var transactions = new List<TransactionPosition>();
            
            var page = PDFium.FPDF_LoadPage(pdfDocument, pageIndex);  // ÄNDERUNG
            if (page == IntPtr.Zero)
                return transactions;
            
            try
            {
                var textPage = PDFium.FPDFText_LoadPage(page);  // ÄNDERUNG
                if (textPage == IntPtr.Zero)
                    return transactions;
                
                try
                {
                    int charCount = PDFium.FPDFText_CountChars(textPage);  // ÄNDERUNG
                    
                    var textBuilder = new StringBuilder(charCount + 1);
                    var positions = new List<(int charIndex, double x, double y)>();
                    
                    for (int i = 0; i < charCount; i++)
                    {
                        uint character = PDFium.FPDFText_GetUnicode(textPage, i);  // ÄNDERUNG
                        if (character != 0)
                        {
                            textBuilder.Append((char)character);
                            
                            if (PDFium.FPDFText_GetCharBox(textPage, i, out double left, out double right, out double bottom, out double top))  // ÄNDERUNG
                            {
                                positions.Add((i, left, top));
                            }
                        }
                    }
                    
                    string pageText = textBuilder.ToString();
                    
                    transactions = DetectTransactions(pageText, positions, pageIndex);
                }
                finally
                {
                    PDFium.FPDFText_ClosePage(textPage);  // ÄNDERUNG
                }
            }
            finally
            {
                PDFium.FPDF_ClosePage(page);  // ÄNDERUNG
            }
            
            return transactions;
        }
        
        private static List<TransactionPosition> DetectTransactions(
            string pageText, 
            List<(int charIndex, double x, double y)> positions,
            int pageIndex)
        {
            var transactions = new List<TransactionPosition>();
            
            var datePattern = @"\b(\d{2}\.\d{2}\.\d{2,4})\b";
            var amountPattern = @"(-?\d{1,3}(?:\.\d{3})*,\d{2})\s*€?";
            
            var dateMatches = Regex.Matches(pageText, datePattern);
            
            foreach (Match dateMatch in dateMatches)
            {
                int charIndex = dateMatch.Index;
                
                if (charIndex < positions.Count)
                {
                    var pos = positions[charIndex];
                    
                    int x = (int)(pos.x * RENDER_DPI / 72.0);
                    int y = (int)(pos.y * RENDER_DPI / 72.0);
                    
                    int lineEnd = Math.Min(charIndex + 100, pageText.Length);
                    string lineText = pageText.Substring(charIndex, lineEnd - charIndex);
                    
                    var amountMatch = Regex.Match(lineText, amountPattern);
                    decimal? amount = null;
                    if (amountMatch.Success)
                    {
                        string amountStr = amountMatch.Groups[1].Value
                            .Replace(".", "")
                            .Replace(",", ".");
                        if (decimal.TryParse(amountStr, out decimal parsedAmount))
                        {
                            amount = parsedAmount;
                        }
                    }
                    
                    DateTime? date = null;
                    if (DateTime.TryParseExact(
                        dateMatch.Groups[1].Value,
                        new[] { "dd.MM.yyyy", "dd.MM.yy" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime parsedDate))
                    {
                        date = parsedDate;
                    }
                    
                    transactions.Add(new TransactionPosition
                    {
                        X = x,
                        Y = y,
                        PageIndex = pageIndex,
                        TransactionText = lineText.Trim(),
                        Date = date,
                        Amount = amount
                    });
                }
            }
            
            return transactions;
        }
        
        private static string CalculateFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}