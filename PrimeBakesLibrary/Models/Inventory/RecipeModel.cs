namespace PrimeBakesLibrary.Models.Inventory;

public class RecipeModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public bool Status { get; set; }
}

public class RecipeDetailModel
{
	public int Id { get; set; }
	public int RecipeId { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}

public class RecipeItemCartModel
{
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
}