using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class StockReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedLocationId = 0;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<StockDetailModel> _stockDetails = [];

	private SfGrid<StockDetailModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;

		if (!await ValidatePassword())
			NavManager.NavigateTo("/Login");

		await LoadInitialData();

		_isLoading = false;
	}

	private async Task<bool> ValidatePassword()
	{
		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var password = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
			return false;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
		if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), password))
			return false;

		_user = user;
		return true;
	}

	private async Task LoadInitialData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);

		if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		await LoadStockDetails();
	}

	private async Task LoadStockDetails()
	{
		int locationId = _user?.LocationId == 1 ? _selectedLocationId : _user.LocationId;

		_stockDetails = await StockData.LoadStockDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			locationId);
	}

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

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);

	private async Task Logout()
	{
		await JS.InvokeVoidAsync("deleteCookie", "UserId");
		await JS.InvokeVoidAsync("deleteCookie", "Passcode");
		NavManager.NavigateTo("/Login");
	}

	private async Task ExportToPdf()
	{
		if (_sfGrid is not null)
			await _sfGrid.ExportToPdfAsync();
	}

	private async Task ExportToExcel()
	{
		if (_sfGrid is not null)
			await _sfGrid.ExportToExcelAsync();
	}

	// Chart data methods
	private List<StockOverviewData> GetStockOverviewData()
	{
		var result = new List<StockOverviewData>
		{
			new() { Component = "Opening Stock", Value = _stockDetails.Sum(s => s.OpeningStock) },
			new() { Component = "Purchases", Value = _stockDetails.Sum(s => s.PurchaseStock) },
			new() { Component = "Sales", Value = _stockDetails.Sum(s => s.SaleStock) },
			new() { Component = "Monthly Stock", Value = _stockDetails.Sum(s => s.MonthlyStock) },
			new() { Component = "Closing Stock", Value = _stockDetails.Sum(s => s.ClosingStock) }
		};

		return result;
	}

	private List<CategoryDistributionData> GetCategoryDistributionData()
	{
		var result = _stockDetails
			.GroupBy(s => s.RawMaterialCategoryName)
			.Select(group => new CategoryDistributionData
			{
				CategoryName = group.Key,
				StockCount = group.Sum(s => s.ClosingStock)
			})
			.OrderByDescending(c => c.StockCount)
			.Take(10)
			.ToList();

		return result;
	}

	private List<TopMovingItemsData> GetTopMovingItemsData()
	{
		var result = _stockDetails
			.Select(s => new TopMovingItemsData
			{
				ItemName = s.RawMaterialName,
				Movement = s.PurchaseStock + s.SaleStock
			})
			.OrderByDescending(i => i.Movement)
			.Take(10)
			.ToList();

		return result;
	}

	private List<OpeningClosingData> GetOpeningClosingData()
	{
		var result = _stockDetails
			.Select(s => new OpeningClosingData
			{
				ItemName = s.RawMaterialName,
				OpeningStock = s.OpeningStock,
				ClosingStock = s.ClosingStock
			})
			.OrderByDescending(i => Math.Abs(i.ClosingStock - i.OpeningStock))
			.Take(10)
			.ToList();

		return result;
	}

	// Chart data classes
	public class StockOverviewData
	{
		public string Component { get; set; }
		public decimal Value { get; set; }
	}

	public class CategoryDistributionData
	{
		public string CategoryName { get; set; }
		public decimal StockCount { get; set; }
	}

	public class TopMovingItemsData
	{
		public string ItemName { get; set; }
		public decimal Movement { get; set; }
	}

	public class OpeningClosingData
	{
		public string ItemName { get; set; }
		public decimal OpeningStock { get; set; }
		public decimal ClosingStock { get; set; }
	}
}