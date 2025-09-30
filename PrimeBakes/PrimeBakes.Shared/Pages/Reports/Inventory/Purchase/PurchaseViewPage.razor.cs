using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Purchase;
using PrimeBakesLibrary.Models.Inventory;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseViewPage : ComponentBase
{
	[Parameter] public int PurchaseId { get; set; }

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleteDialog = false;
	private PurchaseOverviewModel _purchaseOverview;
	private List<PurchaseDetailModel> _purchaseDetails = new();
	private List<RawMaterialModel> _rawMaterials = new();

	private Syncfusion.Blazor.Popups.SfDialog _confirmDeleteDialog;

	protected override async Task OnInitializedAsync()
	{
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

			// Load raw materials for name mapping - this should be from CommonData
			// For now, we'll use an empty list and show IDs
			_rawMaterials = [];
		}

		_isLoading = false;
		StateHasChanged();
	}

	private string GetRawMaterialName(int rawMaterialId)
	{
		var rawMaterial = _rawMaterials.FirstOrDefault(rm => rm.Id == rawMaterialId);
		return rawMaterial?.Name ?? $"Raw Material ID: {rawMaterialId}";
	}

	private void EditPurchase() =>
		NavigationManager.NavigateTo($"/Inventory/Purchase/Edit/{PurchaseId}");

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
		try
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
			await StockData.DeleteRawMaterialStockByTransactionNo(purchase.BillNo);

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
		}
		catch (Exception ex)
		{
			await NotificationService.ShowLocalNotification(
				PurchaseId,
				"Error",
				"Delete Error",
				$"Error deleting purchase: {ex.Message}"
			);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task PrintPurchase()
	{
		var ms = await PurchaseA4Print.GenerateA4PurchaseBill(_purchaseOverview.PurchaseId);
		var fileName = $"Purchase_Bill_{_purchaseOverview.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", ms);
	}
}