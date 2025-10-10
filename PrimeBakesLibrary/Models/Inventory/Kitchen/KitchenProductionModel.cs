namespace PrimeBakesLibrary.Models.Inventory.Kitchen;

public class KitchenProductionModel
{
	public int Id { get; set; }
	public int KitchenId { get; set; }
	public int LocationId { get; set; }
	public int UserId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime ProductionDate { get; set; }
	public string Remarks { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool Status { get; set; }
}

public class KitchenProductionDetailModel
{
	public int Id { get; set; }
	public int KitchenProductionId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
	public bool Status { get; set; }
}

public class KitchenProductionOverviewModel
{
	public int KitchenProductionId { get; set; }
	public string TransactionNo { get; set; }
	public int KitchenId { get; set; }
	public string KitchenName { get; set; }
	public DateTime ProductionDate { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public string Remarks { get; set; }
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal TotalAmount { get; set; }
	public bool Status { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class KitchenProductionProductCartModel
{
	public int ProductCategoryId { get; set; }
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
}

#region Charts and Helper Methods
public class KitchenProductionDetailDisplayModel
{
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
}

public class KitchenWiseProductionChartData
{
	public string KitchenName { get; set; }
	public decimal TotalQuantity { get; set; }
}

public class DailyProductionChartData
{
	public string Date { get; set; }
	public decimal TotalQuantity { get; set; }
}

public class KitchenProductionCountChartData
{
	public string KitchenName { get; set; }
	public int ProductionCount { get; set; }
}
#endregion