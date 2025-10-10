namespace PrimeBakesLibrary.Models.Sale;

public class SaleReturnModel
{
	public int Id { get; set; }
	public int SaleId { get; set; }
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
	public decimal Quantity { get; set; }
	public decimal MaxQuantity { get; set; }
	public decimal SoldQuantity { get; set; }
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
	public string TransactionNo { get; set; }
	public int SaleId { get; set; }
	public string OriginalBillNo { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public DateTime ReturnDateTime { get; set; }
	public string Remarks { get; set; }
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
	public decimal Total { get; set; }
}