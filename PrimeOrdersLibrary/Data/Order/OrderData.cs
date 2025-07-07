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

	#region Helper Chart Classes
	public class ProductOrderSummary
	{
		public int ProductId { get; set; }
		public string ProductCode { get; set; }
		public string ProductName { get; set; }
		public string CategoryName { get; set; }
		public decimal ProductRate { get; set; }
		public int OrderCount { get; set; }  // Number of times this product appears in orders
		public List<int> OrderIds { get; set; } = [];  // Track which orders this product appears in
		public List<string> OrderNumbers { get; set; } = [];  // Track order numbers
		public string OrderNumbersList => string.Join(", ", OrderNumbers);  // For Excel display
		public int OrdersAppeared => OrderIds.Count;  // Number of distinct orders this product appears in
		public decimal TotalQuantity { get; set; }  // Total quantity ordered across all orders
		public decimal TotalValue => ProductRate * TotalQuantity;  // Calculated total value
	}

	public class OrderDetailDisplayModel
	{
		public string ProductName { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
	}

	public class ChallanItemModel
	{
		public string ProductCode { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
	}

	public class ChartData
	{
		public string Date { get; set; }
		public int Count { get; set; }
	}

	public class StatusData
	{
		public string Status { get; set; }
		public int Count { get; set; }
	}
	#endregion
}