using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports;

public partial class OrderHistoryPage
{
	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _deleteConfirmationDialogVisible = false;

	private string _selectedStatusFilter = "All";
	private int _orderToDeleteId = 0;
	private string _orderToDeleteNo = "";

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private List<OrderOverviewModel> _orderOverviews = [];
	private List<LocationModel> _locations = [];
	private int _selectedLocationId = 0;

	private SfGrid<OrderOverviewModel> _sfGrid;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Order)).User) is not null))
			return;

		await LoadLocations();
		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
			_locations.RemoveAll(l => l.Id == 1);
			_selectedLocationId = 0;
		}

		else
			_selectedLocationId = _user.LocationId;
	}

	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadData();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadData();
	}

	private async Task OnStatusFilterChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, string> args)
	{
		_selectedStatusFilter = args.Value;
		await LoadData();
	}

	private async Task LoadData()
	{
		var orders = await OrderData.LoadOrderDetailsByDateLocationId(
		_startDate.ToDateTime(new TimeOnly(0, 0)),
		_endDate.ToDateTime(new TimeOnly(23, 59)),
		_selectedLocationId);

		// Apply status filtering
		_orderOverviews = _selectedStatusFilter switch
		{
			"Pending" => [.. orders.Where(o => !o.SaleId.HasValue)],
			"Sold" => [.. orders.Where(o => o.SaleId.HasValue)],
			_ => orders // "All" or any other value
		};

		StateHasChanged();
	}

	public void OrderHistoryRowSelected(RowSelectEventArgs<OrderOverviewModel> args) =>
		ViewOrderDetails(args.Data.OrderId);

	private void ViewOrderDetails(int orderId) =>
		NavManager.NavigateTo($"/Order/{orderId}");

	private void ShowDeleteConfirmation(int orderId, string orderNo)
	{
		if (!_user.Admin)
		{
			_sfErrorToast.Content = "Only administrators can delete records.";
			_sfErrorToast.ShowAsync();
			return;
		}

		_orderToDeleteId = orderId;
		_orderToDeleteNo = orderNo;
		_deleteConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteOrder()
	{
		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _orderToDeleteId);
		if (order == null)
		{
			_sfErrorToast.Content = "Order not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		var orderOverview = _orderOverviews.FirstOrDefault(o => o.OrderId == _orderToDeleteId);
		if (orderOverview?.SaleId.HasValue == true)
		{
			_sfErrorToast.Content = "Cannot delete completed orders. Only pending orders can be deleted.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		order.Status = false;
		await OrderData.InsertOrder(order);

		_sfSuccessToast.Content = "Order deleted successfully.";
		await _sfSuccessToast.ShowAsync();

		await LoadData();
		_deleteConfirmationDialogVisible = false;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteConfirmationDialogVisible = false;
		_orderToDeleteId = 0;
		_orderToDeleteNo = "";
		StateHasChanged();
	}

	private async Task ExportToProductsExcel()
	{
		var pendingOrders = _orderOverviews?.Where(o => o.SaleId == null).ToList();

		if (pendingOrders is null || pendingOrders.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No pending orders to export");
			return;
		}

		// Dictionary to track product quantities across pending orders
		var productQuantities = new Dictionary<int, ProductOrderSummary>();

		// Get all product categories for later use
		var productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);

		// Process each pending order
		foreach (var order in pendingOrders)
		{
			// Load order details for this order
			var orderDetails = await OrderData.LoadOrderDetailByOrder(order.OrderId);

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

		var memoryStream = ExcelExportUtil.ExportToExcel(
			productSummaries,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		var fileName = $"Pending_Orders_Products_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	private async Task ExportToExcel()
	{
		if (_orderOverviews is null || _orderOverviews.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No data to export");
			return;
		}

		// Create summary items dictionary
		Dictionary<string, object> summaryItems = new()
	{
		{ "Total Orders", _orderOverviews.Count },
		{ "Total Products", _orderOverviews.Sum(o => o.TotalProducts) },
		{ "Total Quantity", _orderOverviews.Sum(o => o.TotalQuantity) },
		{ "Completed Orders", _orderOverviews.Count(o => o.SaleId is not null) },
		{ "Pending Orders", _orderOverviews.Count(o => o.SaleId is null) }
	};

		// Define the column order for better readability
		List<string> columnOrder = [
			nameof(OrderOverviewModel.OrderNo),
		nameof(OrderOverviewModel.OrderDate),
		nameof(OrderOverviewModel.LocationName),
		nameof(OrderOverviewModel.UserName),
		nameof(OrderOverviewModel.TotalProducts),
		nameof(OrderOverviewModel.TotalQuantity),
		nameof(OrderOverviewModel.SaleId),
		nameof(OrderOverviewModel.SaleBillNo)
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
			[nameof(OrderOverviewModel.OrderDate)] = new()
			{
				DisplayName = "Order Date",
				Format = "dd-MMM-yyyy",
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
			}
		};

		// Generate the Excel file
		string reportTitle = "Order History Report";
		string worksheetName = "Order Details";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			_orderOverviews,
			reportTitle,
			worksheetName,
			_startDate,
			_endDate,
			summaryItems,
			columnSettings,
			columnOrder);

		var fileName = $"Order_History_{_startDate:yyyy-MM-dd}_to_{_endDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	private async Task ExportOrderChallan(int orderId)
	{
		// Load order details
		var orderDetails = await OrderData.LoadOrderDetailByOrder(orderId);
		var order = _orderOverviews.FirstOrDefault(o => o.OrderId == orderId);

		if (order == null || orderDetails == null || orderDetails.Count == 0)
		{
			await JS.InvokeVoidAsync("alert", "No order details found to export");
			return;
		}

		// Create a list to hold challan items
		var challanItems = new List<ChallanItemModel>();

		// Load product details for each order item
		foreach (var detail in orderDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			if (product != null)
			{
				challanItems.Add(new ChallanItemModel
				{
					ProductCode = product.Code,
					ProductName = product.Name,
					Quantity = detail.Quantity
				});
			}
		}

		// Create summary items for the challan
		Dictionary<string, object> summaryItems = new()
		{
			{ "Order Number", order.OrderNo },
			{ "Order Date", order.OrderDate.ToString("dd-MMM-yyyy") },
			{ "Location", order.LocationName },
			{ "Created By", order.UserName },
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
		string reportTitle = $"Delivery Challan - Order #{order.OrderNo}";
		string worksheetName = "Challan Items";

		var memoryStream = ExcelExportUtil.ExportToExcel(
			challanItems,
			reportTitle,
			worksheetName,
			order.OrderDate,
			order.OrderDate,
			summaryItems,
			columnSettings,
			columnOrder);

		var fileName = $"Challan_Order_{order.OrderNo}_{order.OrderDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	// Chart data methods
	private List<ChartData> GetDailyOrdersData()
	{
		var result = new List<ChartData>();
		if (_orderOverviews == null || _orderOverviews.Count == 0)
			return result;

		var groupedByDate = _orderOverviews
			.GroupBy(o => o.OrderDate)
			.OrderBy(g => g.Key)
			.ToList();

		foreach (var group in groupedByDate)
		{
			result.Add(new ChartData
			{
				Date = group.Key.ToString("dd/MM/yyyy"),
				Count = group.Count()
			});
		}

		return result;
	}

	private List<StatusData> GetOrderStatusData()
	{
		var result = new List<StatusData>();
		if (_orderOverviews == null || _orderOverviews.Count == 0)
			return result;

		int pendingCount = _orderOverviews.Count(o => o.SaleId is null);
		int completedCount = _orderOverviews.Count(o => o.SaleId is not null);

		result.Add(new StatusData { Status = "Pending", Count = pendingCount });
		result.Add(new StatusData { Status = "Completed", Count = completedCount });

		return result;
	}

	// Chart data classes
	public class ChartData
	{
		public string Date { get; set; }
		public int Count { get; set; }
	}

	public class StatusData
	{
		public string Status { get; set; }
		public int Count { get; set; }
	}

	public class ProductOrderSummary
	{
		public int ProductId { get; set; }
		public string ProductCode { get; set; }
		public string ProductName { get; set; }
		public string CategoryName { get; set; }
		public decimal ProductRate { get; set; }
		public int OrderCount { get; set; }  // Number of times this product appears in orders
		public List<int> OrderIds { get; set; } = [];  // Track which orders this product appears in
		public List<string> OrderNumbers { get; set; } = [];  // Track order numbers
		public string OrderNumbersList => string.Join(", ", OrderNumbers);  // For Excel display
		public int OrdersAppeared => OrderIds.Count;  // Number of distinct orders this product appears in
		public decimal TotalQuantity { get; set; }  // Total quantity ordered across all orders
		public decimal TotalValue => ProductRate * TotalQuantity;  // Calculated total value
	}

	public class ChallanItemModel
	{
		public string ProductCode { get; set; }
		public string ProductName { get; set; }
		public decimal Quantity { get; set; }
	}
}