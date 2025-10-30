using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Exporting.Purchase;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.Calendars;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseReport
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showCharts = true; // Charts hidden by default

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<PurchaseOverviewModel> _purchaseOverviews = [];

	#region Initialization
	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadPurchaseData();
		StateHasChanged();
	}

	private async Task LoadPurchaseData()
	{
		_purchaseOverviews = await PurchaseData.LoadPurchaseDetailsByDate(
			_startDate.ToDateTime(TimeOnly.MinValue),
			_endDate.ToDateTime(TimeOnly.MaxValue));

		_purchaseOverviews = [.. _purchaseOverviews.OrderBy(_ => _.BillDateTime)];

		StateHasChanged();
	}
	#endregion

	#region Event Handlers
	private async Task OnDateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadPurchaseData();
	}

	private void ToggleCharts()
	{
		_showCharts = !_showCharts;
		StateHasChanged();
	}

	private async Task ExportToExcel()
	{
		if (_isProcessing || _purchaseOverviews.Count == 0)
			return;

		_isProcessing = true;
		StateHasChanged();

		var memoryStream = await PurchaseExcelExport.ExportPurchaseOverviewExcel(_purchaseOverviews, _startDate, _endDate, 0);
		var fileName = $"Purchase_Summary_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);

		_isProcessing = false;
		StateHasChanged();
	}
	#endregion

	#region Summary Calculations
	private decimal GetTotalPurchases()
	{
		return _purchaseOverviews.Sum(p => p.Total);
	}

	private decimal GetTotalQuantity()
	{
		return _purchaseOverviews.Sum(p => p.TotalQuantity);
	}

	private int GetTotalItems()
	{
		return _purchaseOverviews.Sum(p => p.TotalItems);
	}

	private decimal GetAveragePurchase()
	{
		if (_purchaseOverviews.Count == 0)
			return 0;

		return _purchaseOverviews.Average(p => p.Total);
	}

	private decimal GetTotalTax()
	{
		return _purchaseOverviews.Sum(p => p.TotalTaxAmount);
	}

	private decimal GetSupplierPercentage(decimal supplierPurchases)
	{
		var totalPurchases = GetTotalPurchases();
		if (totalPurchases == 0)
			return 0;

		return (supplierPurchases / totalPurchases) * 100;
	}
	#endregion

	#region Supplier Summary
	private List<SupplierSummaryModel> GetSupplierSummary()
	{
		return [.. _purchaseOverviews
			.GroupBy(p => p.SupplierName)
			.Select(g => new SupplierSummaryModel
			{
				SupplierName = g.Key ?? "Unknown",
				PurchaseCount = g.Count(),
				TotalPurchases = g.Sum(p => p.Total),
				TotalQuantity = g.Sum(p => p.TotalQuantity),
				AveragePurchase = g.Average(p => p.Total)
			})
			.OrderByDescending(s => s.TotalPurchases)];
	}

	public class SupplierSummaryModel
	{
		public string SupplierName { get; set; } = string.Empty;
		public int PurchaseCount { get; set; }
		public decimal TotalPurchases { get; set; }
		public decimal TotalQuantity { get; set; }
		public decimal AveragePurchase { get; set; }
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetDailyPurchaseData()
	{
		if (_purchaseOverviews.Count == 0)
			return [];

		return [.. _purchaseOverviews
			.GroupBy(p => p.BillDateTime.Date)
			.Select(g => new
			{
				Date = g.Key,
				Amount = g.Sum(p => p.Total)
			})
			.OrderBy(x => x.Date)
			.Cast<object>()];
	}

	private List<object> GetSupplierDistributionData()
	{
		if (_purchaseOverviews.Count == 0)
			return [];

		return [.. _purchaseOverviews
			.GroupBy(p => p.SupplierName)
			.Select(g => new
			{
				Supplier = g.Key ?? "Unknown",
				Amount = g.Sum(p => p.Total)
			})
			.OrderByDescending(x => x.Amount)
			.Take(10) // Top 10 suppliers
			.Cast<object>()];
	}

	private List<object> GetMonthlyComparisonData()
	{
		if (_purchaseOverviews.Count == 0)
			return [];

		return [.. _purchaseOverviews
			.GroupBy(p => p.BillDateTime.ToString("MMM yyyy"))
			.Select(g => new
			{
				Month = g.Key,
				Amount = g.Sum(p => p.Total)
			})
			.OrderBy(x => x.Month)
			.Cast<object>()];
	}
	#endregion

	#region Tax Breakdown Methods
	private List<TaxBreakdownModel> GetTaxBreakdownBySupplier()
	{
		if (_purchaseOverviews.Count == 0)
			return [];

		return [.. _purchaseOverviews
			.GroupBy(p => p.SupplierName)
			.Select(g => new TaxBreakdownModel
			{
				SupplierName = g.Key ?? "Unknown",
				CGST = g.Sum(p => p.CGSTAmount),
				SGST = g.Sum(p => p.SGSTAmount),
				IGST = g.Sum(p => p.IGSTAmount),
				TotalTax = g.Sum(p => p.TotalTaxAmount)
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
		public string SupplierName { get; set; } = string.Empty;
		public decimal CGST { get; set; }
		public decimal SGST { get; set; }
		public decimal IGST { get; set; }
		public decimal TotalTax { get; set; }
	}
	#endregion

	#region Monthly Purchase Trends
	private List<MonthlyTrendModel> GetMonthlyPurchaseTrends()
	{
		if (_purchaseOverviews.Count == 0)
			return [];

		return [.. _purchaseOverviews
			.GroupBy(p => new { p.BillDateTime.Year, p.BillDateTime.Month })
			.Select(g => new MonthlyTrendModel
			{
				MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
				TotalAmount = g.Sum(p => p.Total),
				TransactionCount = g.Count(),
				AverageAmount = g.Average(p => p.Total),
				TotalItems = g.Sum(p => p.TotalItems),
				UniqueSuppliers = g.Select(p => p.SupplierId).Distinct().Count()
			})
			.OrderBy(m => m.MonthName)];
	}

	private decimal GetMonthlyPercentage(decimal monthlyAmount)
	{
		var totalPurchases = GetTotalPurchases();
		if (totalPurchases == 0)
			return 0;

		return (monthlyAmount / totalPurchases) * 100;
	}

	public class MonthlyTrendModel
	{
		public string MonthName { get; set; } = string.Empty;
		public decimal TotalAmount { get; set; }
		public int TransactionCount { get; set; }
		public decimal AverageAmount { get; set; }
		public int TotalItems { get; set; }
		public int UniqueSuppliers { get; set; }
	}
	#endregion

	#region Detailed Supplier Performance
	private List<DetailedSupplierPerformanceModel> GetDetailedSupplierPerformance()
	{
		if (_purchaseOverviews.Count == 0)
			return [];

		return [.. _purchaseOverviews
			.GroupBy(p => p.SupplierName)
			.Select(g =>
			{
				var totalPurchases = g.Sum(p => p.Total);
				var transactionCount = g.Count();
				var averageTransaction = transactionCount > 0 ? totalPurchases / transactionCount : 0;

				// Calculate performance rating based on average transaction and total purchases
				var performanceRating = "Average";
				if (averageTransaction > 10000 && totalPurchases > 100000)
					performanceRating = "Excellent";
				else if (averageTransaction > 5000 && totalPurchases > 50000)
					performanceRating = "Good";
				else if (totalPurchases < 10000)
					performanceRating = "Poor";

				return new DetailedSupplierPerformanceModel
				{
					SupplierName = g.Key ?? "Unknown",
					TotalPurchases = totalPurchases,
					TransactionCount = transactionCount,
					AverageTransaction = averageTransaction,
					TotalItems = g.Sum(p => p.TotalItems),
					TotalTax = g.Sum(p => p.TotalTaxAmount),
					TotalDiscount = g.Sum(p => p.DiscountAmount + p.CashDiscountAmount),
					PerformanceRating = performanceRating
				};
			})
			.OrderByDescending(s => s.TotalPurchases)];
	}

	public class DetailedSupplierPerformanceModel
	{
		public string SupplierName { get; set; } = string.Empty;
		public decimal TotalPurchases { get; set; }
		public int TransactionCount { get; set; }
		public decimal AverageTransaction { get; set; }
		public int TotalItems { get; set; }
		public decimal TotalTax { get; set; }
		public decimal TotalDiscount { get; set; }
		public string PerformanceRating { get; set; } = string.Empty;
	}
	#endregion

	#region Grid Actions
	private async Task ViewPurchaseDetails(int purchaseId)
	{
		// Navigate to purchase view page

		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", $"/Inventory/Purchase/View/{purchaseId}", "_blank");
		else
			NavigationManager.NavigateTo($"/Inventory/Purchase/View/{purchaseId}");
	}
	#endregion
}