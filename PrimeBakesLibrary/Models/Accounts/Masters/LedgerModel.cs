namespace PrimeBakesLibrary.Models.Accounts.Masters;

public class LedgerModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Alias { get; set; }
	public int GroupId { get; set; }
	public int AccountTypeId { get; set; }
	public string Code { get; set; }
	public string Phone { get; set; }
	public string Email { get; set; }
	public string Address { get; set; }
	public string GSTNo { get; set; }
	public string Remarks { get; set; }
	public int StateId { get; set; }
	public int? LocationId { get; set; }
	public bool Status { get; set; }
}

public class LedgerOverviewModel
{
	public int LedgerId { get; set; }
	public string LedgerName { get; set; }
	public string LedgerAlias { get; set; }
	public string LedgerCode { get; set; }
	public int AccountTypeId { get; set; }
	public string AccountTypeName { get; set; }
	public int GroupId { get; set; }
	public string GroupName { get; set; }
	public string Address { get; set; }
	public string GSTNo { get; set; }
	public string Phone { get; set; }
	public string Email { get; set; }
	public int StateId { get; set; }
	public string StateName { get; set; }
	public string LedgerRemarks { get; set; }
	public int? LedgerLocationId { get; set; }
	public string LedgerLocationName { get; set; }
	public int AccountingId { get; set; }
	public DateOnly AccountingDate { get; set; }
	public string TransactionNo { get; set; }
	public string AccountingRemarks { get; set; }
	public int? ReferenceId { get; set; }
	public string? ReferenceType { get; set; }
	public string? ReferenceNo { get; set; }
	public DateTime? ReferenceDate { get; set; }
	public decimal? ReferenceAmount { get; set; }
	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }
	public string Remarks { get; set; }
}