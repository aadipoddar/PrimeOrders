using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

/// <summary>
/// Excel export functionality for Product Category
/// </summary>
public static class ProductCategoryExcelExport
{
	/// <summary>
	/// Export Product Category data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="productCategoryData">Collection of product category records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportProductCategory(IEnumerable<ProductCategoryModel> productCategoryData)
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
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(ProductCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(ProductCategoryModel.Name)] = new() { DisplayName = "Product Category Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(ProductCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			[nameof(ProductCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(ProductCategoryModel.Id),
			nameof(ProductCategoryModel.Name),
			nameof(ProductCategoryModel.Remarks),
			nameof(ProductCategoryModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelExportUtil.ExportToExcel(
			enrichedData,
			"PRODUCT CATEGORY",
			"Product Category Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
