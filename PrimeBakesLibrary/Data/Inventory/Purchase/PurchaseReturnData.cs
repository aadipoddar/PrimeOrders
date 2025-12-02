using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Purchase;

public static class PurchaseReturnData
{
    public static async Task<int> InsertPurchaseReturn(PurchaseReturnModel purchaseReturn) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseReturn, purchaseReturn)).FirstOrDefault();

    public static async Task<int> InsertPurchaseReturnDetail(PurchaseReturnDetailModel purchaseReturnDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertPurchaseReturnDetail, purchaseReturnDetail)).FirstOrDefault();

    public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int purchaseReturnId)
    {
        try
        {
            // Load saved purchase return details (since _purchaseReturn now has the Id)
            var transaction = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturnId) ??
                throw new InvalidOperationException("Transaction not found.");

            // Load purchase return details from database
            var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, purchaseReturnId);
            if (transactionDetails is null || transactionDetails.Count == 0)
                throw new InvalidOperationException("No transaction details found for the transaction.");

            // Load company and party
            var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId);
            if (company is null || party is null)
                throw new InvalidOperationException("Invoice generation skipped - company or party not found.");

            // Generate invoice PDF
            var pdfStream = await Task.Run(() =>
                PurchaseReturnInvoicePDFExport.ExportPurchaseReturnInvoice(
                    transaction,
                    transactionDetails,
                    company,
                    party,
                    null, // logo path - uses default
                    "PURCHASE RETURN INVOICE"
                )
            );

            // Generate file name
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"PURCHASE_RETURN_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
            return (pdfStream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invoice generation failed: {ex.Message}", ex);
        }
    }

    public static async Task DeletePurchaseReturn(int purchaseReturnId)
    {
        var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturnId);
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchaseReturn.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        if (purchaseReturn is not null)
        {
            purchaseReturn.Status = false;
            await InsertPurchaseReturn(purchaseReturn);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.PurchaseReturn.ToString(), purchaseReturn.Id);

            var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(purchaseReturnVoucher.Value), purchaseReturn.Id, purchaseReturn.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                await AccountingData.InsertAccounting(existingAccounting);
            }
        }
    }

    public static async Task RecoverPurchaseReturnTransaction(PurchaseReturnModel purchaseReturn)
    {
        var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, purchaseReturn.Id);
        List<PurchaseReturnItemCartModel> purchaseItemCarts = [];

        foreach (var item in transactionDetails)
            purchaseItemCarts.Add(new()
            {
                ItemId = item.RawMaterialId,
                ItemName = "",
                UnitOfMeasurement = item.UnitOfMeasurement,
                Quantity = item.Quantity,
                Rate = item.Rate,
                BaseTotal = item.BaseTotal,
                DiscountPercent = item.DiscountPercent,
                DiscountAmount = item.DiscountAmount,
                AfterDiscount = item.AfterDiscount,
                CGSTPercent = item.CGSTPercent,
                CGSTAmount = item.CGSTAmount,
                SGSTPercent = item.SGSTPercent,
                SGSTAmount = item.SGSTAmount,
                IGSTPercent = item.IGSTPercent,
                IGSTAmount = item.IGSTAmount,
                InclusiveTax = item.InclusiveTax,
                TotalTaxAmount = item.TotalTaxAmount,
                Total = item.Total,
                NetRate = item.NetRate,
                Remarks = item.Remarks
            });

        await SavePurchaseReturnTransaction(purchaseReturn, purchaseItemCarts);
    }

    public static async Task<int> SavePurchaseReturnTransaction(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> purchaseReturnDetails)
    {
        bool update = purchaseReturn.Id > 0;

        if (update)
        {
            var existingPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturn.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingPurchaseReturn.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");
        }

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchaseReturn.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        purchaseReturn.Id = await InsertPurchaseReturn(purchaseReturn);
        await SavePurchaseReturnDetail(purchaseReturn, purchaseReturnDetails, update);
        await SaveRawMaterialStock(purchaseReturn, purchaseReturnDetails, update);
        await SaveAccounting(purchaseReturn, update);

        return purchaseReturn.Id;
    }

    private static async Task SavePurchaseReturnDetail(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> purchaseReturnDetails, bool update)
    {
        if (update)
        {
            var existingPurchaseDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, purchaseReturn.Id);
            foreach (var item in existingPurchaseDetails)
            {
                item.Status = false;
                await InsertPurchaseReturnDetail(item);
            }
        }

        foreach (var item in purchaseReturnDetails)
            await InsertPurchaseReturnDetail(new()
            {
                Id = 0,
                MasterId = purchaseReturn.Id,
                RawMaterialId = item.ItemId,
                Quantity = item.Quantity,
                UnitOfMeasurement = item.UnitOfMeasurement,
                Rate = item.Rate,
                BaseTotal = item.BaseTotal,
                DiscountPercent = item.DiscountPercent,
                DiscountAmount = item.DiscountAmount,
                AfterDiscount = item.AfterDiscount,
                CGSTPercent = item.CGSTPercent,
                CGSTAmount = item.CGSTAmount,
                SGSTPercent = item.SGSTPercent,
                SGSTAmount = item.SGSTAmount,
                IGSTPercent = item.IGSTPercent,
                IGSTAmount = item.IGSTAmount,
                TotalTaxAmount = item.TotalTaxAmount,
                InclusiveTax = item.InclusiveTax,
                NetRate = item.NetRate,
                Total = item.Total,
                Remarks = item.Remarks,
                Status = true
            });
    }

    private static async Task SaveRawMaterialStock(PurchaseReturnModel purchaseReturn, List<PurchaseReturnItemCartModel> cart, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.PurchaseReturn.ToString(), purchaseReturn.Id);

        foreach (var item in cart)
            await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.ItemId,
                Quantity = -item.Quantity,
                NetRate = item.NetRate,
                TransactionId = purchaseReturn.Id,
                Type = StockType.PurchaseReturn.ToString(),
                TransactionNo = purchaseReturn.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(purchaseReturn.TransactionDateTime)
            });
    }

    private static async Task SaveAccounting(PurchaseReturnModel purchaseReturn, bool update)
    {
        if (update)
        {
            var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(purchaseReturnVoucher.Value), purchaseReturn.Id, purchaseReturn.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                await AccountingData.InsertAccounting(existingAccounting);
            }
        }

        var purchaseReturnOverview = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(ViewNames.PurchaseReturnOverview, purchaseReturn.Id);
        if (purchaseReturnOverview is null)
            return;

        if (purchaseReturnOverview.TotalAmount == 0)
            return;

        var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId);
        var accounting = new AccountingModel
        {
            Id = 0,
            TransactionNo = "",
            CompanyId = purchaseReturnOverview.CompanyId,
            VoucherId = int.Parse(voucher.Value),
            ReferenceId = purchaseReturnOverview.Id,
            ReferenceNo = purchaseReturnOverview.TransactionNo,
            TransactionDateTime = purchaseReturnOverview.TransactionDateTime,
            FinancialYearId = purchaseReturnOverview.FinancialYearId,
            Remarks = purchaseReturnOverview.Remarks,
            CreatedBy = purchaseReturnOverview.CreatedBy,
            CreatedAt = purchaseReturnOverview.CreatedAt,
            CreatedFromPlatform = purchaseReturnOverview.CreatedFromPlatform,
            Status = true
        };

        var accountingCart = new List<AccountingItemCartModel>();

        if (purchaseReturnOverview.TotalAmount > 0)
            accountingCart.Add(new()
            {
                ReferenceId = purchaseReturnOverview.Id,
                ReferenceType = ReferenceTypes.PurchaseReturn.ToString(),
                ReferenceNo = purchaseReturnOverview.TransactionNo,
                LedgerId = purchaseReturnOverview.PartyId,
                Debit = purchaseReturnOverview.TotalAmount,
                Credit = null,
                Remarks = $"Party Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
            });

        if (purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalExtraTaxAmount > 0)
        {
            var purchaseLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = purchaseReturnOverview.Id,
                ReferenceType = ReferenceTypes.PurchaseReturn.ToString(),
                ReferenceNo = purchaseReturnOverview.TransactionNo,
                LedgerId = int.Parse(purchaseLedger.Value),
                Debit = null,
                Credit = purchaseReturnOverview.TotalAmount - purchaseReturnOverview.TotalExtraTaxAmount,
                Remarks = $"Purchase Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
            });
        }

        if (purchaseReturnOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = purchaseReturnOverview.Id,
                ReferenceType = ReferenceTypes.PurchaseReturn.ToString(),
                ReferenceNo = purchaseReturnOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = null,
                Credit = purchaseReturnOverview.TotalExtraTaxAmount,
                Remarks = $"GST Account Posting For Purchase Return Bill {purchaseReturnOverview.TransactionNo}",
            });
        }

        await AccountingData.SaveAccountingTransaction(accounting, accountingCart);
    }
}