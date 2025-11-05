using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Kitchen;

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
			nameof(KitchenProductionOverviewModel.TotalQuantity),
			nameof(KitchenProductionOverviewModel.TotalAmount),
			nameof(KitchenProductionOverviewModel.Remarks),
			nameof(KitchenProductionOverviewModel.CreatedAt)
		];

		// Define custom column settings with professional styling
		Dictionary<string, ExcelExportUtilOld.ColumnSetting> columnSettings = new()
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
				Format = "dd-MMM-yyyy HH:mm",
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
					return new ExcelExportUtilOld.FormatInfo
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
					return new ExcelExportUtilOld.FormatInfo
					{
						Bold = qtyValue > 50,
						FontColor = qtyValue > 100 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) :
								   qtyValue > 50 ? Syncfusion.Drawing.Color.FromArgb(255, 165, 0) : null
					};
				}
			},
			[nameof(KitchenProductionOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Total Amount",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var amountValue = Convert.ToDecimal(value);
					return new ExcelExportUtilOld.FormatInfo
					{
						Bold = amountValue > 1000,
						FontColor = amountValue > 5000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) :
								   amountValue > 1000 ? Syncfusion.Drawing.Color.FromArgb(255, 165, 0) : null
					};
				}
			},
			[nameof(KitchenProductionOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenProductionOverviewModel.CreatedAt)] = new()
			{
				DisplayName = "Created At",
				Format = "dd-MMM-yyyy HH:mm",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
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

		return ExcelExportUtilOld.ExportToExcel(
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