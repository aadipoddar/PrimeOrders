using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakesLibrary.Data.Common;

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
		Order,
		Sale,
		SaleReturn,
		KitchenIssue,
		KitchenProduction,
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
				case CodeType.Sale:
					var sale = await SaleData.LoadSaleByBillNo(code);
					isDuplicate = sale is not null;
					break;
				case CodeType.SaleReturn:
					var saleReturn = await SaleReturnData.LoadSaleReturnByTransactionNo(code);
					isDuplicate = saleReturn is not null;
					break;
				case CodeType.KitchenIssue:
					var kitchenIssue = await KitchenIssueData.LoadKitchenIssueByTransactionNo(code);
					isDuplicate = kitchenIssue is not null;
					break;
				case CodeType.KitchenProduction:
					var kitchenProduction = await KitchenProductionData.LoadKitchenProductionByTransactionNo(code);
					isDuplicate = kitchenProduction is not null;
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

	public static async Task<string> GenerateSaleBillNo(SaleModel sale)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, sale.LocationId);
		var year = $"{sale.SaleDateTime:yy}";
		if (sale.SaleDateTime.Month <= 3)
			year = $"{sale.SaleDateTime.AddYears(-1):yy}";

		var lastSale = await SaleData.LoadLastSaleByLocation(sale.LocationId);
		if (lastSale is not null)
		{
			var lastSaleNo = lastSale.BillNo;
			if (lastSaleNo.StartsWith(location.PrefixCode))
			{
				var lastYear = lastSaleNo.Substring(location.PrefixCode.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(location.PrefixCode.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{location.PrefixCode}{year}SL{nextNumber:D6}", CodeType.Sale);
				}
			}
		}

		return await CheckDuplicateCode($"{location.PrefixCode}{year}SL000001", CodeType.Sale);
	}

	public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel saleReturn)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, saleReturn.LocationId);
		var year = $"{saleReturn.ReturnDateTime:yy}";
		if (saleReturn.ReturnDateTime.Month <= 3)
			year = $"{saleReturn.ReturnDateTime.AddYears(-1):yy}";

		var lastSaleReturn = await SaleReturnData.LoadLastSaleReturnByLocation(saleReturn.LocationId);
		if (lastSaleReturn is not null)
		{
			var lastTransactionNo = lastSaleReturn.TransactionNo;
			if (lastTransactionNo.StartsWith(location.PrefixCode))
			{
				var lastYear = lastTransactionNo.Substring(location.PrefixCode.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastTransactionNo[(location.PrefixCode.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{location.PrefixCode}{year}SR{nextNumber:D6}", CodeType.SaleReturn);
				}
			}
		}

		return await CheckDuplicateCode($"{location.PrefixCode}{year}SR000001", CodeType.SaleReturn);
	}

	public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel kitchenIssue)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, kitchenIssue.LocationId);
		var year = $"{kitchenIssue.IssueDate:yy}";
		if (kitchenIssue.IssueDate.Month <= 3)
			year = $"{kitchenIssue.IssueDate.AddYears(-1):yy}";

		var lastIssue = await KitchenIssueData.LoadLastKitchenIssueByLocation(kitchenIssue.LocationId);
		if (lastIssue is not null)
		{
			var lastSaleNo = lastIssue.TransactionNo;
			if (lastSaleNo.StartsWith(location.PrefixCode))
			{
				var lastYear = lastSaleNo.Substring(location.PrefixCode.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(location.PrefixCode.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{location.PrefixCode}{year}RM{nextNumber:D6}", CodeType.KitchenIssue);
				}
			}
		}

		return await CheckDuplicateCode($"{location.PrefixCode}{year}RM000001", CodeType.KitchenIssue);
	}

	public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel kitchenProduction)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, kitchenProduction.LocationId);
		var year = $"{kitchenProduction.ProductionDate:yy}";
		if (kitchenProduction.ProductionDate.Month <= 3)
			year = $"{kitchenProduction.ProductionDate.AddYears(-1):yy}";

		var lastProduction = await KitchenProductionData.LoadLastKitchenProductionByLocation(kitchenProduction.LocationId);
		if (lastProduction is not null)
		{
			var lastSaleNo = lastProduction.TransactionNo;
			if (lastSaleNo.StartsWith(location.PrefixCode))
			{
				var lastYear = lastSaleNo.Substring(location.PrefixCode.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(location.PrefixCode.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return await CheckDuplicateCode($"{location.PrefixCode}{year}FP{nextNumber:D6}", CodeType.KitchenProduction);
				}
			}
		}

		return await CheckDuplicateCode($"{location.PrefixCode}{year}FP000001", CodeType.KitchenProduction);
	}

	public static async Task<string> GenerateAccountingTransactionNo(AccountingModel accounting)
	{
		var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, accounting.VoucherId);
		var financialYear = await FinancialYearData.LoadFinancialYearByDate(accounting.AccountingDate);
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
