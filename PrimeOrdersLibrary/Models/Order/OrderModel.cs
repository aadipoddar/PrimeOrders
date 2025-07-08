namespace PrimeOrdersLibrary.Models.Order;

public class OrderModel
{
	public int Id { get; set; }
	public string OrderNo { get; set; }
	public DateOnly OrderDate { get; set; }
	public int LocationId { get; set; }
	public int UserId { get; set; }
	public string Remarks { get; set; }
	public int? SaleId { get; set; }
	public bool Status { get; set; }
}

public class OrderDetailModel
{
	public int Id { get; set; }
	public int OrderId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}

public class OrderProductCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
}

public class OrderOverviewModel
{
	public int OrderId { get; set; }
	public string OrderNo { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public DateOnly OrderDate { get; set; }
	public string Remarks { get; set; }
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
	public int? SaleId { get; set; }
	public string SaleBillNo { get; set; }
	public DateTime? SaleDateTime { get; set; }
}

#region Chat and Helper Methods
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

public class OrderChartData
{
	public string Date { get; set; }
	public int Count { get; set; }
}

public class OrderStatusData
{
	public string Status { get; set; }
	public int Count { get; set; }
}
#endregion