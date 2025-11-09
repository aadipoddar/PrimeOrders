using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Stock;

public static class ProductStockData
{
	public static async Task<int> InsertProductStock(ProductStockModel stock) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProductStock, stock)).FirstOrDefault();

	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockSummaryModel, dynamic>(StoredProcedureNames.LoadProductStockSummaryByDateLocationId, new { FromDate = DateOnly.FromDateTime(FromDate), ToDate = DateOnly.FromDateTime(ToDate), LocationId });

	public static async Task<List<ProductStockDetailsModel>> LoadProductStockDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockDetailsModel, dynamic>(StoredProcedureNames.LoadProductStockDetailsByDateLocationId, new { FromDate = DateOnly.FromDateTime(FromDate), ToDate = DateOnly.FromDateTime(ToDate), LocationId });

	public static async Task DeleteProductStockByTypeTransactionId(string Type, int TransactionId) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteProductStockByTypeTransactionId, new { Type, TransactionId });

	public static async Task DeleteProductStockById(int Id)
	{
		var stock = await CommonData.LoadTableDataById<ProductStockModel>(TableNames.ProductStock, Id);
		if (stock is null)
			return;

		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(stock.TransactionDate.ToDateTime(TimeOnly.MinValue));
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new Exception("Cannot delete stock entry as the financial year is locked.");

		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteProductStockById, new { Id });
	}

	public static async Task SaveProductStockAdjustment(DateTime transactionDateTime, int locationId, List<ProductStockAdjustmentCartModel> cart)
	{
		var transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(transactionDateTime, locationId);
		var stockSummary = await LoadProductStockSummaryByDateLocationId(transactionDateTime, transactionDateTime, locationId);

		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new Exception("Cannot delete stock entry as the financial year is locked.");

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
					TransactionNo = transactionNo,
					TransactionDate = DateOnly.FromDateTime(transactionDateTime),
					LocationId = locationId
				});
		}
	}
}