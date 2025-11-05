using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// Excel export functionality for Purchase Item Report
/// </summary>
public static class PurchaseItemReportExcelExport
{
	/// <summary>
	/// Export Purchase Item Report to Excel with custom column order and formatting
	/// </summary>
	/// <param name="purchaseItemData">Collection of purchase item overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportPurchaseItemReport(
		IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true)
	{
		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// IDs - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["PurchaseId"] = new() { DisplayName = "Purchase ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["ItemCategoryId"] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["PartyId"] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields
			["ItemName"] = new() { DisplayName = "Item Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["ItemCode"] = new() { DisplayName = "Item Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["ItemCategoryName"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CompanyName"] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["PartyName"] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["PurchaseRemarks"] = new() { DisplayName = "Purchase Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Remarks"] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Date fields
			["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

			// Numeric fields - Quantity
			["Quantity"] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["Rate"] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
			["NetRate"] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

			// Amount fields - All with N2 format and totals
			["BaseTotal"] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["DiscountAmount"] = new() { DisplayName = "Discount Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["AfterDiscount"] = new() { DisplayName = "After Discount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["SGSTAmount"] = new() { DisplayName = "SGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["CGSTAmount"] = new() { DisplayName = "CGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["IGSTAmount"] = new() { DisplayName = "IGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalTaxAmount"] = new() { DisplayName = "Total Tax Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["Total"] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

			// Percentage fields - Center aligned
			["DiscountPercent"] = new() { DisplayName = "Discount %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["SGSTPercent"] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CGSTPercent"] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["IGSTPercent"] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Boolean fields
			["InclusiveTax"] = new() { DisplayName = "Inclusive Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order based on showAllColumns flag
		List<string> columnOrder;

		// All columns in logical order
		if (showAllColumns)
			columnOrder =
			[
				"ItemName",
				"ItemCode",
				"ItemCategoryName",
				"TransactionNo",
				"TransactionDateTime",
				"CompanyName",
				"PartyName",
				"Quantity",
				"Rate",
				"BaseTotal",
				"DiscountPercent",
				"DiscountAmount",
				"AfterDiscount",
				"SGSTPercent",
				"SGSTAmount",
				"CGSTPercent",
				"CGSTAmount",
				"IGSTPercent",
				"IGSTAmount",
				"TotalTaxAmount",
				"InclusiveTax",
				"Total",
				"NetRate",
				"PurchaseRemarks",
				"Remarks"
			];

		// Summary columns only
		else
			columnOrder =
			[
				"ItemName",
				"ItemCode",
				"TransactionNo",
				"TransactionDateTime",
				"PartyName",
				"Quantity",
				"Rate",
				"Total"
			];

		// Export using the generic utility
		return ExcelExportUtil.ExportToExcel(
			purchaseItemData,
			"PURCHASE ITEM REPORT",
			"Purchase Item Transactions",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder
		);
	}
}
