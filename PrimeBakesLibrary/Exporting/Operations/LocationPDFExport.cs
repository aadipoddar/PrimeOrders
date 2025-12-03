using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Operations;

/// <summary>
/// PDF export functionality for Location
/// </summary>
public static class LocationPDFExport
{
    /// <summary>
    /// Export Location data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="locationData">Collection of location records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportLocation(IEnumerable<LocationModel> locationData)
    {
        // Define custom column settings
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            ["Id"] = new() { DisplayName = "ID", IncludeInTotal = false },
            ["Name"] = new() { DisplayName = "Location Name", IncludeInTotal = false },
            ["PrefixCode"] = new() { DisplayName = "Prefix Code", IncludeInTotal = false },
            ["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            ["Discount"] = new()
            {
                DisplayName = "Discount %",
                Format = "#,##0.00",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Status"] = new()
            {
                DisplayName = "Status",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            }
        };

        // Define column order
        List<string> columnOrder =
        [
            "Id", "Name", "PrefixCode", "Discount", "Remarks", "Status"
        ];

        // Call the generic PDF export utility
        return await PDFReportExportUtil.ExportToPdf(
            locationData,
            "LOCATION MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );
    }
}
