using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class SaleSummaryPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showCharts = true; // Charts hidden by default

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private int? _selectedLocationId = 0;
	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _saleOverviews = [];

	#region Initialization
	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadLocations();
		await LoadSaleData();
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
		{
			_selectedLocationId = _user.LocationId;
		}
	}

	private async Task LoadSaleData()
	{
		var locationId = _selectedLocationId ?? 0;
		_saleOverviews = await SaleData.LoadSaleDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			locationId);

		StateHasChanged();
	}
	#endregion

	#region Event Handlers
	private async Task OnDateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadSaleData();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int?, LocationModel> args)
	{
		_selectedLocationId = args.Value ?? 0;
		await LoadSaleData();
	}

	private void ToggleCharts()
	{
		_showCharts = !_showCharts;
		StateHasChanged();
	}

	private async Task ExportToExcel()
	{
		if (_isProcessing || _saleOverviews.Count == 0)
			return;

		_isProcessing = true;
		StateHasChanged();

		var memoryStream = SaleExcelExport.ExportSaleOverviewExcel(_saleOverviews, _startDate, _endDate);
		var fileName = $"Sale_Summary_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);

		_isProcessing = false;
		StateHasChanged();
	}
	#endregion

	#region Summary Calculations
	private decimal GetTotalSales()
	{
		return _saleOverviews.Sum(s => s.Total);
	}

	private decimal GetTotalQuantity()
	{
		return _saleOverviews.Sum(s => s.TotalQuantity);
	}

	private int GetTotalProducts()
	{
		return _saleOverviews.Sum(s => s.TotalProducts);
	}

	private decimal GetAverageSale()
	{
		if (_saleOverviews.Count == 0)
			return 0;

		return _saleOverviews.Average(s => s.Total);
	}

	private decimal GetTotalTax()
	{
		return _saleOverviews.Sum(s => s.TotalTaxAmount);
	}

	private decimal GetLocationPercentage(decimal locationSales)
	{
		var totalSales = GetTotalSales();
		if (totalSales == 0)
			return 0;

		return (locationSales / totalSales) * 100;
	}
	#endregion

	#region Location Summary
	private List<LocationSummaryModel> GetLocationSummary()
	{
		return [.. _saleOverviews
			.GroupBy(s => s.LocationName)
			.Select(g => new LocationSummaryModel
			{
				LocationName = g.Key ?? "Unknown",
				SalesCount = g.Count(),
				TotalSales = g.Sum(s => s.Total),
				TotalQuantity = g.Sum(s => s.TotalQuantity),
				AverageSale = g.Average(s => s.Total)
			})
			.OrderByDescending(l => l.TotalSales)];
	}

	public class LocationSummaryModel
	{
		public string LocationName { get; set; } = string.Empty;
		public int SalesCount { get; set; }
		public decimal TotalSales { get; set; }
		public decimal TotalQuantity { get; set; }
		public decimal AverageSale { get; set; }
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetDailySalesData()
	{
		if (_saleOverviews.Count == 0)
			return [];

		return [.. _saleOverviews
			.GroupBy(s => s.SaleDateTime.Date)
			.Select(g => new
			{
				Date = g.Key,
				Amount = g.Sum(s => s.Total)
			})
			.OrderBy(x => x.Date)
			.Cast<object>()];
	}

	private List<object> GetPaymentMethodsData()
	{
		if (_saleOverviews.Count == 0)
			return [];

		var paymentData = new List<object>();

		var cashTotal = _saleOverviews.Sum(s => s.Cash);
		var cardTotal = _saleOverviews.Sum(s => s.Card);
		var upiTotal = _saleOverviews.Sum(s => s.UPI);
		var creditTotal = _saleOverviews.Sum(s => s.Credit);

		if (cashTotal > 0)
			paymentData.Add(new { Method = "Cash", Amount = cashTotal });

		if (cardTotal > 0)
			paymentData.Add(new { Method = "Card", Amount = cardTotal });

		if (upiTotal > 0)
			paymentData.Add(new { Method = "UPI", Amount = upiTotal });

		if (creditTotal > 0)
			paymentData.Add(new { Method = "Credit", Amount = creditTotal });

		return paymentData;
	}

	private List<object> GetLocationPerformanceData()
	{
		if (_saleOverviews.Count == 0)
			return [];

		return [.. _saleOverviews
			.GroupBy(s => s.LocationName)
			.Select(g => new
			{
				Location = g.Key ?? "Unknown",
				Sales = g.Sum(s => s.Total)
			})
			.OrderByDescending(x => x.Sales)
			.Cast<object>()];
	}
	#endregion

	#region Tax Breakdown Methods
	private List<TaxBreakdownModel> GetTaxBreakdownByOutlet()
	{
		if (_saleOverviews.Count == 0)
			return [];

		return [.. _saleOverviews
			.GroupBy(s => s.LocationName)
			.Select(g => new TaxBreakdownModel
			{
				LocationName = g.Key ?? "Unknown",
				CGST = g.Sum(s => s.CGSTAmount),
				SGST = g.Sum(s => s.SGSTAmount),
				IGST = g.Sum(s => s.IGSTAmount),
				TotalTax = g.Sum(s => s.TotalTaxAmount)
			})
			.OrderByDescending(t => t.TotalTax)];
	}

	private decimal GetTaxPercentage(decimal taxAmount)
	{
		var totalTax = GetTotalTax();
		if (totalTax == 0)
			return 0;

		return (taxAmount / totalTax) * 100;
	}

	public class TaxBreakdownModel
	{
		public string LocationName { get; set; } = string.Empty;
		public decimal CGST { get; set; }
		public decimal SGST { get; set; }
		public decimal IGST { get; set; }
		public decimal TotalTax { get; set; }
	}
	#endregion

	#region Payment Breakdown Methods
	private PaymentBreakdownModel GetPaymentBreakdownByOutlet(string locationName)
	{
		var locationSales = _saleOverviews.Where(s => s.LocationName == locationName);

		return new PaymentBreakdownModel
		{
			LocationName = locationName,
			Cash = locationSales.Sum(s => s.Cash),
			Card = locationSales.Sum(s => s.Card),
			UPI = locationSales.Sum(s => s.UPI),
			Credit = locationSales.Sum(s => s.Credit)
		};
	}

	private decimal GetPaymentPercentage(decimal paymentAmount, decimal totalSales)
	{
		if (totalSales == 0)
			return 0;

		return (paymentAmount / totalSales) * 100;
	}

	public class PaymentBreakdownModel
	{
		public string LocationName { get; set; } = string.Empty;
		public decimal Cash { get; set; }
		public decimal Card { get; set; }
		public decimal UPI { get; set; }
		public decimal Credit { get; set; }
	}
	#endregion

	#region Detailed Outlet Performance
	private List<DetailedOutletPerformanceModel> GetDetailedOutletPerformance()
	{
		if (_saleOverviews.Count == 0)
			return [];

		return [.. _saleOverviews
			.GroupBy(s => s.LocationName)
			.Select(g =>
			{
				var totalSales = g.Sum(s => s.Total);
				var transactionCount = g.Count();
				var averageTransaction = transactionCount > 0 ? totalSales / transactionCount : 0;

				// Calculate performance rating based on average transaction and total sales
				var performanceRating = "Average";
				if (averageTransaction > 500 && totalSales > 10000)
					performanceRating = "Excellent";
				else if (averageTransaction > 300 && totalSales > 5000)
					performanceRating = "Good";
				else if (totalSales < 1000)
					performanceRating = "Poor";

				// Get peak hour (simplified - could be enhanced with actual hour analysis)
				var peakHour = g.GroupBy(s => s.SaleDateTime.Hour)
					.OrderByDescending(h => h.Count())
					.FirstOrDefault()?.Key.ToString("00") + ":00" ?? "N/A";

				return new DetailedOutletPerformanceModel
				{
					LocationName = g.Key ?? "Unknown",
					TotalSales = totalSales,
					TransactionCount = transactionCount,
					AverageTransaction = averageTransaction,
					TotalItems = (int)g.Sum(s => s.TotalQuantity),
					TotalTax = g.Sum(s => s.TotalTaxAmount),
					PerformanceRating = performanceRating,
					PeakHour = peakHour
				};
			})
			.OrderByDescending(o => o.TotalSales)];
	}

	public class DetailedOutletPerformanceModel
	{
		public string LocationName { get; set; } = string.Empty;
		public decimal TotalSales { get; set; }
		public int TransactionCount { get; set; }
		public decimal AverageTransaction { get; set; }
		public int TotalItems { get; set; }
		public decimal TotalTax { get; set; }
		public string PerformanceRating { get; set; } = string.Empty;
		public string PeakHour { get; set; } = string.Empty;
	}
	#endregion
}