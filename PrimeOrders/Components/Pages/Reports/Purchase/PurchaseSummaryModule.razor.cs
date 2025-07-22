using PrimeOrdersLibrary.Data.Accounts.FinancialAccounting;
using PrimeOrdersLibrary.Exporting.Purchase;

using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports.Purchase;

public partial class PurchaseSummaryModule
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public PurchaseOverviewModel SelectedPurchase { get; set; }
	[Parameter] public List<PurchaseDetailDisplayModel> PurchaseDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private SfDialog _sfPurchaseSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;

	private async Task CloseDialog()
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
	}

	private async Task PrintInvoice()
	{
		var memoryStream = await PurchaseA4Print.GenerateA4PurchaseBill(SelectedPurchase.PurchaseId);
		var fileName = $"Purchase_Invoice_{SelectedPurchase.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await JS.InvokeVoidAsync("savePDF", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}

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

	private async Task ConfirmDeletePurchase()
	{
		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, SelectedPurchase.PurchaseId);
		if (purchase is null)
		{
			_sfErrorToast.Content = "Purchase not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		purchase.Status = false;
		await PurchaseData.InsertPurchase(purchase);
		await StockData.DeleteRawMaterialStockByTransactionNo(purchase.BillNo);

		var accounting = await AccountingData.LoadAccountingByReferenceNo(purchase.BillNo);
		accounting.Status = false;
		await AccountingData.InsertAccounting(accounting);

		_sfSuccessToast.Content = "Purchase deactivated successfully.";
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
}