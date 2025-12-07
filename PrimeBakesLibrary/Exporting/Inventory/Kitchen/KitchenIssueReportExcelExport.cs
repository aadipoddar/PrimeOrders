using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Excel export functionality for Kitchen Issue Report
/// </summary>
public static class KitchenIssueReportExcelExport
{
    /// <summary>
    /// Export Kitchen Issue Report to Excel with custom column order and formatting
    /// </summary>
    /// <param name="kitchenIssueData">Collection of kitchen issue overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="kitchenName">Optional kitchen name to display in header</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportKitchenIssueReport(
        IEnumerable<KitchenIssueOverviewModel> kitchenIssueData,
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
            [nameof(KitchenIssueOverviewModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CompanyId)] = new() { DisplayName = "Company ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.KitchenId)] = new() { DisplayName = "Kitchen ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.FinancialYearId)] = new() { DisplayName = "Financial Year ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.CreatedBy)] = new() { DisplayName = "Created By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(KitchenIssueOverviewModel.LastModifiedBy)] = new() { DisplayName = "Modified By ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Text fields
            [nameof(KitchenIssueOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenIssueOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
            [nameof(KitchenIssueOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Date fields
            [nameof(KitchenIssueOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenIssueOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
            [nameof(KitchenIssueOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },

            // Numeric fields - Items and Quantities
            [nameof(KitchenIssueOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
            [nameof(KitchenIssueOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

            // Amount field
            [nameof(KitchenIssueOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true, HighlightNegative = true }
        };

        // Define column order based on showAllColumns flag
        List<string> columnOrder;

        // Summary view - grouped by location with totals
        if (showSummary)
            columnOrder =
            [
                nameof(KitchenIssueOverviewModel.KitchenName),
                nameof(KitchenIssueOverviewModel.TotalItems),
                nameof(KitchenIssueOverviewModel.TotalQuantity),
                nameof(KitchenIssueOverviewModel.TotalAmount)
            ];

        // All columns in logical order
        else if (showAllColumns)
        {
            columnOrder =
            [
                nameof(KitchenIssueOverviewModel.TransactionNo),
                nameof(KitchenIssueOverviewModel.TransactionDateTime),
                nameof(KitchenIssueOverviewModel.CompanyName),
                nameof(KitchenIssueOverviewModel.FinancialYear),
                nameof(KitchenIssueOverviewModel.TotalItems),
                nameof(KitchenIssueOverviewModel.TotalQuantity),
                nameof(KitchenIssueOverviewModel.TotalAmount),
                nameof(KitchenIssueOverviewModel.Remarks),
                nameof(KitchenIssueOverviewModel.CreatedByName),
                nameof(KitchenIssueOverviewModel.CreatedAt),
                nameof(KitchenIssueOverviewModel.CreatedFromPlatform),
                nameof(KitchenIssueOverviewModel.LastModifiedByUserName),
                nameof(KitchenIssueOverviewModel.LastModifiedAt),
                nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)
            ];

            // Add kitchen column only if not filtering by kitchen
            if (string.IsNullOrEmpty(kitchenName))
                columnOrder.Insert(3, nameof(KitchenIssueOverviewModel.KitchenName));
        }

        // Summary columns only
        else
        {
            columnOrder =
            [
                nameof(KitchenIssueOverviewModel.TransactionNo),
                nameof(KitchenIssueOverviewModel.TransactionDateTime),
                nameof(KitchenIssueOverviewModel.TotalQuantity),
                nameof(KitchenIssueOverviewModel.TotalAmount)
            ];

            // Add kitchen column only if not filtering by kitchen
            if (string.IsNullOrEmpty(kitchenName))
                columnOrder.Insert(2, nameof(KitchenIssueOverviewModel.KitchenName));
        }

        // Export using the generic utility
        return await ExcelReportExportUtil.ExportToExcel(
        kitchenIssueData,
        "KITCHEN ISSUE REPORT",
        "Kitchen Issue Transactions",
        dateRangeStart,
        dateRangeEnd,
        columnSettings,
        columnOrder,
        partyName: kitchenName
    );
    }
}
