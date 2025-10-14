namespace PrimeBakesLibrary.Models.Sale;

public class SaleReturnModel
{
	public int Id { get; set; }
	public string BillNo { get; set; }
	public decimal DiscPercent { get; set; }
	public string DiscReason { get; set; }
	public decimal RoundOff { get; set; }
	public string Remarks { get; set; }
	public int UserId { get; set; }
	public int LocationId { get; set; }
	public DateTime SaleReturnDateTime { get; set; }
	public int? PartyId { get; set; }
	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }
	public int? CustomerId { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool Status { get; set; }
}

public class SaleReturnDetailModel
{
	public int Id { get; set; }
	public int SaleReturnId { get; set; }
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
	public decimal NetRate { get; set; }
	public bool Status { get; set; }
}

public class SaleReturnProductCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public int ProductCategoryId { get; set; }
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
	public decimal NetRate { get; set; }
}

public class SaleReturnOverviewModel
{
	public int SaleReturnId { get; set; }
	public string BillNo { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public DateTime SaleReturnDateTime { get; set; }
	public int? PartyId { get; set; }
	public string? PartyName { get; set; }
	public string Remarks { get; set; }
	public decimal DiscountPercent { get; set; }
	public string DiscountReason { get; set; }
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal TotalTaxAmount { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal SubTotal { get; set; }
	public decimal AfterTax { get; set; }
	public decimal RoundOff { get; set; }
	public decimal Total { get; set; }
	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }
	public int? CustomerId { get; set; }
	public string? CustomerName { get; set; }
	public string? CustomerNumber { get; set; }
	public DateTime CreatedAt { get; set; }
}