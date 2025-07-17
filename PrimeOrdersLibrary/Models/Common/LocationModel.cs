namespace PrimeOrdersLibrary.Models.Common;

public class LocationModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public decimal Discount { get; set; }
	public bool MainLocation { get; set; }
	public bool Status { get; set; }
}