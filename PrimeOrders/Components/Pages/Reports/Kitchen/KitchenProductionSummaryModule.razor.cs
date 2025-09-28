using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Kitchen;

using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports.Kitchen;

public partial class KitchenProductionSummaryModule
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public KitchenProductionOverviewModel SelectedKitchenProduction { get; set; }
	[Parameter] public List<KitchenProductionDetailDisplayModel> KitchenProductionDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private SfDialog _sfKitchenProductionSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;

	private async Task CloseDialog()
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
	}

	private async Task PrintReport()
	{
		var memoryStream = await KitchenProductionA4Print.GenerateA4KitchenProductionBill(SelectedKitchenProduction.KitchenProductionId);
		var fileName = $"Kitchen_Production_{SelectedKitchenProduction.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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

	private async Task ConfirmDeleteKitchenProduction()
	{
		var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, SelectedKitchenProduction.KitchenProductionId);
		if (kitchenProduction is null)
		{
			_sfErrorToast.Content = "Kitchen production not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		kitchenProduction.Status = false;
		await KitchenProductionData.InsertKitchenProduction(kitchenProduction);
		await StockData.DeleteProductStockByTransactionNo(kitchenProduction.TransactionNo);

		_sfSuccessToast.Content = "Kitchen production deactivated successfully.";
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