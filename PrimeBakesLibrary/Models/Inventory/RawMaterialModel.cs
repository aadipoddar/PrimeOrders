namespace PrimeBakesLibrary.Models.Inventory;

public class RawMaterialModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public int RawMaterialCategoryId { get; set; }
	public decimal MRP { get; set; }
	public string MeasurementUnit { get; set; }
	public int TaxId { get; set; }
	public bool Status { get; set; }
}

public class RawMaterialCategoryModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public bool Status { get; set; }
}