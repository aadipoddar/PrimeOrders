using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class AdminDashboard
{
    private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        _isLoading = false;
		StateHasChanged();
	}
}