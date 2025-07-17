using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Exporting;
using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Exporting.Kitchen;

public static class KitchenProductionExcelExport
{
	public static async Task<MemoryStream> ExportKitchenProductionOverviewExcel(
		List<KitchenProductionOverviewModel> kitchenProductionOverviews,
		DateOnly startDate,
		DateOnly endDate,
		int selectedKitchenId)
	{
		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Transactions", kitchenProductionOverviews.Count },
			{ "Total Products", kitchenProductionOverviews.Sum(_ => _.TotalProducts) },
			{ "Total Quantity", kitchenProductionOverviews.Sum(_ => _.TotalQuantity) },
			{ "Kitchens Active", kitchenProductionOverviews.Select(_ => _.KitchenId).Distinct().Count() },
			{ "Average Items per Transaction", kitchenProductionOverviews.Count > 0 ? kitchenProductionOverviews.Average(_ => _.TotalProducts) : 0 },
			{ "Average Quantity per Transaction", kitchenProductionOverviews.Count > 0 ? kitchenProductionOverviews.Average(_ => _.TotalQuantity) : 0 }
		};

		// Add kitchen filter info if specific kitchen is selected
		if (selectedKitchenId > 0)
		{
			var kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen, true);
			var kitchenName = kitchens.FirstOrDefault(k => k.Id == selectedKitchenId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Kitchen", kitchenName);
		}

		// Add top kitchens summary data
		var topKitchens = kitchenProductionOverviews
			.GroupBy(i => i.KitchenName)
			.OrderByDescending(g => g.Sum(x => x.TotalQuantity))
			.Take(3)
			.ToList();

		foreach (var kitchen in topKitchens)
			summaryItems.Add($"Kitchen: {kitchen.Key}", kitchen.Sum(k => k.TotalQuantity));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(KitchenProductionOverviewModel.TransactionNo),
			nameof(KitchenProductionOverviewModel.ProductionDate),
			nameof(KitchenProductionOverviewModel.KitchenName),
			nameof(KitchenProductionOverviewModel.UserName),
			nameof(KitchenProductionOverviewModel.TotalProducts),
			nameof(KitchenProductionOverviewModel.TotalQuantity)
		];

		// Define custom column settings with professional styling
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(KitchenProductionOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenProductionOverviewModel.ProductionDate)] = new()
			{
				DisplayName = "Production Date",
				Format = "dd-MMM-yyyy",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenProductionOverviewModel.KitchenName)] = new()
			{
				DisplayName = "Kitchen",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenProductionOverviewModel.UserName)] = new()
			{
				DisplayName = "Produced By",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenProductionOverviewModel.TotalProducts)] = new()
			{
				DisplayName = "Products",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var productCount = Convert.ToInt32(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = productCount > 5,
						FontColor = productCount > 10 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(KitchenProductionOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Total Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var qtyValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = qtyValue > 50,
						FontColor = qtyValue > 100 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) :
								   qtyValue > 50 ? Syncfusion.Drawing.Color.FromArgb(255, 165, 0) : null
					};
				}
			}
		};

		// Generate title based on kitchen selection if applicable
		string reportTitle = "Kitchen Production Report";

		if (selectedKitchenId > 0)
		{
			var kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen, true);
			var kitchen = kitchens.FirstOrDefault(k => k.Id == selectedKitchenId);
			if (kitchen != null)
				reportTitle = $"Kitchen Production Report - {kitchen.Name}";
		}

		string worksheetName = "Kitchen Productions";

		return ExcelExportUtil.ExportToExcel(
			kitchenProductionOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}