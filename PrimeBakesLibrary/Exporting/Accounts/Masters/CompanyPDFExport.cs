using Microsoft.Extensions.Logging;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for Company
/// </summary>
public static class CompanyPDFExport
{
	/// <summary>
	/// Export company data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="companyData">Collection of company records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportCompany(IEnumerable<CompanyModel> companyData)
	{
		var stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

		// Create enriched data with status formatting
		var enrichedData = companyData.Select(company => new
		{
			company.Id,
			company.Name,
			company.Code,
			StateUT = stateUTs.FirstOrDefault(su => su.Id == company.StateUTId)?.Name ?? "N/A",
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
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(CompanyModel.Id)] = new()
			{
				DisplayName = "ID",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(CompanyModel.Name)] = new() { DisplayName = "Company Name", IncludeInTotal = false },
			[nameof(CompanyModel.Code)] = new() { DisplayName = "Code", IncludeInTotal = false },
			["StateUT"] = new() { DisplayName = "State/UT", IncludeInTotal = false },
			[nameof(CompanyModel.GSTNo)] = new() { DisplayName = "GST No", IncludeInTotal = false },
			[nameof(CompanyModel.PANNo)] = new() { DisplayName = "PAN No", IncludeInTotal = false },
			[nameof(CompanyModel.CINNo)] = new() { DisplayName = "CIN No", IncludeInTotal = false },
			[nameof(CompanyModel.Alias)] = new() { DisplayName = "Alias", IncludeInTotal = false },
			[nameof(CompanyModel.Phone)] = new() { DisplayName = "Phone", IncludeInTotal = false },
			[nameof(CompanyModel.Email)] = new() { DisplayName = "Email", IncludeInTotal = false },
			[nameof(CompanyModel.Address)] = new() { DisplayName = "Address", IncludeInTotal = false },
			[nameof(CompanyModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },

			[nameof(CompanyModel.Status)] = new()
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
			nameof(CompanyModel.Id),
			nameof(CompanyModel.Name),
			nameof(CompanyModel.Code),
			"StateUT",
			nameof(CompanyModel.GSTNo),
			nameof(CompanyModel.PANNo),
			nameof(CompanyModel.CINNo),
			nameof(CompanyModel.Alias),
			nameof(CompanyModel.Phone),
			nameof(CompanyModel.Email),
			nameof(CompanyModel.Address),
			nameof(CompanyModel.Remarks),
			nameof(CompanyModel.Status)
		];

		// Call the generic PDF export utility
		return await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"COMPANY MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: true
		);
	}
}
