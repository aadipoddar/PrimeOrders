using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Inventory.Stock;

/// <summary>
/// PDF export functionality for Raw Material Stock Details Report
/// </summary>
public static class RawMaterialStockDetailsReportPDFExport
{
    /// <summary>
    /// Export Raw Material Stock Details Report to PDF
    /// </summary>
    /// <param name="stockDetailsData">Collection of raw material stock details records</param>
    /// <param name="dateRangeStart">Start date of the report</param>
    /// <param name="dateRangeEnd">End date of the report</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportRawMaterialStockDetailsReport(
        IEnumerable<RawMaterialStockDetailsModel> stockDetailsData,
        DateOnly? dateRangeStart = null,
        DateOnly? dateRangeEnd = null)
    {
        // Define custom column settings for details
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>();

        // Define column order for details (no toggle - always same columns)
        var columnOrder = new List<string>
        {
            nameof(RawMaterialStockDetailsModel.TransactionDateTime),
            nameof(RawMaterialStockDetailsModel.TransactionNo),
            nameof(RawMaterialStockDetailsModel.Type),
            nameof(RawMaterialStockDetailsModel.RawMaterialName),
            nameof(RawMaterialStockDetailsModel.RawMaterialCode),
            nameof(RawMaterialStockDetailsModel.Quantity),
            nameof(RawMaterialStockDetailsModel.NetRate)
        };

        // Customize specific columns for PDF display
        columnSettings[nameof(RawMaterialStockDetailsModel.TransactionDateTime)] = new()
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

        columnSettings[nameof(RawMaterialStockDetailsModel.TransactionNo)] = new()
        {
            DisplayName = "Trans No",
            IncludeInTotal = false
        };

        columnSettings[nameof(RawMaterialStockDetailsModel.Type)] = new()
        {
            DisplayName = "Trans Type",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new()
        {
            DisplayName = "Raw Material",
            IncludeInTotal = false
        };

        columnSettings[nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new()
        {
            DisplayName = "Code",
            IncludeInTotal = false,
            StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
            {
                Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
            }
        };

        columnSettings[nameof(RawMaterialStockDetailsModel.Quantity)] = new()
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

        columnSettings[nameof(RawMaterialStockDetailsModel.NetRate)] = new()
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
        return await PDFReportExportUtil.ExportToPdf(
            stockDetailsData,
            "RAW MATERIAL STOCK TRANSACTION DETAILS",
            dateRangeStart,
            dateRangeEnd,
            columnSettings,
            columnOrder,
            useLandscape: false  // Use portrait orientation for details view
        );
    }
}
