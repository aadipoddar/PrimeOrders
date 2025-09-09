using Microsoft.AspNetCore.Components;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;

namespace PrimeBakes.Services;

internal static class AuthService
{
	private const string _currentUserIdKey = "user_id";
	private const string _currentPasscodeKey = "passcode";

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
	internal static async Task<UserModel?> AuthenticateCurrentUser(NavigationManager navigationManager)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
	{
		var userId = await SecureStorage.Default.GetAsync(_currentUserIdKey);
		var passcode = await SecureStorage.Default.GetAsync(_currentPasscodeKey);

		if (userId is null || userId is "0" || passcode is null)
		{
			SecureStorage.Default.RemoveAll();
			navigationManager.NavigateTo("/Login", true);
			return null;
		}

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
		if (user is null || user.Passcode != int.Parse(passcode))
		{
			SecureStorage.Default.RemoveAll();
			navigationManager.NavigateTo("/Login", true);
		}

		return user;
	}

	internal static async Task SaveCurrentUser(UserModel user)
	{
		await SecureStorage.Default.SetAsync(_currentUserIdKey, user.Id.ToString());
		await SecureStorage.Default.SetAsync(_currentPasscodeKey, user.Passcode.ToString());
	}
}
