using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
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
    private LocationModel _selectedLocation;

    // Filter properties
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
    private int? _selectedLocationId;
    private string _selectedPaymentFilter = "All";

    // Grid reference
    private SfGrid<SaleOverviewModel> _sfGrid;

    protected override async Task OnInitializedAsync()
    {
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
        _user = authResult.User;

        // Handle location parameter from URL
        if (SelectedLocationId.HasValue && _user.LocationId == 1)
            _selectedLocationId = SelectedLocationId.Value;
        else if (_user.LocationId != 1)
            _selectedLocationId = _user.LocationId;

        await LoadData();
        _isLoading = false;
    }

    private async Task LoadData()
    {
        _isLoading = true;
        StateHasChanged();

        await LoadLocations();
        await LoadSalesData();
        ApplyFilters();

        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadLocations()
    {
        if (_user.LocationId == 1)
        {
            _locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);

            // Add "All Locations" option for location 1 users
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

    private async Task LoadSalesData()
    {
        var fromDate = _startDate.ToDateTime(TimeOnly.MinValue);
        var toDate = _endDate.ToDateTime(TimeOnly.MaxValue);

        if (_user.LocationId == 1 && (_selectedLocationId == null || _selectedLocationId == 0))
        {
            // Load all sales for admin when "All Locations" is selected
            _saleOverviews = [];
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

        // Sort by date descending
        _saleOverviews = [.. _saleOverviews.OrderByDescending(s => s.SaleDateTime)];
    }

    private void ApplyFilters()
    {
        var filtered = _saleOverviews.AsEnumerable();

        // Apply payment method filter
        if (!string.IsNullOrEmpty(_selectedPaymentFilter) && _selectedPaymentFilter != "All")
            filtered = filtered.Where(s => GetPrimaryPaymentMethod(s) == _selectedPaymentFilter);

        _filteredSaleOverviews = [.. filtered];
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

    private async Task OnLocationFilterChanged(ChangeEventArgs<int?, LocationModel> args)
    {
        if (args.Value is null)
            _selectedLocationId = 0;
        else
            _selectedLocationId = args.Value;

        _selectedLocation = _locations.FirstOrDefault(l => l.Id == _selectedLocationId);

        // Update URL if admin user
        if (_user.LocationId == 1 && _selectedLocationId.HasValue && _selectedLocationId.Value > 0)
            NavigationManager.NavigateTo($"/Reports/Sale/Detailed/{_selectedLocationId.Value}");
        else if (_user.LocationId == 1 && (_selectedLocationId == null || _selectedLocationId == 0))
            NavigationManager.NavigateTo("/Reports/Sale/Detailed");

        await LoadData();
    }

    private async Task OnPaymentFilterChanged(ChangeEventArgs<string, string> args)
    {
        _selectedPaymentFilter = args.Value ?? "All";
        ApplyFilters();
        await InvokeAsync(StateHasChanged);
    }

    private string GetPrimaryPaymentMethod(SaleOverviewModel sale)
    {
        var payments = new Dictionary<string, decimal>
        {
            { "Cash", sale.Cash },
            { "Card", sale.Card },
            { "UPI", sale.UPI },
            { "Credit", sale.Credit }
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

    // Export methods
    private async Task ExportToExcel()
    {
        var locationName = _selectedLocation?.Name ?? "All Locations";
        var excelData = SaleExcelExport.ExportSaleOverviewExcel(_filteredSaleOverviews, _startDate, _endDate);
        var fileName = $"Sales_Details_{locationName}_{_startDate:yyyyMMdd}_to_{_endDate:yyyyMMdd}.xlsx";
        await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelData);
        VibrationService.VibrateWithTime(100);
    }
}

// Helper data models for charts
public class HourlySalesData
{
    public string Hour { get; set; }
    public int Count { get; set; }
}
