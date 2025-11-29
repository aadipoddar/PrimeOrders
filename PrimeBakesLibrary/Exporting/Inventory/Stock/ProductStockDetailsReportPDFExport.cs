using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// PDF export functionality for Product Stock Details Report
/// </summary>
public static class ProductStockDetailsReportPDFExport
{
    /// <summary>
    /// Export Product Stock Details Report to PDF
    /// </summary>
    /// <param name="stockDetailsData">Collection of product stock details records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <param name="locationName">Optional location name to display in header</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportProductStockDetailsReport(
        IEnumerable<ProductStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null,
        string locationName = null)
    {
        // Define custom column settings for details
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order for details (no toggle - always same columns)
        var columnOrder = new List<string>
        {
            nameof(ProductStockDetailsModel.TransactionDate),
            nameof(ProductStockDetailsModel.TransactionNo),
            nameof(ProductStockDetailsModel.Type),
            nameof(ProductStockDetailsModel.ProductName),
            nameof(ProductStockDetailsModel.ProductCode),
            nameof(ProductStockDetailsModel.Quantity),
            nameof(ProductStockDetailsModel.NetRate)
        };

        // Customize specific columns for PDF display
        columnSettings[nameof(ProductStockDetailsModel.TransactionDate)] = new()
        {
            DisplayName = "Trans Date",
            Format = "dd-MMM-yyyy",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(ProductStockDetailsModel.TransactionNo)] = new()
        {
            DisplayName = "Trans No",
            IncludeInTotal = false
        };

        columnSettings[nameof(ProductStockDetailsModel.Type)] = new()
        {
            DisplayName = "Trans Type",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(ProductStockDetailsModel.ProductName)] = new()
        {
            DisplayName = "Product",
            IncludeInTotal = false
        };

        columnSettings[nameof(ProductStockDetailsModel.ProductCode)] = new()
        {
            DisplayName = "Code",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(ProductStockDetailsModel.Quantity)] = new()
        {
            DisplayName = "Qty",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(ProductStockDetailsModel.NetRate)] = new()
        {
            DisplayName = "Net Rate",
            Format = "#,##0.00",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        // Call the generic PDF export utility with portrait mode
        return PDFReportExportUtil.ExportToPdf(
            stockDetailsData,
            "PRODUCT STOCK TRANSACTION DETAILS",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: false,  // Use portrait orientation for details view
            locationName: locationName
        );
    }
}
