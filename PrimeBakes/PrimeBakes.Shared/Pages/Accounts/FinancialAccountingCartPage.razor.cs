using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingCartPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private AccountingModel _accounting = new();

	private List<VoucherModel> _vouchers = [];
	private readonly List<AccountingCartModel> _cart = [];
	private SfGrid<AccountingCartModel> _sfGrid;

	// Dialog references and visibility
	private SfDialog _transactionDetailsDialog;
	private SfDialog _editEntryDialog;
	private SfDialog _confirmationDialog;
	private bool _transactionDetailsDialogVisible = false;
	private bool _editEntryDialogVisible = false;
	private bool _confirmationDialogVisible = false;

	// Edit entry properties
	private AccountingCartModel _selectedEntry;
	private decimal? _editDebitAmount;
	private decimal? _editCreditAmount;
	private string _editRemarks = string.Empty;

	private decimal TotalDebit => _cart.Sum(x => x.Debit) ?? 0;
	private decimal TotalCredit => _cart.Sum(x => x.Credit) ?? 0;
	private decimal BalanceDifference => TotalDebit - TotalCredit;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		_user = authResult.User;

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadVouchers();
		await LoadCartData();
		StateHasChanged();
	}

	private async Task LoadVouchers()
	{
		_vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(TableNames.Voucher, true);
		_vouchers.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
	}

	private async Task LoadCartData()
	{
		if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
		{
			var cartJson = await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingCartDataFileName);
			var items = System.Text.Json.JsonSerializer.Deserialize<List<AccountingCartModel>>(cartJson) ?? [];

			_cart.Clear();
			_cart.AddRange(items.Where(x => (x.Debit ?? 0) > 0 || (x.Credit ?? 0) > 0));
		}

		// Load accounting model data
		if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingDataFileName))
		{
			var accountingJson = await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingDataFileName);
			var accounting = System.Text.Json.JsonSerializer.Deserialize<AccountingModel>(accountingJson);
			if (accounting != null)
			{
				_accounting = accounting;
			}
		}
		else
		{
			// Initialize with default values
			_accounting = new AccountingModel
			{
				Id = 0,
				CreatedAt = DateTime.Now,
				UserId = _user.Id,
				AccountingDate = DateOnly.FromDateTime(DateTime.Now),
				VoucherId = 0,
				Remarks = string.Empty
			};
		}
	}
	#endregion

	#region Cart
	private async Task RemoveFromCart(AccountingCartModel item)
	{
		if (_isSaving) return;

		_cart.Remove(item);
		await SaveCartData();
		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task SaveCartData()
	{
		if (_cart.Count == 0)
		{
			if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
		}
		else
		{
			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName,
				System.Text.Json.JsonSerializer.Serialize(_cart));
		}
	}

	private void OpenEditDialog(AccountingCartModel entry)
	{
		if (_isSaving) return;

		_selectedEntry = entry;
		_editDebitAmount = entry.Debit;
		_editCreditAmount = entry.Credit;
		_editRemarks = entry.Remarks ?? string.Empty;
		_editEntryDialogVisible = true;
		VibrationService.VibrateHapticClick();
	}
	#endregion

	#region Saving
	// Dialog Methods
	private async Task SaveTransactionDetails()
	{
		// Save accounting model to local storage
		await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName,
			System.Text.Json.JsonSerializer.Serialize(_accounting));

		_transactionDetailsDialogVisible = false;
		VibrationService.VibrateHapticClick();
		StateHasChanged();
	}

	private async Task SaveEntryChanges()
	{
		if (_selectedEntry == null || _isSaving) return;

		// Validate that only one amount is entered
		if ((_editDebitAmount ?? 0) > 0 && (_editCreditAmount ?? 0) > 0)
		{
			// Show validation error through haptic feedback
			VibrationService.VibrateWithTime(300);
			return;
		}

		if ((_editDebitAmount ?? 0) == 0 && (_editCreditAmount ?? 0) == 0)
		{
			// Show validation error through haptic feedback
			VibrationService.VibrateWithTime(300);
			return;
		}

		// Update the entry
		_selectedEntry.Debit = (_editDebitAmount ?? 0) > 0 ? _editDebitAmount : null;
		_selectedEntry.Credit = (_editCreditAmount ?? 0) > 0 ? _editCreditAmount : null;
		_selectedEntry.Remarks = _editRemarks;

		// Save changes
		await SaveCartData();
		_editEntryDialogVisible = false;
		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	// Confirmation dialog methods
	private void ShowConfirmationDialog()
	{
		// Final validation before showing confirmation
		if (_isSaving || BalanceDifference != 0 || _accounting.VoucherId == 0 || !_cart.Any())
		{
			// Provide haptic feedback for invalid state
			VibrationService.VibrateWithTime(300);
			return;
		}

		// Show confirmation dialog
		_confirmationDialogVisible = true;
		VibrationService.VibrateHapticClick();
		StateHasChanged();
	}

	private void CloseConfirmationDialog()
	{
		_confirmationDialogVisible = false;
		StateHasChanged();
	}

	private async Task SubmitTransaction()
	{
		if (_isSaving || BalanceDifference != 0 || _accounting.VoucherId == 0 || !_cart.Any())
			return;

		try
		{
			_isSaving = true;
			_confirmationDialogVisible = false;
			StateHasChanged();

			// Submit transaction to backend
			_accounting.Id = await AccountingData.SaveAccountingTransaction(_accounting, _cart);
			await PrintInvoice();
			await DeleteCart();
			await ShowLocalNotification();
			NavigationManager.NavigateTo("/Accounting/Confirmed", true);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error submitting transaction: {ex.Message}");
			VibrationService.VibrateWithTime(500);
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task PrintInvoice()
	{
		var memoryStream = await AccountingA4Print.GenerateA4AccountingVoucher(_accounting.Id);
		var fileName = $"Accounting_Voucher_{_accounting.ReferenceNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);
	}

	private async Task DeleteCart()
	{
		_cart.Clear();

		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
	}

	private async Task ShowLocalNotification() =>
		await NotificationService.ShowLocalNotification(
			_accounting.Id,
			 "Accounting Entry Saved",
			 "Financial Accounting",
			 $"Accounting entry {_accounting.ReferenceNo} has been successfully saved, with total debit of {TotalDebit:C} and total credit of {TotalCredit:C}.");
	#endregion
}