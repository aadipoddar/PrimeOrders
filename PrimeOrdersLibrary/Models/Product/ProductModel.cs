namespace PrimeOrdersLibrary.Models.Product;

public class ProductModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public int ProductCategoryId { get; set; }
	public decimal Rate { get; set; }
	public int TaxId { get; set; }
	public int LocationId { get; set; }
	public bool Status { get; set; }
}

public class ProductOverviewModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public string ProductCode { get; set; }
	public int ProductCategoryId { get; set; }
	public string ProductCategoryName { get; set; }
	public int SaleId { get; set; }
	public DateTime BillDateTime { get; set; }
	public int LocationId { get; set; }
	public decimal QuantitySold { get; set; }
	public decimal AveragePrice { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal SubTotal { get; set; }
	public decimal CGSTAmount { get; set; }
	public decimal SGSTAmount { get; set; }
	public decimal IGSTAmount { get; set; }
	public decimal TotalTaxAmount { get; set; }
	public decimal TotalAmount { get; set; }
}

public class ProductRateModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public decimal Rate { get; set; }
	public int LocationId { get; set; }
	public bool Status { get; set; }
}

public class ProductCategoryModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int LocationId { get; set; }
	public bool Status { get; set; }
}


#region Chart and Helper Models
public class DailyProductSalesChartData
{
	public string Date { get; set; }
	public decimal Amount { get; set; }
}

public class SalesQuantityChartData
{
	public string Date { get; set; }
	public decimal Amount { get; set; }
	public decimal Quantity { get; set; }
}

public class LocationSalesChartData
{
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public decimal Amount { get; set; }
}

public class TaxComponentChartData
{
	public string Component { get; set; }
	public decimal Amount { get; set; }
}
#endregion