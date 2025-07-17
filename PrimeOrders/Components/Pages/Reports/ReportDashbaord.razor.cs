namespace PrimeOrders.Components.Pages.Reports;

public partial class ReportDashbaord
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private LocationModel _userLocation;
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager)).User) is not null))
			return;

		_userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

		_isLoading = false;
		StateHasChanged();
	}
}