using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

/// <summary>
/// PDF export functionality for Stock Transfer Item Report
/// </summary>
public static class StockTransferItemReportPdfExport
{
	/// <summary>
	/// Export Stock Transfer Item Report to PDF with custom column order and formatting
	/// </summary>
	/// <param name="stockTransferItemData">Collection of stock transfer item overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <param name="showSummary">Whether to show summary grouped by item</param>
	/// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
	/// <param name="locationName">Name of the location for report header</param>
	/// <param name="toLocationName">Name of the to-location for report header</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportStockTransferItemReport(
		IEnumerable<StockTransferItemOverviewModel> stockTransferItemData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		bool showLocation = false,
		string locationName = null,
        bool showToLocation = false,
		string toLocationName = null)
	{
		// Define custom column settings matching Excel export
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

		// Define column order based on showAllColumns and showSummary flags
		List<string> columnOrder;

		// Summary mode - grouped by item with aggregated values
		if (showSummary)
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.ItemCategoryName),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.BaseTotal),
				nameof(StockTransferItemOverviewModel.DiscountAmount),
				nameof(StockTransferItemOverviewModel.AfterDiscount),
				nameof(StockTransferItemOverviewModel.SGSTAmount),
				nameof(StockTransferItemOverviewModel.CGSTAmount),
				nameof(StockTransferItemOverviewModel.IGSTAmount),
				nameof(StockTransferItemOverviewModel.TotalTaxAmount),
				nameof(StockTransferItemOverviewModel.Total),
				nameof(StockTransferItemOverviewModel.NetTotal)
			];

		else if (showAllColumns)
		{
			// All columns - detailed view (matching Excel export)
			List<string> columns =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.ItemCategoryName),
				nameof(StockTransferItemOverviewModel.TransactionNo),
				nameof(StockTransferItemOverviewModel.TransactionDateTime),
				nameof(StockTransferItemOverviewModel.CompanyName)
			];

			if (showLocation)
				columns.Add(nameof(StockTransferItemOverviewModel.LocationName));
			if (showToLocation)
				columns.Add(nameof(StockTransferItemOverviewModel.ToLocationName));

			columns.AddRange([
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.Rate),
				nameof(StockTransferItemOverviewModel.BaseTotal),
				nameof(StockTransferItemOverviewModel.DiscountPercent),
				nameof(StockTransferItemOverviewModel.DiscountAmount),
				nameof(StockTransferItemOverviewModel.AfterDiscount),
				nameof(StockTransferItemOverviewModel.SGSTPercent),
				nameof(StockTransferItemOverviewModel.SGSTAmount),
				nameof(StockTransferItemOverviewModel.CGSTPercent),
				nameof(StockTransferItemOverviewModel.CGSTAmount),
				nameof(StockTransferItemOverviewModel.IGSTPercent),
				nameof(StockTransferItemOverviewModel.IGSTAmount),
				nameof(StockTransferItemOverviewModel.TotalTaxAmount),
				nameof(StockTransferItemOverviewModel.InclusiveTax),
				nameof(StockTransferItemOverviewModel.Total),
				nameof(StockTransferItemOverviewModel.NetRate),
				nameof(StockTransferItemOverviewModel.NetTotal),
				nameof(StockTransferItemOverviewModel.StockTransferRemarks),
				nameof(StockTransferItemOverviewModel.Remarks)
			]);

			columnOrder = columns;
		}
		// Summary columns - key fields only (matching Excel export)
		else
		{
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.TransactionNo),
				nameof(StockTransferItemOverviewModel.TransactionDateTime),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.NetRate),
				nameof(StockTransferItemOverviewModel.NetTotal)
			];

			if (!showLocation)
				columnOrder.Insert(4, nameof(StockTransferItemOverviewModel.LocationName));
			if (showToLocation && !showLocation)
				columnOrder.Insert(5, nameof(StockTransferItemOverviewModel.ToLocationName));
		}

		// Customize specific columns for PDF display (matching Excel column names)
		columnSettings[nameof(StockTransferItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.LocationName)] = new() { DisplayName = "From Location", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.StockTransferRemarks)] = new() { DisplayName = "Stock Transfer Remarks", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };
		columnSettings[nameof(StockTransferItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false };

		columnSettings[nameof(StockTransferItemOverviewModel.Quantity)] = new()
		{
			DisplayName = "Qty",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.Rate)] = new()
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

		columnSettings[nameof(StockTransferItemOverviewModel.NetRate)] = new()
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

		columnSettings[nameof(StockTransferItemOverviewModel.BaseTotal)] = new()
		{
			DisplayName = "Base Total",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.DiscountPercent)] = new()
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

		columnSettings[nameof(StockTransferItemOverviewModel.DiscountAmount)] = new()
		{
			DisplayName = "Disc Amt",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.AfterDiscount)] = new()
		{
			DisplayName = "After Disc",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.SGSTPercent)] = new()
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

		columnSettings[nameof(StockTransferItemOverviewModel.SGSTAmount)] = new()
		{
			DisplayName = "SGST Amt",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.CGSTPercent)] = new()
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

		columnSettings[nameof(StockTransferItemOverviewModel.CGSTAmount)] = new()
		{
			DisplayName = "CGST Amt",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.IGSTPercent)] = new()
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

		columnSettings[nameof(StockTransferItemOverviewModel.IGSTAmount)] = new()
		{
			DisplayName = "IGST Amt",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.TotalTaxAmount)] = new()
		{
			DisplayName = "Tax",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.Total)] = new()
		{
			DisplayName = "Total",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(StockTransferItemOverviewModel.NetTotal)] = new()
		{
			DisplayName = "Net Total",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		// Call the generic PDF export utility with landscape mode for all columns
		return await PDFReportExportUtil.ExportToPdf(
			stockTransferItemData,
			"STOCK TRANSFER ITEM REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useLandscape: showAllColumns || showSummary,  // Use landscape when showing all columns
			locationName: locationName,
			partyName: showToLocation ? toLocationName : null
		);
	}
}
