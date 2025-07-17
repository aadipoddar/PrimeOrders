using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Stock;

public partial class FinishedProductStockReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private int _selectedLocationId;
	private List<ProductStockDetailModel> _stockDetails = [];

	private SfGrid<ProductStockDetailModel> _sfGrid;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager)).User) is not null))
			return;

		await LoadLocations();
		await LoadStockDetails();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		_selectedLocationId = _user.LocationId;

		if (_user.LocationId == 1)
			_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
	}

	private async Task LoadStockDetails() =>
		_stockDetails = await StockData.LoadProductStockDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_selectedLocationId);

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadStockDetails();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadStockDetails();
	}

	private async Task ExportToExcel()
	{
		if (_stockDetails is null || _stockDetails.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = StockExcelExport.ExportFinishedProductStockExcel(_stockDetails, _startDate, _endDate);
		var fileName = $"Finished_Product_Stock_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	#region Chart data methods
	private List<StockOverviewProductChartData> GetStockOverviewData() =>
		[
			new() { Component = "Opening Stock", Value = _stockDetails.Sum(s => s.OpeningStock) },
			new() { Component = "Production", Value = _stockDetails.Sum(s => s.PurchaseStock) },
			new() { Component = "Sales", Value = _stockDetails.Sum(s => s.SaleStock) },
			new() { Component = "Monthly Stock", Value = _stockDetails.Sum(s => s.MonthlyStock) },
			new() { Component = "Closing Stock", Value = _stockDetails.Sum(s => s.ClosingStock) }
		];

	private List<CategoryDistributionProductChartData> GetCategoryDistributionData() =>
		[.. _stockDetails
			.GroupBy(s => s.ProductCategoryName)
			.Select(group => new CategoryDistributionProductChartData
			{
				CategoryName = group.Key,
				StockCount = group.Sum(s => s.ClosingStock)
			})
			.OrderByDescending(c => c.StockCount)
			.Take(10)];

	private List<TopMovingItemsProductChartData> GetTopMovingItemsData() =>
		[.. _stockDetails
			.Select(s => new TopMovingItemsProductChartData
			{
				ItemName = s.ProductName,
				Movement = s.PurchaseStock + s.SaleStock
			})
			.OrderByDescending(i => i.Movement)
			.Take(10)];

	private List<OpeningClosingProductChartData> GetOpeningClosingData() =>
		[.. _stockDetails
			.Select(s => new OpeningClosingProductChartData
			{
				ItemName = s.ProductName,
				OpeningStock = s.OpeningStock,
				ClosingStock = s.ClosingStock
			})
			.OrderByDescending(i => Math.Abs(i.ClosingStock - i.OpeningStock))
			.Take(10)];
	#endregion
}