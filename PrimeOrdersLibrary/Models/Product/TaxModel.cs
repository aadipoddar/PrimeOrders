namespace PrimeOrdersLibrary.Models.Product;

public class TaxModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public decimal CGST { get; set; }
	public decimal SGST { get; set; }
	public decimal IGST { get; set; }
	public bool Status { get; set; }
}
