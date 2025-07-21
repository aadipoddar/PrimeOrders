using PrimeOrdersLibrary.Data.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Exporting.Accounting;
using PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Accounts.Reports;

public partial class AccountingReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	// Filter variables
	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedVoucherId = 0;

	// Data collections
	private List<AccountingOverviewModel> _accountingOverviews = [];
	private List<VoucherModel> _vouchers = [];

	// Grid reference
	private SfGrid<AccountingOverviewModel> _sfGrid;

	// Summary dialog
	private bool _accountingSummaryVisible = false;
	private AccountingOverviewModel _selectedAccounting = new();
	private List<AccountingCartModel> _selectedAccountingDetails = [];

	#region Page Load
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Accounts, true)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_vouchers = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);
		_vouchers.Insert(0, new VoucherModel { Id = 0, Name = "All Vouchers" });
		await LoadAccountingData();
	}

	private async Task LoadAccountingData()
	{
		_accountingOverviews = await AccountingData.LoadAccountingDetailsByDate(_startDate.ToDateTime(TimeOnly.MinValue), _endDate.ToDateTime(TimeOnly.MaxValue));
		_accountingOverviews = _accountingOverviews
			.Where(a => (_selectedVoucherId == 0 || a.VoucherId == _selectedVoucherId))
			.ToList();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Filter Events
	private async Task DateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadAccountingData();
	}

	private async Task OnVoucherChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, VoucherModel> args)
	{
		_selectedVoucherId = args.Value;
		await LoadAccountingData();
	}
	#endregion

	#region Grid Events
	public async Task OnRowSelected(RowSelectEventArgs<AccountingOverviewModel> args)
	{
		_selectedAccounting = args.Data;
		var accountingDetails = await AccountingData.LoadAccountingDetailsByAccounting(_selectedAccounting.AccountingId);

		_selectedAccountingDetails.Clear();
		foreach (var item in accountingDetails)
		{
			var ledger = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, item.LedgerId);
			_selectedAccountingDetails.Add(new()
			{
				Serial = _selectedAccountingDetails.Count + 1,
				Id = item.Id,
				Name = ledger.Name,
				Debit = item.Type == 'D' ? item.Amount : null,
				Credit = item.Type == 'C' ? item.Amount : null,
				Remarks = item.Remarks,
			});
		}

		_accountingSummaryVisible = true;
		StateHasChanged();
	}

	private void OnAccountingSummaryVisibilityChanged(bool isVisible)
	{
		_accountingSummaryVisible = isVisible;
		StateHasChanged();
	}
	#endregion

	#region Chart Data Methods
	private List<VoucherWiseChartData> GetVoucherWiseData()
	{
		return _accountingOverviews
			.GroupBy(a => a.VoucherName)
			.Select(g => new VoucherWiseChartData
			{
				VoucherName = g.Key,
				EntryCount = g.Count()
			})
			.OrderByDescending(x => x.EntryCount)
			.ToList();
	}

	private List<DebitCreditChartData> GetDebitCreditData()
	{
		var totalDebit = _accountingOverviews.Sum(a => a.TotalDebitAmount);
		var totalCredit = _accountingOverviews.Sum(a => a.TotalCreditAmount);

		return new List<DebitCreditChartData>
		{
			new() { Type = "Debit", Amount = totalDebit },
			new() { Type = "Credit", Amount = totalCredit }
		};
	}

	private List<DailyEntryChartData> GetDailyEntryData()
	{
		return _accountingOverviews
			.GroupBy(a => a.AccountingDate)
			.Select(g => new DailyEntryChartData
			{
				Date = g.Key.ToString("dd/MM"),
				EntryCount = g.Count()
			})
			.OrderBy(x => x.Date)
			.ToList();
	}

	private List<MonthlyAmountChartData> GetMonthlyAmountData() =>
		_accountingOverviews
			.GroupBy(a => new { Year = a.AccountingDate.Year, Month = a.AccountingDate.Month })
			.Select(g => new MonthlyAmountChartData
			{
				Month = $"{g.Key.Month:D2}/{g.Key.Year}",
				DebitAmount = g.Sum(x => x.TotalDebitAmount),
				CreditAmount = g.Sum(x => x.TotalCreditAmount)
			})
			.OrderBy(x => x.Month)
			.ToList();
	#endregion

	#region Export
	private async Task ExportToExcel()
	{
		if (_accountingOverviews is null || _accountingOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = AccountingExcelExport.ExportAccountingOverviewExcel(
			_accountingOverviews, _startDate, _endDate, _selectedVoucherId, _vouchers);

		var fileName = $"Accounting_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion
}