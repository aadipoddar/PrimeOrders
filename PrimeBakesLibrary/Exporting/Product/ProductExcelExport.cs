using PrimeBakesLibrary.Models.Product;

namespace PrimeBakesLibrary.Exporting.Product;

public static class ProductExcelExport
{
	public static MemoryStream ExportProductDetailExcel(
		List<ProductOverviewModel> productOverviews,
		DateOnly startDate,
		DateOnly endDate,
		ProductModel selectedProduct = null,
		int selectedCategoryId = 0,
		List<ProductCategoryModel> productCategories = null)
	{
		// Create summary items dictionary with key metrics
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Sales", productOverviews.Sum(p => p.TotalAmount) },
			{ "Total Products", productOverviews.Select(p => p.ProductId).Distinct().Count() },
			{ "Total Quantity", productOverviews.Sum(p => p.QuantitySold) },
			{ "Total Discount", productOverviews.Sum(p => p.DiscountAmount) },
			{ "Total Tax", productOverviews.Sum(p => p.TotalTaxAmount) },
			{ "Transactions", productOverviews.Select(p => p.SaleId).Distinct().Count() },
			{ "Base Total", productOverviews.Sum(p => p.BaseTotal) },
			{ "Sub Total", productOverviews.Sum(p => p.SubTotal) }
		};

		// Add product-specific info if a product is selected
		if (selectedProduct is not null)
		{
			summaryItems.Add("Product Code", selectedProduct.Code);
			summaryItems.Add("MRP", selectedProduct.Rate);
			summaryItems.Add("Average Selling Price", productOverviews.Average(p => p.AveragePrice));

			var categoryName = productCategories?.FirstOrDefault(c => c.Id == selectedProduct.ProductCategoryId)?.Name;
			if (!string.IsNullOrEmpty(categoryName))
				summaryItems.Add("Category", categoryName);
		}
		else if (selectedCategoryId > 0 && productCategories is not null)
		{
			var category = productCategories.FirstOrDefault(c => c.Id == selectedCategoryId);
			if (category is not null)
				summaryItems.Add("Filtered by Category", category.Name);
		}

		// Add top products summary data
		var topProducts = productOverviews
			.GroupBy(p => new { p.ProductId, p.ProductName })
			.OrderByDescending(g => g.Sum(p => p.TotalAmount))
			.Take(3)
			.ToList();

		foreach (var product in topProducts)
			summaryItems.Add($"Top Product: {product.Key.ProductName}", product.Sum(p => p.TotalAmount));

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(ProductOverviewModel.ProductName),
			nameof(ProductOverviewModel.ProductCode),
			nameof(ProductOverviewModel.ProductCategoryName),
			nameof(ProductOverviewModel.BillNo),
			nameof(ProductOverviewModel.BillDateTime),
			nameof(ProductOverviewModel.QuantitySold),
			nameof(ProductOverviewModel.AveragePrice),
			nameof(ProductOverviewModel.BaseTotal),
			nameof(ProductOverviewModel.DiscountAmount),
			nameof(ProductOverviewModel.TotalTaxAmount),
			nameof(ProductOverviewModel.SGSTAmount),
			nameof(ProductOverviewModel.CGSTAmount),
			nameof(ProductOverviewModel.IGSTAmount),
			nameof(ProductOverviewModel.TotalAmount)
		];

		// Define custom column settings with professional styling
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(ProductOverviewModel.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductOverviewModel.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductOverviewModel.ProductCategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductOverviewModel.BillNo)] = new()
			{
				DisplayName = "Bill No",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductOverviewModel.BillDateTime)] = new()
			{
				DisplayName = "Date & Time",
				Format = "dd-MMM-yyyy hh:mm",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductOverviewModel.QuantitySold)] = new()
			{
				DisplayName = "Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var qty = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = qty > 10,
						FontColor = qty > 50 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			},
			[nameof(ProductOverviewModel.AveragePrice)] = new()
			{
				DisplayName = "Rate",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Discount",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var discount = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = discount > 0,
						FontColor = discount > 0 ? Syncfusion.Drawing.Color.FromArgb(220, 53, 69) : null
					};
				}
			},
			[nameof(ProductOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Total Tax",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOverviewModel.SGSTAmount)] = new()
			{
				DisplayName = "SGST",
				Format = "₹#,##0.00",
				Width = 12,
				IsCurrency = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOverviewModel.CGSTAmount)] = new()
			{
				DisplayName = "CGST",
				Format = "₹#,##0.00",
				Width = 12,
				IsCurrency = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOverviewModel.IGSTAmount)] = new()
			{
				DisplayName = "IGST",
				Format = "₹#,##0.00",
				Width = 12,
				IsCurrency = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Total",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight,
				HighlightNegative = true,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var amount = Convert.ToDecimal(value);
					return new ExcelExportUtil.FormatInfo
					{
						Bold = amount > 10000,
						FontColor = amount > 50000 ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : null
					};
				}
			}
		};

		// Generate title based on selected filters
		string reportTitle = "Product Detail Report";

		if (selectedProduct is not null)
			reportTitle = $"Detail Report - {selectedProduct.Name}";
		else if (selectedCategoryId > 0 && productCategories is not null)
		{
			var category = productCategories.FirstOrDefault(c => c.Id == selectedCategoryId);
			if (category != null)
				reportTitle = $"Product Detail Report - {category.Name} Category";
		}

		string worksheetName = "Product Details";

		return ExcelExportUtil.ExportToExcel(
			productOverviews,
			reportTitle,
			worksheetName,
			startDate,
			endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}
}