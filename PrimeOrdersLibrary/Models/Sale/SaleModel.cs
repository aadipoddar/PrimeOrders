namespace PrimeOrdersLibrary.Models.Sale;

public class SaleModel
{
	public int Id { get; set; }
	public decimal DiscPercent { get; set; }
	public string DiscReason { get; set; }
	public string Remarks { get; set; }
	public int UserId { get; set; }
	public int LocationId { get; set; }
	public DateTime SaleDateTime { get; set; }
	public int? PartyId { get; set; }
	public int? OrderId { get; set; }
	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }
	public bool Status { get; set; }
}

public class SaleDetailModel
{
	public int Id { get; set; }
	public int SaleId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscPercent { get; set; }
	public decimal DiscAmount { get; set; }
	public decimal AfterDiscount { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal Total { get; set; }
	public bool Status { get; set; }
}

public class SaleProductCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscPercent { get; set; }
	public decimal DiscAmount { get; set; }
	public decimal AfterDiscount { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal Total { get; set; }
}