namespace PrimeOrdersLibrary.Models.Inventory;

public class StockModel
{
	public int Id { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public string Type { get; set; }
	public int BillId { get; set; }
	public DateOnly TransactionDate { get; set; }
	public int LocationId { get; set; }
}

public enum StockType
{
	Purchase,
	PurchaseReturn,
	Sale
}

public class ItemQantityModel
{
	public int ItemId { get; set; }
	public decimal Quantity { get; set; }
}