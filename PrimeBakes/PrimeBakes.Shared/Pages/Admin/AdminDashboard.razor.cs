using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class AdminDashboard
{
	private UserModel _user;

	// Dashboard Statistics Properties
	public int TotalOutlets { get; set; } = 12;
	public int ActiveUsers { get; set; } = 48;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin);
		_user = authResult.User;

		var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		var users = await CommonData.LoadTableData<UserModel>(TableNames.User);

		TotalOutlets = locations?.Count ?? 0;
		ActiveUsers = users?.Count(u => u.Status) ?? 0;

		StateHasChanged();
	}
}