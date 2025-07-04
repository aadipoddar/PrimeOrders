namespace PrimeOrders.Components.Pages.Admin;

public partial class AdminDashboard
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Admin)).User) is not null))
			return;

		_isLoading = false;
		StateHasChanged();
	}
}