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
    public static MemoryStream ExportKitchenProductionItemReport(
        IEnumerable<KitchenProductionItemOverviewModel> kitchenProductionItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["KitchenProductionId"] = new() { DisplayName = "Kitchen Production ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["ItemCategoryId"] = new() { DisplayName = "Category ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["CompanyId"] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["KitchenId"] = new() { DisplayName = "Kitchen ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            ["ItemName"] = new() { DisplayName = "Item Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["ItemCode"] = new() { DisplayName = "Item Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["ItemCategoryName"] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["TransactionNo"] = new() { DisplayName = "Transaction No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["CompanyName"] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["KitchenName"] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["KitchenProductionRemarks"] = new() { DisplayName = "Kitchen Production Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            ["Remarks"] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Date fields
            ["TransactionDateTime"] = new() { DisplayName = "Transaction Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Quantity
            ["Quantity"] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            ["Rate"] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

            // Amount fields - All with N2 format and totals
            ["Total"] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true }
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
                "KitchenName",
                "Quantity",
                "Rate",
                "Total",
                "KitchenProductionRemarks",
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
                "KitchenName",
                "Quantity",
                "Rate",
                "Total"
            ];

        // Export using the generic utility
        return ExcelExportUtil.ExportToExcel(
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
