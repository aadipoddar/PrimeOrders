using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Exporting.Stock;

public static class RawMaterialStockExcelExport
{
	/// <summary>
	/// Exports raw material stock details to Excel
	/// </summary>
	public static MemoryStream ExportRawMaterialStockSummaryExcel(
		List<RawMaterialStockSummaryModel> stockDetails,
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
			nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName),
			nameof(RawMaterialStockSummaryModel.RawMaterialCode),
			nameof(RawMaterialStockSummaryModel.RawMaterialName),
			nameof(RawMaterialStockSummaryModel.OpeningStock),
			nameof(RawMaterialStockSummaryModel.PurchaseStock),
			nameof(RawMaterialStockSummaryModel.SaleStock),
			nameof(RawMaterialStockSummaryModel.MonthlyStock),
			nameof(RawMaterialStockSummaryModel.ClosingStock),
			nameof(RawMaterialStockSummaryModel.AveragePrice),
			nameof(RawMaterialStockSummaryModel.WeightedAverageValue),
			nameof(RawMaterialStockSummaryModel.LastPurchasePrice),
			nameof(RawMaterialStockSummaryModel.LastPurchaseValue)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(RawMaterialStockSummaryModel.RawMaterialCategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(RawMaterialStockSummaryModel.RawMaterialCode)] = new()
			{
				DisplayName = "Item Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(RawMaterialStockSummaryModel.RawMaterialName)] = new()
			{
				DisplayName = "Item Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(RawMaterialStockSummaryModel.OpeningStock)] = new()
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
			[nameof(RawMaterialStockSummaryModel.PurchaseStock)] = new()
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
			[nameof(RawMaterialStockSummaryModel.SaleStock)] = new()
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
			[nameof(RawMaterialStockSummaryModel.MonthlyStock)] = new()
			{
				DisplayName = "Monthly Stock",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockSummaryModel.ClosingStock)] = new()
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
			[nameof(RawMaterialStockSummaryModel.AveragePrice)] = new()
			{
				DisplayName = "Average Price",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockSummaryModel.WeightedAverageValue)] = new()
			{
				DisplayName = "Weighted Avg Value",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockSummaryModel.LastPurchasePrice)] = new()
			{
				DisplayName = "Last Purchase Price",
				Format = "₹#,##0.00",
				Width = 18,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(RawMaterialStockSummaryModel.LastPurchaseValue)] = new()
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
	/// Exports raw material stock details to Excel
	/// </summary>
	public static MemoryStream ExportRawMaterialStockDetailsExcel(
		List<RawMaterialStockDetailsModel> stockDetails,
		DateOnly startDate,
		DateOnly endDate)
	{
		if (stockDetails is null || stockDetails.Count == 0)
			throw new ArgumentException("No data to export");

		// Create summary items dictionary with transaction details
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Transactions", stockDetails.Count },
			{ "Total Quantity", stockDetails.Sum(s => s.Quantity) },
			{ "Purchase Transactions", stockDetails.Count(s => s.Type == StockType.Purchase.ToString()) },
			{ "Sale Transactions", stockDetails.Count(s => s.Type == StockType.Sale.ToString()) },
			{ "Adjustment Transactions", stockDetails.Count(s => s.Type == StockType.Adjustment.ToString()) },
			{ "Kitchen Issue Transactions", stockDetails.Count(s => s.Type == StockType.KitchenIssue.ToString()) },
			{ "Production Transactions", stockDetails.Count(s => s.Type == StockType.KitchenProduction.ToString()) }
		};

		// Add top materials by transaction count
		var topMaterials = stockDetails
			.GroupBy(s => s.RawMaterialName)
			.OrderByDescending(g => g.Count())
			.Take(3)
			.ToList();

		foreach (var material in topMaterials)
			summaryItems.Add($"Material: {material.Key}", material.Count());

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(RawMaterialStockDetailsModel.Id),
			nameof(RawMaterialStockDetailsModel.TransactionDate),
			nameof(RawMaterialStockDetailsModel.TransactionNo),
			nameof(RawMaterialStockDetailsModel.Type),
			nameof(RawMaterialStockDetailsModel.RawMaterialCode),
			nameof(RawMaterialStockDetailsModel.RawMaterialName),
			nameof(RawMaterialStockDetailsModel.Quantity),
			nameof(RawMaterialStockDetailsModel.NetRate)
		];

		// Define custom column settings
		var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
		{
			[nameof(RawMaterialStockDetailsModel.Id)] = new()
			{
				DisplayName = "Id",
				Width = 8,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(RawMaterialStockDetailsModel.TransactionDate)] = new()
			{
				DisplayName = "Date",
				Width = 12,
				Format = "yyyy-mm-dd",
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(RawMaterialStockDetailsModel.TransactionNo)] = new()
			{
				DisplayName = "Transaction No",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(RawMaterialStockDetailsModel.Type)] = new()
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
						"Purchase" => new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(56, 142, 60),
							Bold = true
						},
						"Sale" => new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(239, 108, 0),
							Bold = true
						},
						"Adjustment" => new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(33, 150, 243),
							Bold = true
						},
						"KitchenIssue" => new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(156, 39, 176),
							Bold = true
						},
						"KitchenProduction" => new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(76, 175, 80),
							Bold = true
						},
						_ => null
					};
				}
			},
			[nameof(RawMaterialStockDetailsModel.RawMaterialCode)] = new()
			{
				DisplayName = "Item Code",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(RawMaterialStockDetailsModel.RawMaterialName)] = new()
			{
				DisplayName = "Item Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(RawMaterialStockDetailsModel.Quantity)] = new()
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
						return new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(56, 142, 60),
							Bold = true
						};
					}
					else if (quantity < 0)
					{
						return new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(198, 40, 40),
							Bold = true
						};
					}

					return null;
				}
			},
			[nameof(RawMaterialStockDetailsModel.NetRate)] = new()
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
						return new ExcelExportUtil.FormatInfo
						{
							FontColor = Syncfusion.Drawing.Color.FromArgb(117, 117, 117),
							Bold = false
						};
					}

					return null;
				}
			}
		};

		// Generate title and worksheet name
		string reportTitle = "Raw Material Stock Transaction Details";
		string worksheetName = "Transaction Details";

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