using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for Group
/// </summary>
public static class GroupExcelExport
{
	/// <summary>
	/// Export Group data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="groupData">Collection of group records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportGroup(IEnumerable<GroupModel> groupData)
	{
		// Create enriched data with status formatting
		var enrichedData = groupData.Select(group => new
		{
			group.Id,
			group.Name,
			group.Remarks,
			Status = group.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(GroupModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(GroupModel.Name)] = new() { DisplayName = "Group Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(GroupModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			[nameof(GroupModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(GroupModel.Id),
			nameof(GroupModel.Name),
			nameof(GroupModel.Remarks),
			nameof(GroupModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"GROUP",
			"Group Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
