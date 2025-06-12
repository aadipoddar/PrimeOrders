using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory;

public static class KitchenProductionData
{
	public static async Task<int> InsertKitchenProduction(KitchenProductionModel kitchenPoduction) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProduction, kitchenPoduction)).FirstOrDefault();

	public static async Task InsertKitchenProductionDetail(KitchenProductionDetailModel kitchenProductionDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertKitchenProductionDetail, kitchenProductionDetail);

	public static async Task<KitchenProductionModel> LoadLastKitchenProductionByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<KitchenProductionModel, dynamic>(StoredProcedureNames.LoadLastKitchenProductionByLocation, new { LocationId })).FirstOrDefault();
}
