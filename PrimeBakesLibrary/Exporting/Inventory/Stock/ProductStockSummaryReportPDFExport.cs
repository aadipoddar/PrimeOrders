using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// PDF export functionality for Product Stock Report
/// </summary>
public static class ProductStockSummaryReportPDFExport
{
    /// <summary>
    /// Export Product Stock Report to PDF with custom column order and formatting
    /// </summary>
    /// <param name="stockData">Collection of product stock summary records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="showAllColumns">Whether to include all columns or just summary columns</param>
    /// <param name="locationName">Optional location name to display in header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportProductStockReport(
        IEnumerable<ProductStockSummaryModel> stockData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        bool showAllColumns = true,
        string locationName = null)
    {
        // Define custom column settings matching Excel export
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order based on visibility setting (matching Excel export)
        List<string> columnOrder;

            // All columns - detailed view (matching Excel export)
        if (showAllColumns)
            columnOrder =
            [
                nameof(ProductStockSummaryModel.ProductName),
                nameof(ProductStockSummaryModel.ProductCode),
                nameof(ProductStockSummaryModel.ProductCategoryName),
                nameof(ProductStockSummaryModel.OpeningStock),
                nameof(ProductStockSummaryModel.PurchaseStock),
                nameof(ProductStockSummaryModel.SaleStock),
                nameof(ProductStockSummaryModel.MonthlyStock),
                nameof(ProductStockSummaryModel.ClosingStock),
                nameof(ProductStockSummaryModel.Rate),
                nameof(ProductStockSummaryModel.ClosingValue),
                nameof(ProductStockSummaryModel.AveragePrice),
                nameof(ProductStockSummaryModel.WeightedAverageValue),
                nameof(ProductStockSummaryModel.LastSalePrice),
                nameof(ProductStockSummaryModel.LastSaleValue)
            ];
            // Summary columns - key fields only (matching Excel export)
        else
            columnOrder =
            [
                nameof(ProductStockSummaryModel.ProductName),
                nameof(ProductStockSummaryModel.OpeningStock),
                nameof(ProductStockSummaryModel.PurchaseStock),
                nameof(ProductStockSummaryModel.SaleStock),
                nameof(ProductStockSummaryModel.ClosingStock),
                nameof(ProductStockSummaryModel.Rate),
                nameof(ProductStockSummaryModel.ClosingValue)
            ];

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings[nameof(ProductStockSummaryModel.ProductName)] = new() { DisplayName = "Product", IncludeInTotal = false };
        columnSettings[nameof(ProductStockSummaryModel.ProductCode)] = new() { DisplayName = "Code", IncludeInTotal = false };
        columnSettings[nameof(ProductStockSummaryModel.ProductCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false };

        // Stock quantity fields - All with totals
        columnSettings[nameof(ProductStockSummaryModel.OpeningStock)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.PurchaseStock)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.SaleStock)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.MonthlyStock)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.ClosingStock)] = new()
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
        columnSettings[nameof(ProductStockSummaryModel.Rate)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.AveragePrice)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.LastSalePrice)] = new()
        {
            DisplayName = "Last Sale Price",
            Format = "#,##0.00",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Value fields - All with totals
        columnSettings[nameof(ProductStockSummaryModel.ClosingValue)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.WeightedAverageValue)] = new()
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

        columnSettings[nameof(ProductStockSummaryModel.LastSaleValue)] = new()
        {
            DisplayName = "Last Sale Value",
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
            "PRODUCT STOCK REPORT",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: showAllColumns,  // Use landscape when showing all columns
            locationName: locationName
        );
    }
}
