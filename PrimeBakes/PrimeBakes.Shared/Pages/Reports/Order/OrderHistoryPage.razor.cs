using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Order;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Order;

public partial class OrderHistoryPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _showCharts = true; // Charts hidden by default
	private bool _chartsLoading = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private string _selectedStatusFilter = "All";
	private int _selectedLocationId = 0;

	private List<LocationModel> _locations = [];
	private List<OrderOverviewModel> _orderOverviews = [];

	private SfGrid<OrderOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadLocations();
		await LoadOrderHistory();
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations.RemoveAll(l => l.Id == 1);
			_locations.Insert(0, new LocationModel { Id = 0, Name = "All Outlets / Franchises" });
		}
		else
			_selectedLocationId = _user.LocationId;
	}

	private async Task LoadOrderHistory()
	{
		var orders = await OrderData.LoadOrderDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_selectedLocationId);

		// Apply status filtering
		_orderOverviews = _selectedStatusFilter switch
		{
			"Pending" => [.. orders.Where(o => !o.SaleId.HasValue)],
			"Sold" => [.. orders.Where(o => o.SaleId.HasValue)],
			_ => orders // "All" or any other value
		};

		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadOrderHistory();
	}

	private async Task OnStatusFilterChanged(ChangeEventArgs<string, string> args)
	{
		_selectedStatusFilter = args.Value;
		await LoadOrderHistory();
	}

	private void ViewOrderDetails(OrderOverviewModel order) =>
		NavigationManager.NavigateTo($"/Reports/Order/View/{order.OrderId}");
	#endregion

	#region Excel
	private async Task ExportToProductsExcel()
	{
		var pendingOrders = _orderOverviews?.Where(o => o.SaleId == null).ToList();
		if (pendingOrders is null || pendingOrders.Count == 0)
			return;

		var memoryStream = await OrderExcelExport.ExportPendingProductsExcel(pendingOrders, _startDate, _endDate);
		var fileName = $"Pending_Orders_Products_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
	}

	private async Task ExportToExcel()
	{
		var memoryStream = OrderExcelExport.ExportOrderOverviewExcel(_orderOverviews, _startDate, _endDate);
		var fileName = $"Order_History_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetOrderTrendsData()
	{
		if (_orderOverviews is null || _orderOverviews.Count == 0)
			return [];

		return [.. _orderOverviews
			.GroupBy(o => o.OrderDateTime.Date)
			.Select(g => new
			{
				Date = g.Key,
				Count = g.Count()
			})
			.OrderBy(x => x.Date)
			.Cast<object>()];
	}

	private List<object> GetStatusDistributionData()
	{
		if (_orderOverviews is null || _orderOverviews.Count == 0)
			return [];

		var pendingCount = _orderOverviews.Count(o => !o.SaleId.HasValue);
		var completedCount = _orderOverviews.Count(o => o.SaleId.HasValue);

		var data = new List<object>();

		if (pendingCount > 0)
			data.Add(new { Status = "Pending", Count = pendingCount });

		if (completedCount > 0)
			data.Add(new { Status = "Completed", Count = completedCount });

		return data;
	}

	private List<object> GetLocationDistributionData()
	{
		if (_orderOverviews is null || _orderOverviews.Count == 0)
			return [];

		return [.. _orderOverviews
			.GroupBy(o => o.LocationName)
			.Select(g => new
			{
				Location = g.Key ?? "Unknown",
				Count = g.Count()
			})
			.OrderByDescending(x => x.Count)
			.Cast<object>()];
	}

	private async Task OnLocationFilterChanged(ChangeEventArgs<int?, LocationModel> args)
	{
		_selectedLocationId = args.Value ?? 0;
		await LoadOrderHistory();
	}
	#endregion
}