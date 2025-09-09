using System.Text;

using PrimeOrdersLibrary.Data.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Data.Accounts.Masters;
using PrimeOrdersLibrary.Data.Inventory.Kitchen;
using PrimeOrdersLibrary.Data.Order;
using PrimeOrdersLibrary.Data.Sale;
using PrimeOrdersLibrary.Models.Accounts.Masters;
using PrimeOrdersLibrary.Models.Inventory;
using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Data.Common;

public static class GenerateCodes
{
	private static string GeneratePhoneticCode(string name, int maxLength = 4)
	{
		if (string.IsNullOrWhiteSpace(name))
			return string.Empty;

		// Clean and normalize the input
		var cleanName = new string(name.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray())
			.ToUpperInvariant()
			.Trim();

		if (string.IsNullOrEmpty(cleanName))
			return string.Empty;

		var words = cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		// For single words, use consonant-vowel pattern
		if (words.Length == 1)
			return GenerateConsonantCode(words[0], maxLength);

		// For multiple words, take first 2 letters of each significant word
		var result = new StringBuilder();
		foreach (var word in words.Take(maxLength / 2))
		{
			if (word.Length >= 2)
				result.Append(word[..2]);
			else
				result.Append(word);
		}

		return result.ToString()[..Math.Min(result.Length, maxLength)];
	}

	private static string GenerateConsonantCode(string word, int maxLength)
	{
		var consonants = new StringBuilder();
		var vowels = new StringBuilder();
		var vowelSet = new HashSet<char> { 'A', 'E', 'I', 'O', 'U' };

		// Separate consonants and vowels
		foreach (char c in word)
		{
			if (vowelSet.Contains(c))
				vowels.Append(c);
			else
				consonants.Append(c);
		}

		// Prefer consonants, then add vowels if needed
		var result = consonants.ToString();
		if (result.Length < maxLength && vowels.Length > 0)
		{
			result += vowels.ToString()[..(maxLength - result.Length)];
		}

		return result[..Math.Min(result.Length, maxLength)];
	}

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

	public static async Task<string> GenerateOrderBillNo(OrderModel order)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, order.LocationId);
		var prefix = GeneratePhoneticCode(location.Name);
		var year = $"{order.OrderDateTime:yy}";
		if (order.OrderDateTime.Month <= 3)
			year = $"{order.OrderDateTime.AddYears(-1):yy}";

		var lastOrder = await OrderData.LoadLastOrderByLocation(order.LocationId);
		if (lastOrder is not null)
		{
			var lastOrderNo = lastOrder.OrderNo;
			if (lastOrderNo.StartsWith(prefix))
			{
				var lastYear = lastOrderNo.Substring(prefix.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastOrderNo[(prefix.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}{year}OD{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}{year}OD000001";
	}

	public static async Task<string> GenerateSaleBillNo(SaleModel sale)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, sale.LocationId);
		var prefix = GeneratePhoneticCode(location.Name);
		var year = $"{sale.SaleDateTime:yy}";
		if (sale.SaleDateTime.Month <= 3)
			year = $"{sale.SaleDateTime.AddYears(-1):yy}";

		var lastSale = await SaleData.LoadLastSaleByLocation(sale.LocationId);
		if (lastSale is not null)
		{
			var lastSaleNo = lastSale.BillNo;
			if (lastSaleNo.StartsWith(prefix))
			{
				var lastYear = lastSaleNo.Substring(prefix.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(prefix.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}{year}SL{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}{year}SL000001";
	}

	public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel saleReturn)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, saleReturn.LocationId);
		var prefix = GeneratePhoneticCode(location.Name);
		var year = $"{saleReturn.ReturnDateTime:yy}";
		if (saleReturn.ReturnDateTime.Month <= 3)
			year = $"{saleReturn.ReturnDateTime.AddYears(-1):yy}";

		var lastSaleReturn = await SaleReturnData.LoadLastSaleReturnByLocation(saleReturn.LocationId);
		if (lastSaleReturn is not null)
		{
			var lastTransactionNo = lastSaleReturn.TransactionNo;
			if (lastTransactionNo.StartsWith(prefix))
			{
				var lastYear = lastTransactionNo.Substring(prefix.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastTransactionNo[(prefix.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}{year}SR{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}{year}SR000001";
	}

	public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel kitchenIssue)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, kitchenIssue.LocationId);
		var prefix = GeneratePhoneticCode(location.Name);
		var year = $"{kitchenIssue.IssueDate:yy}";
		if (kitchenIssue.IssueDate.Month <= 3)
			year = $"{kitchenIssue.IssueDate.AddYears(-1):yy}";

		var lastIssue = await KitchenIssueData.LoadLastKitchenIssueByLocation(kitchenIssue.LocationId);
		if (lastIssue is not null)
		{
			var lastSaleNo = lastIssue.TransactionNo;
			if (lastSaleNo.StartsWith(prefix))
			{
				var lastYear = lastSaleNo.Substring(prefix.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(prefix.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}{year}RM{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}{year}RM000001";
	}

	public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel kitchenProduction)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, kitchenProduction.LocationId);
		var prefix = GeneratePhoneticCode(location.Name);
		var year = $"{kitchenProduction.ProductionDate:yy}";
		if (kitchenProduction.ProductionDate.Month <= 3)
			year = $"{kitchenProduction.ProductionDate.AddYears(-1):yy}";

		var lastProduction = await KitchenProductionData.LoadLastKitchenProductionByLocation(kitchenProduction.LocationId);
		if (lastProduction is not null)
		{
			var lastSaleNo = lastProduction.TransactionNo;
			if (lastSaleNo.StartsWith(prefix))
			{
				var lastYear = lastSaleNo.Substring(prefix.Length, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(prefix.Length + 4)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}{year}FP{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}{year}FP000001";
	}

	public static async Task<string> GenerateAccountingReferenceNo(int voucherId, DateOnly accountingDate)
	{
		var voucher = await CommonData.LoadTableDataById<VoucherModel>(TableNames.Voucher, voucherId);
		var financialYear = await FinancialYearData.LoadFinancialYearByDate(accountingDate);
		var lastAccounting = await AccountingData.LoadLastAccountingByFinancialYearVoucher(financialYear.Id, voucherId);

		var voucherPrefix = GeneratePhoneticCode(voucher.Name);
		var year = financialYear.YearNo;

		if (lastAccounting is not null)
		{
			var lastReferenceNo = lastAccounting.ReferenceNo;
			if (lastReferenceNo.StartsWith($"FA{year}{voucherPrefix}"))
			{
				var lastNumber = int.Parse(lastReferenceNo[(2 + year.GetNumberOfDigits() + voucherPrefix.Length)..]);
				int nextNumber = lastNumber + 1;
				return $"FA{year}{voucherPrefix}{nextNumber:D6}";
			}
		}

		return $"FA{year}{voucherPrefix}000001";
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
