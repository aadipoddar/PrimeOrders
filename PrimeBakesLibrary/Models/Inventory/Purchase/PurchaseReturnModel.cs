namespace PrimeBakesLibrary.Models.Inventory.Purchase;

public class PurchaseReturnModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public int PartyId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public decimal ItemsTotalAmount { get; set; }
	public decimal OtherChargesPercent { get; set; }
	public decimal OtherChargesAmount { get; set; }
	public decimal CashDiscountPercent { get; set; }
	public decimal CashDiscountAmount { get; set; }
	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }
	public string? Remarks { get; set; }
	public string? DocumentUrl { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class PurchaseReturnDetailModel
{
	public int Id { get; set; }
	public int PurchaseReturnId { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public string UnitOfMeasurement { get; set; }
	public decimal Rate { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal AfterDiscount { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal TotalTaxAmount { get; set; }
	public bool InclusiveTax { get; set; }
	public decimal Total { get; set; }
	public decimal NetRate { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class PurchaseReturnItemCartModel
{
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
	public string UnitOfMeasurement { get; set; }
	public decimal Rate { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal AfterDiscount { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal TotalTaxAmount { get; set; }
	public bool InclusiveTax { get; set; }
	public decimal Total { get; set; }
	public decimal NetRate { get; set; }
	public string? Remarks { get; set; }
}

public class PurchaseReturnOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int PartyId { get; set; }
	public string PartyName { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public decimal OtherChargesPercent { get; set; }
	public decimal OtherChargesAmount { get; set; }
	public decimal CashDiscountPercent { get; set; }
	public decimal CashDiscountAmount { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }

	public decimal BaseTotal { get; set; }

	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }

	public decimal AfterDiscount { get; set; }

	public decimal SGSTPercent { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal IGSTPercent { get; set; }

	public decimal SGSTAmount { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal IGSTAmount { get; set; }

	public decimal TotalTaxAmount { get; set; }

	public decimal TotalAfterTax { get; set; }

	public decimal TotalAfterOtherCharges { get; set; }
	public decimal TotalAfterCashDiscount { get; set; }
	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }

	public string? Remarks { get; set; }
	public string? DocumentUrl { get; set; }
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

public class PurchaseReturnItemOverviewModel
{
	public int Id { get; set; }
	public string ItemName { get; set; }
	public string ItemCode { get; set; }
	public int ItemCategoryId { get; set; }
	public string ItemCategoryName { get; set; }
	public int PurchaseReturnId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int PartyId { get; set; }
	public string PartyName { get; set; }
	public string PurchaseReturnRemarks { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal AfterDiscount { get; set; }
	public decimal CGSTPercent { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTPercent { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTPercent { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal TotalTaxAmount { get; set; }
	public bool InclusiveTax { get; set; }
	public decimal Total { get; set; }
	public decimal NetRate { get; set; }
	public string? Remarks { get; set; }
}