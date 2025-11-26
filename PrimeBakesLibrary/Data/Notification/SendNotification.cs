using System.Text.Json;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

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

        var order = await CommonData.LoadTableDataById<OrderOverviewModel>(ViewNames.OrderOverview, orderId);
        var title = $"New Order Placed by {order.LocationName}";
        var text = $"Order No: {order.TransactionNo} | Total Items: {order.TotalItems} | Total Qty: {order.TotalQuantity} | Location: {order.LocationName} | User: {order.CreatedByName} | Date: {order.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task SendSaleNotificationPartyAdmin(int saleId)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);

        if (sale.PartyId is null || sale.PartyId <= 0)
            return;

        var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);

        if (party.LocationId is null || party.LocationId <= 0)
            return;

        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == party.LocationId)];

        var title = $"New Sale Created for {party.Name}";
        var text = $"Sale No: {sale.TransactionNo} | Party: {party.Name} | Total Items: {sale.TotalItems} | Total Qty: {sale.TotalQuantity} | Total Amount: {sale.TotalAmount.FormatIndianCurrency()} | User: {sale.CreatedByName} | Date: {sale.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {sale.Remarks} | Order No: {sale.TransactionNo}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task SendKitchenIssueNotificationMainLocationAdminInventory(int kitchenIssueId)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueOverviewModel>(ViewNames.KitchenIssueOverview, kitchenIssueId);
        var title = $"New Kitchen Issue Placed to {kitchenIssue.KitchenName}";
        var text = $"Kitchen Issue No: {kitchenIssue.TransactionNo} | Total Items: {kitchenIssue.TotalItems} | Total Qty: {kitchenIssue.TotalQuantity} | Kitchen: {kitchenIssue.KitchenName} | User: {kitchenIssue.CreatedByName} | Date: {kitchenIssue.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {kitchenIssue.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }
}
