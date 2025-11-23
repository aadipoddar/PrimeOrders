namespace PrimeBakesLibrary.Models.Sales.Order;

public class OrderModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public int LocationId { get; set; }
	public int? SaleId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }

}

public class OrderDetailModel
{
	public int Id { get; set; }
	public int OrderId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class OrderItemCartModel
{
	public int ItemCategoryId { get; set; }
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
	public string? Remarks { get; set; }
}

public class OrderOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }

    public int? SaleId { get; set; }
	public string? SaleTransactionNo { get; set; }
	public DateTime? SaleDateTime { get; set; }

    public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }

    public DateTime TransactionDateTime { get; set; }
	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public int? LastModifiedBy { get; set; }
	public string? LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
	public bool Status { get; set; }
}

public class OrderItemOverviewModel
{
	public int Id { get; set; }
	public string ItemName { get; set; }
	public string ItemCode { get; set; }
	public int ItemCategoryId { get; set; }
	public string ItemCategoryName { get; set; }

	public int OrderId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }

	public int? SaleId { get; set; }
	public string? SaleTransactionNo { get; set; }
	public string? OrderRemarks { get; set; }

	public decimal Quantity { get; set; }
	public string? Remarks { get; set; }
}