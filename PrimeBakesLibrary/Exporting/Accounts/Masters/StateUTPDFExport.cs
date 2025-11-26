using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for State/UT
/// </summary>
public static class StateUTPDFExport
{
    /// <summary>
    /// Export state/UT data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="stateUTData">Collection of state/UT records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportStateUT(IEnumerable<StateUTModel> stateUTData)
    {
        // Create enriched data with status formatting
        var enrichedData = stateUTData.Select(stateUT => new
        {
            stateUT.Id,
            stateUT.Name,
            stateUT.Remarks,
            UnionTerritory = stateUT.UnionTerritory ? "Yes" : "No",
            Status = stateUT.Status ? "Active" : "Deleted"
        });        // Define custom column settings
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            ["Id"] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Name"] = new() { DisplayName = "State/UT Name", IncludeInTotal = false },

            ["UnionTerritory"] = new()
            {
                DisplayName = "Union Territory",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
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
            "Id", "Name", "Remarks", "UnionTerritory", "Status"
        ];        // Call the generic PDF export utility
        return PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "STATE & UNION TERRITORY MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );
    }
}
