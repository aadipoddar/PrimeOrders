namespace PrimeOrdersLibrary.Models.Product;

public class ProductModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public int ProductCategoryId { get; set; }
	public decimal Rate { get; set; }
	public int TaxId { get; set; }
	public bool Status { get; set; }
}