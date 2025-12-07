using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

/// <summary>
/// Excel export functionality for Stock Transfer Item Report
/// </summary>
public static class StockTransferItemReportExcelExport
{
	/// <summary>
	/// Export Stock Transfer Item Report to Excel with custom column order and formatting
	/// </summary>
	/// <param name="stockTransferItemData">Collection of stock transfer item overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <param name="showSummary">Whether to show summary grouped by item</param>
	/// <param name="showLocation">Whether to include location column (for location ID 1 users)</param>
	/// <param name="locationName">Name of the location for report header</param>
	/// <param name="toLocationName">Name of the to-location for report header</param>
	/// <returns>MemoryStream containing the Excel file</returns>
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
		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			// IDs - Center aligned, no totals
			[nameof(StockTransferItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			
			// Text fields
			[nameof(StockTransferItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.StockTransferRemarks)] = new() { DisplayName = "Stock Transfer Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(StockTransferItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Date fields
			[nameof(StockTransferItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

			// Numeric fields - Quantity
			[nameof(StockTransferItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

			// Amount fields - All with N2 format and totals
			[nameof(StockTransferItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Discount Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

			// Percentage fields - Center aligned
			[nameof(StockTransferItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Boolean fields
			[nameof(StockTransferItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

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

		// All columns in logical order
		else if (showAllColumns)
		{
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
		// Summary columns only
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
			if (!showToLocation)
				columnOrder.Insert(5, nameof(StockTransferItemOverviewModel.ToLocationName));
		}

		// Export using the generic utility
		return await ExcelReportExportUtil.ExportToExcel(
			stockTransferItemData,
			"STOCK TRANSFER ITEM REPORT",
			"Stock Transfer Item Transactions",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			locationName,
			showToLocation ? toLocationName : null
		);
	}
}
