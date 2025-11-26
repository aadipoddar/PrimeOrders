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
            "TransactionDate",
            "TransactionNo",
            "Type",
            "ProductName",
            "ProductCode",
            "Quantity",
            "NetRate"
        };

        // Customize specific columns for PDF display
        columnSettings["TransactionDate"] = new()
        {
            DisplayName = "Transaction Date",
            Format = "dd-MMM-yyyy",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["TransactionNo"] = new()
        {
            DisplayName = "Transaction No",
            IncludeInTotal = false
        };

        columnSettings["Type"] = new()
        {
            DisplayName = "Transaction Type",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["ProductName"] = new()
        {
            DisplayName = "Product Name",
            IncludeInTotal = false
        };

        columnSettings["ProductCode"] = new()
        {
            DisplayName = "Product Code",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["Quantity"] = new()
        {
            DisplayName = "Quantity",
            Format = "#,##0.00",
            HighlightNegative = true,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings["NetRate"] = new()
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
