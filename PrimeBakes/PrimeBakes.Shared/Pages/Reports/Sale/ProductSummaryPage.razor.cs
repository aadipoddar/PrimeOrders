using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class ProductSummaryPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _showCharts = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
	private int _selectedCategoryId = 0;

	private List<ProductOverviewModel> _productOverviews = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;
		await LoadInitialData();
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadInitialData()
	{
		try
		{
			_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory, true);
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);

			// Add "All Categories" option
			_productCategories.Insert(0, new ProductCategoryModel { Id = 0, Name = "All Categories" });
		}
		catch (Exception ex)
		{
			// Handle error
			Console.WriteLine($"Error loading initial data: {ex.Message}");
		}
	}

	private async Task LoadData()
	{
		try
		{
			var fromDate = _startDate.ToDateTime(new TimeOnly(0, 0));
			var toDate = _endDate.ToDateTime(new TimeOnly(23, 59));

			if (_user.LocationId == 1)
			{
				// Admin users can see all locations data
				_productOverviews = await ProductData.LoadProductDetailsByDateLocationId(fromDate, toDate, 0);
			}
			else
			{
				// Non-admin users can only see their own location data
				_productOverviews = await ProductData.LoadProductDetailsByDateLocationId(fromDate, toDate, _user.LocationId);
			}

			// Filter by category if selected
			if (_selectedCategoryId > 0)
			{
				_productOverviews = [.. _productOverviews.Where(p => p.ProductCategoryId == _selectedCategoryId)];
			}

			StateHasChanged();
		}
		catch (Exception ex)
		{
			// Handle error
			Console.WriteLine($"Error loading product data: {ex.Message}");
		}
	}

	#region Event Handlers
	private async Task OnDateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		if (args.StartDate != default && args.EndDate != default)
		{
			_startDate = args.StartDate;
			_endDate = args.EndDate;
			await LoadData();
		}
	}

	private async Task OnCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedCategoryId = args.Value;
		await LoadData();
	}

	private void ToggleCharts()
	{
		_showCharts = !_showCharts;
		StateHasChanged();
	}

	private async Task ExportToExcel()
	{
		try
		{
			var selectedProduct = _productOverviews.FirstOrDefault()?.ProductId > 0 ?
				new ProductModel { Id = _productOverviews.First().ProductId, Name = _productOverviews.First().ProductName } : null;

			var excelData = ProductExcelExport.ExportProductDetailExcel(
				_productOverviews,
				_startDate,
				_endDate,
				selectedProduct,
				_selectedCategoryId,
				_productCategories
			);

			var fileName = $"ProductSummary_{_startDate:yyyyMMdd}_{_endDate:yyyyMMdd}.xlsx";
			await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelData);
		}
		catch (Exception ex)
		{
			// Handle error
			Console.WriteLine($"Error exporting to Excel: {ex.Message}");
		}
	}
	#endregion

	#region Summary Calculations
	private decimal GetTotalProductSales()
	{
		return _productOverviews.Sum(p => p.TotalAmount);
	}

	private decimal GetTotalQuantity()
	{
		return _productOverviews.Sum(p => p.QuantitySold);
	}

	private int GetUniqueProducts()
	{
		return _productOverviews.Select(p => p.ProductId).Distinct().Count();
	}

	private decimal GetAverageProductValue()
	{
		if (!_productOverviews.Any()) return 0;
		var validProducts = _productOverviews.Where(p => p.QuantitySold > 0);
		if (!validProducts.Any()) return 0;
		return validProducts.Average(p => p.TotalAmount / p.QuantitySold);
	}

	private decimal GetTotalDiscount()
	{
		return _productOverviews.Sum(p => p.DiscountAmount);
	}

	private decimal GetTotalTax()
	{
		return _productOverviews.Sum(p => p.TotalTaxAmount);
	}

	private decimal GetDiscountPercentage()
	{
		var totalSales = GetTotalProductSales();
		return totalSales > 0 ? (GetTotalDiscount() / totalSales) * 100 : 0;
	}

	private decimal GetTaxPercentage()
	{
		var totalSales = GetTotalProductSales();
		return totalSales > 0 ? (GetTotalTax() / totalSales) * 100 : 0;
	}

	private decimal GetCategoryPercentage(decimal categorySales)
	{
		var totalSales = GetTotalProductSales();
		return totalSales > 0 ? (categorySales / totalSales) * 100 : 0;
	}
	#endregion

	#region Category Summary
	private List<CategorySummaryModel> GetCategorySummary()
	{
		return [.. _productOverviews
			.Where(p => p.ProductCategoryId > 0)
			.GroupBy(p => new { p.ProductCategoryId, p.ProductCategoryName })
			.Select(g => new CategorySummaryModel
			{
				CategoryId = g.Key.ProductCategoryId,
				CategoryName = g.Key.ProductCategoryName,
				ProductCount = g.Select(p => p.ProductId).Distinct().Count(),
				TotalSales = g.Sum(p => p.TotalAmount),
				TotalQuantity = g.Sum(p => p.QuantitySold),
				AveragePrice = g.Average(p => p.AveragePrice)
			})
			.OrderByDescending(c => c.TotalSales)];
	}

	public class CategorySummaryModel
	{
		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = string.Empty;
		public int ProductCount { get; set; }
		public decimal TotalSales { get; set; }
		public decimal TotalQuantity { get; set; }
		public decimal AveragePrice { get; set; }
	}
	#endregion

	#region Top Products
	private List<TopProductModel> GetTopProducts()
	{
		return [.. _productOverviews
			.GroupBy(p => new { p.ProductId, p.ProductName, p.ProductCode })
			.Select(g => new TopProductModel
			{
				ProductId = g.Key.ProductId,
				ProductName = g.Key.ProductName,
				ProductCode = g.Key.ProductCode,
				TotalSales = g.Sum(p => p.TotalAmount),
				TotalQuantity = g.Sum(p => p.QuantitySold),
				AveragePrice = g.Average(p => p.AveragePrice)
			})
			.OrderByDescending(p => p.TotalSales)];
	}

	public class TopProductModel
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; } = string.Empty;
		public string ProductCode { get; set; } = string.Empty;
		public decimal TotalSales { get; set; }
		public decimal TotalQuantity { get; set; }
		public decimal AveragePrice { get; set; }
	}
	#endregion

	#region Chart Data Methods
	private List<object> GetTopProductsChartData()
	{
		return [.. GetTopProducts()
			.Take(8)
			.Select(p => new
			{
				ProductName = p.ProductName.Length > 15 ? p.ProductName.Substring(0, 15) + "..." : p.ProductName,
				Sales = p.TotalSales
			})
			.Cast<object>()];
	}

	private List<object> GetCategorySalesChartData()
	{
		return [.. GetCategorySummary()
			.Take(8)
			.Select(c => new
			{
				CategoryName = c.CategoryName.Length > 12 ? c.CategoryName.Substring(0, 12) + "..." : c.CategoryName,
				Sales = c.TotalSales
			})
			.Cast<object>()];
	}

	private List<object> GetMonthlySalesChartData()
	{
		return [.. _productOverviews
			.GroupBy(p => new { Month = p.BillDateTime.ToString("MMM yyyy") })
			.Select(g => new
			{
				Month = g.Key.Month,
				Sales = g.Sum(p => p.TotalAmount)
			})
			.OrderBy(d => DateTime.ParseExact(d.Month, "MMM yyyy", null))
			.Cast<object>()];
	}

	private List<object> GetQuantityRevenueChartData()
	{
		return [.. GetTopProducts()
			.Take(10)
			.Select(p => new
			{
				ProductName = p.ProductName,
				Quantity = p.TotalQuantity,
				Revenue = p.TotalSales
			})
			.Cast<object>()];
	}
	#endregion
}