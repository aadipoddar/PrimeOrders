using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data;

public static class GenerateCodes
{
	private static int GetNumberOfDigits(this int number) =>
		number switch
		{
			< 10 => 1,
			< 100 => 2,
			< 1000 => 3,
			< 10000 => 4,
			< 100000 => 5,
			_ => 6
		};

	public enum CodeType
	{
		Purchase,
		PurchaseReturn,
		KitchenIssue,
		KitchenProduction,
		Order,
		Sale,
		SaleReturn,
		Accounting
	}

	private static async Task<string> CheckDuplicateCode(string code, CodeType type)
	{
		var isDuplicate = true;

		while (isDuplicate)
		{
			switch (type)
			{
				case CodeType.Order:
					var item = await OrderData.LoadOrderByOrderNo(code);
					isDuplicate = item is not null;
					break;
				case CodeType.Accounting:
					var accounting = await AccountingData.LoadAccountingByTransactionNo(code);
					isDuplicate = accounting is not null;
					break;
				default:
					isDuplicate = false;
					break;
			}

			if (!isDuplicate)
				return code;

			var prefix = code[..(code.Length - 6)];
			var lastNumberPart = code[(code.Length - 6)..];
			int lastNumber = int.Parse(lastNumberPart);
			int nextNumber = lastNumber + 1;
			code = $"{prefix}{nextNumber:D6}";
		}

		return code;
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




	public static async Task<string> GenerateOrderBillNo(OrderModel order)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, order.LocationId);
		var year = $"{order.OrderDateTime:yy}";
		if (order.OrderDateTime.Month <= 3)
			year = $"{order.OrderDateTime.AddYears(-1):yy}";

		var lastOrder = await OrderData.LoadLastOrderByLocation(order.LocationId);
		if (lastOrder is not null)
		{
			var lastOrderNo = lastOrder.OrderNo;
			if (lastOrderNo.StartsWith(location.PrefixCode))
			{
				var lastYear = lastOrderNo.Substring(location.PrefixCode.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastOrderNo[(location.PrefixCode.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{location.PrefixCode}{year}OD{nextNumber:D6}", CodeType.Order);
				}
			}
		}

		return await CheckDuplicateCode($"{location.PrefixCode}{year}OD000001", CodeType.Order);
	}

	public static async Task<string> GenerateAccountingTransactionNo(AccountingModel accounting)
	{
		var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, accounting.VoucherId);
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(accounting.AccountingDate.ToDateTime(TimeOnly.MinValue));
		var lastAccounting = await AccountingData.LoadLastAccountingByFinancialYearVoucher(financialYear.Id, accounting.VoucherId);
		var year = financialYear.YearNo;

		if (lastAccounting is not null)
		{
			var lastTransactionNo = lastAccounting.TransactionNo;
			if (lastTransactionNo.StartsWith($"FA{year}{voucher.PrefixCode}"))
			{
				var lastNumber = int.Parse(lastTransactionNo[(2 + year.GetNumberOfDigits() + voucher.PrefixCode.Length)..]);
				int nextNumber = lastNumber + 1;
				return await CheckDuplicateCode($"FA{year}{voucher.PrefixCode}{nextNumber:D6}", CodeType.Accounting);
			}
		}

		return await CheckDuplicateCode($"FA{year}{voucher.PrefixCode}000001", CodeType.Accounting);
	}

	public static string GenerateRawMaterialCode(string lastRawMaterialCode)
	{
		if (string.IsNullOrWhiteSpace(lastRawMaterialCode))
			return "RM0001";

		var prefix = "RM";
		var lastNumberPart = lastRawMaterialCode[prefix.Length..];

		if (int.TryParse(lastNumberPart, out int lastNumber))
		{
			int nextNumber = lastNumber + 1;
			return $"{prefix}{nextNumber:D4}";
		}

		return $"{prefix}0001";
	}

	public static string GenerateProductCode(string lastProductCode)
	{
		if (string.IsNullOrWhiteSpace(lastProductCode))
			return "FP0001";

		var prefix = "FP";
		var lastNumberPart = lastProductCode[prefix.Length..];

		if (int.TryParse(lastNumberPart, out int lastNumber))
		{
			int nextNumber = lastNumber + 1;
			return $"{prefix}{nextNumber:D4}";
		}

		return $"{prefix}0001";
	}

	public static string GenerateLedgerCode(string lastLedgerCode)
	{
		if (string.IsNullOrWhiteSpace(lastLedgerCode))
			return "LD00001";

		var prefix = "LD";
		var lastNumberPart = lastLedgerCode[prefix.Length..];

		if (int.TryParse(lastNumberPart, out int lastNumber))
		{
			int nextNumber = lastNumber + 1;
			return $"{prefix}{nextNumber:D5}";
		}

		return $"{prefix}00001";
	}
}
