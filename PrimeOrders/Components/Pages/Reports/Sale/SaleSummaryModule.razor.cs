using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Sale;

using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports.Sale;

public partial class SaleSummaryModule
{
	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public SaleOverviewModel SelectedSale { get; set; }
	[Parameter] public List<SaleDetailDisplayModel> SaleDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	private SfDialog _sfSaleSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfDialog _sfUnlinkOrderConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;
	private bool _unlinkOrderConfirmationDialogVisible = false;

	private async Task CloseDialog()
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
	}

	#region Printing
	private async Task PrintInvoice()
	{
		var memoryStream = await SaleA4Print.GenerateA4SaleBill(SelectedSale.SaleId);
		var fileName = $"Sale_Bill_{SelectedSale.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await JS.InvokeVoidAsync("savePDF", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

	private async Task PrintThermalReceipt()
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SelectedSale.SaleId);
		if (sale is null)
		{
			_sfErrorToast.Content = "Sale not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		var content = await SaleThermalPrint.GenerateThermalBill(sale);
		await JS.InvokeVoidAsync("printToPrinter", content.ToString());

		_sfSuccessToast.Content = "Thermal receipt printed successfully.";
		await _sfSuccessToast.ShowAsync();
	}
	#endregion

	#region Delete Sale
	private void ShowDeleteConfirmation()
	{
		if (!CurrentUser.Admin)
		{
			_sfErrorToast.Content = "Only administrators can delete records.";
			_sfErrorToast.ShowAsync();
			return;
		}

		_deleteConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmDeleteSale()
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SelectedSale.SaleId);
		if (sale is null)
		{
			_sfErrorToast.Content = "Sale not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		sale.Status = false;
		await SaleData.InsertSale(sale);
		await StockData.DeleteProductStockByTransactionNo(sale.BillNo);

		if (sale.LocationId == 1)
		{
			var accounting = await AccountingData.LoadAccountingByReferenceNo(sale.BillNo);
			accounting.Status = false;
			await AccountingData.InsertAccounting(accounting);
		}

		_sfSuccessToast.Content = "Sale deactivated successfully.";
		await _sfSuccessToast.ShowAsync();

		_deleteConfirmationDialogVisible = false;
		await CloseDialog();
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteConfirmationDialogVisible = false;
		StateHasChanged();
	}
	#endregion

	#region Unlink Order
	private void ShowUnlinkOrderConfirmation()
	{
		if (!CurrentUser.Admin)
		{
			_sfErrorToast.Content = "Only administrators can unlink orders.";
			_sfErrorToast.ShowAsync();
			return;
		}

		if (!SelectedSale.OrderId.HasValue)
		{
			_sfErrorToast.Content = "No order is linked to this sale.";
			_sfErrorToast.ShowAsync();
			return;
		}

		_unlinkOrderConfirmationDialogVisible = true;
		StateHasChanged();
	}

	private async Task ConfirmUnlinkOrder()
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SelectedSale.SaleId);
		if (sale is null)
		{
			_sfErrorToast.Content = "Sale not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if (!sale.OrderId.HasValue)
		{
			_sfErrorToast.Content = "No order is linked to this sale.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is null)
		{
			_sfErrorToast.Content = "Linked order not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		order.SaleId = null;
		await OrderData.InsertOrder(order);

		sale.OrderId = null;
		await SaleData.InsertSale(sale);

		_sfSuccessToast.Content = "Order unlinked successfully. The order is now available as pending.";
		await _sfSuccessToast.ShowAsync();

		_unlinkOrderConfirmationDialogVisible = false;
		await CloseDialog();
		StateHasChanged();
	}

	private void CancelUnlinkOrder()
	{
		_unlinkOrderConfirmationDialogVisible = false;
		StateHasChanged();
	}
	#endregion
}