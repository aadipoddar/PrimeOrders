using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Order;

public partial class OrderHistoryPage
{
	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private int _selectedLocationId = 0;
	private bool _isLoading = true;
	private bool _orderSummaryVisible = false;
	private string _selectedStatusFilter = "All";

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private OrderOverviewModel _selectedOrder;

	private readonly List<OrderDetailDisplayModel> _selectedOrderDetails = [];
	private List<OrderOverviewModel> _orderOverviews = [];
	private List<LocationModel> _locations = [];

	private SfGrid<OrderOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Order)).User) is not null))
			return;

		await LoadLocations();
		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
			_locations.RemoveAll(l => l.Id == 1);
			_selectedLocationId = 0;
		}
		else
			_selectedLocationId = _user.LocationId;
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadData();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadData();
	}

	private async Task OnStatusFilterChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, string> args)
	{
		_selectedStatusFilter = args.Value;
		await LoadData();
	}

	private async Task LoadData()
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
	#endregion

	#region Order Summary Module Methods
	private async Task OnOrderRowSelected(RowSelectEventArgs<OrderOverviewModel> args)
	{
		_selectedOrder = args.Data;
		await LoadOrderDetails(_selectedOrder.OrderId);
		_orderSummaryVisible = true;
		StateHasChanged();
	}

	private async Task LoadOrderDetails(int orderId)
	{
		_selectedOrderDetails.Clear();

		var orderDetails = await OrderData.LoadOrderDetailByOrder(orderId);
		foreach (var detail in orderDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			if (product is not null)
				_selectedOrderDetails.Add(new()
				{
					ProductName = product.Name,
					Quantity = detail.Quantity
				});
		}
	}

	private async Task OnOrderSummaryVisibilityChanged(bool isVisible)
	{
		_orderSummaryVisible = isVisible;
		await LoadData();
		StateHasChanged();
	}
	#endregion

	#region Excel
	private async Task ExportToProductsExcel()
	{
		var pendingOrders = _orderOverviews?.Where(o => o.SaleId == null).ToList();
		if (pendingOrders is null || pendingOrders.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No pending orders to export");
			return;
		}

		var memoryStream = await OrderExcelExport.ExportPendingProductsExcel(pendingOrders, _startDate, _endDate);
		var fileName = $"Pending_Orders_Products_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	private async Task ExportToExcel()
	{
		if (_orderOverviews is null || _orderOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = OrderExcelExport.ExportOrderOverviewExcel(_orderOverviews, _startDate, _endDate);
		var fileName = $"Order_History_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Charts
	private List<OrderChartData> GetDailyOrdersData()
	{
		var result = new List<OrderChartData>();
		if (_orderOverviews == null || _orderOverviews.Count == 0)
			return result;

		var groupedByDate = _orderOverviews
			.GroupBy(o => o.OrderDate)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (var group in groupedByDate)
			result.Add(new()
			{
				Date = group.Key.ToString("dd/MM/yyyy"),
				Count = group.Count()
			});

		return result;
	}

	private List<OrderStatusData> GetOrderStatusData()
	{
		var result = new List<OrderStatusData>();
		if (_orderOverviews == null || _orderOverviews.Count == 0)
			return result;

		int pendingCount = _orderOverviews.Count(o => o.SaleId is null);
		int completedCount = _orderOverviews.Count(o => o.SaleId is not null);

		result.Add(new() { Status = "Pending", Count = pendingCount });
		result.Add(new() { Status = "Completed", Count = completedCount });

		return result;
	}
	#endregion
}