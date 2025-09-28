using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Stock;

public static class StockExcelExport
{
	/// <summary>
	/// Exports raw material stock details to Excel
	/// </summary>
	public static MemoryStream ExportRawMaterialStockExcel(
		List<RawMaterialStockDetailModel> stockDetails,
		DateOnly startDate,
		DateOnly endDate)
	{
		if (stockDetails is null || stockDetails.Count == 0)
			throw new ArgumentException("No data to export");

		// Create summary items dictionary with key stock metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Stock Items", stockDetails.Count },
			{ "Opening Stock", stockDetails.Sum(s => s.OpeningStock) },
			{ "Total Purchases", stockDetails.Sum(s => s.PurchaseStock) },
			{ "Total Sales", stockDetails.Sum(s => s.SaleStock) },
			{ "Monthly Stock", stockDetails.Sum(s => s.MonthlyStock) },
			{ "Closing Stock", stockDetails.Sum(s => s.ClosingStock) },
			{ "Stock Movement", stockDetails.Sum(s => s.PurchaseStock + s.SaleStock) },
			{ "Net Stock Change", stockDetails.Sum(s => s.ClosingStock - s.OpeningStock) }
		};

		// Add top categories summary data
		var topCategories = stockDetails
			.GroupBy(s => s.RawMaterialCategoryName)
			.OrderByDescending(g => g.Sum(s => s.ClosingStock))
			.Take(3)
			.ToList();

		foreach (var category in topCategories)
			summaryItems.Add($"Category: {category.Key}", category.Sum(s => s.ClosingStock));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(RawMaterialStockDetailModel.RawMaterialCategoryName),
			nameof(RawMaterialStockDetailModel.RawMaterialCode),
			nameof(RawMaterialStockDetailModel.RawMaterialName),
			nameof(RawMaterialStockDetailModel.OpeningStock),
			nameof(RawMaterialStockDetailModel.PurchaseStock),
			nameof(RawMaterialStockDetailModel.SaleStock),
			nameof(RawMaterialStockDetailModel.MonthlyStock),
			nameof(RawMaterialStockDetailModel.ClosingStock),
			nameof(RawMaterialStockDetailModel.AveragePrice),
			nameof(RawMaterialStockDetailModel.WeightedAverageValue),
			nameof(RawMaterialStockDetailModel.LastPurchasePrice),
			nameof(RawMaterialStockDetailModel.LastPurchaseValue)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(RawMaterialStockDetailModel.RawMaterialCategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(RawMaterialStockDetailModel.RawMaterialCode)] = new()
			{
				DisplayName = "Item Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(RawMaterialStockDetailModel.RawMaterialName)] = new()
			{
				DisplayName = "Item Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(RawMaterialStockDetailModel.OpeningStock)] = new()
			{
				DisplayName = "Opening Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue <= 0,
						FontColor = stockValue <= 0 ? Syncfusion.Drawing.Color.FromArgb(198, 40, 40) : null
					};
				}
			},
			[nameof(RawMaterialStockDetailModel.PurchaseStock)] = new()
			{
				DisplayName = "Purchases",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(RawMaterialStockDetailModel.SaleStock)] = new()
			{
				DisplayName = "Sales",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(239, 108, 0) : null
					};
				}
			},
			[nameof(RawMaterialStockDetailModel.MonthlyStock)] = new()
			{
				DisplayName = "Monthly Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockDetailModel.ClosingStock)] = new()
			{
				DisplayName = "Closing Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				HighlightNegative = true,
				FormatCallback = (value) =>
				{
					if (value is null) return null;

					var stockValue = Convert.ToDecimal(value);
					if (stockValue <= 0)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40)
						};
					}
					else if (stockValue <= 5)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0)
						};
					}

					return null;
				}
			},
			[nameof(RawMaterialStockDetailModel.AveragePrice)] = new()
			{
				DisplayName = "Average Price",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockDetailModel.WeightedAverageValue)] = new()
			{
				DisplayName = "Weighted Avg Value",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockDetailModel.LastPurchasePrice)] = new()
			{
				DisplayName = "Last Purchase Price",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockDetailModel.LastPurchaseValue)] = new()
			{
				DisplayName = "Last Purchase Value",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			}
		};

		// Generate title based on location if selected
		string reportTitle = "Raw Material Stock Report";
		string worksheetName = "Stock Details";

		return ExcelExportUtil.ExportToExcel(
			stockDetails,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}

	/// <summary>
	/// Exports finished product stock details to Excel
	/// </summary>
	public static MemoryStream ExportFinishedProductStockExcel(
		List<ProductStockDetailModel> stockDetails,
		DateOnly startDate,
		DateOnly endDate,
		int selectedLocationId = 0,
		List<LocationModel> locations = null)
	{
		if (stockDetails is null || stockDetails.Count == 0)
			throw new ArgumentException("No data to export");

		// Create summary items dictionary with key stock metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Stock Items", stockDetails.Count },
			{ "Opening Stock", stockDetails.Sum(s => s.OpeningStock) },
			{ "Total Production", stockDetails.Sum(s => s.PurchaseStock) },
			{ "Total Sales", stockDetails.Sum(s => s.SaleStock) },
			{ "Monthly Stock", stockDetails.Sum(s => s.MonthlyStock) },
			{ "Closing Stock", stockDetails.Sum(s => s.ClosingStock) },
			{ "Stock Movement", stockDetails.Sum(s => s.PurchaseStock + s.SaleStock) },
			{ "Net Stock Change", stockDetails.Sum(s => s.ClosingStock - s.OpeningStock) }
		};

		// Add location filter info if specific location is selected
		if (selectedLocationId > 0 && locations is not null)
		{
			var locationName = locations.FirstOrDefault(l => l.Id == selectedLocationId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Location", locationName);
		}

		// Add top categories summary data
		var topCategories = stockDetails
			.GroupBy(s => s.ProductCategoryName)
			.OrderByDescending(g => g.Sum(s => s.ClosingStock))
			.Take(3)
			.ToList();

		foreach (var category in topCategories)
			summaryItems.Add($"Category: {category.Key}", category.Sum(s => s.ClosingStock));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(ProductStockDetailModel.ProductCategoryName),
			nameof(ProductStockDetailModel.ProductCode),
			nameof(ProductStockDetailModel.ProductName),
			nameof(ProductStockDetailModel.OpeningStock),
			nameof(ProductStockDetailModel.PurchaseStock),
			nameof(ProductStockDetailModel.SaleStock),
			nameof(ProductStockDetailModel.MonthlyStock),
			nameof(ProductStockDetailModel.ClosingStock),
			nameof(ProductStockDetailModel.AveragePrice),
			nameof(ProductStockDetailModel.WeightedAverageValue),
			nameof(ProductStockDetailModel.LastSalePrice),
			nameof(ProductStockDetailModel.LastSaleValue)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(ProductStockDetailModel.ProductCategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockDetailModel.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockDetailModel.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockDetailModel.OpeningStock)] = new()
			{
				DisplayName = "Opening Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue <= 0,
						FontColor = stockValue <= 0 ? Syncfusion.Drawing.Color.FromArgb(198, 40, 40) : null
					};
				}
			},
			[nameof(ProductStockDetailModel.PurchaseStock)] = new()
			{
				DisplayName = "Production",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(ProductStockDetailModel.SaleStock)] = new()
			{
				DisplayName = "Sales",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var stockValue = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(239, 108, 0) : null
					};
				}
			},
			[nameof(ProductStockDetailModel.MonthlyStock)] = new()
			{
				DisplayName = "Monthly Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductStockDetailModel.ClosingStock)] = new()
			{
				DisplayName = "Closing Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				HighlightNegative = true,
				FormatCallback = (value) =>
				{
					if (value is null) return null;

					var stockValue = Convert.ToDecimal(value);
					if (stockValue <= 0)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40)
						};
					}
					else if (stockValue <= 5)
					{
						return new ExcelExportUtil.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0)
						};
					}

					return null;
				}
			},
			[nameof(ProductStockDetailModel.AveragePrice)] = new()
			{
				DisplayName = "Average Price",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductStockDetailModel.WeightedAverageValue)] = new()
			{
				DisplayName = "Weighted Avg Value",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductStockDetailModel.LastSalePrice)] = new()
			{
				DisplayName = "Last Sale Price",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductStockDetailModel.LastSaleValue)] = new()
			{
				DisplayName = "Last Sale Value",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			}
		};

		// Generate title based on location if selected
		string reportTitle = "Finished Product Stock Report";

		if (selectedLocationId > 0 && locations is not null)
		{
			var location = locations.FirstOrDefault(l => l.Id == selectedLocationId);
			if (location is not null)
				reportTitle = $"Finished Product Stock Report - {location.Name}";
		}

		string worksheetName = "Product Stock Details";

		return ExcelExportUtil.ExportToExcel(
			stockDetails,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}