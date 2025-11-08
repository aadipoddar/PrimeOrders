using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Stock;

public static class RawMaterialStockData
{
	public static async Task<int> InsertRawMaterialStock(RawMaterialStockModel stock) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRawMaterialStock, stock)).FirstOrDefault();

	public static async Task DeleteRawMaterialStockByTypeIdNo(string Type, int? TransactionId, string TransactionNo) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteRawMaterialStockByTypeIdNo, new { Type, TransactionId, TransactionNo });

	public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<RawMaterialStockSummaryModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockSummaryByDate, new { FromDate, ToDate });

	public static async Task<List<RawMaterialStockDetailsModel>> LoadRawMaterialStockDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<RawMaterialStockDetailsModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockDetailsByDate, new { FromDate, ToDate });

	public static async Task SaveRawMaterialStockAdjustment(DateTime transactionDateTime, List<RawMaterialStockAdjustmentCartModel> cart)
	{
		var transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(transactionDateTime);

		var stockSummary = await LoadRawMaterialStockSummaryByDate(
			transactionDateTime.Date,
			transactionDateTime.Date);

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
					TransactionId = null,
					Type = StockType.Adjustment.ToString(),
					TransactionNo = transactionNo,
					TransactionDate = DateOnly.FromDateTime(transactionDateTime)
				});
		}
	}
}