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