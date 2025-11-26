using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for Account Type
/// </summary>
public static class AccountTypePDFExport
{
	/// <summary>
	/// Export account type data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="accountTypeData">Collection of account type records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static MemoryStream ExportAccountType(IEnumerable<AccountTypeModel> accountTypeData)
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

			["Name"] = new() { DisplayName = "Account Type Name", IncludeInTotal = false },
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
			"Id", "Name", "Remarks", "Status"
		];

		// Call the generic PDF export utility
		return PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"ACCOUNT TYPE MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);
	}
}
