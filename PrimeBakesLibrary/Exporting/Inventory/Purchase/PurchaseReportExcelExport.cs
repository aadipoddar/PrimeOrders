using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// Excel export functionality for Purchase Report
/// </summary>
public static class PurchaseReportExcelExport
{
	/// <summary>
	/// Export Purchase Report to Excel with custom column order and formatting
	/// </summary>
	/// <param name="purchaseData">Collection of purchase overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportPurchaseReport(
		IEnumerable<PurchaseOverviewModel> purchaseData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true)
	{
		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// IDs - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["PartyId"] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["FinancialYearId"] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CreatedBy"] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["LastModifiedBy"] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields
			["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CompanyName"] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["PartyName"] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["FinancialYear"] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["CreatedByName"] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["LastModifiedByUserName"] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["DocumentUrl"] = new() { DisplayName = "Document URL", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

			// Date fields
			["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

			// Numeric fields - Items and Quantities
			["TotalItems"] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			["TotalQuantity"] = new() { DisplayName = "Total Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

			// Amount fields - All with N2 format and totals
			["BaseTotal"] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["DiscountAmount"] = new() { DisplayName = "Discount Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["AfterDiscount"] = new() { DisplayName = "After Discount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["SGSTAmount"] = new() { DisplayName = "SGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["CGSTAmount"] = new() { DisplayName = "CGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["IGSTAmount"] = new() { DisplayName = "IGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalTaxAmount"] = new() { DisplayName = "Total Tax Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAfterTax"] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["OtherChargesAmount"] = new() { DisplayName = "Other Charges Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAfterOtherCharges"] = new() { DisplayName = "After Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["CashDiscountAmount"] = new() { DisplayName = "Cash Discount Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAfterCashDiscount"] = new() { DisplayName = "After Cash Discount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["RoundOffAmount"] = new() { DisplayName = "Round Off Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAmount"] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

			// Percentage fields - Center aligned
			["DiscountPercent"] = new() { DisplayName = "Discount %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["SGSTPercent"] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CGSTPercent"] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["IGSTPercent"] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["OtherChargesPercent"] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CashDiscountPercent"] = new() { DisplayName = "Cash Discount %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order based on showAllColumns flag
		List<string> columnOrder;

		// All columns in logical order
		if (showAllColumns)
			columnOrder =
			[
				"TransactionNo",
				"TransactionDateTime",
				"CompanyName",
				"PartyName",
				"FinancialYear",
				"TotalItems",
				"TotalQuantity",
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
				"TotalAfterTax",
				"OtherChargesPercent",
				"OtherChargesAmount",
				"TotalAfterOtherCharges",
				"CashDiscountPercent",
				"CashDiscountAmount",
				"TotalAfterCashDiscount",
				"RoundOffAmount",
				"TotalAmount",
				"Remarks",
				"CreatedByName",
				"CreatedAt",
				"CreatedFromPlatform",
				"LastModifiedByUserName",
				"LastModifiedAt",
				"LastModifiedFromPlatform"
			];

		// Summary columns only
		else
			columnOrder =
			[
				"TransactionNo",
				"TransactionDateTime",
				"PartyName",
				"TotalQuantity",
				"TotalAfterTax",
				"OtherChargesPercent",
				"CashDiscountPercent",
				"TotalAmount"
			];

		// Export using the generic utility
		return ExcelExportUtil.ExportToExcel(
			purchaseData,
			"PURCHASE REPORT",
			"Purchase Transactions",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder
		);
	}
}
