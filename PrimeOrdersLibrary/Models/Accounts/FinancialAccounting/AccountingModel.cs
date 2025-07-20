namespace PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;

public class AccountingModel
{
	public int Id { get; set; }
	public string ReferenceNo { get; set; }
	public int VoucherId { get; set; }
	public string Remarks { get; set; }
	public DateOnly AccountingDate { get; set; }
	public int FinancialYearId { get; set; }
	public int UserId { get; set; }
	public bool Generated { get; set; }
	public bool Status { get; set; }
}

public class AccountingDetailsModel
{
	public int Id { get; set; }
	public int AccountingId { get; set; }
	public char Type { get; set; }
	public int LedgerId { get; set; }
	public decimal Amount { get; set; }
	public string Remarks { get; set; }
	public bool Status { get; set; }
}

public class AccountingCartModel
{
	public int Serial { get; set; }
	public int Id { get; set; }
	public string Name { get; set; }
	public string Remarks { get; set; }
	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }
}