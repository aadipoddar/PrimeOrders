using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory.Purchase;

public static class SupplierData
{
	public static async Task InsertSupplier(SupplierModel supplier) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertSupplier, supplier);

	public static async Task<SupplierModel> LoadSupplierByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<SupplierModel, dynamic>(StoredProcedureNames.LoadSupplierByLocation, new { LocationId })).FirstOrDefault();
}
