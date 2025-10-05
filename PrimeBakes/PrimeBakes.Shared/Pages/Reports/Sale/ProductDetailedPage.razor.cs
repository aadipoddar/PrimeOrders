using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sale;

public partial class ProductDetailedPage
{
    private UserModel _user;
    private bool _isLoading = true;

    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

    private int? _selectedLocationId;
    private int _selectedCategoryId = 0;
    private int _selectedProductId = 0;

    private LocationModel _selectedLocation;
    private ProductOverviewModel _selectedProductDetail;

    private List<LocationModel> _locations = [];
    private List<ProductCategoryModel> _productCategories = [];
    private List<ProductModel> _products = [];
    private List<ProductModel> _filteredProducts = [];
    private List<ProductOverviewModel> _productOverviews = [];
    private List<ProductOverviewModel> _filteredProductOverviews = [];
    private bool _showCharts = true;

    private SfGrid<ProductOverviewModel> _sfGrid;

    protected override async Task OnInitializedAsync()
    {
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
        _user = authResult.User;

        await LoadData();
        _isLoading = false;
    }

    private async Task LoadData()
    {
        _isLoading = true;
        StateHasChanged();

        await LoadLocations();
        await LoadCategories();
        await LoadProducts();
        await LoadProductData();
        ApplyFilters();

        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadLocations()
    {
        if (_user.LocationId == 1)
        {
            _locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);

            // Add "All Locations" option for admin users
            _locations.Insert(0, new LocationModel { Id = 0, Name = "All Locations" });

            _selectedLocationId = 0;
            _selectedLocation = _locations.FirstOrDefault(l => l.Id == 0);
        }
        else
        {
            // Non-admin users can only see their own location
            var userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);
            if (userLocation != null)
            {
                _locations = [userLocation];
                _selectedLocationId = _user.LocationId;
                _selectedLocation = userLocation;
            }
        }
    }

    private async Task LoadCategories()
    {
        try
        {
            _productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory, true);
            _productCategories.Insert(0, new ProductCategoryModel { Id = 0, Name = "All Categories" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading categories: {ex.Message}");
        }
    }

    private async Task LoadProducts()
    {
        try
        {
            _products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product, true);
            _products.Insert(0, new ProductModel { Id = 0, Name = "All Products" });
            _filteredProducts = [.. _products];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading products: {ex.Message}");
        }
    }

    private async Task LoadProductData()
    {
        try
        {
            var fromDate = _startDate.ToDateTime(TimeOnly.MinValue);
            var toDate = _endDate.ToDateTime(TimeOnly.MaxValue);

            if (_user.LocationId == 1 && (_selectedLocationId == null || _selectedLocationId == 0))
            {
                // Load all product data for admin when "All Locations" is selected
                _productOverviews = [];
                foreach (var location in _locations.Where(l => l.Id > 0))
                {
                    var locationProducts = await ProductData.LoadProductDetailsByDateLocationId(fromDate, toDate, location.Id);
                    _productOverviews.AddRange(locationProducts);
                }
            }
            else
            {
                // Load product data for specific location
                var locationId = _selectedLocationId ?? _user.LocationId;
                _productOverviews = await ProductData.LoadProductDetailsByDateLocationId(fromDate, toDate, locationId);
            }

            // Sort by date descending
            _productOverviews = [.. _productOverviews.OrderByDescending(p => p.BillDateTime)];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading product data: {ex.Message}");
        }
    }

    private void ApplyFilters()
    {
        _filteredProductOverviews = [.. _productOverviews];

        // Filter by category
        if (_selectedCategoryId > 0)
        {
            _filteredProductOverviews = [.. _filteredProductOverviews.Where(p => p.ProductCategoryId == _selectedCategoryId)];
        }

        // Filter by product
        if (_selectedProductId > 0)
        {
            _filteredProductOverviews = [.. _filteredProductOverviews.Where(p => p.ProductId == _selectedProductId)];
        }
    }

    #region Event Handlers
    private async Task OnDateRangeChanged(RangePickerEventArgs<DateOnly> args)
    {
        if (args.StartDate != default && args.EndDate != default)
        {
            _startDate = args.StartDate;
            _endDate = args.EndDate;
            await LoadProductData();
            ApplyFilters();
            StateHasChanged();
        }
    }

    private async Task OnLocationChanged(ChangeEventArgs<int?, LocationModel> args)
    {
        _selectedLocationId = args.Value;
        _selectedLocation = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);
        await LoadProductData();
        ApplyFilters();
        StateHasChanged();
    }

    private void OnCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
    {
        _selectedCategoryId = args.Value;

        // Filter products based on selected category
        if (_selectedCategoryId > 0)
        {
            _filteredProducts = [.. _products.Where(p => p.Id == 0 || p.ProductCategoryId == _selectedCategoryId)];
        }
        else
        {
            _filteredProducts = [.. _products];
        }

        // Reset product selection if current selection is not in filtered list
        if (_selectedProductId > 0 && !_filteredProducts.Any(p => p.Id == _selectedProductId))
        {
            _selectedProductId = 0;
        }

        ApplyFilters();
        StateHasChanged();
    }

    private void OnProductChanged(ChangeEventArgs<int, ProductModel> args)
    {
        _selectedProductId = args.Value;
        ApplyFilters();
        StateHasChanged();
    }

    private async Task OnProductRowSelected(RowSelectEventArgs<ProductOverviewModel> args)
    {
        _selectedProductDetail = args.Data;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private void CloseProductDetail()
    {
        _selectedProductDetail = null;
        StateHasChanged();
    }
    #endregion

    #region Export
    private async Task ExportToExcel()
    {
        try
        {
            var selectedProduct = _filteredProductOverviews.FirstOrDefault()?.ProductId > 0 ?
                new ProductModel { Id = _filteredProductOverviews.First().ProductId, Name = _filteredProductOverviews.First().ProductName } : null;

            var excelData = ProductExcelExport.ExportProductDetailExcel(
                _filteredProductOverviews,
                _startDate,
                _endDate,
                selectedProduct,
                _selectedCategoryId,
                _productCategories
            );

            var fileName = $"ProductDetailed_{_startDate:yyyyMMdd}_{_endDate:yyyyMMdd}.xlsx";
            await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting to Excel: {ex.Message}");
        }
    }
    #endregion

    #region Chart Toggle
    private void ToggleCharts()
    {
        _showCharts = !_showCharts;
        StateHasChanged();
    }
    #endregion

    #region Summary Calculations
    private decimal GetTotalProductSales()
    {
        return _filteredProductOverviews.Sum(p => p.TotalAmount);
    }

    private decimal GetTotalQuantity()
    {
        return _filteredProductOverviews.Sum(p => p.QuantitySold);
    }

    private int GetUniqueProducts()
    {
        return _filteredProductOverviews.Select(p => p.ProductId).Distinct().Count();
    }

    private decimal GetAverageProductValue()
    {
        if (!_filteredProductOverviews.Any()) return 0;
        var validProducts = _filteredProductOverviews.Where(p => p.QuantitySold > 0);
        if (!validProducts.Any()) return 0;
        return validProducts.Average(p => p.TotalAmount / p.QuantitySold);
    }

    private decimal GetTotalDiscount()
    {
        return _filteredProductOverviews.Sum(p => p.DiscountAmount);
    }

    private decimal GetTotalTax()
    {
        return _filteredProductOverviews.Sum(p => p.TotalTaxAmount);
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
        return [.. _filteredProductOverviews
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
        return [.. _filteredProductOverviews
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
        return [.. _filteredProductOverviews
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