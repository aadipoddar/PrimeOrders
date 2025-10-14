using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingPage
{
	private bool _isLoading = true;
	private bool _isRetrieving = false;
	private bool _isSaving = false;

	private UserModel _user;
	private FinancialYearModel _financialYear;
	private LedgerModel _selectedLedger;
	private LedgerOverviewModel? _selectedLedgerReference;
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
		if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingDataFileName))
			_accounting = System.Text.Json.JsonSerializer.Deserialize<AccountingModel>(
				await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingDataFileName));

		else
		{
			_accounting.UserId = _user.Id;
			_accounting.AccountingDate = DateOnly.FromDateTime(DateTime.Now);
			_accounting.VoucherId = _vouchers.FirstOrDefault().Id;
			_accounting.GeneratedModule = GeneratedModules.FinancialAccounting.ToString();
			_accounting.Status = true;
			_accounting.Remarks = string.Empty;
			_accounting.CreatedAt = DateTime.Now;
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

		_selectedLedgerReference = null;
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

		List<LedgerOverviewModel> unbalancedLedgers = [];
		foreach (var item in _ledgerReferences)
		{
			if (unbalancedLedgers.Where(x => x.ReferenceNo == item.ReferenceNo && x.ReferenceType == item.ReferenceType && x.ReferenceId == item.ReferenceId).FirstOrDefault() is not null)
			{
				unbalancedLedgers.Where(x => x.ReferenceNo == item.ReferenceNo && x.ReferenceType == item.ReferenceType && x.ReferenceId == item.ReferenceId).FirstOrDefault().Credit
					= unbalancedLedgers.Where(x => x.ReferenceNo == item.ReferenceNo && x.ReferenceType == item.ReferenceType && x.ReferenceId == item.ReferenceId).FirstOrDefault().Credit + (item.Credit ?? 0);
				unbalancedLedgers.Where(x => x.ReferenceNo == item.ReferenceNo && x.ReferenceType == item.ReferenceType && x.ReferenceId == item.ReferenceId).FirstOrDefault().Debit
					= unbalancedLedgers.Where(x => x.ReferenceNo == item.ReferenceNo && x.ReferenceType == item.ReferenceType && x.ReferenceId == item.ReferenceId).FirstOrDefault().Debit + (item.Debit ?? 0);
			}
			else
				unbalancedLedgers.Add(item);
		}

		_ledgerReferences = [.. unbalancedLedgers.Where(x => (x.Debit ?? 0) != (x.Credit ?? 0))];
		_isRetrieving = false;

		StateHasChanged();
	}

	private void OnReferenceChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerOverviewModel?, LedgerOverviewModel> args)
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
		else
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
			return;

		_accountingCart.Add(_selectedCart);

		await SaveAccountingToCart();
	}
	#endregion

	#region Saving
	private async Task SaveAccountingToCart()
	{
		_financialYear = await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate);
		_accounting.FinancialYearId = _financialYear.Id;
		_accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(_accounting);

		await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName, System.Text.Json.JsonSerializer.Serialize(_accounting));
		await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_accountingCart));

		if (_sfAccountingCart is not null)
			await _sfAccountingCart.Refresh();

		StateHasChanged();
	}

	private async Task SaveAccountingEntry()
	{

	}

	private async Task<bool> ValidateForm()
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

	private async Task ConfirmAccountingEntry()
	{
		if (_isRetrieving || _accountingCart.Count == 0 || _balance != 0 || _isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveAccountingToCart();

			if (!await ValidateForm())
				return;

			_accounting.Id = await AccountingData.SaveAccountingTransaction(_accounting, _accountingCart);
			await DeleteCart();
			NavigationManager.NavigateTo("/Accounting/Confirmed");
		}
		catch (Exception ex)
		{
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task DeleteCart()
	{
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
	}
	#endregion
}