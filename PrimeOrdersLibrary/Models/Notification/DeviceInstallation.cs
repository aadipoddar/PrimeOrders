using System.Text.Json.Serialization;

namespace PrimeOrdersLibrary.Models.Notification;

public class DeviceInstallation
{
	[JsonPropertyName("installationId")]
	public string InstallationId { get; set; }

	[JsonPropertyName("platform")]
	public string Platform { get; set; }

	[JsonPropertyName("pushChannel")]
	public string PushChannel { get; set; }

	[JsonPropertyName("tags")]
	public List<string> Tags { get; set; } = [];
}