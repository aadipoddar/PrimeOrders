using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Data.Sale;

public static class SaleData
{
	public static async Task<int> InsertSale(SaleModel sale) =>
	(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSale, sale)).FirstOrDefault();

	public static async Task InsertSaleDetail(SaleDetailModel saleDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertSaleDetail, saleDetail);

	public static async Task<List<SaleDetailModel>> LoadSaleDetailBySale(int SaleId) =>
		await SqlDataAccess.LoadData<SaleDetailModel, dynamic>(StoredProcedureNames.LoadSaleDetailBySale, new { SaleId });
}
