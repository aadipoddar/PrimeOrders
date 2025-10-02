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

	public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<RawMaterialStockSummaryModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockSummaryByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<List<RawMaterialStockDetailsModel>> LoadRawMaterialStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<RawMaterialStockDetailsModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockSummaryModel, dynamic>(StoredProcedureNames.LoadProductStockSummaryByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<List<ProductStockDetailsModel>> LoadProductStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockDetailsModel, dynamic>(StoredProcedureNames.LoadProductStockDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task SaveRawMaterialStockAdjustment(List<StockAdjustmentRawMaterialCartModel> cart)
	{
		// Use the summary model to get closing stock values for adjustment calculation
		var stockSummary = await LoadRawMaterialStockSummaryByDateLocationId(
			DateTime.Now.AddDays(-1),
			DateTime.Now.AddDays(1),
			1);

		foreach (var item in cart)
		{
			decimal adjustmentQuantity = 0;
			var existingStock = stockSummary.FirstOrDefault(s => s.RawMaterialId == item.RawMaterialId);

			if (existingStock is null)
				adjustmentQuantity = item.Quantity;
			else
				adjustmentQuantity = item.Quantity - existingStock.ClosingStock;

			if (adjustmentQuantity != 0)
				await InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = item.RawMaterialId,
					Quantity = adjustmentQuantity,
					NetRate = null,
					Type = StockType.Adjustment.ToString(),
					TransactionNo = $"RMADJ{DateTime.Now:yyyyMMddHHmmss}",
					TransactionDate = DateOnly.FromDateTime(DateTime.Now),
					LocationId = 1
				});
		}
	}
}