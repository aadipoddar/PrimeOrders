using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Models.Common;

namespace PrimeBakes.Components.Pages;

public partial class Dashboard
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;

	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);
	}

	private void Logout()
	{
		SecureStorage.Default.RemoveAll();
		NavManager.NavigateTo("/Login", true);
	}
}