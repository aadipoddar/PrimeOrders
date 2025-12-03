using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// PDF export functionality for Kitchen
/// </summary>
public static class KitchenPDFExport
{
    /// <summary>
    /// Export Kitchen data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="kitchenData">Collection of kitchen records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchen(IEnumerable<KitchenModel> kitchenData)
    {
        // Create enriched data with status formatting
        var enrichedData = kitchenData.Select(kitchen => new
        {
            kitchen.Id,
            kitchen.Name,
            kitchen.Remarks,
            Status = kitchen.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
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

            ["Name"] = new() { DisplayName = "Kitchen Name", IncludeInTotal = false },
            ["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false },

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
            "Id", "Name", "Remarks", "Status"
        ];

        // Call the generic PDF export utility
        return await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "KITCHEN MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );
    }
}
