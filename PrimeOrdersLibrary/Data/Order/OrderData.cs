namespace PrimeOrdersLibrary.Data.Order;

public static class OrderData
{
	public static async Task<int> InsertOrder(OrderModel order) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrder, order)).FirstOrDefault();

	public static async Task InsertOrderDetail(OrderDetailModel orderDetail) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertOrderDetail, orderDetail);

	public static async Task<List<OrderDetailModel>> LoadOrderDetailByOrder(int OrderId) =>
		await SqlDataAccess.LoadData<OrderDetailModel, dynamic>(StoredProcedureNames.LoadOrderDetailByOrder, new { OrderId });

	public static async Task<List<OrderModel>> LoadOrderByCompleted(bool Completed = true) =>
		await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadOrderByCompleted, new { Completed });
}
