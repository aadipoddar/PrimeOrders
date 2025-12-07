using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Excel export functionality for Kitchen Production Report
/// </summary>
public static class KitchenProductionReportExcelExport
{
    /// <summary>
    /// Export Kitchen Production Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="kitchenProductionData">Collection of kitchen production overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="kitchenName">Optional kitchen name to display in header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportKitchenProductionReport(
        IEnumerable<KitchenProductionOverviewModel> kitchenProductionData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string kitchenName = null,
        bool showSummary = false)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // IDs - Center aligned, no totals
            [nameof(KitchenProductionOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.KitchenId)] = new() { DisplayName = "Kitchen ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenProductionOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            [nameof(KitchenProductionOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenProductionOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenProductionOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Date fields
            [nameof(KitchenProductionOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenProductionOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenProductionOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Items and Quantities
            [nameof(KitchenProductionOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenProductionOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount field
            [nameof(KitchenProductionOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        if (showSummary)
            // Summary view - grouped by kitchen with totals
            columnOrder =
            [
                nameof(KitchenProductionOverviewModel.KitchenName),
                nameof(KitchenProductionOverviewModel.TotalItems),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount)
            ];

        // All columns in logical order
        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(KitchenProductionOverviewModel.TransactionNo),
                nameof(KitchenProductionOverviewModel.TransactionDateTime),
                nameof(KitchenProductionOverviewModel.CompanyName),
                nameof(KitchenProductionOverviewModel.FinancialYear),
                nameof(KitchenProductionOverviewModel.TotalItems),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount),
                nameof(KitchenProductionOverviewModel.Remarks),
                nameof(KitchenProductionOverviewModel.CreatedByName),
                nameof(KitchenProductionOverviewModel.CreatedAt),
                nameof(KitchenProductionOverviewModel.CreatedFromPlatform),
                nameof(KitchenProductionOverviewModel.LastModifiedByUserName),
                nameof(KitchenProductionOverviewModel.LastModifiedAt),
                nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform),
            ];

            // Add kitchen column only if not filtering by kitchen
            if (string.IsNullOrEmpty(kitchenName))
                columnOrder.Insert(3, nameof(KitchenProductionOverviewModel.KitchenName));
        }

        // Summary columns only
        else
        {
            columnOrder =
            [
                nameof(KitchenProductionOverviewModel.TransactionNo),
                nameof(KitchenProductionOverviewModel.TransactionDateTime),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount)
            ];

            // Add kitchen column only if not filtering by kitchen
            if (string.IsNullOrEmpty(kitchenName))
                columnOrder.Insert(2, nameof(KitchenProductionOverviewModel.KitchenName));
        }

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
            kitchenProductionData,
            "KITCHEN PRODUCTION REPORT",
            "Kitchen Production Transactions",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            partyName: kitchenName
        );
    }
}
