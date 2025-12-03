using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.Data.Sales.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

using Toolbelt.Blazor.HotKeys2;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccounting : IAsyncDisposable
{
	[Inject] private HotKeys HotKeys { get; set; }
	private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private VoucherModel _selectedVoucher = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private LedgerModel? _selectedLedger = new();
	private AccountingLedgerOverviewModel _selectedAccountingLedger = new();
	private AccountingItemCartModel _selectedCart = new();
	private AccountingModel _accounting = new();

	private List<CompanyModel> _companies = [];
	private List<VoucherModel> _vouchers = [];
	private List<LedgerModel> _ledgers = [];
	private List<AccountingLedgerOverviewModel> _accountingLedgers = [];
	private List<AccountingItemCartModel> _cart = [];

	private SfAutoComplete<LedgerModel?, LedgerModel> _sfLedgerAutoComplete;
	private SfGrid<AccountingItemCartModel> _sfCartGrid;

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;

	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.Enter, AddItemToCart, "Add item to cart", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, () => _sfLedgerAutoComplete.FocusAsync(), "Focus on ledger input", Exclude.None)
			.Add(ModCode.Ctrl, Code.S, SaveTransaction, "Save the transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, DownloadInvoice, "Download invoice", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistoryPage, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.T, NavigateToTrialBalance, "Open trial balance report", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);

		await LoadCompanies();
		await LoadVouchers();
		await LoadExistingTransaction();
		await LoadLedgers();
		await LoadExistingCart();
		await SaveTransactionFile();
	}

	private async Task LoadCompanies()
	{
		try
		{
			_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
			_companies = [.. _companies.OrderBy(s => s.Name)];
			_companies.Add(new()
			{
				Id = 0,
				Name = "Create New Company ..."
			});

			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? throw new Exception("Main Company Not Found");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Companies", ex.Message, "error");
		}
	}

	private async Task LoadVouchers()
	{
		try
		{
			_vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(TableNames.Voucher);
			_vouchers = [.. _vouchers.OrderBy(s => s.Name)];
			_vouchers.Add(new()
			{
				Id = 0,
				Name = "Create New Voucher ..."
			});

			_selectedVoucher = _vouchers.FirstOrDefault();
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Vouchers", ex.Message, "error");
		}
	}

	private async Task LoadExistingTransaction()
	{
		try
		{
			if (Id.HasValue)
			{
				_accounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, Id.Value);
				if (_accounting is null || _accounting.Id == 0)
				{
					await ShowToast("Transaction Not Found", "The requested transaction could not be found.", "error");
					NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingDataFileName))
				_accounting = System.Text.Json.JsonSerializer.Deserialize<AccountingModel>(await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingDataFileName));

			else
			{
				_accounting = new()
				{
					Id = 0,
					TransactionNo = string.Empty,
					CompanyId = _selectedCompany.Id,
					TransactionDateTime = await CommonData.LoadCurrentDateTime(),
					ReferenceId = null,
					ReferenceNo = null,
					VoucherId = _selectedVoucher.Id,
					FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
					TotalDebitAmount = 0,
					TotalCreditAmount = 0,
					TotalDebitLedgers = 0,
					TotalCreditLedgers = 0,
					Remarks = "",
					CreatedBy = _user.Id,
					CreatedAt = DateTime.Now,
					CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
					Status = true,
					LastModifiedAt = null,
					LastModifiedBy = null,
					LastModifiedFromPlatform = null
				};

				await DeleteLocalFiles();
			}


			if (_accounting.CompanyId > 0)
				_selectedCompany = _companies.FirstOrDefault(s => s.Id == _accounting.CompanyId);
			else
			{
				var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
				_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
				_accounting.CompanyId = _selectedCompany.Id;
			}

			if (_accounting.VoucherId > 0)
				_selectedVoucher = _vouchers.FirstOrDefault(s => s.Id == _accounting.VoucherId);
			else
			{
				_selectedVoucher = _vouchers.FirstOrDefault();
				_accounting.VoucherId = _selectedVoucher.Id;
			}

			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _accounting.FinancialYearId);
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Transaction Data", ex.Message, "error");
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}

	private async Task LoadLedgers()
	{
		try
		{
			_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
			_ledgers = [.. _ledgers.OrderBy(s => s.Name)];
			_ledgers.Add(new()
			{
				Id = 0,
				Name = "Create New Ledger ..."
			});
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Ledgers", ex.Message, "error");
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (_accounting.Id > 0)
			{
				var existingCart = await CommonData.LoadTableDataByMasterId<AccountingDetailModel>(TableNames.AccountingDetail, _accounting.Id);

				foreach (var item in existingCart)
				{
					if (_ledgers.FirstOrDefault(s => s.Id == item.LedgerId) is null)
					{
						var ledger = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, item.LedgerId);
						await ShowToast("Ledger Not Found", $"The ledger {ledger?.Name} (ID: {item.LedgerId}) in the existing transaction cart was not found in the available ledgers list. It may have been deleted or is inaccessible.", "error");
						continue;
					}

					_cart.Add(new()
					{
						LedgerId = item.LedgerId,
						LedgerName = _ledgers.First(s => s.Id == item.LedgerId).Name,
						Credit = item.Credit,
						Debit = item.Debit,
						ReferenceId = item.ReferenceId,
						ReferenceNo = item.ReferenceNo,
						ReferenceType = item.ReferenceType,
						Remarks = item.Remarks
					});
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<AccountingItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingCartDataFileName));
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Existing Cart", ex.Message, "error");
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}
	#endregion

	#region Change Events
	private async Task OnCompanyChanged(ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminCompany, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminCompany);

			return;
		}

		_selectedCompany = args.Value;
		_accounting.CompanyId = _selectedCompany.Id;

		await SaveTransactionFile();
	}

	private async Task OnPartyChanged(ChangeEventArgs<VoucherModel, VoucherModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminVoucher, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminVoucher);

			return;
		}

		_selectedVoucher = args.Value;
		_accounting.VoucherId = _selectedVoucher.Id;

		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_accounting.TransactionDateTime = args.Value;
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<LedgerModel?, LedgerModel?> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLedger, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminLedger);

			return;
		}

		_selectedLedger = args.Value;

		if (_selectedLedger is null)
			_selectedCart = new()
			{
				LedgerId = 0,
				LedgerName = string.Empty,
				Credit = null,
				Debit = null,
				ReferenceId = null,
				ReferenceNo = null,
				ReferenceType = null,
				Remarks = string.Empty
			};

		else
		{
			_selectedCart.LedgerId = _selectedLedger.Id;
			_selectedCart.LedgerName = _selectedLedger.Name;
			_selectedCart.Credit = null;
			_selectedCart.Debit = null;
		}
	}

	private void OnReferenceChanged(ChangeEventArgs<AccountingLedgerOverviewModel, AccountingLedgerOverviewModel> args)
	{
		if (args.Value is null)
		{
			_selectedAccountingLedger = new();
			_selectedCart.ReferenceNo = null;
			_selectedCart.ReferenceId = null;
			_selectedCart.ReferenceType = null;
			return;
		}

		_selectedAccountingLedger = args.Value;
		_selectedCart.ReferenceNo = args.Value.ReferenceNo;
		_selectedCart.ReferenceId = args.Value.ReferenceId;
		_selectedCart.ReferenceType = args.Value.ReferenceType;

		if ((_selectedAccountingLedger.Debit ?? 0) > (_selectedAccountingLedger.Credit ?? 0))
			_selectedCart.Credit = (_selectedAccountingLedger.Debit ?? 0) - (_selectedAccountingLedger.Credit ?? 0);
		else if ((_selectedAccountingLedger.Credit ?? 0) > (_selectedAccountingLedger.Debit ?? 0))
			_selectedCart.Debit = (_selectedAccountingLedger.Credit ?? 0) - (_selectedAccountingLedger.Debit ?? 0);
	}

	private async Task AddItemToCart()
	{
		if (_selectedLedger is null || _selectedLedger.LocationId <= 0 ||
			((_selectedCart.Debit ?? 0) <= 0 && (_selectedCart.Credit ?? 0) <= 0) ||
			((_selectedCart.Debit ?? 0) > 0 && (_selectedCart.Credit ?? 0) > 0) ||
			(_selectedCart.Debit ?? 0) < 0 || (_selectedCart.Credit ?? 0) < 0)
		{
			await ShowToast("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", "error");
			return;
		}

		_cart.Add(new()
		{
			LedgerId = _selectedCart.LedgerId,
			LedgerName = _selectedCart.LedgerName,
			Credit = _selectedCart.Credit == 0 ? null : _selectedCart.Credit,
			Debit = _selectedCart.Debit == 0 ? null : _selectedCart.Debit,
			ReferenceId = _selectedCart.ReferenceId,
			ReferenceNo = _selectedCart.ReferenceNo,
			ReferenceType = _selectedCart.ReferenceType,
			Remarks = _selectedCart.Remarks
		});

		_selectedLedger = null;
		_selectedAccountingLedger = null;
		_accountingLedgers = [];
		_selectedCart = new();

		await _sfLedgerAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(AccountingItemCartModel cartItem)
	{
		_selectedLedger = _ledgers.FirstOrDefault(s => s.Id == cartItem.LedgerId);

		if (_selectedLedger is null)
			return;

		_selectedCart = new()
		{
			LedgerId = cartItem.LedgerId,
			LedgerName = cartItem.LedgerName,
			Credit = cartItem.Credit ?? 0,
			Debit = cartItem.Debit ?? 0,
			ReferenceId = cartItem.ReferenceId,
			ReferenceNo = cartItem.ReferenceNo,
			ReferenceType = cartItem.ReferenceType,
			Remarks = cartItem.Remarks
		};

		await _sfLedgerAutoComplete.FocusAsync();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(AccountingItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}

	private async Task GetReferences()
	{
		if (_isProcessing || _isLoading)
			return;

		if (_selectedLedger is null || _selectedLedger.Id <= 0)
		{
			await ShowToast("Select Ledger", "Please select a ledger first.", "error");
			return;
		}

		try
		{
			_isProcessing = true;

			// Load all accounting ledger transactions for the selected ledger within the financial year
			var allLedgerTransactions = await CommonData.LoadTableDataByDate<AccountingLedgerOverviewModel>(
				ViewNames.AccountingLedgerOverview,
				_selectedFinancialYear.StartDate.ToDateTime(TimeOnly.MinValue),
				_accounting.TransactionDateTime);

			// Filter for the selected ledger only
			var ledgerTransactions = allLedgerTransactions.Where(x => x.Id == _selectedLedger.Id).ToList();

			if (ledgerTransactions.Count == 0)
			{
				await ShowToast("No References Found", "No reference transactions found for the selected ledger within the financial year.", "info");
				return;
			}

			// Group by ReferenceNo, ReferenceType, and ReferenceId to consolidate transactions
			var ledgerGroups = new Dictionary<(string ReferenceNo, string ReferenceType, int? ReferenceId), AccountingLedgerOverviewModel>();

			foreach (var item in ledgerTransactions)
			{
				var key = (item.ReferenceNo, item.ReferenceType, item.ReferenceId);

				if (ledgerGroups.TryGetValue(key, out var existingLedger))
				{
					// Aggregate debit and credit amounts	
					existingLedger.Debit = (existingLedger.Debit ?? 0) + (item.Debit ?? 0);
					existingLedger.Credit = (existingLedger.Credit ?? 0) + (item.Credit ?? 0);
				}
				else
				{
					ledgerGroups[key] = item;
				}
			}

			// Filter out balanced entries (where Debit == Credit) and entries without reference information
			_accountingLedgers = [.. ledgerGroups.Values
				.Where(x => (x.Debit ?? 0) != (x.Credit ?? 0) &&
							x.ReferenceId is not null &&
							!string.IsNullOrEmpty(x.ReferenceNo))
				.OrderByDescending(x => x.ReferenceDateTime)];

			if (_accountingLedgers.Count == 0)
			{
				await ShowToast("No Outstanding References", "All references for this ledger are fully balanced.", "info");
				return;
			}

			await ShowToast("References Loaded", $"Found {_accountingLedgers.Count} outstanding reference(s) for the selected ledger.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Fetching References", ex.Message, "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart.ToList())
		{
			if ((item.Credit ?? 0) == 0)
				item.Credit = null;

			if ((item.Debit ?? 0) == 0)
				item.Debit = null;

			if ((item.Credit ?? 0) > 0 && (item.Debit ?? 0) > 0)
			{
				_cart.Remove(item);
				continue;
			}

			if ((item.Debit ?? 0) < 0 || (item.Credit ?? 0) < 0)
			{
				_cart.Remove(item);
				continue;
			}

			if (item.ReferenceId == 0 || item.ReferenceId is null || string.IsNullOrWhiteSpace(item.ReferenceNo))
			{
				item.ReferenceId = null;
				item.ReferenceNo = null;
				item.ReferenceType = null;
			}

			item.Remarks = item.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(item.Remarks))
				item.Remarks = null;
		}

		_accounting.TotalCreditAmount = _cart.Sum(x => x.Credit ?? 0);
		_accounting.TotalDebitAmount = _cart.Sum(x => x.Debit ?? 0);
		_accounting.TotalCreditLedgers = _cart.Count(x => (x.Credit ?? 0) > 0);
		_accounting.TotalDebitLedgers = _cart.Count(x => (x.Debit ?? 0) > 0);

		_accounting.CompanyId = _selectedCompany.Id;
		_accounting.VoucherId = _selectedVoucher.Id;
		_accounting.CreatedBy = _user.Id;

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_accounting.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_accounting.FinancialYearId = _selectedFinancialYear.Id;
		else
		{
			await ShowToast("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", "error");
			_accounting.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_accounting.TransactionDateTime);
			_accounting.FinancialYearId = _selectedFinancialYear.Id;
		}
		#endregion

		if (Id is null)
			_accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(_accounting);
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName, System.Text.Json.JsonSerializer.Serialize(_accounting));
			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Transaction Data", ex.Message, "error");
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (_selectedCompany is null || _accounting.CompanyId <= 0)
		{
			await ShowToast("Company Not Selected", "Please select a company for the transaction.", "error");
			return false;
		}

		if (_selectedVoucher is null || _accounting.VoucherId <= 0)
		{
			await ShowToast("Voucher Not Selected", "Please select a voucher for the transaction.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_accounting.TransactionNo))
		{
			await ShowToast("Transaction Number Missing", "Please enter a transaction number for the transaction.", "error");
			return false;
		}

		if (_accounting.TransactionDateTime == default)
		{
			await ShowToast("Transaction Date Missing", "Please select a valid transaction date for the transaction.", "error");
			return false;
		}

		if (_selectedFinancialYear is null || _accounting.FinancialYearId <= 0)
		{
			await ShowToast("Financial Year Not Found", "The transaction date does not fall within any financial year. Please check the date and try again.", "error");
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await ShowToast("Financial Year Locked", "The financial year for the selected transaction date is locked. Please select a different date.", "error");
			return false;
		}

		if (_selectedFinancialYear.Status == false)
		{
			await ShowToast("Financial Year Inactive", "The financial year for the selected transaction date is inactive. Please select a different date.", "error");
			return false;
		}

		if (_cart.Any(item => (item.Credit ?? 0) < 0) || _cart.Any(item => (item.Debit ?? 0) < 0))
		{
			await ShowToast("Invalid Item Amount", "One or more items in the cart have an invalid amount. Please correct the amounts before saving.", "error");
			return false;
		}

		if (_accounting.TotalCreditAmount <= 0 && _accounting.TotalDebitAmount <= 0)
		{
			await ShowToast("Invalid Total Amounts", "The total credit and debit amounts must be greater than zero.", "error");
			return false;
		}

		if (_accounting.TotalDebitLedgers <= 0 && _accounting.TotalCreditLedgers <= 0)
		{
			await ShowToast("Invalid Total Ledgers", "The total number of credit and debit ledgers must be greater than zero.", "error");
			return false;
		}

		if (_accounting.TotalDebitAmount - _accounting.TotalCreditAmount != 0)
		{
			await ShowToast("Debit and Credit Amounts Mismatch", "The total debit and credit amounts do not match. Please ensure they are equal before saving.", "error");
			return false;
		}

		if (_accounting.Id > 0)
		{
			var existingAccounting = await CommonData.LoadTableDataById<AccountingModel>(TableNames.Accounting, _accounting.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingAccounting.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			{
				await ShowToast("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", "error");
				return false;
			}

			if (!_user.Admin)
			{
				await ShowToast("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", "error");
				await DeleteLocalFiles();
				NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);
				return false;
			}
		}

		_accounting.Remarks = _accounting.Remarks?.Trim();
		if (string.IsNullOrWhiteSpace(_accounting.Remarks))
			_accounting.Remarks = null;

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await SaveTransactionFile();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			_accounting.Status = true;
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_accounting.TransactionDateTime = DateOnly.FromDateTime(_accounting.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_accounting.LastModifiedAt = currentDateTime;
			_accounting.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_accounting.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_accounting.CreatedBy = _user.Id;
			_accounting.LastModifiedBy = _user.Id;

			_accounting.Id = await AccountingData.SaveAccountingTransaction(_accounting, _cart);
			var (pdfStream, fileName) = await AccountingData.GenerateAndDownloadInvoice(_accounting.Id);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await DeleteLocalFiles();
			NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);

			await ShowToast("Save Transaction", "Transaction saved successfully! Invoice has been generated.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Transaction", ex.Message, "error");
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
	}
	#endregion

	#region Utilities
	private async Task DownloadInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await ShowToast("No Transaction Selected", "Please save the transaction first before downloading the invoice.", "error");
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			var (pdfStream, fileName) = await AccountingData.GenerateAndDownloadInvoice(Id.Value);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await ShowToast("Invoice Downloaded", "The invoice has been downloaded successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Downloading Invoice", ex.Message, "error");
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportFinancialAccounting, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportFinancialAccounting);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportAccountingLedger, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportAccountingLedger);
	}

	private async Task NavigateToTrialBalance()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportTrialBalance, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportTrialBalance);
	}

	private async Task ViewReferenceInvoice()
	{
		if (_accounting.ReferenceId is null || _accounting.ReferenceId <= 0)
		{
			await ShowToast("Invalid Reference", "No reference transaction found.", "error");
			return;
		}

		try
		{
			var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
			var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
			var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId);
			var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId);
			var stockTransferVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId);

			if (_accounting.VoucherId == int.Parse(saleVoucher.Value))
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Sale}/{_accounting.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Sale}/{_accounting.ReferenceId.Value}");
			}

			else if (_accounting.VoucherId == int.Parse(saleReturnVoucher.Value))
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{_accounting.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{_accounting.ReferenceId.Value}");
			}

			else if (_accounting.VoucherId == int.Parse(purchaseVoucher.Value))
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Purchase}/{_accounting.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Purchase}/{_accounting.ReferenceId.Value}");
			}

			else if (_accounting.VoucherId == int.Parse(purchaseReturnVoucher.Value))
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.PurchaseReturn}/{_accounting.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.PurchaseReturn}/{_accounting.ReferenceId.Value}");
			}

			else if (_accounting.VoucherId == int.Parse(stockTransferVoucher.Value))
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{_accounting.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{_accounting.ReferenceId.Value}");
			}

			else
				await ShowToast("Unsupported Voucher Type", "The voucher type associated with the reference transaction is not supported for viewing.", "error");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to view invoice: {ex.Message}", "error");
		}
	}

	private async Task DownloadReferenceInvoice()
	{
		if (_accounting.ReferenceId is null || _accounting.ReferenceId <= 0)
		{
			await ShowToast("Invalid Reference", "No reference transaction found.", "error");
			return;
		}

		try
		{
			var saleVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
			var saleReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
			var purchaseVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId);
			var purchaseReturnVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId);
			var stockTransferVoucher = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId);

			if (_accounting.VoucherId == int.Parse(saleVoucher.Value))
			{
				var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(_accounting.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_accounting.VoucherId == int.Parse(saleReturnVoucher.Value))
			{
				var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(_accounting.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_accounting.VoucherId == int.Parse(purchaseVoucher.Value))
			{
				var (pdfStream, fileName) = await PurchaseData.GenerateAndDownloadInvoice(_accounting.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_accounting.VoucherId == int.Parse(purchaseReturnVoucher.Value))
			{
				var (pdfStream, fileName) = await PurchaseReturnData.GenerateAndDownloadInvoice(_accounting.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_accounting.VoucherId == int.Parse(stockTransferVoucher.Value))
			{
				var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(_accounting.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else
			{
				await ShowToast("Unsupported Voucher Type", "The voucher type associated with the reference transaction is not supported for downloading.", "error");
				return;
			}

			await ShowToast("Invoice Downloaded", "The invoice has been downloaded successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to download invoice: {ex.Message}", "error");
		}
	}

	private async Task ViewCartReferenceInvoice()
	{
		if (_selectedAccountingLedger is null || _selectedAccountingLedger.ReferenceId is null || _selectedAccountingLedger.ReferenceId <= 0)
		{
			await ShowToast("Invalid Reference", "No reference transaction found.", "error");
			return;
		}

		try
		{
			if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.Sale.ToString())
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Sale}/{_selectedAccountingLedger.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Sale}/{_selectedAccountingLedger.ReferenceId.Value}");
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.SaleReturn.ToString())
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{_selectedAccountingLedger.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{_selectedAccountingLedger.ReferenceId.Value}");
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.Purchase.ToString())
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Purchase}/{_selectedAccountingLedger.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Purchase}/{_selectedAccountingLedger.ReferenceId.Value}");
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.PurchaseReturn.ToString())
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.PurchaseReturn}/{_selectedAccountingLedger.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.PurchaseReturn}/{_selectedAccountingLedger.ReferenceId.Value}");
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.StockTransfer.ToString())
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{_selectedAccountingLedger.ReferenceId.Value}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{_selectedAccountingLedger.ReferenceId.Value}");
			}

			else
				await ShowToast("Unsupported Voucher Type", "The voucher type associated with the reference transaction is not supported for viewing.", "error");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to view invoice: {ex.Message}", "error");
		}
	}

	private async Task DownloadCartReferenceInvoice()
	{
		if (_selectedAccountingLedger is null || _selectedAccountingLedger.ReferenceId is null || _selectedAccountingLedger.ReferenceId <= 0)
		{
			await ShowToast("Invalid Reference", "No reference transaction found.", "error");
			return;
		}

		try
		{
			if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.Sale.ToString())
			{
				var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(_selectedAccountingLedger.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.SaleReturn.ToString())
			{
				var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(_selectedAccountingLedger.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.Purchase.ToString())
			{
				var (pdfStream, fileName) = await PurchaseData.GenerateAndDownloadInvoice(_selectedAccountingLedger.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.PurchaseReturn.ToString())
			{
				var (pdfStream, fileName) = await PurchaseReturnData.GenerateAndDownloadInvoice(_selectedAccountingLedger.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else if (_selectedAccountingLedger.ReferenceType.ToString() == ReferenceTypes.StockTransfer.ToString())
			{
				var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(_selectedAccountingLedger.ReferenceId.Value);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			else
			{
				await ShowToast("Unsupported Voucher Type", "The voucher type associated with the reference transaction is not supported for downloading.", "error");
				return;
			}

			await ShowToast("Invoice Downloaded", "The invoice has been downloaded successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to download invoice: {ex.Message}", "error");
		}
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}

		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}