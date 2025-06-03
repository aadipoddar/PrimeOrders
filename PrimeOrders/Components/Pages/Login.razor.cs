using Syncfusion.Blazor.Inputs;

namespace PrimeOrders.Components.Pages;

public partial class Login
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private string _passcode = "";

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
			Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var passcode = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(passcode))
		{
			var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
			if (user is null) return;

			if (BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), passcode))
				NavManager.NavigateTo("/Dashboard");
		}
	}

	private async Task CheckPasscode(OtpInputEventArgs e)
	{
		_passcode = e.Value?.ToString() ?? string.Empty;
		if (_passcode.Length != 4) return;

		var user = await UserData.LoadUserByPasscode(_passcode);
		if (user is null || !user.Status) return;

		await JS.InvokeVoidAsync("setCookie", "UserId", user.Id, 1);
		await JS.InvokeVoidAsync("setCookie", "Passcode", BCrypt.Net.BCrypt.EnhancedHashPassword(user.Passcode.ToString(), 13), 1);

		NavManager.NavigateTo("/Dashboard");
	}
}