using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Accounts;

public partial class LedgerReportsPage
{
	private bool _isLoading = true;
	private bool _showCharts = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private LedgerModel _selectedLedger;

	private List<LedgerModel> _ledgers = [];
	private List<LedgerOverviewModel> _ledgerOverviews = [];
	private List<TrialBalanceModel> _trialBalances = [];

	private SfGrid<LedgerOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		try
		{
			_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger, true);
			await LoadLedgerData();
			ApplyFilters();
			await LoadTrialBalance();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading ledger data: {ex.Message}");
		}
	}

	private async Task LoadLedgerData() =>
		_ledgerOverviews = await LedgerData.LoadLedgerDetailsByDateLedger(
			_startDate.ToDateTime(TimeOnly.MinValue),
			_endDate.ToDateTime(TimeOnly.MaxValue),
			0);

	private void ApplyFilters()
	{
		if (_selectedLedger is null || _selectedLedger.Id <= 0)
			return;

		List<LedgerOverviewModel> filteredOverviews = [];
		var partyLedgers = _ledgerOverviews.Where(l => l.LedgerId == _selectedLedger.Id).ToList();

		foreach (var item in partyLedgers)
		{
			var referenceLedgers = _ledgerOverviews
				.Where(l => l.AccountingId == item.AccountingId && l.LedgerId != _selectedLedger.Id) // Exclude the selected ledger rows
				.ToList();

			var referenceLedgerNamesWithAmount = string.Join("\n",
				referenceLedgers.Select(l =>
				$"{l.LedgerName}\t({(l.Debit > 0 ? "Dr " + l.Debit.FormatIndianCurrency() : l.Credit > 0 ? "Cr " + l.Credit.FormatIndianCurrency() : "0.00")})"));

			item.LedgerName = referenceLedgerNamesWithAmount;
			filteredOverviews.Add(item);
		}

		_ledgerOverviews = [.. filteredOverviews];
	}

	private async Task LoadTrialBalance() =>
		_trialBalances = await AccountingData.LoadTrialBalanceByDate(
			_startDate.ToDateTime(TimeOnly.MinValue),
			_endDate.ToDateTime(TimeOnly.MaxValue));
	#endregion

	#region Event Handlers
	private async Task OnDateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		if (args.StartDate != default && args.EndDate != default)
		{
			_startDate = args.StartDate;
			_endDate = args.EndDate;
			await LoadData();
		}
	}

	private async Task OnLedgerChanged(ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		_selectedLedger = args.Value;
		await LoadData();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private void ToggleCharts()
	{
		_showCharts = !_showCharts;
		StateHasChanged();
	}

	private async Task ExportToExcel()
	{
		try
		{
			if (_ledgerOverviews.Count == 0)
				return;

			var memoryStream = LedgerExcelExport.ExportLedgerDetailExcel(
				_ledgerOverviews,
				_startDate,
				_endDate,
				_selectedLedger,
				0,
				null,
				0,
				null);

			// Generate filename based on filters
			string filenameSuffix = string.Empty;
			if (_selectedLedger != null)
				filenameSuffix = $"_{_selectedLedger.Name}";

			var fileName = $"Ledger_Report{filenameSuffix}_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";

			await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting to Excel: {ex.Message}");
		}
	}
	#endregion

	#region Summary Calculations
	private decimal GetTotalDebitAmount()
	{
		return _ledgerOverviews.Sum(l => l.Debit ?? 0);
	}

	private decimal GetTotalCreditAmount()
	{
		return _ledgerOverviews.Sum(l => l.Credit ?? 0);
	}

	private decimal GetNetBalance()
	{
		return GetTotalDebitAmount() - GetTotalCreditAmount();
	}

	private int GetTotalTransactions()
	{
		return _ledgerOverviews.Count;
	}

	private int GetActiveLedgers()
	{
		return _ledgerOverviews.Select(l => l.LedgerId).Distinct().Count();
	}

	private decimal GetAverageTransactionAmount()
	{
		if (!_ledgerOverviews.Any()) return 0;
		return _ledgerOverviews.Average(l => (l.Debit ?? 0) + (l.Credit ?? 0));
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetDebitCreditChartData() => [
			new { Type = "Debit", Amount = GetTotalDebitAmount() },
			new { Type = "Credit", Amount = GetTotalCreditAmount() }
		];

	private List<object> GetTopLedgersChartData() => _ledgerOverviews
			.GroupBy(l => new { l.LedgerId, l.LedgerName })
			.Select(g => new
			{
				LedgerName = g.Key.LedgerName.Length > 15 ? g.Key.LedgerName.Substring(0, 15) + "..." : g.Key.LedgerName,
				TransactionCount = g.Count()
			})
			.OrderByDescending(x => x.TransactionCount)
			.Take(10)
			.Cast<object>()
			.ToList();

	private List<object> GetDailyTrendChartData()
	{
		return _ledgerOverviews
			.GroupBy(l => l.AccountingDate.ToString("MMM dd"))
			.Select(g => new
			{
				Date = g.Key,
				DebitAmount = g.Sum(x => x.Debit ?? 0),
				CreditAmount = g.Sum(x => x.Credit ?? 0)
			})
			.OrderBy(x => x.Date)
			.Cast<object>()
			.ToList();
	}

	private List<object> GetAccountTypeChartData()
	{
		return _ledgerOverviews
			.GroupBy(l => l.AccountTypeName)
			.Select(g => new
			{
				AccountTypeName = g.Key,
				TotalAmount = g.Sum(x => (x.Debit ?? 0) + (x.Credit ?? 0))
			})
			.OrderByDescending(x => x.TotalAmount)
			.Cast<object>()
			.ToList();
	}
	#endregion
}