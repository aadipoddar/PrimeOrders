using Syncfusion.Blazor.Inputs;

namespace PrimeOrders.Components.Pages;

public partial class Login
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private string _passcode = "";
	private bool IsVerifying { get; set; } = false;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && (await AuthService.ValidateUser(JS, NavManager)).User is not null)
			NavManager.NavigateTo("/");
	}

	private async Task CheckPasscode(OtpInputEventArgs e)
	{
		_passcode = e.Value?.ToString() ?? string.Empty;
		if (_passcode.Length != 4 || IsVerifying)
			return;

		IsVerifying = true;
		StateHasChanged();

		var user = await UserData.LoadUserByPasscode(int.Parse(_passcode));

		if (user is null || !user.Status)
		{
			IsVerifying = false;
			StateHasChanged();
			return;
		}

		await JS.InvokeVoidAsync("setCookie", "UserId", user.Id, 10);
		await JS.InvokeVoidAsync("setCookie", "Passcode", BCrypt.Net.BCrypt.EnhancedHashPassword(user.Passcode.ToString(), 13), 10);

		NavManager.NavigateTo("/");
	}
}