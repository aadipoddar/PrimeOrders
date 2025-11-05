namespace PrimeBakesLibrary.Models.Accounts.Masters;

public class VoucherModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string PrefixCode { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
