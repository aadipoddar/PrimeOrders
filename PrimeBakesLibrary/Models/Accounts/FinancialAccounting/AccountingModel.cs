namespace PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

public class AccountingModel
{
    public int Id { get; set; }
    public string TransactionNo { get; set; }
    public int CompanyId { get; set; }
    public int VoucherId { get; set; }
    public int? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public int FinancialYearId { get; set; }
    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedFromPlatform { get; set; }
    public bool Status { get; set; }
    public int? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedFromPlatform { get; set; }
}

public class AccountingDetailModel
{
    public int Id { get; set; }
    public int AccountingId { get; set; }
    public int LedgerId { get; set; }
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public string? Remarks { get; set; }
    public bool Status { get; set; }
}

public class AccountingItemCartModel
{
    public int LedgerId { get; set; }
    public string LedgerName { get; set; }
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public string? Remarks { get; set; }
}

public enum ReferenceTypes
{
    Purchase,
    PurchaseReturn,
    Sale,
    SaleReturn,
}

public class AccountingOverviewModel
{
    public int Id { get; set; }
    public string TransactionNo { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; }
    public int VoucherId { get; set; }
    public string VoucherName { get; set; }

    public int? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }

    public DateTime TransactionDateTime { get; set; }
    public int FinancialYearId { get; set; }
    public string FinancialYear { get; set; }

    public int TotalLedgers { get; set; }
    public int TotalDebitLedgers { get; set; }
    public int TotalCreditLedgers { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Remarks { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedFromPlatform { get; set; }
    public int? LastModifiedBy { get; set; }
    public string? LastModifiedByUserName { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedFromPlatform { get; set; }
    public bool Status { get; set; }
}

public class AccountingLedgerOverviewModel
{
    public int Id { get; set; }
    public string LedgerName { get; set; }
    public string LedgerCode { get; set; }
    public int AccountTypeId { get; set; }
    public string AccountTypeName { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; }

    public int AccountingId { get; set; }
    public string TransactionNo { get; set; }
    public DateOnly TransactionDateTime { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; }
    public string AccountingRemarks { get; set; }

    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime? ReferenceDateTime { get; set; }
    public decimal? ReferenceAmount { get; set; }

    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }

    public string? Remarks { get; set; }
}

public class TrialBalanceModel
{
    public int LedgerId { get; set; }
    public string LedgerCode { get; set; }
    public string LedgerName { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public int AccountTypeId { get; set; }
    public string AccountTypeName { get; set; }

    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}