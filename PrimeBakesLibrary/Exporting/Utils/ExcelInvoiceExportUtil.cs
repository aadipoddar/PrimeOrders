using NumericWordsConversion;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Drawing;
using Syncfusion.XlsIO;

namespace PrimeBakesLibrary.Exporting.Utils;

/// <summary>
/// Professional Invoice Excel export utility - SAP-style professional invoices
/// </summary>
public static class ExcelInvoiceExportUtil
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
		public double Width { get; set; }
		public ExcelHAlign Alignment { get; set; } = ExcelHAlign.HAlignRight;
		public string Format { get; set; }
		public bool ShowOnlyIfHasValue { get; set; } = false;

		public InvoiceColumnSetting(string propertyName, string displayName, double width,
			ExcelHAlign alignment = ExcelHAlign.HAlignRight, string format = null, bool showOnlyIfHasValue = false)
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

	/// <summary>
	/// Accounting voucher line item for ledger entries
	/// </summary>
	public class AccountingLineItem
	{
		public int LedgerId { get; set; }
		public string LedgerName { get; set; }
		public string ReferenceNo { get; set; }
		public string ReferenceType { get; set; }
		public decimal? Debit { get; set; }
		public decimal? Credit { get; set; }
		public string Remarks { get; set; }
	}

	#endregion

	#region Color Definitions

	private static readonly Color PrimaryBlue = Color.FromArgb(59, 130, 246);
	private static readonly Color HeaderBackground = Color.FromArgb(241, 245, 249);
	private static readonly Color BorderColor = Color.FromArgb(203, 213, 225);
	private static readonly Color AlternateRowColor = Color.FromArgb(249, 250, 251);
	private static readonly Color TotalRowBackground = Color.FromArgb(239, 246, 255);
	private static readonly Color DeletedBadgeColor = Color.FromArgb(220, 38, 38);
	private static readonly Color SuccessColor = Color.FromArgb(16, 185, 129);

	#endregion

	#region Public Methods

	/// <summary>
	/// Export invoice to Excel with professional layout (unified method for all transaction types)
	/// </summary>
	public static async Task<MemoryStream> ExportInvoiceToExcel(
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
			using ExcelEngine excelEngine = new();
			IApplication application = excelEngine.Excel;
			application.DefaultVersion = ExcelVersion.Xlsx;

			IWorkbook workbook = application.Workbooks.Create(1);
			IWorksheet worksheet = workbook.Worksheets[0];
			worksheet.Name = "Invoice";

			// Set document properties
			workbook.BuiltInDocumentProperties.Title = $"{invoiceType} - {invoiceData.TransactionNo}";
			workbook.BuiltInDocumentProperties.Subject = invoiceType;
			workbook.BuiltInDocumentProperties.Author = "Prime Bakes";

			int currentRow = 1;

			// 1. Header Section with Logo and Company Name
			currentRow = await DrawInvoiceHeader(worksheet, company, logoPath, currentRow);

			// 2. Invoice Type and Number
			currentRow = DrawInvoiceTitle(worksheet, invoiceType, invoiceData.TransactionNo, invoiceData.TransactionDateTime, outlet, currentRow);

			// 2.5. Draw DELETED status badge if Status is false
			if (!invoiceData.Status)
				currentRow = DrawDeletedStatusBadge(worksheet, currentRow);

			// 3. Company and Customer Information (Two Columns)
			currentRow = DrawCompanyAndCustomerInfo(worksheet, company, billTo, customer, invoiceData, currentRow);

			// 4. Line Items Table
			currentRow = DrawLineItemsTable(worksheet, lineItems, currentRow);

			// 5. Summary Section with Remarks
			currentRow = DrawSummaryAndRemarks(worksheet, invoiceData, currentRow);

			// 6. Amount in Words and Payment Methods
			currentRow = DrawAmountInWordsAndPayment(worksheet, invoiceData, currentRow);

			// 7. Branding Footer
			currentRow = await DrawBrandingFooter(worksheet, currentRow);

			// Apply final formatting
			ApplyFinalFormatting(worksheet);

			workbook.SaveAs(ms);
			ms.Position = 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting invoice to Excel: {ex.Message}");
			throw;
		}

		return ms;
	}

	/// <summary>
	/// Export accounting voucher to Excel with specialized layout for ledger entries
	/// </summary>
	public static async Task<MemoryStream> ExportAccountingVoucherToExcel(
		InvoiceData invoiceData,
		List<AccountingLineItem> accountingItems,
		CompanyModel company,
		string logoPath = null,
		string invoiceType = "ACCOUNTING VOUCHER")
	{
		MemoryStream ms = new();

		try
		{
			using ExcelEngine excelEngine = new();
			IApplication application = excelEngine.Excel;
			application.DefaultVersion = ExcelVersion.Xlsx;

			IWorkbook workbook = application.Workbooks.Create(1);
			IWorksheet worksheet = workbook.Worksheets[0];
			worksheet.Name = "Voucher";

			workbook.BuiltInDocumentProperties.Title = $"{invoiceType} - {invoiceData.TransactionNo}";
			workbook.BuiltInDocumentProperties.Subject = invoiceType;
			workbook.BuiltInDocumentProperties.Author = "Prime Bakes";

			int currentRow = 1;

			// 1. Header Section
			currentRow = await DrawInvoiceHeader(worksheet, company, logoPath, currentRow);

			// 2. Invoice Type and Number
			currentRow = DrawInvoiceTitle(worksheet, invoiceType, invoiceData.TransactionNo, invoiceData.TransactionDateTime, null, currentRow);

			// 2.5. Draw DELETED status badge if Status is false
			if (!invoiceData.Status)
				currentRow = DrawDeletedStatusBadge(worksheet, currentRow);

			// 3. Company Information Only
			currentRow = DrawCompanyInfoOnly(worksheet, company, currentRow);

			// 4. Accounting Line Items Table
			currentRow = DrawAccountingLineItemsTable(worksheet, accountingItems, currentRow);

			// 5. Accounting Summary
			currentRow = DrawAccountingSummary(worksheet, accountingItems, invoiceData, currentRow);

			// 6. Branding Footer
			currentRow = await DrawBrandingFooter(worksheet, currentRow);

			// Apply final formatting
			ApplyFinalFormatting(worksheet);

			workbook.SaveAs(ms);
			ms.Position = 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting accounting voucher to Excel: {ex.Message}");
			throw;
		}

		return ms;
	}

	#endregion

	#region Private Helper Methods

	/// <summary>
	/// Draw invoice header with logo and company name
	/// </summary>
	private static async Task<int> DrawInvoiceHeader(IWorksheet worksheet, CompanyModel company, string logoPath, int startRow)
	{
		int currentRow = startRow;

		// Try to load and insert logo
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
				IPictureShape logo = worksheet.Pictures.AddPicture(currentRow, 4, imageStream); // Column 4 for centering
				
				// Calculate logo dimensions maintaining aspect ratio
				double maxLogoHeight = 50; // Slightly bigger for better visibility
				double originalWidth = logo.Width;
				double originalHeight = logo.Height;
				double aspectRatio = originalWidth / originalHeight;
				
				// Scale proportionally based on max height
				logo.Height = (int)maxLogoHeight;
				logo.Width = (int)(maxLogoHeight * aspectRatio);
				
				currentRow += 4;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Logo loading failed: {ex.Message}");
		}

		// Draw separator line (using bottom border on merged cells)
		worksheet.Range[currentRow, 1, currentRow, 10].Merge();
		worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Medium;
		worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Blue;
		currentRow++;

		return currentRow;
	}

	/// <summary>
	/// Draw invoice type and number
	/// </summary>
	private static int DrawInvoiceTitle(IWorksheet worksheet, string invoiceType, string invoiceNumber, DateTime transactionDateTime, string outlet, int startRow)
	{
		int currentRow = startRow;

		// Row 1: Invoice Type (left) and Invoice Number (right)
		worksheet.Range[currentRow, 1, currentRow, 5].Merge();
		worksheet.Range[currentRow, 1].Text = invoiceType.ToUpper();
		worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
		worksheet.Range[currentRow, 1].CellStyle.Font.Size = 14;
		worksheet.Range[currentRow, 1].CellStyle.Font.RGBColor = PrimaryBlue;

		worksheet.Range[currentRow, 6, currentRow, 10].Merge();
		worksheet.Range[currentRow, 6].Text = $"Invoice #: {invoiceNumber}";
		worksheet.Range[currentRow, 6].CellStyle.Font.Bold = true;
		worksheet.Range[currentRow, 6].CellStyle.Font.Size = 12;
		worksheet.Range[currentRow, 6].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		currentRow++;

		// Row 2: Outlet (left, gray color like PDF) and Date (right)
		if (!string.IsNullOrWhiteSpace(outlet))
		{
			worksheet.Range[currentRow, 1, currentRow, 5].Merge();
			worksheet.Range[currentRow, 1].Text = $"Outlet: {outlet}";
			worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
			worksheet.Range[currentRow, 1].CellStyle.Font.Size = 10;
			worksheet.Range[currentRow, 1].CellStyle.Font.RGBColor = Color.FromArgb(100, 100, 100); // Gray like PDF
		}

		worksheet.Range[currentRow, 6, currentRow, 10].Merge();
		worksheet.Range[currentRow, 6].Text = $"Invoice Date: {transactionDateTime:dd-MMM-yyyy hh:mm tt}";
		worksheet.Range[currentRow, 6].CellStyle.Font.Size = 10;
		worksheet.Range[currentRow, 6].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		currentRow += 2;

		return currentRow;
	}

	/// <summary>
	/// Draw DELETED status badge
	/// </summary>
	private static int DrawDeletedStatusBadge(IWorksheet worksheet, int startRow)
	{
		int currentRow = startRow;

		worksheet.Range[currentRow, 4, currentRow, 6].Merge();
		worksheet.Range[currentRow, 4].Text = "DELETED";
		worksheet.Range[currentRow, 4].CellStyle.Font.Bold = true;
		worksheet.Range[currentRow, 4].CellStyle.Font.Size = 14;
		worksheet.Range[currentRow, 4].CellStyle.Font.Color = ExcelKnownColors.White;
		worksheet.Range[currentRow, 4].CellStyle.Color = DeletedBadgeColor;
		worksheet.Range[currentRow, 4].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		worksheet.Range[currentRow, 4].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
		worksheet.Range[currentRow, 4].RowHeight = 25;
		currentRow += 2;

		return currentRow;
	}

	/// <summary>
	/// Draw company and customer information in two columns
	/// </summary>
	private static int DrawCompanyAndCustomerInfo(IWorksheet worksheet, CompanyModel company, LedgerModel billTo, CustomerModel customer, InvoiceData invoiceData, int startRow)
	{
		int leftRow = startRow;
		int rightRow = startRow;

		// FROM Section (Left Column)
		worksheet.Range[leftRow, 1].Text = "FROM:";
		worksheet.Range[leftRow, 1].CellStyle.Font.Bold = true;
		worksheet.Range[leftRow, 1].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
		leftRow++;

		// BILL TO Section (Right Column)
		if (billTo != null)
		{
			worksheet.Range[rightRow, 6].Text = "BILL TO:";
			worksheet.Range[rightRow, 6].CellStyle.Font.Bold = true;
			worksheet.Range[rightRow, 6].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
			rightRow++;
		}

		// Company Name (Left)
		worksheet.Range[leftRow, 1, leftRow, 4].Merge();
		worksheet.Range[leftRow, 1].Text = company.Name;
		worksheet.Range[leftRow, 1].CellStyle.Font.Bold = true;
		leftRow++;

		// Bill To Name (Right)
		if (billTo != null)
		{
			worksheet.Range[rightRow, 6, rightRow, 10].Merge();
			worksheet.Range[rightRow, 6].Text = billTo.Name;
			worksheet.Range[rightRow, 6].CellStyle.Font.Bold = true;
			rightRow++;
		}

		// Company Address (Left)
		if (!string.IsNullOrEmpty(company.Address))
		{
			worksheet.Range[leftRow, 1, leftRow, 4].Merge();
			worksheet.Range[leftRow, 1].Text = company.Address;
			worksheet.Range[leftRow, 1].WrapText = true;
			leftRow++;
		}

		// Bill To Address (Right)
		if (billTo != null && !string.IsNullOrEmpty(billTo.Address))
		{
			worksheet.Range[rightRow, 6, rightRow, 10].Merge();
			worksheet.Range[rightRow, 6].Text = billTo.Address;
			worksheet.Range[rightRow, 6].WrapText = true;
			rightRow++;
		}

		// Company Phone (Left)
		if (!string.IsNullOrEmpty(company.Phone))
		{
			worksheet.Range[leftRow, 1, leftRow, 4].Merge();
			worksheet.Range[leftRow, 1].Text = $"Phone: {company.Phone}";
			leftRow++;
		}

		// Bill To Phone (Right)
		if (billTo != null && !string.IsNullOrEmpty(billTo.Phone))
		{
			worksheet.Range[rightRow, 6, rightRow, 10].Merge();
			worksheet.Range[rightRow, 6].Text = $"Phone: {billTo.Phone}";
			rightRow++;
		}

		// Company Email (Left)
		if (!string.IsNullOrEmpty(company.Email))
		{
			worksheet.Range[leftRow, 1, leftRow, 4].Merge();
			worksheet.Range[leftRow, 1].Text = $"Email: {company.Email}";
			leftRow++;
		}

		// Bill To Email (Right)
		if (billTo != null && !string.IsNullOrEmpty(billTo.Email))
		{
			worksheet.Range[rightRow, 6, rightRow, 10].Merge();
			worksheet.Range[rightRow, 6].Text = $"Email: {billTo.Email}";
			rightRow++;
		}

		// Company GST (Left)
		if (!string.IsNullOrEmpty(company.GSTNo))
		{
			worksheet.Range[leftRow, 1, leftRow, 4].Merge();
			worksheet.Range[leftRow, 1].Text = $"GSTIN: {company.GSTNo}";
			leftRow++;
		}

		// Bill To GST (Right)
		if (billTo != null && !string.IsNullOrEmpty(billTo.GSTNo))
		{
			worksheet.Range[rightRow, 6, rightRow, 10].Merge();
			worksheet.Range[rightRow, 6].Text = $"GSTIN: {billTo.GSTNo}";
			rightRow++;
		}

		// Customer Information (Right column, after Bill To)
		if (customer != null && (!string.IsNullOrEmpty(customer.Name) || !string.IsNullOrEmpty(customer.Number)))
		{
			rightRow++; // Add spacing
			worksheet.Range[rightRow, 6].Text = "CUSTOMER:";
			worksheet.Range[rightRow, 6].CellStyle.Font.Bold = true;
			worksheet.Range[rightRow, 6].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
			rightRow++;

			if (!string.IsNullOrEmpty(customer.Name))
			{
				worksheet.Range[rightRow, 6, rightRow, 10].Merge();
				worksheet.Range[rightRow, 6].Text = customer.Name;
				rightRow++;
			}
			if (!string.IsNullOrEmpty(customer.Number))
			{
				worksheet.Range[rightRow, 6, rightRow, 10].Merge();
				worksheet.Range[rightRow, 6].Text = $"Phone: {customer.Number}";
				rightRow++;
			}
		}

		// Use the maximum row from both columns
		int currentRow = Math.Max(leftRow, rightRow);

		// Linked Transaction Details (Left column) - shows connected order/sale reference
		if (!string.IsNullOrWhiteSpace(invoiceData.OrderTransactionNo))
		{
			currentRow++; // Add spacing
			worksheet.Range[currentRow, 1, currentRow, 4].Merge();
			worksheet.Range[currentRow, 1].Text = $"Ref. No: {invoiceData.OrderTransactionNo}";
			currentRow++;

			if (invoiceData.OrderDateTime.HasValue)
			{
				worksheet.Range[currentRow, 1, currentRow, 4].Merge();
				worksheet.Range[currentRow, 1].Text = $"Ref. Date: {invoiceData.OrderDateTime.Value:dd-MMM-yyyy hh:mm tt}";
				currentRow++;
			}
		}

		currentRow++;
		return currentRow;
	}

	/// <summary>
	/// Draw company information only (for accounting vouchers)
	/// </summary>
	private static int DrawCompanyInfoOnly(IWorksheet worksheet, CompanyModel company, int startRow)
	{
		int currentRow = startRow;

		worksheet.Range[currentRow, 1].Text = "COMPANY:";
		worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
		worksheet.Range[currentRow, 1].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
		currentRow++;

		worksheet.Range[currentRow, 1, currentRow, 5].Merge();
		worksheet.Range[currentRow, 1].Text = company.Name;
		worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
		currentRow++;

		if (!string.IsNullOrEmpty(company.Address))
		{
			worksheet.Range[currentRow, 1, currentRow, 5].Merge();
			worksheet.Range[currentRow, 1].Text = company.Address;
			worksheet.Range[currentRow, 1].WrapText = true;
			currentRow++;
		}

		if (!string.IsNullOrEmpty(company.Phone))
		{
			worksheet.Range[currentRow, 1, currentRow, 5].Merge();
			worksheet.Range[currentRow, 1].Text = $"Phone: {company.Phone}";
			currentRow++;
		}

		if (!string.IsNullOrEmpty(company.GSTNo))
		{
			worksheet.Range[currentRow, 1, currentRow, 5].Merge();
			worksheet.Range[currentRow, 1].Text = $"GSTIN: {company.GSTNo}";
			currentRow++;
		}

		currentRow++;
		return currentRow;
	}

	/// <summary>
	/// Draw line items table - Optimized for performance using range-based styling
	/// </summary>
	private static int DrawLineItemsTable(IWorksheet worksheet, List<InvoiceLineItem> lineItems, int startRow)
	{
		if (lineItems == null || lineItems.Count == 0)
			return startRow + 1;

		int currentRow = startRow;
		int itemCount = lineItems.Count;

		// Determine which columns to show
		bool hasDiscount = lineItems.Any(i => i.DiscountPercent > 0);
		bool hasTax = lineItems.Any(i => i.CGSTPercent > 0 || i.SGSTPercent > 0 || i.IGSTPercent > 0 || i.TotalTaxAmount > 0);
		bool hasUOM = lineItems.Any(i => !string.IsNullOrWhiteSpace(i.UnitOfMeasurement));
		bool hasRate = lineItems.Any(i => i.Rate > 0);
		bool hasTotal = lineItems.Any(i => i.Total > 0);

		// Build column list with metadata
		var columns = new List<(string Header, double Width, ExcelHAlign Align, string Format, bool IsNumeric)>
		{
			("#", 5, ExcelHAlign.HAlignCenter, null, true),
			("Item Description", 30, ExcelHAlign.HAlignLeft, null, false),
			("Qty", 10, ExcelHAlign.HAlignRight, "#,##0.00", true)
		};

		if (hasUOM) columns.Add(("UOM", 8, ExcelHAlign.HAlignCenter, null, false));
		if (hasRate) columns.Add(("Rate", 12, ExcelHAlign.HAlignRight, "#,##0.00", true));
		if (hasDiscount)
		{
			columns.Add(("Disc %", 8, ExcelHAlign.HAlignRight, "#,##0.00", true));
			columns.Add(("Taxable", 12, ExcelHAlign.HAlignRight, "#,##0.00", true));
		}
		if (hasTax)
		{
			columns.Add(("Tax %", 8, ExcelHAlign.HAlignRight, "#,##0.00", true));
			columns.Add(("Tax Amt", 12, ExcelHAlign.HAlignRight, "#,##0.00", true));
		}
		if (hasTotal) columns.Add(("Total", 15, ExcelHAlign.HAlignRight, "#,##0.00", true));

		int colCount = columns.Count;

		// Set column widths and write header row
		for (int c = 0; c < colCount; c++)
		{
			worksheet.SetColumnWidth(c + 1, columns[c].Width);
			worksheet.Range[currentRow, c + 1].Text = columns[c].Header;
		}

		int headerRow = currentRow;
		currentRow++;

		// Write data rows
		int dataStartRow = currentRow;
		int rowNum = 1;
		foreach (var item in lineItems)
		{
			int c = 1;

			// Row number
			worksheet.Range[currentRow, c++].Number = rowNum;

			// Item name
			worksheet.Range[currentRow, c++].Text = item.ItemName;

			// Quantity
			worksheet.Range[currentRow, c++].Number = (double)item.Quantity;

			// UOM
			if (hasUOM)
				worksheet.Range[currentRow, c++].Text = item.UnitOfMeasurement ?? "-";

			// Rate
			if (hasRate)
				worksheet.Range[currentRow, c++].Number = (double)item.Rate;

			// Discount
			if (hasDiscount)
			{
				worksheet.Range[currentRow, c++].Number = (double)item.DiscountPercent;
				worksheet.Range[currentRow, c++].Number = (double)item.AfterDiscount;
			}

			// Tax
			if (hasTax)
			{
				decimal totalTaxPercent = item.CGSTPercent + item.SGSTPercent + item.IGSTPercent;
				if (totalTaxPercent > 0)
					worksheet.Range[currentRow, c].Number = (double)totalTaxPercent;
				else
					worksheet.Range[currentRow, c].Text = "-";
				c++;

				if (item.TotalTaxAmount > 0)
					worksheet.Range[currentRow, c].Number = (double)item.TotalTaxAmount;
				else
					worksheet.Range[currentRow, c].Text = "-";
				c++;
			}

			// Total
			if (hasTotal)
				worksheet.Range[currentRow, c++].Number = (double)item.Total;

			currentRow++;
			rowNum++;
		}

		int dataEndRow = currentRow - 1;

		// Style header row (single range operation)
		IRange headerRange = worksheet.Range[headerRow, 1, headerRow, colCount];
		headerRange.CellStyle.Font.Bold = true;
		headerRange.CellStyle.Font.Size = 10;
		headerRange.CellStyle.Color = PrimaryBlue;
		headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
		headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		headerRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Blue;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].Color = ExcelKnownColors.Blue;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].Color = ExcelKnownColors.Blue;
		headerRange.BorderInside(ExcelLineStyle.Thin, ExcelKnownColors.Blue);
		worksheet.SetRowHeight(headerRow, 22);

		// Style all data rows at once
		if (itemCount > 0)
		{
			// Apply borders to entire data range at once
			IRange allDataRange = worksheet.Range[dataStartRow, 1, dataEndRow, colCount];
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.BorderInside(ExcelLineStyle.Hair, ExcelKnownColors.Grey_25_percent);

			// Apply column-specific formatting
			for (int c = 0; c < colCount; c++)
			{
				IRange colRange = worksheet.Range[dataStartRow, c + 1, dataEndRow, c + 1];
				colRange.CellStyle.HorizontalAlignment = columns[c].Align;
				if (!string.IsNullOrEmpty(columns[c].Format))
					colRange.NumberFormat = columns[c].Format;
			}

			// Make Total column bold
			if (hasTotal)
			{
				IRange totalCol = worksheet.Range[dataStartRow, colCount, dataEndRow, colCount];
				totalCol.CellStyle.Font.Bold = true;
			}

			// Apply alternate row colors
			for (int r = 0; r < itemCount; r++)
			{
				if ((r + 1) % 2 == 0)
				{
					IRange altRow = worksheet.Range[dataStartRow + r, 1, dataStartRow + r, colCount];
					altRow.CellStyle.Color = AlternateRowColor;
				}
			}
		}

		currentRow++;
		return currentRow;
	}

	/// <summary>
	/// Draw accounting line items table - Optimized for performance using range-based styling
	/// </summary>
	private static int DrawAccountingLineItemsTable(IWorksheet worksheet, List<AccountingLineItem> accountingItems, int startRow)
	{
		if (accountingItems == null || accountingItems.Count == 0)
			return startRow + 1;

		int currentRow = startRow;
		int itemCount = accountingItems.Count;
		bool hasReferences = accountingItems.Any(i => !string.IsNullOrWhiteSpace(i.ReferenceNo));
		bool hasRemarks = accountingItems.Any(i => !string.IsNullOrWhiteSpace(i.Remarks));

		// Build column list with metadata
		var columns = new List<(string Header, double Width, ExcelHAlign Align, string Format)>
		{
			("#", 5, ExcelHAlign.HAlignCenter, null),
			("Ledger", 35, ExcelHAlign.HAlignLeft, null)
		};

		if (hasReferences)
		{
			columns.Add(("Ref No", 15, ExcelHAlign.HAlignLeft, null));
			columns.Add(("Ref Type", 12, ExcelHAlign.HAlignCenter, null));
		}

		columns.Add(("Debit", 15, ExcelHAlign.HAlignRight, "#,##0.00"));
		columns.Add(("Credit", 15, ExcelHAlign.HAlignRight, "#,##0.00"));

		if (hasRemarks) columns.Add(("Remarks", 25, ExcelHAlign.HAlignLeft, null));

		int colCount = columns.Count;

		// Set column widths and write header row
		for (int c = 0; c < colCount; c++)
		{
			worksheet.SetColumnWidth(c + 1, columns[c].Width);
			worksheet.Range[currentRow, c + 1].Text = columns[c].Header;
		}

		int headerRow = currentRow;
		currentRow++;

		// Write data rows
		int dataStartRow = currentRow;
		int rowNum = 1;
		foreach (var item in accountingItems)
		{
			int c = 1;

			// Row number
			worksheet.Range[currentRow, c++].Number = rowNum;

			// Ledger name
			worksheet.Range[currentRow, c++].Text = item.LedgerName;

			// References
			if (hasReferences)
			{
				worksheet.Range[currentRow, c++].Text = item.ReferenceNo ?? "-";
				worksheet.Range[currentRow, c++].Text = item.ReferenceType ?? "-";
			}

			// Debit
			if (item.Debit.HasValue && item.Debit.Value > 0)
				worksheet.Range[currentRow, c].Number = (double)item.Debit.Value;
			else
				worksheet.Range[currentRow, c].Text = "-";
			c++;

			// Credit
			if (item.Credit.HasValue && item.Credit.Value > 0)
				worksheet.Range[currentRow, c].Number = (double)item.Credit.Value;
			else
				worksheet.Range[currentRow, c].Text = "-";
			c++;

			// Remarks
			if (hasRemarks)
				worksheet.Range[currentRow, c++].Text = item.Remarks ?? "-";

			currentRow++;
			rowNum++;
		}

		int dataEndRow = currentRow - 1;

		// Style header row (single range operation)
		IRange headerRange = worksheet.Range[headerRow, 1, headerRow, colCount];
		headerRange.CellStyle.Font.Bold = true;
		headerRange.CellStyle.Font.Size = 10;
		headerRange.CellStyle.Color = PrimaryBlue;
		headerRange.CellStyle.Font.Color = ExcelKnownColors.White;
		headerRange.CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
		headerRange.CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Blue;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].Color = ExcelKnownColors.Blue;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
		headerRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].Color = ExcelKnownColors.Blue;
		headerRange.BorderInside(ExcelLineStyle.Thin, ExcelKnownColors.Blue);
		worksheet.SetRowHeight(headerRow, 22);

		// Style all data rows at once
		if (itemCount > 0)
		{
			// Apply borders to entire data range at once
			IRange allDataRange = worksheet.Range[dataStartRow, 1, dataEndRow, colCount];
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeBottom].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeLeft].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Hair;
			allDataRange.CellStyle.Borders[ExcelBordersIndex.EdgeRight].Color = ExcelKnownColors.Grey_25_percent;
			allDataRange.BorderInside(ExcelLineStyle.Hair, ExcelKnownColors.Grey_25_percent);

			// Apply column-specific formatting
			for (int c = 0; c < colCount; c++)
			{
				IRange colRange = worksheet.Range[dataStartRow, c + 1, dataEndRow, c + 1];
				colRange.CellStyle.HorizontalAlignment = columns[c].Align;
				if (!string.IsNullOrEmpty(columns[c].Format))
					colRange.NumberFormat = columns[c].Format;
			}

			// Apply wrap text to remarks column if present
			if (hasRemarks)
			{
				IRange remarksCol = worksheet.Range[dataStartRow, colCount, dataEndRow, colCount];
				remarksCol.WrapText = true;
			}

			// Apply alternate row colors
			for (int r = 0; r < itemCount; r++)
			{
				if ((r + 1) % 2 == 0)
				{
					IRange altRow = worksheet.Range[dataStartRow + r, 1, dataStartRow + r, colCount];
					altRow.CellStyle.Color = AlternateRowColor;
				}
			}
		}

		currentRow++;
		return currentRow;
	}

	/// <summary>
	/// Draw summary and remarks section
	/// </summary>
	private static int DrawSummaryAndRemarks(IWorksheet worksheet, InvoiceData invoiceData, int startRow)
	{
		int currentRow = startRow;
		int summaryCol = 8;

		// Remarks (Left side)
		if (!string.IsNullOrWhiteSpace(invoiceData.Remarks))
		{
			worksheet.Range[currentRow, 1].Text = "Remarks:";
			worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
			currentRow++;

			worksheet.Range[currentRow, 1, currentRow + 2, 5].Merge();
			worksheet.Range[currentRow, 1].Text = invoiceData.Remarks;
			worksheet.Range[currentRow, 1].WrapText = true;
			worksheet.Range[currentRow, 1].CellStyle.VerticalAlignment = ExcelVAlign.VAlignTop;
		}

		// Summary (Right side)
		int summaryRow = startRow;

		// Items Total
		worksheet.Range[summaryRow, summaryCol].Text = "Items Total:";
		worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
		worksheet.Range[summaryRow, summaryCol + 1].Number = (double)invoiceData.ItemsTotalAmount;
		worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		summaryRow++;

		// Other Charges
		if (invoiceData.OtherChargesAmount != 0)
		{
			string otherChargesLabel = invoiceData.OtherChargesPercent > 0
				? $"Other Charges ({invoiceData.OtherChargesPercent:0.##}%):"
				: "Other Charges:";
			worksheet.Range[summaryRow, summaryCol].Text = otherChargesLabel;
			worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
			worksheet.Range[summaryRow, summaryCol + 1].Number = (double)invoiceData.OtherChargesAmount;
			worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
			worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			summaryRow++;
		}

		// Discount
		if (invoiceData.CashDiscountAmount != 0)
		{
			string discountLabel = invoiceData.CashDiscountPercent > 0
				? $"Discount ({invoiceData.CashDiscountPercent:0.##}%):"
				: "Discount:";
			worksheet.Range[summaryRow, summaryCol].Text = discountLabel;
			worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
			worksheet.Range[summaryRow, summaryCol + 1].Number = (double)(-invoiceData.CashDiscountAmount);
			worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
			worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Color = ExcelKnownColors.Red;
			summaryRow++;
		}

		// Round Off
		if (invoiceData.RoundOffAmount != 0)
		{
			worksheet.Range[summaryRow, summaryCol].Text = "Round Off:";
			worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
			worksheet.Range[summaryRow, summaryCol + 1].Number = (double)invoiceData.RoundOffAmount;
			worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
			worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
			summaryRow++;
		}

		// Separator line
		worksheet.Range[summaryRow, summaryCol, summaryRow, summaryCol + 2].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Medium;
		worksheet.Range[summaryRow, summaryCol, summaryRow, summaryCol + 2].CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;

		// Grand Total
		worksheet.Range[summaryRow, summaryCol].Text = "GRAND TOTAL:";
		worksheet.Range[summaryRow, summaryCol].CellStyle.Font.Bold = true;
		worksheet.Range[summaryRow, summaryCol].CellStyle.Font.Size = 11;
		worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
		worksheet.Range[summaryRow, summaryCol + 1].Number = (double)invoiceData.TotalAmount;
		worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Bold = true;
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Size = 11;
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Color = TotalRowBackground;
		summaryRow++;

		currentRow = Math.Max(currentRow + 3, summaryRow) + 1;
		return currentRow;
	}

	/// <summary>
	/// Draw accounting summary
	/// </summary>
	private static int DrawAccountingSummary(IWorksheet worksheet, List<AccountingLineItem> accountingItems, InvoiceData invoiceData, int startRow)
	{
		int currentRow = startRow;
		int summaryCol = 6;

		decimal totalDebit = accountingItems.Sum(i => i.Debit ?? 0);
		decimal totalCredit = accountingItems.Sum(i => i.Credit ?? 0);
		decimal difference = totalDebit - totalCredit;

		// Remarks (Left side)
		if (!string.IsNullOrWhiteSpace(invoiceData.Remarks))
		{
			worksheet.Range[currentRow, 1].Text = "Remarks:";
			worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
			currentRow++;

			worksheet.Range[currentRow, 1, currentRow + 1, 4].Merge();
			worksheet.Range[currentRow, 1].Text = invoiceData.Remarks;
			worksheet.Range[currentRow, 1].WrapText = true;
		}

		// Summary (Right side)
		int summaryRow = startRow;

		// Total Debit
		worksheet.Range[summaryRow, summaryCol].Text = "Total Debit:";
		worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
		worksheet.Range[summaryRow, summaryCol + 1].Number = (double)totalDebit;
		worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		summaryRow++;

		// Total Credit
		worksheet.Range[summaryRow, summaryCol].Text = "Total Credit:";
		worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
		worksheet.Range[summaryRow, summaryCol + 1].Number = (double)totalCredit;
		worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		summaryRow++;

		// Separator
		worksheet.Range[summaryRow, summaryCol, summaryRow, summaryCol + 2].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Medium;
		worksheet.Range[summaryRow, summaryCol, summaryRow, summaryCol + 2].CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;

		// Difference
		worksheet.Range[summaryRow, summaryCol].Text = "Difference:";
		worksheet.Range[summaryRow, summaryCol].CellStyle.Font.Bold = true;
		worksheet.Range[summaryRow, summaryCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
		worksheet.Range[summaryRow, summaryCol + 1, summaryRow, summaryCol + 2].Merge();
		worksheet.Range[summaryRow, summaryCol + 1].Number = (double)difference;
		worksheet.Range[summaryRow, summaryCol + 1].NumberFormat = "#,##0.00";
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Bold = true;
		worksheet.Range[summaryRow, summaryCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;

		if (difference == 0)
			worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Color = ExcelKnownColors.Green;
		else
			worksheet.Range[summaryRow, summaryCol + 1].CellStyle.Font.Color = ExcelKnownColors.Red;

		summaryRow++;

		currentRow = Math.Max(currentRow + 2, summaryRow) + 1;
		return currentRow;
	}

	/// <summary>
	/// Draw amount in words and payment methods
	/// </summary>
	private static int DrawAmountInWordsAndPayment(IWorksheet worksheet, InvoiceData invoiceData, int startRow)
	{
		int currentRow = startRow;

		// Amount in Words (Left side)
		worksheet.Range[currentRow, 1].Text = "Amount in Words:";
		worksheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
		currentRow++;

		try
		{
			var converter = new CurrencyWordsConverter(new CurrencyWordsConversionOptions
			{
				Culture = Culture.International,
				OutputFormat = OutputFormat.English,
				CurrencyUnit = "Rupees",
				SubCurrencyUnit = "Paise",
				CurrencyUnitSeparator = "and"
			});

			string amountInWords = converter.ToWords(invoiceData.TotalAmount);
			amountInWords = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(amountInWords.ToLower());

			worksheet.Range[currentRow, 1, currentRow, 5].Merge();
			worksheet.Range[currentRow, 1].Text = amountInWords;
			worksheet.Range[currentRow, 1].CellStyle.Font.Italic = true;
			worksheet.Range[currentRow, 1].WrapText = true;
		}
		catch
		{
			worksheet.Range[currentRow, 1, currentRow, 5].Merge();
			worksheet.Range[currentRow, 1].Text = $"₹ {invoiceData.TotalAmount:N2}";
		}

		// Payment Methods (Right side)
		int paymentRow = startRow;
		int paymentCol = 7;
		bool hasPayments = invoiceData.Cash > 0 || invoiceData.Card > 0 || invoiceData.UPI > 0 || invoiceData.Credit > 0;

		if (hasPayments)
		{
			worksheet.Range[paymentRow, paymentCol].Text = "Payment Methods:";
			worksheet.Range[paymentRow, paymentCol].CellStyle.Font.Bold = true;
			paymentRow++;

			if (invoiceData.Cash > 0)
			{
				worksheet.Range[paymentRow, paymentCol].Text = "Cash:";
				worksheet.Range[paymentRow, paymentCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				worksheet.Range[paymentRow, paymentCol + 1, paymentRow, paymentCol + 2].Merge();
				worksheet.Range[paymentRow, paymentCol + 1].Number = (double)invoiceData.Cash;
				worksheet.Range[paymentRow, paymentCol + 1].NumberFormat = "#,##0.00";
				worksheet.Range[paymentRow, paymentCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				paymentRow++;
			}

			if (invoiceData.Card > 0)
			{
				worksheet.Range[paymentRow, paymentCol].Text = "Card:";
				worksheet.Range[paymentRow, paymentCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				worksheet.Range[paymentRow, paymentCol + 1, paymentRow, paymentCol + 2].Merge();
				worksheet.Range[paymentRow, paymentCol + 1].Number = (double)invoiceData.Card;
				worksheet.Range[paymentRow, paymentCol + 1].NumberFormat = "#,##0.00";
				worksheet.Range[paymentRow, paymentCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				paymentRow++;
			}

			if (invoiceData.UPI > 0)
			{
				worksheet.Range[paymentRow, paymentCol].Text = "UPI:";
				worksheet.Range[paymentRow, paymentCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				worksheet.Range[paymentRow, paymentCol + 1, paymentRow, paymentCol + 2].Merge();
				worksheet.Range[paymentRow, paymentCol + 1].Number = (double)invoiceData.UPI;
				worksheet.Range[paymentRow, paymentCol + 1].NumberFormat = "#,##0.00";
				worksheet.Range[paymentRow, paymentCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				paymentRow++;
			}

			if (invoiceData.Credit > 0)
			{
				worksheet.Range[paymentRow, paymentCol].Text = "Credit:";
				worksheet.Range[paymentRow, paymentCol].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				worksheet.Range[paymentRow, paymentCol + 1, paymentRow, paymentCol + 2].Merge();
				worksheet.Range[paymentRow, paymentCol + 1].Number = (double)invoiceData.Credit;
				worksheet.Range[paymentRow, paymentCol + 1].NumberFormat = "#,##0.00";
				worksheet.Range[paymentRow, paymentCol + 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
				paymentRow++;
			}
		}

		currentRow = Math.Max(currentRow + 2, paymentRow) + 1;
		return currentRow;
	}

	/// <summary>
	/// Draw branding footer - matches PDF invoice footer style
	/// </summary>
	private static async Task<int> DrawBrandingFooter(IWorksheet worksheet, int startRow)
	{
		int currentRow = startRow + 1;

		// Blue separator line at top (matching PDF)
		worksheet.Range[currentRow, 1, currentRow, 10].Merge();
		worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
		worksheet.Range[currentRow, 1].CellStyle.Borders[ExcelBordersIndex.EdgeTop].Color = ExcelKnownColors.Blue;
		currentRow++;

		// Get current date/time
		var currentDateTime = await CommonData.LoadCurrentDateTime();

		// Footer row with three sections: Left (branding), Center (export date), Right (empty for Excel - no page numbers needed)
		// Left section: AadiSoft branding
		worksheet.Range[currentRow, 1, currentRow, 3].Merge();
		worksheet.Range[currentRow, 1].Text = $"© {currentDateTime.Year} A Product By aadisoft.vercel.app";
		worksheet.Range[currentRow, 1].CellStyle.Font.Size = 7;
		worksheet.Range[currentRow, 1].CellStyle.Font.Italic = true;
		worksheet.Range[currentRow, 1].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
		worksheet.Range[currentRow, 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

		// Center section: Export date
		worksheet.Range[currentRow, 4, currentRow, 7].Merge();
		worksheet.Range[currentRow, 4].Text = $"Exported on: {currentDateTime:dd-MMM-yyyy hh:mm tt}";
		worksheet.Range[currentRow, 4].CellStyle.Font.Size = 7;
		worksheet.Range[currentRow, 4].CellStyle.Font.Italic = true;
		worksheet.Range[currentRow, 4].CellStyle.Font.Color = ExcelKnownColors.Grey_50_percent;
		worksheet.Range[currentRow, 4].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

		return currentRow;
	}

	/// <summary>
	/// Apply final formatting to the worksheet
	/// </summary>
	private static void ApplyFinalFormatting(IWorksheet worksheet)
	{
		// Hide gridlines for cleaner invoice appearance (only data grid has borders)
		worksheet.IsGridLinesVisible = false;

		// Set print options
		worksheet.PageSetup.Orientation = ExcelPageOrientation.Portrait;
		worksheet.PageSetup.PaperSize = ExcelPaperSize.PaperA4;
		worksheet.PageSetup.LeftMargin = 0.5;
		worksheet.PageSetup.RightMargin = 0.5;
		worksheet.PageSetup.TopMargin = 0.5;
		worksheet.PageSetup.BottomMargin = 0.5;
		worksheet.PageSetup.FitToPagesTall = 0;
		worksheet.PageSetup.FitToPagesWide = 1;

		// Set default font
		worksheet.UsedRange.CellStyle.Font.FontName = "Calibri";
	}

	#endregion
}
