using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Stock;

public static class RawMaterialStockData
{
	public static async Task<int> InsertRawMaterialStock(RawMaterialStockModel stock) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRawMaterialStock, stock)).FirstOrDefault();

	public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<RawMaterialStockSummaryModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockSummaryByDate, new { FromDate = DateOnly.FromDateTime(FromDate), ToDate = DateOnly.FromDateTime(ToDate) });

	public static async Task<List<RawMaterialStockDetailsModel>> LoadRawMaterialStockDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<RawMaterialStockDetailsModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockDetailsByDate, new { FromDate = DateOnly.FromDateTime(FromDate), ToDate = DateOnly.FromDateTime(ToDate) });

	public static async Task DeleteRawMaterialStockByTypeTransactionId(string Type, int TransactionId) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteRawMaterialStockByTypeTransactionId, new { Type, TransactionId });

	public static async Task DeleteRawMaterialStockById(int Id)
	{
		var stock = await CommonData.LoadTableDataById<RawMaterialStockModel>(TableNames.RawMaterialStock, Id);
		if (stock is null)
			return;

		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(stock.TransactionDate.ToDateTime(TimeOnly.MinValue));
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new Exception("Cannot delete stock entry as the financial year is locked.");

		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteRawMaterialStockById, new { Id });
	}

	public static async Task SaveRawMaterialStockAdjustment(DateTime transactionDateTime, List<RawMaterialStockAdjustmentCartModel> cart)
	{
		var transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(transactionDateTime);
		var stockSummary = await LoadRawMaterialStockSummaryByDate(transactionDateTime, transactionDateTime);

		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime);
		if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new Exception("Cannot delete stock entry as the financial year is locked.");

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