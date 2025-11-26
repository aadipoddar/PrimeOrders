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
    public static MemoryStream ExportProductStockReport(
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

        if (showAllColumns)
        {
            // All columns - detailed view (matching Excel export)
            columnOrder =
            [
                "ProductName",
                "ProductCode",
                "ProductCategoryName",
                "OpeningStock",
                "PurchaseStock",
                "SaleStock",
                "MonthlyStock",
                "ClosingStock",
                "Rate",
                "ClosingValue",
                "AveragePrice",
                "WeightedAverageValue",
                "LastSalePrice",
                "LastSaleValue"
            ];
        }
        else
        {
            // Summary columns - key fields only (matching Excel export)
            columnOrder =
            [
                "ProductName",
                "OpeningStock",
                "PurchaseStock",
                "SaleStock",
                "ClosingStock",
                "Rate",
                "ClosingValue"
            ];
        }

        // Customize specific columns for PDF display (matching Excel column names)
        columnSettings["ProductName"] = new() { DisplayName = "Product Name", IncludeInTotal = false };
        columnSettings["ProductCode"] = new() { DisplayName = "Product Code", IncludeInTotal = false };
        columnSettings["ProductCategoryName"] = new() { DisplayName = "Category", IncludeInTotal = false };

        // Stock quantity fields - All with totals
        columnSettings["OpeningStock"] = new()
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

        columnSettings["PurchaseStock"] = new()
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

        columnSettings["SaleStock"] = new()
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

        columnSettings["MonthlyStock"] = new()
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

        columnSettings["ClosingStock"] = new()
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
        columnSettings["Rate"] = new()
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

        columnSettings["AveragePrice"] = new()
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

        columnSettings["LastSalePrice"] = new()
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
        columnSettings["ClosingValue"] = new()
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

        columnSettings["WeightedAverageValue"] = new()
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

        columnSettings["LastSaleValue"] = new()
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
        return PDFReportExportUtil.ExportToPdf(
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
