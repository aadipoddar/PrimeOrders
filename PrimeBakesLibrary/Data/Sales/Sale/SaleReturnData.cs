using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Sale;

public static class SaleReturnData
{
    public static async Task<int> InsertSaleReturn(SaleReturnModel saleReturn) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturn, saleReturn)).FirstOrDefault();

    public static async Task<int> InsertSaleReturnDetail(SaleReturnDetailModel saleReturnDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertSaleReturnDetail, saleReturnDetail)).FirstOrDefault();

    public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int saleReturnId)
    {
        try
        {
            // Load saved sale return details
            var transaction = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturnId) ??
                throw new InvalidOperationException("Transaction not found.");

            // Load sale return details from database
            var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturnId);
            if (transactionDetails is null || transactionDetails.Count == 0)
                throw new InvalidOperationException("No transaction details found for the transaction.");

            // Load company, location, and party
            var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
            var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

            // Try to load party (party can be null for cash sales)
            LedgerModel party = null;
            if (transaction.PartyId.HasValue && transaction.PartyId.Value > 0)
                party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId.Value);

            // Try to load customer (customer can be null)
            CustomerModel customer = null;
            if (transaction.CustomerId.HasValue && transaction.CustomerId.Value > 0)
                customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, transaction.CustomerId.Value);

            if (company is null)
                throw new InvalidOperationException("Company information is missing.");

            // Generate invoice PDF
            var pdfStream = await SaleReturnInvoicePDFExport.ExportSaleReturnInvoice(
                transaction,
                transactionDetails,
                company,
                party,
                customer,
                null, // logo path - uses default
                "SALE RETURN INVOICE",
                location?.Name // outlet
            );

            // Generate file name
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"SALE_RETURN_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
            return (pdfStream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate and download invoice." + ex.Message);
        }
    }

    public static async Task<(MemoryStream excelStream, string fileName)> GenerateAndDownloadExcelInvoice(int saleReturnId)
    {
        try
        {
            // Load saved sale return details
            var transaction = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturnId) ??
                throw new InvalidOperationException("Transaction not found.");

            // Load sale return details from database
            var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturnId);
            if (transactionDetails is null || transactionDetails.Count == 0)
                throw new InvalidOperationException("No transaction details found for the transaction.");

            // Load company, location, and party
            var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
            var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

            // Try to load party (party can be null for cash sales)
            LedgerModel party = null;
            if (transaction.PartyId.HasValue && transaction.PartyId.Value > 0)
                party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId.Value);

            // Try to load customer (customer can be null)
            CustomerModel customer = null;
            if (transaction.CustomerId.HasValue && transaction.CustomerId.Value > 0)
                customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, transaction.CustomerId.Value);

            if (company is null)
                throw new InvalidOperationException("Company information is missing.");

            // Generate invoice Excel
            var excelStream = await SaleReturnInvoiceExcelExport.ExportSaleReturnInvoice(
                transaction,
                transactionDetails,
                company,
                party,
                customer,
                null, // logo path - uses default
                "SALE RETURN INVOICE",
                location?.Name // outlet
            );

            // Generate file name
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"SALE_RETURN_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
            return (excelStream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate and download Excel invoice." + ex.Message);
        }
    }

    public static async Task DeleteSaleReturn(int saleReturnId)
    {
        var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturnId);
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        if (saleReturn is not null)
        {
            saleReturn.Status = false;
            await InsertSaleReturn(saleReturn);

            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.SaleReturn.ToString(), saleReturn.Id, saleReturn.LocationId);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.SaleReturn.ToString(), saleReturn.Id);

            if (saleReturn.PartyId is not null || saleReturn.PartyId > 0)
            {
                var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
                if (party.LocationId.HasValue && party.LocationId.Value > 0)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.PurchaseReturn.ToString(), saleReturn.Id, party.LocationId.Value);
            }

            var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleReturnVoucher.Value), saleReturn.Id, saleReturn.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                await AccountingData.InsertAccounting(existingAccounting);
            }
        }
    }

    public static async Task RecoverSaleReturnTransaction(SaleReturnModel saleReturn)
    {
        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturn.Id);
        List<SaleReturnItemCartModel> transactionItemCarts = [];

        foreach (var item in transactionDetails)
            transactionItemCarts.Add(new()
            {
                ItemId = item.ProductId,
                ItemName = "",
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

        await SaveSaleReturnTransaction(saleReturn, transactionItemCarts);
    }

    public static async Task<int> SaveSaleReturnTransaction(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> saleReturnDetails)
    {
        bool update = saleReturn.Id > 0;

        if (update)
        {
            var existingSaleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, saleReturn.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingSaleReturn.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            saleReturn.TransactionNo = existingSaleReturn.TransactionNo;
        }
        else
            saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(saleReturn);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        saleReturn.Id = await InsertSaleReturn(saleReturn);
        await SaveSaleReturnDetail(saleReturn, saleReturnDetails, update);
        await SaveProductStock(saleReturn, saleReturnDetails, update);
        await SaveRawMaterialStockByRecipe(saleReturn, saleReturnDetails, update);
        await SaveAccounting(saleReturn, update);

        return saleReturn.Id;
    }

    private static async Task SaveSaleReturnDetail(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> saleReturnDetails, bool update)
    {
        if (update)
        {
            var existingTransactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, saleReturn.Id);
            foreach (var item in existingTransactionDetails)
            {
                item.Status = false;
                await InsertSaleReturnDetail(item);
            }
        }

        foreach (var item in saleReturnDetails)
            await InsertSaleReturnDetail(new()
            {
                Id = 0,
                MasterId = saleReturn.Id,
                ProductId = item.ItemId,
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
                TotalTaxAmount = item.TotalTaxAmount,
                InclusiveTax = item.InclusiveTax,
                NetRate = item.NetRate,
                Total = item.Total,
                Remarks = item.Remarks,
                Status = true
            });
    }

    private static async Task SaveProductStock(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, bool update)
    {
        if (update)
        {
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.SaleReturn.ToString(), saleReturn.Id, saleReturn.LocationId);

            if (saleReturn.PartyId is not null || saleReturn.PartyId > 0)
            {
                var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
                if (party.LocationId.HasValue && party.LocationId.Value > 0)
                    await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(StockType.PurchaseReturn.ToString(), saleReturn.Id, party.LocationId.Value);
            }
        }

        // Location Stock Update
        foreach (var item in cart)
            await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ItemId,
                Quantity = item.Quantity,
                NetRate = item.NetRate,
                TransactionId = saleReturn.Id,
                Type = StockType.SaleReturn.ToString(),
                TransactionNo = saleReturn.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
                LocationId = saleReturn.LocationId
            });

        // Party Location Stock Update
        if (saleReturn.PartyId is not null || saleReturn.PartyId > 0)
        {
            var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, saleReturn.PartyId.Value);
            if (party.LocationId.HasValue && party.LocationId.Value > 0)
                foreach (var item in cart)
                    await ProductStockData.InsertProductStock(new()
                    {
                        Id = 0,
                        ProductId = item.ItemId,
                        Quantity = -item.Quantity,
                        NetRate = item.NetRate,
                        TransactionId = saleReturn.Id,
                        Type = StockType.PurchaseReturn.ToString(),
                        TransactionNo = saleReturn.TransactionNo,
                        TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime),
                        LocationId = party.LocationId.Value
                    });
        }
    }

    private static async Task SaveRawMaterialStockByRecipe(SaleReturnModel saleReturn, List<SaleReturnItemCartModel> cart, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.SaleReturn.ToString(), saleReturn.Id);

        if (saleReturn.LocationId != 1)
            return;

        foreach (var product in cart)
        {
            var recipe = await RecipeData.LoadRecipeByProduct(product.ItemId);
            var recipeItems = recipe is null ? [] : await RecipeData.LoadRecipeDetailByRecipe(recipe.Id);

            foreach (var recipeItem in recipeItems)
                await RawMaterialStockData.InsertRawMaterialStock(new()
                {
                    Id = 0,
                    RawMaterialId = recipeItem.RawMaterialId,
                    Quantity = recipeItem.Quantity * product.Quantity,
                    NetRate = product.NetRate / recipeItem.Quantity,
                    TransactionId = saleReturn.Id,
                    TransactionNo = saleReturn.TransactionNo,
                    Type = StockType.SaleReturn.ToString(),
                    TransactionDate = DateOnly.FromDateTime(saleReturn.TransactionDateTime)
                });
        }
    }

    private static async Task SaveAccounting(SaleReturnModel saleReturn, bool update)
    {
        if (update)
        {
            var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
            var existingAccounting = await AccountingData.LoadAccountingByVoucherReference(int.Parse(saleReturnVoucher.Value), saleReturn.Id, saleReturn.TransactionNo);
            if (existingAccounting is not null && existingAccounting.Id > 0)
            {
                existingAccounting.Status = false;
                await AccountingData.InsertAccounting(existingAccounting);
            }
        }

		if (saleReturn.LocationId != 1)
			return;

		var saleReturnOverview = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(ViewNames.SaleReturnOverview, saleReturn.Id);
        if (saleReturnOverview is null)
            return;

        if (saleReturnOverview.TotalAmount == 0)
            return;

        var accountingCart = new List<AccountingItemCartModel>();

        if (saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card > 0)
        {
            var cashLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = saleReturnOverview.Id,
                ReferenceType = ReferenceTypes.SaleReturn.ToString(),
                ReferenceNo = saleReturnOverview.TransactionNo,
                LedgerId = int.Parse(cashLedger.Value),
                Debit = null,
                Credit = saleReturnOverview.Cash + saleReturnOverview.UPI + saleReturnOverview.Card,
                Remarks = $"Cash Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
            });
        }

        if (saleReturnOverview.Credit > 0)
            accountingCart.Add(new()
            {
                ReferenceId = saleReturnOverview.Id,
                ReferenceType = ReferenceTypes.SaleReturn.ToString(),
                ReferenceNo = saleReturnOverview.TransactionNo,
                LedgerId = saleReturnOverview.PartyId.Value,
                Debit = null,
                Credit = saleReturnOverview.Credit,
                Remarks = $"Party Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
            });

        if (saleReturnOverview.TotalAmount - saleReturnOverview.TotalExtraTaxAmount > 0)
        {
            var saleLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = saleReturnOverview.Id,
                ReferenceType = ReferenceTypes.SaleReturn.ToString(),
                ReferenceNo = saleReturnOverview.TransactionNo,
                LedgerId = int.Parse(saleLedger.Value),
                Debit = saleReturnOverview.TotalAmount - saleReturnOverview.TotalExtraTaxAmount,
                Credit = null,
                Remarks = $"Sale Return Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
            });
        }

        if (saleReturnOverview.TotalExtraTaxAmount > 0)
        {
            var gstLedger = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
            accountingCart.Add(new()
            {
                ReferenceId = saleReturnOverview.Id,
                ReferenceType = ReferenceTypes.SaleReturn.ToString(),
                ReferenceNo = saleReturnOverview.TransactionNo,
                LedgerId = int.Parse(gstLedger.Value),
                Debit = saleReturnOverview.TotalExtraTaxAmount,
                Credit = null,
                Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
            });
        }

		var voucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
		var accounting = new AccountingModel
		{
			Id = 0,
			TransactionNo = "",
			CompanyId = saleReturnOverview.CompanyId,
			VoucherId = int.Parse(voucher.Value),
			ReferenceId = saleReturnOverview.Id,
			ReferenceNo = saleReturnOverview.TransactionNo,
			TransactionDateTime = saleReturnOverview.TransactionDateTime,
			FinancialYearId = saleReturnOverview.FinancialYearId,
			TotalDebitLedgers = accountingCart.Count(a => a.Debit.HasValue),
			TotalCreditLedgers = accountingCart.Count(a => a.Credit.HasValue),
			TotalDebitAmount = accountingCart.Sum(a => a.Debit ?? 0),
			TotalCreditAmount = accountingCart.Sum(a => a.Credit ?? 0),
			Remarks = saleReturnOverview.Remarks,
			CreatedBy = saleReturnOverview.CreatedBy,
			CreatedAt = saleReturnOverview.CreatedAt,
			CreatedFromPlatform = saleReturnOverview.CreatedFromPlatform,
			Status = true
		};

		await AccountingData.SaveAccountingTransaction(accounting, accountingCart);
    }
}
