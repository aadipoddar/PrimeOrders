using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen Production Item Report
/// </summary>
public static class KitchenProductionItemReportPDFExport
{
    /// <summary>
    /// Export Kitchen Production Item Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenProductionItemData">Collection of kitchen production item overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenProductionItemReport(
        IEnumerable<KitchenProductionItemOverviewModel> kitchenProductionItemData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting (matching Excel export)
        List<string> columnOrder;

        // All columns - detailed view (matching Excel export)
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
        // Summary columns - key fields only (matching Excel export)
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

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(KitchenProductionItemOverviewModel.ItemName)] = new() { DisplayName = "Product", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks)] = new() { DisplayName = "Kitchen Production Remarks", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionItemOverviewModel.Remarks)] = new() { DisplayName = "Product Remarks", IncludeInTotal = false };

        columnSettings[nameof(KitchenProductionItemOverviewModel.Quantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(KitchenProductionItemOverviewModel.Rate)] = new()
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

        columnSettings[nameof(KitchenProductionItemOverviewModel.Total)] = new()
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
            kitchenProductionItemData,
            "KITCHEN PRODUCTION ITEM REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns  // Use landscape when showing all columns
        );
    }
}
