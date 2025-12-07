using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen Issue Report
/// </summary>
public static class KitchenIssueReportPDFExport
{
    /// <summary>
    /// Export Kitchen Issue Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenIssueData">Collection of kitchen issue overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="kitchenName">Optional kitchen name to display in header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenIssueReport(
        IEnumerable<KitchenIssueOverviewModel> kitchenIssueData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string kitchenName = null,
        bool showSummary = false)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting (matching Excel export)
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

        // All columns - detailed view (matching Excel export)
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
        // Summary columns - key fields only (matching Excel export)
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

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(KitchenIssueOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings[nameof(KitchenIssueOverviewModel.TotalItems)] = new()
        {
            DisplayName = "Items",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(KitchenIssueOverviewModel.TotalQuantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(KitchenIssueOverviewModel.TotalAmount)] = new()
        {
            DisplayName = "Total",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with landscape mode for all columns
        return await PDFReportExportUtil.ExportToPdf(
            kitchenIssueData,
            "KITCHEN ISSUE REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns && !showSummary,  // Use landscape when showing all columns
            partyName: kitchenName
        );
    }
}
