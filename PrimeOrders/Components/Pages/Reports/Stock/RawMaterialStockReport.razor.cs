using PrimeBakesLibrary.Exporting.Stock;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Stock;

public partial class RawMaterialStockReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<RawMaterialStockSummaryModel> _stockDetails = [];

	private SfGrid<RawMaterialStockSummaryModel> _sfGrid;

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
		_stockDetails = await StockData.LoadRawMaterialStockDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_user.LocationId);

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadStockDetails();
	}

	private async Task ExportToExcel()
	{
		if (_stockDetails is null || _stockDetails.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = StockExcelExport.ExportRawMaterialStockSummaryExcel(_stockDetails, _startDate, _endDate);
		var fileName = $"Raw_material_Stock_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	#region Chart Methods
	private List<StockOverviewRawMaterialChartData> GetStockOverviewData() =>
		[
			new() { Component = "Opening Stock", Value = _stockDetails.Sum(s => s.OpeningStock) },
			new() { Component = "Purchases", Value = _stockDetails.Sum(s => s.PurchaseStock) },
			new() { Component = "Sales", Value = _stockDetails.Sum(s => s.SaleStock) },
			new() { Component = "Monthly Stock", Value = _stockDetails.Sum(s => s.MonthlyStock) },
			new() { Component = "Closing Stock", Value = _stockDetails.Sum(s => s.ClosingStock) }
		];

	private List<CategoryDistributionRawMaterialChartData> GetCategoryDistributionData() =>
		[.. _stockDetails
			.GroupBy(s => s.RawMaterialCategoryName)
			.Select(group => new CategoryDistributionRawMaterialChartData
			{
				CategoryName = group.Key,
				StockCount = group.Sum(s => s.ClosingStock)
			})
			.OrderByDescending(c => c.StockCount)
			.Take(10)];

	private List<TopMovingItemsRawMaterialChartData> GetTopMovingItemsData() =>
		[.. _stockDetails
			.Select(s => new TopMovingItemsRawMaterialChartData
			{
				ItemName = s.RawMaterialName,
				Movement = s.PurchaseStock + s.SaleStock
			})
			.OrderByDescending(i => i.Movement)
			.Take(10)];

	private List<OpeningClosingRawMaterialChartData> GetOpeningClosingData() =>
		[.. _stockDetails
			.Select(s => new OpeningClosingRawMaterialChartData
			{
				ItemName = s.RawMaterialName,
				OpeningStock = s.OpeningStock,
				ClosingStock = s.ClosingStock
			})
			.OrderByDescending(i => Math.Abs(i.ClosingStock - i.OpeningStock))
			.Take(10)];
	#endregion
}