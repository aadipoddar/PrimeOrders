using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Accounts;

public partial class LedgerReportsPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _showCharts = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedAccountTypeId = 0;
	private int _selectedGroupId = 0;
	private int _selectedLedgerId = 0;

	private List<AccountTypeModel> _accountTypes = [];
	private List<GroupModel> _groups = [];
	private List<LedgerModel> _ledgers = [];
	private List<LedgerModel> _filteredLedgers = [];
	private List<LedgerOverviewModel> _ledgerOverviews = [];
	private SfGrid<LedgerOverviewModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		_user = authResult.User;

		// Only admin users can access ledger reports
		if (_user.LocationId != 1)
		{
			NavigationManager.NavigateTo("/Reports-Dashboard");
			return;
		}

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
			_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger, true);

			// Add "All" options for dropdowns
			_accountTypes.Insert(0, new AccountTypeModel { Id = 0, Name = "All Account Types" });
			_groups.Insert(0, new GroupModel { Id = 0, Name = "All Groups" });

			_filteredLedgers = [.. _ledgers];
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
			await LoadLedgerData();
			await ApplyFilters();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading ledger data: {ex.Message}");
		}
	}

	private async Task LoadLedgerData()
	{
		_ledgerOverviews = await LedgerData.LoadLedgerDetailsByDateLedger(
			_startDate.ToDateTime(TimeOnly.MinValue),
			_endDate.ToDateTime(TimeOnly.MaxValue),
			_selectedLedgerId);
	}

	private async Task ApplyFilters()
	{
		// Filter ledgers based on selected group and account type
		if (_selectedGroupId > 0)
			_filteredLedgers = [.. _ledgers.Where(l => l.GroupId == _selectedGroupId)];
		else
			_filteredLedgers = [.. _ledgers];

		if (_selectedAccountTypeId > 0)
			_filteredLedgers = [.. _filteredLedgers.Where(l => l.AccountTypeId == _selectedAccountTypeId)];

		// Apply filters to overview data
		var filteredOverviews = _ledgerOverviews.AsEnumerable();

		if (_selectedGroupId > 0)
			filteredOverviews = filteredOverviews.Where(o => o.GroupId == _selectedGroupId);

		if (_selectedAccountTypeId > 0)
			filteredOverviews = filteredOverviews.Where(o => o.AccountTypeId == _selectedAccountTypeId);

		if (_selectedLedgerId > 0)
			filteredOverviews = filteredOverviews.Where(o => o.LedgerId == _selectedLedgerId);

		_ledgerOverviews = filteredOverviews.ToList();

		await Task.CompletedTask;
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
		_selectedLedgerId = 0; // Reset ledger filter
		await LoadData();
	}

	private async Task OnGroupChanged(ChangeEventArgs<int, GroupModel> args)
	{
		_selectedGroupId = args.Value;
		_selectedLedgerId = 0; // Reset ledger filter
		await LoadData();
	}

	private async Task OnLedgerChanged(ChangeEventArgs<int, LedgerModel> args)
	{
		_selectedLedgerId = args.Value;
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
			if (!_ledgerOverviews.Any())
			{
				return;
			}

			// Use the existing ledger export functionality
			var selectedLedger = _selectedLedgerId > 0 ? _ledgers.FirstOrDefault(l => l.Id == _selectedLedgerId) : null;

			var memoryStream = LedgerExcelExport.ExportLedgerDetailExcel(
				_ledgerOverviews,
				_startDate,
				_endDate,
				selectedLedger,
				_selectedGroupId,
				_groups,
				_selectedAccountTypeId,
				_accountTypes);

			// Generate filename based on filters
			string filenameSuffix = string.Empty;
			if (selectedLedger != null)
				filenameSuffix = $"_{selectedLedger.Name}";
			else if (_selectedGroupId > 0)
			{
				var group = _groups.FirstOrDefault(g => g.Id == _selectedGroupId);
				if (group != null)
					filenameSuffix = $"_{group.Name}";
			}
			else if (_selectedAccountTypeId > 0)
			{
				var accountType = _accountTypes.FirstOrDefault(a => a.Id == _selectedAccountTypeId);
				if (accountType != null)
					filenameSuffix = $"_{accountType.Name}";
			}

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
	private List<object> GetDebitCreditChartData()
	{
		return new List<object>
		{
			new { Type = "Debit", Amount = GetTotalDebitAmount() },
			new { Type = "Credit", Amount = GetTotalCreditAmount() }
		};
	}

	private List<object> GetTopLedgersChartData()
	{
		return _ledgerOverviews
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
	}

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