using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Purchase;

using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace PrimeBakesLibrary.Exporting;

/// <summary>
/// Professional Invoice PDF export utility - SAP-style professional invoices
/// </summary>
public static class PDFInvoiceExportUtil
{
	#region Generic Invoice Models

	/// <summary>
	/// Generic invoice header data that works with any transaction type
	/// </summary>
	public class InvoiceData
	{
		public string TransactionNo { get; set; }
		public DateTime TransactionDateTime { get; set; }
		public decimal ItemsTotalAmount { get; set; }
		public decimal OtherChargesAmount { get; set; }
		public decimal OtherChargesPercent { get; set; }
		public decimal CashDiscountAmount { get; set; }
		public decimal CashDiscountPercent { get; set; }
		public decimal RoundOffAmount { get; set; }
		public decimal TotalAmount { get; set; }
		public string Remarks { get; set; }
	}

	/// <summary>
	/// Generic invoice line item that works with any transaction type
	/// </summary>
	public class InvoiceLineItem
	{
		public int ItemId { get; set; }
		public string ItemName { get; set; }
		public decimal Quantity { get; set; }
		public string UnitOfMeasurement { get; set; }
		public decimal Rate { get; set; }
		public decimal DiscountPercent { get; set; }
		public decimal AfterDiscount { get; set; }
		public decimal CGSTPercent { get; set; }
		public decimal SGSTPercent { get; set; }
		public decimal IGSTPercent { get; set; }
		public decimal TotalTaxAmount { get; set; }
		public decimal Total { get; set; }
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// Export invoice to PDF with professional layout (unified method for all transaction types)
	/// </summary>
	/// <param name="invoiceData">Generic invoice header data</param>
	/// <param name="lineItems">Generic invoice line items</param>
	/// <param name="company">Company information (Bill From)</param>
	/// <param name="customer">Customer/Party information (Bill To)</param>
	/// <param name="logoPath">Optional: Path to company logo</param>
	/// <param name="invoiceType">Type of invoice (INVOICE, PURCHASE RETURN, SALES INVOICE, etc.)</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportInvoiceToPdf(
		InvoiceData invoiceData,
		List<InvoiceLineItem> lineItems,
		CompanyModel company,
		LedgerModel customer,
		string logoPath = null,
		string invoiceType = "INVOICE")
	{
		MemoryStream ms = new();

		try
		{
			using PdfDocument pdfDocument = new();
			// Add page
			PdfPage page = pdfDocument.Pages.Add();
			PdfGraphics graphics = page.Graphics;

			float pageWidth = page.GetClientSize().Width;
			float leftMargin = 20;
			float rightMargin = 20;
			float currentY = 15;

			// 1. Header Section with Logo and Company Info
			currentY = DrawInvoiceHeader(graphics, company, logoPath, leftMargin, pageWidth, currentY);

			// 2. Invoice Type and Number
			currentY = DrawInvoiceTitle(graphics, invoiceType, invoiceData.TransactionNo, leftMargin, pageWidth, currentY);

			// 3. Company and Customer Information (Two Columns)
			currentY = DrawCompanyAndCustomerInfo(graphics, company, customer, invoiceData, leftMargin, pageWidth, currentY);

			// 4. Line Items Table
			currentY = DrawLineItemsTable(page, pdfDocument, lineItems, leftMargin, rightMargin, pageWidth, currentY);

			// 5. Summary Section with Remarks (two-column layout)
			currentY = DrawSummaryAndRemarks(graphics, invoiceData, leftMargin, pageWidth, currentY);

			// 6. Amount in Words
			currentY = DrawAmountInWords(graphics, invoiceData.TotalAmount, leftMargin, pageWidth, currentY);

			// 7. Software Branding Footer
			DrawSoftwareBrandingFooter(graphics, leftMargin, pageWidth, page.GetClientSize().Height);

			// Save PDF document to stream
			pdfDocument.Save(ms);
			ms.Position = 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting invoice to PDF: {ex.Message}");
			throw;
		}

		return ms;
	}

	#endregion

	#region Private Helper Methods

	/// <summary>
	/// Draw invoice header with logo and company name
	/// </summary>
	private static float DrawInvoiceHeader(PdfGraphics graphics, CompanyModel company, string logoPath, float leftMargin, float pageWidth, float startY)
	{
		float currentY = startY;

		// Try to load logo - use same logic as PDFReportExportUtil
		try
		{
			// Use custom logo path if provided, otherwise try default locations
			string[] possibleLogoPaths;

			if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
			{
				possibleLogoPaths = [logoPath];
			}
			else
			{
				// Try multiple possible logo paths
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

				// Calculate logo dimensions (larger for top placement)
				float maxLogoHeight = 50;
				float logoWidth = logoBitmap.Width;
				float logoHeight = logoBitmap.Height;
				float aspectRatio = logoWidth / logoHeight;

				if (logoHeight > maxLogoHeight)
				{
					logoHeight = maxLogoHeight;
					logoWidth = logoHeight * aspectRatio;
				}

				// Center the logo horizontally at the top
				float logoX = (pageWidth - logoWidth) / 2;
				graphics.DrawImage(logoBitmap, new PointF(logoX, currentY), new SizeF(logoWidth, logoHeight));
				currentY += logoHeight + 8;
			}
		}
		catch (Exception ex)
		{
			// Log error but continue without logo
			Console.WriteLine($"Logo loading failed: {ex.Message}");
		}

		// Draw a separator line
		PdfPen separatorPen = new(new PdfColor(59, 130, 246), 2f);
		graphics.DrawLine(separatorPen, new PointF(leftMargin, currentY), new PointF(pageWidth - 20, currentY));
		currentY += 6;

		return currentY;
	}

	/// <summary>
	/// Draw invoice type and number
	/// </summary>
	private static float DrawInvoiceTitle(PdfGraphics graphics, string invoiceType, string invoiceNumber, float leftMargin, float pageWidth, float startY)
	{
		float currentY = startY;

		// Invoice Type (e.g., "TAX INVOICE")
		PdfStandardFont titleFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
		PdfBrush titleBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
		graphics.DrawString(invoiceType.ToUpper(), titleFont, titleBrush, new PointF(leftMargin, currentY));

		// Invoice Number (right aligned)
		PdfStandardFont numberFont = new(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
		string invoiceNumberText = $"Invoice #: {invoiceNumber}";
		SizeF numberSize = numberFont.MeasureString(invoiceNumberText);
		graphics.DrawString(invoiceNumberText, numberFont, new PdfSolidBrush(new PdfColor(0, 0, 0)),
			new PointF(pageWidth - 20 - numberSize.Width, currentY));

		currentY += 20;
		return currentY;
	}

	/// <summary>
	/// Draw company and customer information in two columns
	/// </summary>
	private static float DrawCompanyAndCustomerInfo(PdfGraphics graphics, CompanyModel company,
		LedgerModel customer, InvoiceData invoiceData, float leftMargin, float pageWidth, float startY)
	{
		float currentY = startY;
		float columnWidth = (pageWidth - 40 - 10) / 2; // 10px gap between columns
		float rightColumnX = leftMargin + columnWidth + 10;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));
		PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

		float padding = 5;
		float leftTextY = currentY + padding;
		float rightTextY = currentY + padding;
		float leftStartY = leftTextY;
		float rightStartY = rightTextY;

		// Left Column - From (Company)
		graphics.DrawString("FROM:", labelFont, labelBrush, new PointF(leftMargin + padding, leftTextY));
		leftTextY += 10;

		graphics.DrawString(company.Name, valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
		leftTextY += 9;

		if (!string.IsNullOrEmpty(company.Address))
		{
			// Calculate actual address height dynamically
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

		if (!string.IsNullOrEmpty(company.Email))
		{
			graphics.DrawString($"Email: {company.Email}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
			leftTextY += 9;
		}

		if (!string.IsNullOrEmpty(company.GSTNo))
		{
			graphics.DrawString($"GSTIN: {company.GSTNo}", valueFont, valueBrush, new PointF(leftMargin + padding, leftTextY));
			leftTextY += 9;
		}

		// Right Column - To (Customer)
		graphics.DrawString("BILL TO:", labelFont, labelBrush, new PointF(rightColumnX + padding, rightTextY));
		rightTextY += 10;

		graphics.DrawString(customer.Name, valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
		rightTextY += 9;

		if (!string.IsNullOrEmpty(customer.Address))
		{
			// Calculate actual address height dynamically
			int addressLines = Math.Max(1, customer.Address.Length / 50);
			float addressHeight = addressLines * 9;
			DrawWrappedText(graphics, customer.Address, valueFont, valueBrush,
				new RectangleF(rightColumnX + padding, rightTextY, columnWidth - 2 * padding, addressHeight + 5));
			rightTextY += addressHeight + 2;
		}

		if (!string.IsNullOrEmpty(customer.Phone))
		{
			graphics.DrawString($"Phone: {customer.Phone}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
			rightTextY += 9;
		}

		if (!string.IsNullOrEmpty(customer.Email))
		{
			graphics.DrawString($"Email: {customer.Email}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
			rightTextY += 9;
		}

		if (!string.IsNullOrEmpty(customer.GSTNo))
		{
			graphics.DrawString($"GSTIN: {customer.GSTNo}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
			rightTextY += 9;
		}

		// Calculate dynamic box height based on content
		float leftContentHeight = leftTextY - leftStartY;
		float rightContentHeight = rightTextY - rightStartY;
		float boxHeight = Math.Max(leftContentHeight, rightContentHeight) + padding;

		// No borders - removed for cleaner look
		// PdfPen boxPen = new(new PdfColor(200, 200, 200), 1f);
		// graphics.DrawRectangle(boxPen, new RectangleF(leftMargin, currentY, columnWidth, boxHeight));
		// graphics.DrawRectangle(boxPen, new RectangleF(rightColumnX, currentY, columnWidth, boxHeight));

		currentY += boxHeight + 8;

		// Invoice Details Row (Date & Time)
		graphics.DrawString($"Invoice Date: {invoiceData.TransactionDateTime:dd-MMM-yyyy hh:mm tt}", valueFont, valueBrush,
			new PointF(leftMargin, currentY));
		currentY += 15;

		return currentY;
	}

	/// <summary>
	/// Draw line items table
	/// </summary>
	private static float DrawLineItemsTable(PdfPage page, PdfDocument document, List<InvoiceLineItem> lineItems,
		float leftMargin, float rightMargin, float pageWidth, float startY)
	{
		PdfGrid pdfGrid = new();

		// Define columns (removed HSN/SAC)
		string[] columns = ["#", "Item Description", "Qty", "UOM", "Rate", "Disc %", "Taxable", "Tax %", "Tax Amt", "Total"];
		pdfGrid.Columns.Add(columns.Length);

		// Set column widths - optimized to fit within page
		float availableWidth = pageWidth - leftMargin - rightMargin;
		pdfGrid.Columns[0].Width = 18;  // #
		pdfGrid.Columns[1].Width = availableWidth * 0.28f;  // Item Description
		pdfGrid.Columns[2].Width = 32;  // Qty
		pdfGrid.Columns[3].Width = 32;  // UOM
		pdfGrid.Columns[4].Width = 45;  // Rate
		pdfGrid.Columns[5].Width = 35;  // Disc %
		pdfGrid.Columns[6].Width = 50;  // Taxable
		pdfGrid.Columns[7].Width = 35;  // Tax %
		pdfGrid.Columns[8].Width = 45;  // Tax Amt
		pdfGrid.Columns[9].Width = 55;  // Total

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
			headerRow.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f, PdfFontStyle.Bold);
			headerRow.Cells[i].Style.StringFormat = new PdfStringFormat
			{
				Alignment = PdfTextAlignment.Center,
				LineAlignment = PdfVerticalAlignment.Middle,
				WordWrap = PdfWordWrapType.Word
			};
			headerRow.Cells[i].Style.CellPadding = new PdfPaddings(1.5f, 1.5f, 1.5f, 1.5f);
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
			row.Cells[5].Value = item.DiscountPercent > 0 ? item.DiscountPercent.ToString("#,##0.00") : "-";
			row.Cells[6].Value = item.AfterDiscount.ToString("#,##0.00");
			row.Cells[7].Value = item.CGSTPercent + item.SGSTPercent + item.IGSTPercent > 0 ? (item.CGSTPercent + item.SGSTPercent + item.IGSTPercent).ToString("#,##0.00") : "-";
			row.Cells[8].Value = item.TotalTaxAmount > 0 ? item.TotalTaxAmount.ToString("#,##0.00") : "-";
			row.Cells[9].Value = item.Total.ToString("#,##0.00");

			// Cell styling
			for (int i = 0; i < columns.Length; i++)
			{
				row.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f);
				row.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(220, 220, 220), 0.5f);
				row.Cells[i].Style.CellPadding = new PdfPaddings(1.5f, 1.5f, 1.5f, 1.5f);

				// Right align numeric columns
				if (i >= 2 && i <= 9)
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

	/// <summary>
	/// Draw invoice summary (subtotal, taxes, total) on the right and remarks on the left
	/// </summary>
	private static float DrawSummaryAndRemarks(PdfGraphics graphics, InvoiceData invoiceData, float leftMargin, float pageWidth, float startY)
	{
		float currentY = startY;
		float summaryStartY = startY;
		float remarksStartY = startY;

		// Define column boundaries
		float remarksColumnWidth = pageWidth - 240; // Left side for remarks
		float summaryColumnX = pageWidth - 200; // Right side for summary
		float rightMargin = 20;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
		PdfStandardFont totalFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(80, 80, 80));
		PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

		// ===== LEFT SIDE: REMARKS =====
		if (!string.IsNullOrWhiteSpace(invoiceData.Remarks))
		{
			PdfStandardFont remarksLabelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
			PdfStandardFont remarksValueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
			PdfBrush remarksBrush = new PdfSolidBrush(new PdfColor(60, 60, 60));

			graphics.DrawString("Remarks:", remarksLabelFont, new PdfSolidBrush(new PdfColor(0, 0, 0)), new PointF(leftMargin, remarksStartY));
			remarksStartY += 12;

			// Calculate dynamic height based on text content
			float remarksBoxWidth = remarksColumnWidth - 30;
			float textWidth = remarksBoxWidth - 10; // Account for padding

			// Measure the text height with word wrapping
			PdfStringFormat format = new()
			{
				LineAlignment = PdfVerticalAlignment.Top,
				Alignment = PdfTextAlignment.Left,
				WordWrap = PdfWordWrapType.Word
			};

			SizeF textSize = remarksValueFont.MeasureString(invoiceData.Remarks, textWidth, format);
			float remarksBoxHeight = textSize.Height + 10; // Add padding (5px top + 5px bottom)

			// Ensure minimum height
			if (remarksBoxHeight < 30)
				remarksBoxHeight = 30;

			// No border - removed for cleaner look
			// PdfPen remarksBorderPen = new(new PdfColor(200, 200, 200), 0.5f);
			// graphics.DrawRectangle(remarksBorderPen, new RectangleF(leftMargin, remarksStartY, remarksBoxWidth, remarksBoxHeight));

			// Draw remarks text with padding (no box)
			RectangleF remarksTextRect = new(leftMargin + 5, remarksStartY + 5, remarksBoxWidth - 10, remarksBoxHeight - 10);
			DrawWrappedText(graphics, invoiceData.Remarks, remarksValueFont, remarksBrush, remarksTextRect);

			remarksStartY += remarksBoxHeight + 5;
		}

		// ===== RIGHT SIDE: SUMMARY =====
		// Subtotal
		graphics.DrawString("Subtotal:", labelFont, labelBrush, new PointF(summaryColumnX, summaryStartY));
		string subtotalText = $"{invoiceData.ItemsTotalAmount:N2}";
		SizeF subtotalSize = valueFont.MeasureString(subtotalText);
		graphics.DrawString(subtotalText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - subtotalSize.Width, summaryStartY));
		summaryStartY += 10;

		// Other charges
		if (invoiceData.OtherChargesAmount > 0)
		{
			string otherChargesLabel = invoiceData.OtherChargesPercent > 0
				? $"Other Charges ({invoiceData.OtherChargesPercent:N2}%):"
				: "Other Charges:";
			graphics.DrawString(otherChargesLabel, labelFont, labelBrush, new PointF(summaryColumnX, summaryStartY));
			string otherChargesText = $"{invoiceData.OtherChargesAmount:N2}";
			SizeF otherChargesSize = valueFont.MeasureString(otherChargesText);
			graphics.DrawString(otherChargesText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - otherChargesSize.Width, summaryStartY));
			summaryStartY += 10;
		}

		// Cash Discount (if available)
		if (invoiceData.CashDiscountAmount > 0)
		{
			string cashDiscountLabel = invoiceData.CashDiscountPercent > 0
				? $"Cash Discount ({invoiceData.CashDiscountPercent:N2}%):"
				: "Cash Discount:";
			graphics.DrawString(cashDiscountLabel, labelFont, labelBrush, new PointF(summaryColumnX, summaryStartY));
			string cashDiscountText = $"- {invoiceData.CashDiscountAmount:N2}";
			SizeF cashDiscountSize = valueFont.MeasureString(cashDiscountText);
			graphics.DrawString(cashDiscountText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - cashDiscountSize.Width, summaryStartY));
			summaryStartY += 10;
		}

		// Round off
		if (invoiceData.RoundOffAmount != 0)
		{
			graphics.DrawString("Round Off:", labelFont, labelBrush, new PointF(summaryColumnX, summaryStartY));
			string roundOffText = $"{(invoiceData.RoundOffAmount >= 0 ? "+" : "")} {invoiceData.RoundOffAmount:N2}";
			SizeF roundOffSize = valueFont.MeasureString(roundOffText);
			graphics.DrawString(roundOffText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - roundOffSize.Width, summaryStartY));
			summaryStartY += 10;
		}

		// Draw line above total
		PdfPen linePen = new(new PdfColor(59, 130, 246), 1f);
		graphics.DrawLine(linePen, new PointF(summaryColumnX - 10, summaryStartY), new PointF(pageWidth - 20, summaryStartY));
		summaryStartY += 4;

		// Total Amount
		PdfBrush totalBrush = new PdfSolidBrush(new PdfColor(59, 130, 246));
		graphics.DrawString("TOTAL:", totalFont, totalBrush, new PointF(summaryColumnX, summaryStartY));
		string totalText = $"{invoiceData.TotalAmount:N2}";
		SizeF totalSize = totalFont.MeasureString(totalText);
		graphics.DrawString(totalText, totalFont, totalBrush, new PointF(pageWidth - rightMargin - totalSize.Width, summaryStartY));
		summaryStartY += 15;

		// Return the maximum Y position of both columns
		currentY = Math.Max(remarksStartY, summaryStartY);
		return currentY;
	}

	/// <summary>
	/// Draw amount in words
	/// </summary>
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

	/// <summary>
	/// Draw remarks section
	/// </summary>
	/// <summary>
	/// Draw software branding footer
	/// </summary>
	private static void DrawSoftwareBrandingFooter(PdfGraphics graphics, float leftMargin, float pageWidth, float pageHeight)
	{
		float footerY = pageHeight - 20;

		PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Regular);
		PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));

		// Draw separator line
		PdfPen separatorPen = new(new PdfColor(200, 200, 200), 0.5f);
		graphics.DrawLine(separatorPen, new PointF(leftMargin, footerY - 5), new PointF(pageWidth - 20, footerY - 5));

		// Draw branding text centered
		string brandingText = "Generated from Prime Bakes - A Product of aadisoft.vercel.app";
		SizeF textSize = footerFont.MeasureString(brandingText);
		float textX = (pageWidth - textSize.Width) / 2;

		graphics.DrawString(brandingText, footerFont, footerBrush, new PointF(textX, footerY));
	}

	/// <summary>
	/// Draw terms and conditions
	/// </summary>
	private static float DrawTermsAndConditions(PdfGraphics graphics, string termsAndConditions, float leftMargin, float pageWidth, float startY)
	{
		if (string.IsNullOrEmpty(termsAndConditions))
			return startY;

		float currentY = startY;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Regular);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

		graphics.DrawString("Terms & Conditions:", labelFont, labelBrush, new PointF(leftMargin, currentY));
		currentY += 10;

		DrawWrappedText(graphics, termsAndConditions, valueFont, labelBrush,
			new RectangleF(leftMargin, currentY, pageWidth - 40, 40));

		// Calculate approximate height based on text length (more compact)
		int lineCount = Math.Max(1, termsAndConditions.Length / 100);
		currentY += Math.Min(lineCount * 10, 35);

		return currentY;
	}

	/// <summary>
	/// Draw invoice footer with bank details and signature
	/// </summary>
	private static void DrawInvoiceFooter(PdfGraphics graphics, CompanyModel company, PurchaseModel purchaseHeader,
		float leftMargin, float pageWidth, float pageHeight)
	{
		float footerY = pageHeight - 80;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Bold);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Regular);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

		// Authorized Signature (Right side)
		float signatureX = pageWidth - 150;
		float signatureY = pageHeight - 80;

		graphics.DrawString("For " + company.Name, valueFont, labelBrush, new PointF(signatureX, signatureY));
		signatureY += 30;

		// Signature line
		PdfPen signaturePen = new(new PdfColor(0, 0, 0), 0.5f);
		graphics.DrawLine(signaturePen, new PointF(signatureX, signatureY), new PointF(pageWidth - 20, signatureY));
		signatureY += 5;

		graphics.DrawString("Authorized Signatory", valueFont, labelBrush, new PointF(signatureX, signatureY));

		// Bottom footer
		float bottomY = pageHeight - 15;
		PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 6, PdfFontStyle.Italic);
		PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(120, 120, 120));

		string footerText = $"This is a computer-generated invoice | Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}";
		SizeF footerSize = footerFont.MeasureString(footerText);
		graphics.DrawString(footerText, footerFont, footerBrush,
			new PointF((pageWidth - footerSize.Width) / 2, bottomY));
	}

	/// <summary>
	/// Helper method to draw wrapped text within a rectangle
	/// </summary>
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

	/// <summary>
	/// Convert amount to words (Indian format)
	/// </summary>
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

			if (rupees >= 10000000) // Crores
			{
				words += ConvertNumberToWords(rupees / 10000000, ones, teens, tens) + " Crore ";
				rupees %= 10000000;
			}

			if (rupees >= 100000) // Lakhs
			{
				words += ConvertNumberToWords(rupees / 100000, ones, teens, tens) + " Lakh ";
				rupees %= 100000;
			}

			if (rupees >= 1000) // Thousands
			{
				words += ConvertNumberToWords(rupees / 1000, ones, teens, tens) + " Thousand ";
				rupees %= 1000;
			}

			if (rupees >= 100) // Hundreds
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
