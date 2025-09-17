using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Models.Product;

using static PrimeOrdersLibrary.Data.Order.OrderData;

namespace PrimeOrdersLibrary.Exporting.Order;

public static class OrderExcelExport
{
	public static MemoryStream ExportOrderOverviewExcel(List<OrderOverviewModel> orderOverviews, DateOnly _startDate, DateOnly _endDate)
	{
		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Total Orders", orderOverviews.Count },
			{ "Total Products", orderOverviews.Sum(o => o.TotalProducts) },
			{ "Total Quantity", orderOverviews.Sum(o => o.TotalQuantity) },
			{ "Completed Orders", orderOverviews.Count(o => o.SaleId is not null) },
			{ "Pending Orders", orderOverviews.Count(o => o.SaleId is null) }
		};

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(OrderOverviewModel.OrderNo),
			nameof(OrderOverviewModel.OrderDateTime),
			nameof(OrderOverviewModel.LocationName),
			nameof(OrderOverviewModel.UserName),
			nameof(OrderOverviewModel.TotalProducts),
			nameof(OrderOverviewModel.TotalQuantity),
			nameof(OrderOverviewModel.SaleId),
			nameof(OrderOverviewModel.SaleBillNo),
			nameof(OrderOverviewModel.Remarks),
			nameof(OrderOverviewModel.CreatedAt)
		];

		// Define custom column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(OrderOverviewModel.OrderNo)] = new()
			{
				DisplayName = "Order #",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(OrderOverviewModel.OrderDateTime)] = new()
			{
				DisplayName = "Order Date",
				Format = "dd-MMM-yyyy hh:mm",
				Width = 15
			},
			[nameof(OrderOverviewModel.LocationName)] = new()
			{
				DisplayName = "Location",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(OrderOverviewModel.UserName)] = new()
			{
				DisplayName = "Created By",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(OrderOverviewModel.TotalProducts)] = new()
			{
				DisplayName = "Products",
				Format = "#,##0",
				Width = 12,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(OrderOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(OrderOverviewModel.SaleId)] = new()
			{
				DisplayName = "Status",
				Width = 12,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter,
				FormatCallback = (value) =>
				{
					if (value == null) return null;
					var isCompleted = value != null;
					return new ExcelExportUtil.FormatInfo
					{
						Bold = true,
						FontColor = isCompleted ? Syncfusion.Drawing.Color.FromArgb(56, 142, 60) : Syncfusion.Drawing.Color.FromArgb(255, 165, 0),
						FormattedText = isCompleted ? "Completed" : "Pending"
					};
				}
			},
			[nameof(OrderOverviewModel.SaleBillNo)] = new()
			{
				DisplayName = "Sale Bill #",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(OrderOverviewModel.Remarks)] = new()
			{
				DisplayName = "Remarks",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(OrderOverviewModel.CreatedAt)] = new()
			{
				DisplayName = "Created At",
				Format = "dd-MMM-yyyy hh:mm",
				Width = 15
			}
		};

		// Generate the Excel file
		string reportTitle = "Order History Report";
		string worksheetName = "Order Details";

		return ExcelExportUtil.ExportToExcel(
			orderOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}

	public static async Task<MemoryStream> ExportPendingProductsExcel(List<OrderOverviewModel> pendingOrders, DateOnly _startDate, DateOnly _endDate)
	{
		// Dictionary to track product quantities across pending orders
		var productQuantities = new Dictionary<int, ProductOrderSummary>();

		// Get all product categories for later use
		var productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);

		// Process each pending order
		foreach (var order in pendingOrders)
		{
			// Load order details for this order
			var orderDetails = await LoadOrderDetailByOrder(order.OrderId);

			// Process each product in the order
			foreach (var detail in orderDetails)
			{
				// Get or create summary for this product
				if (!productQuantities.TryGetValue(detail.ProductId, out var summary))
				{
					// Load product info if we haven't seen this product yet
					var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
					if (product == null) continue;

					// Find category name
					var categoryName = productCategories
						.FirstOrDefault(c => c.Id == product.ProductCategoryId)?.Name ?? "Unknown";

					summary = new ProductOrderSummary
					{
						ProductId = product.Id,
						ProductCode = product.Code,
						ProductName = product.Name,
						CategoryName = categoryName,
						ProductRate = product.Rate,
						OrderCount = 0,
						TotalQuantity = 0
					};

					productQuantities[detail.ProductId] = summary;
				}

				// Update summary with this order's quantity
				summary.OrderCount++;
				summary.TotalQuantity += detail.Quantity;

				// Track order IDs this product appears in
				if (!summary.OrderIds.Contains(order.OrderId))
					summary.OrderIds.Add(order.OrderId);

				// Track order numbers for reference
				if (!summary.OrderNumbers.Contains(order.OrderNo))
					summary.OrderNumbers.Add(order.OrderNo);
			}
		}

		// Convert dictionary to sorted list
		var productSummaries = productQuantities.Values
			.OrderByDescending(p => p.TotalQuantity)
			.ToList();

		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
		{
			{ "Report Type", "Pending Orders Product Summary" },
			{ "Date Range", $"{_startDate:dd MMM yyyy} - {_endDate:dd MMM yyyy}" },
			{ "Total Pending Orders", pendingOrders.Count },
			{ "Unique Products", productSummaries.Count },
			{ "Total Quantity", productSummaries.Sum(p => p.TotalQuantity) },
			{ "Estimated Value", productSummaries.Sum(p => p.TotalValue) }
		};

		// Add top 3 products by quantity
		var topProducts = productSummaries.Take(3).ToList();
		foreach (var product in topProducts)
		{
			summaryItems.Add($"Top Product: {product.ProductName}", product.TotalQuantity);
		}

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(ProductOrderSummary.ProductCode),
			nameof(ProductOrderSummary.ProductName),
			nameof(ProductOrderSummary.CategoryName),
			nameof(ProductOrderSummary.ProductRate),
			nameof(ProductOrderSummary.OrderCount),
			nameof(ProductOrderSummary.OrdersAppeared),
			nameof(ProductOrderSummary.OrderNumbersList),
			nameof(ProductOrderSummary.TotalQuantity),
			nameof(ProductOrderSummary.TotalValue)
		];

		// Define custom column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(ProductOrderSummary.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ProductOrderSummary.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductOrderSummary.CategoryName)] = new()
			{
				DisplayName = "Category",
				Width = 20,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductOrderSummary.ProductRate)] = new()
			{
				DisplayName = "Rate",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOrderSummary.OrderCount)] = new()
			{
				DisplayName = "Times Ordered",
				Format = "#,##0",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOrderSummary.OrdersAppeared)] = new()
			{
				DisplayName = "Orders Count",
				Format = "#,##0",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			},
			[nameof(ProductOrderSummary.OrderNumbersList)] = new()
			{
				DisplayName = "Order Numbers",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ProductOrderSummary.TotalQuantity)] = new()
			{
				DisplayName = "Total Quantity",
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
			[nameof(ProductOrderSummary.TotalValue)] = new()
			{
				DisplayName = "Total Value",
				Format = "₹#,##0.00",
				Width = 15,
				IsCurrency = true,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			}
		};

		// Generate the Excel file
		string reportTitle = "Pending Orders - Product Quantities Report";
		string worksheetName = "Pending Products";

		return ExcelExportUtil.ExportToExcel(
			productSummaries,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);
	}

	public static async Task<MemoryStream> ExportOrderChallanExcel(OrderOverviewModel selectedOrder, List<OrderDetailModel> orderDetails)
	{
		// Create a list to hold challan items
		var challanItems = new List<ChallanItemModel>();

		// Load product details for each order item
		foreach (var detail in orderDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			if (product is not null)
				challanItems.Add(new ChallanItemModel
				{
					ProductCode = product.Code,
					ProductName = product.Name,
					Quantity = detail.Quantity
				});
		}

		// Create summary items for the challan
		Dictionary<string, object> summaryItems = new()
			{
				{ "Order Number", selectedOrder.OrderNo },
				{ "Order Date", selectedOrder.OrderDateTime.ToString("dd-MMM-yyyy hh:mm tt") },
				{ "Location", selectedOrder.LocationName },
				{ "Created By", selectedOrder.UserName },
				{ "Total Products", challanItems.Count },
				{ "Total Quantity", challanItems.Sum(c => c.Quantity) }
			};

		// Define column order for better readability
		List<string> columnOrder = [
			nameof(ChallanItemModel.ProductCode),
				nameof(ChallanItemModel.ProductName),
				nameof(ChallanItemModel.Quantity)
		];

		// Define custom column settings
		Dictionary<string, ExcelExportUtil.ColumnSetting> columnSettings = new()
		{
			[nameof(ChallanItemModel.ProductCode)] = new()
			{
				DisplayName = "Product Code",
				Width = 15,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter
			},
			[nameof(ChallanItemModel.ProductName)] = new()
			{
				DisplayName = "Product Name",
				Width = 30,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft
			},
			[nameof(ChallanItemModel.Quantity)] = new()
			{
				DisplayName = "Quantity",
				Format = "#,##0.00",
				Width = 15,
				IncludeInTotal = true,
				Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight
			}
		};

		// Generate the Excel file
		string reportTitle = $"Delivery Challan - Order #{selectedOrder.OrderNo}";
		string worksheetName = "Challan Items";

		return ExcelExportUtil.ExportToExcel(
			challanItems,
			reportTitle,
			worksheetName,
			DateOnly.FromDateTime(selectedOrder.OrderDateTime),
			DateOnly.FromDateTime(selectedOrder.OrderDateTime),
			summaryItems,
			columnSettings,
			columnOrder);
	}
}
