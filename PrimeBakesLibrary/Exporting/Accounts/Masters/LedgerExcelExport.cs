using PrimeBakesLibrary.Models.Accounts.Masters;

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
	public static MemoryStream ExportLedger(IEnumerable<LedgerModel> ledgerData)
	{
		// Create enriched data with status formatting
		var enrichedData = ledgerData.Select(ledger => new
		{
			ledger.Id,
			ledger.Name,
			ledger.Code,
			ledger.GroupId,
			ledger.AccountTypeId,
			ledger.StateUTId,
			ledger.GSTNo,
			ledger.PANNo,
			ledger.CINNo,
			ledger.Alias,
			ledger.Phone,
			ledger.Email,
			ledger.Address,
			ledger.LocationId,
			ledger.Remarks,
			Status = ledger.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			["Name"] = new() { DisplayName = "Ledger Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Code"] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["GroupId"] = new() { DisplayName = "Group ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["AccountTypeId"] = new() { DisplayName = "Account Type ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["StateUTId"] = new() { DisplayName = "State/UT ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["GSTNo"] = new() { DisplayName = "GST No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["PANNo"] = new() { DisplayName = "PAN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CINNo"] = new() { DisplayName = "CIN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Alias"] = new() { DisplayName = "Alias", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Phone"] = new() { DisplayName = "Phone", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Email"] = new() { DisplayName = "Email", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Address"] = new() { DisplayName = "Address", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["LocationId"] = new() { DisplayName = "Location ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			"Id", "Name", "Code", "GroupId", "AccountTypeId", "StateUTId", "GSTNo", "PANNo", "CINNo", 
			"Alias", "Phone", "Email", "Address", "LocationId", "Remarks", "Status"
		];

		// Call the generic Excel export utility
		return ExcelExportUtil.ExportToExcel(
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
