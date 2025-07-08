using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory.Purchase;

public class PurchaseData
{
	public static async Task<int> InsertPurchase(PurchaseModel purchase) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchase, purchase)).FirstOrDefault();

	public static async Task InsertPurchaseDetail(PurchaseDetailModel purchaseDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertPurchaseDetail, purchaseDetail);

	public static async Task<List<PurchaseDetailModel>> LoadPurchaseDetailByPurchase(int PurchaseId) =>
		await SqlDataAccess.LoadData<PurchaseDetailModel, dynamic>(StoredProcedureNames.LoadPurchaseDetailByPurchase, new { PurchaseId });

	public static async Task<List<PurchaseOverviewModel>> LoadPurchaseDetailsByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<PurchaseOverviewModel, dynamic>(StoredProcedureNames.LoadPurchaseDetailsByDate, new { FromDate, ToDate });
}