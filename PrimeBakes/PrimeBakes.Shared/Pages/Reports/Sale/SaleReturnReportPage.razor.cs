using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class SaleReturnReportPage
{
	private UserModel _user;
	private bool _isLoading = true;

	// Data collections
	private List<SaleReturnOverviewModel> _saleReturnOverviews = [];
	private List<SaleReturnOverviewModel> _filteredSaleReturnOverviews = [];

	// Filter properties
	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private string _selectedPaymentFilter = "All";

	// Grid reference
	private SfGrid<SaleReturnOverviewModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_isLoading = true;
		StateHasChanged();

		await LoadSalesReturnData();
		ApplyFilters();

		_isLoading = false;
		StateHasChanged();
	}
	private async Task LoadSalesReturnData()
	{
		var fromDate = _startDate.ToDateTime(TimeOnly.MinValue);
		var toDate = _endDate.ToDateTime(TimeOnly.MaxValue);

		_saleReturnOverviews = await SaleReturnData.LoadSaleReturnDetailsByDateLocationId(fromDate, toDate, _user.LocationId);
		_saleReturnOverviews = [.. _saleReturnOverviews.OrderByDescending(s => s.SaleReturnDateTime)];
	}

	private void ApplyFilters()
	{
		var filtered = _saleReturnOverviews.AsEnumerable();

		// Apply payment method filter
		if (!string.IsNullOrEmpty(_selectedPaymentFilter) && _selectedPaymentFilter != "All")
			filtered = filtered.Where(s => GetPrimaryPaymentMethod(s) == _selectedPaymentFilter);

		_filteredSaleReturnOverviews = [.. filtered];
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		if (args.StartDate != default && args.EndDate != default)
		{
			_startDate = args.StartDate;
			_endDate = args.EndDate;
			await LoadData();
		}
	}

	private async Task OnPaymentFilterChanged(ChangeEventArgs<string, string> args)
	{
		_selectedPaymentFilter = args.Value ?? "All";
		ApplyFilters();
		await InvokeAsync(StateHasChanged);
	}

	private string GetPrimaryPaymentMethod(SaleReturnOverviewModel saleReturn)
	{
		var payments = new Dictionary<string, decimal>
		{
			{ "Cash", saleReturn.Cash },
			{ "Card", saleReturn.Card },
			{ "UPI", saleReturn.UPI },
			{ "Credit", saleReturn.Credit }
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

	private void ViewSaleReturnDetails(SaleReturnOverviewModel sale) =>
		NavigationManager.NavigateTo($"/Reports/SaleReturn/View/{sale.SaleReturnId}");

	// Chart data methods
	private List<DailySalesChartData> GetSalesTrendsData()
	{
		if (_filteredSaleReturnOverviews == null || _filteredSaleReturnOverviews.Count == 0)
			return [];

		return [.. _filteredSaleReturnOverviews
			.GroupBy(s => s.SaleReturnDateTime.Date)
			.Select(g => new DailySalesChartData
			{
				Date = g.Key.ToString("yyyy-MM-dd"),
				Amount = g.Sum(s => s.Total)
			})
			.OrderBy(d => d.Date)];
	}

	private List<PaymentMethodChartData> GetPaymentMethodData()
	{
		if (_filteredSaleReturnOverviews == null || _filteredSaleReturnOverviews.Count == 0)
			return [];

		var paymentData = new Dictionary<string, decimal>
		{
			{ "Cash", _filteredSaleReturnOverviews.Sum(s => s.Cash) },
			{ "Card", _filteredSaleReturnOverviews.Sum(s => s.Card) },
			{ "UPI", _filteredSaleReturnOverviews.Sum(s => s.UPI) },
			{ "Credit", _filteredSaleReturnOverviews.Sum(s => s.Credit) }
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
		if (_filteredSaleReturnOverviews == null || _filteredSaleReturnOverviews.Count == 0)
			return [];

		var locationData = _filteredSaleReturnOverviews
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
		if (_filteredSaleReturnOverviews == null || _filteredSaleReturnOverviews.Count == 0)
			return [];

		return [.. _filteredSaleReturnOverviews
			.GroupBy(s => s.SaleReturnDateTime.Hour)
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
		if (_filteredSaleReturnOverviews.Count == 0) return "No data";
		var avgPerSale = _filteredSaleReturnOverviews.Average(s => s.Total);
		return $"₹{avgPerSale:N0} avg/sale";
	}

	private decimal GetAverageOrderValue()
	{
		return _filteredSaleReturnOverviews.Count > 0 ? _filteredSaleReturnOverviews.Average(s => s.Total) : 0;
	}

	// Export methods
	private async Task ExportToExcel()
	{
		var excelData = SaleReturnExcelExport.ExportSaleReturnOverviewExcel(_filteredSaleReturnOverviews, _startDate, _endDate);
		var fileName = $"Sale_Return_Details_{_startDate:yyyyMMdd}_to_{_endDate:yyyyMMdd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelData);
		VibrationService.VibrateWithTime(100);
	}
}