using PrimeOrdersLibrary.Data.Inventory;
using PrimeOrdersLibrary.Data.Order;
using PrimeOrdersLibrary.Data.Sale;
using PrimeOrdersLibrary.Models.Inventory;
using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Data.Common;

public static class GenerateBillNo
{
	private static async Task<string> GetLocationPrefix(int locationId)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId);
		if (location is null)
			return string.Empty;

		string prefix = string.Empty;
		string[] words = location.Name.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var word in words)
			if (word.Length > 1)
			{
				var firstLetter = word[0];

				if (char.IsLetter(firstLetter) && char.IsUpper(firstLetter))
					prefix += char.ToUpper(firstLetter);
			}

		return prefix;
	}

	public static async Task<string> GenerateOrderBillNo(OrderModel order)
	{
		var prefix = await GetLocationPrefix(order.LocationId);
		var year = $"{order.OrderDate:yy}";
		if (order.OrderDate.Month <= 3)
			year = $"{order.OrderDate.AddYears(-1):yy}";

		var lastOrder = await OrderData.LoadLastOrderByLocation(order.LocationId);
		if (lastOrder is not null)
		{
			var lastOrderNo = lastOrder.OrderNo;
			if (lastOrderNo.StartsWith(prefix))
			{
				var lastYear = lastOrderNo.Substring(prefix.Length + 1, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastOrderNo[(prefix.Length + 3)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}O{year}{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}O{year}000001";
	}

	public static async Task<string> GenerateSaleBillNo(SaleModel sale)
	{
		var prefix = await GetLocationPrefix(sale.LocationId);
		var year = $"{sale.SaleDateTime:yy}";
		if (sale.SaleDateTime.Month <= 3)
			year = $"{sale.SaleDateTime.AddYears(-1):yy}";

		var lastSale = await SaleData.LoadLastSaleByLocation(sale.LocationId);
		if (lastSale is not null)
		{
			var lastSaleNo = lastSale.BillNo;
			if (lastSaleNo.StartsWith(prefix))
			{
				var lastYear = lastSaleNo.Substring(prefix.Length + 1, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(prefix.Length + 3)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}S{year}{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}S{year}000001";
	}

	public static async Task<string> GenerateSaleReturnTransactionNo(SaleReturnModel saleReturn)
	{
		var prefix = await GetLocationPrefix(saleReturn.LocationId);
		var year = $"{saleReturn.ReturnDateTime:yy}";
		if (saleReturn.ReturnDateTime.Month <= 3)
			year = $"{saleReturn.ReturnDateTime.AddYears(-1):yy}";

		var lastSaleReturn = await SaleReturnData.LoadLastSaleReturnByLocation(saleReturn.LocationId);
		if (lastSaleReturn is not null)
		{
			var lastTransactionNo = lastSaleReturn.TransactionNo;
			if (lastTransactionNo.StartsWith(prefix))
			{
				var lastYear = lastTransactionNo.Substring(prefix.Length + 2, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastTransactionNo[(prefix.Length + 3)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}SR{year}{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}SR{year}000001";
	}

	public static async Task<string> GenerateKitchenIssueTransactionNo(KitchenIssueModel kitchenIssue)
	{
		var prefix = await GetLocationPrefix(kitchenIssue.LocationId);
		var year = $"{kitchenIssue.IssueDate:yy}";
		if (kitchenIssue.IssueDate.Month <= 3)
			year = $"{kitchenIssue.IssueDate.AddYears(-1):yy}";

		var lastIssue = await KitchenIssueData.LoadLastKitchenIssueByLocation(kitchenIssue.LocationId);
		if (lastIssue is not null)
		{
			var lastSaleNo = lastIssue.TransactionNo;
			if (lastSaleNo.StartsWith(prefix))
			{
				var lastYear = lastSaleNo.Substring(prefix.Length + 2, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(prefix.Length + 3)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}RM{year}{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}RM{year}000001";
	}

	public static async Task<string> GenerateKitchenProductionTransactionNo(KitchenProductionModel kitchenProduction)
	{
		var prefix = await GetLocationPrefix(kitchenProduction.LocationId);
		var year = $"{kitchenProduction.ProductionDate:yy}";
		if (kitchenProduction.ProductionDate.Month <= 3)
			year = $"{kitchenProduction.ProductionDate.AddYears(-1):yy}";

		var lastProduction = await KitchenProductionData.LoadLastKitchenProductionByLocation(kitchenProduction.LocationId);
		if (lastProduction is not null)
		{
			var lastSaleNo = lastProduction.TransactionNo;
			if (lastSaleNo.StartsWith(prefix))
			{
				var lastYear = lastSaleNo.Substring(prefix.Length + 2, 2);
				if (lastYear == year)
				{
					int lastNumber = int.Parse(lastSaleNo[(prefix.Length + 3)..]);
					int nextNumber = lastNumber + 1;
					return $"{prefix}FP{year}{nextNumber:D6}";
				}
			}
		}

		return $"{prefix}FP{year}000001";
	}

	public static async Task<string> GenerateRawMaterialCode()
	{
		return "";
	}
}
