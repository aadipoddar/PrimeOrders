using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

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

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30)); // Default to last 30 days
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];
	private List<ProductModel> _filteredProducts = [];
	private List<ProductOverviewModel> _productOverviews = [];
	private List<ProductOverviewModel> _filteredProductOverviews = [];

	private SfGrid<ProductOverviewModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;

		if (!await ValidatePassword())
		{
			NavManager.NavigateTo("/Login");
			return;
		}

		await LoadInitialData();

		// Check if ProductId parameter was provided
		if (ProductId.HasValue && ProductId.Value > 0)
		{
			_selectedProductId = ProductId.Value;
		}

		await ApplyFilters();
		_isLoading = false;
	}

	private async Task LoadInitialData()
	{
		// Load all required data
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory, true);
		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product, true);
		_filteredProducts = _products.ToList(); // Initially show all products

		// Set location based on user permissions
		if (_user.LocationId != 1) // Admin has LocationId = 1
		{
			_selectedLocationId = _user.LocationId;
		}

		// Load product overviews
		await LoadProductOverviews();
	}

	private async Task LoadProductOverviews()
	{
		int locationId = _user?.LocationId == 1 ? _selectedLocationId : _user.LocationId;

		_productOverviews = await ProductData.LoadProductDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			locationId);

		await ApplyFilters();
	}

	private async Task ApplyFilters()
	{
		// Filter product dropdown based on selected category
		if (_selectedCategoryId > 0)
		{
			_filteredProducts = _products.Where(p => p.ProductCategoryId == _selectedCategoryId).ToList();
		}
		else
		{
			_filteredProducts = _products.ToList();
		}

		// Filter product overviews based on selected filters
		_filteredProductOverviews = _productOverviews;

		if (_selectedCategoryId > 0)
		{
			_filteredProductOverviews = _filteredProductOverviews
				.Where(p => p.ProductCategoryId == _selectedCategoryId)
				.ToList();
		}

		if (_selectedProductId > 0)
		{
			_filteredProductOverviews = _filteredProductOverviews
				.Where(p => p.ProductId == _selectedProductId)
				.ToList();

			// Load selected product details
			_selectedProduct = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, _selectedProductId);
		}

		StateHasChanged();
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

	private async Task Logout()
	{
		await JS.InvokeVoidAsync("deleteCookie", "UserId");
		await JS.InvokeVoidAsync("deleteCookie", "Passcode");
		NavManager.NavigateTo("/Login");
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
		_selectedProductId = 0; // Reset product selection when category changes
		await ApplyFilters();
	}

	private async Task OnProductChanged(ChangeEventArgs<int, ProductModel> args)
	{
		_selectedProductId = args.Value;
		await ApplyFilters();
	}

	private async Task ExportToPdf()
	{
		if (_sfGrid != null)
		{
			await _sfGrid.ExportToPdfAsync();
		}
	}

	private async Task ExportToExcel()
	{
		if (_sfGrid != null)
		{
			await _sfGrid.ExportToExcelAsync();
		}
	}

	#region Chart Data Methods

	private List<DailySalesData> GetDailySalesData()
	{
		var result = _filteredProductOverviews
			.GroupBy(s => s.BillDateTime.Date)
			.Select(group => new DailySalesData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.TotalAmount)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
	}

	private List<SalesQuantityData> GetDailySalesQuantityData()
	{
		var result = _filteredProductOverviews
			.GroupBy(s => s.BillDateTime.Date)
			.Select(group => new SalesQuantityData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.TotalAmount),
				Quantity = group.Sum(s => s.QuantitySold)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
	}

	private List<LocationSalesData> GetLocationSalesData()
	{
		if (_locations == null || !_locations.Any() || _filteredProductOverviews == null || !_filteredProductOverviews.Any())
			return new List<LocationSalesData>();

		return _filteredProductOverviews
			.GroupBy(s => s.LocationId)
			.Select(group => new LocationSalesData
			{
				LocationId = group.Key,
				LocationName = _locations.FirstOrDefault(l => l.Id == group.Key)?.Name ?? "Unknown",
				Amount = group.Sum(s => s.TotalAmount)
			})
			.OrderByDescending(l => l.Amount)
			.Take(10)
			.ToList();
	}

	private List<TaxComponentData> GetTaxDistributionData()
	{
		if (_filteredProductOverviews == null || !_filteredProductOverviews.Any())
			return new List<TaxComponentData>();

		decimal totalTax = _filteredProductOverviews.Sum(p => p.TotalTaxAmount);
		if (totalTax <= 0)
			return new List<TaxComponentData>();

		decimal sgst = _filteredProductOverviews.Sum(p => p.SGSTAmount);
		decimal cgst = _filteredProductOverviews.Sum(p => p.CGSTAmount);
		decimal igst = _filteredProductOverviews.Sum(p => p.IGSTAmount);

		return new List<TaxComponentData>
		{
			new() { Component = "SGST", Amount = sgst },
			new() { Component = "CGST", Amount = cgst },
			new() { Component = "IGST", Amount = igst }
		}.Where(t => t.Amount > 0).ToList();
	}

	#endregion

	#region Chart Data Classes

	public class DailySalesData
	{
		public string Date { get; set; }
		public decimal Amount { get; set; }
	}

	public class SalesQuantityData
	{
		public string Date { get; set; }
		public decimal Amount { get; set; }
		public decimal Quantity { get; set; }
	}

	public class LocationSalesData
	{
		public int LocationId { get; set; }
		public string LocationName { get; set; }
		public decimal Amount { get; set; }
	}

	public class TaxComponentData
	{
		public string Component { get; set; }
		public decimal Amount { get; set; }
	}

	#endregion
}