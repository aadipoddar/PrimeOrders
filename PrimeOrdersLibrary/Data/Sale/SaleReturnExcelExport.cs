using PrimeOrdersLibrary.Exporting;
using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Data.Sale;

public static class SaleReturnExcelExport
{
	public static MemoryStream ExportSaleReturnOverviewExcel(
		List<SaleReturnOverviewModel> saleReturnOverviews,
		DateOnly startDate,
		DateOnly endDate,
		int selectedLocationId = 0,
		List<LocationModel> locations = null)
	{
		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Returns", saleReturnOverviews.Count },
			{ "Total Products", saleReturnOverviews.Sum(_ => _.TotalProducts) },
			{ "Total Quantity", saleReturnOverviews.Sum(_ => _.TotalQuantity) },
			{ "Locations Active", saleReturnOverviews.Select(_ => _.LocationId).Distinct().Count() },
			{ "Average Products per Return", saleReturnOverviews.Count > 0 ? saleReturnOverviews.Average(_ => _.TotalProducts) : 0 },
			{ "Average Quantity per Return", saleReturnOverviews.Count > 0 ? saleReturnOverviews.Average(_ => _.TotalQuantity) : 0 }
		};

		// Add location filter info if specific location is selected
		if (selectedLocationId > 0 && locations is not null)
		{
			var locationName = locations.FirstOrDefault(l => l.Id == selectedLocationId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Location", locationName);
		}

		// Add top locations summary data
		var topLocations = saleReturnOverviews
			.GroupBy(sr => sr.LocationName)
			.OrderByDescending(g => g.Sum(x => x.TotalQuantity))
			.Take(3)
			.ToList();

		foreach (var location in topLocations)
			summaryItems.Add($"Location: {location.Key}", location.Sum(l => l.TotalQuantity));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(SaleReturnOverviewModel.TransactionNo),
			nameof(SaleReturnOverviewModel.ReturnDateTime),
			nameof(SaleReturnOverviewModel.LocationName),
			nameof(SaleReturnOverviewModel.UserName),
			nameof(SaleReturnOverviewModel.OriginalBillNo),
			nameof(SaleReturnOverviewModel.TotalProducts),
			nameof(SaleReturnOverviewModel.TotalQuantity),
			nameof(SaleReturnOverviewModel.Remarks)
		];

		// Define custom column settings with professional styling
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(SaleReturnOverviewModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(SaleReturnOverviewModel.ReturnDateTime)] = new()
			{
				DisplayName = "Return Date",
				Format = "dd-MMM-yyyy HH:mm",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(SaleReturnOverviewModel.LocationName)] = new()
			{
				DisplayName = "Location",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(SaleReturnOverviewModel.UserName)] = new()
			{
				DisplayName = "Processed By",
				Width = 18,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(SaleReturnOverviewModel.OriginalBillNo)] = new()
			{
				DisplayName = "Original Bill",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(SaleReturnOverviewModel.TotalProducts)] = new()
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
						Bold = productCount > 3,
						FontColor = productCount > 5 ? Syncfusion.Drawing.Color.FromArgb(220, 53, 69) : null
					};
				}
			},
			[nameof(SaleReturnOverviewModel.TotalQuantity)] = new()
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
						Bold = qtyValue > 10,
						FontColor = qtyValue > 20 ? Syncfusion.Drawing.Color.FromArgb(220, 53, 69) :
								   qtyValue > 10 ? Syncfusion.Drawing.Color.FromArgb(255, 165, 0) : null
					};
				}
			},
			[nameof(SaleReturnOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			}
		};

		// Generate title based on location selection if applicable
		string reportTitle = "Sale Return Report";

		if (selectedLocationId > 0 && locations is not null)
		{
			var location = locations.FirstOrDefault(l => l.Id == selectedLocationId);
			if (location is not null)
				reportTitle = $"Sale Return Report - {location.Name}";
		}

		string worksheetName = "Return Details";

		return ExcelExportUtil.ExportToExcel(
			saleReturnOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}