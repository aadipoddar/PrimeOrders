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