namespace PrimeOrdersLibrary.Models.Sale;

public class SaleReturnModel
{
	public int Id { get; set; }
	public int SaleId { get; set; }
	public string TransactionNo { get; set; }
	public string Remarks { get; set; }
	public int UserId { get; set; }
	public int LocationId { get; set; }
	public DateTime ReturnDateTime { get; set; }
	public bool Status { get; set; }
}

public class SaleReturnDetailModel
{
	public int Id { get; set; }
	public int SaleReturnId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}

public class SaleReturnProductCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
	public decimal MaxQuantity { get; set; }
	public decimal SoldQuantity { get; set; }
	public decimal AlreadyReturnedQuantity { get; set; }
}

public class SaleReturnOverviewModel
{
	public int SaleReturnId { get; set; }
	public string TransactionNo { get; set; }
	public int SaleId { get; set; }
	public string OriginalBillNo { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public DateTime ReturnDateTime { get; set; }
	public string Remarks { get; set; }
	public bool Status { get; set; }
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
}

#region Charts and Helper Methods
public class LocationGroupSaleReturnChartData
{
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public int TotalReturns { get; set; }
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
}

public class LocationWiseSaleReturnData
{
	public string LocationName { get; set; }
	public decimal TotalQuantity { get; set; }
}

public class DailySaleReturnChartData
{
	public string Date { get; set; }
	public decimal TotalQuantity { get; set; }
}

public class ProductCategorySaleReturnChartData
{
	public string CategoryName { get; set; }
	public int ProductCount { get; set; }
}
#endregion