using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Stock;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Stock;

public partial class ProductStockReport
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showCharts = true;
	private bool _showSummaryView = true; // true for Summary, false for Details

	private int _selectedLocationId = 1;
	private DateOnly? _fromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly? _toDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<ProductStockSummaryModel> _stockSummary = [];
	private List<ProductStockDetailsModel> _stockDetails = [];

	private SfGrid<ProductStockSummaryModel> _sfStockSummaryGrid;
	private SfGrid<ProductStockDetailsModel> _sfStockDetailsGrid;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;
		await LoadLocations();
		await LoadStockReport();
		_isLoading = false;
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		}

		_selectedLocationId = _user.LocationId;
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		if (args is null || args.Value <= 0)
			return;

		_selectedLocationId = args.Value;
		await LoadStockReport();
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
		StateHasChanged();

		var startDate = (_fromDate ?? DateOnly.FromDateTime(DateTime.Now)).ToDateTime(new TimeOnly(0, 0));
		var endDate = (_toDate ?? DateOnly.FromDateTime(DateTime.Now)).ToDateTime(new TimeOnly(23, 59));

		_stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(startDate, endDate, _selectedLocationId);
		_stockDetails = await ProductStockData.LoadProductStockDetailsByDateLocationId(startDate, endDate, _selectedLocationId);

		_stockSummary.RemoveAll(s => s.OpeningStock == 0 && s.PurchaseStock == 0 && s.SaleStock == 0 && s.ClosingStock == 0);

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
	private decimal GetTotalProduction() => _stockSummary.Sum(s => s.PurchaseStock);
	private decimal GetTotalSales() => _stockSummary.Sum(s => s.SaleStock);
	private decimal GetTotalClosingStock() => _stockSummary.Sum(s => s.ClosingStock);
	private decimal GetTotalStockValue() => _stockSummary.Sum(s => s.WeightedAverageValue);
	private decimal GetStockMovement() => _stockSummary.Sum(s => s.PurchaseStock + s.SaleStock);
	private decimal GetNetStockChange() => GetTotalClosingStock() - GetTotalOpeningStock();
	#endregion

	#region Chart Data Methods
	private List<StockOverviewProductChartData> GetStockOverviewData() =>
		[
			new() { Component = "Opening Stock", Value = GetTotalOpeningStock() },
			new() { Component = "Production", Value = GetTotalProduction() },
			new() { Component = "Sales", Value = GetTotalSales() },
			new() { Component = "Closing Stock", Value = GetTotalClosingStock() }
		];

	private List<CategoryDistributionProductChartData> GetCategoryDistributionData() =>
		[.. _stockSummary
			.GroupBy(s => s.ProductCategoryName)
			.Select(group => new CategoryDistributionProductChartData
			{
				CategoryName = group.Key,
				StockCount = group.Sum(s => s.ClosingStock)
			})
			.OrderByDescending(c => c.StockCount)
			.Take(10)];

	private List<TopMovingItemsProductChartData> GetTopMovingItemsData() =>
		[.. _stockSummary
			.Select(s => new TopMovingItemsProductChartData
			{
				ItemName = s.ProductName,
				Movement = s.PurchaseStock + s.SaleStock
			})
			.OrderByDescending(i => i.Movement)
			.Take(10)];

	private List<OpeningClosingProductChartData> GetOpeningClosingData() =>
		[.. _stockSummary
			.Select(s => new OpeningClosingProductChartData
			{
				ItemName = s.ProductName,
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
		var locationName = _locations.FirstOrDefault(l => l.Id == _selectedLocationId)?.Name ?? "Unknown Location";

		var memoryStream = ProductStockExcelExport.ExportFinishedProductStockSummaryExcel(_stockSummary, startDate, endDate, _selectedLocationId, _locations);
		var fileName = $"Product_Stock_Summary_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}_{locationName}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);

		VibrationService.VibrateWithTime(100);
		await NotificationService.ShowLocalNotification(101, "Export", "Product Stock Summary Export", "Product stock summary has been exported successfully.");

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
		var locationName = _locations.FirstOrDefault(l => l.Id == _selectedLocationId)?.Name ?? "Unknown Location";

		var memoryStream = ProductStockExcelExport.ExportFinishedProductStockDetailsExcel(_stockDetails, startDate, endDate, _selectedLocationId, _locations);
		var fileName = $"Product_Stock_Details_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}_{locationName}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);

		VibrationService.VibrateWithTime(100);
		await NotificationService.ShowLocalNotification(102, "Export", "Product Stock Details Export", "Product stock details have been exported successfully.");

		_isProcessing = false;
		StateHasChanged();
	}
}

#region Chart Data Models
public class StockOverviewProductChartData
{
	public string Component { get; set; } = string.Empty;
	public decimal Value { get; set; }
}

public class CategoryDistributionProductChartData
{
	public string CategoryName { get; set; } = string.Empty;
	public decimal StockCount { get; set; }
}

public class TopMovingItemsProductChartData
{
	public string ItemName { get; set; } = string.Empty;
	public decimal Movement { get; set; }
}

public class OpeningClosingProductChartData
{
	public string ItemName { get; set; } = string.Empty;
	public decimal OpeningStock { get; set; }
	public decimal ClosingStock { get; set; }
}
#endregion