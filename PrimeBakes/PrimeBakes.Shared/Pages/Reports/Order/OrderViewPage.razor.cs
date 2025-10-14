using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Order;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Order;

public partial class OrderViewPage
{
	[Parameter] public int OrderId { get; set; }

	private UserModel _user;

	private OrderOverviewModel _orderOverview;
	private readonly List<DetailedOrderProductModel> _detailedOrderProducts = [];
	private bool _isLoading = true;
	private bool _isProcessing = false;

	// Delete confirmation dialog
	private SfDialog _deleteConfirmDialog;
	private bool _showDeleteConfirm = false;

	protected override async Task OnInitializedAsync()
	{
		_isLoading = true;

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
		_user = authResult.User;

		await LoadOrderDetails();
		_isLoading = false;
	}

	private async Task LoadOrderDetails()
	{
		_orderOverview = await OrderData.LoadOrderOverviewByOrderId(OrderId);

		if (_user.LocationId != 1 && _user.LocationId != _orderOverview.LocationId)
		{
			NavigationManager.NavigateTo("/Reports/OrderHistory");
			return;
		}

		if (_orderOverview is not null)
		{
			// Load detailed order products
			var orderDetails = await OrderData.LoadOrderDetailByOrder(OrderId);
			var products = await ProductData.LoadProductByLocationRate(_orderOverview.LocationId);

			// Load product information for each order detail
			foreach (var detail in orderDetails)
			{
				var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
				if (product is not null)
				{
					// Load category information
					var category = await CommonData.LoadTableDataById<ProductCategoryModel>(TableNames.ProductCategory, product.ProductCategoryId);

					_detailedOrderProducts.Add(new DetailedOrderProductModel
					{
						ProductCode = product.Code,
						ProductName = product.Name,
						CategoryName = category?.Name ?? "N/A",
						Quantity = detail.Quantity,
						Rate = product.Rate,
						Total = detail.Quantity * product.Rate
					});
				}
			}
		}

		StateHasChanged();
	}

	#region Exporting
	private async Task ExportChallan()
	{
		if (_orderOverview is null || _isProcessing)
			return;

		_isProcessing = true;
		StateHasChanged();

		var orderDetails = await OrderData.LoadOrderDetailByOrder(_orderOverview.OrderId);
		var memoryStream = await OrderExcelExport.ExportOrderChallanExcel(_orderOverview, orderDetails);
		var fileName = $"Challan_{_orderOverview.OrderNo}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
		await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", memoryStream);

		_isProcessing = false;
		StateHasChanged();
	}

	private async Task PrintPDF()
	{
		if (_orderOverview == null || _isProcessing)
			return;

		_isProcessing = true;
		StateHasChanged();

		var memoryStream = await OrderA4Print.GenerateA4OrderDocument(_orderOverview.OrderId);
		var fileName = $"Order_{_orderOverview.OrderNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);

		_isProcessing = false;
		StateHasChanged();
	}
	#endregion

	#region Order Actions
	private async Task EditOrder()
	{
		if (_orderOverview is null || _isProcessing || _orderOverview.SaleId.HasValue || _user.LocationId != 1 || !_user.Admin)
			return;

		await DataStorageService.LocalRemove(StorageFileNames.OrderDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderCartDataFileName);

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _orderOverview.OrderId);
		var orderDetails = await OrderData.LoadOrderDetailByOrder(_orderOverview.OrderId);

		List<OrderProductCartModel> cart = [];
		foreach (var product in orderDetails)
		{
			var prod = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, product.ProductId);
			cart.Add(new()
			{
				ProductId = product.ProductId,
				ProductName = prod.Name,
				ProductCategoryId = prod.ProductCategoryId,
				Quantity = product.Quantity
			});
		}

		await DataStorageService.LocalSaveAsync(StorageFileNames.OrderDataFileName, System.Text.Json.JsonSerializer.Serialize(order));
		await DataStorageService.LocalSaveAsync(StorageFileNames.OrderCartDataFileName, System.Text.Json.JsonSerializer.Serialize(cart));

		NavigationManager.NavigateTo("/Order");
	}

	private void DeleteOrder()
	{
		if (_orderOverview is null || _isProcessing || _orderOverview.SaleId.HasValue || _user.LocationId != 1 || !_user.Admin)
			return;

		_showDeleteConfirm = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteOrder()
	{
		_showDeleteConfirm = false;
		_isProcessing = true;
		StateHasChanged();

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _orderOverview.OrderId);
		if (order is null)
		{
			NavigationManager.NavigateTo("/Reports/OrderHistory");
			return;
		}

		order.Status = false;
		await OrderData.InsertOrder(order);
		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo("/Reports/OrderHistory");
	}

	private void CancelDeleteOrder()
	{
		_showDeleteConfirm = false;
		StateHasChanged();
	}

	private void ViewSale()
	{
		if (_orderOverview?.SaleId.HasValue == true)
			NavigationManager.NavigateTo($"/Reports/Sale/View/{_orderOverview.SaleId.Value}");
	}
	#endregion

	// Model class for detailed order products
	public class DetailedOrderProductModel
	{
		public string ProductCode { get; set; } = string.Empty;
		public string ProductName { get; set; } = string.Empty;
		public string CategoryName { get; set; } = string.Empty;
		public decimal Quantity { get; set; }
		public decimal Rate { get; set; }
		public decimal Total { get; set; }
	}
}