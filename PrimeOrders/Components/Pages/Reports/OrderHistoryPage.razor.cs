using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class OrderHistoryPage
{
	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<OrderOverviewModel> _orderOverviews = [];
	private List<LocationModel> _locations = [];
	private int _selectedLocationId = 0;

	private SfGrid<OrderOverviewModel> _sfGrid;

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

	private async Task LoadData()
	{
		_orderOverviews = await OrderData.LoadOrderDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_selectedLocationId);

		StateHasChanged();
	}

	public void OrderHistoryRowSelected(RowSelectEventArgs<OrderOverviewModel> args) =>
		ViewOrderDetails(args.Data.OrderId);

	private void ViewOrderDetails(int orderId) =>
		NavManager.NavigateTo($"/Order/{orderId}");

	private async Task ExportToPdf() =>
		await _sfGrid?.ExportToPdfAsync();

	private async Task ExportToExcel()
	{
		if (_orderOverviews is null || _orderOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Orders", _orderOverviews.Count },
			{ "Total Products", _orderOverviews.Sum(o => o.TotalProducts) },
			{ "Total Quantity", _orderOverviews.Sum(o => o.TotalQuantity) },
			{ "Completed Orders", _orderOverviews.Count(o => o.SaleId is not null) },
			{ "Pending Orders", _orderOverviews.Count(o => o.SaleId is null) }
		};

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(OrderOverviewModel.OrderNo),
			nameof(OrderOverviewModel.OrderDate),
			nameof(OrderOverviewModel.LocationName),
			nameof(OrderOverviewModel.UserName),
			nameof(OrderOverviewModel.TotalProducts),
			nameof(OrderOverviewModel.TotalQuantity),
			nameof(OrderOverviewModel.SaleBillNo)
		];

		// Generate the Excel file
		string reportTitle = "Order History Report";
		string worksheetName = "Order Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_orderOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			[],
			columnOrder);

		var fileName = $"Order_History_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Chart data methods
	private List<ChartData> GetDailyOrdersData()
	{
		var result = new List<ChartData>();
		if (_orderOverviews == null || _orderOverviews.Count == 0)
			return result;

		var groupedByDate = _orderOverviews
			.GroupBy(o => o.OrderDate)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (var group in groupedByDate)
		{
			result.Add(new ChartData
			{
				Date = group.Key.ToString("dd/MM/yyyy"),
				Count = group.Count()
			});
		}

		return result;
	}

	private List<StatusData> GetOrderStatusData()
	{
		var result = new List<StatusData>();
		if (_orderOverviews == null || _orderOverviews.Count == 0)
			return result;

		int pendingCount = _orderOverviews.Count(o => o.SaleId is null);
		int completedCount = _orderOverviews.Count(o => o.SaleId is not null);

		result.Add(new StatusData { Status = "Pending", Count = pendingCount });
		result.Add(new StatusData { Status = "Completed", Count = completedCount });

		return result;
	}

	// Chart data classes
	public class ChartData
	{
		public string Date { get; set; }
		public int Count { get; set; }
	}

	public class StatusData
	{
		public string Status { get; set; }
		public int Count { get; set; }
	}
}