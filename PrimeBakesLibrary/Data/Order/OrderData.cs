using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Notification;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;

namespace PrimeBakesLibrary.Data.Order;

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

	public static async Task<OrderOverviewModel> LoadOrderOverviewByOrderId(int OrderId) =>
		(await SqlDataAccess.LoadData<OrderOverviewModel, dynamic>(StoredProcedureNames.LoadOrderOverviewByOrderId, new { OrderId })).FirstOrDefault();

	public static async Task<int> SaveOrder(OrderModel order, List<OrderProductCartModel> cart)
	{
		bool update = order.Id > 0;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, order.UserId);

		if (!user.Admin || user.LocationId != 1)
			order.LocationId = user.LocationId;

		order.Status = true;
		order.CreatedAt = DateTime.Now;
		order.OrderDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(order.OrderDateTime)
			.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)),
			"India Standard Time");
		order.OrderNo = update ?
			order.OrderNo :
			await GenerateCodes.GenerateOrderBillNo(order);

		order.Id = await InsertOrder(order);
		await SaveOrderDetail(order, cart, update);
		await SendNotification.SendOrderNotificationMainLocationAdmin(order.Id);

		return order.Id;
	}

	private static async Task SaveOrderDetail(OrderModel order, List<OrderProductCartModel> cart, bool update)
	{
		if (update)
		{
			var existingOrderDetails = await OrderData.LoadOrderDetailByOrder(order.Id);
			foreach (var existingDetail in existingOrderDetails)
			{
				existingDetail.Status = false;
				await OrderData.InsertOrderDetail(existingDetail);
			}
		}

		foreach (var cartItem in cart)
			await InsertOrderDetail(new()
			{
				Id = 0,
				OrderId = order.Id,
				ProductId = cartItem.ProductId,
				Quantity = cartItem.Quantity,
				Status = true
			});
	}
}