using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen Issue Item Report
/// </summary>
public static class KitchenIssueItemReportPDFExport
{
    /// <summary>
    /// Export Kitchen Issue Item Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenIssueItemData">Collection of kitchen issue item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="showSummary">Whether to show summary grouped by item</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenIssueItemReport(
        IEnumerable<KitchenIssueItemOverviewModel> kitchenIssueItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        bool showSummary = false)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

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
        // All columns - detailed view (matching Excel export)
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
        // Summary columns - key fields only (matching Excel export)
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

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(KitchenIssueItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks)] = new() { DisplayName = "Kitchen Issue Remarks", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false };

        columnSettings[nameof(KitchenIssueItemOverviewModel.Quantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(KitchenIssueItemOverviewModel.Rate)] = new()
        {
            DisplayName = "Rate",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            },
            IncludeInTotal = false
        };

        columnSettings[nameof(KitchenIssueItemOverviewModel.Total)] = new()
        {
            DisplayName = "Total",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return await PDFReportExportUtil.ExportToPdf(
            kitchenIssueItemData,
            "KITCHEN ISSUE ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns && !showSummary  // Use landscape when showing all columns
        );
    }
}
