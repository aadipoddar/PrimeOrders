using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Notification;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueData
{
	public static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue)).FirstOrDefault();

	public static async Task InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail);

	public static async Task<KitchenIssueModel> LoadLastKitchenIssueByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<KitchenIssueModel, dynamic>(StoredProcedureNames.LoadLastKitchenIssueByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<List<KitchenIssueOverviewModel>> LoadKitchenIssueDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<KitchenIssueOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenIssueDetailsByDate, new { FromDate, ToDate });

	public static async Task<List<KitchenIssueDetailModel>> LoadKitchenIssueDetailByKitchenIssue(int KitchenIssueId) =>
		await SqlDataAccess.LoadData<KitchenIssueDetailModel, dynamic>(StoredProcedureNames.LoadKitchenIssueDetailByKitchenIssue, new { KitchenIssueId });

	public static async Task<KitchenIssueOverviewModel> LoadKitchenIssueOverviewByKitchenIssueId(int KitchenIssueId) =>
		(await SqlDataAccess.LoadData<KitchenIssueOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenIssueOverviewByKitchenIssueId, new { KitchenIssueId })).FirstOrDefault();

	public static async Task<int> SaveKitchenIssue(KitchenIssueModel kitchenIssue, List<KitchenIssueRawMaterialCartModel> cart)
	{
		bool update = kitchenIssue.Id > 0;

		kitchenIssue.LocationId = 1;
		kitchenIssue.Status = true;
		kitchenIssue.IssueDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(kitchenIssue.IssueDate)
			.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)),
			"India Standard Time");
		kitchenIssue.TransactionNo = update ?
			kitchenIssue.TransactionNo :
			await GenerateCodes.GenerateKitchenIssueTransactionNo(kitchenIssue);

		kitchenIssue.Id = await InsertKitchenIssue(kitchenIssue);
		await SaveKitchenIssueDetail(kitchenIssue, cart, update);
		await SaveStock(kitchenIssue, cart, update);
		await SendNotification.SendKitchenIssueNotificationMainLocationAdminInventory(kitchenIssue.Id);

		return kitchenIssue.Id;
	}

	private static async Task SaveKitchenIssueDetail(KitchenIssueModel kitchenIssue, List<KitchenIssueRawMaterialCartModel> cart, bool update)
	{
		if (update)
		{
			var existingKitchenIssueDetails = await LoadKitchenIssueDetailByKitchenIssue(kitchenIssue.Id);
			foreach (var item in existingKitchenIssueDetails)
			{
				item.Status = false;
				await InsertKitchenIssueDetail(item);
			}
		}

		foreach (var item in cart)
			await InsertKitchenIssueDetail(new()
			{
				Id = 0,
				KitchenIssueId = kitchenIssue.Id,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				Status = true
			});
	}

	private static async Task SaveStock(KitchenIssueModel kitchenIssue, List<KitchenIssueRawMaterialCartModel> cart, bool update)
	{
		if (update)
			await StockData.DeleteRawMaterialStockByTransactionNo(kitchenIssue.TransactionNo);

		if (kitchenIssue.Status)
			foreach (var item in cart)
				await StockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = item.RawMaterialId,
					Quantity = -item.Quantity,
					Type = StockType.KitchenIssue.ToString(),
					TransactionNo = kitchenIssue.TransactionNo,
					TransactionDate = DateOnly.FromDateTime(kitchenIssue.IssueDate),
					LocationId = kitchenIssue.LocationId
				});
	}
}