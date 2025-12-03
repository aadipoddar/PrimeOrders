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
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			["Name"] = new() { DisplayName = "Raw Material Category Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			"Id", "Name", "Remarks", "Status"
		];

		// Call the generic Excel export utility
		return await ExcelExportUtil.ExportToExcel(
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
