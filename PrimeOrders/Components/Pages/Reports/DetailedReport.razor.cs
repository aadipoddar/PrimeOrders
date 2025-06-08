using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Data;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeOrders.Components.Pages.Reports;

public partial class DetailedReport
{
	[Parameter] public int? LocationId { get; set; }

	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedLocationId = 0;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now);
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _saleOverviews = [];

	private SfGrid<SaleOverviewModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;

		if (!await ValidatePassword())
		{
			NavManager.NavigateTo("/Login");
			return;
		}

		if (LocationId.HasValue && LocationId.Value > 0)
		{
			_selectedLocationId = LocationId.Value;
		}

		await LoadData();
		_isLoading = false;
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
		await LoadData();
	}

	private async void OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadData();
	}

	private async Task LoadData()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);

			if (_selectedLocationId == 0)
				_selectedLocationId = _locations.FirstOrDefault()?.Id ?? 0;
		}

		else
			_selectedLocationId = _user.LocationId;

		_saleOverviews = await SaleData.LoadSaleDetailsByDateLocationId(_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			_selectedLocationId);

		StateHasChanged();
	}

	private async Task ExportToPdf()
	{
		if (_sfGrid is not null)
			await _sfGrid.ExportToPdfAsync();
	}

	private async Task ExportToExcel()
	{
		if (_sfGrid is not null)
			await _sfGrid.ExportToExcelAsync();
	}

	private List<DailySalesData> GetDailySalesData()
	{
		var result = _saleOverviews
			.GroupBy(s => s.SaleDateTime.Date)
			.Select(group => new DailySalesData
			{
				Date = group.Key.ToString("dd/MM"),
				Amount = group.Sum(s => s.Total)
			})
			.OrderBy(d => DateTime.ParseExact(d.Date, "dd/MM", null))
			.ToList();

		return result;
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

	public class PaymentMethodData
	{
		public string PaymentMethod { get; set; }
		public decimal Amount { get; set; }
	}

	public class DailySalesData
	{
		public string Date { get; set; }
		public decimal Amount { get; set; }
	}
}