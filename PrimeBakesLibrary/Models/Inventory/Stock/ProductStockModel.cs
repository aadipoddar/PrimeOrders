namespace PrimeBakesLibrary.Models.Inventory.Stock;

public class ProductStockModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? NetRate { get; set; }
    public string Type { get; set; }
    public int? TransactionId { get; set; }
    public string TransactionNo { get; set; }
    public DateOnly TransactionDate { get; set; }
    public int LocationId { get; set; }
}

public class ProductStockDetailsModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal? NetRate { get; set; }
    public string Type { get; set; }
    public int? TransactionId { get; set; }
    public string TransactionNo { get; set; }
    public DateOnly TransactionDate { get; set; }
    public int LocationId { get; set; }
    public string LocationName { get; set; }
}

public class ProductStockSummaryModel
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
    public decimal Rate { get; set; }
    public decimal ClosingValue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal WeightedAverageValue { get; set; }
    public decimal LastSalePrice { get; set; }
    public decimal LastSaleValue { get; set; }
}

public class ProductStockAdjustmentCartModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Stock { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Total { get; set; }
}