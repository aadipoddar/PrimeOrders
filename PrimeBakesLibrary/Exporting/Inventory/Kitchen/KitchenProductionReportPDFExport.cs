using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen Production Report
/// </summary>
public static class KitchenProductionReportPDFExport
{
    /// <summary>
    /// Export Kitchen Production Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenProductionData">Collection of kitchen production overview records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="kitchenName">Optional kitchen name to display in header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenProductionReport(
        IEnumerable<KitchenProductionOverviewModel> kitchenProductionData,
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

        if (showSummary)
            columnOrder =
            [
                nameof(KitchenProductionOverviewModel.KitchenName),
                nameof(KitchenProductionOverviewModel.TotalItems),
                nameof(KitchenProductionOverviewModel.TotalQuantity),
                nameof(KitchenProductionOverviewModel.TotalAmount)
            ];

        // All columns - detailed view (matching Excel export)
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
                nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform)
            ];

            // Add kitchen column only if not filtering by kitchen
            if (string.IsNullOrEmpty(kitchenName))
                columnOrder.Insert(3, nameof(KitchenProductionOverviewModel.KitchenName));
        }
        // Summary columns - key fields only (matching Excel export)
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

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(KitchenProductionOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false };
        columnSettings[nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false };

        columnSettings[nameof(KitchenProductionOverviewModel.TotalItems)] = new()
        {
            DisplayName = "Items",
            Format = "#,##0",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(KitchenProductionOverviewModel.TotalQuantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(KitchenProductionOverviewModel.TotalAmount)] = new()
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
            kitchenProductionData,
            "KITCHEN PRODUCTION REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns && !showSummary,  // Use landscape when showing all columns
            partyName: kitchenName
        );
    }
}
