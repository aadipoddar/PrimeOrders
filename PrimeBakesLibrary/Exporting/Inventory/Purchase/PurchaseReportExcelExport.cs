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
    public static async Task<MemoryStream> ExportPurchaseReport(
        IEnumerable<PurchaseOverviewModel> purchaseData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(PurchaseOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            [nameof(PurchaseOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.DocumentUrl)] = new() { DisplayName = "Document URL", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Date fields
            [nameof(PurchaseOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Items and Quantities
            [nameof(PurchaseOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(PurchaseOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount fields - All with N2 format and totals
            [nameof(PurchaseOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

            // Percentage fields - Center aligned
            [nameof(PurchaseOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
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

        // Summary columns only
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

        // Export using the generic utility
        return await ExcelExportUtil.ExportToExcel(
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
