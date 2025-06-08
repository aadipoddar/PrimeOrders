using Syncfusion.Blazor.Calendars;

namespace PrimeOrders.Components.Pages.Reports;

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
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);
		_saleOverviews = await SaleData.LoadSaleDetailsByDateLocationId(_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			0);

		StateHasChanged();
	}

	private async Task Logout()
	{
		await JS.InvokeVoidAsync("deleteCookie", "UserId");
		await JS.InvokeVoidAsync("deleteCookie", "Passcode");
		NavManager.NavigateTo("/Login");
	}

	private List<PaymentMethodData> GetPaymentMethodsData()
	{
		var paymentData = new List<PaymentMethodData>
		{
			new() { PaymentMethod = "Cash", Amount = _saleOverviews.Sum(s => s.Cash) },
			new() { PaymentMethod = "Card", Amount = _saleOverviews.Sum(s => s.Card) },
			new() { PaymentMethod = "UPI", Amount = _saleOverviews.Sum(s => s.UPI) },
			new() { PaymentMethod = "Credit", Amount = _saleOverviews.Sum(s => s.Credit) }
		};

		return [.. paymentData.Where(p => p.Amount > 0)];
	}

	private List<LocationSalesData> GetLocationSalesData()
	{
		return [.. _locations
			.Select(location => new LocationSalesData
			{
				LocationName = location.Name,
				Amount = _saleOverviews.Where(s => s.LocationId == location.Id).Sum(s => s.Total)
			})
			.Where(data => data.Amount > 0)];
	}

	private List<PaymentMethodData> GetLocationPaymentData(int locationId)
	{
		var locationSales = _saleOverviews.Where(s => s.LocationId == locationId).ToList();

		var paymentData = new List<PaymentMethodData>
		{
			new() { PaymentMethod = "Cash", Amount = locationSales.Sum(s => s.Cash) },
			new() { PaymentMethod = "Card", Amount = locationSales.Sum(s => s.Card) },
			new() { PaymentMethod = "UPI", Amount = locationSales.Sum(s => s.UPI) },
			new() { PaymentMethod = "Credit", Amount = locationSales.Sum(s => s.Credit) }
		};

		return [.. paymentData.Where(p => p.Amount > 0)];
	}

	private async Task ExportReport()
	{
		await JS.InvokeVoidAsync("alert", "PDF Export functionality will be implemented here");
	}

	public class PaymentMethodData
	{
		public string PaymentMethod { get; set; }
		public decimal Amount { get; set; }
	}

	public class LocationSalesData
	{
		public string LocationName { get; set; }
		public decimal Amount { get; set; }
	}
}