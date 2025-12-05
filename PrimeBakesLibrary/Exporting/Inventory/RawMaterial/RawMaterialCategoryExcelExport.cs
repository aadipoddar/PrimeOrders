using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.RawMaterial;

/// <summary>
/// Excel export functionality for Raw Material Category
/// </summary>
public static class RawMaterialCategoryExcelExport
{
	/// <summary>
	/// Export Raw Material Category data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="rawMaterialCategoryData">Collection of raw material category records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
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
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(RawMaterialCategoryModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(RawMaterialCategoryModel.Name)] = new() { DisplayName = "Raw Material Category Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(RawMaterialCategoryModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			[nameof(RawMaterialCategoryModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(RawMaterialCategoryModel.Id),
			nameof(RawMaterialCategoryModel.Name),
			nameof(RawMaterialCategoryModel.Remarks),
			nameof(RawMaterialCategoryModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"RAW MATERIAL CATEGORY",
			"Raw Material Category Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
