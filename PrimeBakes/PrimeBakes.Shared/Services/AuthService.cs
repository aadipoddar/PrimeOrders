using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Services;

public static class AuthService
{
	public static async Task<AuthenticationResult> ValidateUser(IDataStorageService dataStorageService, NavigationManager navigationManager, INotificationService notificationService, IVibrationService vibrationService, Enum userRoles = null, bool primaryLocationRequirement = false)
	{
		var userData = await dataStorageService.SecureGetAsync(StorageFileNames.UserDataFileName);
		if (string.IsNullOrEmpty(userData))
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		var user = System.Text.Json.JsonSerializer.Deserialize<UserModel>(userData);
		if (user is null)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		if (!user.Status)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		if (primaryLocationRequirement && user.LocationId != 1)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		if (userRoles is null)
			return new AuthenticationResult(true, user, null);

		var hasPermission = userRoles switch
		{
			UserRoles.Admin => user.Admin,
			UserRoles.Sales => user.Sales,
			UserRoles.Order => user.Order,
			UserRoles.Inventory => user.Inventory,
			UserRoles.Accounts => user.Accounts,
			_ => false
		};

		if (!hasPermission)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		return new AuthenticationResult(true, user, null);
	}

	public static async Task Logout(IDataStorageService dataStorageService, NavigationManager navigationManager, INotificationService notificationService, IVibrationService vibrationService)
	{
		await dataStorageService.SecureRemoveAll();
		await notificationService.DeregisterDevicePushNotification();
		vibrationService.VibrateWithTime(500);
		navigationManager.NavigateTo("/Login", forceLoad: true);
	}
}
