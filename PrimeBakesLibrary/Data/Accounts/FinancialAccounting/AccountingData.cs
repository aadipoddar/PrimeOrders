using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.FinancialAccounting;

public static class AccountingData
{
    public static async Task<int> InsertAccounting(AccountingModel accounting) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccounting, accounting)).FirstOrDefault();

    public static async Task<int> InsertAccountingDetail(AccountingDetailModel accountingDetails) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAccountingDetail, accountingDetails)).FirstOrDefault();

    public static async Task<AccountingModel> LoadAccountingByVoucherReference(int VoucherId, int ReferenceId, string ReferenceNo) =>
        (await SqlDataAccess.LoadData<AccountingModel, dynamic>(StoredProcedureNames.LoadAccountingByVoucherReference, new { VoucherId, ReferenceId, ReferenceNo })).FirstOrDefault();

    public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByDate(DateTime StartDate, DateTime EndDate) =>
        await SqlDataAccess.LoadData<TrialBalanceModel, dynamic>(StoredProcedureNames.LoadTrialBalanceByDate, new { StartDate, EndDate });

    public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int accountingId)
    {
        try
        {
            // Load saved accounting details
            var transaction = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accountingId) ??
				throw new InvalidOperationException("Transaction not found.");

            // Load accounting details from database
            var transactionDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accountingId);
            if (transactionDetails is null || transactionDetails.Count == 0)
				throw new InvalidOperationException("No transaction details found for the transaction.");

            // Load company and voucher
            var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
            var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, transaction.VoucherId);

            if (company is null)
                throw new InvalidOperationException("Invoice generation skipped - company not found.");

            if (voucher is null)
                throw new InvalidOperationException("Invoice generation skipped - voucher not found.");

            // Generate invoice PDF
            var pdfStream = await AccountingInvoicePDFExport.ExportAccountingInvoice(
                transaction,
                transactionDetails,
                company,
                voucher,
                null, // logo path - uses default
                $"{voucher.Name.ToUpper()} VOUCHER"
            );

            // Generate file name
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"ACCOUNTING_VOUCHER_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
            return (pdfStream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invoice generation failed: {ex.Message}", ex);
        }
    }

    public static async Task DeleteAccounting(int accountingId)
    {
        var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accountingId);
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        accounting.Status = false;
        await InsertAccounting(accounting);
    }

    public static async Task RecoverAccountingTransaction(AccountingModel accounting)
    {
        var accountingDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accounting.Id);
        List<AccountingItemCartModel> accountingItemCarts = [];

        foreach (var item in accountingDetails)
            accountingItemCarts.Add(new()
            {
                LedgerName = string.Empty,
                LedgerId = item.LedgerId,
                ReferenceType = item.ReferenceType,
                Credit = item.Credit,
                Debit = item.Debit,
                ReferenceId = item.ReferenceId,
                ReferenceNo = item.ReferenceId.HasValue ? item.ReferenceId.Value.ToString() : null,
                Remarks = item.Remarks
            });

        await SaveAccountingTransaction(accounting, accountingItemCarts);
    }

    public static async Task<int> SaveAccountingTransaction(AccountingModel accounting, List<AccountingItemCartModel> accountingDetails)
    {
        bool update = accounting.Id > 0;

        if (update)
        {
            var existingAccounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accounting.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingAccounting.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            accounting.TransactionNo = existingAccounting.TransactionNo;
        }
        else
            accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(accounting);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        accounting.Id = await InsertAccounting(accounting);
        await SaveAccountingDetail(accounting, accountingDetails, update);

        return accounting.Id;
    }

    private static async Task SaveAccountingDetail(AccountingModel accounting, List<AccountingItemCartModel> accountingDetails, bool update)
    {
        if (update)
        {
            var existingAccountingDetails = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, accounting.Id);
            foreach (var item in existingAccountingDetails)
            {
                item.Status = false;
                await InsertAccountingDetail(item);
            }
        }

        foreach (var item in accountingDetails)
            await InsertAccountingDetail(new()
            {
                Id = 0,
                MasterId = accounting.Id,
                LedgerId = item.LedgerId,
                Credit = item.Credit,
                Debit = item.Debit,
                ReferenceType = item.ReferenceType,
                ReferenceId = item.ReferenceId,
                ReferenceNo = item.ReferenceNo,
                Remarks = item.Remarks,
                Status = true
            });
    }
}
