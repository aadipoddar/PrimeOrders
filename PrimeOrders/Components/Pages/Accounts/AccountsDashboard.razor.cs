namespace PrimeOrders.Components.Pages.Accounts;

public partial class AccountsDashboard
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Accounts, true)).User) is not null))
			return;

		_isLoading = false;
		StateHasChanged();
	}
}