namespace PrimeOrdersLibrary.Models.Accounts;

public class LedgerModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int GroupId { get; set; }
	public int AccountTypeId { get; set; }
	public string Code { get; set; }
	public string Phone { get; set; }
	public string Address { get; set; }
	public string GSTNo { get; set; }
	public string Remarks { get; set; }
	public int StateId { get; set; }
	public int? LocationId { get; set; }
	public bool Status { get; set; }
}