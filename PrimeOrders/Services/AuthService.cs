namespace PrimeOrders.Services;

public static class AuthService
{
	public static async Task<AuthenticationResult> ValidateUser(IJSRuntime jSRuntime, NavigationManager navigationManager, Enum permission = null, bool? primaryLocationRequirement = null)
	{
		var userId = await jSRuntime.InvokeAsync<string>("getCookie", "UserId");
		var password = await jSRuntime.InvokeAsync<string>("getCookie", "Passcode");

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
		{
			navigationManager.NavigateTo("/Login");
			return new AuthenticationResult(false, null, "No authentication cookies found");
		}

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));

		if (user is null)
		{
			navigationManager.NavigateTo("/Login");
			return new AuthenticationResult(false, null, "User not found");
		}

		if (!BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), password))
		{
			navigationManager.NavigateTo("/Login");
			return new AuthenticationResult(false, null, "Invalid credentials");
		}

		if (!user.Status)
		{
			navigationManager.NavigateTo("/Login");
			return new AuthenticationResult(false, null, "User account is disabled");
		}

		if (permission is not null)
		{
			var hasPermission = permission switch
			{
				UserRoles.Admin => user.Admin,
				UserRoles.Sales => user.Sales,
				UserRoles.Order => user.Order,
				UserRoles.Inventory => user.Inventory,
				UserRoles.Accounts => user.Accounts,
				_ => false
			};

			if (!hasPermission)
			{
				navigationManager.NavigateTo("/");
				return new AuthenticationResult(false, null, $"Insufficient permissions: {permission} access required");
			}
		}

		if (primaryLocationRequirement.HasValue && primaryLocationRequirement.Value)
		{
			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, user.LocationId);
			if (location is null || location.Id != 1)
			{
				navigationManager.NavigateTo("/");
				return new AuthenticationResult(false, null, "User must be associated with a primary location");
			}
		}

		return new AuthenticationResult(true, user);
	}

	public static async Task Logout(IJSRuntime jSRuntime, NavigationManager navigationManager)
	{
		await jSRuntime.InvokeVoidAsync("deleteCookie", "UserId");
		await jSRuntime.InvokeVoidAsync("deleteCookie", "Passcode");

		navigationManager.NavigateTo("/Login");
	}
}
