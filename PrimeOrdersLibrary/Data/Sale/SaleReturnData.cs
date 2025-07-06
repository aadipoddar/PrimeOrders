using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Data.Sale;

public static class SaleReturnData
{
	public static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturn, saleReturn)).FirstOrDefault();

	public static async Task InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertSaleReturnDetail, saleReturnDetail);

	public static async Task<List<SaleReturnDetailModel>> LoadSaleReturnDetailBySaleReturn(int SaleReturnId) =>
		await SqlDataAccess.LoadData<SaleReturnDetailModel, dynamic>(StoredProcedureNames.LoadSaleReturnDetailBySaleReturn, new { SaleReturnId });

	public static async Task<List<SaleReturnModel>> LoadSaleReturnBySale(int SaleId) =>
		await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadSaleReturnBySale, new { SaleId });

	public static async Task<SaleReturnModel> LoadLastSaleReturnByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<SaleReturnModel, dynamic>(StoredProcedureNames.LoadLastSaleReturnByLocation, new { LocationId })).FirstOrDefault();
}
