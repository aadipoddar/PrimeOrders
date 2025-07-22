using PrimeOrdersLibrary.Data.Accounts.Masters;
using PrimeOrdersLibrary.Exporting.Accounting;
using PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Accounts.Reports;

public partial class LedgerDetailReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedAccountTypeId = 0;
	private int _selectedGroupId = 0;
	private int _selectedLedgerId = 0;
	private LedgerModel _selectedLedger;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<AccountTypeModel> _accountTypes = [];
	private List<GroupModel> _groups = [];
	private List<LedgerModel> _ledgers = [];
	private List<LedgerModel> _filteredLedgers = [];
	private List<LedgerOverviewModel> _ledgerOverviews = [];
	private List<LedgerOverviewModel> _filteredLedgerOverviews = [];

	private SfGrid<LedgerOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_accountTypes = await CommonData.LoadTableDataByStatus<AccountTypeModel>(TableNames.AccountType);
		_groups = await CommonData.LoadTableDataByStatus<GroupModel>(TableNames.Group);
		_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
		_filteredLedgers = [.. _ledgers];

		await LoadProductOverviews();
	}

	private async Task LoadProductOverviews()
	{
		_ledgerOverviews = await LedgerData.LoadLedgerDetailsByDateLedger(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			0);

		await ApplyFilters();
	}

	private async Task ApplyFilters()
	{
		if (_selectedGroupId > 0)
			_filteredLedgers = [.. _ledgers.Where(p => p.GroupId == _selectedGroupId)];
		else
			_filteredLedgers = [.. _ledgers];

		if (_selectedAccountTypeId > 0)
			_filteredLedgers = [.. _filteredLedgers.Where(p => p.AccountTypeId == _selectedAccountTypeId)];
		else
			_filteredLedgers = [.. _filteredLedgers];

		_filteredLedgerOverviews = _ledgerOverviews;

		if (_selectedGroupId > 0)
			_filteredLedgerOverviews = [.. _filteredLedgerOverviews.Where(p => p.GroupId == _selectedGroupId)];

		if (_selectedAccountTypeId > 0)
			_filteredLedgerOverviews = [.. _filteredLedgerOverviews.Where(p => p.AccountTypeId == _selectedAccountTypeId)];

		if (_selectedLedgerId > 0)
		{
			_filteredLedgerOverviews = [.. _filteredLedgerOverviews.Where(p => p.LedgerId == _selectedLedgerId)];
			_selectedLedger = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, _selectedLedgerId);
		}

		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadProductOverviews();
	}

	private async Task OnAccountTypeChanged(ChangeEventArgs<int, AccountTypeModel> args)
	{
		_selectedAccountTypeId = args.Value;
		await LoadProductOverviews();
	}

	private async Task OnGroupChanged(ChangeEventArgs<int, GroupModel> args)
	{
		_selectedGroupId = args.Value;
		_selectedLedgerId = 0;
		await ApplyFilters();
	}

	private async Task OnLedgerChanged(ChangeEventArgs<int, LedgerModel> args)
	{
		_selectedLedgerId = args.Value;
		await ApplyFilters();
	}
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_filteredLedgerOverviews is null || _filteredLedgerOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = LedgerExcelExport.ExportLedgerDetailExcel(
			_filteredLedgerOverviews,
			_startDate,
			_endDate,
			_selectedLedger,
			_selectedGroupId,
			_groups,
			_selectedAccountTypeId,
			_accountTypes);

		// Generate filename based on selected ledger/group
		string filenameSuffix = string.Empty;
		if (_selectedLedger is not null)
			filenameSuffix = $"_{_selectedLedger.Name}";
		else if (_selectedGroupId > 0)
		{
			var category = _groups.FirstOrDefault(c => c.Id == _selectedGroupId);
			if (category is not null)
				filenameSuffix = $"_{category.Name}";
		}

		var fileName = $"Ledger_Detail{filenameSuffix}_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Chart Data Methods
	private List<DailyProductSalesChartData> GetDailyLedgerData() =>
		[.. _filteredLedgerOverviews
			.GroupBy(s => s.AccountingDate)
			.Select(group => new DailyProductSalesChartData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.Debit + s.Credit).Value
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))];

	private List<DebitCreditChartData> GetDebitCreditData() =>
		[
			new() { Type = "Debit", Amount = _filteredLedgerOverviews.Sum(a => a.Debit).Value },
			new() { Type = "Credit", Amount = _filteredLedgerOverviews.Sum(a => a.Credit).Value }
		];
	#endregion
}