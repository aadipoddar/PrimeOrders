namespace PrimeOrdersLibrary.Models.Inventory;

public class KitchenIssueModel
{
	public int Id { get; set; }
	public int KitchenId { get; set; }
	public int LocationId { get; set; }
	public int UserId { get; set; }
	public DateTime IssueDate { get; set; }
	public bool Status { get; set; }
}

public class KitchenIssueDetailModel
{
	public int Id { get; set; }
	public int KitchenIssueId { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}