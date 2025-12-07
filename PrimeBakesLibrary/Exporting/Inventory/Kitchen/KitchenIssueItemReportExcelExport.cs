using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Excel export functionality for Kitchen Issue Item Report
/// </summary>
public static class KitchenIssueItemReportExcelExport
{
    /// <summary>
    /// Export Kitchen Issue Item Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="kitchenIssueItemData">Collection of kitchen issue item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showSummary">Whether to show summary grouped by item</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportKitchenIssueItemReport(
        IEnumerable<KitchenIssueItemOverviewModel> kitchenIssueItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(KitchenIssueItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueItemOverviewModel.KitchenId)] = new() { DisplayName = "Kitchen ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            // Text fields
            [nameof(KitchenIssueItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks)] = new() { DisplayName = "Kitchen Issue Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            // Date fields
            [nameof(KitchenIssueItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            [nameof(KitchenIssueItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenIssueItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(KitchenIssueItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on showAllColumns and showSummary flags
        List<string> columnOrder;

        // Summary mode - grouped by item with aggregated values
        if (showSummary)
            columnOrder =
            [
                nameof(KitchenIssueItemOverviewModel.ItemName),
                nameof(KitchenIssueItemOverviewModel.ItemCode),
                nameof(KitchenIssueItemOverviewModel.ItemCategoryName),
                nameof(KitchenIssueItemOverviewModel.Quantity),
                nameof(KitchenIssueItemOverviewModel.Total)
            ];

        // All columns in logical order
        else if (showAllColumns)
            columnOrder =
            [
                nameof(KitchenIssueItemOverviewModel.ItemName),
                nameof(KitchenIssueItemOverviewModel.ItemCode),
                nameof(KitchenIssueItemOverviewModel.ItemCategoryName),
                nameof(KitchenIssueItemOverviewModel.TransactionNo),
                nameof(KitchenIssueItemOverviewModel.TransactionDateTime),
                nameof(KitchenIssueItemOverviewModel.CompanyName),
                nameof(KitchenIssueItemOverviewModel.KitchenName),
                nameof(KitchenIssueItemOverviewModel.Quantity),
                nameof(KitchenIssueItemOverviewModel.Rate),
                nameof(KitchenIssueItemOverviewModel.Total),
                nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks),
                nameof(KitchenIssueItemOverviewModel.Remarks)
            ];

        // Summary columns only
        else
            columnOrder =
            [
                nameof(KitchenIssueItemOverviewModel.ItemName),
                nameof(KitchenIssueItemOverviewModel.ItemCode),
                nameof(KitchenIssueItemOverviewModel.TransactionNo),
                nameof(KitchenIssueItemOverviewModel.TransactionDateTime),
                nameof(KitchenIssueItemOverviewModel.KitchenName),
                nameof(KitchenIssueItemOverviewModel.Quantity),
                nameof(KitchenIssueItemOverviewModel.Rate),
                nameof(KitchenIssueItemOverviewModel.Total)
            ];

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
            kitchenIssueItemData,
            "KITCHEN ISSUE ITEM REPORT",
            "Kitchen Issue Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder
        );
    }
}
