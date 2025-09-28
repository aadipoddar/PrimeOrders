using System.Text.Json;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

namespace PrimeBakesLibrary.Data.Notification;

public static class SendNotification
{
	private static async Task SendNotificationToAPI(List<UserModel> users, string title, string text)
	{
		string endpoint = $"https://primebakesnotificationapi.azurewebsites.net/api/notifications/requests";
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("apikey", Secrets.NotificationAPIKey);

		var notificationPayload = new
		{
			Title = title,
			Text = text,
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

	public static async Task SendOrderNotificationMainLocationAdmin(int orderId)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1)];

		var order = await OrderData.LoadOrderOverviewByOrderId(orderId);
		var title = $"New Order Placed by {order.LocationName}";
		var text = $"Order No: {order.OrderNo} | Total Items: {order.TotalProducts} | Total Qty: {order.TotalQuantity} | Location: {order.LocationName} | User: {order.UserName} | Date: {order.OrderDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}";

		await SendNotificationToAPI(users, title, text);
	}

	public static async Task SendSaleNotificationPartyAdmin(int saleId)
	{
		var sale = await SaleData.LoadSaleOverviewBySaleId(saleId);

		if (sale.PartyId is null || sale.PartyId <= 0)
			return;

		var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);

		if (party.LocationId is null || party.LocationId <= 0)
			return;

		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == party.LocationId)];

		var title = $"New Sale Created for {party.Name}";
		var text = $"Sale No: {sale.BillNo} | Party: {party.Name} | Total Items: {sale.TotalProducts} | Total Qty: {sale.TotalQuantity} | Total Amount: {sale.Total.FormatIndianCurrency()} | User: {sale.UserName} | Date: {sale.SaleDateTime:dd/MM/yy hh:mm tt} | Remarks: {sale.Remarks} | Order No: {sale.OrderNo}";

		await SendNotificationToAPI(users, title, text);
	}

	public static async Task SendKitchenIssueNotificationMainLocationAdminInventory(int kitchenIssueId)
	{
		var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
		users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

		var kitchenIssue = await KitchenIssueData.LoadKitchenIssueOverviewByKitchenIssueId(kitchenIssueId);
		var title = $"New Kitchen Issue Placed to {kitchenIssue.KitchenName}";
		var text = $"Kitchen Issue No: {kitchenIssue.TransactionNo} | Total Items: {kitchenIssue.TotalProducts} | Total Qty: {kitchenIssue.TotalQuantity} | Kitchen: {kitchenIssue.KitchenName} | User: {kitchenIssue.UserName} | Date: {kitchenIssue.IssueDate:dd/MM/yy hh:mm tt} | Remarks: {kitchenIssue.Remarks}";

		await SendNotificationToAPI(users, title, text);
	}
}
