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

public partial class KitchenIssueReportPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _showCharts = false; // Charts hidden by default
	private bool _chartsLoading = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private int _selectedKitchenId = 0;

	private List<KitchenModel> _kitchens = [];
	private List<KitchenIssueOverviewModel> _kitchenIssueOverviews = [];

	private SfGrid<KitchenIssueOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadKitchens();
		await LoadKitchenIssueData();
		StateHasChanged();
	}

	private async Task LoadKitchens()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		_kitchens.Insert(0, new KitchenModel { Id = 0, Name = "All Kitchens" });
	}

	private async Task LoadKitchenIssueData()
	{
		var issues = await KitchenIssueData.LoadKitchenIssueDetailsByDate(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)));

		// Apply kitchen filtering
		_kitchenIssueOverviews = _selectedKitchenId > 0
			? [.. issues.Where(i => i.KitchenId == _selectedKitchenId)]
			: issues;

		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadKitchenIssueData();
	}

	private async Task OnKitchenFilterChanged(ChangeEventArgs<int, KitchenModel> args)
	{
		_selectedKitchenId = args.Value;
		await LoadKitchenIssueData();
	}

	private async Task RefreshData()
	{
		_isLoading = true;
		StateHasChanged();
		await LoadKitchenIssueData();
		_isLoading = false;
		StateHasChanged();
	}

	private void ViewIssueDetails(KitchenIssueOverviewModel issue) =>
		NavigationManager.NavigateTo($"/Reports/Kitchen-Issue/View/{issue.KitchenIssueId}");

	private void ViewIssueDetails(RowSelectEventArgs<KitchenIssueOverviewModel> args) =>
		ViewIssueDetails(args.Data);
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
			return;

		var memoryStream = await KitchenIssueExcelExport.ExportKitchenIssueOverviewExcel(_kitchenIssueOverviews, _startDate, _endDate, _selectedKitchenId);
		var fileName = $"Kitchen_Issue_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetKitchenWiseData()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
			return [];

		return [.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new
			{
				KitchenName = g.Key ?? "Unknown",
				IssueCount = g.Count(),
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				TotalAmount = g.Sum(x => x.TotalAmount)
			})
			.OrderByDescending(x => x.IssueCount)
			.Cast<object>()];
	}

	private List<object> GetDailyIssueData()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
			return [];

		return [.. _kitchenIssueOverviews
			.GroupBy(i => i.IssueDate.Date)
			.Select(g => new
			{
				Date = g.Key.ToString("MMM dd"),
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				IssueCount = g.Count()
			})
			.OrderBy(x => x.Date)
			.Cast<object>()];
	}

	private List<object> GetPieChartData()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
			return [];

		return [.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new
			{
				KitchenName = g.Key ?? "Unknown",
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				TotalAmount = g.Sum(x => x.TotalAmount),
				IssueCount = g.Count()
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Cast<object>()];
	}

	private List<object> GetMonthlyTrendData()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
			return [];

		return [.. _kitchenIssueOverviews
			.GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
			.Select(g => new
			{
				Month = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				TotalAmount = g.Sum(x => x.TotalAmount),
				IssueCount = g.Count()
			})
			.OrderBy(x => x.Month)
			.Cast<object>()];
	}

	private List<object> GetTopMaterialsData()
	{
		if (_kitchenIssueOverviews is null || _kitchenIssueOverviews.Count == 0)
			return [];

		return [.. _kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.Select(g => new
			{
				Kitchen = g.Key ?? "Unknown",
				TotalProducts = g.Sum(x => x.TotalProducts),
				TotalQuantity = g.Sum(x => x.TotalQuantity),
				AveragePerIssue = g.Average(x => x.TotalProducts)
			})
			.OrderByDescending(x => x.TotalProducts)
			.Take(10)
			.Cast<object>()];
	}

	// Summary calculations
	private decimal GetTotalIssueValue()
	{
		return _kitchenIssueOverviews?.Sum(x => x.TotalAmount) ?? 0;
	}

	private decimal GetAverageIssueValue()
	{
		if (_kitchenIssueOverviews?.Count > 0)
			return _kitchenIssueOverviews.Average(x => x.TotalAmount);
		return 0;
	}

	private int GetTotalMaterialTypes()
	{
		return _kitchenIssueOverviews?.Sum(x => x.TotalProducts) ?? 0;
	}

	private decimal GetTotalQuantityIssued()
	{
		return _kitchenIssueOverviews?.Sum(x => x.TotalQuantity) ?? 0;
	}

	// Performance metrics
	private string GetMostActiveKitchen()
	{
		if (_kitchenIssueOverviews?.Count > 0)
		{
			var mostActive = _kitchenIssueOverviews
				.GroupBy(x => x.KitchenName)
				.OrderByDescending(g => g.Count())
				.FirstOrDefault();
			return mostActive?.Key ?? "N/A";
		}
		return "N/A";
	}

	private DateTime? GetLastIssueDate()
	{
		return _kitchenIssueOverviews?.OrderByDescending(x => x.IssueDate).FirstOrDefault()?.IssueDate;
	}

	private int GetTotalKitchensInvolved()
	{
		return _kitchenIssueOverviews?.Select(x => x.KitchenId).Distinct().Count() ?? 0;
	}

	private string GetDateRangeText()
	{
		if (_startDate == _endDate)
			return _startDate.ToString("dd MMM yyyy");
		return $"{_startDate:dd MMM yyyy} - {_endDate:dd MMM yyyy}";
	}
	#endregion
}