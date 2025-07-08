namespace PrimeOrdersLibrary.Data.Order;

public static class OrderData
{
	public static async Task<int> InsertOrder(OrderModel order) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrder, order)).FirstOrDefault();

	public static async Task InsertOrderDetail(OrderDetailModel orderDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertOrderDetail, orderDetail);

	public static async Task<List<OrderDetailModel>> LoadOrderDetailByOrder(int OrderId) =>
		await SqlDataAccess.LoadData<OrderDetailModel, dynamic>(StoredProcedureNames.LoadOrderDetailByOrder, new { OrderId });

	public static async Task<List<OrderModel>> LoadOrderByLocation(int LocationId) =>
		await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadOrderByLocation, new { LocationId });

	public static async Task<OrderModel> LoadLastOrderByLocation(int LocationId) =>
		(await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadLastOrderByLocation, new { LocationId })).FirstOrDefault();

	public static async Task<List<OrderOverviewModel>> LoadOrderDetailsByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<OrderOverviewModel, dynamic>(StoredProcedureNames.LoadOrderDetailsByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<OrderModel> LoadOrderBySale(int SaleId) =>
		(await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadOrderBySale, new { SaleId })).FirstOrDefault();
}