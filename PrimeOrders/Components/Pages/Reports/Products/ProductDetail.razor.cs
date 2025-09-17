using PrimeOrdersLibrary.Exporting.Product;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports.Products;

public partial class ProductDetail
{
	[Parameter] public int? ProductId { get; set; }

	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedLocationId = 0;
	private int _selectedCategoryId = 0;
	private int _selectedProductId = 0;
	private ProductModel _selectedProduct;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];
	private List<ProductModel> _filteredProducts = [];
	private List<ProductOverviewModel> _productOverviews = [];
	private List<ProductOverviewModel> _filteredProductOverviews = [];

	private SfGrid<ProductOverviewModel> _sfGrid;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		if (ProductId.HasValue && ProductId.Value > 0)
			_selectedProductId = ProductId.Value;

		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);
		_selectedLocationId = _locations.FirstOrDefault()?.Id ?? 0;

		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory, true);
		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product, true);
		_filteredProducts = [.. _products];

		if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		await LoadProductOverviews();
	}

	private async Task LoadProductOverviews()
	{
		int locationId = _user.LocationId == 1 ? _selectedLocationId : _user.LocationId;

		_productOverviews = await ProductData.LoadProductDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			locationId);

		await ApplyFilters();
	}

	private async Task ApplyFilters()
	{
		if (_selectedCategoryId > 0)
			_filteredProducts = [.. _products.Where(p => p.ProductCategoryId == _selectedCategoryId)];
		else
			_filteredProducts = [.. _products];

		_filteredProductOverviews = _productOverviews;

		if (_selectedCategoryId > 0)
			_filteredProductOverviews = [.. _filteredProductOverviews.Where(p => p.ProductCategoryId == _selectedCategoryId)];

		if (_selectedProductId > 0)
		{
			_filteredProductOverviews = [.. _filteredProductOverviews.Where(p => p.ProductId == _selectedProductId)];
			_selectedProduct = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, _selectedProductId);
		}

		StateHasChanged();
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadProductOverviews();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadProductOverviews();
	}

	private async Task OnCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedCategoryId = args.Value;
		_selectedProductId = 0;
		await ApplyFilters();
	}

	private async Task OnProductChanged(ChangeEventArgs<int, ProductModel> args)
	{
		_selectedProductId = args.Value;
		await ApplyFilters();
	}
	#endregion

	#region Excel Export
	private async Task ExportToExcel()
	{
		if (_filteredProductOverviews is null || _filteredProductOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Use the new ProductExcelExport class for generating Excel
		var memoryStream = ProductExcelExport.ExportProductDetailExcel(
			_filteredProductOverviews,
			_startDate,
			_endDate,
			_selectedProduct,
			_selectedCategoryId,
			_productCategories);

		// Generate filename based on selected product/category
		string filenameSuffix = string.Empty;
		if (_selectedProduct is not null)
			filenameSuffix = $"_{_selectedProduct.Name}";
		else if (_selectedCategoryId > 0)
		{
			var category = _productCategories.FirstOrDefault(c => c.Id == _selectedCategoryId);
			if (category is not null)
				filenameSuffix = $"_{category.Name}";
		}

		var fileName = $"Product_Detail{filenameSuffix}_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion

	#region Chart Data Methods
	private List<DailyProductSalesChartData> GetDailySalesData()
	{
		var result = _filteredProductOverviews
			.GroupBy(s => s.BillDateTime.Date)
			.Select(group => new DailyProductSalesChartData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.TotalAmount)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
	}

	private List<SalesQuantityChartData> GetDailySalesQuantityData()
	{
		var result = _filteredProductOverviews
			.GroupBy(s => s.BillDateTime.Date)
			.Select(group => new SalesQuantityChartData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.TotalAmount),
				Quantity = group.Sum(s => s.QuantitySold)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
	}

	private List<LocationSalesChartData> GetLocationSalesData()
	{
		if (_locations is null || _locations.Count == 0 || _filteredProductOverviews is null || _filteredProductOverviews.Count == 0)
			return [];

		return [.. _filteredProductOverviews
			.GroupBy(s => s.LocationId)
			.Select(group => new LocationSalesChartData
			{
				LocationId = group.Key,
				LocationName = _locations.FirstOrDefault(l => l.Id == group.Key)?.Name ?? "Unknown",
				Amount = group.Sum(s => s.TotalAmount)
			})
			.OrderByDescending(l => l.Amount)
			.Take(10)];
	}

	private List<TaxComponentChartData> GetTaxDistributionData()
	{
		if (_filteredProductOverviews is null || _filteredProductOverviews.Count == 0)
			return [];

		decimal totalTax = _filteredProductOverviews.Sum(p => p.TotalTaxAmount);
		if (totalTax <= 0)
			return [];

		decimal sgst = _filteredProductOverviews.Sum(p => p.SGSTAmount);
		decimal cgst = _filteredProductOverviews.Sum(p => p.CGSTAmount);
		decimal igst = _filteredProductOverviews.Sum(p => p.IGSTAmount);

		return [.. new List<TaxComponentChartData>
		{
			new() { Component = "SGST", Amount = sgst },
			new() { Component = "CGST", Amount = cgst },
			new() { Component = "IGST", Amount = igst }
		}.Where(t => t.Amount > 0)];
	}
	#endregion
}