using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Notification;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenProductionData
{
	public static async Task<int> InsertKitchenProduction(KitchenProductionModel kitchenProduction) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProduction, kitchenProduction)).FirstOrDefault();

	public static async Task InsertKitchenProductionDetail(KitchenProductionDetailModel kitchenProductionDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertKitchenProductionDetail, kitchenProductionDetail);

	public static async Task<KitchenProductionModel> LoadLastKitchenProductionByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<KitchenProductionModel, dynamic>(StoredProcedureNames.LoadLastKitchenProductionByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<List<KitchenProductionOverviewModel>> LoadKitchenProductionDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<KitchenProductionOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenProductionDetailsByDate, new { FromDate, ToDate });

	public static async Task<List<KitchenProductionDetailModel>> LoadKitchenProductionDetailByKitchenProduction(int KitchenProductionId) =>
		await SqlDataAccess.LoadData<KitchenProductionDetailModel, dynamic>(StoredProcedureNames.LoadKitchenProductionDetailByKitchenProduction, new { KitchenProductionId });

	public static async Task<KitchenProductionOverviewModel> LoadKitchenProductionOverviewByKitchenProductionId(int KitchenProductionId) =>
		(await SqlDataAccess.LoadData<KitchenProductionOverviewModel, dynamic>(StoredProcedureNames.LoadKitchenProductionOverviewByKitchenProductionId, new { KitchenProductionId })).FirstOrDefault();

	public static async Task<KitchenProductionModel> LoadKitchenProductionByTransactionNo(string TransactionNo) =>
		(await SqlDataAccess.LoadData<KitchenProductionModel, dynamic>(StoredProcedureNames.LoadKitchenProductionByTransactionNo, new { TransactionNo })).FirstOrDefault();

	public static async Task<int> SaveKitchenProduction(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> cart)
	{
		bool update = kitchenProduction.Id > 0;

		kitchenProduction.LocationId = 1;
		kitchenProduction.Status = true;
		kitchenProduction.ProductionDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(kitchenProduction.ProductionDate)
			.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)),
			"India Standard Time");
		kitchenProduction.TransactionNo = update ?
			kitchenProduction.TransactionNo :
			await GenerateCodes.GenerateKitchenProductionTransactionNo(kitchenProduction);

		kitchenProduction.Id = await InsertKitchenProduction(kitchenProduction);
		await SaveKitchenProductionDetail(kitchenProduction, cart, update);
		await SaveStock(kitchenProduction, cart, update);
		await SendNotification.SendKitchenProductionNotificationMainLocationAdminInventory(kitchenProduction.Id);

		return kitchenProduction.Id;
	}

	private static async Task SaveKitchenProductionDetail(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> cart, bool update)
	{
		if (update)
		{
			var existingKitchenProductionDetails = await LoadKitchenProductionDetailByKitchenProduction(kitchenProduction.Id);
			foreach (var item in existingKitchenProductionDetails)
			{
				item.Status = false;
				await InsertKitchenProductionDetail(item);
			}
		}

		foreach (var item in cart)
			await InsertKitchenProductionDetail(new()
			{
				Id = 0,
				KitchenProductionId = kitchenProduction.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Rate = item.Rate,
				Total = item.Total,
				Status = true
			});
	}

	private static async Task SaveStock(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> cart, bool update)
	{
		if (update)
			await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.KitchenProduction.ToString(), kitchenProduction.Id, 1);

		if (kitchenProduction.Status)
			foreach (var item in cart)
				await ProductStockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = item.ProductId,
					Quantity = item.Quantity,
					Type = StockType.KitchenProduction.ToString(),
					TransactionNo = kitchenProduction.TransactionNo,
					TransactionDate = DateOnly.FromDateTime(kitchenProduction.ProductionDate),
					LocationId = 1
				});
	}
}
