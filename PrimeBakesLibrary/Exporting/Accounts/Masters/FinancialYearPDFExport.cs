using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for Financial Year
/// </summary>
public static class FinancialYearPDFExport
{
    /// <summary>
    /// Export financial year data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="financialYearData">Collection of financial year records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static MemoryStream ExportFinancialYear(IEnumerable<FinancialYearModel> financialYearData)
    {
        // Create enriched data with status formatting
        var enrichedData = financialYearData.Select(fy => new
        {
            fy.Id,
            StartDate = fy.StartDate.ToString("dd-MMM-yyyy"),
            EndDate = fy.EndDate.ToString("dd-MMM-yyyy"),
            fy.YearNo,
            fy.Remarks,
            Locked = fy.Locked ? "Yes" : "No",
            Status = fy.Status ? "Active" : "Deleted"
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

            ["StartDate"] = new()
            {
                DisplayName = "Start Date",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["EndDate"] = new()
            {
                DisplayName = "End Date",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["YearNo"] = new()
            {
                DisplayName = "Year No",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            ["Locked"] = new()
            {
                DisplayName = "Locked",
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
            "Id", "StartDate", "EndDate", "YearNo", "Remarks", "Locked", "Status"
        ];

        // Call the generic PDF export utility
        return PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "FINANCIAL YEAR MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: false
        );
    }
}
