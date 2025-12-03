using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Inventory;

public partial class InventoryDashboard
{
    private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        _isLoading = false;
        StateHasChanged();
	}
}