using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.XlsIO;

namespace PrimeBakesLibrary.Exporting.Accounting;

public static class AccountingExcelExport
{
	public static MemoryStream ExportAccountingOverviewExcel(
		List<AccountingOverviewModel> accountingOverviews,
		DateOnly startDate,
		DateOnly endDate,
		int selectedVoucherId = 0,
		List<VoucherModel> vouchers = null)
	{
		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Entries", accountingOverviews.Count },
			{ "Total Debit Amount", accountingOverviews.Sum(a => a.TotalDebitAmount) },
			{ "Total Credit Amount", accountingOverviews.Sum(a => a.TotalCreditAmount) },
			{ "Total Amount", accountingOverviews.Sum(a => a.TotalAmount) },
			{ "Unique Vouchers", accountingOverviews.Select(a => a.VoucherId).Distinct().Count() },
			{ "Unique Users", accountingOverviews.Select(a => a.UserId).Distinct().Count() }
		};

		// Add voucher filter info if specific voucher is selected
		if (selectedVoucherId > 0 && vouchers is not null)
		{
			var voucherName = vouchers.FirstOrDefault(v => v.Id == selectedVoucherId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Voucher", voucherName);
		}

		// Add top vouchers summary data
		var topVouchers = accountingOverviews
			.GroupBy(a => a.VoucherName)
			.OrderByDescending(g => g.Count())
			.Take(3)
			.ToList();

		foreach (var voucher in topVouchers)
			summaryItems.Add($"Voucher: {voucher.Key}", voucher.Count());

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(AccountingOverviewModel.AccountingId),
			nameof(AccountingOverviewModel.ReferenceNo),
			nameof(AccountingOverviewModel.AccountingDate),
			nameof(AccountingOverviewModel.VoucherName),
			nameof(AccountingOverviewModel.FinancialYearPeriod),
			nameof(AccountingOverviewModel.UserName),
			nameof(AccountingOverviewModel.GeneratedModule),
			nameof(AccountingOverviewModel.TotalLedgers),
			nameof(AccountingOverviewModel.TotalDebitLedgers),
			nameof(AccountingOverviewModel.TotalCreditLedgers),
			nameof(AccountingOverviewModel.TotalDebitAmount),
			nameof(AccountingOverviewModel.TotalCreditAmount),
			nameof(AccountingOverviewModel.TotalAmount),
			nameof(AccountingOverviewModel.Remarks),
			nameof(AccountingOverviewModel.CreatedAt)
		];

		// Define custom column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(AccountingOverviewModel.AccountingId)] = new()
			{
				DisplayName = "Entry #",
				Width = 12,
				Alignment = ExcelHAlign.HAlignCenter
			},
			[nameof(AccountingOverviewModel.ReferenceNo)] = new()
			{
				DisplayName = "Reference No",
				Width = 18,
				Alignment = ExcelHAlign.HAlignCenter
			},
			[nameof(AccountingOverviewModel.AccountingDate)] = new()
			{
				DisplayName = "Date",
				Format = "dd-MMM-yyyy",
				Width = 15,
				Alignment = ExcelHAlign.HAlignCenter
			},
			[nameof(AccountingOverviewModel.VoucherName)] = new()
			{
				DisplayName = "Voucher",
				Width = 18,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(AccountingOverviewModel.FinancialYearPeriod)] = new()
			{
				DisplayName = "Financial Year",
				Width = 15,
				Alignment = ExcelHAlign.HAlignCenter
			},
			[nameof(AccountingOverviewModel.UserName)] = new()
			{
				DisplayName = "User",
				Width = 18,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(AccountingOverviewModel.GeneratedModule)] = new()
			{
				DisplayName = "Module",
				Width = 15,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(AccountingOverviewModel.TotalLedgers)] = new()
			{
				DisplayName = "Ledgers",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight
			},
			[nameof(AccountingOverviewModel.TotalDebitLedgers)] = new()
			{
				DisplayName = "Debit Ledgers",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight
			},
			[nameof(AccountingOverviewModel.TotalCreditLedgers)] = new()
			{
				DisplayName = "Credit Ledgers",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight
			},
			[nameof(AccountingOverviewModel.TotalDebitAmount)] = new()
			{
				DisplayName = "Debit Amount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight
			},
			[nameof(AccountingOverviewModel.TotalCreditAmount)] = new()
			{
				DisplayName = "Credit Amount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight
			},
			[nameof(AccountingOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Total Amount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight,
				HighlightNegative = true,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var amount = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = amount > 100000,
						FontColor = amount > 500000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(AccountingOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(AccountingOverviewModel.CreatedAt)] = new()
			{
				DisplayName = "Created At",
				Format = "dd-MMM-yyyy HH:mm",
				Width = 18,
				Alignment = ExcelHAlign.HAlignCenter
			}
		};

		// Generate title based on voucher selection if applicable
		string reportTitle = "Accounting Report";
		if (selectedVoucherId > 0 && vouchers is not null)
		{
			var voucher = vouchers.FirstOrDefault(v => v.Id == selectedVoucherId);
			if (voucher is not null)
				reportTitle = $"Accounting Report - {voucher.Name}";
		}

		string worksheetName = "Accounting Details";

		return ExcelExportUtil.ExportToExcel(
			accountingOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}