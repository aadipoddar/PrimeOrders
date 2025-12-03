using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

/// <summary>
/// PDF export functionality for Order Report
/// </summary>
public static class OrderReportPdfExport
{
	/// <summary>
	/// Export Order Report to PDF with custom column order and formatting
	/// </summary>
	/// <param name="orderData">Collection of order overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <param name="showLocation">Whether to include location column</param>
	/// <param name="locationName">Name of the location for report header</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportOrderReport(
		IEnumerable<OrderOverviewModel> orderData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showLocation = false,
		string locationName = null)
	{
		// Define custom column settings matching Excel export
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

		// Define column order based on visibility setting (matching Excel export)
		List<string> columnOrder;

		if (showAllColumns)
		{
			// All columns - detailed view (matching Excel export)
			columnOrder =
			[
				nameof(OrderOverviewModel.TransactionNo),
				nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.CompanyName)
			];

			// Add location columns if showLocation is true
			if (showLocation)
				columnOrder.Add(nameof(OrderOverviewModel.LocationName));

			// Continue with remaining columns
			columnOrder.AddRange(
			[
				nameof(OrderOverviewModel.TransactionDateTime),
				nameof(OrderOverviewModel.SaleDateTime),
				nameof(OrderOverviewModel.FinancialYear),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity),
				nameof(OrderOverviewModel.Remarks),
				nameof(OrderOverviewModel.CreatedByName),
				nameof(OrderOverviewModel.CreatedAt),
				nameof(OrderOverviewModel.CreatedFromPlatform),
				nameof(OrderOverviewModel.LastModifiedByUserName),
				nameof(OrderOverviewModel.LastModifiedAt),
				nameof(OrderOverviewModel.LastModifiedFromPlatform),
			]);
		}
		// Summary columns - key fields only (matching Excel export)
		else
			columnOrder =
			[
				nameof(OrderOverviewModel.TransactionNo),
				nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.TransactionDateTime),
				nameof(OrderOverviewModel.LocationName),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity),
			];

		// Customize specific columns for PDF display (matching Excel column names)
		columnSettings[nameof(OrderOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.SaleDateTime)] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
		columnSettings[nameof(OrderOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

		columnSettings[nameof(OrderOverviewModel.TotalItems)] = new()
		{
			DisplayName = "Items",
			Format = "#,##0",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		columnSettings[nameof(OrderOverviewModel.TotalQuantity)] = new()
		{
			DisplayName = "Qty",
			Format = "#,##0.00",
			StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
			{
				Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
				LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
			}
		};

		// Call the generic PDF export utility with landscape mode for all columns
		return await PDFReportExportUtil.ExportToPdf(
			orderData,
			"ORDER REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useLandscape: showAllColumns,  // Use landscape when showing all columns
			locationName: locationName
		);
	}
}
