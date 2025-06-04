using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory;

public static class SupplierData
{
	public static async Task InsertSupplier(SupplierModel supplier) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertSupplier, supplier);
}
