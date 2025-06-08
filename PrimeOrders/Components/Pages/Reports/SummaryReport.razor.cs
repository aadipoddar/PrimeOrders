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
}