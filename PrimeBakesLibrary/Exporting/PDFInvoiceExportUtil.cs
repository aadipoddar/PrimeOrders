using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using NumericWordsConversion;

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
		public string OrderTransactionNo { get; set; }
		public DateTime? OrderDateTime { get; set; }
		public decimal ItemsTotalAmount { get; set; }
		public decimal OtherChargesAmount { get; set; }
		public decimal OtherChargesPercent { get; set; }
		public decimal CashDiscountAmount { get; set; }
		public decimal CashDiscountPercent { get; set; }
		public decimal RoundOffAmount { get; set; }
		public decimal TotalAmount { get; set; }
		public decimal Cash { get; set; }
		public decimal Card { get; set; }
		public decimal UPI { get; set; }
		public decimal Credit { get; set; }
		public string Remarks { get; set; }
		public bool Status { get; set; } = true; // True = Active, False = Deleted
	}

	/// <summary>
	/// Column configuration for invoice line items table
	/// </summary>
	public class InvoiceColumnSetting
	{
		public string PropertyName { get; set; }
		public string DisplayName { get; set; }
		public float Width { get; set; }
		public PdfTextAlignment Alignment { get; set; } = PdfTextAlignment.Right;
		public string Format { get; set; }
		public bool ShowOnlyIfHasValue { get; set; } = false;

		public InvoiceColumnSetting(string propertyName, string displayName, float width,
			PdfTextAlignment alignment = PdfTextAlignment.Right, string format = null, bool showOnlyIfHasValue = false)
		{
			PropertyName = propertyName;
			DisplayName = displayName;
			Width = width;
			Alignment = alignment;
			Format = format;
			ShowOnlyIfHasValue = showOnlyIfHasValue;
		}
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
	/// <param name="billTo">Customer/Party information (Bill To)</param>
	/// <param name="logoPath">Optional: Path to company logo</param>
	/// <param name="invoiceType">Type of invoice (INVOICE, PURCHASE RETURN, SALES INVOICE, etc.)</param>
	/// <param name="outlet">Optional: Outlet/Location name</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportInvoiceToPdf(
		InvoiceData invoiceData,
		List<InvoiceLineItem> lineItems,
		CompanyModel company,
		LedgerModel billTo,
		string logoPath = null,
		string invoiceType = "INVOICE",
		string outlet = null,
		CustomerModel customer = null)
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
			currentY = DrawInvoiceTitle(graphics, invoiceType, invoiceData.TransactionNo, invoiceData.TransactionDateTime, leftMargin, pageWidth, currentY, outlet);

			// 2.5. Draw DELETED status badge if Status is false
			if (!invoiceData.Status)
			{
				currentY = DrawDeletedStatusBadge(graphics, pageWidth, currentY);
			}

			// 3. Company and Customer Information (Two Columns)
			currentY = DrawCompanyAndCustomerInfo(graphics, company, billTo, customer, invoiceData, leftMargin, pageWidth, currentY);

			// 4. Line Items Table
			PdfGridLayoutResult gridResult = DrawLineItemsTableWithResult(page, pdfDocument, lineItems, leftMargin, rightMargin, pageWidth, currentY);

			// Get the last page where the grid ended
			PdfPage lastPage = gridResult.Page;
			PdfGraphics lastPageGraphics = lastPage.Graphics;
			currentY = gridResult.Bounds.Bottom + 8;

			// 5. Summary Section with Remarks (two-column layout on the last page)
			currentY = DrawSummaryAndRemarks(lastPageGraphics, invoiceData, leftMargin, pageWidth, currentY);

			// 6. Amount in Words and Payment Methods side by side (on the last page)
			float amountInWordsStartY = currentY;
			float amountInWordsEndY = DrawAmountInWords(lastPageGraphics, invoiceData.TotalAmount, leftMargin, pageWidth, currentY);
			float paymentMethodsEndY = DrawPaymentMethods(lastPageGraphics, invoiceData, leftMargin, pageWidth, amountInWordsStartY);
			currentY = Math.Max(amountInWordsEndY, paymentMethodsEndY);

			// 7. Software Branding Footer (on all pages)
			AddBrandingFooter(pdfDocument);

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
	private static float DrawInvoiceTitle(PdfGraphics graphics, string invoiceType, string invoiceNumber, DateTime transactionDateTime, float leftMargin, float pageWidth, float startY, string outlet = null)
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

		// Draw Outlet label and Invoice Date on same line
		if (!string.IsNullOrWhiteSpace(outlet))
		{
			PdfStandardFont outletFont = new(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
			PdfBrush outletBrush = new PdfSolidBrush(new PdfColor(100, 100, 100));
			string outletText = $"Outlet: {outlet}";
			graphics.DrawString(outletText, outletFont, outletBrush, new PointF(leftMargin, currentY));
		}

		// Invoice Date (right aligned, on same line as outlet)
		PdfStandardFont dateFont = new(PdfFontFamily.Helvetica, 10);
		string invoiceDateText = $"Invoice Date: {transactionDateTime:dd-MMM-yyyy hh:mm tt}";
		SizeF dateSize = dateFont.MeasureString(invoiceDateText);
		graphics.DrawString(invoiceDateText, dateFont, PdfBrushes.Black,
			new PointF(pageWidth - 20 - dateSize.Width, currentY));

		currentY += 18;

		return currentY;
	}

	/// <summary>
	/// Draw DELETED status badge when invoice is deleted
	/// </summary>
	private static float DrawDeletedStatusBadge(PdfGraphics graphics, float pageWidth, float startY)
	{
		// Draw a prominent red badge/box with "DELETED" text
		float badgeWidth = 120;
		float badgeHeight = 30;
		float badgeX = (pageWidth - badgeWidth) / 2; // Center horizontally
		float badgeY = startY + 5;

		// Draw red background rectangle with rounded corners
		PdfBrush redBrush = new PdfSolidBrush(new PdfColor(220, 38, 38)); // Red-600
		PdfPen redPen = new(new PdfColor(220, 38, 38), 2f);
		graphics.DrawRectangle(redBrush, new RectangleF(badgeX, badgeY, badgeWidth, badgeHeight));

		// Draw white "DELETED" text centered in the badge
		PdfStandardFont badgeFont = new(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
		PdfBrush whiteBrush = new PdfSolidBrush(new PdfColor(255, 255, 255));
		string deletedText = "DELETED";
		SizeF textSize = badgeFont.MeasureString(deletedText);
		float textX = badgeX + (badgeWidth - textSize.Width) / 2;
		float textY = badgeY + (badgeHeight - textSize.Height) / 2;
		graphics.DrawString(deletedText, badgeFont, whiteBrush, new PointF(textX, textY));

		// Return updated Y position (badge Y + badge height + spacing)
		return badgeY + badgeHeight + 10;
	}

	/// <summary>
	/// Draw company and customer information in two columns
	/// </summary>
	private static float DrawCompanyAndCustomerInfo(PdfGraphics graphics, CompanyModel company,
		LedgerModel billTo, CustomerModel customer, InvoiceData invoiceData, float leftMargin, float pageWidth, float startY)
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

		// Right Column - To (Customer/Party) - skip entire section if null
		if (billTo != null)
		{
			graphics.DrawString("BILL TO:", labelFont, labelBrush, new PointF(rightColumnX + padding, rightTextY));
			rightTextY += 10;

			graphics.DrawString(billTo.Name, valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
			rightTextY += 9;

			if (!string.IsNullOrEmpty(billTo.Address))
			{
				// Calculate actual address height dynamically
				int addressLines = Math.Max(1, billTo.Address.Length / 50);
				float addressHeight = addressLines * 9;
				DrawWrappedText(graphics, billTo.Address, valueFont, valueBrush,
					new RectangleF(rightColumnX + padding, rightTextY, columnWidth - 2 * padding, addressHeight + 5));
				rightTextY += addressHeight + 2;
			}

			if (!string.IsNullOrEmpty(billTo.Phone))
			{
				graphics.DrawString($"Phone: {billTo.Phone}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
				rightTextY += 9;
			}

			if (!string.IsNullOrEmpty(billTo.Email))
			{
				graphics.DrawString($"Email: {billTo.Email}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
				rightTextY += 9;
			}

			if (!string.IsNullOrEmpty(billTo.GSTNo))
			{
				graphics.DrawString($"GSTIN: {billTo.GSTNo}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
				rightTextY += 9;
			}
		}

		// Customer Information (if present) - displayed after BILL TO section or standalone
		if (customer != null && (!string.IsNullOrEmpty(customer.Name) || !string.IsNullOrEmpty(customer.Number)))
		{
			// Add spacing if billTo section exists
			if (billTo != null)
				rightTextY += 5;

			graphics.DrawString("CUSTOMER:", labelFont, labelBrush, new PointF(rightColumnX + padding, rightTextY));
			rightTextY += 10;

			if (!string.IsNullOrEmpty(customer.Name))
			{
				graphics.DrawString(customer.Name, valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
				rightTextY += 9;
			}

			if (!string.IsNullOrEmpty(customer.Number))
			{
				graphics.DrawString($"Phone: {customer.Number}", valueFont, valueBrush, new PointF(rightColumnX + padding, rightTextY));
				rightTextY += 9;
			}
		}

		// Calculate dynamic box height based on content
		float leftContentHeight = leftTextY - leftStartY;
		float rightContentHeight = rightTextY - rightStartY;
		float boxHeight = Math.Max(leftContentHeight, rightContentHeight) + padding;

		currentY += boxHeight + 8;

		// Order Details (if present)
		if (!string.IsNullOrWhiteSpace(invoiceData.OrderTransactionNo))
		{
			graphics.DrawString($"Order No: {invoiceData.OrderTransactionNo}", valueFont, valueBrush,
				new PointF(leftMargin, currentY));
			currentY += 12;

			if (invoiceData.OrderDateTime.HasValue)
			{
				graphics.DrawString($"Order Date: {invoiceData.OrderDateTime.Value:dd-MMM-yyyy hh:mm tt}", valueFont, valueBrush,
					new PointF(leftMargin, currentY));
				currentY += 15;
			}
		}

		return currentY;
	}

	/// <summary>
	/// Draw line items table
	/// </summary>
	private static PdfGridLayoutResult DrawLineItemsTableWithResult(PdfPage page, PdfDocument document, List<InvoiceLineItem> lineItems,
		float leftMargin, float rightMargin, float pageWidth, float startY)
	{
		PdfGrid pdfGrid = new();

		// Check if any item has discount, tax, rate, total, or UOM
		bool hasDiscount = lineItems.Any(i => i.DiscountPercent > 0);
		bool hasTax = lineItems.Any(i => i.CGSTPercent > 0 || i.SGSTPercent > 0 || i.IGSTPercent > 0 || i.TotalTaxAmount > 0);
		bool hasUOM = lineItems.Any(i => !string.IsNullOrWhiteSpace(i.UnitOfMeasurement));
		bool hasRate = lineItems.Any(i => i.Rate > 0);
		bool hasTotal = lineItems.Any(i => i.Total > 0);

		// Define column settings
		var columnSettings = GetInvoiceColumnSettings(hasDiscount, hasTax, hasUOM, hasRate, hasTotal);

		// Calculate available width and adjust description column
		float availableWidth = pageWidth - leftMargin - rightMargin;
		float fixedWidths = columnSettings.Where(c => c.PropertyName != "ItemName").Sum(c => c.Width);
		var descColumn = columnSettings.First(c => c.PropertyName == "ItemName");
		descColumn.Width = availableWidth - fixedWidths;

		// Add columns to grid
		pdfGrid.Columns.Add(columnSettings.Count);
		for (int i = 0; i < columnSettings.Count; i++)
		{
			pdfGrid.Columns[i].Width = columnSettings[i].Width;
		}

		pdfGrid.Style.AllowHorizontalOverflow = false;
		pdfGrid.RepeatHeader = true;
		pdfGrid.AllowRowBreakAcrossPages = true;

		// Add header row
		PdfGridRow headerRow = pdfGrid.Headers.Add(1)[0];
		for (int i = 0; i < columnSettings.Count; i++)
		{
			headerRow.Cells[i].Value = columnSettings[i].DisplayName;
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

			// Populate cells based on column settings
			for (int i = 0; i < columnSettings.Count; i++)
			{
				var column = columnSettings[i];
				string cellValue = GetCellValue(item, column, rowNumber);
				row.Cells[i].Value = cellValue;

				// Apply styling
				row.Cells[i].Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 6.5f);
				row.Cells[i].Style.Borders.All = new PdfPen(new PdfColor(220, 220, 220), 0.5f);
				row.Cells[i].Style.CellPadding = new PdfPaddings(1.5f, 1.5f, 1.5f, 1.5f);
				row.Cells[i].Style.StringFormat = new PdfStringFormat
				{
					Alignment = column.Alignment,
					LineAlignment = PdfVerticalAlignment.Middle,
					WordWrap = PdfWordWrapType.Word
				};
			}

			// Alternating row colors
			if (rowNumber % 2 == 0)
			{
				for (int i = 0; i < columnSettings.Count; i++)
				{
					row.Cells[i].Style.BackgroundBrush = new PdfSolidBrush(new PdfColor(249, 250, 251));
				}
			}

			rowNumber++;
		}

		// Draw grid with improved pagination layout
		// Reserve more space for footer to accommodate summary, payment methods, and amount in words
		float footerHeight = 130; // Increased to prevent content from being cut off
		float pageHeight = page.GetClientSize().Height;

		// Calculate maximum available height for the grid
		float maxGridHeight = pageHeight - startY - footerHeight;

		// Ensure minimum space is available for grid
		if (maxGridHeight < 100)
		{
			// If not enough space on current page, start on next page
			page = document.Pages.Add();
			startY = 20; // Reset to top margin on new page
			maxGridHeight = pageHeight - startY - footerHeight;
		}

		PdfGridLayoutFormat layoutFormat = new()
		{
			Layout = PdfLayoutType.Paginate,
			Break = PdfLayoutBreakType.FitPage
		};

		PdfGridLayoutResult result = pdfGrid.Draw(page, new RectangleF(leftMargin, startY, pageWidth - leftMargin - rightMargin, maxGridHeight), layoutFormat);

		// Return the layout result so caller can get the last page
		return result;
	}

	/// <summary>
	/// Get invoice column settings based on whether items have discount/tax/UOM
	/// </summary>
	private static List<InvoiceColumnSetting> GetInvoiceColumnSettings(bool hasDiscount, bool hasTax, bool hasUOM, bool hasRate, bool hasTotal)
	{
		var settings = new List<InvoiceColumnSetting>
		{
			new("RowNumber", "#", 25, PdfTextAlignment.Center),
			new("ItemName", "Item Description", 0, PdfTextAlignment.Left), // Width calculated dynamically
			new("Quantity", "Qty", 40, PdfTextAlignment.Right, "#,##0.00")
		};

		if (hasUOM)
			settings.Add(new("UnitOfMeasurement", "UOM", 40, PdfTextAlignment.Center, null, true));

		if (hasRate)
			settings.Add(new("Rate", "Rate", 55, PdfTextAlignment.Right, "#,##0.00"));

		if (hasDiscount)
			settings.Add(new("DiscountPercent", "Disc %", 45, PdfTextAlignment.Right, "#,##0.00", true));

		if (hasTax)
		{
			settings.Add(new("AfterDiscount", "Taxable", 60, PdfTextAlignment.Right, "#,##0.00"));
			settings.Add(new("TotalTaxPercent", "Tax %", 45, PdfTextAlignment.Right, "#,##0.00", true));
			settings.Add(new("TotalTaxAmount", "Tax Amt", 55, PdfTextAlignment.Right, "#,##0.00", true));
		}

		if (hasTotal)
			settings.Add(new("Total", "Total", 65, PdfTextAlignment.Right, "#,##0.00"));

		return settings;
	}

	/// <summary>
	/// Get cell value based on column property name
	/// </summary>
	private static string GetCellValue(InvoiceLineItem item, InvoiceColumnSetting column, int rowNumber)
	{
		string value = column.PropertyName switch
		{
			"RowNumber" => rowNumber.ToString(),
			"ItemName" => item.ItemName,
			"Quantity" => item.Quantity.ToString(column.Format ?? "#,##0.00"),
			"UnitOfMeasurement" => item.UnitOfMeasurement,
			"Rate" => item.Rate.ToString(column.Format ?? "#,##0.00"),
			"DiscountPercent" => item.DiscountPercent > 0 ? item.DiscountPercent.ToString(column.Format ?? "#,##0.00") : "-",
			"AfterDiscount" => item.AfterDiscount.ToString(column.Format ?? "#,##0.00"),
			"TotalTaxPercent" => (item.CGSTPercent + item.SGSTPercent + item.IGSTPercent) > 0
				? (item.CGSTPercent + item.SGSTPercent + item.IGSTPercent).ToString(column.Format ?? "#,##0.00")
				: "-",
			"TotalTaxAmount" => item.TotalTaxAmount > 0 ? item.TotalTaxAmount.ToString(column.Format ?? "#,##0.00") : "-",
			"Total" => item.Total.ToString(column.Format ?? "#,##0.00"),
			_ => ""
		};

		return value;
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
		string subtotalText = invoiceData.ItemsTotalAmount.FormatIndianCurrency();
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
			string otherChargesText = invoiceData.OtherChargesAmount.FormatIndianCurrency();
			SizeF otherChargesSize = valueFont.MeasureString(otherChargesText);
			graphics.DrawString(otherChargesText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - otherChargesSize.Width, summaryStartY));
			summaryStartY += 10;
		}

		// Cash Discount (if available)
		if (invoiceData.CashDiscountAmount > 0)
		{
			string cashDiscountLabel = invoiceData.CashDiscountPercent > 0
				? $"Discount ({invoiceData.CashDiscountPercent:N2}%):"
				: "Discount:";
			graphics.DrawString(cashDiscountLabel, labelFont, labelBrush, new PointF(summaryColumnX, summaryStartY));
			string cashDiscountText = $"- {invoiceData.CashDiscountAmount.FormatIndianCurrency()}";
			SizeF cashDiscountSize = valueFont.MeasureString(cashDiscountText);
			graphics.DrawString(cashDiscountText, valueFont, valueBrush, new PointF(pageWidth - rightMargin - cashDiscountSize.Width, summaryStartY));
			summaryStartY += 10;
		}

		// Round off
		if (invoiceData.RoundOffAmount != 0)
		{
			graphics.DrawString("Round Off:", labelFont, labelBrush, new PointF(summaryColumnX, summaryStartY));
			string roundOffText = $"{(invoiceData.RoundOffAmount >= 0 ? "+" : "")} {invoiceData.RoundOffAmount.FormatIndianCurrency()}";
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
		string totalText = invoiceData.TotalAmount.FormatIndianCurrency();
		SizeF totalSize = totalFont.MeasureString(totalText);
		graphics.DrawString(totalText, totalFont, totalBrush, new PointF(pageWidth - rightMargin - totalSize.Width, summaryStartY));
		summaryStartY += 15;

		// Return the maximum Y position of both columns
		currentY = Math.Max(remarksStartY, summaryStartY);
		return currentY;
	}

	/// <summary>
	/// Draw amount in words and payment methods side by side
	/// </summary>
	private static float DrawAmountInWords(PdfGraphics graphics, decimal amount, float leftMargin, float pageWidth, float startY)
	{
		float currentY = startY;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Italic);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));

		string amountInWords = ConvertAmountToWords(amount);

		// Left column width for amount in words
		float leftColumnWidth = (pageWidth - 40) * 0.6f; // 60% for amount in words

		graphics.DrawString("Amount in Words:", labelFont, labelBrush, new PointF(leftMargin, currentY));
		currentY += 10;

		DrawWrappedText(graphics, amountInWords, valueFont, labelBrush,
			new RectangleF(leftMargin, currentY, leftColumnWidth, 20));
		currentY += 18;

		return currentY;
	}

	/// <summary>
	/// Draw payment methods breakdown
	/// </summary>
	private static float DrawPaymentMethods(PdfGraphics graphics, InvoiceData invoiceData, float leftMargin, float pageWidth, float startY)
	{
		// Check if any payment method has value
		bool hasPayments = invoiceData.Cash > 0 || invoiceData.Card > 0 || invoiceData.UPI > 0 || invoiceData.Credit > 0;
		if (!hasPayments)
			return startY;

		PdfStandardFont labelFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
		PdfStandardFont valueFont = new(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular);
		PdfBrush labelBrush = new PdfSolidBrush(new PdfColor(0, 0, 0));
		PdfBrush valueBrush = new PdfSolidBrush(new PdfColor(37, 99, 235)); // Blue for amounts

		// Position payment methods on the right side (40% of width)
		float rightColumnX = leftMargin + (pageWidth - 40) * 0.6f + 20; // Start after amount in words
		float currentY = startY;

		graphics.DrawString("Payment Methods:", labelFont, labelBrush, new PointF(rightColumnX, currentY));
		currentY += 12;

		// Draw each payment method that has a value > 0
		float methodX = rightColumnX + 10;
		float amountX = rightColumnX + 80;

		if (invoiceData.Cash > 0)
		{
			graphics.DrawString("Cash:", valueFont, labelBrush, new PointF(methodX, currentY));
			graphics.DrawString(invoiceData.Cash.FormatIndianCurrency(), valueFont, valueBrush, new PointF(amountX, currentY));
			currentY += 10;
		}

		if (invoiceData.Card > 0)
		{
			graphics.DrawString("Card:", valueFont, labelBrush, new PointF(methodX, currentY));
			graphics.DrawString(invoiceData.Card.FormatIndianCurrency(), valueFont, valueBrush, new PointF(amountX, currentY));
			currentY += 10;
		}

		if (invoiceData.UPI > 0)
		{
			graphics.DrawString("UPI:", valueFont, labelBrush, new PointF(methodX, currentY));
			graphics.DrawString(invoiceData.UPI.FormatIndianCurrency(), valueFont, valueBrush, new PointF(amountX, currentY));
			currentY += 10;
		}

		if (invoiceData.Credit > 0)
		{
			graphics.DrawString("Credit:", valueFont, labelBrush, new PointF(methodX, currentY));
			graphics.DrawString(invoiceData.Credit.FormatIndianCurrency(), valueFont, valueBrush, new PointF(amountX, currentY));
			currentY += 10;
		}

		currentY += 5; // Add spacing after payment methods
		return currentY;
	}

	/// <summary>
	/// Add branding footer to all pages
	/// </summary>
	private static void AddBrandingFooter(PdfDocument document)
	{
		try
		{
			// Create footer template
			PdfPageTemplateElement footer = new(new RectangleF(0, 0, document.Pages[0].GetClientSize().Width, 25));

			PdfStandardFont footerFont = new(PdfFontFamily.Helvetica, 7, PdfFontStyle.Italic);
			PdfBrush footerBrush = new PdfSolidBrush(new PdfColor(107, 114, 128)); // Gray

			// Left: AadiSoft branding
			string branding = $"© {DateTime.Now.Year} A Product By aadisoft.vercel.app";
			footer.Graphics.DrawString(branding, footerFont, footerBrush, new PointF(15, 5));

			// Center: Export date
			string exportDate = $"Exported on: {DateTime.Now:dd-MMM-yyyy hh:mm tt}";
			SizeF exportDateSize = footerFont.MeasureString(exportDate);
			float centerX = (document.Pages[0].GetClientSize().Width - exportDateSize.Width) / 2;
			footer.Graphics.DrawString(exportDate, footerFont, footerBrush, new PointF(centerX, 5));

			// Right: Page numbers
			PdfPageNumberField pageNumber = new();
			PdfPageCountField pageCount = new();
			PdfCompositeField pageInfo = new(
				footerFont,
				footerBrush,
				"Page {0} of {1}",
				pageNumber,
				pageCount);

			string pageText = "Page 999 of 999"; // Max width for alignment
			SizeF pageInfoSize = footerFont.MeasureString(pageText);
			float rightX = document.Pages[0].GetClientSize().Width - pageInfoSize.Width - 15;
			pageInfo.Draw(footer.Graphics, new PointF(rightX, 5));

			// Add footer to document
			document.Template.Bottom = footer;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adding branding footer: {ex.Message}");
		}
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
			// Use NumericWordsConversion package for Indian currency
			var converter = new CurrencyWordsConverter(new CurrencyWordsConversionOptions
			{
				Culture = Culture.Hindi,
				CurrencyUnit = "Rupee",
				SubCurrencyUnit = "Paise",
				EndOfWordsMarker = "Only"
			});

			string words = converter.ToWords(amount);
			return words;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error converting amount to words: {ex.Message}");
			return "Amount in Words Not Available";
		}
	}

	#endregion
}
