using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Reports;

public partial class ReportsDashboard
{
    private bool _isLoading = true;
    private UserModel _user;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
        _isLoading = false;
        StateHasChanged();
	}
}