namespace PrimeOrdersLibrary.Models.Inventory;

public class KitchenProductionModel
{
	public int Id { get; set; }
	public int KitchenId { get; set; }
	public int LocationId { get; set; }
	public int UserId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime ProductionDate { get; set; }
	public bool Status { get; set; }
}

public class KitchenProductionDetailModel
{
	public int Id { get; set; }
	public int KitchenProductionId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
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
	public int TotalProducts { get; set; }
	public decimal TotalQuantity { get; set; }
	public bool Status { get; set; }
}