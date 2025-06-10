using PrimeOrdersLibrary.Data.Order;
using PrimeOrdersLibrary.Data.Sale;

namespace PrimeOrdersLibrary.Data.Common;

public static class GenerateBillNo
{
	public static async Task<string> GenerateOrderBillNo(int locationId)
	{
		var prefix = await GetLocationPrefix(locationId);
		var year = $"{DateTime.Now:yy}";
		if (DateTime.Now.Month <= 3)
			year = $"{DateTime.Now.AddYears(-1):yy}";

		var lastOrder = await OrderData.LoadLastOrderByLocation(locationId);
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

	public static async Task<string> GenerateSaleBillNo(int locationId)
	{
		var prefix = await GetLocationPrefix(locationId);
		var year = $"{DateTime.Now:yy}";
		if (DateTime.Now.Month <= 3)
			year = $"{DateTime.Now.AddYears(-1):yy}";

		var lastSale = await SaleData.LoadLastSaleByLocation(locationId);
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

	private static async Task<string> GetLocationPrefix(int locationId)
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId);
		if (location is null)
			return string.Empty;

		string suffix = string.Empty;
		string[] words = location.Name.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var word in words)
			if (word.Length > 0)
			{
				var firstLetter = word[0];

				if (char.IsLetter(firstLetter) && char.IsUpper(firstLetter) && word.Length > 1)
					suffix += char.ToUpper(firstLetter);
			}

		return suffix;
	}
}
