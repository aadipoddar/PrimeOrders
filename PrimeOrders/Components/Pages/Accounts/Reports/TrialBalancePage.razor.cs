using PrimeOrdersLibrary.Data.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Exporting.Accounting;
using PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Accounts.Reports;

public partial class TrialBalancePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<TrialBalanceModel> _trialBalances = [];
	private SfGrid<TrialBalanceModel> _sfGrid;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, primaryLocationRequirement: true)).User) is not null))
			return;

		await LoadStockDetails();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadStockDetails() =>
		_trialBalances = await AccountingData.LoadTrialBalanceByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadStockDetails();
	}

	private async Task ExportToExcel()
	{
		if (_trialBalances is null || _trialBalances.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = TrialBalanceExcelExport.ExportTrialBalanceExcel(_trialBalances, _startDate, _endDate);
		var fileName = $"Trail_Balance_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	#region Chart Methods
	private List<StockOverviewRawMaterialChartData> GetStockOverviewData() =>
		[
			new() { Component = "Opening Balance", Value = _trialBalances.Sum(s => s.OpeningBalance) },
			new() { Component = "Debit", Value = _trialBalances.Sum(s => s.TotalDebit) },
			new() { Component = "Credit", Value = _trialBalances.Sum(s => s.TotalCredit) },
			new() { Component = "Closing Balance", Value = _trialBalances.Sum(s => s.ClosingBalance) }
		];

	private List<CategoryDistributionRawMaterialChartData> GetCategoryDistributionData() =>
		[.. _trialBalances
		.GroupBy(s => s.GroupName)
		.Select(group => new CategoryDistributionRawMaterialChartData
		{
			CategoryName = group.Key,
			StockCount = group.Sum(s => s.ClosingBalance)
		})
		.OrderByDescending(c => c.StockCount)
		.Take(10)];

	private List<TopMovingItemsRawMaterialChartData> GetTopMovingItemsData() =>
		[.. _trialBalances
		.Select(s => new TopMovingItemsRawMaterialChartData
		{
			ItemName = s.LedgerName,
			Movement = s.TotalDebit + s.TotalCredit
		})
		.OrderByDescending(i => i.Movement)
		.Take(10)];

	private List<OpeningClosingRawMaterialChartData> GetOpeningClosingData() =>
		[.. _trialBalances
		.Select(s => new OpeningClosingRawMaterialChartData
		{
			ItemName = s.LedgerName,
			OpeningStock = s.TotalDebit,
			ClosingStock = s.TotalCredit
		})
		.OrderByDescending(i => Math.Abs(i.ClosingStock - i.OpeningStock))
		.Take(10)];
	#endregion
}