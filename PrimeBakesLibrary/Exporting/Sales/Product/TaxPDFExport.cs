using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

/// <summary>
/// PDF export functionality for Tax
/// </summary>
public static class TaxPDFExport
{
    /// <summary>
    /// Export tax data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="taxData">Collection of tax records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportTax(IEnumerable<TaxModel> taxData)
    {
        // Create enriched data with status formatting
        var enrichedData = taxData.Select(tax => new
        {
            tax.Id,
            tax.Code,
            CGST = $"{tax.CGST:N2}%",
            SGST = $"{tax.SGST:N2}%",
            IGST = $"{tax.IGST:N2}%",
            Total = $"{(tax.CGST + tax.SGST):N2}%",
            Inclusive = tax.Inclusive ? "Yes" : "No",
            Extra = tax.Extra ? "Yes" : "No",
            tax.Remarks,
            Status = tax.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(TaxModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(TaxModel.Code)] = new() { DisplayName = "Tax Code", IncludeInTotal = false },

            [nameof(TaxModel.CGST)] = new()
            {
                DisplayName = "CGST %",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(TaxModel.SGST)] = new()
            {
                DisplayName = "SGST %",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(TaxModel.IGST)] = new()
            {
                DisplayName = "IGST %",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Total"] = new()
            {
                DisplayName = "Total GST %",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(TaxModel.Inclusive)] = new()
            {
                DisplayName = "Inclusive",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(TaxModel.Extra)] = new()
            {
                DisplayName = "Extra",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(TaxModel.Status)] = new()
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
			nameof(TaxModel.Id),
			nameof(TaxModel.Code),
			nameof(TaxModel.CGST),
			nameof(TaxModel.SGST),
			nameof(TaxModel.IGST),
			"Total",
			nameof(TaxModel.Inclusive),
			nameof(TaxModel.Extra),
			nameof(TaxModel.Remarks),
			nameof(TaxModel.Status)
		];

        // Call the generic PDF export utility
        return await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "TAX MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: true
        );
    }
}
