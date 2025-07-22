namespace PrimeOrdersLibrary.Models.Accounts.FinancialAccounting;

public class TrialBalanceModel
{
	public int LedgerId { get; set; }
	public string LedgerName { get; set; }
	public string LedgerCode { get; set; }
	public int GroupId { get; set; }
	public string GroupName { get; set; }
	public int AccountTypeId { get; set; }
	public string AccountTypeName { get; set; }
	public string Phone { get; set; }
	public string Address { get; set; }
	public string GSTNo { get; set; }
	public int StateId { get; set; }
	public string StateName { get; set; }
	public int? LocationId { get; set; }
	public string LocationName { get; set; }
	public decimal OpeningBalance { get; set; }
	public decimal TotalDebit { get; set; }
	public decimal TotalCredit { get; set; }
	public decimal ClosingBalance { get; set; }
}
