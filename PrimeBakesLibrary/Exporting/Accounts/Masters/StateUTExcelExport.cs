using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for State/UT
/// </summary>
public static class StateUTExcelExport
{
	/// <summary>
	/// Export State/UT data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="stateUTData">Collection of state/UT records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportStateUT(IEnumerable<StateUTModel> stateUTData)
	{
		// Create enriched data with status formatting
		var enrichedData = stateUTData.Select(stateUT => new
		{
			stateUT.Id,
			stateUT.Name,
			stateUT.Remarks,
			UnionTerritory = stateUT.UnionTerritory ? "Yes" : "No",
			Status = stateUT.Status ? "Active" : "Deleted"
		});
		
		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(StateUTModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(StateUTModel.Name)] = new() { DisplayName = "State/UT Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(StateUTModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Union Territory - Center aligned
			[nameof(StateUTModel.UnionTerritory)] = new() { DisplayName = "Union Territory", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Status - Center aligned
			[nameof(StateUTModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(StateUTModel.Id),
			nameof(StateUTModel.Name),
			nameof(StateUTModel.Remarks),
			nameof(StateUTModel.UnionTerritory),
			nameof(StateUTModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"STATE & UNION TERRITORY",
			"State UT Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
