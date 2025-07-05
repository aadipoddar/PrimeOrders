namespace PrimeOrdersLibrary.Models.Sale;

public class SaleReturnModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public string Remarks { get; set; }
	public int UserId { get; set; }
	public int LocationId { get; set; }
	public DateTime ReturnDateTime { get; set; }
	public bool Status { get; set; }
}

public class SaleReturnDetailModel
{
	public int Id { get; set; }
	public int SaleReturnId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}

public class SaleReturnProductCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
}