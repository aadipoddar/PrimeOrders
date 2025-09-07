using PrimeOrdersLibrary.Exporting.Order;

using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports.Order;

public partial class OrderSummaryModule
{
	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public OrderOverviewModel SelectedOrder { get; set; }
	[Parameter] public List<OrderDetailDisplayModel> OrderDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	private SfDialog _sfOrderSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;
	private int _orderToDeleteId = 0;
	private string _orderToDeleteNo = "";

	private bool _saleSummaryVisible = false;
	private SaleOverviewModel _selectedSale;
	private readonly List<SaleDetailDisplayModel> _selectedSaleDetails = [];

	private async Task ExportOrderChallan()
	{
		var orderDetails = await OrderData.LoadOrderDetailByOrder(SelectedOrder.OrderId);
		if (orderDetails is null || orderDetails.Count == 0)
		{
			await ShowErrorToast("No order details found to export");
			return;
		}

		var memoryStream = await OrderExcelExport.ExportOrderChallanExcel(SelectedOrder, orderDetails);
		var fileName = $"Challan_Order_{SelectedOrder.OrderNo}_{SelectedOrder.OrderDateTime:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveExcel", Convert.ToBase64String(memoryStream.ToArray()), fileName);

		await CloseDialog();
	}

	#region Delete Order
	private void ShowDeleteConfirmation()
	{
		_orderToDeleteId = SelectedOrder?.OrderId ?? 0;
		_orderToDeleteNo = SelectedOrder?.OrderNo ?? "";
		_deleteConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteOrder()
	{
		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _orderToDeleteId);
		if (order is null)
		{
			await ShowErrorToast("Order not found.");
			return;
		}

		if (CurrentUser.Admin == false && CurrentUser.Order == false)
		{
			await ShowErrorToast("You do not have permission to delete orders.");
			return;
		}

		if (SelectedOrder?.SaleId.HasValue == true)
		{
			await ShowErrorToast("Cannot delete completed orders. Only pending orders can be deleted.");
			return;
		}

		order.Status = false;
		await OrderData.InsertOrder(order);

		_deleteConfirmationDialogVisible = false;
		_sfSuccessToast.Content = "Order deleted successfully.";
		await _sfSuccessToast.ShowAsync();
		await CloseDialog();
	}

	private void CancelDelete()
	{
		_deleteConfirmationDialogVisible = false;
		_orderToDeleteId = 0;
		_orderToDeleteNo = "";
		StateHasChanged();
	}
	#endregion

	#region Sale Summary
	private async Task ViewCorrespondingSale()
	{
		if (!SelectedOrder.SaleId.HasValue)
		{
			await ShowErrorToast("No sale is linked to this order.");
			return;
		}

		_selectedSale = await SaleData.LoadSaleOverviewBySaleId(SelectedOrder.SaleId.Value);
		if (_selectedSale is null)
		{
			await ShowErrorToast("Sale not found.");
			return;
		}

		_selectedSaleDetails.Clear();

		var saleDetails = await SaleData.LoadSaleDetailBySale(_selectedSale.SaleId);
		foreach (var detail in saleDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);
			if (product is not null)
				_selectedSaleDetails.Add(new SaleDetailDisplayModel
				{
					ProductName = product.Name,
					Quantity = detail.Quantity,
					Rate = detail.Rate,
					Total = detail.Total
				});
		}

		await CloseDialog();
		_saleSummaryVisible = true;
		StateHasChanged();
	}

	private void OnSaleSummaryVisibilityChanged(bool isVisible)
	{
		_saleSummaryVisible = isVisible;
		if (!isVisible)
		{
			// Clear sale data when sale summary is closed
			_selectedSale = null;
			_selectedSaleDetails.Clear();
		}
		StateHasChanged();
	}
	#endregion

	#region Dialog Management
	private async Task CloseDialog()
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
	}

	private async Task ShowErrorToast(string message)
	{
		_sfErrorToast.Content = message;
		await _sfErrorToast.ShowAsync();
	}
	#endregion
}