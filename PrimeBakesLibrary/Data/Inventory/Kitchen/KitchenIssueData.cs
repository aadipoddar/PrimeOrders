using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueData
{
    public static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue)).FirstOrDefault();

    public static async Task<int> InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail)).FirstOrDefault();

    public static async Task<(MemoryStream pdfStream, string fileName)> GenerateAndDownloadInvoice(int kitchenIssueId)
    {
        try
        {
			// Load saved transaction details
			var transaction = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssueId) ??
                throw new InvalidOperationException("Transaction not found.");

            // Load transaction details from database
            var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, transaction.Id);
            if (transactionDetails is null || transactionDetails.Count == 0)
                throw new InvalidOperationException("No transaction details found for the transaction.");

            // Load company and kitchen
            var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
            var kitchen = await CommonData.LoadTableDataById<LocationModel>(TableNames.Kitchen, transaction.KitchenId);
            if (company is null || kitchen is null)
                throw new InvalidOperationException("Company or kitchen details not found.");

            // Convert kitchen issue details to cart items with item names
            var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
            var cartItems = new List<KitchenIssueItemCartModel>();
            foreach (var detail in transactionDetails)
            {
                var rawMaterial = rawMaterials.FirstOrDefault(rm => rm.Id == detail.RawMaterialId);
                cartItems.Add(new KitchenIssueItemCartModel
                {
                    ItemId = detail.RawMaterialId,
                    ItemName = rawMaterial?.Name ?? "Unknown Item",
                    Quantity = detail.Quantity,
                    UnitOfMeasurement = detail.UnitOfMeasurement,
                    Rate = detail.Rate,
                    Total = detail.Total,
                    Remarks = detail.Remarks
                });
            }

            // Generate invoice PDF
            var pdfStream = await KitchenIssueInvoicePDFExport.ExportKitchenIssueInvoiceWithItems(
                    transaction,
                    cartItems,
                    company,
                    kitchen,
                    null, // logo path - uses default
                    "KITCHEN ISSUE INVOICE"
                );

            // Generate file name
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"KITCHEN_ISSUE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
            return (pdfStream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate and download invoice." + ex.Message);
        }
    }

    public static async Task<(MemoryStream excelStream, string fileName)> GenerateAndDownloadExcelInvoice(int kitchenIssueId)
    {
        try
        {
            // Load saved transaction details
            var transaction = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssueId) ??
                throw new InvalidOperationException("Transaction not found.");

            // Load transaction details from database
            var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, transaction.Id);
            if (transactionDetails is null || transactionDetails.Count == 0)
                throw new InvalidOperationException("No transaction details found for the transaction.");

            // Load company and kitchen
            var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
            var kitchen = await CommonData.LoadTableDataById<LocationModel>(TableNames.Kitchen, transaction.KitchenId);
            if (company is null || kitchen is null)
                throw new InvalidOperationException("Company or kitchen details not found.");

            // Convert kitchen issue details to cart items with item names
            var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
            var cartItems = new List<KitchenIssueItemCartModel>();
            foreach (var detail in transactionDetails)
            {
                var rawMaterial = rawMaterials.FirstOrDefault(rm => rm.Id == detail.RawMaterialId);
                cartItems.Add(new KitchenIssueItemCartModel
                {
                    ItemId = detail.RawMaterialId,
                    ItemName = rawMaterial?.Name ?? "Unknown Item",
                    Quantity = detail.Quantity,
                    UnitOfMeasurement = detail.UnitOfMeasurement,
                    Rate = detail.Rate,
                    Total = detail.Total,
                    Remarks = detail.Remarks
                });
            }

            // Generate invoice Excel
            var excelStream = await KitchenIssueInvoiceExcelExport.ExportKitchenIssueInvoiceWithItems(
                    transaction,
                    cartItems,
                    company,
                    kitchen,
                    null,
                    "KITCHEN ISSUE INVOICE"
                );

            // Generate file name
            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"KITCHEN_ISSUE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
            return (excelStream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate and download Excel invoice." + ex.Message);
        }
    }

    public static async Task DeleteKitchenIssue(int kitchenIssueId)
    {
        var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssueId);
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        if (kitchenIssue is not null)
        {
            kitchenIssue.Status = false;
            await InsertKitchenIssue(kitchenIssue);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.KitchenIssue.ToString(), kitchenIssue.Id);
        }
    }

    public static async Task RecoverKitchenIssueTransaction(KitchenIssueModel kitchenIssue)
    {
        var kitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, kitchenIssue.Id);
        List<KitchenIssueItemCartModel> kitchenIssueItemCarts = [];

        foreach (var item in kitchenIssueDetails)
            kitchenIssueItemCarts.Add(new()
            {
                ItemId = item.RawMaterialId,
                ItemName = "",
                UnitOfMeasurement = item.UnitOfMeasurement,
                Quantity = item.Quantity,
                Rate = item.Rate,
                Total = item.Total,
                Remarks = item.Remarks
            });

        await SaveKitchenIssueTransaction(kitchenIssue, kitchenIssueItemCarts);
    }

    public static async Task<int> SaveKitchenIssueTransaction(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> kitchenIssueDetails)
    {
        bool update = kitchenIssue.Id > 0;

        if (update)
        {
            var existingKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssue.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingKitchenIssue.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || updateFinancialYear.Status == false)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            kitchenIssue.TransactionNo = existingKitchenIssue.TransactionNo;
        }
        else
            kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(kitchenIssue);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
        if (financialYear is null || financialYear.Locked || financialYear.Status == false)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        kitchenIssue.Id = await InsertKitchenIssue(kitchenIssue);
        await SaveKitchenIssueDetail(kitchenIssue, kitchenIssueDetails, update);
        await SaveRawMaterialStock(kitchenIssue, kitchenIssueDetails, update);

        return kitchenIssue.Id;
    }

    private static async Task SaveKitchenIssueDetail(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> kitchenIssueDetails, bool update)
    {
        if (update)
        {
            var existingKitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, kitchenIssue.Id);
            foreach (var item in existingKitchenIssueDetails)
            {
                item.Status = false;
                await InsertKitchenIssueDetail(item);
            }
        }

        foreach (var item in kitchenIssueDetails)
            await InsertKitchenIssueDetail(new()
            {
                Id = 0,
                MasterId = kitchenIssue.Id,
                RawMaterialId = item.ItemId,
                Quantity = item.Quantity,
                UnitOfMeasurement = item.UnitOfMeasurement,
                Rate = item.Rate,
                Total = item.Total,
                Remarks = item.Remarks,
                Status = true
            });
    }

    private static async Task SaveRawMaterialStock(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> cart, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(StockType.KitchenIssue.ToString(), kitchenIssue.Id);

        foreach (var item in cart)
            await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.ItemId,
                Quantity = -item.Quantity,
                NetRate = null,
                Type = StockType.KitchenIssue.ToString(),
                TransactionId = kitchenIssue.Id,
                TransactionNo = kitchenIssue.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(kitchenIssue.TransactionDateTime)
            });
    }
}