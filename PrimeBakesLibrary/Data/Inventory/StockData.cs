using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Data.Inventory;

public static class StockData
{
	public static async Task InsertRawMaterialStock(RawMaterialStockModel stock) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertRawMaterialStock, stock);

	public static async Task InsertProductStock(ProductStockModel stock) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertProductStock, stock);

	public static async Task DeleteRawMaterialStockByTransactionNo(string TransactionNo) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteRawMaterialStockByTransactionNo, new { TransactionNo });

	public static async Task DeleteProductStockByTransactionNo(string TransactionNo) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteProductStockByTransactionNo, new { TransactionNo });

	public static async Task<List<RawMaterialStockDetailModel>> LoadRawMaterialStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<RawMaterialStockDetailModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<List<ProductStockDetailModel>> LoadProductStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockDetailModel, dynamic>(StoredProcedureNames.LoadProductStockDetailsByDateLocationId, new { FromDate, ToDate, LocationId });
}