namespace PrimeOrdersLibrary.Models.Inventory;

public class SupplierModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public string GSTNo { get; set; }
	public string Phone { get; set; }
	public string Email { get; set; }
	public string Address { get; set; }
	public bool Status { get; set; }
}