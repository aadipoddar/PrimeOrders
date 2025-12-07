using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// Excel export functionality for Purchase Return Item Report
/// </summary>
public static class PurchaseReturnItemReportExcelExport
{
    /// <summary>
    /// Export Purchase Return Item Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="purchaseReturnItemData">Collection of purchase return item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showSummary">Whether to show summary grouped by item</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportPurchaseReturnItemReport(
        IEnumerable<PurchaseReturnItemOverviewModel> purchaseReturnItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(PurchaseReturnItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            // Text fields
            [nameof(PurchaseReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks)] = new() { DisplayName = "Purchase Return Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            [nameof(PurchaseReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            [nameof(PurchaseReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(PurchaseReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amount", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

            // Percentage fields - Center aligned
            [nameof(PurchaseReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Boolean fields
            [nameof(PurchaseReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns and showSummary flags
        List<string> columnOrder;

        // Summary mode - grouped by item with aggregated values
        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.BaseTotal),
                nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
                nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
                nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseReturnItemOverviewModel.Total),
                nameof(PurchaseReturnItemOverviewModel.NetTotal)
            ];
        }
        // All columns in logical order
        else if (showAllColumns)
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
                nameof(PurchaseReturnItemOverviewModel.TransactionNo),
                nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnItemOverviewModel.CompanyName),
                nameof(PurchaseReturnItemOverviewModel.PartyName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.Rate),
                nameof(PurchaseReturnItemOverviewModel.BaseTotal),
                nameof(PurchaseReturnItemOverviewModel.DiscountPercent),
                nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
                nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
                nameof(PurchaseReturnItemOverviewModel.SGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.CGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.IGSTPercent),
                nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
                nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseReturnItemOverviewModel.InclusiveTax),
                nameof(PurchaseReturnItemOverviewModel.Total),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks),
                nameof(PurchaseReturnItemOverviewModel.Remarks),
            ];

        // Summary columns only
        else
            columnOrder =
            [
                nameof(PurchaseReturnItemOverviewModel.ItemName),
                nameof(PurchaseReturnItemOverviewModel.ItemCode),
                nameof(PurchaseReturnItemOverviewModel.TransactionNo),
                nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
                nameof(PurchaseReturnItemOverviewModel.PartyName),
                nameof(PurchaseReturnItemOverviewModel.Quantity),
                nameof(PurchaseReturnItemOverviewModel.NetRate),
                nameof(PurchaseReturnItemOverviewModel.NetTotal),
            ];

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
            purchaseReturnItemData,
            "PURCHASE RETURN ITEM REPORT",
            "Purchase Return Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder
        );
    }
}
