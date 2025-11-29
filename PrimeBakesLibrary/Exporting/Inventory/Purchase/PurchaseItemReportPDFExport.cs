using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// PDF export functionality for Purchase Item Report
/// </summary>
public static class PurchaseItemReportPDFExport
{
	/// <summary>
	/// Export Purchase Item Report to PDF with custom column order and formatting
	/// </summary>
	/// <param name="purchaseItemData">Collection of purchase item overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportPurchaseItemReport(
		IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
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
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.ItemCategoryName),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.CompanyName),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.Rate),
				nameof(PurchaseItemOverviewModel.BaseTotal),
				nameof(PurchaseItemOverviewModel.DiscountPercent),
				nameof(PurchaseItemOverviewModel.DiscountAmount),
				nameof(PurchaseItemOverviewModel.AfterDiscount),
				nameof(PurchaseItemOverviewModel.SGSTPercent),
				nameof(PurchaseItemOverviewModel.SGSTAmount),
				nameof(PurchaseItemOverviewModel.CGSTPercent),
				nameof(PurchaseItemOverviewModel.CGSTAmount),
				nameof(PurchaseItemOverviewModel.IGSTPercent),
				nameof(PurchaseItemOverviewModel.IGSTAmount),
				nameof(PurchaseItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseItemOverviewModel.InclusiveTax),
				nameof(PurchaseItemOverviewModel.Total),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal),
				nameof(PurchaseItemOverviewModel.PurchaseRemarks),
				nameof(PurchaseItemOverviewModel.Remarks)
			];
		// Summary columns - key fields only (matching Excel export)
		else
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal)
			];

		// Customize specific columns for PDF display (matching Excel column names)
		columnSettings[nameof(PurchaseItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.PurchaseRemarks)] = new() { DisplayName = "Purchase Remarks", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };
		columnSettings[nameof(PurchaseItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false };

		columnSettings[nameof(PurchaseItemOverviewModel.Quantity)] = new()
		{
			DisplayName = "Qty",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseItemOverviewModel.Rate)] = new()
		{
			DisplayName = "Rate",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseItemOverviewModel.NetRate)] = new()
		{
			DisplayName = "Net Rate",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseItemOverviewModel.BaseTotal)] = new()
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

		columnSettings[nameof(PurchaseItemOverviewModel.DiscountPercent)] = new()
		{
			DisplayName = "Disc %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseItemOverviewModel.DiscountAmount)] = new()
		{
			DisplayName = "Disc Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseItemOverviewModel.AfterDiscount)] = new()
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

		columnSettings[nameof(PurchaseItemOverviewModel.SGSTPercent)] = new()
		{
			DisplayName = "SGST %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseItemOverviewModel.SGSTAmount)] = new()
		{
			DisplayName = "SGST Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseItemOverviewModel.CGSTPercent)] = new()
		{
			DisplayName = "CGST %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseItemOverviewModel.CGSTAmount)] = new()
		{
			DisplayName = "CGST Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseItemOverviewModel.IGSTPercent)] = new()
		{
			DisplayName = "IGST %",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			},
			IncludeInTotal = false
		};

		columnSettings[nameof(PurchaseItemOverviewModel.IGSTAmount)] = new()
		{
			DisplayName = "IGST Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseItemOverviewModel.TotalTaxAmount)] = new()
		{
			DisplayName = "Tax Amt",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(PurchaseItemOverviewModel.Total)] = new()
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

		columnSettings[nameof(PurchaseItemOverviewModel.NetTotal)] = new()
		{
			DisplayName = "Net Total",
			Format = "#,##0.00",
			HighlightNegative = true,
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		// Call the generic PDF export utility with landscape mode for all columns
		return PDFReportExportUtil.ExportToPdf(
			purchaseItemData,
			"PURCHASE ITEM REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useLandscape: showAllColumns  // Use landscape when showing all columns
		);
	}
}
