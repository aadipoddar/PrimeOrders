namespace PrimeBakesLibrary.Models.Inventory.Stock;

public class RawMaterialStockModel
{
	public int Id { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public decimal? NetRate { get; set; }
	public string Type { get; set; }
	public int? TransactionId { get; set; }
	public string TransactionNo { get; set; }
	public DateOnly TransactionDate { get; set; }
}

public enum StockType
{
	Purchase,
	PurchaseReturn,
	Sale,
	SaleReturn,
	Adjustment,
	KitchenIssue,
	KitchenProduction,
}

public class RawMaterialStockDetailsModel
{
	public int Id { get; set; }
	public int RawMaterialId { get; set; }
	public string RawMaterialCode { get; set; }
	public string RawMaterialName { get; set; }
	public decimal Quantity { get; set; }
	public decimal? NetRate { get; set; }
	public string Type { get; set; }
	public int? TransactionId { get; set; }
	public string TransactionNo { get; set; }
	public DateOnly TransactionDate { get; set; }
}

public class RawMaterialStockSummaryModel
{
	public int RawMaterialId { get; set; }
	public string RawMaterialName { get; set; }
	public string RawMaterialCode { get; set; }
	public int RawMaterialCategoryId { get; set; }
	public string RawMaterialCategoryName { get; set; }
	public string UnitOfMeasurement { get; set; }
	public decimal OpeningStock { get; set; }
	public decimal PurchaseStock { get; set; }
	public decimal SaleStock { get; set; }
	public decimal MonthlyStock { get; set; }
	public decimal ClosingStock { get; set; }
	public decimal Rate { get; set; }
	public decimal ClosingValue { get; set; }
	public decimal AveragePrice { get; set; }
	public decimal LastPurchasePrice { get; set; }
	public decimal WeightedAverageValue { get; set; }
	public decimal LastPurchaseValue { get; set; }
}

public class RawMaterialStockAdjustmentCartModel
{
	public int RawMaterialId { get; set; }
	public string RawMaterialName { get; set; }
	public decimal Stock { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
}