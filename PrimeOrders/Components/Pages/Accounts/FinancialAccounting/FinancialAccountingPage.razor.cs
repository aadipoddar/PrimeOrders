using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Exporting.Accounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Accounts.FinancialAccounting;

public partial class FinancialAccountingPage
{
	[Parameter] public int? AccountingId { get; set; }
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;
	private DotNetObjectReference<FinancialAccountingPage> _dotNetHelper;

	// Main models
	private AccountingModel _accounting = new()
	{
		ReferenceNo = "",
		VoucherId = 0,
		Remarks = "",
		AccountingDate = DateOnly.FromDateTime(DateTime.Now),
		FinancialYearId = 0,
		Status = true
	};

	// Data collections
	private List<LedgerModel> _ledgers = [];
	private List<VoucherModel> _vouchers = [];
	private List<FinancialYearModel> _financialYears = [];
	private readonly List<AccountingCartModel> _accountingCart = [];

	// Search functionality
	private bool _isLedgerSearchActive = false;
	private List<LedgerModel> _filteredLedgers = [];
	private LedgerModel _selectedLedger = new();
	private string _ledgerSearchText = "";
	private int _selectedLedgerIndex = 0;

	// Dialog visibility
	private bool _entryDetailsDialogVisible = false;
	private bool _entrySummaryDialogVisible = false;
	private bool _amountDialogVisible = false;
	private bool _entryManageDialogVisible = false;

	// Entry management - Updated for text boxes
	private decimal _selectedDebitAmount = 0;
	private decimal _selectedCreditAmount = 0;
	private string _selectedRemarks = "";
	private AccountingCartModel _selectedAccountingCart = new();

	private SfGrid<LedgerModel> _sfLedgerGrid;
	private SfGrid<AccountingCartModel> _sfAccountingCartGrid;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	private SfDialog _sfAmountDialog;
	private SfDialog _sfEntryManageDialog;
	private SfDialog _sfEntrySummaryDialog;
	private SfDialog _sfEntryDetailsDialog;

	// Balance calculations
	private decimal TotalDebit => _accountingCart.Sum(x => x.Debit ?? 0);
	private decimal TotalCredit => _accountingCart.Sum(x => x.Credit ?? 0);
	private decimal BalanceDifference => TotalDebit - TotalCredit;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Accounts, true)).User) is not null))
			return;

		_dotNetHelper = DotNetObjectReference.Create(this);
		await JS.InvokeVoidAsync("setupAccountingPageKeyboardHandlers", _dotNetHelper);

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
		_vouchers = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);
		_financialYears = await CommonData.LoadTableData<FinancialYearModel>(TableNames.FinancialYear);

		_accounting.VoucherId = _vouchers.FirstOrDefault().Id;
		_accounting.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate)).Id;
		_accounting.ReferenceNo = await GenerateCodes.GenerateAccountingReferenceNo(_accounting.VoucherId, _accounting.AccountingDate);

		if (AccountingId.HasValue && AccountingId.Value > 0)
			await LoadExistingEntry(AccountingId.Value);

		if (_sfLedgerGrid is not null)
			await _sfLedgerGrid.Refresh();

		if (_sfAccountingCartGrid is not null)
			await _sfAccountingCartGrid.Refresh();

		StateHasChanged();
	}

	private async Task LoadExistingEntry(int accountingId)
	{
		var existingEntry = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, accountingId);
		if (existingEntry is not null)
		{
			_accounting = existingEntry;

			var details = await AccountingData.LoadAccountingDetailsByAccounting(accountingId);
			_accountingCart.Clear();

			foreach (var detail in details)
			{
				var ledger = _ledgers.FirstOrDefault(l => l.Id == detail.LedgerId);
				if (ledger is not null)
					_accountingCart.Add(new()
					{
						Serial = _accountingCart.Count + 1,
						Id = ledger.Id,
						Name = ledger.Name,
						Remarks = detail.Remarks,
						Debit = detail.Debit,
						Credit = detail.Credit
					});
			}
		}
	}

	private async Task OnVoucherSelectionChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, VoucherModel> args)
	{
		_accounting.VoucherId = args.Value;
		_accounting.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate)).Id;
		_accounting.ReferenceNo = await GenerateCodes.GenerateAccountingReferenceNo(_accounting.VoucherId, _accounting.AccountingDate);
		StateHasChanged();
	}

	private async Task OnAccountingDateSelectionChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateOnly> args)
	{
		_accounting.AccountingDate = args.Value;
		_accounting.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate)).Id;
		_accounting.ReferenceNo = await GenerateCodes.GenerateAccountingReferenceNo(_accounting.VoucherId, _accounting.AccountingDate);
		StateHasChanged();
	}
	#endregion

	#region Keyboard Events
	[JSInvokable]
	public async Task HandleKeyboardShortcut(string key)
	{
		switch (key)
		{
			case "F2":
				await StartLedgerSearch();
				break;
			case "Escape":
				await CancelLedgerSearch();
				break;
			case "Enter":
				if (_isLedgerSearchActive && _filteredLedgers.Count != 0)
					await SelectCurrentLedger();
				break;
			case "ArrowUp":
				if (_isLedgerSearchActive)
					NavigateLedgerSelection(-1);
				break;
			case "ArrowDown":
				if (_isLedgerSearchActive)
					NavigateLedgerSelection(1);
				break;
			default:
				if (_isLedgerSearchActive && key.Length == 1 && char.IsLetterOrDigit(key[0]))
				{
					_ledgerSearchText += key;
					await FilterLedgers();
				}
				else if (_isLedgerSearchActive && key == "Backspace" && _ledgerSearchText.Length > 0)
				{
					_ledgerSearchText = _ledgerSearchText[..^1];
					await FilterLedgers();
				}
				break;
		}
	}

	private async Task StartLedgerSearch()
	{
		_isLedgerSearchActive = true;
		_ledgerSearchText = "";
		_selectedLedgerIndex = 0;
		_filteredLedgers = [.. _ledgers];

		if (_filteredLedgers.Count != 0)
			_selectedLedger = _filteredLedgers.First();

		await JS.InvokeVoidAsync("showLedgerSearchIndicator", _ledgerSearchText);
		await UpdateLedgerSearchIndicator();
		StateHasChanged();
	}

	private async Task CancelLedgerSearch()
	{
		_isLedgerSearchActive = false;
		_ledgerSearchText = "";
		_filteredLedgers.Clear();
		if (!_amountDialogVisible)
			_selectedLedger = new();
		await JS.InvokeVoidAsync("hideLedgerSearchIndicator");
		StateHasChanged();
	}

	private async Task FilterLedgers()
	{
		if (string.IsNullOrWhiteSpace(_ledgerSearchText))
			_filteredLedgers = [.. _ledgers];
		else
			_filteredLedgers = [.. _ledgers.Where(x => x.Name.Contains(_ledgerSearchText, StringComparison.OrdinalIgnoreCase))];

		_selectedLedgerIndex = 0;
		if (_filteredLedgers.Count != 0)
			_selectedLedger = _filteredLedgers.First();

		await UpdateLedgerSearchIndicator();
		StateHasChanged();
	}

	private void NavigateLedgerSelection(int direction)
	{
		if (_filteredLedgers.Count == 0) return;

		_selectedLedgerIndex = Math.Max(0, Math.Min(_filteredLedgers.Count - 1, _selectedLedgerIndex + direction));
		_selectedLedger = _filteredLedgers[_selectedLedgerIndex];
		StateHasChanged();
	}

	private async Task SelectCurrentLedger()
	{
		if (_selectedLedger?.Id > 0)
			OnAddEntryButtonClick(_selectedLedger);

		await CancelLedgerSearch();
	}

	private async Task UpdateLedgerSearchIndicator() =>
		await JS.InvokeVoidAsync("updateLedgerSearchIndicator", _ledgerSearchText, _filteredLedgers.Count);
	#endregion

	#region Add Ledger
	private void OnAddEntryButtonClick(LedgerModel ledger)
	{
		_selectedLedger = ledger;
		_selectedDebitAmount = 0;
		_selectedCreditAmount = 0;
		_selectedRemarks = "";
		_amountDialogVisible = true;
		StateHasChanged();
	}

	private void OnDebitAmountChanged(decimal amount)
	{
		_selectedDebitAmount = amount;
		if (amount > 0)
			_selectedCreditAmount = 0;

		StateHasChanged();
	}

	private void OnCreditAmountChanged(decimal amount)
	{
		_selectedCreditAmount = amount;
		if (amount > 0)
			_selectedDebitAmount = 0;

		StateHasChanged();
	}

	private async Task OnAddToEntryClick()
	{
		if (_selectedLedger is null || _selectedLedger.Id == 0)
		{
			_sfErrorToast.Content = "Please select a valid ledger.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if (_selectedDebitAmount == 0 && _selectedCreditAmount == 0)
		{
			_sfErrorToast.Content = "Please enter either a Debit or Credit amount.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if (_selectedDebitAmount > 0 && _selectedCreditAmount > 0)
		{
			_sfErrorToast.Content = "Please enter either Debit OR Credit amount, not both.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		_accountingCart.Add(new()
		{
			Serial = _accountingCart.Count + 1,
			Id = _selectedLedger.Id,
			Name = _selectedLedger.Name,
			Remarks = _selectedRemarks,
			Debit = _selectedDebitAmount > 0 ? _selectedDebitAmount : null,
			Credit = _selectedCreditAmount > 0 ? _selectedCreditAmount : null
		});

		await _sfAccountingCartGrid.Refresh();
		_amountDialogVisible = false;
		StateHasChanged();
	}
	#endregion

	#region Data Grid Events
	public void AccountingCartRowSelectHandler(RowSelectEventArgs<AccountingCartModel> args)
	{
		_selectedAccountingCart = args.Data;
		_entryManageDialogVisible = true;
		StateHasChanged();
	}

	private void OnManageDebitAmountChanged(decimal amount)
	{
		_selectedAccountingCart.Debit = amount;
		if (amount > 0)
			_selectedAccountingCart.Credit = null;

		StateHasChanged();
	}

	private void OnManageCreditAmountChanged(decimal amount)
	{
		_selectedAccountingCart.Credit = amount;
		if (amount > 0)
			_selectedAccountingCart.Debit = null;

		StateHasChanged();
	}

	private async Task OnSaveEntryManageClick()
	{
		if ((_selectedAccountingCart.Debit ?? 0) == 0 && (_selectedAccountingCart.Credit ?? 0) == 0)
		{
			_sfErrorToast.Content = "Please enter either a Debit or Credit amount.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if ((_selectedAccountingCart.Debit ?? 0) > 0 && (_selectedAccountingCart.Credit ?? 0) > 0)
		{
			_sfErrorToast.Content = "Please enter either Debit OR Credit amount, not both.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		await _sfAccountingCartGrid.Refresh();
		_entryManageDialogVisible = false;
		StateHasChanged();
	}

	private async Task OnRemoveEntryClick()
	{
		_accountingCart.Remove(_selectedAccountingCart);
		await _sfAccountingCartGrid.Refresh();
		_entryManageDialogVisible = false;
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateEntry()
	{
		if (AccountingId is null)
		{
			_accounting.FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(_accounting.AccountingDate)).Id;
			_accounting.ReferenceNo = await GenerateCodes.GenerateAccountingReferenceNo(_accounting.VoucherId, _accounting.AccountingDate);
			_accounting.UserId = _user.Id;
			_accounting.GeneratedModule = GeneratedModules.FinancialAccounting.ToString();
		}

		if (_accountingCart.Count == 0)
		{
			_sfErrorToast.Content = "Please add at least one ledger entry.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (BalanceDifference != 0)
		{
			_sfErrorToast.Content = "Entry is not balanced. Debit and Credit amounts must be equal.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_accounting.VoucherId <= 0)
		{
			_sfErrorToast.Content = "Please select a voucher type.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_accounting.FinancialYearId <= 0)
		{
			_sfErrorToast.Content = "Please select a financial year.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task<bool> OnSaveEntry()
	{
		if (!await ValidateEntry() || _isSaving)
			return false;

		_isSaving = true;
		_accounting.Id = await AccountingData.InsertAccounting(_accounting);

		if (AccountingId.HasValue && AccountingId.Value > 0)
		{
			var existingEntry = await AccountingData.LoadAccountingDetailsByAccounting(AccountingId.Value);
			foreach (var detail in existingEntry)
			{
				detail.Status = false;
				await AccountingData.InsertAccountingDetails(detail);
			}
		}

		foreach (var cartItem in _accountingCart)
			await AccountingData.InsertAccountingDetails(new()
			{
				Id = 0,
				AccountingId = _accounting.Id,
				LedgerId = cartItem.Id,
				Debit = cartItem.Debit,
				Credit = cartItem.Credit,
				Remarks = cartItem.Remarks ?? "",
				Status = true
			});

		return true;
	}

	// Button 1: Save Only
	private async Task OnSaveEntryClick()
	{
		if (await OnSaveEntry())
		{
			_sfSuccessToast.Content = "Accounting entry saved successfully.";
			await _sfSuccessToast.ShowAsync();
		}
	}

	// Button 2: Save and A4 Prints
	private async Task OnSaveAndPrintClick()
	{
		if (await OnSaveEntry())
		{
			await PrintInvoice();
			_sfSuccessToast.Content = "Accounting entry saved successfully.";
			await _sfSuccessToast.ShowAsync();
		}
	}

	private async Task PrintInvoice()
	{
		var memoryStream = await AccountingA4Print.GenerateA4AccountingVoucher(_accounting.Id);
		var fileName = $"Accounting_Voucher_{_accounting.ReferenceNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await JS.InvokeVoidAsync("savePDF", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion
}