namespace PrimeOrdersLibrary.Models.Inventory;

public class RawMaterialStockModel
{
	public int Id { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public decimal? NetRate { get; set; }
	public string Type { get; set; }
	public string TransactionNo { get; set; }
	public DateOnly TransactionDate { get; set; }
	public int LocationId { get; set; }
}

public class ProductStockModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public string Type { get; set; }
	public string TransactionNo { get; set; }
	public DateOnly TransactionDate { get; set; }
	public int LocationId { get; set; }
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

public class ItemQantityModel
{
	public int ItemId { get; set; }
	public decimal Quantity { get; set; }
}

public class RawMaterialStockDetailModel
{
	public int RawMaterialId { get; set; }
	public string RawMaterialName { get; set; }
	public string RawMaterialCode { get; set; }
	public int RawMaterialCategoryId { get; set; }
	public string RawMaterialCategoryName { get; set; }
	public decimal OpeningStock { get; set; }
	public decimal PurchaseStock { get; set; }
	public decimal SaleStock { get; set; }
	public decimal MonthlyStock { get; set; }
	public decimal ClosingStock { get; set; }
	public decimal AveragePrice { get; set; }
	public decimal LastPurchasePrice { get; set; }
	public decimal WeightedAverageValue { get; set; }
	public decimal LastPurchaseValue { get; set; }
}

public class ProductStockDetailModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public string ProductCode { get; set; }
	public int ProductCategoryId { get; set; }
	public string ProductCategoryName { get; set; }
	public decimal OpeningStock { get; set; }
	public decimal PurchaseStock { get; set; }
	public decimal SaleStock { get; set; }
	public decimal MonthlyStock { get; set; }
	public decimal ClosingStock { get; set; }
}

public class StockAdjustmentRawMaterialCartModel
{
	public int RawMaterialId { get; set; }
	public string RawMaterialName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
}

public class ProductStockAdjustmentCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
}