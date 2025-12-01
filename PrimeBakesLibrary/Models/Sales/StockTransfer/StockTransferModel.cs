namespace PrimeBakesLibrary.Models.Sales.StockTransfer;

public class StockTransferModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public int LocationId { get; set; }
	public int ToLocationId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal ItemDiscountAmount { get; set; }
	public decimal TotalAfterItemDiscount { get; set; }
	public decimal TotalInclusiveTaxAmount { get; set; }
	public decimal TotalExtraTaxAmount { get; set; }
	public decimal TotalAfterTax { get; set; }
	public decimal OtherChargesPercent { get; set; }
	public decimal OtherChargesAmount { get; set; }
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }
	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class StockTransferDetailModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public int ProductId { get; set; }
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
	public bool Status { get; set; }
}

public class StockTransferItemCartModel
{
	public int ItemId { get; set; }
	public string ItemName { get; set; }
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

public class StockTransferOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }

	public int LocationId { get; set; }
	public string LocationName { get; set; }

	public int ToLocationId { get; set; }
	public string ToLocationName { get; set; }

	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal ItemDiscountAmount { get; set; }
	public decimal TotalAfterItemDiscount { get; set; }
	public decimal TotalInclusiveTaxAmount { get; set; }
	public decimal TotalExtraTaxAmount { get; set; }
	public decimal TotalAfterTax { get; set; }

	public decimal OtherChargesPercent { get; set; }
	public decimal OtherChargesAmount { get; set; }
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }

	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }

	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }

	public string? PaymentModes { get; set; }

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

public class StockTransferItemOverviewModel
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

	public int LocationId { get; set; }
	public string LocationName { get; set; }
	
	public int ToLocationId { get; set; }
	public string ToLocationName { get; set; }

	public string? StockTransferRemarks { get; set; }

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
	public decimal NetTotal { get; set; }

	public string? Remarks { get; set; }
}