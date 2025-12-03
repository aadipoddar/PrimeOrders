using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// PDF export functionality for Purchase Report
/// </summary>
public static class PurchaseReportPDFExport
{
	/// <summary>
	/// Export Purchase Report to PDF with custom column order and formatting
	/// </summary>
	/// <param name="purchaseData">Collection of purchase overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportPurchaseReport(
		IEnumerable<PurchaseOverviewModel> purchaseData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true)
	{
		// Define custom column settings matching Excel export
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

		// Define column order based on visibility setting (matching Excel export)
		List<string> columnOrder;

		// All columns - detailed view (matching Excel export)
		if (showAllColumns)
			columnOrder =
			[
				nameof(PurchaseOverviewModel.TransactionNo),
				nameof(PurchaseOverviewModel.TransactionDateTime),
				nameof(PurchaseOverviewModel.CompanyName),
				nameof(PurchaseOverviewModel.PartyName),
				nameof(PurchaseOverviewModel.FinancialYear),
				nameof(PurchaseOverviewModel.TotalItems),
				nameof(PurchaseOverviewModel.TotalQuantity),
				nameof(PurchaseOverviewModel.BaseTotal),
				nameof(PurchaseOverviewModel.ItemDiscountAmount),
				nameof(PurchaseOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseOverviewModel.TotalAfterTax),
				nameof(PurchaseOverviewModel.OtherChargesPercent),
				nameof(PurchaseOverviewModel.OtherChargesAmount),
				nameof(PurchaseOverviewModel.CashDiscountPercent),
				nameof(PurchaseOverviewModel.CashDiscountAmount),
				nameof(PurchaseOverviewModel.RoundOffAmount),
				nameof(PurchaseOverviewModel.TotalAmount),
				nameof(PurchaseOverviewModel.Remarks),
				nameof(PurchaseOverviewModel.CreatedByName),
				nameof(PurchaseOverviewModel.CreatedAt),
				nameof(PurchaseOverviewModel.CreatedFromPlatform),
				nameof(PurchaseOverviewModel.LastModifiedByUserName),
				nameof(PurchaseOverviewModel.LastModifiedAt),
				nameof(PurchaseOverviewModel.LastModifiedFromPlatform)
			];
		// Summary columns - key fields only (matching Excel export)
		else
			columnOrder =
			[
				nameof(PurchaseOverviewModel.TransactionNo),
				nameof(PurchaseOverviewModel.TransactionDateTime),
				nameof(PurchaseOverviewModel.PartyName),
				nameof(PurchaseOverviewModel.TotalQuantity),
				nameof(PurchaseOverviewModel.TotalAfterTax),
				nameof(PurchaseOverviewModel.OtherChargesPercent),
				nameof(PurchaseOverviewModel.CashDiscountPercent),
				nameof(PurchaseOverviewModel.TotalAmount)
			];

		// Customize specific columns for PDF display (matching Excel column names)
		columnSettings[nameof(PurchaseOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(PurchaseOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

		columnSettings[nameof(PurchaseOverviewModel.TotalItems)] = new()
		{
			DisplayName = "Items",
			Format = "#,##0",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.TotalQuantity)] = new()
		{
			DisplayName = "Qty",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.BaseTotal)] = new()
		{
			DisplayName = "Base Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.ItemDiscountAmount)] = new()
		{
			DisplayName = "Dis Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.TotalAfterItemDiscount)] = new()
		{
			DisplayName = "After Disc",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount)] = new()
		{
			DisplayName = "Incl Tax",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.TotalExtraTaxAmount)] = new()
		{
			DisplayName = "Extra Tax",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.TotalAfterTax)] = new()
		{
			DisplayName = "Sub Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.OtherChargesPercent)] = new()
		{
			DisplayName = "Other Charges %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseOverviewModel.OtherChargesAmount)] = new()
		{
			DisplayName = "Other Charges Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.CashDiscountPercent)] = new()
		{
			DisplayName = "Cash Disc %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseOverviewModel.CashDiscountAmount)] = new()
		{
			DisplayName = "Cash Disc Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.RoundOffAmount)] = new()
		{
			DisplayName = "Round Off",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseOverviewModel.TotalAmount)] = new()
		{
			DisplayName = "Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		// Call the generic PDF export utility with landscape mode for all columns
		return await PDFReportExportUtil.ExportToPdf(
			purchaseData,
			"PURCHASE REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useLandscape: showAllColumns  // Use landscape when showing all columns
		);
	}
}
