using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for Ledger
/// </summary>
public static class LedgerExcelExport
{
	/// <summary>
	/// Export Ledger data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="ledgerData">Collection of ledger records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportLedger(IEnumerable<LedgerModel> ledgerData)
	{
		var groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
		var accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
		var stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);
		var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		// Create enriched data with status formatting
		var enrichedData = ledgerData.Select(ledger => new
		{
			ledger.Id,
			ledger.Name,
			ledger.Code,
			Group = groups.FirstOrDefault(g => g.Id == ledger.GroupId)?.Name ?? "N/A",
			AccountType = accountTypes.FirstOrDefault(at => at.Id == ledger.AccountTypeId)?.Name ?? "N/A",
			StateUT = stateUTs.FirstOrDefault(su => su.Id == ledger.StateUTId)?.Name ?? "N/A",
			ledger.GSTNo,
			ledger.PANNo,
			ledger.CINNo,
			ledger.Alias,
			ledger.Phone,
			ledger.Email,
			ledger.Address,
			Location = ledger.LocationId.HasValue
				? locations.FirstOrDefault(loc => loc.Id == ledger.LocationId.Value)?.Name ?? "N/A"
				: "N/A",
			ledger.Remarks,
			Status = ledger.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(LedgerModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(LedgerModel.Name)] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(LedgerModel.Code)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Group"] = new() { DisplayName = "Group", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["AccountType"] = new() { DisplayName = "Account Type", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["StateUT"] = new() { DisplayName = "State/UT", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			[nameof(LedgerModel.GSTNo)] = new() { DisplayName = "GST No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(LedgerModel.PANNo)] = new() { DisplayName = "PAN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(LedgerModel.CINNo)] = new() { DisplayName = "CIN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(LedgerModel.Alias)] = new() { DisplayName = "Alias", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(LedgerModel.Phone)] = new() { DisplayName = "Phone", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(LedgerModel.Email)] = new() { DisplayName = "Email", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			[nameof(LedgerModel.Address)] = new() { DisplayName = "Address", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Location"] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			[nameof(LedgerModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			[nameof(LedgerModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(LedgerModel.Id),
			nameof(LedgerModel.Name),
			nameof(LedgerModel.Code),
			"Group",
			"AccountType",
			"StateUT",
			nameof(LedgerModel.GSTNo),
			nameof(LedgerModel.PANNo),
			nameof(LedgerModel.CINNo),
			nameof(LedgerModel.Alias),
			nameof(LedgerModel.Phone),
			nameof(LedgerModel.Email),
			nameof(LedgerModel.Address),
			"Location",
			nameof(LedgerModel.Remarks),
			nameof(LedgerModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelExportUtil.ExportToExcel(
			enrichedData,
			"LEDGER",
			"Ledger Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
