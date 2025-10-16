using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
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

public partial class TrialBalancePage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _showCharts = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedAccountTypeId = 0;
	private int _selectedGroupId = 0;

	private List<AccountTypeModel> _accountTypes = [];
	private List<GroupModel> _groups = [];
	private List<GroupModel> _filteredGroups = [];
	private List<TrialBalanceModel> _trialBalances = [];
	private SfGrid<TrialBalanceModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		await LoadInitialData();
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadInitialData()
	{
		try
		{
			_accountTypes = await CommonData.LoadTableDataByStatus<AccountTypeModel>(TableNames.AccountType, true);
			_groups = await CommonData.LoadTableDataByStatus<GroupModel>(TableNames.Group, true);

			// Add "All" options for dropdowns
			_accountTypes.Insert(0, new AccountTypeModel { Id = 0, Name = "All Account Types" });
			_groups.Insert(0, new GroupModel { Id = 0, Name = "All Groups" });

			_filteredGroups = [.. _groups];
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading initial data: {ex.Message}");
		}
	}

	private async Task LoadData()
	{
		try
		{
			await LoadTrialBalanceData();
			ApplyFilters();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading trial balance data: {ex.Message}");
		}
	}

	private async Task LoadTrialBalanceData() =>
		_trialBalances = await AccountingData.LoadTrialBalanceByDate(
			_startDate.ToDateTime(TimeOnly.MinValue),
			_endDate.ToDateTime(TimeOnly.MaxValue));

	private void ApplyFilters()
	{
		// Apply filters to trial balance data
		var filteredBalances = _trialBalances.AsEnumerable();

		if (_selectedAccountTypeId > 0)
			filteredBalances = filteredBalances.Where(t => t.AccountTypeId == _selectedAccountTypeId);

		if (_selectedGroupId > 0)
			filteredBalances = filteredBalances.Where(t => t.GroupId == _selectedGroupId);

		_trialBalances = filteredBalances.ToList();
	}

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

	private async Task OnAccountTypeChanged(ChangeEventArgs<int, AccountTypeModel> args)
	{
		_selectedAccountTypeId = args.Value;
		_selectedGroupId = 0; // Reset group filter
		await LoadData();
	}

	private async Task OnGroupChanged(ChangeEventArgs<int, GroupModel> args)
	{
		_selectedGroupId = args.Value;
		await LoadData();
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
			if (!_trialBalances.Any())
			{
				return;
			}

			var memoryStream = TrialBalanceExcelExport.ExportTrialBalanceExcel(_trialBalances, _startDate, _endDate);
			var fileName = $"Trial_Balance_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";

			await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error exporting to Excel: {ex.Message}");
		}
	}
	#endregion

	#region Summary Calculations
	private decimal GetTotalOpeningBalance()
	{
		return _trialBalances.Sum(t => t.OpeningBalance);
	}

	private decimal GetTotalDebit()
	{
		return _trialBalances.Sum(t => t.TotalDebit);
	}

	private decimal GetTotalCredit()
	{
		return _trialBalances.Sum(t => t.TotalCredit);
	}

	private decimal GetTotalClosingBalance()
	{
		return _trialBalances.Sum(t => t.ClosingBalance);
	}

	private decimal GetNetBalance()
	{
		return GetTotalDebit() - GetTotalCredit();
	}

	private int GetTotalAccounts()
	{
		return _trialBalances.Count;
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetBalanceOverviewChartData()
	{
		return new List<object>
		{
			new { Type = "Opening Balance", Amount = GetTotalOpeningBalance() },
			new { Type = "Total Debit", Amount = GetTotalDebit() },
			new { Type = "Total Credit", Amount = GetTotalCredit() },
			new { Type = "Closing Balance", Amount = GetTotalClosingBalance() }
		};
	}

	private List<object> GetTopGroupsChartData()
	{
		return _trialBalances
			.GroupBy(t => t.GroupName)
			.Select(g => new
			{
				GroupName = g.Key.Length > 15 ? g.Key.Substring(0, 15) + "..." : g.Key,
				TotalBalance = g.Sum(x => Math.Abs(x.ClosingBalance))
			})
			.OrderByDescending(x => x.TotalBalance)
			.Take(10)
			.Cast<object>()
			.ToList();
	}

	private List<object> GetDebitCreditAnalysisChartData()
	{
		return _trialBalances
			.OrderByDescending(t => t.TotalDebit + t.TotalCredit)
			.Take(10)
			.Select(t => new
			{
				LedgerName = t.LedgerName.Length > 15 ? t.LedgerName.Substring(0, 15) + "..." : t.LedgerName,
				DebitAmount = t.TotalDebit,
				CreditAmount = t.TotalCredit
			})
			.Cast<object>()
			.ToList();
	}

	private List<object> GetAccountTypeDistributionChartData()
	{
		return _trialBalances
			.GroupBy(t => t.AccountTypeName)
			.Select(g => new
			{
				AccountTypeName = g.Key,
				TotalBalance = g.Sum(x => Math.Abs(x.ClosingBalance))
			})
			.OrderByDescending(x => x.TotalBalance)
			.Cast<object>()
			.ToList();
	}
	#endregion
}