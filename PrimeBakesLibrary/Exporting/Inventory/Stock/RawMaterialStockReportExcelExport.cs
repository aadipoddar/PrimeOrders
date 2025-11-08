using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// Excel export functionality for Raw Material Stock Report
/// </summary>
public static class RawMaterialStockReportExcelExport
{
	/// <summary>
	/// Export Raw Material Stock Report to Excel with custom column order and formatting
	/// </summary>
	/// <param name="stockData">Collection of raw material stock summary records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <param name="stockDetailsData">Optional collection of raw material stock detail records for second worksheet</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportRawMaterialStockReport(
		IEnumerable<RawMaterialStockSummaryModel> stockData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		IEnumerable<RawMaterialStockDetailsModel> stockDetailsData = null)
	{
		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// IDs - Center aligned, no totals
			["RawMaterialId"] = new() { DisplayName = "Material ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },
			["RawMaterialCategoryId"] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },

			// Text fields
			["RawMaterialName"] = new() { DisplayName = "Raw Material Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 25 },
			["RawMaterialCode"] = new() { DisplayName = "Material Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
			["RawMaterialCategoryName"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 20 },
			["UnitOfMeasurement"] = new() { DisplayName = "Unit", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 10 },

			// Stock quantity fields - All with totals
			["OpeningStock"] = new() { DisplayName = "Opening Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
			["PurchaseStock"] = new() { DisplayName = "Purchase Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
			["SaleStock"] = new() { DisplayName = "Sale Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
			["MonthlyStock"] = new() { DisplayName = "Monthly Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
			["ClosingStock"] = new() { DisplayName = "Closing Stock", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },

			// Rate/Price fields - Right aligned, no totals
			["Rate"] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 },
			["AveragePrice"] = new() { DisplayName = "Average Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 15 },
			["LastPurchasePrice"] = new() { DisplayName = "Last Purchase Price", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 18 },

			// Value fields - All with totals
			["ClosingValue"] = new() { DisplayName = "Closing Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
			["WeightedAverageValue"] = new() { DisplayName = "Weighted Avg Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 },
			["LastPurchaseValue"] = new() { DisplayName = "Last Purchase Value", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 18 }
		};

		// Define column order based on showAllColumns flag
		List<string> columnOrder;

		// All columns in logical order
		if (showAllColumns)
			columnOrder =
			[
				"RawMaterialName",
				"RawMaterialCode",
				"RawMaterialCategoryName",
				"UnitOfMeasurement",
				"OpeningStock",
				"PurchaseStock",
				"SaleStock",
				"MonthlyStock",
				"ClosingStock",
				"Rate",
				"ClosingValue",
				"AveragePrice",
				"LastPurchasePrice",
				"WeightedAverageValue",
				"LastPurchaseValue"
			];

		// Summary columns only (key information)
		else
			columnOrder =
			[
				"RawMaterialName",
				"UnitOfMeasurement",
				"OpeningStock",
				"PurchaseStock",
				"SaleStock",
				"ClosingStock",
				"Rate",
				"ClosingValue"
			];

		// If no details data provided, use the simple single-worksheet export
		if (stockDetailsData == null || !stockDetailsData.Any())
		{
			return ExcelExportUtil.ExportToExcel(
				stockData,
				"RAW MATERIAL STOCK REPORT",
				"Stock Summary",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder
			);
		}

		// Multi-worksheet export
		return ExportWithDetails(
			stockData,
			stockDetailsData,
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder
		);
	}

	/// <summary>
	/// Export with both summary and details worksheets
	/// </summary>
	private static MemoryStream ExportWithDetails(
		IEnumerable<RawMaterialStockSummaryModel> stockData,
		IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
		DateOnly? dateRangeStart,
		DateOnly? dateRangeEnd,
		Dictionary<string, ExcelExportUtil.ColumnSetting> summaryColumnSettings,
		List<string> summaryColumnOrder)
	{
		// Create the first worksheet with summary data
		var summaryStream = ExcelExportUtil.ExportToExcel(
			stockData,
			"RAW MATERIAL STOCK REPORT",
			"Stock Summary",
			dateRangeStart,
			dateRangeEnd,
			summaryColumnSettings,
			summaryColumnOrder
		);

		// Define column settings for details worksheet
		var detailsColumnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// IDs - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 10 },
			["RawMaterialId"] = new() { DisplayName = "Material ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 12 },
			["TransactionId"] = new() { DisplayName = "Transaction ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false, Width = 15 },

			// Text fields
			["RawMaterialName"] = new() { DisplayName = "Raw Material Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 25 },
			["RawMaterialCode"] = new() { DisplayName = "Material Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },
			["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, Width = 18 },
			["Type"] = new() { DisplayName = "Transaction Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 18 },

			// Date fields
			["TransactionDate"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, Width = 15 },

			// Numeric fields
			["Quantity"] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true, Width = 15 },
			["NetRate"] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false, Width = 12 }
		};

		// Define column order for details
		var detailsColumnOrder = new List<string>
		{
			"TransactionDate",
			"TransactionNo",
			"Type",
			"RawMaterialName",
			"RawMaterialCode",
			"Quantity",
			"NetRate"
		};

		// Create the details worksheet
		var detailsStream = ExcelExportUtil.ExportToExcel(
			stockDetailsData,
			"RAW MATERIAL STOCK DETAILS",
			"Transaction Details",
			dateRangeStart,
			dateRangeEnd,
			detailsColumnSettings,
			detailsColumnOrder
		);

		// Now combine both worksheets into one workbook
		return CombineWorksheets(summaryStream, detailsStream);
	}

	/// <summary>
	/// Combine two Excel streams into a single workbook with multiple worksheets
	/// </summary>
	private static MemoryStream CombineWorksheets(MemoryStream summaryStream, MemoryStream detailsStream)
	{
		using var excelEngine = new Syncfusion.XlsIO.ExcelEngine();
		var application = excelEngine.Excel;
		application.DefaultVersion = Syncfusion.XlsIO.ExcelVersion.Xlsx;

		// Load the summary workbook
		var workbook = application.Workbooks.Open(summaryStream);

		// Load the details workbook
		var detailsWorkbook = application.Workbooks.Open(detailsStream);

		// Copy the worksheet from details workbook to main workbook
		workbook.Worksheets.AddCopy(detailsWorkbook.Worksheets[0]);

		// Close the details workbook
		detailsWorkbook.Close();

		// Save the combined workbook to a new stream
		var combinedStream = new MemoryStream();
		workbook.SaveAs(combinedStream);
		combinedStream.Position = 0;

		// Clean up
		summaryStream.Dispose();
		detailsStream.Dispose();

		return combinedStream;
	}
}
