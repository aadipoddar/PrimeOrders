using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for Ledger
/// </summary>
public static class LedgerPDFExport
{
	/// <summary>
	/// Export ledger data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="ledgerData">Collection of ledger records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportLedger(IEnumerable<LedgerModel> ledgerData)
	{
		// Create enriched data with status formatting
		var enrichedData = ledgerData.Select(ledger => new
		{
			ledger.Id,
			ledger.Name,
			ledger.Code,
			ledger.GSTNo,
			ledger.Phone,
			ledger.Email,
			ledger.Remarks,
			Status = ledger.Status ? "Active" : "Deleted"
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

			["Name"] = new() { DisplayName = "Ledger Name", IncludeInTotal = false },
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
			"LEDGER MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: true
		);
	}
}
