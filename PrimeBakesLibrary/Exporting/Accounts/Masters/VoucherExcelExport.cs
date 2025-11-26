using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

/// <summary>
/// Excel export functionality for Voucher
/// </summary>
public static class VoucherExcelExport
{
	/// <summary>
	/// Export Voucher data to Excel with custom column order and formatting
	/// </summary>
	/// <param name="voucherData">Collection of voucher records</param>
	/// <returns>MemoryStream containing the Excel file</returns>
	public static MemoryStream ExportVoucher(IEnumerable<VoucherModel> voucherData)
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
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			["Name"] = new() { DisplayName = "Voucher Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["PrefixCode"] = new() { DisplayName = "Prefix Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			"Id", "Name", "PrefixCode", "Remarks", "Status"
		];

		// Call the generic Excel export utility
		return ExcelExportUtil.ExportToExcel(
			enrichedData,
			"VOUCHER",
			"Voucher Data",
			null,
			null,
			columnSettings,
			columnOrder
		);
	}
}
