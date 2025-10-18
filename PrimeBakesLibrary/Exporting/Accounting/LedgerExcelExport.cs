using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.XlsIO;

namespace PrimeBakesLibrary.Exporting.Accounting;

public static class LedgerExcelExport
{
	public static MemoryStream ExportLedgerDetailExcel(
		List<LedgerOverviewModel> ledgerOverviews,
		DateOnly startDate,
		DateOnly endDate,
		LedgerModel selectedLedger = null,
		int selectedGroupId = 0,
		List<GroupModel> groups = null,
		int selectedAccountTypeId = 0,
		List<AccountTypeModel> accountTypes = null)
	{
		// Build summary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Entries", ledgerOverviews.Count },
			{ "Total Debit", ledgerOverviews.Sum(l => l.Debit ?? 0) },
			{ "Total Credit", ledgerOverviews.Sum(l => l.Credit ?? 0) },
			{ "Net Balance", ledgerOverviews.Sum(l => (l.Debit ?? 0) - (l.Credit ?? 0)) },
			{ "Unique Ledgers", ledgerOverviews.Select(l => l.LedgerId).Distinct().Count() }
		};

		if (selectedLedger is not null)
			summaryItems.Add("Ledger", selectedLedger.Name);
		else if (selectedGroupId > 0 && groups is not null)
		{
			var group = groups.FirstOrDefault(g => g.Id == selectedGroupId);
			if (group is not null)
				summaryItems.Add("Filtered by Group", group.Name);
		}
		else if (selectedAccountTypeId > 0 && accountTypes is not null)
		{
			var accType = accountTypes.FirstOrDefault(a => a.Id == selectedAccountTypeId);
			if (accType is not null)
				summaryItems.Add("Filtered by Account Type", accType.Name);
		}

		// Top 3 ledgers by debit
		var topLedgers = ledgerOverviews
			.GroupBy(l => l.LedgerName)
			.OrderByDescending(g => g.Sum(l => l.Debit ?? 0))
			.Take(3)
			.ToList();

		foreach (var ledger in topLedgers)
			summaryItems.Add($"Top Ledger: {ledger.Key}", ledger.Sum(l => l.Debit ?? 0));

		// Column order
		List<string> columnOrder = [
			nameof(LedgerOverviewModel.AccountingDate),
			nameof(LedgerOverviewModel.TransactionNo),
			nameof(LedgerOverviewModel.LedgerName),
			nameof(LedgerOverviewModel.ReferenceType),
			nameof(LedgerOverviewModel.Debit),
			nameof(LedgerOverviewModel.Credit),
			nameof(LedgerOverviewModel.Remarks),
			nameof(LedgerOverviewModel.AccountingRemarks),
			nameof(LedgerOverviewModel.LedgerRemarks)
		];

		// Column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(LedgerOverviewModel.AccountingDate)] = new()
			{
				DisplayName = "Date",
				Format = "dd-MMM-yyyy",
				Width = 20,
				Alignment = ExcelHAlign.HAlignCenter
			},
			[nameof(LedgerOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction No",
				Width = 18,
				Alignment = ExcelHAlign.HAlignCenter
			},
			[nameof(LedgerOverviewModel.LedgerName)] = new()
			{
				DisplayName = "Ledger Name",
				Width = 30
			},
			[nameof(LedgerOverviewModel.ReferenceType)] = new()
			{
				DisplayName = "Reference Type",
				Width = 20,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(LedgerOverviewModel.Debit)] = new()
			{
				DisplayName = "Debit",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var debit = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = debit > 0,
						FontColor = debit > 10000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(LedgerOverviewModel.Credit)] = new()
			{
				DisplayName = "Credit",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var credit = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = credit > 0,
						FontColor = credit > 10000 ? Syncfusion.Drawing.Color.FromArgb(220, 53, 69) : null
					};
				}
			},
			[nameof(LedgerOverviewModel.Remarks)] = new()
			{
				DisplayName = "Entry Remarks",
				Width = 40,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(LedgerOverviewModel.AccountingRemarks)] = new()
			{
				DisplayName = "Accounting Remarks",
				Width = 40,
				Alignment = ExcelHAlign.HAlignLeft
			},
			[nameof(LedgerOverviewModel.LedgerRemarks)] = new()
			{
				DisplayName = "Ledger Remarks",
				Width = 40,
				Alignment = ExcelHAlign.HAlignLeft
			}
		};

		// Title
		string reportTitle = selectedLedger is not null
			? $"Ledger Detail Report - {selectedLedger.Name}"
			: "Ledger Detail Report";

		string worksheetName = "Ledger Details";

		return ExcelExportUtil.ExportToExcel(
			ledgerOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}