using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

/// <summary>
/// PDF export functionality for Raw Material Category
/// </summary>
public static class RawMaterialCategoryPDFExport
{
	/// <summary>
	/// Export raw material category data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="rawMaterialCategoryData">Collection of raw material category records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportRawMaterialCategory(IEnumerable<RawMaterialCategoryModel> rawMaterialCategoryData)
	{
		// Create enriched data with status formatting
		var enrichedData = rawMaterialCategoryData.Select(rawMaterialCategory => new
		{
			rawMaterialCategory.Id,
			rawMaterialCategory.Name,
			rawMaterialCategory.Remarks,
			Status = rawMaterialCategory.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            [nameof(RawMaterialCategoryModel.Id)] = new()
            {
                DisplayName = "ID",
                StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
                {
                    Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
                    LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
                },
                IncludeInTotal = false
            },

            [nameof(RawMaterialCategoryModel.Name)] = new() { DisplayName = "Raw Material Category Name", IncludeInTotal = false },
            [nameof(RawMaterialCategoryModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

            [nameof(RawMaterialCategoryModel.Status)] = new()
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
			nameof(RawMaterialCategoryModel.Id),
			nameof(RawMaterialCategoryModel.Name),
			nameof(RawMaterialCategoryModel.Remarks),
			nameof(RawMaterialCategoryModel.Status)
		];

		// Call the generic PDF export utility
		return await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"Raw Material Category MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);
	}
}
