using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Stock;

public static class ProductStockData
{
	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockSummaryModel, dynamic>(StoredProcedureNames.LoadProductStockSummaryByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<List<ProductStockDetailsModel>> LoadProductStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockDetailsModel, dynamic>(StoredProcedureNames.LoadProductStockDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task InsertProductStock(ProductStockModel stock) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertProductStock, stock);

	public static async Task DeleteProductStockByTransactionNo(string TransactionNo) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteProductStockByTransactionNo, new { TransactionNo });

	public static async Task SaveProductStockAdjustment(List<ProductStockAdjustmentCartModel> cart, int locationId)
	{
		// Use the summary model to get closing stock values for adjustment calculation
		var stockSummary = await LoadProductStockSummaryByDateLocationId(
			DateTime.Now.AddDays(-1),
			DateTime.Now.AddDays(1),
			locationId);

		foreach (var item in cart)
		{
			decimal adjustmentQuantity = 0;
			var existingStock = stockSummary.FirstOrDefault(s => s.ProductId == item.ProductId);

			if (existingStock is null)
				adjustmentQuantity = item.Quantity;
			else
				adjustmentQuantity = item.Quantity - existingStock.ClosingStock;

			if (adjustmentQuantity != 0)
				await InsertProductStock(new()
				{
					Id = 0,
					ProductId = item.ProductId,
					Quantity = adjustmentQuantity,
					NetRate = null,
					Type = StockType.Adjustment.ToString(),
					TransactionNo = $"FPADJ{DateTime.Now:yyyyMMddHHmmss}",
					TransactionDate = DateOnly.FromDateTime(DateTime.Now),
					LocationId = locationId
				});
		}
	}
}