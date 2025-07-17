using PrimeOrdersLibrary.Exporting.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Sale;

public partial class SaleReturnReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private LocationModel _userLocation;
	private bool _isLoading = true;
	private bool _saleReturnSummaryVisible = false;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedLocationId = 0;

	private List<SaleReturnOverviewModel> _saleReturnOverviews = [];
	private List<LocationModel> _locations = [];

	private SaleReturnOverviewModel _selectedSaleReturn;
	private List<SaleReturnProductCartModel> _selectedSaleReturnDetails = [];

	private SfGrid<SaleReturnOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Sales, primaryLocationRequirement: true)).User) is not null))
			return;

		_userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

		await LoadLocations();
		await LoadSaleReturnData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_userLocation.MainLocation)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations.Insert(0, new LocationModel { Id = 0, Name = "All Locations" });
		}
	}

	private async Task LoadSaleReturnData()
	{
		if (!_userLocation.MainLocation)
			_selectedLocationId = _user.LocationId;

		_saleReturnOverviews = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
				_startDate.ToDateTime(new TimeOnly(0, 0)),
				_endDate.ToDateTime(new TimeOnly(23, 59)),
				_selectedLocationId);
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await RefreshData();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await RefreshData();
	}

	private async Task RefreshData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadSaleReturnData();

		_isLoading = false;
		StateHasChanged();
	}
	#endregion

	#region Summary Module Methods
	private async void OnRowSelected(RowSelectEventArgs<SaleReturnOverviewModel> args)
	{
		_selectedSaleReturn = _saleReturnOverviews.FirstOrDefault(s => s.SaleReturnId == args.Data.SaleReturnId);
		if (_selectedSaleReturn is null)
			return;

		var saleReturns = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(args.Data.SaleReturnId);
		foreach (var saleReturn in saleReturns)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, saleReturn.ProductId);
			if (product is not null)
				_selectedSaleReturnDetails.Add(new()
				{
					ProductId = product.Id,
					ProductName = product.Name,
					Quantity = saleReturn.Quantity
				});
		}

		_saleReturnSummaryVisible = true;
		StateHasChanged();
	}

	private void OnSaleReturnSummaryVisibilityChanged(bool isVisible)
	{
		_saleReturnSummaryVisible = isVisible;
		StateHasChanged();
	}
	#endregion

	#region Location Analysis Methods
	private List<LocationGroupSaleReturnChartData> GetLocationGroups() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => new { sr.LocationId, sr.LocationName })
			.Select(g => new LocationGroupSaleReturnChartData
			{
				LocationId = g.Key.LocationId,
				LocationName = g.Key.LocationName,
				TotalReturns = g.Count(),
				TotalProducts = g.Sum(x => x.TotalProducts),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalReturns)];

	private string GetMostActiveDay(int locationId)
	{
		var dayGroup = _saleReturnOverviews
			.Where(sr => sr.LocationId == locationId)
			.GroupBy(sr => sr.ReturnDateTime.DayOfWeek)
			.OrderByDescending(g => g.Count())
			.FirstOrDefault();

		return dayGroup?.Key.ToString() ?? "N/A";
	}

	private string GetLatestReturnDate(int locationId)
	{
		var latestReturn = _saleReturnOverviews
			.Where(sr => sr.LocationId == locationId)
			.OrderByDescending(sr => sr.ReturnDateTime)
			.FirstOrDefault();

		return latestReturn?.ReturnDateTime.ToString("dd/MM/yyyy") ?? "N/A";
	}

	private string GetReturnRate(int locationId)
	{
		var locationReturns = _saleReturnOverviews.Where(sr => sr.LocationId == locationId);
		var totalDays = (_endDate.ToDateTime(TimeOnly.MinValue) - _startDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
		var rate = locationReturns.Count() / (double)totalDays;
		return $"{rate:F1}/day";
	}

	private string GetStaffCount(int locationId)
	{
		var uniqueStaff = _saleReturnOverviews
			.Where(sr => sr.LocationId == locationId)
			.Select(sr => sr.UserId)
			.Distinct()
			.Count();

		return uniqueStaff.ToString();
	}

	private List<DailySaleReturnChartData> GetLocationDailyData(int locationId) =>
		[.. _saleReturnOverviews
			.Where(sr => sr.LocationId == locationId)
			.GroupBy(sr => DateOnly.FromDateTime(sr.ReturnDateTime))
			.Select(g => new DailySaleReturnChartData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private async Task FilterByLocation(int locationId)
	{
		_selectedLocationId = locationId;
		await RefreshData();
	}
	#endregion

	#region Chart Data Methods
	private List<LocationWiseSaleReturnData> GetLocationWiseData() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => sr.LocationName)
			.Select(g => new LocationWiseSaleReturnData
			{
				LocationName = g.Key,
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(10)];

	private List<DailySaleReturnChartData> GetDailyReturnData() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => DateOnly.FromDateTime(sr.ReturnDateTime))
			.Select(g => new DailySaleReturnChartData
			{
				Date = g.Key.ToString("dd/MM"),
				TotalQuantity = g.Sum(x => x.TotalQuantity)
			})
			.OrderBy(x => DateOnly.Parse(x.Date, new System.Globalization.CultureInfo("en-GB")))];

	private List<ProductCategorySaleReturnChartData> GetProductCategoryData() =>
		[.. _saleReturnOverviews
			.GroupBy(sr => sr.LocationName)
			.Select(g => new ProductCategorySaleReturnChartData
			{
				CategoryName = g.Key,
				ProductCount = g.Count()
			})
			.OrderByDescending(x => x.ProductCount)
			.Take(10)];
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_saleReturnOverviews is null || _saleReturnOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		var memoryStream = SaleReturnExcelExport.ExportSaleReturnOverviewExcel(_saleReturnOverviews, _startDate, _endDate);
		var fileName = $"Sale_Return_Report_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion
}