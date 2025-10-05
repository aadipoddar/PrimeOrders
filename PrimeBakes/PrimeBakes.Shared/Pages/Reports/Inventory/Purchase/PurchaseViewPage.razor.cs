using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Purchase;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseViewPage : ComponentBase
{
	[Parameter] public int PurchaseId { get; set; }

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleteDialog = false;
	private PurchaseOverviewModel _purchaseOverview;
	private List<PurchaseDetailModel> _purchaseDetails = [];
	private List<RawMaterialModel> _rawMaterials = [];

	private Syncfusion.Blazor.Popups.SfDialog _confirmDeleteDialog;

	protected override async Task OnInitializedAsync()
	{
		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadData();
	}

	private async Task LoadData()
	{
		_isLoading = true;
		StateHasChanged();

		// Load specific purchase overview
		_purchaseOverview = await PurchaseData.LoadPurchaseOverviewByPurchaseId(PurchaseId);

		if (_purchaseOverview != null)
		{
			// Load detailed purchase items
			_purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(PurchaseId);

			_rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
		}

		_isLoading = false;
		StateHasChanged();
	}

	private string GetRawMaterialName(int rawMaterialId)
	{
		var rawMaterial = _rawMaterials.FirstOrDefault(rm => rm.Id == rawMaterialId);
		return rawMaterial?.Name ?? $"Raw Material ID: {rawMaterialId}";
	}

	private async Task EditPurchase()
	{
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseCartDataFileName);

		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, _purchaseOverview.PurchaseId);
		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(_purchaseOverview.PurchaseId);

		await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseDataFileName, System.Text.Json.JsonSerializer.Serialize(purchase));
		await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseCartDataFileName, System.Text.Json.JsonSerializer.Serialize(purchaseDetails));

		NavigationManager.NavigateTo("/Inventory/Purchase");
	}

	private void DeletePurchase()
	{
		_showDeleteDialog = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_showDeleteDialog = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		_isProcessing = true;
		StateHasChanged();

		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, PurchaseId);
		if (purchase is null)
		{
			await NotificationService.ShowLocalNotification(
				PurchaseId,
				"Error",
				"Delete Error",
				"Purchase not found."
			);
			return;
		}

		// Mark purchase as deleted
		purchase.Status = false;
		await PurchaseData.InsertPurchase(purchase);

		// Reverse stock entries
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(purchase.BillNo);

		// Mark accounting entries as deleted
		var accounting = await AccountingData.LoadAccountingByReferenceNo(purchase.BillNo);
		if (accounting != null)
		{
			accounting.Status = false;
			await AccountingData.InsertAccounting(accounting);
		}

		// Show success notification
		await NotificationService.ShowLocalNotification(
			PurchaseId,
			"Success",
			"Purchase Deleted",
			$"Purchase {_purchaseOverview.BillNo} has been successfully deleted."
		);

		// Close dialog and navigate back
		_showDeleteDialog = false;
		NavigationManager.NavigateTo("/Reports/Purchase");

		_isProcessing = false;
		StateHasChanged();
	}

	private async Task PrintPurchase()
	{
		var ms = await PurchaseA4Print.GenerateA4PurchaseBill(_purchaseOverview.PurchaseId);
		var fileName = $"Purchase_Bill_{_purchaseOverview.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", ms);
	}
}