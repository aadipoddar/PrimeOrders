using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// Excel export functionality for Purchase Return Report
/// </summary>
public static class PurchaseReturnReportExcelExport
{
	/// <summary>
	/// Export Purchase Return Report to Excel with custom column order and formatting
	/// </summary>
	/// <param name="purchaseReturnData">Collection of purchase return overview records</param>
	/// <param name="dateRangeStart">Start date of the report</param>
	/// <param name="dateRangeEnd">End date of the report</param>
	/// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportPurchaseReturnReport(
		IEnumerable<PurchaseReturnOverviewModel> purchaseReturnData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true)
	{
		// Define custom column settings (same structure as PurchaseOverviewModel)
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// IDs - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["PartyId"] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["FinancialYearId"] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CreatedBy"] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["LastModifiedBy"] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CompanyName"] = new() { DisplayName = "Company Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["PartyName"] = new() { DisplayName = "Party Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["FinancialYear"] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["DocumentUrl"] = new() { DisplayName = "Document URL", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CreatedByName"] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CreatedFromPlatform"] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["LastModifiedByUserName"] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["LastModifiedFromPlatform"] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Dates - Center aligned with custom format
			["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["CreatedAt"] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["LastModifiedAt"] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

			// Numeric fields - Right aligned with totals
			["TotalItems"] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			["TotalQuantity"] = new() { DisplayName = "Total Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			["BaseTotal"] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["DiscountAmount"] = new() { DisplayName = "Discount Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["AfterDiscount"] = new() { DisplayName = "After Discount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["SGSTAmount"] = new() { DisplayName = "SGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["CGSTAmount"] = new() { DisplayName = "CGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["IGSTAmount"] = new() { DisplayName = "IGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalTaxAmount"] = new() { DisplayName = "Total Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAfterTax"] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["OtherChargesAmount"] = new() { DisplayName = "Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAfterOtherCharges"] = new() { DisplayName = "After Other Charges", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["CashDiscountAmount"] = new() { DisplayName = "Cash Discount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAfterCashDiscount"] = new() { DisplayName = "After Cash Discount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["RoundOffAmount"] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
			["TotalAmount"] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

			// Percentage fields - Center aligned, no totals
			["DiscountPercent"] = new() { DisplayName = "Discount %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["SGSTPercent"] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CGSTPercent"] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["IGSTPercent"] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["OtherChargesPercent"] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			["CashDiscountPercent"] = new() { DisplayName = "Cash Discount %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order based on visibility setting
		List<string> columnOrder;

		if (showAllColumns)
		{
			// All columns - detailed view
			columnOrder =
			[
				"Id", "TransactionNo", "CompanyId", "CompanyName", "PartyId", "PartyName",
				"TransactionDateTime", "FinancialYearId", "FinancialYear",
				"TotalItems", "TotalQuantity", "BaseTotal",
				"DiscountPercent", "DiscountAmount", "AfterDiscount",
				"SGSTPercent", "CGSTPercent", "IGSTPercent",
				"SGSTAmount", "CGSTAmount", "IGSTAmount", "TotalTaxAmount", "TotalAfterTax",
				"OtherChargesPercent", "OtherChargesAmount", "TotalAfterOtherCharges",
				"CashDiscountPercent", "CashDiscountAmount", "TotalAfterCashDiscount",
				"RoundOffAmount", "TotalAmount",
				"Remarks", "DocumentUrl",
				"CreatedBy", "CreatedByName", "CreatedAt", "CreatedFromPlatform",
				"LastModifiedBy", "LastModifiedByUserName", "LastModifiedAt", "LastModifiedFromPlatform"
			];
		}
		else
		{
			// Summary columns - key fields only
			columnOrder =
			[
				"TransactionNo", "TransactionDateTime", "PartyName",
				"TotalQuantity", "TotalAfterTax", "OtherChargesPercent", "CashDiscountPercent", "TotalAmount"
			];
		}

		// Call the generic Excel export utility
		return ExcelExportUtil.ExportToExcel(
			purchaseReturnData,
			"PURCHASE RETURN REPORT",
			"Purchase Return Transactions",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder
		);
	}
}
