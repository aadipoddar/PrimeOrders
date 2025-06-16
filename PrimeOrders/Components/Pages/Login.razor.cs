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
		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var passcode = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(passcode))
		{
			IsVerifying = true;
			StateHasChanged();

			var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
			if (user is null)
			{
				IsVerifying = false;
				StateHasChanged();
				return;
			}

			if (BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), passcode))
				NavManager.NavigateTo("/");
			else
			{
				IsVerifying = false;
				StateHasChanged();
			}
		}
	}

	private async Task CheckPasscode(OtpInputEventArgs e)
	{
		_passcode = e.Value?.ToString() ?? string.Empty;
		if (_passcode.Length != 4 || IsVerifying) return;

		// Show loading animation
		IsVerifying = true;
		StateHasChanged();

		// Add a small delay to ensure the UI updates before potentially redirecting
		await Task.Delay(100);

		try
		{
			var user = await UserData.LoadUserByPasscode(int.Parse(_passcode));

			if (user is null || !user.Status)
			{
				IsVerifying = false;
				StateHasChanged();
				return;
			}

			await JS.InvokeVoidAsync("setCookie", "UserId", user.Id, 1);
			await JS.InvokeVoidAsync("setCookie", "Passcode", BCrypt.Net.BCrypt.EnhancedHashPassword(user.Passcode.ToString(), 13), 1);

			NavManager.NavigateTo("/");
		}
		catch
		{
			// Handle any exceptions that might occur during verification
			IsVerifying = false;
			StateHasChanged();
		}
	}
}