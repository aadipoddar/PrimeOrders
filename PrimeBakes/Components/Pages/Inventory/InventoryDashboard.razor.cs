using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Models.Common;

namespace PrimeBakes.Components.Pages.Inventory;

public partial class InventoryDashboard
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		if (_user?.Inventory != true)
		{
			NavManager.NavigateTo("/");
			return;
		}

		_isLoading = false;
	}
}