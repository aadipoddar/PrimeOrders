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
		// Create enriched data with status formatting
		var enrichedData = companyData.Select(company => new
		{
			company.Id,
			company.Name,
			company.Code,
			company.GSTNo,
			company.Phone,
			company.Email,
			company.Remarks,
			Status = company.Status ? "Active" : "Deleted"
		});

		// Define custom column settings
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			["Id"] = new()
			{
				DisplayName = "ID",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			["Name"] = new() { DisplayName = "Company Name", IncludeInTotal = false },
			["Code"] = new() { DisplayName = "Code", IncludeInTotal = false },
			["GSTNo"] = new() { DisplayName = "GST No", IncludeInTotal = false },
			["Phone"] = new() { DisplayName = "Phone", IncludeInTotal = false },
			["Email"] = new() { DisplayName = "Email", IncludeInTotal = false },
			["Remarks"] = new() { DisplayName = "Remarks", IncludeInTotal = false },

			["Status"] = new()
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
			"Id", "Name", "Code", "GSTNo", "Phone", "Email", "Remarks", "Status"
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
