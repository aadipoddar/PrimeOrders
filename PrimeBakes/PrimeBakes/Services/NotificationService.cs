using Plugin.LocalNotification;

namespace PrimeBakes.Services;

public class NotificationService(INotificationRegistrationService notificationRegistrationService) : Shared.Services.INotificationService
{
	readonly INotificationRegistrationService _notificationRegistrationService = notificationRegistrationService;

	public async Task RegisterDevicePushNotification(string tag)
	{
		await Permissions.RequestAsync<Permissions.PostNotifications>();
		await _notificationRegistrationService.RegisterDeviceAsync(tag);
	}

	public async Task DeregisterDevicePushNotification() =>
		await _notificationRegistrationService.DeregisterDeviceAsync();

	public async Task ShowLocalNotification(int id, string title, string subTitle, string description)
	{
		if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
			await LocalNotificationCenter.Current.RequestNotificationPermission();

		var request = new NotificationRequest
		{
			NotificationId = id,
			Title = title,
			Subtitle = subTitle,
			Description = description,
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = DateTime.Now.AddSeconds(5)
			}
		};

		await LocalNotificationCenter.Current.Show(request);
	}
}
