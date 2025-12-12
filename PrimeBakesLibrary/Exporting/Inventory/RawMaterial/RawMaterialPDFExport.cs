using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

/// <summary>
/// PDF export functionality for Raw Material
/// </summary>
public static class RawMaterialPDFExport
{
    /// <summary>
    /// Export raw material data to PDF with custom column order and formatting
    /// </summary>
    /// <param name="rawMaterialData">Collection of raw material records</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportRawMaterial<T>(IEnumerable<T> rawMaterialData)
    {
        // Create enriched data with status formatting
        var enrichedData = rawMaterialData.Select(rm =>
        {
            var props = typeof(T).GetProperties();
            var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(rm);
            var name = props.FirstOrDefault(p => p.Name == "Name")?.GetValue(rm)?.ToString();
            var code = props.FirstOrDefault(p => p.Name == "Code")?.GetValue(rm)?.ToString();
            var category = props.FirstOrDefault(p => p.Name == "Category")?.GetValue(rm)?.ToString();
            var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(rm);
            var unit = props.FirstOrDefault(p => p.Name == "UnitOfMeasurement")?.GetValue(rm)?.ToString();
            var tax = props.FirstOrDefault(p => p.Name == "Tax")?.GetValue(rm)?.ToString();
            var remarks = props.FirstOrDefault(p => p.Name == "Remarks")?.GetValue(rm)?.ToString();
            var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(rm);

            return new
            {
                Id = id,
                Name = name,
                Code = code,
                Category = category,
                Rate = rate is decimal rateVal ? $"{rateVal:N2}" : "0.00",
                UnitOfMeasurement = unit,
                Tax = tax,
                Remarks = remarks,
                Status = status is bool and true ? "Active" : "Deleted"
            };
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(RawMaterialModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(RawMaterialModel.Name)] = new() { DisplayName = "Raw Material Name", IncludeInTotal = false },

            [nameof(RawMaterialModel.Code)] = new() { DisplayName = "Code", IncludeInTotal = false },

            ["Category"] = new() { DisplayName = "Category", IncludeInTotal = false },

            [nameof(RawMaterialModel.Rate)] = new()
            {
                DisplayName = "Rate",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(RawMaterialModel.UnitOfMeasurement)] = new()
            {
                DisplayName = "Unit",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            ["Tax"] = new()
            {
                DisplayName = "Tax Code",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(RawMaterialModel.Status)] = new()
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
			nameof(RawMaterialModel.Id),
			nameof(RawMaterialModel.Name),
			nameof(RawMaterialModel.Code),
			"Category",
			nameof(RawMaterialModel.Rate),
			nameof(RawMaterialModel.UnitOfMeasurement),
			"Tax",
			nameof(RawMaterialModel.Remarks),
			nameof(RawMaterialModel.Status)
        ];

        // Call the generic PDF export utility
        return await PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "RAW MATERIAL MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            useLandscape: true
        );
    }
}
