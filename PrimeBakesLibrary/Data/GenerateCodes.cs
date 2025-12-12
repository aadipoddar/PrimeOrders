using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Data;

public static class GenerateCodes
{
    public enum CodeType
    {
        Purchase,
        PurchaseReturn,
        KitchenIssue,
        KitchenProduction,
        Order,
        Sale,
        SaleReturn,
        StockTransfer,
		Accounting,
        RawMaterial,
        FinishedProduct,
        Ledger,
    }

    private static async Task<string> CheckDuplicateCode(string code, int numberLength, CodeType type)
    {
        var isDuplicate = true;
        while (isDuplicate)
        {
            switch (type)
            {
                case CodeType.Purchase:
                    var purchase = await CommonData.LoadTableDataByTransactionNo<PurchaseModel>(TableNames.Purchase, code);
                    isDuplicate = purchase is not null;
                    break;
                case CodeType.PurchaseReturn:
                    var purchaseReturn = await CommonData.LoadTableDataByTransactionNo<PurchaseReturnModel>(TableNames.PurchaseReturn, code);
                    isDuplicate = purchaseReturn is not null;
                    break;
                case CodeType.KitchenIssue:
                    var kitchenIssue = await CommonData.LoadTableDataByTransactionNo<KitchenIssueModel>(TableNames.KitchenIssue, code);
                    isDuplicate = kitchenIssue is not null;
                    break;
                case CodeType.KitchenProduction:
                    var kitchenProduction = await CommonData.LoadTableDataByTransactionNo<KitchenProductionModel>(TableNames.KitchenProduction, code);
                    isDuplicate = kitchenProduction is not null;
                    break;
                case CodeType.Sale:
                    var sale = await CommonData.LoadTableDataByTransactionNo<SaleModel>(TableNames.Sale, code);
                    isDuplicate = sale is not null;
                    break;
                case CodeType.SaleReturn:
                    var saleReturn = await CommonData.LoadTableDataByTransactionNo<SaleReturnModel>(TableNames.SaleReturn, code);
                    isDuplicate = saleReturn is not null;
                    break;
                case CodeType.StockTransfer:
                    var stockTransfer = await CommonData.LoadTableDataByTransactionNo<StockTransferModel>(TableNames.StockTransfer, code);
                    isDuplicate = stockTransfer is not null;
                    break;
				case CodeType.Order:
                    var order = await CommonData.LoadTableDataByTransactionNo<OrderModel>(TableNames.Order, code);
                    isDuplicate = order is not null;
                    break;
                case CodeType.Accounting:
                    var accounting = await CommonData.LoadTableDataByTransactionNo<AccountingModel>(TableNames.Accounting, code);
                    isDuplicate = accounting is not null;
                    break;
                case CodeType.RawMaterial:
                    var rawMaterial = await CommonData.LoadTableDataByCode<RawMaterialModel>(TableNames.RawMaterial, code);
                    isDuplicate = rawMaterial is not null;
                    break;
                case CodeType.FinishedProduct:
                    var product = await CommonData.LoadTableDataByCode<ProductModel>(TableNames.Product, code);
                    isDuplicate = product is not null;
                    break;
                case CodeType.Ledger:
                    var ledger = await CommonData.LoadTableDataByCode<LedgerModel>(TableNames.Ledger, code);
                    isDuplicate = ledger is not null;
                    break;
            }

            if (!isDuplicate)
                return code;

            var prefix = code[..(code.Length - numberLength)];
            var lastNumberPart = code[(code.Length - numberLength)..];
            if (int.TryParse(lastNumberPart, out int lastNumber))
            {
                int nextNumber = lastNumber + 1;
                code = $"{prefix}{nextNumber.ToString($"D{numberLength}")}";
            }
            else
                code = $"{prefix}{1.ToString($"D{numberLength}")}";
        }
        return code;
    }

    public static async Task<string> GeneratePurchaseTransactionNo(PurchaseModel purchase)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchase.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1)).PrefixCode;
        var purchasePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseTransactionPrefix)).Value;

        var lastPurchase = await CommonData.LoadLastTableDataByFinancialYear<PurchaseModel>(TableNames.Purchase, purchase.FinancialYearId);
        if (lastPurchase is not null)
        {
            var lastTransactionNo = lastPurchase.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{purchasePrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + purchasePrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchasePrefix}{nextNumber:D6}", 6, CodeType.Purchase);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchasePrefix}000001", 6, CodeType.Purchase);
    }

    public static async Task<string> GeneratePurchaseReturnTransactionNo(PurchaseReturnModel purchaseReturn)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchaseReturn.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1)).PrefixCode;
        var purchaseReturnPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnTransactionPrefix)).Value;

        var lastPurchase = await CommonData.LoadLastTableDataByFinancialYear<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturn.FinancialYearId);
        if (lastPurchase is not null)
        {
            var lastTransactionNo = lastPurchase.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{purchaseReturnPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + purchaseReturnPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchaseReturnPrefix}{nextNumber:D6}", 6, CodeType.PurchaseReturn);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{purchaseReturnPrefix}000001", 6, CodeType.PurchaseReturn);
    }

    public static async Task<string> GenerateProductStockAdjustmentTransactionNo(DateTime transactionDateTime, int locationId)
    {
        var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId)).PrefixCode;
        var adjustmentPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.ProductStockAdjustmentTransactionPrefix)).Value;
        var currentDateTime = await CommonData.LoadCurrentDateTime();

        return $"{locationPrefix}{financialYear.YearNo}{adjustmentPrefix}{currentDateTime:ddMMyy}{currentDateTime:HHmmss}";
    }

    public static async Task<string> GenerateRawMaterialStockAdjustmentTransactionNo(DateTime transactionDateTime)
    {
        var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(transactionDateTime);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1)).PrefixCode;
        var adjustmentPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix)).Value;
        var currentDateTime = await CommonData.LoadCurrentDateTime();

        return $"{locationPrefix}{financialYear.YearNo}{adjustmentPrefix}{currentDateTime:ddMMyy}{currentDateTime:HHmmss}";
    }

    public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel kitchenIssue)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1)).PrefixCode;
        var kitchenIssuePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenIssueTransactionPrefix)).Value;

        var lastKitchenIssue = await CommonData.LoadLastTableDataByFinancialYear<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssue.FinancialYearId);
        if (lastKitchenIssue is not null)
        {
            var lastTransactionNo = lastKitchenIssue.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{kitchenIssuePrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + kitchenIssuePrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenIssuePrefix}{nextNumber:D6}", 6, CodeType.KitchenIssue);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenIssuePrefix}000001", 6, CodeType.KitchenIssue);
    }

    public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel kitchenProduction)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenProduction.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1)).PrefixCode;
        var kitchenProductionPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenProductionTransactionPrefix)).Value;

        var lastKitchenProduction = await CommonData.LoadLastTableDataByFinancialYear<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProduction.FinancialYearId);
        if (lastKitchenProduction is not null)
        {
            var lastTransactionNo = lastKitchenProduction.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{kitchenProductionPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + kitchenProductionPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenProductionPrefix}{nextNumber:D6}", 6, CodeType.KitchenProduction);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{kitchenProductionPrefix}000001", 6, CodeType.KitchenProduction);
    }

    public static async Task<string> GenerateSaleTransactionNo(SaleModel sale)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, sale.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, sale.LocationId)).PrefixCode;
        var salePrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SaleTransactionPrefix)).Value;

        var lastSale = await CommonData.LoadLastTableDataByLocationFinancialYear<SaleModel>(TableNames.Sale, sale.LocationId, sale.FinancialYearId);
        if (lastSale is not null)
        {
            var lastTransactionNo = lastSale.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{salePrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + salePrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{salePrefix}{nextNumber:D6}", 6, CodeType.Sale);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{salePrefix}000001", 6, CodeType.Sale);
    }

    public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel saleReturn)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, saleReturn.LocationId)).PrefixCode;
        var saleReturnPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnTransactionPrefix)).Value;

        var lastSaleReturn = await CommonData.LoadLastTableDataByLocationFinancialYear<SaleReturnModel>(TableNames.SaleReturn, saleReturn.LocationId, saleReturn.FinancialYearId);
        if (lastSaleReturn is not null)
        {
            var lastTransactionNo = lastSaleReturn.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{saleReturnPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + saleReturnPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{saleReturnPrefix}{nextNumber:D6}", 6, CodeType.SaleReturn);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{saleReturnPrefix}000001", 6, CodeType.SaleReturn);
    }

	public static async Task<string> GenerateStockTransferTransactionNo(StockTransferModel stockTransfer)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, stockTransfer.FinancialYearId);
		var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, stockTransfer.LocationId)).PrefixCode;
		var stockTransferPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferTransactionPrefix)).Value;

		var lastStockTransfer = await CommonData.LoadLastTableDataByLocationFinancialYear<StockTransferModel>(TableNames.StockTransfer, stockTransfer.LocationId, stockTransfer.FinancialYearId);
		if (lastStockTransfer is not null)
		{
			var lastTransactionNo = lastStockTransfer.TransactionNo;
			if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{stockTransferPrefix}"))
			{
				var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + stockTransferPrefix.Length)..];
				if (int.TryParse(lastNumberPart, out int lastNumber))
				{
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{stockTransferPrefix}{nextNumber:D6}", 6, CodeType.StockTransfer);
				}
			}
		}

		return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{stockTransferPrefix}000001", 6, CodeType.StockTransfer);
	}

	public static async Task<string> GenerateOrderTransactionNo(OrderModel order)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, order.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, order.LocationId)).PrefixCode;
        var orderPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.OrderTransactionPrefix)).Value;

        var lastOrder = await CommonData.LoadLastTableDataByLocationFinancialYear<OrderModel>(TableNames.Order, order.LocationId, order.FinancialYearId);
        if (lastOrder is not null)
        {
            var lastTransactionNo = lastOrder.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{orderPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + orderPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{orderPrefix}{nextNumber:D6}", 6, CodeType.Order);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{orderPrefix}000001", 6, CodeType.Order);
    }

    public static async Task<string> GenerateAccountingTransactionNo(AccountingModel accounting)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, accounting.FinancialYearId);
        var locationPrefix = (await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, 1)).PrefixCode;
        var accountingPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.AccountingTransactionPrefix)).Value;

        var lastAccounting = await CommonData.LoadLastTableDataByFinancialYear<AccountingModel>(TableNames.Accounting, accounting.FinancialYearId);
        if (lastAccounting is not null)
        {
            var lastTransactionNo = lastAccounting.TransactionNo;
            if (lastTransactionNo.StartsWith($"{locationPrefix}{financialYear.YearNo}{accountingPrefix}"))
            {
                var lastNumberPart = lastTransactionNo[(locationPrefix.Length + financialYear.YearNo.ToString().Length + accountingPrefix.Length)..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{accountingPrefix}{nextNumber:D6}", 6, CodeType.Accounting);
                }
            }
        }

        return await CheckDuplicateCode($"{locationPrefix}{financialYear.YearNo}{accountingPrefix}000001", 6, CodeType.Accounting);
    }

    public static async Task<string> GenerateRawMaterialCode()
    {
        var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
        var rawMaterialPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialCodePrefix)).Value;

        var lastRawMaterial = rawMaterials.OrderByDescending(r => r.Id).FirstOrDefault();
        if (lastRawMaterial is not null)
        {
            var lastRawMaterialCode = lastRawMaterial.Code;
            if (lastRawMaterialCode.StartsWith(rawMaterialPrefix))
            {
                var lastNumberPart = lastRawMaterialCode[rawMaterialPrefix.Length..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{rawMaterialPrefix}{nextNumber:D4}", 4, CodeType.RawMaterial);
                }
            }
        }

        return await CheckDuplicateCode($"{rawMaterialPrefix}0001", 4, CodeType.RawMaterial);
    }

    public static async Task<string> GenerateProductCode()
    {
        var products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
        var productPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.FinishedProductCodePrefix)).Value;

        var lastProduct = products.OrderByDescending(p => p.Id).FirstOrDefault();
        if (lastProduct is not null)
        {
            var lastProductCode = lastProduct.Code;
            if (lastProductCode.StartsWith(productPrefix))
            {
                var lastNumberPart = lastProductCode[productPrefix.Length..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{productPrefix}{nextNumber:D4}", 4, CodeType.FinishedProduct);
                }
            }
        }

        return await CheckDuplicateCode($"{productPrefix}0001", 4, CodeType.FinishedProduct);
    }

    public static async Task<string> GenerateLedgerCode()
    {
        var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
        var ledgerPrefix = (await SettingsData.LoadSettingsByKey(SettingsKeys.LedgerCodePrefix)).Value;

        var lastLedger = ledgers.OrderByDescending(l => l.Id).FirstOrDefault();
        if (lastLedger is not null)
        {
            var lastLedgerCode = lastLedger.Code;
            if (lastLedgerCode.StartsWith(ledgerPrefix))
            {
                var lastNumberPart = lastLedgerCode[ledgerPrefix.Length..];
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    int nextNumber = lastNumber + 1;
                    return await CheckDuplicateCode($"{ledgerPrefix}{nextNumber:D5}", 5, CodeType.Ledger);
                }
            }
        }

        return await CheckDuplicateCode($"{ledgerPrefix}00001", 5, CodeType.Ledger);
    }
}
