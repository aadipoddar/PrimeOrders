using Syncfusion.Blazor.Calendars;

namespace PrimeOrders.Components.Pages.Reports;

public partial class ProductSummary
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)); // Default to last month
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];
	private List<ProductOverviewModel> _productOverviews = [];

	// Summary data holder
	private ProductSummaryData _productSummary = new();

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_isLoading = true;

		if (firstRender && !await ValidatePassword()) NavManager.NavigateTo("/Login");

		_isLoading = false;

		StateHasChanged();

		if (firstRender)
			await LoadData();
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

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;

		await LoadData();
	}

	private async Task LoadData()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory, true);
		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product, true);

		// Use the stored procedure to fetch product overview data
		_productOverviews = await ProductData.LoadProductDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_user?.LocationId == 1 ? 0 : _user.LocationId);

		// Calculate summary data
		CalculateSummaryData();

		StateHasChanged();
	}

	private void CalculateSummaryData()
	{
		_productSummary = new ProductSummaryData
		{
			TotalProducts = _productOverviews.Select(p => p.ProductId).Distinct().Count(),
			TotalAmount = _productOverviews.Sum(p => p.TotalAmount),
			TotalQuantity = _productOverviews.Sum(p => p.QuantitySold),
			TotalDiscount = _productOverviews.Sum(p => p.DiscountAmount),
			TotalTax = _productOverviews.Sum(p => p.TotalTaxAmount)
		};
	}

	private async Task Logout()
	{
		await JS.InvokeVoidAsync("deleteCookie", "UserId");
		await JS.InvokeVoidAsync("deleteCookie", "Passcode");
		NavManager.NavigateTo("/Login");
	}

	private async Task ExportReport()
	{
		await JS.InvokeVoidAsync("alert", "PDF Export functionality will be implemented here");
	}

	// Chart data methods
	private List<TopProductData> GetTopProductsData()
	{
		return _productOverviews
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.Select(g => new TopProductData
			{
				ProductName = g.Key.ProductName,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderByDescending(p => p.Amount)
			.Take(5)
			.ToList();
	}

	private List<CategorySalesData> GetCategorySalesData()
	{
		return _productOverviews
			.GroupBy(p => new { p.ProductCategoryId, p.ProductCategoryName })
			.Select(g => new CategorySalesData
			{
				CategoryId = g.Key.ProductCategoryId,
				CategoryName = g.Key.ProductCategoryName,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderByDescending(p => p.Amount)
			.Take(8) // Top 8 categories
			.ToList();
	}

	private List<MonthlySalesData> GetMonthlySalesData()
	{
		var startDate = _startDate.AddMonths(-12); // Go back 12 months from selected date
		var endDate = _endDate;

		return _productOverviews
			.GroupBy(p => new { Month = p.BillDateTime.ToString("MMM yyyy") })
			.Select(g => new MonthlySalesData
			{
				Month = g.Key.Month,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderBy(d => DateTime.ParseExact(d.Month, "MMM yyyy", null))
			.ToList();
	}

	private List<ProductQuantityRevenueData> GetQuantityRevenueData()
	{
		return _productOverviews
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.Select(g => new ProductQuantityRevenueData
			{
				ProductName = g.Key.ProductName,
				Amount = g.Sum(p => p.TotalAmount),
				Quantity = g.Sum(p => p.QuantitySold)
			})
			.OrderByDescending(p => p.Amount)
			.Take(5)
			.ToList();
	}

	private List<TopProductData> GetTopProductsByCategoryData(int categoryId)
	{
		return _productOverviews
			.Where(p => p.ProductCategoryId == categoryId)
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.Select(g => new TopProductData
			{
				ProductName = g.Key.ProductName,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderByDescending(p => p.Amount)
			.Take(5)
			.ToList();
	}

	// Helper methods for category data
	private decimal GetCategorySalesAmount(int categoryId) =>
		_productOverviews.Where(p => p.ProductCategoryId == categoryId).Sum(p => p.TotalAmount);

	private decimal GetCategoryBaseTotal(int categoryId) =>
		_productOverviews.Where(p => p.ProductCategoryId == categoryId).Sum(p => p.BaseTotal);

	private decimal GetCategoryDiscount(int categoryId) =>
		_productOverviews.Where(p => p.ProductCategoryId == categoryId).Sum(p => p.DiscountAmount);

	private decimal GetCategoryTax(int categoryId) =>
		_productOverviews.Where(p => p.ProductCategoryId == categoryId).Sum(p => p.TotalTaxAmount);

	private int GetCategoryProductCount(int categoryId) =>
		_productOverviews.Where(p => p.ProductCategoryId == categoryId).Select(p => p.ProductId).Distinct().Count();

	private decimal GetCategoryQuantity(int categoryId) =>
		_productOverviews.Where(p => p.ProductCategoryId == categoryId).Sum(p => p.QuantitySold);

	// Data classes to support charts
	public class ProductSummaryData
	{
		public int TotalProducts { get; set; }
		public decimal TotalAmount { get; set; }
		public decimal TotalQuantity { get; set; }
		public decimal TotalDiscount { get; set; }
		public decimal TotalTax { get; set; }
	}

	public class TopProductData
	{
		public string ProductName { get; set; }
		public decimal Amount { get; set; }
	}

	public class CategorySalesData
	{
		public int CategoryId { get; set; }
		public string CategoryName { get; set; }
		public decimal Amount { get; set; }
	}

	public class MonthlySalesData
	{
		public string Month { get; set; }
		public decimal Amount { get; set; }
	}

	public class ProductQuantityRevenueData
	{
		public string ProductName { get; set; }
		public decimal Amount { get; set; }
		public decimal Quantity { get; set; }
	}
}