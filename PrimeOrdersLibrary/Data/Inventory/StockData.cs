using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory;

public static class StockData
{
	public static async Task InsertStock(StockModel stock) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertStock, stock);

	public static async Task<List<StockDetailModel>> LoadStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<StockDetailModel, dynamic>(StoredProcedureNames.LoadStockDetailsByDateLocationId, new { FromDate, ToDate, LocationId });
}