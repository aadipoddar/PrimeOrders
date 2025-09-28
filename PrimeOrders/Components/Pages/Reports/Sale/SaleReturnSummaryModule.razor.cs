using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Sale;

using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Reports.Sale;

public partial class SaleReturnSummaryModule
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public SaleReturnOverviewModel SelectedSaleReturn { get; set; }
	[Parameter] public List<SaleReturnProductCartModel> SaleReturnDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private SfDialog _sfSaleReturnSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;

	private bool _saleSummaryVisible = false;
	private SaleOverviewModel _selectedSale;
	private List<SaleDetailDisplayModel> _selectedSaleDetails = [];

	private async Task CloseDialog()
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
	}

	private async Task PrintReturnReceipt()
	{
		var memoryStream = await SaleReturnA4Print.GenerateA4SaleReturnBill(SelectedSaleReturn.SaleReturnId);
		var fileName = $"Sale_Return_{SelectedSaleReturn.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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

	private async Task ConfirmDeleteSaleReturn()
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SelectedSaleReturn.SaleReturnId);
		if (saleReturn is null)
		{
			_sfErrorToast.Content = "Sale return not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		saleReturn.Status = false;
		await SaleReturnData.InsertSaleReturn(saleReturn);
		await StockData.DeleteProductStockByTransactionNo(saleReturn.TransactionNo);

		var accounting = await AccountingData.LoadAccountingByReferenceNo(saleReturn.TransactionNo);
		accounting.Status = false;
		await AccountingData.InsertAccounting(accounting);

		_sfSuccessToast.Content = "Sale return deleted successfully.";
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

	#region Sale Summary
	private async Task ViewOriginalSale()
	{
		if (SelectedSaleReturn?.SaleId is null || SelectedSaleReturn.SaleId == 0)
		{
			_sfErrorToast.Content = "No original sale is linked to this return.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		_selectedSale = await SaleData.LoadSaleOverviewBySaleId(SelectedSaleReturn.SaleId);
		if (_selectedSale is null)
		{
			_sfErrorToast.Content = "Original sale not found.";
			await _sfErrorToast.ShowAsync();
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

	private string GetProcessingTime()
	{
		if (SelectedSaleReturn?.ReturnDateTime is not null)
		{
			var processingTime = DateTime.Now - SelectedSaleReturn.ReturnDateTime;
			if (processingTime.Days > 0)
				return $"{processingTime.Days} days ago";
			else if (processingTime.Hours > 0)
				return $"{processingTime.Hours} hours ago";
			else
				return $"{processingTime.Minutes} minutes ago";
		}
		return "Unknown";
	}
}