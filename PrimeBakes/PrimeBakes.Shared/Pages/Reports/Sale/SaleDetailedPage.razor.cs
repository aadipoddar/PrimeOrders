using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class SaleDetailedPage
{
	[Parameter] public int? SelectedLocationId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	// Data collections
	private List<SaleOverviewModel> _saleOverviews = [];
	private List<SaleOverviewModel> _filteredSaleOverviews = [];
	private List<LocationModel> _locations = [];
	private List<LedgerModel> _suppliers = [];
	private LocationModel _selectedLocation;
	private LedgerModel _selectedSupplier = null;

	// Filter properties
	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int? _selectedLocationId;
	private string _selectedPaymentFilter = "All";

	// Grid reference
	private SfGrid<SaleOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;

		// Handle location parameter from URL
		if (SelectedLocationId.HasValue && _user.LocationId == 1)
			_selectedLocationId = SelectedLocationId.Value;
		else if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		await LoadLocations();
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);

			// Add "All Locations" option for location 1 users
			_locations.Insert(0, new() { Id = 0, Name = "All Locations" });

			_selectedLocationId = 0;
			_selectedLocation = _locations.FirstOrDefault(l => l.Id == 0);

			_suppliers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
		}
		else
		{
			// Non-admin users can only see their own location
			var userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);
			if (userLocation is not null)
			{
				_locations = [userLocation];
				_selectedLocationId = _user.LocationId;
				_selectedLocation = userLocation;
			}
		}
	}

	private async Task LoadData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadSalesData();
		await ApplyFilters();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadSalesData()
	{
		var fromDate = _startDate.ToDateTime(TimeOnly.MinValue);
		var toDate = _endDate.ToDateTime(TimeOnly.MaxValue);

		if (_user.LocationId == 1 && (_selectedLocationId is null || _selectedLocationId == 0))
		{
			_saleOverviews = [];
			// Load all sales for admin when "All Locations" is selected
			foreach (var location in _locations.Where(l => l.Id > 0))
			{
				var locationSales = await SaleData.LoadSaleDetailsByDateLocationId(fromDate, toDate, location.Id);
				_saleOverviews.AddRange(locationSales);
			}
		}
		else
		{
			// Load sales for specific location
			var locationId = _selectedLocationId ?? _user.LocationId;
			_saleOverviews = await SaleData.LoadSaleDetailsByDateLocationId(fromDate, toDate, locationId);
		}

		// Sort by date ascending
		_saleOverviews = [.. _saleOverviews.OrderBy(s => s.SaleDateTime).ThenBy(s => s.BillNo)];
	}

	private async Task ApplyFilters()
	{
		var filtered = _saleOverviews.ToList();

		// Apply payment method filter
		if (!string.IsNullOrEmpty(_selectedPaymentFilter) && _selectedPaymentFilter != "All")
			filtered = [.. filtered.Where(s => GetPrimaryPaymentMethod(s) == _selectedPaymentFilter)];

		if (_selectedSupplier is not null && _user.LocationId == 1)
		{
			filtered = [.. filtered.Where(s => s.PartyId == _selectedSupplier.Id)];

			var allSaleReturns = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(
				_startDate.ToDateTime(TimeOnly.MinValue),
				_endDate.ToDateTime(TimeOnly.MaxValue),
				1);

			var saleReturns = allSaleReturns
				.Where(sr => sr.PartyId == _selectedSupplier.Id).ToList();

			// Apply payment method filter
			if (!string.IsNullOrEmpty(_selectedPaymentFilter) && _selectedPaymentFilter != "All")
				saleReturns = [.. saleReturns.Where(s => GetPrimaryPaymentMethod(s) == _selectedPaymentFilter)];

			// Sort Sale Returns by Date and Bill No
			saleReturns = [.. saleReturns.OrderBy(sr => sr.SaleReturnDateTime).ThenBy(sr => sr.BillNo)];

			foreach (var saleReturn in saleReturns)
				filtered.Add(new()
				{
					SaleId = -saleReturn.SaleReturnId, // Negative ID to differentiate
					BillNo = saleReturn.BillNo,
					UserId = saleReturn.UserId,
					UserName = saleReturn.UserName,
					SaleDateTime = saleReturn.SaleReturnDateTime,
					LocationId = saleReturn.LocationId,
					LocationName = saleReturn.LocationName,
					PartyId = saleReturn.PartyId,
					PartyName = saleReturn.PartyName,
					OrderId = null,
					OrderNo = null,
					CustomerId = saleReturn.CustomerId,
					CustomerName = saleReturn.CustomerName,
					CustomerNumber = saleReturn.CustomerNumber,
					TotalProducts = -saleReturn.TotalProducts,
					TotalQuantity = -saleReturn.TotalQuantity,
					BaseTotal = -saleReturn.BaseTotal,
					ProductDiscountAmount = -saleReturn.ProductDiscountAmount,
					SubTotal = -saleReturn.SubTotal,
					CGSTPercent = saleReturn.CGSTPercent,
					CGSTAmount = -saleReturn.CGSTAmount,
					SGSTPercent = saleReturn.SGSTPercent,
					SGSTAmount = -saleReturn.SGSTAmount,
					IGSTPercent = saleReturn.IGSTPercent,
					IGSTAmount = -saleReturn.IGSTAmount,
					TotalTaxAmount = -saleReturn.TotalTaxAmount,
					AfterTax = -saleReturn.AfterTax,
					BillDiscountPercent = saleReturn.BillDiscountPercent,
					BillDiscountAmount = -saleReturn.BillDiscountAmount,
					BillDiscountReason = saleReturn.BillDiscountReason,
					AfterBillDiscount = -saleReturn.AfterBillDiscount,
					RoundOff = -saleReturn.RoundOff,
					Total = -saleReturn.Total,
					Cash = -saleReturn.Cash,
					Card = -saleReturn.Card,
					UPI = -saleReturn.UPI,
					Credit = -saleReturn.Credit,
					CreatedAt = saleReturn.CreatedAt,
					Remarks = saleReturn.Remarks
				});
		}

		_filteredSaleOverviews = [.. filtered];
	}
	#endregion

	#region Change Events
	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		if (args.StartDate != default && args.EndDate != default)
		{
			_startDate = args.StartDate;
			_endDate = args.EndDate;
			await LoadData();
		}
	}

	private async Task OnLocationFilterChanged(ChangeEventArgs<int?, LocationModel> args)
	{
		if (args.Value is null)
		{
			_selectedLocationId = 0;
			return;
		}
		else
			_selectedLocationId = args.Value;

		_selectedLocation = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);

		await LoadData();
	}

	private async Task OnSupplierChanged(ChangeEventArgs<LedgerModel?, LedgerModel> args)
	{
		_selectedSupplier = args.Value;

		if (args.ItemData is not null && args.ItemData.Id > 0)
			_selectedSupplier = args.ItemData;
		else
			_selectedSupplier = null;

		await LoadData();
	}

	private async Task OnPaymentFilterChanged(ChangeEventArgs<string, string> args)
	{
		_selectedPaymentFilter = args.Value ?? "All";
		await ApplyFilters();
	}
	#endregion

	#region Export
	private async Task ExportToExcel()
	{
		var locationName = _selectedLocation?.Name ?? "All Locations";
		var excelData = SaleExcelExport.ExportSaleOverviewExcel(_filteredSaleOverviews, _startDate, _endDate);
		var fileName = $"Sales_Details_{locationName}_{_startDate:yyyyMMdd}_to_{_endDate:yyyyMMdd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelData);
		VibrationService.VibrateWithTime(100);
	}

	private async Task ExportToPdf()
	{
		if (_filteredSaleOverviews == null || !_filteredSaleOverviews.Any())
			return;

		var pdfData = await SaleReportPDFExport.GenerateA4SaleReport(_filteredSaleOverviews, _startDate, _endDate, _selectedSupplier);
		var fileName = $"Sales_Register_{_selectedSupplier?.Name ?? "All"}_{_startDate:yyyyMMdd}_to_{_endDate:yyyyMMdd}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", pdfData);
		VibrationService.VibrateWithTime(100);
	}
	#endregion

	#region Charts
	private string GetPrimaryPaymentMethod(SaleOverviewModel sale)
	{
		var payments = new Dictionary<string, decimal>
	{
		{ "Cash", sale.Cash },
		{ "Card", sale.Card },
		{ "UPI", sale.UPI },
		{ "Credit", sale.Credit }
	};

		KeyValuePair<string, decimal>? primaryPayment = null;

		if (sale.SaleId < 0)
			primaryPayment = payments.Where(p => p.Value < 0).OrderByDescending(p => p.Value).FirstOrDefault();
		else
			primaryPayment = payments.Where(p => p.Value > 0).OrderByDescending(p => p.Value).FirstOrDefault();

		if (primaryPayment == null || string.IsNullOrEmpty(primaryPayment.Value.Key))
			return "Cash"; // Default

		// Check if it's mixed payment (more than one method used)
		int usedMethods = sale.SaleId < 0
			? payments.Count(p => p.Value < 0)
			: payments.Count(p => p.Value > 0);

		return usedMethods > 1 ? "Mixed" : primaryPayment.Value.Key;
	}

	private string GetPrimaryPaymentMethod(SaleReturnOverviewModel saleReturn)
	{
		var payments = new Dictionary<string, decimal>
		{
			{ "Cash", -saleReturn.Cash },
			{ "Card", -saleReturn.Card },
			{ "UPI", -saleReturn.UPI },
			{ "Credit", -saleReturn.Credit }
		};

		var primaryPayment = payments.Where(p => p.Value > 0).OrderByDescending(p => p.Value).FirstOrDefault();

		if (primaryPayment.Key == null)
			return "Cash"; // Default

		// Check if it's mixed payment (more than one method used)
		var usedMethods = payments.Count(p => p.Value > 0);
		return usedMethods > 1 ? "Mixed" : primaryPayment.Key;
	}

	private string GetPaymentBadgeClass(string paymentMethod)
	{
		return paymentMethod.ToLower() switch
		{
			"cash" => "payment-cash",
			"card" => "payment-card",
			"upi" => "payment-upi",
			"credit" => "payment-credit",
			"mixed" => "payment-mixed",
			_ => "payment-cash"
		};
	}

	private string GetPaymentIcon(string paymentMethod)
	{
		return paymentMethod.ToLower() switch
		{
			"cash" => "fas fa-money-bill",
			"card" => "fas fa-credit-card",
			"upi" => "fas fa-mobile-alt",
			"credit" => "fas fa-handshake",
			"mixed" => "fas fa-layer-group",
			_ => "fas fa-money-bill"
		};
	}

	private void ViewSaleDetails(SaleOverviewModel sale)
	{
		if (sale.SaleId < 0)
		{
			// Navigate to Sale Return details
			NavigationManager.NavigateTo($"/Reports/SaleReturn/View/{-sale.SaleId}");
			return;
		}

		// Navigate to Sale details
		NavigationManager.NavigateTo($"/Reports/Sale/View/{sale.SaleId}");
	}

	// Chart data methods
	private List<DailySalesChartData> GetSalesTrendsData()
	{
		if (_filteredSaleOverviews == null || _filteredSaleOverviews.Count == 0)
			return [];

		return [.. _filteredSaleOverviews
			.GroupBy(s => s.SaleDateTime.Date)
			.Select(g => new DailySalesChartData
			{
				Date = g.Key.ToString("yyyy-MM-dd"),
				Amount = g.Sum(s => s.Total)
			})
			.OrderBy(d => d.Date)];
	}

	private List<PaymentMethodChartData> GetPaymentMethodData()
	{
		if (_filteredSaleOverviews == null || _filteredSaleOverviews.Count == 0)
			return [];

		var paymentData = new Dictionary<string, decimal>
		{
			{ "Cash", _filteredSaleOverviews.Sum(s => s.Cash) },
			{ "Card", _filteredSaleOverviews.Sum(s => s.Card) },
			{ "UPI", _filteredSaleOverviews.Sum(s => s.UPI) },
			{ "Credit", _filteredSaleOverviews.Sum(s => s.Credit) }
		};

		var result = paymentData
			.Where(p => p.Value > 0)
			.Select(p => new PaymentMethodChartData
			{
				PaymentMethod = p.Key,
				Amount = p.Value
			})
			.ToList();

		// Ensure we always have at least one data point for the chart
		if (result.Count == 0)
			result.Add(new PaymentMethodChartData { PaymentMethod = "No Data", Amount = 1 });

		return result;
	}

	private List<LocationSalesSummaryChartData> GetLocationPerformanceData()
	{
		// Only show for admin users
		if (_user?.LocationId != 1)
			return [];

		// If no data or admin is viewing a specific location, don't show location comparison
		if (_filteredSaleOverviews == null || _filteredSaleOverviews.Count == 0)
			return [];

		// Only show location performance when admin is viewing "All Locations"
		if (_selectedLocationId.HasValue && _selectedLocationId.Value > 0)
			return [];

		var locationData = _filteredSaleOverviews
			.Where(s => !string.IsNullOrEmpty(s.LocationName)) // Ensure location name exists
			.GroupBy(s => new { s.LocationId, s.LocationName })
			.Select(g => new LocationSalesSummaryChartData
			{
				LocationName = g.Key.LocationName,
				Amount = g.Sum(s => s.Total)
			})
			.OrderByDescending(l => l.Amount)
			.ToList();

		return locationData;
	}

	private List<HourlySalesData> GetHourlySalesData()
	{
		if (_filteredSaleOverviews == null || _filteredSaleOverviews.Count == 0)
			return [];

		return [.. _filteredSaleOverviews
			.GroupBy(s => s.SaleDateTime.Hour)
			.Select(g => new HourlySalesData
			{
				Hour = g.Key.ToString("D2"),
				Count = g.Count()
			})
			.OrderBy(h => h.Hour)];
	}

	// Summary methods
	private string GetDateRangeText()
	{
		var days = (_endDate.ToDateTime(TimeOnly.MinValue) - _startDate.ToDateTime(TimeOnly.MinValue)).Days;
		return days == 0 ? "Today" : $"Last {days + 1} days";
	}

	private string GetAverageRevenueText()
	{
		if (_filteredSaleOverviews.Count == 0) return "No data";
		var avgPerSale = _filteredSaleOverviews.Average(s => s.Total);
		return $"₹{avgPerSale:N0} avg/sale";
	}

	private decimal GetAverageOrderValue()
	{
		return _filteredSaleOverviews.Count > 0 ? _filteredSaleOverviews.Average(s => s.Total) : 0;
	}
	#endregion
}

// Helper data models for charts
public class HourlySalesData
{
	public string Hour { get; set; }
	public int Count { get; set; }
}
