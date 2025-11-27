namespace PrimeBakesLibrary.Models.Inventory.Kitchen;

public class KitchenProductionModel
{
    public int Id { get; set; }
    public string TransactionNo { get; set; }
    public int CompanyId { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public int FinancialYearId { get; set; }
    public int KitchenId { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalQuantity { get; set; }
	public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedFromPlatform { get; set; }
    public bool Status { get; set; }
    public int? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedFromPlatform { get; set; }
}

public class KitchenProductionDetailModel
{
    public int Id { get; set; }
    public int KitchenProductionId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
    public string? Remarks { get; set; }
    public bool Status { get; set; }
}

public class KitchenProductionProductCartModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
    public string? Remarks { get; set; }
}

public class KitchenProductionOverviewModel
{
    public int Id { get; set; }
    public string TransactionNo { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public int FinancialYearId { get; set; }
    public string FinancialYear { get; set; }
    public int KitchenId { get; set; }
    public string KitchenName { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string Remarks { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedFromPlatform { get; set; }
    public int? LastModifiedBy { get; set; }
    public string LastModifiedByUserName { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string LastModifiedFromPlatform { get; set; }
    public bool Status { get; set; }
}

public class KitchenProductionItemOverviewModel
{
    public int Id { get; set; }
    public string ItemName { get; set; }
    public string ItemCode { get; set; }
    public int ItemCategoryId { get; set; }
    public string ItemCategoryName { get; set; }

    public int MasterId { get; set; }
    public string TransactionNo { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; }
    public int KitchenId { get; set; }
    public string KitchenName { get; set; }
    public string? KitchenProductionRemarks { get; set; }

    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }

    public string? Remarks { get; set; }
}