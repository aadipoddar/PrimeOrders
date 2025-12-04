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
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			// ID - Center aligned, no totals
			[nameof(VoucherModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Text fields - Left aligned
			[nameof(VoucherModel.Name)] = new() { DisplayName = "Voucher Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(VoucherModel.PrefixCode)] = new() { DisplayName = "Prefix Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
			[nameof(VoucherModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			// Status - Center aligned
			[nameof(VoucherModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		// Define column order
		List<string> columnOrder =
		[
			nameof(VoucherModel.Id),
			nameof(VoucherModel.Name),
			nameof(VoucherModel.PrefixCode),
			nameof(VoucherModel.Remarks),
			nameof(VoucherModel.Status)
		];

		// Call the generic Excel export utility
		return await ExcelExportUtil.ExportToExcel(
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
