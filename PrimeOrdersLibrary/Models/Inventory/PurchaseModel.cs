namespace PrimeOrdersLibrary.Models.Inventory;

public class PurchaseModel
{
	public int Id { get; set; }
	public string BillNo { get; set; }
	public int SupplierId { get; set; }
	public DateOnly BillDate { get; set; }
	public decimal CDPercent { get; set; }
	public decimal CDAmount { get; set; }
	public string Remarks { get; set; }
	public int UserId { get; set; }
	public bool Status { get; set; }
}

public class PurchaseDetailModel
{
	public int Id { get; set; }
	public int PurchaseId { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public string MeasurementUnit { get; set; }
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

public class PurchaseRawMaterialCartModel
{
	public int RawMaterialId { get; set; }
	public string RawMaterialName { get; set; }
	public decimal Quantity { get; set; }
	public string MeasurementUnit { get; set; }
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

public class PurchaseOverviewModel
{
	public int PurchaseId { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public int SupplierId { get; set; }
	public string SupplierName { get; set; }
	public string BillNo { get; set; }
	public DateOnly BillDate { get; set; }
	public string Remarks { get; set; }
	public decimal CashDiscountPercent { get; set; }
	public decimal CashDiscountAmount { get; set; }
	public int TotalItems { get; set; }
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

public enum MeasurementUnit
{
	KiloGram,
	Gram,
	Litre
}