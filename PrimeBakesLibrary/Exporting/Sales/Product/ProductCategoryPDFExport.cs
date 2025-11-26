using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

/// <summary>
/// PDF export functionality for Product Category
/// </summary>
public static class ProductCategoryPDFExport
{
	/// <summary>
	/// Export product category data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="productCategoryData">Collection of product category records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportProductCategory(IEnumerable<ProductCategoryModel> productCategoryData)
	{
		// Create enriched data with status formatting
		var enrichedData = productCategoryData.Select(productCategory => new
		{
			productCategory.Id,
			productCategory.Name,
			productCategory.Remarks,
			Status = productCategory.Status ? "Active" : "Deleted"
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

			["Name"] = new() { DisplayName = "Product Category Name", IncludeInTotal = false },
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
		return PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"PRODUCT CATEGORY MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);
	}
}
