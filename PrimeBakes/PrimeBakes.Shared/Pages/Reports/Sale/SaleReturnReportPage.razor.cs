using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class SaleReturnReportPage
{
	[Parameter] public int? SelectedLocationId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	// Data collections
	private List<SaleReturnOverviewModel> _saleReturnOverviews = [];
	private List<LocationModel> _locations = [];
	private LocationModel _selectedLocation;

	// Filter properties
	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int? _selectedLocationId;

	// Grid reference
	private SfGrid<SaleReturnOverviewModel> _sfGrid;

	// Chart data models
	public class DailyReturnData
	{
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }
		public int Count { get; set; }
	}

	public class LocationReturnData
	{
		public string LocationName { get; set; }
		public decimal Amount { get; set; }
		public int Count { get; set; }
	}

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;

		// Handle location parameter from URL
		if (SelectedLocationId.HasValue && _user.LocationId == 1)
			_selectedLocationId = SelectedLocationId.Value;
		else if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadLocations();
		await LoadSaleReturnData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);

			// Add "All Locations" option for primary location users
			_locations.Insert(0, new LocationModel { Id = 0, Name = "All Locations" });

			if (!_selectedLocationId.HasValue)
			{
				_selectedLocationId = 0;
			}
			_selectedLocation = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);
		}
		else
		{
			// Non-primary users can only see their own location
			var userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);
			if (userLocation != null)
			{
				_locations = [userLocation];
				_selectedLocationId = _user.LocationId;
				_selectedLocation = userLocation;
			}
		}
	}

	private async Task LoadSaleReturnData()
	{
		try
		{
			if (_user.LocationId == 1 && (_selectedLocationId == null || _selectedLocationId == 0))
			{
				// Load all sale returns for primary location when "All Locations" is selected
				_saleReturnOverviews = [];
				foreach (var location in _locations.Where(l => l.Id > 0))
				{
					var locationReturns = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
						_startDate.ToDateTime(new TimeOnly(0, 0)),
						_endDate.ToDateTime(new TimeOnly(23, 59)),
						location.Id);
					_saleReturnOverviews.AddRange(locationReturns);
				}
			}
			else
			{
				// Load returns for specific location
				var locationId = _selectedLocationId ?? _user.LocationId;
				_saleReturnOverviews = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
					_startDate.ToDateTime(new TimeOnly(0, 0)),
					_endDate.ToDateTime(new TimeOnly(23, 59)),
					locationId);
			}

			// Sort by return date descending
			_saleReturnOverviews = _saleReturnOverviews.OrderByDescending(r => r.ReturnDateTime).ToList();
		}
		catch (Exception ex)
		{
			await NotificationService.ShowLocalNotification(999, "Error", "Data Load Failed", $"Failed to load sale return data: {ex.Message}");
			_saleReturnOverviews = [];
		}
	}

	#region Event Handlers

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadSaleReturnData();
	}

	private async Task OnLocationFilterChanged(ChangeEventArgs<int?, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		_selectedLocation = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);
		await LoadSaleReturnData();
	}

	#endregion

	#region Chart Data Methods

	private List<DailyReturnData> GetDailyReturnData()
	{
		return _saleReturnOverviews
			.GroupBy(r => r.ReturnDateTime.Date)
			.Select(g => new DailyReturnData
			{
				Date = g.Key,
				Amount = g.Sum(r => r.Total),
				Count = g.Count()
			})
			.OrderBy(d => d.Date)
			.ToList();
	}

	private List<LocationReturnData> GetLocationReturnData()
	{
		return _saleReturnOverviews
			.GroupBy(r => r.LocationName)
			.Select(g => new LocationReturnData
			{
				LocationName = g.Key,
				Amount = g.Sum(r => r.Total),
				Count = g.Count()
			})
			.OrderByDescending(l => l.Amount)
			.ToList();
	}

	#endregion

	#region Export Methods

	private async Task ExportExcel()
	{
		try
		{
			var memoryStream = SaleReturnExcelExport.ExportSaleReturnOverviewExcel(
				_saleReturnOverviews,
				_startDate,
				_endDate,
				_selectedLocationId ?? 0,
				_locations);

			var fileName = $"Sale_Return_Report_{_startDate:yyyyMMdd}_{_endDate:yyyyMMdd}.xlsx";
			await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);

			await NotificationService.ShowLocalNotification(1, "Export Successful", "Excel Export", "Sale return report has been exported to Excel successfully.");
		}
		catch (Exception ex)
		{
			await NotificationService.ShowLocalNotification(999, "Export Failed", "Excel Export Error", $"Failed to export to Excel: {ex.Message}");
		}
	}
	#endregion

	#region Navigation Methods

	private void ViewReturnDetails(int saleReturnId)
	{
		NavigationManager.NavigateTo($"/Reports/SaleReturnView/{saleReturnId}");
	}

	#endregion
}