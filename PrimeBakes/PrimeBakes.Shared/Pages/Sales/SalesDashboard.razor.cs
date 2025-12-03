using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Sales;

public partial class SalesDashboard
{
    private bool _isLoading = true;
    private UserModel _user;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
        _isLoading = false;
        StateHasChanged();
    }
}