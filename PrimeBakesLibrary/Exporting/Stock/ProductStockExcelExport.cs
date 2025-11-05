using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Stock;

public static class ProductStockExcelExport
{
	/// <summary>
	/// Exports finished product stock details to Excel
	/// </summary>
	public static MemoryStream ExportFinishedProductStockSummaryExcel(
		List<ProductStockSummaryModel> stockDetails,
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
			nameof(ProductStockSummaryModel.ProductCategoryName),
			nameof(ProductStockSummaryModel.ProductCode),
			nameof(ProductStockSummaryModel.ProductName),
			nameof(ProductStockSummaryModel.OpeningStock),
			nameof(ProductStockSummaryModel.PurchaseStock),
			nameof(ProductStockSummaryModel.SaleStock),
			nameof(ProductStockSummaryModel.ClosingStock),
			nameof(ProductStockSummaryModel.StockValueAtProductRate)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtilOld.ColumnSetting>
		{
			[nameof(ProductStockSummaryModel.ProductCategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockSummaryModel.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockSummaryModel.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockSummaryModel.OpeningStock)] = new()
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
					return new ExcelExportUtilOld.FormatInfo
					{
						Bold = stockValue <= 0,
						FontColor = stockValue <= 0 ? Syncfusion.Drawing.Color.FromArgb(198, 40, 40) : null
					};
				}
			},
			[nameof(ProductStockSummaryModel.PurchaseStock)] = new()
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
					return new ExcelExportUtilOld.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(ProductStockSummaryModel.SaleStock)] = new()
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
					return new ExcelExportUtilOld.FormatInfo
					{
						Bold = stockValue > 0,
						FontColor = stockValue > 0 ? Syncfusion.Drawing.Color.FromArgb(239, 108, 0) : null
					};
				}
			},
			[nameof(ProductStockSummaryModel.ClosingStock)] = new()
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
						return new ExcelExportUtilOld.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40)
						};
					}
					else if (stockValue <= 5)
					{
						return new ExcelExportUtilOld.FormatInfo
						{
							Bold = true,
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0)
						};
					}

					return null;
				}
			},
			[nameof(ProductStockSummaryModel.StockValueAtProductRate)] = new()
			{
				DisplayName = "Stock Value at Product Rate",
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

		return ExcelExportUtilOld.ExportToExcel(
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
	public static MemoryStream ExportFinishedProductStockDetailsExcel(
		List<ProductStockDetailsModel> stockDetails,
		DateOnly startDate,
		DateOnly endDate,
		int selectedLocationId = 0,
		List<LocationModel> locations = null)
	{
		if (stockDetails is null || stockDetails.Count == 0)
			throw new ArgumentException("No data to export");

		// Create summary items dictionary with transaction details
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Transactions", stockDetails.Count },
			{ "Total Quantity", stockDetails.Sum(s => s.Quantity) },
			{ "Production Transactions", stockDetails.Count(s => s.Type == StockType.Purchase.ToString() || s.Type == StockType.KitchenProduction.ToString()) },
			{ "Sale Transactions", stockDetails.Count(s => s.Type == StockType.Sale.ToString()) },
			{ "Sale Return Transactions", stockDetails.Count(s => s.Type == StockType.SaleReturn.ToString()) },
			{ "Purchase Return Transactions", stockDetails.Count(s => s.Type == StockType.PurchaseReturn.ToString()) },
			{ "Adjustment Transactions", stockDetails.Count(s => s.Type == StockType.Adjustment.ToString()) }
		};

		// Add location filter info if specific location is selected
		if (selectedLocationId > 0 && locations is not null)
		{
			var locationName = locations.FirstOrDefault(l => l.Id == selectedLocationId)?.Name ?? "Unknown";
			summaryItems.Add("Filtered by Location", locationName);
		}

		// Add top products by transaction count
		var topProducts = stockDetails
			.GroupBy(s => s.ProductName)
			.OrderByDescending(g => g.Count())
			.Take(3)
			.ToList();

		foreach (var product in topProducts)
			summaryItems.Add($"Product: {product.Key}", product.Count());

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(ProductStockDetailsModel.Id),
			nameof(ProductStockDetailsModel.TransactionDate),
			nameof(ProductStockDetailsModel.TransactionNo),
			nameof(ProductStockDetailsModel.Type),
			nameof(ProductStockDetailsModel.ProductCode),
			nameof(ProductStockDetailsModel.ProductName),
			nameof(ProductStockDetailsModel.Quantity),
			nameof(ProductStockDetailsModel.NetRate)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtilOld.ColumnSetting>
		{
			[nameof(ProductStockDetailsModel.Id)] = new()
			{
				DisplayName = "Id",
				Width = 8,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockDetailsModel.TransactionDate)] = new()
			{
				DisplayName = "Date",
				Width = 12,
				Format = "yyyy-mm-dd",
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockDetailsModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction No",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockDetailsModel.Type)] = new()
			{
				DisplayName = "Transaction Type",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter,
				FormatCallback = (value) =>
				{
					if (value is null) return null;

					var type = value.ToString();
					return type switch
					{
						"Purchase" or "KitchenProduction" => new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(56, 142, 60),
							Bold = true
						},
						"Sale" => new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0),
							Bold = true
						},
						"SaleReturn" => new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(255, 152, 0),
							Bold = true
						},
						"PurchaseReturn" => new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(244, 67, 54),
							Bold = true
						},
						"Adjustment" => new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(33, 150, 243),
							Bold = true
						},
						_ => null
					};
				}
			},
			[nameof(ProductStockDetailsModel.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductStockDetailsModel.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductStockDetailsModel.Quantity)] = new()
			{
				DisplayName = "Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value is null) return null;

					var quantity = Convert.ToDecimal(value);
					if (quantity > 0)
					{
						return new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(56, 142, 60),
							Bold = true
						};
					}
					else if (quantity < 0)
					{
						return new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40),
							Bold = true
						};
					}

					return null;
				}
			},
			[nameof(ProductStockDetailsModel.NetRate)] = new()
			{
				DisplayName = "Net Rate",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value is null)
					{
						return new ExcelExportUtilOld.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(117, 117, 117),
							Bold = false
						};
					}

					return null;
				}
			}
		};

		// Generate title and worksheet name based on location
		string reportTitle = "Finished Product Stock Transaction Details";

		if (selectedLocationId > 0 && locations is not null)
		{
			var location = locations.FirstOrDefault(l => l.Id == selectedLocationId);
			if (location is not null)
				reportTitle = $"Finished Product Stock Transaction Details - {location.Name}";
		}

		string worksheetName = "Product Transaction Details";

		return ExcelExportUtilOld.ExportToExcel(
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