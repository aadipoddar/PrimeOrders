using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Kitchen;

public static class KitchenIssueExcelExport
{
	public static async Task<MemoryStream> ExportKitchenIssueOverviewExcel(
		List<KitchenIssueOverviewModel> kitchenIssueOverviews,
		DateOnly startDate,
		DateOnly endDate,
		int selectedKitchenId)
	{
		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Transactions", kitchenIssueOverviews.Count },
			{ "Total Products", kitchenIssueOverviews.Sum(_ => _.TotalProducts) },
			{ "Total Quantity", kitchenIssueOverviews.Sum(_ => _.TotalQuantity) },
			{ "Total Amount", kitchenIssueOverviews.Sum(_ => _.TotalAmount) },
			{ "Average Items per Transaction", kitchenIssueOverviews.Count > 0 ? kitchenIssueOverviews.Average(_ => _.TotalProducts) : 0 },
			{ "Average Quantity per Transaction", kitchenIssueOverviews.Count > 0 ? kitchenIssueOverviews.Average(_ => _.TotalQuantity) : 0 }
		};

		// Add kitchen filter info if specific kitchen is selected
		if (selectedKitchenId > 0)
		{
			var kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen, true);
			var kitchenName = kitchens.FirstOrDefault(k => k.Id == selectedKitchenId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Kitchen", kitchenName);
		}

		// Add top kitchens summary data
		var topKitchens = kitchenIssueOverviews
			.GroupBy(i => i.KitchenName)
			.OrderByDescending(g => g.Sum(x => x.TotalQuantity))
			.Take(3)
			.ToList();

		foreach (var kitchen in topKitchens)
			summaryItems.Add($"Kitchen: {kitchen.Key}", kitchen.Sum(k => k.TotalQuantity));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(KitchenIssueOverviewModel.TransactionNo),
			nameof(KitchenIssueOverviewModel.IssueDate),
			nameof(KitchenIssueOverviewModel.KitchenName),
			nameof(KitchenIssueOverviewModel.UserName),
			nameof(KitchenIssueOverviewModel.TotalProducts),
			nameof(KitchenIssueOverviewModel.TotalQuantity),
			nameof(KitchenIssueOverviewModel.TotalAmount),
			nameof(KitchenIssueOverviewModel.Remarks),
			nameof(KitchenIssueOverviewModel.CreatedAt)
		];

		// Define custom column settings with professional styling
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(KitchenIssueOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenIssueOverviewModel.IssueDate)] = new()
			{
				DisplayName = "Issue Date",
				Format = "dd-MMM-yyyy HH:mm",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(KitchenIssueOverviewModel.KitchenName)] = new()
			{
				DisplayName = "Kitchen",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenIssueOverviewModel.UserName)] = new()
			{
				DisplayName = "Issued By",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenIssueOverviewModel.TotalProducts)] = new()
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
			[nameof(KitchenIssueOverviewModel.TotalQuantity)] = new()
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
			},
			[nameof(KitchenIssueOverviewModel.TotalAmount)] = new()
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
					return new ExcelExportUtil.FormatInfo
					{
						Bold = amountValue > 1000,
						FontColor = amountValue > 5000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) :
								   amountValue > 1000 ? Syncfusion.Drawing.Color.FromArgb(255, 165, 0) : null
					};
				}
			},
			[nameof(KitchenIssueOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(KitchenIssueOverviewModel.CreatedAt)] = new()
			{
				DisplayName = "Created Date",
				Format = "dd-MMM-yyyy HH:mm",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			}
		};

		// Generate title based on kitchen selection if applicable
		string reportTitle = "Kitchen Issue Report";

		if (selectedKitchenId > 0)
		{
			var kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen, true);
			var kitchen = kitchens.FirstOrDefault(k => k.Id == selectedKitchenId);
			if (kitchen != null)
				reportTitle = $"Kitchen Issue Report - {kitchen.Name}";
		}

		string worksheetName = "Kitchen Issues";

		return ExcelExportUtil.ExportToExcel(
			kitchenIssueOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}