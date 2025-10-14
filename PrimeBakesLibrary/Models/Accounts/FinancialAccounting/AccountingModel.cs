namespace PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

public class AccountingModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int VoucherId { get; set; }
	public string Remarks { get; set; }
	public DateOnly AccountingDate { get; set; }
	public int FinancialYearId { get; set; }
	public int UserId { get; set; }
	public string GeneratedModule { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool Status { get; set; }
}

public class AccountingDetailsModel
{
	public int Id { get; set; }
	public int AccountingId { get; set; }
	public int LedgerId { get; set; }
	public string? ReferenceType { get; set; }
	public int? ReferenceId { get; set; }
	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }
	public string Remarks { get; set; }
	public bool Status { get; set; }
}

public class AccountingCartModel
{
	public int GroupId { get; set; }
	public int AccountTypeId { get; set; }
	public int Id { get; set; }
	public string Name { get; set; }
	public string? ReferenceType { get; set; }
	public int? ReferenceId { get; set; }
	public string? ReferenceNo { get; set; }
	public string Remarks { get; set; }
	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }
}

public enum GeneratedModules
{
	FinancialAccounting,
	Sales,
	Purchase,
	SaleReturn,
}

public enum ReferenceTypes
{
	Sales,
	Purchase,
	SaleReturn,
}

public class AccountingOverviewModel
{
	public int AccountingId { get; set; }
	public string TransactionNo { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public DateOnly AccountingDate { get; set; }
	public int VoucherId { get; set; }
	public string VoucherName { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYearPeriod { get; set; }
	public string GeneratedModule { get; set; }
	public string Remarks { get; set; }
	public int TotalLedgers { get; set; }
	public int TotalDebitLedgers { get; set; }
	public int TotalCreditLedgers { get; set; }
	public decimal TotalCreditAmount { get; set; }
	public decimal TotalDebitAmount { get; set; }
	public decimal TotalAmount { get; set; }
	public DateTime CreatedAt { get; set; }
}