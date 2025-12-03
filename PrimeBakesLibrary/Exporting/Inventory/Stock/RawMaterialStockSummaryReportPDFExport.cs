using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// PDF export functionality for Raw Material Stock Report
/// </summary>
public static class RawMaterialStockSummaryReportPDFExport
{
    /// <summary>
    /// Export Raw Material Stock Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="stockData">Collection of raw material stock summary records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportRawMaterialStockReport(
        IEnumerable<RawMaterialStockSummaryModel> stockData,
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
                nameof(RawMaterialStockSummaryModel.RawMaterialName),
                nameof(RawMaterialStockSummaryModel.RawMaterialCode),
                nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName),
                nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
                nameof(RawMaterialStockSummaryModel.OpeningStock),
                nameof(RawMaterialStockSummaryModel.PurchaseStock),
                nameof(RawMaterialStockSummaryModel.SaleStock),
                nameof(RawMaterialStockSummaryModel.MonthlyStock),
                nameof(RawMaterialStockSummaryModel.ClosingStock),
                nameof(RawMaterialStockSummaryModel.Rate),
                nameof(RawMaterialStockSummaryModel.ClosingValue),
                nameof(RawMaterialStockSummaryModel.AveragePrice),
                nameof(RawMaterialStockSummaryModel.WeightedAverageValue),
                nameof(RawMaterialStockSummaryModel.LastPurchasePrice),
                nameof(RawMaterialStockSummaryModel.LastPurchaseValue)
            ];
        // Summary columns - key fields only (matching Excel export)
        else
            columnOrder =
            [
                nameof(RawMaterialStockSummaryModel.RawMaterialName),
                nameof(RawMaterialStockSummaryModel.UnitOfMeasurement),
                nameof(RawMaterialStockSummaryModel.OpeningStock),
                nameof(RawMaterialStockSummaryModel.PurchaseStock),
                nameof(RawMaterialStockSummaryModel.SaleStock),
                nameof(RawMaterialStockSummaryModel.ClosingStock),
                nameof(RawMaterialStockSummaryModel.Rate),
                nameof(RawMaterialStockSummaryModel.ClosingValue)
            ];

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(RawMaterialStockSummaryModel.RawMaterialName)] = new() { DisplayName = "Raw Material", IncludeInTotal = false };
        columnSettings[nameof(RawMaterialStockSummaryModel.RawMaterialCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };
        columnSettings[nameof(RawMaterialStockSummaryModel.UnitOfMeasurement)] = new()
        {
            DisplayName = "UOM",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Stock quantity fields - All with totals
        columnSettings[nameof(RawMaterialStockSummaryModel.OpeningStock)] = new()
        {
            DisplayName = "Opening Stock",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new()
        {
            DisplayName = "Purchase Stock",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.SaleStock)] = new()
        {
            DisplayName = "Sale Stock",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new()
        {
            DisplayName = "Monthly Stock",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.ClosingStock)] = new()
        {
            DisplayName = "Closing Stock",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Rate/Price fields - Right aligned, no totals
        columnSettings[nameof(RawMaterialStockSummaryModel.Rate)] = new()
        {
            DisplayName = "Rate",
            Format = "#,##0.00",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.AveragePrice)] = new()
        {
            DisplayName = "Average Price",
            Format = "#,##0.00",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.LastPurchasePrice)] = new()
        {
            DisplayName = "Last Purchase Price",
            Format = "#,##0.00",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Value fields - All with totals
        columnSettings[nameof(RawMaterialStockSummaryModel.ClosingValue)] = new()
        {
            DisplayName = "Closing Value",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.WeightedAverageValue)] = new()
        {
            DisplayName = "Weighted Avg Value",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockSummaryModel.LastPurchaseValue)] = new()
        {
            DisplayName = "Last Purchase Value",
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
            stockData,
            "RAW MATERIAL STOCK REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns  // Use landscape when showing all columns
        );
    }
}
