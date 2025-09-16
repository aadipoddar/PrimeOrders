using PrimeOrdersLibrary.Models.Notification;

namespace PrimeBakes.Services;

public interface IDeviceInstallationService
{
	string Token { get; set; }
	bool NotificationsSupported { get; }
	string GetDeviceId();
	DeviceInstallation GetDeviceInstallation(params string[] tags);
}