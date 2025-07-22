using PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;

namespace PrimeOrdersLibrary.Exporting.Accounting;

public static class TrialBalanceExcelExport
{
	/// <summary>
	/// Exports trial balance details to Excel
	/// </summary>
	public static MemoryStream ExportTrialBalanceExcel(
		List<TrialBalanceModel> trialBalances,
		DateOnly startDate,
		DateOnly endDate)
	{
		if (trialBalances is null || trialBalances.Count == 0)
			throw new ArgumentException("No data to export");

		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Ledger Items", trialBalances.Count },
			{ "Opening Balance", trialBalances.Sum(s => s.OpeningBalance) },
			{ "Total Debits", trialBalances.Sum(s => s.TotalDebit) },
			{ "Total Credits", trialBalances.Sum(s => s.TotalCredit) },
			{ "Closing Balance", trialBalances.Sum(s => s.ClosingBalance) }
		};

		// Add top groups summary data
		var topGroups = trialBalances
			.GroupBy(s => s.GroupName)
			.OrderByDescending(g => g.Sum(s => s.ClosingBalance))
			.Take(3)
			.ToList();

		foreach (var group in topGroups)
			summaryItems.Add($"Group: {group.Key}", group.Sum(s => s.ClosingBalance));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(TrialBalanceModel.AccountTypeName),
			nameof(TrialBalanceModel.GroupName),
			nameof(TrialBalanceModel.LedgerName),
			nameof(TrialBalanceModel.GSTNo),
			nameof(TrialBalanceModel.Phone),
			nameof(TrialBalanceModel.Address),
			nameof(TrialBalanceModel.StateName),
			nameof(TrialBalanceModel.LocationName),
			nameof(TrialBalanceModel.OpeningBalance),
			nameof(TrialBalanceModel.TotalDebit),
			nameof(TrialBalanceModel.TotalCredit),
			nameof(TrialBalanceModel.ClosingBalance)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(TrialBalanceModel.AccountTypeName)] = new()
			{
				DisplayName = "Account Type",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(TrialBalanceModel.GroupName)] = new()
			{
				DisplayName = "Group",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(TrialBalanceModel.LedgerName)] = new()
			{
				DisplayName = "Ledger",
				Width = 28,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(TrialBalanceModel.GSTNo)] = new()
			{
				DisplayName = "GST No",
				Width = 16,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(TrialBalanceModel.Phone)] = new()
			{
				DisplayName = "Phone",
				Width = 16,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(TrialBalanceModel.Address)] = new()
			{
				DisplayName = "Address",
				Width = 32,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(TrialBalanceModel.StateName)] = new()
			{
				DisplayName = "State",
				Width = 16,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(TrialBalanceModel.LocationName)] = new()
			{
				DisplayName = "Location",
				Width = 16,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(TrialBalanceModel.OpeningBalance)] = new()
			{
				DisplayName = "Opening Balance",
				Format = "₹#,##0.00",
				Width = 18,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var val = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = val < 0,
						FontColor = val < 0 ? Syncfusion.Drawing.Color.FromArgb(198, 40, 40) : null
					};
				}
			},
			[nameof(TrialBalanceModel.TotalDebit)] = new()
			{
				DisplayName = "Debit",
				Format = "₹#,##0.00",
				Width = 18,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var val = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = val > 0,
						FontColor = val > 0 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(TrialBalanceModel.TotalCredit)] = new()
			{
				DisplayName = "Credit",
				Format = "₹#,##0.00",
				Width = 18,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var val = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = val > 0,
						FontColor = val > 0 ? Syncfusion.Drawing.Color.FromArgb(239, 108, 0) : null
					};
				}
			},
			[nameof(TrialBalanceModel.ClosingBalance)] = new()
			{
				DisplayName = "Closing Balance",
				Format = "₹#,##0.00",
				Width = 18,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				HighlightNegative = true,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var val = Convert.ToDecimal(value);
					if (val < 0)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40)
						};
					}
					else if (val < 100)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0)
						};
					}
					return null;
				}
			}
		};

		string reportTitle = "Trial Balance Report";
		string worksheetName = "Trial Balance";

		return ExcelExportUtil.ExportToExcel(
			trialBalances,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder
		);
	}
}