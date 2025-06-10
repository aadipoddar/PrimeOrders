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

	public static async Task<List<SaleOverviewModel>> LoadSaleDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<SaleOverviewModel, dynamic>(StoredProcedureNames.LoadSaleDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<SaleModel> LoadLastSaleByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<SaleModel, dynamic>(StoredProcedureNames.LoadLastSaleByLocation, new { LocationId })).FirstOrDefault();
}
