using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Reports;

public partial class ReportsDashboard
{
	private bool _isLoading = true;
	private UserModel _user;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
		_user = authResult.User;
		_isLoading = false;
	}
}