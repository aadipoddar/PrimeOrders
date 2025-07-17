using Syncfusion.Blazor.Calendars;

namespace PrimeOrders.Components.Pages.Reports.Products;

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

	private ProductSummaryChartData _productSummary = new();

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, primaryLocationRequirement: true)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;

		await LoadData();
	}

	private async Task LoadData()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory, true);
		_productCategories.RemoveAll(c => c.LocationId != 1);

		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product, true);
		_products.RemoveAll(r => r.LocationId != 1);

		// Use the stored procedure to fetch product overview data
		_productOverviews = await ProductData.LoadProductDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_user?.LocationId == 1 ? 0 : _user.LocationId);

		// Calculate summary data
		CalculateSummaryData();

		StateHasChanged();
	}

	private void CalculateSummaryData() =>
		_productSummary = new()
		{
			TotalProducts = _productOverviews.Select(p => p.ProductId).Distinct().Count(),
			TotalAmount = _productOverviews.Sum(p => p.TotalAmount),
			TotalQuantity = _productOverviews.Sum(p => p.QuantitySold),
			TotalDiscount = _productOverviews.Sum(p => p.DiscountAmount),
			TotalTax = _productOverviews.Sum(p => p.TotalTaxAmount)
		};

	#region Chart data methods
	private List<TopProductChartData> GetTopProductsData()
	{
		return [.. _productOverviews
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.Select(g => new TopProductChartData
			{
				ProductName = g.Key.ProductName,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderByDescending(p => p.Amount)
			.Take(5)];
	}

	private List<CategorySalesChartData> GetCategorySalesData() =>
		[.. _productOverviews
			.GroupBy(p => new { p.ProductCategoryId, p.ProductCategoryName })
			.Select(g => new CategorySalesChartData
			{
				CategoryId = g.Key.ProductCategoryId,
				CategoryName = g.Key.ProductCategoryName,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderByDescending(p => p.Amount)
			.Take(8)];

	private List<MonthlySalesChartData> GetMonthlySalesData() =>
		[.. _productOverviews
			.GroupBy(p => new { Month = p.BillDateTime.ToString("MMM yyyy") })
			.Select(g => new MonthlySalesChartData
			{
				Month = g.Key.Month,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderBy(d => DateTime.ParseExact(d.Month, "MMM yyyy", null))];

	private List<ProductQuantityRevenueChartData> GetQuantityRevenueData() =>
		[.. _productOverviews
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.Select(g => new ProductQuantityRevenueChartData
			{
				ProductName = g.Key.ProductName,
				Amount = g.Sum(p => p.TotalAmount),
				Quantity = g.Sum(p => p.QuantitySold)
			})
			.OrderByDescending(p => p.Amount)
			.Take(5)];

	private List<TopProductChartData> GetTopProductsByCategoryData(int categoryId) =>
		[.. _productOverviews
			.Where(p => p.ProductCategoryId == categoryId)
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.Select(g => new TopProductChartData
			{
				ProductName = g.Key.ProductName,
				Amount = g.Sum(p => p.TotalAmount)
			})
			.OrderByDescending(p => p.Amount)
			.Take(5)];
	#endregion
}