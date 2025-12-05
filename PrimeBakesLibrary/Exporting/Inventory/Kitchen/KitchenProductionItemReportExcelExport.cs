using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Excel export functionality for Kitchen Production Item Report
/// </summary>
public static class KitchenProductionItemReportExcelExport
{
    /// <summary>
    /// Export Kitchen Production Item Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="kitchenProductionItemData">Collection of kitchen production item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportKitchenProductionItemReport(
        IEnumerable<KitchenProductionItemOverviewModel> kitchenProductionItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(KitchenProductionItemOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.MasterId)] = new() { DisplayName = "Master ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.ItemCategoryId)] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionItemOverviewModel.KitchenId)] = new() { DisplayName = "Kitchen ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            // Text fields
            [nameof(KitchenProductionItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks)] = new() { DisplayName = "Kitchen Production Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            // Date fields
            [nameof(KitchenProductionItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            [nameof(KitchenProductionItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenProductionItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            [nameof(KitchenProductionItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // All columns in logical order
        if (showAllColumns)
            columnOrder =
            [
                nameof(KitchenProductionItemOverviewModel.ItemName),
                nameof(KitchenProductionItemOverviewModel.ItemCode),
                nameof(KitchenProductionItemOverviewModel.ItemCategoryName),
                nameof(KitchenProductionItemOverviewModel.TransactionNo),
                nameof(KitchenProductionItemOverviewModel.TransactionDateTime),
                nameof(KitchenProductionItemOverviewModel.CompanyName),
                nameof(KitchenProductionItemOverviewModel.KitchenName),
                nameof(KitchenProductionItemOverviewModel.Quantity),
                nameof(KitchenProductionItemOverviewModel.Rate),
                nameof(KitchenProductionItemOverviewModel.Total),
                nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks),
                nameof(KitchenProductionItemOverviewModel.Remarks)
            ];

        // Summary columns only
        else
            columnOrder =
            [
                nameof(KitchenProductionItemOverviewModel.ItemName),
                nameof(KitchenProductionItemOverviewModel.ItemCode),
                nameof(KitchenProductionItemOverviewModel.TransactionNo),
                nameof(KitchenProductionItemOverviewModel.TransactionDateTime),
                nameof(KitchenProductionItemOverviewModel.KitchenName),
                nameof(KitchenProductionItemOverviewModel.Quantity),
                nameof(KitchenProductionItemOverviewModel.Rate),
                nameof(KitchenProductionItemOverviewModel.Total)
            ];

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
            kitchenProductionItemData,
            "KITCHEN PRODUCTION ITEM REPORT",
            "Kitchen Production Item Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder
        );
    }
}
