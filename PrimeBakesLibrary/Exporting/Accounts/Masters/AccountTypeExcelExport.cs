using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for Account Type
/// </summary>
public static class AccountTypeExcelExport
{
	/// <summary>
	/// Export Account Type data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="accountTypeData">Collection of account type records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportAccountType(IEnumerable<AccountTypeModel> accountTypeData)
	{
		// Create enriched data with status formatting
		var enrichedData = accountTypeData.Select(accountType => new
		{
			accountType.Id,
			accountType.Name,
			accountType.Remarks,
			Status = accountType.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(AccountTypeModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(AccountTypeModel.Name)] = new() { DisplayName = "Account Type Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(AccountTypeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			[nameof(AccountTypeModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(AccountTypeModel.Id),
			nameof(AccountTypeModel.Name),
			nameof(AccountTypeModel.Remarks),
			nameof(AccountTypeModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelExportUtil.ExportToExcel(
			enrichedData,
			"ACCOUNT TYPE",
			"Account Type Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
