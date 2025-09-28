using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;

using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Accounts.Reports;

public partial class AccountingSummaryModule
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
	[Parameter] public AccountingOverviewModel SelectedAccounting { get; set; }
	[Parameter] public List<AccountingCartModel> AccountingDetails { get; set; }
	[Parameter] public UserModel CurrentUser { get; set; }

	[Inject] private NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private SfDialog _sfAccountingSummaryModuleDialog;
	private SfDialog _sfDeleteConfirmationDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private bool _deleteConfirmationDialogVisible = false;

	private async Task CloseDialog()
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
	}

	private async Task PrintVoucher()
	{
		var memoryStream = await AccountingA4Print.GenerateA4AccountingVoucher(SelectedAccounting.AccountingId);
		var fileName = $"Accounting_Voucher_{SelectedAccounting.ReferenceNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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

	private async Task ConfirmDeleteVoucher()
	{
		var accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, SelectedAccounting.AccountingId);
		if (accounting is null)
		{
			_sfErrorToast.Content = "Accounting voucher not found.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		accounting.Status = false;
		await AccountingData.InsertAccounting(accounting);

		_sfSuccessToast.Content = "Accounting voucher deactivated successfully.";
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