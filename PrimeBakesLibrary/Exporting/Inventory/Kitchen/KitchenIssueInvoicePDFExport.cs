using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Convert Kitchen Issue data to Invoice PDF format
/// </summary>
public static class KitchenIssueInvoicePDFExport
{
    /// <summary>
    /// Export Kitchen Issue as a professional invoice PDF (automatically loads item names)
    /// Uses simplified invoice format without discount and tax columns
    /// </summary>
    /// <param name="kitchenIssueHeader">Kitchen Issue header data</param>
    /// <param name="kitchenIssueDetails">Kitchen Issue detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="kitchen">Kitchen/Location information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (KITCHEN ISSUE, MATERIAL ISSUE, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenIssueInvoice(
        KitchenIssueModel kitchenIssueHeader,
        List<KitchenIssueDetailModel> kitchenIssueDetails,
        CompanyModel company,
        LocationModel kitchen,
        string logoPath = null,
        string invoiceType = "KITCHEN ISSUE")
    {
        // Load all raw materials to get names
        var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        // Map line items with actual item names
        var cartItems = kitchenIssueDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
            string itemName = item?.Name ?? $"Item #{detail.RawMaterialId}";

            return new KitchenIssueItemCartModel
            {
                ItemId = detail.RawMaterialId,
                ItemName = itemName,
                Quantity = detail.Quantity,
                UnitOfMeasurement = detail.UnitOfMeasurement,
                Rate = detail.Rate,
                Total = detail.Total,
                Remarks = detail.Remarks
            };
        }).ToList();

        // Use the simplified export method
        return ExportKitchenIssueInvoiceWithItems(
            kitchenIssueHeader,
            cartItems,
            company,
            kitchen,
            logoPath,
            invoiceType
        );
    }

    /// <summary>
    /// Export Kitchen Issue with item names already loaded (requires additional data)
    /// Uses simplified invoice format without discount and tax columns
    /// </summary>
    public static MemoryStream ExportKitchenIssueInvoiceWithItems(
        KitchenIssueModel kitchenIssueHeader,
        List<KitchenIssueItemCartModel> kitchenIssueItems,
        CompanyModel company,
        LocationModel kitchen,
        string logoPath = null,
        string invoiceType = "KITCHEN ISSUE")
    {
        MemoryStream ms = new();

        try
        {
            using PdfDocument pdfDocument = new();
            PdfPage page = pdfDocument.Pages.Add();
            PdfGraphics graphics = page.Graphics;

            float pageWidth = page.GetClientSize().Width;
            float leftMargin = 20;
            float rightMargin = 20;
            float currentY = 15;

            // 1. Header Section with Logo and Company Info
            currentY = DrawInvoiceHeader(graphics, company, logoPath, leftMargin, pageWidth, currentY);

            // 2. Invoice Type and Number
            currentY = DrawInvoiceTitle(graphics, invoiceType, kitchenIssueHeader.TransactionNo, leftMargin, pageWidth, currentY);

            // 2.5. Draw DELETED status badge if Status is false
            if (!kitchenIssueHeader.Status)
            {
                currentY = DrawDeletedStatusBadge(graphics, pageWidth, currentY);
            }

            // 3. Company and Kitchen Information
            currentY = DrawCompanyAndKitchenInfo(graphics, company, kitchen, kitchenIssueHeader, leftMargin, pageWidth, currentY);

            // 4. Simplified Line Items Table (without discount and tax columns)
            currentY = DrawSimplifiedLineItemsTable(page, pdfDocument, kitchenIssueItems, leftMargin, rightMargin, pageWidth, currentY);

            // 5. Summary Section
            currentY = DrawSimplifiedSummary(graphics, kitchenIssueHeader, leftMargin, pageWidth, currentY);

            // 6. Amount in Words
            currentY = DrawAmountInWords(graphics, kitchenIssueHeader.TotalAmount, leftMargin, pageWidth, currentY);

            // 7. Software Branding Footer
            DrawSoftwareBrandingFooter(graphics, leftMargin, pageWidth, page.GetClientSize().Height);

            pdfDocument.Save(ms);
            ms.Position = 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting kitchen issue invoice to PDF: {ex.Message}");
            throw;
        }

        return ms;
    }

    #region Private Helper Methods

    private static float DrawInvoiceHeader(PdfGraphics graphics, CompanyModel company, string logoPath, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;

        try
        {
            string[] possibleLogoPaths;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                possibleLogoPaths = [logoPath];
            }
            else
            {
                possibleLogoPaths =
                [
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo_full.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot", "images", "logo_full.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo_full.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrimeBakes", "PrimeBakes", "wwwroot", "images", "logo_full.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "PrimeBakes", "PrimeBakes.Web", "wwwroot", "images", "logo_full.png")
                ];
            }

            string resolvedLogoPath = possibleLogoPaths.FirstOrDefault(File.Exists);

            if (!string.IsNullOrEmpty(resolvedLogoPath))
            {
                using FileStream imageStream = new(resolvedLogoPath, FileMode.Open, FileAccess.Read);
                PdfBitmap logoBitmap = new(imageStream);

                float maxLogoHeight = 50;
                float logoWidth = logoBitmap.Width;
                float logoHeight = logoBitmap.Height;
                float aspectRatio = logoWidth / logoHeight;

                if (logoHeight > maxLogoHeight)
                {
                    logoHeight = maxLogoHeight;
                    logoWidth = logoHeight * aspectRatio;
                }

                float logoX = (pageWidth - logoWidth) / 2;
                graphics.DrawImage(logoBitmap, new PointF(logoX, currentY), new SizeF(logoWidth, logoHeight));
                currentY += logoHeight + 8;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logo loading failed: {ex.Message}");
        }

        PdfPen separatorPen = new(new PdfColor(59, 130, 246), 2f);
        graphics.DrawLine(separatorPen, new PointF(leftMargin, currentY), new PointF(pageWidth - 20, currentY));
        currentY += 6;

        return currentY;
    }

    private static float DrawInvoiceTitle(PdfGraphics graphics, string invoiceType, string invoiceNumber, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;

        PdfStandardFont titleFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        PdfBrush titleBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
        graphics.DrawString(invoiceType.ToUpper(), titleFont, titleBrush, new PointF(leftMargin, currentY));

        PdfStandardFont numberFont = new(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        string invoiceNumberText = $"Invoice #: {invoiceNumber}";
        SizeF numberSize = numberFont.MeasureString(invoiceNumberText);
        graphics.DrawString(invoiceNumberText, numberFont, new PdfSolidBrush(new PdfColor(0, 0, 0)),
            new PointF(pageWidth - 20 - numberSize.Width, currentY));

        currentY += 20;
        return currentY;
    }

    private static float DrawDeletedStatusBadge(PdfGraphics graphics, float pageWidth, float startY)
    {
        float badgeWidth = 120;
        float badgeHeight = 30;
        float badgeX = (pageWidth - badgeWidth) / 2;
        float badgeY = startY + 5;

        PdfBrush redBrush = new PdfSolidBrush(new PdfColor(220, 38, 38));
        graphics.DrawRectangle(redBrush, new RectangleF(badgeX, badgeY, badgeWidth, badgeHeight));

        PdfStandardFont badgeFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        PdfBrush whiteBrush = new PdfSolidBrush(new PdfColor(255, 255, 255));
        string deletedText = "DELETED";
        SizeF textSize = badgeFont.MeasureString(deletedText);
        float textX = badgeX + (badgeWidth - textSize.Width) / 2;
        float textY = badgeY + (badgeHeight - textSize.Height) / 2;
        graphics.DrawString(deletedText, badgeFont, whiteBrush, new PointF(textX, textY));

        return badgeY + badgeHeight + 10;
    }

    private static float DrawCompanyAndKitchenInfo(PdfGraphics graphics, CompanyModel company,
        LocationModel kitchen, KitchenIssueModel kitchenIssue, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;
        float columnWidth = (pageWidth - 40 - 10) / 2;
        float rightColumnX = leftMargin + columnWidth + 10;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));
        PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

        float padding = 5;
        float leftTextY = currentY + padding;
        float rightTextY = currentY + padding;

        // Left Column - From (Company)
        graphics.DrawString("FROM:", labelFont, labelBrush, new PointF(leftMargin + padding, leftTextY));
        leftTextY += 10;
        graphics.DrawString(company.Name, valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
        leftTextY += 9;

        if (!string.IsNullOrEmpty(company.Address))
        {
            int addressLines = Math.Max(1, company.Address.Length / 50);
            float addressHeight = addressLines * 9;
            DrawWrappedText(graphics, company.Address, valueFont, valueBrush,
                new RectangleF(leftMargin + padding, leftTextY, columnWidth - 2 * padding, addressHeight + 5));
            leftTextY += addressHeight + 2;
        }

        if (!string.IsNullOrEmpty(company.Phone))
        {
            graphics.DrawString($"Phone: {company.Phone}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
            leftTextY += 9;
        }

        if (!string.IsNullOrEmpty(company.GSTNo))
        {
            graphics.DrawString($"GSTIN: {company.GSTNo}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
            leftTextY += 9;
        }

        // Right Column - To (Kitchen)
        graphics.DrawString("ISSUED TO:", labelFont, labelBrush, new PointF(rightColumnX + padding, rightTextY));
        rightTextY += 10;
        graphics.DrawString(kitchen.Name, valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
        rightTextY += 9;

        float boxHeight = Math.Max(leftTextY, rightTextY) - currentY + padding;
        currentY += boxHeight + 8;

        // Invoice Date
        graphics.DrawString($"Issue Date: {kitchenIssue.TransactionDateTime:dd-MMM-yyyy hh:mm tt}", valueFont, valueBrush,
            new PointF(leftMargin, currentY));
        currentY += 15;

        return currentY;
    }

    private static float DrawSimplifiedLineItemsTable(PdfPage page, PdfDocument document, List<KitchenIssueItemCartModel> lineItems,
        float leftMargin, float rightMargin, float pageWidth, float startY)
    {
        PdfGrid pdfGrid = new();

        // Simplified columns: # Item Description Qty UOM Rate Total (no discount, taxable, tax%, tax amt)
        string[] columns = ["#", "Item Description", "Qty", "UOM", "Rate", "Total"];
        pdfGrid.Columns.Add(columns.Length);

        float availableWidth = pageWidth - leftMargin - rightMargin;
        // Calculate widths to fit exactly within available space
        float fixedColumnsWidth = 25 + 45 + 50 + 70 + 80; // Sum of all fixed width columns
        float descriptionWidth = availableWidth - fixedColumnsWidth; // Remaining space for description

        pdfGrid.Columns[0].Width = 25;  // #
        pdfGrid.Columns[1].Width = descriptionWidth;  // Item Description (uses remaining space)
        pdfGrid.Columns[2].Width = 45;  // Qty
        pdfGrid.Columns[3].Width = 50;  // UOM
        pdfGrid.Columns[4].Width = 70;  // Rate
        pdfGrid.Columns[5].Width = 80;  // Total

        pdfGrid.Style.AllowHorizontalOverflow = false;
        pdfGrid.RepeatHeader = true;
        pdfGrid.AllowRowBreakAcrossPages = false;

        // Add header row
        PdfGridRow headerRow = pdfGrid.Headers.Add(1)[0];
        for (int i = 0; i < columns.Length; i++)
        {
            headerRow.Cells[i].Value = columns[i];
            headerRow.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
            headerRow.Cells[i].Style.TextBrush = PdfBrushes.White;
            headerRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 7f, PdfFontStyle.Bold);
            headerRow.Cells[i].Style.StringFormat = new PdfStringFormat
            {
                Alignment = PdfTextAlignment.Center,
                LineAlignment = PdfVerticalAlignment.Middle,
                WordWrap = PdfWordWrapType.Word
            };
            headerRow.Cells[i].Style.CellPadding = new PdfPaddings(2f, 2f, 2f, 2f);
        }

        // Add data rows
        int rowNumber = 1;
        foreach (var item in lineItems)
        {
            PdfGridRow row = pdfGrid.Rows.Add();

            row.Cells[0].Value = rowNumber.ToString();
            row.Cells[1].Value = item.ItemName;
            row.Cells[2].Value = item.Quantity.ToString("#,##0.00");
            row.Cells[3].Value = item.UnitOfMeasurement;
            row.Cells[4].Value = item.Rate.ToString("#,##0.00");
            row.Cells[5].Value = item.Total.ToString("#,##0.00");

            // Cell styling
            for (int i = 0; i < columns.Length; i++)
            {
                row.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 7f);
                row.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(220, 220, 220), 0.5f);
                row.Cells[i].Style.CellPadding = new PdfPaddings(2f, 2f, 2f, 2f);

                // Right align numeric columns
                if (i >= 2 && i <= 5)
                {
                    row.Cells[i].Style.StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Right,
                        LineAlignment = PdfVerticalAlignment.Middle,
                        WordWrap = PdfWordWrapType.Word
                    };
                }
                else if (i == 1) // Description - left align
                {
                    row.Cells[i].Style.StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Left,
                        LineAlignment = PdfVerticalAlignment.Middle,
                        WordWrap = PdfWordWrapType.Word
                    };
                }
                else // Center align for #
                {
                    row.Cells[i].Style.StringFormat = new PdfStringFormat
                    {
                        Alignment = PdfTextAlignment.Center,
                        LineAlignment = PdfVerticalAlignment.Middle,
                        WordWrap = PdfWordWrapType.Word
                    };
                }
            }

            // Alternating row colors
            if (rowNumber % 2 == 0)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    row.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(249, 250, 251));
                }
            }

            rowNumber++;
        }

        // Draw grid
        PdfGridLayoutFormat layoutFormat = new()
        {
            Layout = PdfLayoutType.Paginate,
            Break = PdfLayoutBreakType.FitPage,
            PaginateBounds = new RectangleF(leftMargin, startY, pageWidth - leftMargin - rightMargin,
                page.GetClientSize().Height - startY - 150)
        };

        PdfGridLayoutResult result = pdfGrid.Draw(page, new PointF(leftMargin, startY), layoutFormat);
        return result.Bounds.Bottom + 8;
    }

    private static float DrawSimplifiedSummary(PdfGraphics graphics, KitchenIssueModel kitchenIssue, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;
        float summaryColumnX = pageWidth - 200;
        float rightMargin = 20;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
        PdfStandardFont totalFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(80, 80, 80));
        PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

        // Remarks (Left side)
        if (!string.IsNullOrWhiteSpace(kitchenIssue.Remarks))
        {
            PdfStandardFont remarksLabelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
            PdfStandardFont remarksValueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
            PdfBrush remarksBrush = new PdfSolidBrush(new PdfColor(60, 60, 60));

            graphics.DrawString("Remarks:", remarksLabelFont, new PdfSolidBrush(new PdfColor(0, 0, 0)), new PointF(leftMargin, currentY));
            currentY += 12;

            float remarksBoxWidth = pageWidth - 240;
            float textWidth = remarksBoxWidth - 10;

            PdfStringFormat format = new()
            {
                LineAlignment = PdfVerticalAlignment.Top,
                Alignment = PdfTextAlignment.Left,
                WordWrap = PdfWordWrapType.Word
            };

            SizeF textSize = remarksValueFont.MeasureString(kitchenIssue.Remarks, textWidth, format);
            float remarksBoxHeight = textSize.Height + 10;

            if (remarksBoxHeight < 30)
                remarksBoxHeight = 30;

            RectangleF remarksTextRect = new(leftMargin + 5, currentY + 5, remarksBoxWidth - 10, remarksBoxHeight - 10);
            DrawWrappedText(graphics, kitchenIssue.Remarks, remarksValueFont, remarksBrush, remarksTextRect);

            // Update currentY to account for the full remarks box height
            currentY += remarksBoxHeight + 5;
        }

        // Total (Right side - reset Y to startY for right column)
        float summaryY = startY;

        // Draw line above total
        PdfPen linePen = new(new PdfColor(59, 130, 246), 1f);
        graphics.DrawLine(linePen, new PointF(summaryColumnX - 10, summaryY), new PointF(pageWidth - 20, summaryY));
        summaryY += 4;

        // Total Amount
        PdfBrush totalBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
        graphics.DrawString("TOTAL:", totalFont, totalBrush, new PointF(summaryColumnX, summaryY));
        string totalText = $"{kitchenIssue.TotalAmount:N2}";
        SizeF totalSize = totalFont.MeasureString(totalText);
        graphics.DrawString(totalText, totalFont, totalBrush, new PointF(pageWidth - rightMargin - totalSize.Width, summaryY));
        summaryY += 15;

        return Math.Max(currentY, summaryY);
    }

    private static float DrawAmountInWords(PdfGraphics graphics, decimal amount, float leftMargin, float pageWidth, float startY)
    {
        float currentY = startY;

        PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Italic);
        PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

        string amountInWords = ConvertAmountToWords(amount);

        graphics.DrawString("Amount in Words:", labelFont, labelBrush, new PointF(leftMargin, currentY));
        currentY += 10;

        DrawWrappedText(graphics, amountInWords, valueFont, labelBrush,
            new RectangleF(leftMargin, currentY, pageWidth - 40, 20));
        currentY += 18;

        return currentY;
    }

    private static void DrawSoftwareBrandingFooter(PdfGraphics graphics, float leftMargin, float pageWidth, float pageHeight)
    {
        float footerY = pageHeight - 20;

        PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Regular);
        PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));

        PdfPen separatorPen = new(new PdfColor(200, 200, 200), 0.5f);
        graphics.DrawLine(separatorPen, new PointF(leftMargin, footerY - 5), new PointF(pageWidth - 20, footerY - 5));

        string brandingText = "Generated from Prime Bakes - A Product of aadisoft.vercel.app";
        SizeF textSize = footerFont.MeasureString(brandingText);
        float textX = (pageWidth - textSize.Width) / 2;

        graphics.DrawString(brandingText, footerFont, footerBrush, new PointF(textX, footerY));
    }

    private static void DrawWrappedText(PdfGraphics graphics, string text, PdfFont font, PdfBrush brush, RectangleF bounds)
    {
        if (string.IsNullOrEmpty(text))
            return;

        PdfStringFormat format = new()
        {
            LineAlignment = PdfVerticalAlignment.Top,
            Alignment = PdfTextAlignment.Left,
            WordWrap = PdfWordWrapType.Word
        };

        graphics.DrawString(text, font, brush, bounds, format);
    }

    private static string ConvertAmountToWords(decimal amount)
    {
        try
        {
            if (amount == 0)
                return "Zero Rupees Only";

            string[] ones = ["", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine"];
            string[] teens = ["Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"];
            string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

            long rupees = (long)amount;
            int paise = (int)((amount - rupees) * 100);

            string words = "";

            if (rupees >= 10000000)
            {
                words += ConvertNumberToWords(rupees / 10000000, ones, teens, tens) + " Crore ";
                rupees %= 10000000;
            }

            if (rupees >= 100000)
            {
                words += ConvertNumberToWords(rupees / 100000, ones, teens, tens) + " Lakh ";
                rupees %= 100000;
            }

            if (rupees >= 1000)
            {
                words += ConvertNumberToWords(rupees / 1000, ones, teens, tens) + " Thousand ";
                rupees %= 1000;
            }

            if (rupees >= 100)
            {
                words += ConvertNumberToWords(rupees / 100, ones, teens, tens) + " Hundred ";
                rupees %= 100;
            }

            if (rupees > 0)
            {
                if (rupees < 10)
                    words += ones[rupees];
                else if (rupees < 20)
                    words += teens[rupees - 10];
                else
                {
                    words += tens[rupees / 10];
                    if (rupees % 10 > 0)
                        words += " " + ones[rupees % 10];
                }
            }

            words = words.Trim() + " Rupees";

            if (paise > 0)
            {
                words += " and " + ConvertNumberToWords(paise, ones, teens, tens) + " Paise";
            }

            words += " Only";

            return words;
        }
        catch
        {
            return "Amount in Words Not Available";
        }
    }

    private static string ConvertNumberToWords(long number, string[] ones, string[] teens, string[] tens)
    {
        if (number == 0)
            return "";

        if (number < 10)
            return ones[number];

        if (number < 20)
            return teens[number - 10];

        if (number < 100)
        {
            string result = tens[number / 10];
            if (number % 10 > 0)
                result += " " + ones[number % 10];
            return result;
        }

        return "";
    }

    #endregion
}
