using PrimeBakesLibrary.Models.Inventory.Kitchen;

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

	public static async Task<KitchenProductionModel> LoadKitchenProductionByTransactionNo(string TransactionNo) =>
		(await SqlDataAccess.LoadData<KitchenProductionModel, dynamic>(StoredProcedureNames.LoadKitchenProductionByTransactionNo, new { TransactionNo })).FirstOrDefault();
}
