using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Reports;

public partial class ReportsDashboard : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	private bool _isLoading = true;
    private UserModel _user;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);

		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.L, Logout, "Logout", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateToDashboard, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Dashboard", Exclude.None);

		_isLoading = false;
        StateHasChanged();
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task Logout()=>
		await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
}