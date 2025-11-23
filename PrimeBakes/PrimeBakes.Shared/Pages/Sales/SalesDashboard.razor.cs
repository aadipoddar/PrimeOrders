using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Sales;

public partial class SalesDashboard
{
	private UserModel _user;
	private bool _isLoading = true;

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;
	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfErrorToast;
	private SfToast _sfSuccessToast;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
		_user = authResult.User;
		_isLoading = false;
	}

	private async Task NavigateToOrder()
	{
		if (!_user.Order)
			await ShowToast("Access Denied", "You do not have permission to access the Order module. Please contact your administrator.", "error");

		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Order, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.Order, true);
	}

	#region Utilities
	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}
		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}
	#endregion
}