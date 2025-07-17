using Syncfusion.Blazor.Calendars;

namespace PrimeOrders.Components.Pages.Reports.Sale;

public partial class SummaryReport
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _saleOverviews = [];

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
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);
		_saleOverviews = await SaleData.LoadSaleDetailsByDateLocationId(_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			0);

		StateHasChanged();
	}

	private List<PaymentMethodChartData> GetPaymentMethodsData()
	{
		var paymentData = new List<PaymentMethodChartData>
		{
			new() { PaymentMethod = "Cash", Amount = _saleOverviews.Sum(s => s.Cash) },
			new() { PaymentMethod = "Card", Amount = _saleOverviews.Sum(s => s.Card) },
			new() { PaymentMethod = "UPI", Amount = _saleOverviews.Sum(s => s.UPI) },
			new() { PaymentMethod = "Credit", Amount = _saleOverviews.Sum(s => s.Credit) }
		};

		return [.. paymentData.Where(p => p.Amount > 0)];
	}

	private List<LocationSalesSummaryChartData> GetLocationSalesData() =>
		[.. _locations
			.Select(location => new LocationSalesSummaryChartData
			{
				LocationName = location.Name,
				Amount = _saleOverviews.Where(s => s.LocationId == location.Id).Sum(s => s.Total)
			})
			.Where(data => data.Amount > 0)];

	private List<PaymentMethodChartData> GetLocationPaymentData(int locationId)
	{
		var locationSales = _saleOverviews.Where(s => s.LocationId == locationId).ToList();

		var paymentData = new List<PaymentMethodChartData>
		{
			new() { PaymentMethod = "Cash", Amount = locationSales.Sum(s => s.Cash) },
			new() { PaymentMethod = "Card", Amount = locationSales.Sum(s => s.Card) },
			new() { PaymentMethod = "UPI", Amount = locationSales.Sum(s => s.UPI) },
			new() { PaymentMethod = "Credit", Amount = locationSales.Sum(s => s.Credit) }
		};

		return [.. paymentData.Where(p => p.Amount > 0)];
	}
}