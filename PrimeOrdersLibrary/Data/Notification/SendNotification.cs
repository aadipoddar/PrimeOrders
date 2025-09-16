using System.Text.Json;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Order;

namespace PrimeOrdersLibrary.Data.Notification;

public static class SendNotification
{
	public static async Task SendOrderNotificationMainLocationAdmin(int orderId)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);

		// Change to Main Location Admins instead of LocationId = 1
		users = [.. users.Where(u => u.Admin && u.LocationId == 1)];

		var order = await OrderData.LoadOrderOverviewByOrderId(orderId);

		string endpoint = $"https://primebakesnotificationapi.azurewebsites.net/api/notifications/requests";
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("apikey", Secrets.NotificationAPIKey);

		var notificationPayload = new
		{
			Title = $"New Order Placed by {order.LocationName}",
			Text = $"Order No: {order.OrderNo} | Total Items: {order.TotalProducts} | Total Qty: {order.TotalQuantity} | Location: {order.LocationName} | User: {order.UserName} | Date: {order.OrderDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}",
			Action = "action_a",
			Tags = users.Select(u => u.Id.ToString()).ToArray(),
		};

		string jsonPayload = JsonSerializer.Serialize(notificationPayload, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		});

		var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
		var response = await httpClient.PostAsync(endpoint, content);
	}
}
