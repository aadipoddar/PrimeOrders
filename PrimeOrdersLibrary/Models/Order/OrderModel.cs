namespace PrimeOrdersLibrary.Models.Order;

public class OrderModel
{
	public int Id { get; set; }
	public string OrderNo { get; set; }
	public DateOnly OrderDate { get; set; }
	public int LocationId { get; set; }
	public int UserId { get; set; }
	public string Remarks { get; set; }
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