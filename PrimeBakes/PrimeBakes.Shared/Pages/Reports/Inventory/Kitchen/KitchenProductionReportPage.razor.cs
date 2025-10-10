using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Kitchen;

public partial class KitchenProductionReportPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _showCharts = true; // Charts hidden by default
	private bool _chartsLoading = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private int _selectedKitchenId = 0;

	private List<KitchenModel> _kitchens = [];
	private List<KitchenProductionOverviewModel> _kitchenProductionOverviews = [];

	private SfGrid<KitchenProductionOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadKitchens();
		await LoadKitchenProductionData();
		StateHasChanged();
	}

	private async Task LoadKitchens()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		_kitchens.Insert(0, new KitchenModel { Id = 0, Name = "All Kitchens" });
	}

	private async Task LoadKitchenProductionData()
	{
		var productions = await KitchenProductionData.LoadKitchenProductionDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		// Apply kitchen filtering
		_kitchenProductionOverviews = _selectedKitchenId > 0
			? [.. productions.Where(p => p.KitchenId == _selectedKitchenId)]
			: productions;

		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadKitchenProductionData();
	}

	private async Task OnKitchenFilterChanged(ChangeEventArgs<int, KitchenModel> args)
	{
		_selectedKitchenId = args.Value;
		await LoadKitchenProductionData();
	}

	private async Task RefreshData()
	{
		_isLoading = true;
		StateHasChanged();
		await LoadKitchenProductionData();
		_isLoading = false;
		StateHasChanged();
	}

	private void ViewProductionDetails(KitchenProductionOverviewModel production) =>
		NavigationManager.NavigateTo($"/Reports/Kitchen-Production/View/{production.KitchenProductionId}");

	private void ViewProductionDetails(RowSelectEventArgs<KitchenProductionOverviewModel> args) =>
		ViewProductionDetails(args.Data);
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
			return;

		var memoryStream = await KitchenProductionExcelExport.ExportKitchenProductionOverviewExcel(_kitchenProductionOverviews, _startDate, _endDate, _selectedKitchenId);
		var fileName = $"Kitchen_Production_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetKitchenWiseData()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
			return [];

		return [.. _kitchenProductionOverviews
			.GroupBy(p => p.KitchenName)
			.Select(g => new
			{
				KitchenName = g.Key ?? "Unknown",
				ProductionCount = g.Count(),
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				TotalAmount = g.Sum(x => x.TotalAmount)
			})
			.OrderByDescending(x => x.ProductionCount)
			.Cast<object>()];
	}

	private List<object> GetDailyProductionData()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
			return [];

		return [.. _kitchenProductionOverviews
			.GroupBy(p => p.ProductionDate.Date)
			.Select(g => new
			{
				Date = g.Key.ToString("MMM dd"),
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				ProductionCount = g.Count()
			})
			.OrderBy(x => x.Date)
			.Cast<object>()];
	}

	private List<object> GetPieChartData()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
			return [];

		return [.. _kitchenProductionOverviews
			.GroupBy(p => p.KitchenName)
			.Select(g => new
			{
				KitchenName = g.Key ?? "Unknown",
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				TotalAmount = g.Sum(x => x.TotalAmount),
				ProductionCount = g.Count()
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Cast<object>()];
	}

	private List<object> GetMonthlyTrendData()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
			return [];

		return [.. _kitchenProductionOverviews
			.GroupBy(p => new { p.ProductionDate.Year, p.ProductionDate.Month })
			.Select(g => new
			{
				Month = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				TotalAmount = g.Sum(x => x.TotalAmount),
				ProductionCount = g.Count()
			})
			.OrderBy(x => x.Month)
			.Cast<object>()];
	}

	private List<object> GetTopProductsData()
	{
		if (_kitchenProductionOverviews is null || _kitchenProductionOverviews.Count == 0)
			return [];

		return [.. _kitchenProductionOverviews
			.GroupBy(p => p.KitchenName)
			.Select(g => new
			{
				Kitchen = g.Key ?? "Unknown",
				TotalProducts = g.Sum(x => x.TotalProducts),
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				AveragePerProduction = g.Average(x => x.TotalProducts)
			})
			.OrderByDescending(x => x.TotalProducts)
			.Take(10)
			.Cast<object>()];
	}

	// Summary calculations
	private decimal GetTotalProductionValue()
	{
		return _kitchenProductionOverviews?.Sum(x => x.TotalAmount) ?? 0;
	}

	private decimal GetAverageProductionValue()
	{
		if (_kitchenProductionOverviews?.Count > 0)
			return _kitchenProductionOverviews.Average(x => x.TotalAmount);
		return 0;
	}

	private int GetTotalProductTypes()
	{
		return _kitchenProductionOverviews?.Sum(x => x.TotalProducts) ?? 0;
	}

	private decimal GetTotalQuantityProduced()
	{
		return _kitchenProductionOverviews?.Sum(x => x.TotalQuantity) ?? 0;
	}

	// Performance metrics
	private string GetMostActiveKitchen()
	{
		if (_kitchenProductionOverviews?.Count > 0)
		{
			var mostActive = _kitchenProductionOverviews
				.GroupBy(x => x.KitchenName)
				.OrderByDescending(g => g.Count())
				.FirstOrDefault();
			return mostActive?.Key ?? "N/A";
		}
		return "N/A";
	}

	private DateTime? GetLastProductionDate()
	{
		return _kitchenProductionOverviews?.OrderByDescending(x => x.ProductionDate).FirstOrDefault()?.ProductionDate;
	}

	private int GetTotalKitchensInvolved()
	{
		return _kitchenProductionOverviews?.Select(x => x.KitchenId).Distinct().Count() ?? 0;
	}

	private string GetDateRangeText()
	{
		if (_startDate == _endDate)
			return _startDate.ToString("dd MMM yyyy");
		return $"{_startDate:dd MMM yyyy} - {_endDate:dd MMM yyyy}";
	}
	#endregion
}