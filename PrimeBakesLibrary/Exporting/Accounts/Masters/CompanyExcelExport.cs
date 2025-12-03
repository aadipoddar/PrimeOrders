using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for Company
/// </summary>
public static class CompanyExcelExport
{
	/// <summary>
	/// Export Company data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="companyData">Collection of company records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static async Task<MemoryStream> ExportCompany(IEnumerable<CompanyModel> companyData)
	{
		// Create enriched data with status formatting
		var enrichedData = companyData.Select(company => new
		{
			company.Id,
			company.Name,
			company.Code,
			company.GSTNo,
			company.PANNo,
			company.CINNo,
			company.Alias,
			company.Phone,
			company.Email,
			company.Address,
			company.Remarks,
			Status = company.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			["Name"] = new() { DisplayName = "Company Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Code"] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["GSTNo"] = new() { DisplayName = "GST No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["PANNo"] = new() { DisplayName = "PAN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["CINNo"] = new() { DisplayName = "CIN No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Alias"] = new() { DisplayName = "Alias", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Phone"] = new() { DisplayName = "Phone", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Email"] = new() { DisplayName = "Email", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Address"] = new() { DisplayName = "Address", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			"Id", "Name", "Code", "GSTNo", "PANNo", "CINNo", "Alias", "Phone", "Email", "Address", "Remarks", "Status"
		];

		// Call the generic Excel export utility
		return await ExcelExportUtil.ExportToExcel(
			enrichedData,
			"COMPANY",
			"Company Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
