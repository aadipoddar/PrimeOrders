using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory;

public static class StockData
{
	public static async Task InsertStock(StockModel stock) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertStock, stock);
}
