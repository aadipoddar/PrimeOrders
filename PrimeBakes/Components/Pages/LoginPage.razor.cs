using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;

using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Components.Pages;

public partial class LoginPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private string _passcode = "";
	private bool _isVerifying = false;

	protected override Task OnInitializedAsync()
	{
		SecureStorage.Default.RemoveAll();
		return base.OnInitializedAsync();
	}

	private async Task CheckPasscode(OtpInputEventArgs e)
	{
		_passcode = e.Value?.ToString() ?? string.Empty;
		if (_passcode.Length != 4 || _isVerifying)
			return;

		_isVerifying = true;
		StateHasChanged();

		var user = await UserData.LoadUserByPasscode(int.Parse(_passcode));

		if (user is null || !user.Status)
		{
			_isVerifying = false;
			StateHasChanged();
			return;
		}

		await AuthService.SaveCurrentUser(user);
		NavManager.NavigateTo("/");
	}
}