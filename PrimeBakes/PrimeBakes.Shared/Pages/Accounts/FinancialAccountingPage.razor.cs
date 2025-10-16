using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingPage
{
	private bool _isLoading = true;
	private bool _isRetrieving = false;
	private bool _isSaving = false;
	private bool _showConfirmDialog = false;

	private UserModel _user;
	private FinancialYearModel _financialYear;
	private LedgerModel _selectedLedger;
	private LedgerOverviewModel _selectedLedgerReference;
	private AccountingCartModel _selectedCart;
	private AccountingModel _accounting = new()
	{
		Id = 0,
		TransactionNo = string.Empty,
		VoucherId = 0,
		AccountingDate = DateOnly.FromDateTime(DateTime.Now),
		CreatedAt = DateTime.Now,
		FinancialYearId = 0,
		GeneratedModule = GeneratedModules.FinancialAccounting.ToString(),
		Remarks = string.Empty,
		UserId = 0,
		Status = true,
	};

	private List<LedgerModel> _ledgers;
	private List<VoucherModel> _vouchers;
	private List<LedgerOverviewModel> _ledgerReferences = [];
	private List<AccountingCartModel> _accountingCart = [];

	private SfGrid<AccountingCartModel> _sfAccountingCart;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;
	private SfToast _sfWarningToast;
	private SfToast _sfInfoToast;
	private SfDialog _saveConfirmDialog;
	private SfDialog _clearConfirmDialog;
	private SfDialog _removeConfirmDialog;

	private string _saveDialogContent = string.Empty;
	private string _removeDialogContent = string.Empty;
	private AccountingCartModel _itemToRemove;

	// Semaphore to prevent concurrent file access
	private static readonly SemaphoreSlim _fileSemaphore = new(1, 1);

	private decimal _totalDebit => _accountingCart.Sum(x => x.Debit ?? 0);
	private decimal _totalCredit => _accountingCart.Sum(x => x.Credit ?? 0);
	private decimal _balance => _totalDebit - _totalCredit;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(TableNames.Voucher);
		_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);

		await LoadAccountingFromCart();
	}

	private async Task LoadAccountingFromCart()
	{
		await _fileSemaphore.WaitAsync();
		try
		{
			if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingDataFileName))
			{
				var accountingData = await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingDataFileName);
				if (!string.IsNullOrEmpty(accountingData))
					_accounting = System.Text.Json.JsonSerializer.Deserialize<AccountingModel>(accountingData);
			}
			else
			{
				_accounting.UserId = _user.Id;
				_accounting.AccountingDate = DateOnly.FromDateTime(DateTime.Now);
				_accounting.VoucherId = _vouchers?.FirstOrDefault()?.Id ?? 0;
				_accounting.GeneratedModule = GeneratedModules.FinancialAccounting.ToString();
				_accounting.Status = true;
				_accounting.Remarks = string.Empty;
				_accounting.CreatedAt = DateTime.Now;
			}

			// Load cart data if exists
			if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
			{
				var cartData = await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingCartDataFileName);
				if (!string.IsNullOrEmpty(cartData))
					_accountingCart = System.Text.Json.JsonSerializer.Deserialize<List<AccountingCartModel>>(cartData) ?? [];
			}
		}
		catch (IOException ex)
		{
			await ShowErrorToast($"File access error while loading data: {ex.Message}");
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Error loading data: {ex.Message}");
		}
		finally
		{
			_fileSemaphore.Release();
		}

		await SaveAccountingToCart();
	}
	#endregion

	#region Event Change
	private async Task OnAccountingDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateOnly> args)
	{
		_accounting.AccountingDate = args.Value;
		_financialYear = await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate);
		_accounting.FinancialYearId = _financialYear.Id;
		await SaveAccountingToCart();
	}

	private async Task OnVoucherTypeChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, VoucherModel> args)
	{
		_accounting.VoucherId = args.Value;
		await SaveAccountingToCart();
	}

	private async Task OnLedgerChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		_selectedLedger = args.Value;

		if (_selectedLedger is not null)
			_selectedCart = new()
			{
				Id = _selectedLedger.Id,
				Name = _selectedLedger.Name,
				GroupId = _selectedLedger.GroupId,
				AccountTypeId = _selectedLedger.AccountTypeId,
				ReferenceId = null,
				ReferenceType = null,
				ReferenceNo = null,
				Debit = null,
				Credit = null,
				Remarks = string.Empty
			};
		else
			_selectedCart = null;

		_selectedLedgerReference = default;
		_ledgerReferences = [];

		await SaveAccountingToCart();
	}

	private void OnDebitChanged(Syncfusion.Blazor.Inputs.ChangeEventArgs<decimal?> args)
	{
		if (_selectedCart is not null)
			_selectedCart.Debit = args.Value;

		StateHasChanged();
	}

	private void OnCreditChanged(Syncfusion.Blazor.Inputs.ChangeEventArgs<decimal?> args)
	{
		if (_selectedCart is not null)
			_selectedCart.Credit = args.Value;

		StateHasChanged();
	}

	private void OnRemarksChanged(string args)
	{
		if (_selectedCart is not null)
			_selectedCart.Remarks = args ?? string.Empty;

		StateHasChanged();
	}

	private async Task RetrieveReferences()
	{
		if (_selectedCart is null)
		{
			await ShowWarningToast("Please select a ledger first.");
			return;
		}

		if (_isRetrieving)
		{
			await ShowInfoToast("Already retrieving references, please wait...");
			return;
		}

		_isRetrieving = true;
		StateHasChanged();

		try
		{
			_ledgerReferences = await LedgerData.LoadLedgerDetailsByDateLedger(
				_accounting.AccountingDate.AddYears(-10).ToDateTime(TimeOnly.MinValue),
				_accounting.AccountingDate.ToDateTime(TimeOnly.MaxValue),
				_selectedCart.Id);

			var ledgerGroups = new Dictionary<(string ReferenceNo, string ReferenceType, int? ReferenceId), LedgerOverviewModel>();

			foreach (var item in _ledgerReferences)
			{
				var key = (item.ReferenceNo, item.ReferenceType, item.ReferenceId);

				if (ledgerGroups.TryGetValue(key, out var existingLedger))
				{
					existingLedger.Credit = (existingLedger.Credit ?? 0) + (item.Credit ?? 0);
					existingLedger.Debit = (existingLedger.Debit ?? 0) + (item.Debit ?? 0);
				}
				else
					ledgerGroups[key] = item;
			}

			_ledgerReferences = [.. ledgerGroups.Values.Where(x => ((x.Debit ?? 0) != (x.Credit ?? 0)) && (x.ReferenceId is not null) && (x.ReferenceNo is not null))];

			if (_ledgerReferences.Count > 0)
				await ShowSuccessToast($"Found {_ledgerReferences.Count} reference(s) for {_selectedCart.Name}.");
			else
				await ShowInfoToast($"No outstanding references found for {_selectedCart.Name}.");
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Error retrieving references: {ex.Message}");
			_ledgerReferences = [];
		}
		finally
		{
			_isRetrieving = false;
			StateHasChanged();
		}
	}

	private void OnReferenceChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerOverviewModel, LedgerOverviewModel> args)
	{
		_selectedLedgerReference = args.Value;
		if (_selectedCart is not null && _selectedLedgerReference is not null)
		{
			_selectedCart.Credit = null;
			_selectedCart.Debit = null;
			StateHasChanged();

			_selectedCart.ReferenceId = _selectedLedgerReference.ReferenceId;
			_selectedCart.ReferenceType = _selectedLedgerReference.ReferenceType;
			_selectedCart.ReferenceNo = _selectedLedgerReference.ReferenceNo;

			if ((_selectedLedgerReference.Debit ?? 0) > (_selectedLedgerReference.Credit ?? 0))
				_selectedCart.Credit = (_selectedLedgerReference.Debit ?? 0) - (_selectedLedgerReference.Credit ?? 0);
			else
				_selectedCart.Debit = (_selectedLedgerReference.Credit ?? 0) - (_selectedLedgerReference.Debit ?? 0);
		}
		else if (_selectedCart is not null)
		{
			_selectedCart.ReferenceId = null;
			_selectedCart.ReferenceType = null;
			_selectedCart.ReferenceNo = null;
		}

		StateHasChanged();
	}
	#endregion

	#region Cart
	private async Task AddToCart()
	{
		if (_selectedCart.Debit is not null && _selectedCart.Debit == 0)
			_selectedCart.Debit = null;

		if (_selectedCart.Credit is not null && _selectedCart.Credit == 0)
			_selectedCart.Credit = null;

		if (_isRetrieving)
		{
			await ShowWarningToast("Please wait for the current operation to complete.");
			return;
		}

		if (_selectedCart is null)
		{
			await ShowErrorToast("Please select a ledger first.");
			return;
		}

		if (_selectedCart.Debit is null && _selectedCart.Credit is null)
		{
			await ShowErrorToast("Please enter either a debit or credit amount.");
			return;
		}

		if (_selectedCart.Debit is not null && _selectedCart.Credit is not null)
		{
			await ShowErrorToast("Please enter either debit OR credit amount, not both.");
			return;
		}

		if ((_selectedCart.Debit ?? 0) <= 0 && (_selectedCart.Credit ?? 0) <= 0)
		{
			await ShowErrorToast("Amount must be greater than zero.");
			return;
		}

		// Check if ledger already exists in cart
		var existingEntry = _accountingCart.FirstOrDefault(x => x.Id == _selectedCart.Id &&
			x.ReferenceId == _selectedCart.ReferenceId &&
			x.ReferenceType == _selectedCart.ReferenceType);

		if (existingEntry is not null)
		{
			await ShowWarningToast($"An entry for '{_selectedCart.Name}' with the same reference already exists in the cart.");
			return;
		}

		_accountingCart.Add(_selectedCart);
		await ShowSuccessToast($"Added {_selectedCart.Name} (₹{(_selectedCart.Debit ?? _selectedCart.Credit):N2}) to cart.");

		// Reset selection
		_selectedLedger = default;
		_selectedCart = null;
		_selectedLedgerReference = default;
		_ledgerReferences = [];

		await SaveAccountingToCart();
	}

	private async Task ClearCart()
	{
		var itemCount = _accountingCart.Count;
		_accountingCart.Clear();
		_selectedLedger = default;
		_selectedCart = null;
		_selectedLedgerReference = default;
		_ledgerReferences = [];

		await ShowSuccessToast($"Cleared {itemCount} entries from cart.");
		await SaveAccountingToCart();
	}

	private async Task EditCartItem(AccountingCartModel item)
	{
		// Remove the item from cart first
		_accountingCart.Remove(item);

		// Find the corresponding ledger
		var ledger = _ledgers.FirstOrDefault(l => l.Id == item.Id);
		if (ledger != null)
		{
			// Set the selected ledger and cart with the item's data
			_selectedLedger = ledger;
			_selectedCart = new()
			{
				Id = item.Id,
				Name = item.Name,
				GroupId = item.GroupId,
				AccountTypeId = item.AccountTypeId,
				ReferenceId = item.ReferenceId,
				ReferenceType = item.ReferenceType,
				ReferenceNo = item.ReferenceNo,
				Debit = item.Debit,
				Credit = item.Credit,
				Remarks = item.Remarks
			};

			// If there was a reference, try to load the reference data
			if (item.ReferenceId.HasValue && !string.IsNullOrEmpty(item.ReferenceType))
			{
				await RetrieveReferences();
				_selectedLedgerReference = _ledgerReferences.FirstOrDefault(r =>
					r.ReferenceId == item.ReferenceId &&
					r.ReferenceType == item.ReferenceType);
			}

			await ShowInfoToast($"Loaded {item.Name} for editing. Modify the values and click 'Add to Cart' to update.");
		}
		else
		{
			await ShowErrorToast($"Could not find ledger for {item.Name}.");
		}

		await SaveAccountingToCart();
		StateHasChanged();
	}
	#endregion

	#region Dialog Methods
	private async Task ShowSaveConfirmDialog()
	{
		if (!ValidateForm())
		{
			string errorMessage = GetValidationErrorMessage();
			await ShowErrorToast(errorMessage);
			return;
		}

		_saveDialogContent = $"Are you sure you want to save this transaction?\n\n" +
							$"Transaction No: {_accounting.TransactionNo}\n" +
							$"Total Entries: {_accountingCart.Count}\n" +
							$"Total Debit: ₹{_totalDebit:N2}\n" +
							$"Total Credit: ₹{_totalCredit:N2}\n" +
							$"Balance: ₹{_balance:N2}";

		await _saveConfirmDialog.ShowAsync();
	}

	private async Task ShowRemoveConfirmDialog(AccountingCartModel item)
	{
		_itemToRemove = item;
		_removeDialogContent = $"Are you sure you want to remove the entry for '{item.Name}'?";
		await _removeConfirmDialog.ShowAsync();
	}

	private async Task HideRemoveConfirmDialog()
	{
		_itemToRemove = null;
		await _removeConfirmDialog.HideAsync();
	}

	private async Task ConfirmRemoveEntry()
	{
		if (_itemToRemove != null)
		{
			_accountingCart.Remove(_itemToRemove);
			await ShowSuccessToast($"Removed {_itemToRemove.Name} from cart.");
			await SaveAccountingToCart();
		}
		await HideRemoveConfirmDialog();
	}

	private async Task ConfirmClearCart()
	{
		await ClearCart();
		await _clearConfirmDialog.HideAsync();
	}
	#endregion

	#region Saving
	private async Task SaveAccountingToCart()
	{
		await _fileSemaphore.WaitAsync();
		try
		{
			_financialYear = await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate);
			_accounting.FinancialYearId = _financialYear.Id;
			_accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(_accounting);

			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName, System.Text.Json.JsonSerializer.Serialize(_accounting));

			if (_accountingCart.Count == 0 && await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_accountingCart));

			if (_sfAccountingCart is not null)
				await _sfAccountingCart.Refresh();

			StateHasChanged();
		}
		catch (IOException ex)
		{
			await ShowErrorToast($"File access error: {ex.Message}. Please try again.");
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Error saving data: {ex.Message}");
		}
		finally
		{
			_fileSemaphore.Release();
		}
	}

	public bool ValidateForm()
	{
		if (_accountingCart.Count == 0)
			return false;

		if (_balance != 0)
			return false;

		if (_financialYear is null || _financialYear.Id == 0)
			return false;

		if (_accounting.VoucherId == 0)
			return false;

		if (_accounting.AccountingDate < _financialYear.StartDate || _accounting.AccountingDate > _financialYear.EndDate)
			return false;

		if (string.IsNullOrEmpty(_accounting.TransactionNo))
			return false;

		return true;
	}

	private string GetValidationErrorMessage()
	{
		if (_accountingCart.Count == 0)
			return "Please add at least one ledger entry to the cart.";

		if (_balance != 0)
			return $"The transaction is not balanced. Difference: {_balance:N2}";

		if (_financialYear is null || _financialYear.Id == 0)
			return "Please select a valid accounting date with an active financial year.";

		if (_accounting.VoucherId == 0)
			return "Please select a voucher type.";

		if (_accounting.AccountingDate < _financialYear.StartDate || _accounting.AccountingDate > _financialYear.EndDate)
			return "The accounting date must be within the financial year period.";

		if (string.IsNullOrEmpty(_accounting.TransactionNo))
			return "Transaction number is missing.";

		return "Unknown validation error.";
	}

	private async Task ConfirmAccountingEntry()
	{
		if (_isRetrieving || _accountingCart.Count == 0 || _balance != 0 || _isSaving)
			return;

		await _saveConfirmDialog.HideAsync();
		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveAccountingToCart();

			if (!ValidateForm())
			{
				await ShowErrorToast(GetValidationErrorMessage());
				return;
			}

			_accounting.Id = await AccountingData.SaveAccountingTransaction(_accounting, _accountingCart);
			await ShowSuccessToast($"Transaction {_accounting.TransactionNo} saved successfully!");
			await PrintInvoice();
			await DeleteCart();
			NavigationManager.NavigateTo("/Accounting/Confirmed");
		}
		catch (Exception ex)
		{
			await ShowErrorToast($"Failed to save transaction: {ex.Message}");
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
		var fileName = $"AccountingVoucher_{_accounting.TransactionNo}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);
	}

	private async Task DeleteCart()
	{
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
	}
	#endregion

	#region Toast Notifications

	private async Task ShowSuccessToast(string message)
	{
		if (_sfToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Success",
				Content = message,
				CssClass = "e-toast-success",
				Icon = "e-success toast-icons",
				ShowCloseButton = true,
				Timeout = 3000
			};
			await _sfToast.ShowAsync(toastModel);
		}
	}

	private async Task ShowErrorToast(string message)
	{
		if (_sfErrorToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Error",
				Content = message,
				CssClass = "e-toast-danger",
				Icon = "e-error toast-icons",
				ShowCloseButton = true,
				Timeout = 5000
			};
			await _sfErrorToast.ShowAsync(toastModel);
		}
	}

	private async Task ShowInfoToast(string message)
	{
		if (_sfInfoToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Information",
				Content = message,
				CssClass = "e-toast-info",
				Icon = "e-info toast-icons",
				ShowCloseButton = true,
				Timeout = 4000
			};
			await _sfInfoToast.ShowAsync(toastModel);
		}
	}

	private async Task ShowWarningToast(string message)
	{
		if (_sfWarningToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Warning",
				Content = message,
				CssClass = "e-toast-warning",
				Icon = "e-warning toast-icons",
				ShowCloseButton = true,
				Timeout = 4000
			};
			await _sfWarningToast.ShowAsync(toastModel);
		}
	}
	#endregion
}