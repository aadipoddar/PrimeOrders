using NumericWordsConversion;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Models.Inventory;
using PrimeOrdersLibrary.Models.Sale;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace PrimeOrdersLibrary.Data.Sale;

public static class SaleA4Print
{
	private static readonly Color _primaryColor = Color.FromArgb(81, 43, 212);
	private static readonly Color _secondaryColor = Color.FromArgb(104, 33, 122);
	private static readonly Color _accentColor = Color.FromArgb(0, 164, 239);
	private static readonly Color _lightGray = Color.FromArgb(245, 245, 245);
	private static readonly Color _darkGray = Color.FromArgb(64, 64, 64);

	// Page layout constants
	private const float _pageMargin = 20f;
	private const float _rowHeight = 25f;
	private const float _headerHeight = 30f;
	private const float _footerReservedSpace = 100f; // Space reserved for footer content

	public static async Task<byte[]> GenerateA4SaleBill(SaleModel sale, List<SaleProductCartModel> saleProducts)
	{
		using var document = new PdfDocument();
		var page = document.Pages.Add();
		var graphics = page.Graphics;

		// Page dimensions
		var pageSize = page.GetClientSize();
		var pageWidth = pageSize.Width;
		var pageHeight = pageSize.Height;

		// Initialize fonts
		var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 24, PdfFontStyle.Bold);
		var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
		var subHeaderFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
		var normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
		var boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
		var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);

		float currentY = _pageMargin;

		// Header Section with Company Info (only on first page)
		currentY = await DrawHeader(graphics, pageWidth, currentY, titleFont, headerFont, normalFont, sale.LocationId);

		// Invoice Details Section (only on first page)
		currentY = DrawInvoiceDetails(graphics, pageWidth, currentY, subHeaderFont, normalFont, boldFont, sale);

		// Customer Information (only on first page, if party exists)
		if (sale.PartyId.HasValue && sale.PartyId.Value > 0)
		{
			currentY = await DrawCustomerInfo(graphics, pageWidth, currentY, subHeaderFont, normalFont, sale);
		}

		// Items Table with pagination support
		var (finalY, lastPage) = DrawItemsTableWithPagination(document, saleProducts, currentY, pageWidth, pageHeight, subHeaderFont, normalFont, boldFont);

		// Use the last page for remaining content
		graphics = lastPage.Graphics;
		currentY = finalY;

		// Summary Section
		currentY = DrawSummarySection(graphics, pageWidth, currentY, subHeaderFont, normalFont, boldFont, saleProducts, sale);

		// Amount in Words
		currentY = DrawAmountInWords(graphics, pageWidth, currentY, normalFont, boldFont, saleProducts.Sum(x => x.Total));

		// Payment Information
		currentY = DrawPaymentInfo(graphics, pageWidth, currentY, normalFont, boldFont, sale);

		// Footer on all pages
		DrawFooterOnAllPages(document, pageWidth, pageHeight, smallFont, normalFont);

		// Save the document
		using var stream = new MemoryStream();
		document.Save(stream);
		return stream.ToArray();
	}

	private static (float finalY, PdfPage lastPage) DrawItemsTableWithPagination(PdfDocument document, List<SaleProductCartModel> saleProducts,
		float startY, float pageWidth, float pageHeight, PdfFont subHeaderFont, PdfFont normalFont, PdfFont boldFont)
	{
		if (saleProducts == null || saleProducts.Count == 0)
			return (startY + 50, document.Pages[0]);

		var currentPage = document.Pages[0];
		var graphics = currentPage.Graphics;
		var currentY = startY;
		var pageNumber = 1;

		// Table configuration
		var tableStartX = _pageMargin;
		var tableWidth = pageWidth - _pageMargin * 2;

		// Define column widths (as percentages of table width)
		var col1Width = tableWidth * 0.08f;  // S.No - 8%
		var col2Width = tableWidth * 0.40f;  // Product Name - 40%
		var col3Width = tableWidth * 0.12f;  // Qty - 12%
		var col4Width = tableWidth * 0.15f;  // Rate - 15%
		var col5Width = tableWidth * 0.15f;  // Amount - 15%
		var col6Width = tableWidth * 0.10f;  // Total - 10%

		// Calculate column positions
		var col1X = tableStartX;
		var col2X = col1X + col1Width;
		var col3X = col2X + col2Width;
		var col4X = col3X + col3Width;
		var col5X = col4X + col4Width;
		var col6X = col5X + col5Width;

		// Draw table header
		DrawTableHeader(graphics, tableStartX, currentY, tableWidth, col1X, col2X, col3X, col4X, col5X, col6X, boldFont);
		currentY += _headerHeight;

		// Calculate maximum Y position before needing a new page
		var maxYForContent = pageHeight - _footerReservedSpace;

		// Draw table rows with pagination
		for (int i = 0; i < saleProducts.Count; i++)
		{
			// Check if we need a new page
			if (currentY + _rowHeight > maxYForContent)
			{
				// Add a new page
				currentPage = document.Pages.Add();
				graphics = currentPage.Graphics;
				currentY = _pageMargin;
				pageNumber++;

				// Draw table header on new page
				DrawTableHeader(graphics, tableStartX, currentY, tableWidth, col1X, col2X, col3X, col4X, col5X, col6X, boldFont);
				currentY += _headerHeight;
			}

			// Draw the row
			DrawTableRow(graphics, saleProducts[i], i, tableStartX, currentY, tableWidth,
				col1X, col2X, col3X, col4X, col5X, col6X, col1Width, col2Width, col3Width, col4Width, col5Width, col6Width,
				normalFont, boldFont);

			currentY += _rowHeight;
		}

		// Add some space after table
		currentY += 20;

		return (currentY, currentPage);
	}

	private static void DrawTableHeader(PdfGraphics graphics, float tableStartX, float currentY, float tableWidth,
		float col1X, float col2X, float col3X, float col4X, float col5X, float col6X, PdfFont boldFont)
	{
		// Draw table header background
		var tableHeaderRect = new RectangleF(tableStartX, currentY, tableWidth, _headerHeight);
		graphics.DrawRectangle(new PdfPen(Color.Black, 1), new PdfSolidBrush(_primaryColor), tableHeaderRect);

		// Draw column separators for header
		DrawColumnSeparators(graphics, currentY, _headerHeight, col1X, col2X, col3X, col4X, col5X, col6X, tableStartX + tableWidth);

		// Header text with proper alignment
		var headerY = currentY + 8;

		// S.No - Center aligned
		DrawCenteredText(graphics, "S.No", boldFont, PdfBrushes.White, col1X, tableWidth * 0.08f, headerY);

		// Product Name - Left aligned
		graphics.DrawString("Product Name", boldFont, PdfBrushes.White, new PointF(col2X + 5, headerY));

		// Qty - Center aligned  
		DrawCenteredText(graphics, "Qty", boldFont, PdfBrushes.White, col3X, tableWidth * 0.12f, headerY);

		// Rate - Right aligned
		DrawRightAlignedText(graphics, "Rate", boldFont, PdfBrushes.White, col4X, tableWidth * 0.15f, headerY);

		// Amount - Right aligned
		DrawRightAlignedText(graphics, "Amount", boldFont, PdfBrushes.White, col5X, tableWidth * 0.15f, headerY);

		// Total - Right aligned
		DrawRightAlignedText(graphics, "Total", boldFont, PdfBrushes.White, col6X, tableWidth * 0.10f, headerY);
	}

	private static void DrawTableRow(PdfGraphics graphics, SaleProductCartModel product, int index,
		float tableStartX, float currentY, float tableWidth,
		float col1X, float col2X, float col3X, float col4X, float col5X, float col6X,
		float col1Width, float col2Width, float col3Width, float col4Width, float col5Width, float col6Width,
		PdfFont normalFont, PdfFont boldFont)
	{
		// Draw row background
		var rowRect = new RectangleF(tableStartX, currentY, tableWidth, _rowHeight);
		var rowBrush = index % 2 == 0 ? PdfBrushes.White : new PdfSolidBrush(Color.FromArgb(248, 249, 250));
		graphics.DrawRectangle(new PdfPen(Color.Black, 0.5f), rowBrush, rowRect);

		// Draw column separators for each row
		DrawColumnSeparators(graphics, currentY, _rowHeight, col1X, col2X, col3X, col4X, col5X, col6X, tableStartX + tableWidth);

		var rowY = currentY + 6;

		// S.No - Center aligned
		DrawCenteredText(graphics, $"{index + 1}", normalFont, PdfBrushes.Black, col1X, col1Width, rowY);

		// Product Name - Left aligned with truncation
		var productName = TruncateString(product.ProductName ?? "Unknown", 30);
		graphics.DrawString(productName, normalFont, PdfBrushes.Black, new PointF(col2X + 5, rowY));

		// Qty - Center aligned
		DrawCenteredText(graphics, $"{product.Quantity:N2}", normalFont, PdfBrushes.Black, col3X, col3Width, rowY);

		// Rate - Right aligned
		DrawRightAlignedText(graphics, $"₹{product.Rate:N2}", normalFont, PdfBrushes.Black, col4X, col4Width, rowY);

		// Amount - Right aligned
		DrawRightAlignedText(graphics, $"₹{product.BaseTotal:N2}", normalFont, PdfBrushes.Black, col5X, col5Width, rowY);

		// Total - Right aligned with color
		DrawRightAlignedText(graphics, $"₹{product.Total:N2}", boldFont, new PdfSolidBrush(_secondaryColor), col6X, col6Width, rowY);
	}

	private static void DrawFooterOnAllPages(PdfDocument document, float pageWidth, float pageHeight, PdfFont smallFont, PdfFont normalFont)
	{
		for (int i = 0; i < document.Pages.Count; i++)
		{
			var page = document.Pages[i];
			var graphics = page.Graphics;

			var footerY = pageHeight - 60;

			// Thank you message (only on last page)
			if (i == document.Pages.Count - 1)
			{
				var thankYouText = "Thank you for your business!";
				var thankYouSize = normalFont.MeasureString(thankYouText);
				graphics.DrawString(thankYouText, normalFont, new PdfSolidBrush(_primaryColor),
					new PointF((pageWidth - thankYouSize.Width) / 2, footerY));
			}

			// Footer line
			graphics.DrawLine(new PdfPen(_lightGray, 1), new PointF(20, footerY + 20), new PointF(pageWidth - 20, footerY + 20));

			// System info with page number
			var systemInfo = $"Generated by Prime Orders System | Page {i + 1} of {document.Pages.Count} | Printed on: {DateTime.Now:dd/MM/yyyy HH:mm}";
			var systemInfoSize = smallFont.MeasureString(systemInfo);
			graphics.DrawString(systemInfo, smallFont, new PdfSolidBrush(_darkGray),
				new PointF((pageWidth - systemInfoSize.Width) / 2, footerY + 30));
		}
	}

	private static async Task<float> DrawHeader(PdfGraphics graphics, float pageWidth, float currentY, PdfFont titleFont, PdfFont headerFont, PdfFont normalFont, int locationId)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId);

		// Company name with colored background
		var headerRect = new RectangleF(0, currentY, pageWidth, 60);
		graphics.DrawRectangle(new PdfSolidBrush(_primaryColor), headerRect);

		// Company name
		var companyName = location?.Name ?? "Prime Orders";
		var companySize = titleFont.MeasureString(companyName);
		graphics.DrawString(companyName, titleFont, PdfBrushes.White,
			new PointF((pageWidth - companySize.Width) / 2, currentY + 15));

		currentY += 70;

		// Company details in a box
		var detailsRect = new RectangleF(20, currentY, pageWidth - 40, 80);
		graphics.DrawRectangle(new PdfPen(_lightGray, 1), new PdfSolidBrush(_lightGray), detailsRect);

		var detailsY = currentY + 10;
		graphics.DrawString("GST NO: XXXXXXX", normalFont, new PdfSolidBrush(_darkGray), new PointF(30, detailsY));
		graphics.DrawString("Salasar Foods Guwahati", normalFont, new PdfSolidBrush(_darkGray), new PointF(30, detailsY + 15));
		graphics.DrawString("Mobile No: XXXXXX", normalFont, new PdfSolidBrush(_darkGray), new PointF(30, detailsY + 30));
		graphics.DrawString("Email: info@primeorders.com", normalFont, new PdfSolidBrush(_darkGray), new PointF(30, detailsY + 45));

		// Invoice title on the right
		var invoiceTitle = "SALES INVOICE";
		var invoiceTitleSize = headerFont.MeasureString(invoiceTitle);
		graphics.DrawString(invoiceTitle, headerFont, new PdfSolidBrush(_secondaryColor),
			new PointF(pageWidth - invoiceTitleSize.Width - 30, detailsY + 20));

		return currentY + 100;
	}

	private static float DrawInvoiceDetails(PdfGraphics graphics, float pageWidth, float currentY, PdfFont subHeaderFont, PdfFont normalFont, PdfFont boldFont, SaleModel sale)
	{
		// Invoice details section
		var detailsRect = new RectangleF(20, currentY, pageWidth - 40, 60);
		graphics.DrawRectangle(new PdfPen(Color.Black, 1), detailsRect);

		// Left side details
		var leftX = 30;
		var rightX = pageWidth / 2 + 20;
		var detailY = currentY + 10;

		graphics.DrawString("Invoice Details", subHeaderFont, new PdfSolidBrush(_primaryColor), new PointF(leftX, detailY));

		detailY += 25;
		graphics.DrawString("Invoice No: ", normalFont, PdfBrushes.Black, new PointF(leftX, detailY));
		graphics.DrawString($"{sale.BillNo ?? "N/A"}", boldFont, new PdfSolidBrush(_secondaryColor), new PointF(leftX + 70, detailY));

		// Right side details
		detailY = currentY + 35;
		graphics.DrawString("Date: ", normalFont, PdfBrushes.Black, new PointF(rightX, detailY));
		graphics.DrawString($"{sale.SaleDateTime:dddd, MMMM dd, yyyy hh:mm tt}", boldFont, new PdfSolidBrush(_secondaryColor), new PointF(rightX + 35, detailY));

		return currentY + 80;
	}

	private static async Task<float> DrawCustomerInfo(PdfGraphics graphics, float pageWidth, float currentY,
		PdfFont subHeaderFont, PdfFont normalFont, SaleModel sale)
	{
		var party = await CommonData.LoadTableDataById<SupplierModel>(TableNames.Supplier, sale.PartyId!.Value);
		if (party is null) return currentY;

		// Customer info section
		var customerRect = new RectangleF(20, currentY, pageWidth - 40, 60);
		graphics.DrawRectangle(new PdfPen(Color.Black, 1), new PdfSolidBrush(_lightGray), customerRect);

		var customerY = currentY + 10;
		graphics.DrawString("Bill To:", subHeaderFont, new PdfSolidBrush(_primaryColor), new PointF(30, customerY));

		customerY += 20;
		graphics.DrawString($"Name: {party.Name ?? "N/A"}", normalFont, PdfBrushes.Black, new PointF(30, customerY));
		customerY += 15;
		if (!string.IsNullOrEmpty(party.Phone))
			graphics.DrawString($"Phone: {party.Phone}", normalFont, PdfBrushes.Black, new PointF(30, customerY));

		return currentY + 80;
	}

	private static float DrawSummarySection(PdfGraphics graphics, float pageWidth, float currentY, PdfFont subHeaderFont, PdfFont normalFont, PdfFont boldFont, List<SaleProductCartModel> saleProducts, SaleModel sale)
	{
		if (saleProducts == null || saleProducts.Count == 0)
			return currentY + 50;

		var summaryWidth = 250f;
		var summaryX = pageWidth - summaryWidth - 20;
		var summaryHeight = 160f;

		// Summary box
		var summaryRect = new RectangleF(summaryX, currentY, summaryWidth, summaryHeight);
		graphics.DrawRectangle(new PdfPen(Color.Black, 1), new PdfSolidBrush(_lightGray), summaryRect);

		var summaryY = currentY + 10;
		graphics.DrawString("Summary", subHeaderFont, new PdfSolidBrush(_primaryColor), new PointF(summaryX + 10, summaryY));

		summaryY += 25;
		var baseTotal = saleProducts.Sum(x => x.BaseTotal);
		var discountTotal = saleProducts.Sum(x => x.DiscAmount);
		var cgstTotal = saleProducts.Sum(x => x.CGSTAmount);
		var sgstTotal = saleProducts.Sum(x => x.SGSTAmount);
		var igstTotal = saleProducts.Sum(x => x.IGSTAmount);
		var grandTotal = saleProducts.Sum(x => x.Total);

		// Summary lines with proper padding
		DrawSummaryLine(graphics, "Sub Total:", $"₹{baseTotal:N2}", summaryX, summaryWidth, summaryY, normalFont, boldFont);
		summaryY += 15;

		if (discountTotal > 0)
		{
			DrawSummaryLine(graphics, $"Discount ({sale.DiscPercent:N1}%):", $"-₹{discountTotal:N2}", summaryX, summaryWidth, summaryY, normalFont, boldFont);
			summaryY += 15;
		}

		if (cgstTotal > 0)
		{
			var cgstPercent = saleProducts.FirstOrDefault()?.CGSTPercent ?? 0;
			DrawSummaryLine(graphics, $"CGST ({cgstPercent:N1}%):", $"₹{cgstTotal:N2}", summaryX, summaryWidth, summaryY, normalFont, boldFont);
			summaryY += 15;
		}

		if (sgstTotal > 0)
		{
			var sgstPercent = saleProducts.FirstOrDefault()?.SGSTPercent ?? 0;
			DrawSummaryLine(graphics, $"SGST ({sgstPercent:N1}%):", $"₹{sgstTotal:N2}", summaryX, summaryWidth, summaryY, normalFont, boldFont);
			summaryY += 15;
		}

		if (igstTotal > 0)
		{
			var igstPercent = saleProducts.FirstOrDefault()?.IGSTPercent ?? 0;
			DrawSummaryLine(graphics, $"IGST ({igstPercent:N1}%):", $"₹{igstTotal:N2}", summaryX, summaryWidth, summaryY, normalFont, boldFont);
			summaryY += 15;
		}

		// Grand total with emphasis and proper padding
		summaryY += 10;
		var grandTotalRect = new RectangleF(summaryX + 5, summaryY - 5, summaryWidth - 10, 25);
		graphics.DrawRectangle(new PdfPen(_primaryColor, 2), new PdfSolidBrush(_primaryColor), grandTotalRect);
		graphics.DrawString("Grand Total:", new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold),
			PdfBrushes.White, new PointF(summaryX + 10, summaryY));

		var grandTotalText = $"₹{grandTotal:N2}";
		var grandTotalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
		var grandTotalSize = grandTotalFont.MeasureString(grandTotalText);
		// Added proper padding (15px from right edge instead of 10px)
		graphics.DrawString(grandTotalText, grandTotalFont,
			PdfBrushes.White, new PointF(summaryX + summaryWidth - grandTotalSize.Width - 15, summaryY));

		return currentY + summaryHeight + 20;
	}

	private static void DrawSummaryLine(PdfGraphics graphics, string label, string value, float summaryX, float summaryWidth, float y, PdfFont normalFont, PdfFont boldFont)
	{
		// Left side label with left padding
		graphics.DrawString(label, normalFont, PdfBrushes.Black, new PointF(summaryX + 10, y));

		// Right side value with proper right padding
		var valueSize = boldFont.MeasureString(value);
		// Changed from fixed position to calculated position with 15px padding from right edge
		graphics.DrawString(value, boldFont, new PdfSolidBrush(_secondaryColor),
			new PointF(summaryX + summaryWidth - valueSize.Width - 15, y));
	}

	private static float DrawAmountInWords(PdfGraphics graphics, float pageWidth, float currentY, PdfFont normalFont, PdfFont boldFont, decimal total)
	{
		var numericWords = new CurrencyWordsConverter(new()
		{
			Culture = Culture.Hindi,
			OutputFormat = OutputFormat.English
		});

		var amountInWords = numericWords.ToWords(Math.Round(total));
		if (string.IsNullOrEmpty(amountInWords))
			amountInWords = "Zero";

		amountInWords += " Rupees Only";

		// Amount in words box
		var wordsRect = new RectangleF(20, currentY, pageWidth - 40, 40);
		graphics.DrawRectangle(new PdfPen(Color.Black, 1), new PdfSolidBrush(Color.FromArgb(250, 250, 250)), wordsRect);

		graphics.DrawString("Amount in Words:", boldFont, new PdfSolidBrush(_primaryColor), new PointF(30, currentY + 8));
		graphics.DrawString(amountInWords, normalFont, PdfBrushes.Black, new PointF(30, currentY + 23));

		return currentY + 60;
	}

	private static float DrawPaymentInfo(PdfGraphics graphics, float pageWidth, float currentY, PdfFont normalFont, PdfFont boldFont, SaleModel sale)
	{
		var paymentMode = GetPaymentMode(sale);

		var paymentRect = new RectangleF(20, currentY, pageWidth - 40, 30);
		graphics.DrawRectangle(new PdfPen(Color.Black, 1), paymentRect);

		graphics.DrawString("Payment Mode: ", normalFont, PdfBrushes.Black, new PointF(30, currentY + 8));
		graphics.DrawString(paymentMode, boldFont, new PdfSolidBrush(_secondaryColor), new PointF(120, currentY + 8));

		if (!string.IsNullOrEmpty(sale.Remarks))
		{
			currentY += 40;
			graphics.DrawString($"Remarks: {sale.Remarks}", normalFont, PdfBrushes.Black, new PointF(30, currentY));
		}

		return currentY + 50;
	}

	private static string GetPaymentMode(SaleModel sale)
	{
		if (sale.Cash > 0 && sale.Cash >= sale.Card + sale.UPI + sale.Credit)
			return "Cash";
		else if (sale.Card > 0 && sale.Card >= sale.Cash + sale.UPI + sale.Credit)
			return "Card";
		else if (sale.UPI > 0 && sale.UPI >= sale.Cash + sale.Card + sale.Credit)
			return "UPI";
		else if (sale.Credit > 0 && sale.Credit >= sale.Cash + sale.Card + sale.UPI)
			return "Credit";
		else if (sale.Cash > 0 || sale.Card > 0 || sale.UPI > 0 || sale.Credit > 0)
			return "Multiple";
		else
			return "Unknown";
	}

	private static void DrawColumnSeparators(PdfGraphics graphics, float y, float height, params float[] columnPositions)
	{
		foreach (var x in columnPositions)
			graphics.DrawLine(new PdfPen(Color.Black, 0.5f), new PointF(x, y), new PointF(x, y + height));
	}

	private static void DrawCenteredText(PdfGraphics graphics, string text, PdfFont font, PdfBrush brush, float columnX, float columnWidth, float y)
	{
		var textSize = font.MeasureString(text);
		var centeredX = columnX + (columnWidth - textSize.Width) / 2;
		graphics.DrawString(text, font, brush, new PointF(centeredX, y));
	}

	private static void DrawRightAlignedText(PdfGraphics graphics, string text, PdfFont font, PdfBrush brush, float columnX, float columnWidth, float y)
	{
		var textSize = font.MeasureString(text);
		var rightAlignedX = columnX + columnWidth - textSize.Width - 5; // 5px padding from right edge
		graphics.DrawString(text, font, brush, new PointF(rightAlignedX, y));
	}

	private static string TruncateString(string text, int maxLength)
	{
		if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
			return text ?? string.Empty;

		return text[..(maxLength - 3)] + "...";
	}
}
