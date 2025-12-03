using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// PDF export functionality for Voucher
/// </summary>
public static class VoucherPDFExport
{
	/// <summary>
	/// Export voucher data to PDF with custom column order and formatting
	/// </summary>
	/// <param name="voucherData">Collection of voucher records</param>
	/// <returns>MemoryStream containing the PDF file</returns>
	public static async Task<MemoryStream> ExportVoucher(IEnumerable<VoucherModel> voucherData)
	{
		// Create enriched data with status formatting
		var enrichedData = voucherData.Select(voucher => new
		{
			voucher.Id,
			voucher.Name,
			voucher.PrefixCode,
			voucher.Remarks,
			Status = voucher.Status ? "Active" : "Deleted"
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

			["Name"] = new() { DisplayName = "Voucher Name", IncludeInTotal = false },
			["PrefixCode"] = new() { DisplayName = "Prefix Code", IncludeInTotal = false },
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
			"Id", "Name", "PrefixCode", "Remarks", "Status"
		];

		// Call the generic PDF export utility
		return await PDFReportExportUtil.ExportToPdf(
			enrichedData,
			"VOUCHER MASTER",
			null,
			null,
			columnSettings,
			columnOrder,
			useLandscape: false
		);
	}
}
