using PrimeBakesLibrary.Exporting.Utils;
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
    /// <param name="showSummary">Whether to show summary grouped by item</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportPurchaseItemReport(
        IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(PurchaseItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.PartyId)] = new() { DisplayName = "Party ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            [nameof(PurchaseItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.PurchaseRemarks)] = new() { DisplayName = "Purchase Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(PurchaseItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            [nameof(PurchaseItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            [nameof(PurchaseItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(PurchaseItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },
            [nameof(PurchaseItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true },

			// Percentage fields - Center aligned
			[nameof(PurchaseItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(PurchaseItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Boolean fields
            [nameof(PurchaseItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order based on showAllColumns and showSummary flags
        List<string> columnOrder;

        // Summary mode - grouped by item with aggregated values
        if (showSummary)
        {
            columnOrder =
            [
                nameof(PurchaseItemOverviewModel.ItemName),
                nameof(PurchaseItemOverviewModel.ItemCode),
                nameof(PurchaseItemOverviewModel.ItemCategoryName),
                nameof(PurchaseItemOverviewModel.Quantity),
                nameof(PurchaseItemOverviewModel.BaseTotal),
                nameof(PurchaseItemOverviewModel.DiscountAmount),
                nameof(PurchaseItemOverviewModel.AfterDiscount),
                nameof(PurchaseItemOverviewModel.SGSTAmount),
                nameof(PurchaseItemOverviewModel.CGSTAmount),
                nameof(PurchaseItemOverviewModel.IGSTAmount),
                nameof(PurchaseItemOverviewModel.TotalTaxAmount),
                nameof(PurchaseItemOverviewModel.Total),
                nameof(PurchaseItemOverviewModel.NetTotal)
            ];
        }
        // All columns in logical order
        else if (showAllColumns)
            columnOrder =
            [
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.ItemCategoryName),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.CompanyName),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.Rate),
				nameof(PurchaseItemOverviewModel.BaseTotal),
				nameof(PurchaseItemOverviewModel.DiscountPercent),
				nameof(PurchaseItemOverviewModel.DiscountAmount),
				nameof(PurchaseItemOverviewModel.AfterDiscount),
				nameof(PurchaseItemOverviewModel.SGSTPercent),
				nameof(PurchaseItemOverviewModel.SGSTAmount),
				nameof(PurchaseItemOverviewModel.CGSTPercent),
				nameof(PurchaseItemOverviewModel.CGSTAmount),
				nameof(PurchaseItemOverviewModel.IGSTPercent),
				nameof(PurchaseItemOverviewModel.IGSTAmount),
				nameof(PurchaseItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseItemOverviewModel.InclusiveTax),
				nameof(PurchaseItemOverviewModel.Total),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal),
				nameof(PurchaseItemOverviewModel.PurchaseRemarks),
				nameof(PurchaseItemOverviewModel.Remarks)
			];

        // Summary columns only
        else
            columnOrder =
            [
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal)
			];

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
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
