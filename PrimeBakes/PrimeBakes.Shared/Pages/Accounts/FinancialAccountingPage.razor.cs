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
	private string _successMessage = string.Empty;
	private string _errorMessage = string.Empty;

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
				{
					_accounting = System.Text.Json.JsonSerializer.Deserialize<AccountingModel>(accountingData);
				}
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
				{
					_accountingCart = System.Text.Json.JsonSerializer.Deserialize<List<AccountingCartModel>>(cartData) ?? [];
				}
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
				Debit = 0,
				Credit = 0,
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
		{
			_selectedCart.Credit = null;
			_selectedCart.Debit = args.Value;
		}

		StateHasChanged();
	}

	private void OnCreditChanged(Syncfusion.Blazor.Inputs.ChangeEventArgs<decimal?> args)
	{
		if (_selectedCart is not null)
		{
			_selectedCart.Debit = null;
			_selectedCart.Credit = args.Value;
		}

		StateHasChanged();
	}

	private void OnRemarksChanged(string args)
	{
		if (_selectedCart is not null)
			_selectedCart.Remarks = args;

		StateHasChanged();
	}

	private async Task RetrieveReferences()
	{
		if (_selectedCart is null || _isRetrieving)
			return;

		_isRetrieving = true;

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
		_isRetrieving = false;

		StateHasChanged();
	}

	private void OnReferenceChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerOverviewModel, LedgerOverviewModel> args)
	{
		_selectedLedgerReference = args.Value;
		if (_selectedCart is not null && _selectedLedgerReference is not null)
		{
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
		if (_isRetrieving || _selectedCart is null || (_selectedCart.Debit is null && _selectedCart.Credit is null) || (_selectedCart.Debit is not null && _selectedCart.Credit is not null))
		{
			await ShowErrorToast("Please select a ledger and enter either debit or credit amount (not both).");
			return;
		}

		_accountingCart.Add(_selectedCart);
		await ShowSuccessToast($"Added {_selectedCart.Name} to cart.");

		// Reset selection
		_selectedLedger = default;
		_selectedCart = null;
		_selectedLedgerReference = default;
		_ledgerReferences = [];

		await SaveAccountingToCart();
	}

	private async Task RemoveFromCart(int index)
	{
		if (index >= 0 && index < _accountingCart.Count)
		{
			var removedItem = _accountingCart[index];
			_accountingCart.RemoveAt(index);
			await ShowSuccessToast($"Removed {removedItem.Name} from cart.");
			await SaveAccountingToCart();
		}
	}

	private async Task ClearCart()
	{
		_accountingCart.Clear();
		_selectedLedger = default;
		_selectedCart = null;
		_selectedLedgerReference = default;
		_ledgerReferences = [];
		await ShowSuccessToast("Cart has been cleared.");
		await SaveAccountingToCart();
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

	private async Task ShowConfirmationDialog()
	{
		if (!ValidateForm())
		{
			string errorMessage = GetValidationErrorMessage();
			await ShowErrorToast(errorMessage);
			return;
		}

		_showConfirmDialog = true;
		StateHasChanged();
	}

	private bool ValidateForm()
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

		_showConfirmDialog = false;
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
		_successMessage = message;
		StateHasChanged();

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

	private async Task ShowErrorToast(string message)
	{
		_errorMessage = message;
		StateHasChanged();

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
	#endregion
}