namespace PrimeBakesLibrary.Models.Inventory.Kitchen;

public class KitchenIssueModel
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

public class KitchenIssueDetailModel
{
    public int Id { get; set; }
    public int MasterId { get; set; }
    public int RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasurement { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
    public string? Remarks { get; set; }
    public bool Status { get; set; }
}

public class KitchenIssueItemCartModel
{
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasurement { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
    public string? Remarks { get; set; }
}

public class KitchenIssueOverviewModel
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

public class KitchenIssueItemOverviewModel
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
    public string? KitchenIssueRemarks { get; set; }

    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }

    public string? Remarks { get; set; }
}