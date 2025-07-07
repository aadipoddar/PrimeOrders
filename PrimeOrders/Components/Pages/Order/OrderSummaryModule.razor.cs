using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Order;

public partial class OrderSummaryModule
{
	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public OrderOverviewModel SelectedOrder { get; set; }
	[Parameter] public List<OrderData.OrderDetailDisplayModel> OrderDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	private SfDialog _sfOrderSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;
	private int _orderToDeleteId = 0;
	private string _orderToDeleteNo = "";

	#region Order Actions
	private async Task ExportOrderChallan()
	{
		var orderDetails = await OrderData.LoadOrderDetailByOrder(SelectedOrder.OrderId);
		if (orderDetails is null || orderDetails.Count == 0)
		{
			await ShowErrorToast("No order details found to export");
			return;
		}

		var memoryStream = await OrderExcelExport.ExportOrderChallanExcel(SelectedOrder, orderDetails);
		var fileName = $"Challan_Order_{SelectedOrder.OrderNo}_{SelectedOrder.OrderDate:yyyy-MM-dd}.xlsx";
		await JS.InvokeVoidAsync("saveAs", Convert.ToBase64String(memoryStream.ToArray()), fileName);

		await CloseDialog();
	}

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