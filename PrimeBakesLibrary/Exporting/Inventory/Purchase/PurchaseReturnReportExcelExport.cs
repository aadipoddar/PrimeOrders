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
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(PurchaseReturnOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            
            // Text fields
            [nameof(PurchaseReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.DocumentUrl)] = new() { DisplayName = "Document URL", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Date fields
            [nameof(PurchaseReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(PurchaseReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm tt", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Items and Quantities
            [nameof(PurchaseReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(PurchaseReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount fields - All with N2 format and totals
            [nameof(PurchaseReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl. Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

            // Percentage fields - Center aligned
            [nameof(PurchaseReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on visibility setting
        List<string> columnOrder;

        if (showAllColumns)
        {
            // All columns - detailed view
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.TransactionNo),
                nameof(PurchaseReturnOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnOverviewModel.CompanyName),
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.FinancialYear),
                nameof(PurchaseReturnOverviewModel.TotalItems),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.BaseTotal),
                nameof(PurchaseReturnOverviewModel.ItemDiscountAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount),
                nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
                nameof(PurchaseReturnOverviewModel.OtherChargesAmount),
                nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
                nameof(PurchaseReturnOverviewModel.CashDiscountAmount),
                nameof(PurchaseReturnOverviewModel.RoundOffAmount),
                nameof(PurchaseReturnOverviewModel.TotalAmount),
                nameof(PurchaseReturnOverviewModel.Remarks),
                nameof(PurchaseReturnOverviewModel.CreatedByName),
                nameof(PurchaseReturnOverviewModel.CreatedAt),
                nameof(PurchaseReturnOverviewModel.CreatedFromPlatform),
                nameof(PurchaseReturnOverviewModel.LastModifiedByUserName),
                nameof(PurchaseReturnOverviewModel.LastModifiedAt),
                nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)
            ];
        }
        else
        {
            // Summary columns - key fields only
            columnOrder =
            [
                nameof(PurchaseReturnOverviewModel.TransactionNo),
                nameof(PurchaseReturnOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnOverviewModel.PartyName),
                nameof(PurchaseReturnOverviewModel.TotalQuantity),
                nameof(PurchaseReturnOverviewModel.TotalAfterTax),
                nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
                nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
                nameof(PurchaseReturnOverviewModel.TotalAmount)
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
