
using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class NotificationService : INotificationService
{
	public async Task RegisterDevicePushNotification(string tag) { }

	public async Task DeregisterDevicePushNotification() { }

	public async Task ShowLocalNotification(int id, string title, string subTitle, string description) { }
}
