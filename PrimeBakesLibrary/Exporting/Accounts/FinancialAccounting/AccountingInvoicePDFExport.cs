using System.Threading.Tasks;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;

/// <summary>
/// Convert Accounting voucher data to Invoice PDF format
/// </summary>
public static class AccountingInvoicePDFExport
{
    /// <summary>
    /// Export Accounting voucher as a professional accounting voucher PDF (automatically loads ledger names)
    /// </summary>
    /// <param name="accountingHeader">Accounting header data</param>
    /// <param name="accountingDetails">Accounting detail line items (ledger entries)</param>
    /// <param name="company">Company information</param>
    /// <param name="voucher">Voucher type information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (JOURNAL VOUCHER, PAYMENT VOUCHER, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportAccountingInvoice(
        AccountingModel accountingHeader,
        List<AccountingDetailModel> accountingDetails,
        CompanyModel company,
        VoucherModel voucher,
        string logoPath = null,
        string invoiceType = "ACCOUNTING VOUCHER")
    {
        // Load all ledgers to get names
        var allLedgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

        // Map to accounting line items with proper Debit/Credit columns
        var accountingLineItems = accountingDetails.Select(detail =>
        {
            var ledger = allLedgers.FirstOrDefault(l => l.Id == detail.LedgerId);
            string ledgerName = ledger?.Name ?? $"Ledger #{detail.LedgerId}";

            return new PDFInvoiceExportUtil.AccountingLineItem
            {
                LedgerId = detail.LedgerId,
                LedgerName = ledgerName,
                ReferenceNo = detail.ReferenceNo,
                ReferenceType = detail.ReferenceType,
                Debit = detail.Debit,
                Credit = detail.Credit,
                Remarks = detail.Remarks
            };
        }).ToList();

        // Calculate totals
        decimal totalDebit = accountingDetails.Sum(d => d.Debit ?? 0);
        decimal totalCredit = accountingDetails.Sum(d => d.Credit ?? 0);

        // Map invoice header data
        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = accountingHeader.TransactionNo,
            TransactionDateTime = accountingHeader.TransactionDateTime,
            OrderTransactionNo = accountingHeader.ReferenceNo,
            ItemsTotalAmount = Math.Max(totalDebit, totalCredit),
            OtherChargesAmount = 0,
            OtherChargesPercent = 0,
            CashDiscountAmount = 0,
            CashDiscountPercent = 0,
            RoundOffAmount = 0,
            TotalAmount = Math.Max(totalDebit, totalCredit),
            Cash = 0,
            Card = 0,
            UPI = 0,
            Credit = 0,
            Remarks = accountingHeader.Remarks,
            Status = accountingHeader.Status
        };

        // Use voucher name as invoice type
        string voucherInvoiceType = !string.IsNullOrWhiteSpace(voucher?.Name)
            ? $"{voucher.Name.ToUpper()}"
            : invoiceType;

        // Generate specialized accounting voucher PDF
        return await PDFInvoiceExportUtil.ExportAccountingVoucherToPdf(
            invoiceData,
            accountingLineItems,
            company,
            logoPath,
            voucherInvoiceType
        );
    }

    /// <summary>
    /// Export Accounting with ledger names (requires additional data)
    /// </summary>
    public static async Task<MemoryStream> ExportAccountingInvoiceWithItems(
        AccountingModel accountingHeader,
        List<AccountingItemCartModel> accountingItems,
        CompanyModel company,
        VoucherModel voucher,
        string logoPath = null,
        string invoiceType = "ACCOUNTING VOUCHER")
    {
        // Map to accounting line items with proper Debit/Credit columns
        var accountingLineItems = accountingItems.Select(item => new PDFInvoiceExportUtil.AccountingLineItem
        {
            LedgerId = item.LedgerId,
            LedgerName = item.LedgerName,
            ReferenceNo = item.ReferenceNo,
            ReferenceType = item.ReferenceType,
            Debit = item.Debit,
            Credit = item.Credit,
            Remarks = item.Remarks
        }).ToList();

        // Calculate totals
        decimal totalDebit = accountingItems.Sum(i => i.Debit ?? 0);
        decimal totalCredit = accountingItems.Sum(i => i.Credit ?? 0);

        // Map invoice header data
        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = accountingHeader.TransactionNo,
            TransactionDateTime = accountingHeader.TransactionDateTime,
            OrderTransactionNo = accountingHeader.ReferenceNo,
            ItemsTotalAmount = Math.Max(totalDebit, totalCredit),
            OtherChargesAmount = 0,
            OtherChargesPercent = 0,
            CashDiscountAmount = 0,
            CashDiscountPercent = 0,
            RoundOffAmount = 0,
            TotalAmount = Math.Max(totalDebit, totalCredit),
            Cash = 0,
            Card = 0,
            UPI = 0,
            Credit = 0,
            Remarks = accountingHeader.Remarks,
            Status = accountingHeader.Status
        };

        // Use voucher name as invoice type
        string voucherInvoiceType = !string.IsNullOrWhiteSpace(voucher?.Name)
            ? $"{voucher.Name.ToUpper()}"
            : invoiceType;

        // Generate specialized accounting voucher PDF
        return await PDFInvoiceExportUtil.ExportAccountingVoucherToPdf(
            invoiceData,
            accountingLineItems,
            company,
            logoPath,
            voucherInvoiceType
        );
    }
}