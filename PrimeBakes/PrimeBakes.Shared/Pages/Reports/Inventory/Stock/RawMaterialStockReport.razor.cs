using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Exporting.Stock;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Stock;

public partial class RawMaterialStockReport
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showCharts = true;
	private bool _showSummaryView = true; // true for Summary, false for Details

	private DateOnly? _fromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly? _toDate = DateOnly.FromDateTime(DateTime.Now);

	private List<RawMaterialStockSummaryModel> _stockSummary = [];
	private List<RawMaterialStockDetailsModel> _stockDetails = [];

	private SfGrid<RawMaterialStockSummaryModel> _sfStockSummaryGrid;
	private SfGrid<RawMaterialStockDetailsModel> _sfStockDetailsGrid;

	protected override async Task OnInitializedAsync()
	{
		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadStockReport();
		_isLoading = false;
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly?> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadStockReport();
	}

	private async Task LoadStockReport()
	{
		_isProcessing = true;
		_isProcessing = true;
		StateHasChanged();

		var startDate = (_fromDate ?? DateOnly.FromDateTime(DateTime.Now)).ToDateTime(new TimeOnly(0, 0));
		var endDate = (_toDate ?? DateOnly.FromDateTime(DateTime.Now)).ToDateTime(new TimeOnly(23, 59));

		_stockSummary = await StockData.LoadRawMaterialStockSummaryByDateLocationId(startDate, endDate, 1);
		_stockDetails = await StockData.LoadRawMaterialStockDetailsByDateLocationId(startDate, endDate, 1);

		_isProcessing = false;
		StateHasChanged();
	}

	private void ToggleCharts()
	{
		_showCharts = !_showCharts;
		StateHasChanged();
	}

	private void ToggleView()
	{
		_showSummaryView = !_showSummaryView;
		StateHasChanged();
	}

	#region Summary Card Methods
	private int GetTotalStockItems() => _stockSummary.Count;
	private decimal GetTotalOpeningStock() => _stockSummary.Sum(s => s.OpeningStock);
	private decimal GetTotalPurchases() => _stockSummary.Sum(s => s.PurchaseStock);
	private decimal GetTotalSales() => _stockSummary.Sum(s => s.SaleStock);
	private decimal GetTotalClosingStock() => _stockSummary.Sum(s => s.ClosingStock);
	private decimal GetTotalStockValue() => _stockSummary.Sum(s => s.WeightedAverageValue);
	private decimal GetStockMovement() => _stockSummary.Sum(s => s.PurchaseStock + s.SaleStock);
	private decimal GetNetStockChange() => GetTotalClosingStock() - GetTotalOpeningStock();
	#endregion

	#region Chart Data Methods
	private List<StockOverviewRawMaterialChartData> GetStockOverviewData() =>
		[
			new() { Component = "Opening Stock", Value = GetTotalOpeningStock() },
			new() { Component = "Purchases", Value = GetTotalPurchases() },
			new() { Component = "Sales", Value = GetTotalSales() },
			new() { Component = "Closing Stock", Value = GetTotalClosingStock() }
		];

	private List<CategoryDistributionRawMaterialChartData> GetCategoryDistributionData() =>
		[.. _stockSummary
			.GroupBy(s => s.RawMaterialCategoryName)
			.Select(group => new CategoryDistributionRawMaterialChartData
			{
				CategoryName = group.Key,
				StockCount = group.Sum(s => s.ClosingStock)
			})
			.OrderByDescending(c => c.StockCount)
			.Take(10)];

	private List<TopMovingItemsRawMaterialChartData> GetTopMovingItemsData() =>
		[.. _stockSummary
			.Select(s => new TopMovingItemsRawMaterialChartData
			{
				ItemName = s.RawMaterialName,
				Movement = s.PurchaseStock + s.SaleStock
			})
			.OrderByDescending(i => i.Movement)
			.Take(10)];

	private List<OpeningClosingRawMaterialChartData> GetOpeningClosingData() =>
		[.. _stockSummary
			.Select(s => new OpeningClosingRawMaterialChartData
			{
				ItemName = s.RawMaterialName,
				OpeningStock = s.OpeningStock,
				ClosingStock = s.ClosingStock
			})
			.OrderByDescending(i => Math.Abs(i.ClosingStock - i.OpeningStock))
			.Take(10)];
	#endregion

	private async Task ExportStockSummaryToExcel()
	{
		if (_stockSummary is null || _stockSummary.Count == 0)
			return;

		_isProcessing = true;
		StateHasChanged();

		var startDate = _fromDate ?? DateOnly.FromDateTime(DateTime.Now);
		var endDate = _toDate ?? DateOnly.FromDateTime(DateTime.Now);
		var memoryStream = StockExcelExport.ExportRawMaterialStockSummaryExcel(_stockSummary, startDate, endDate);
		var fileName = $"Raw_Material_Stock_Summary_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
		VibrationService.VibrateWithTime(100);

		_isProcessing = false;
		StateHasChanged();
	}

	private async Task ExportStockDetailsToExcel()
	{
		if (_stockDetails is null || _stockDetails.Count == 0)
			return;

		_isProcessing = true;
		StateHasChanged();

		var startDate = _fromDate ?? DateOnly.FromDateTime(DateTime.Now);
		var endDate = _toDate ?? DateOnly.FromDateTime(DateTime.Now);
		var memoryStream = StockExcelExport.ExportRawMaterialStockDetailsExcel(_stockDetails, startDate, endDate);
		var fileName = $"Raw_Material_Stock_Details_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
		VibrationService.VibrateWithTime(100);

		_isProcessing = false;
		StateHasChanged();
	}

	#region Navigation Methods
	private void NavigateToReports()
	{
		;
	}

	private void NavigateToStockAdjustment()
	{
		;
	}
	#endregion
}