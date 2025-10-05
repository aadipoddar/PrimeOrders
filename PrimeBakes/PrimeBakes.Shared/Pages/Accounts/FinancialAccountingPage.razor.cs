using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingPage
{
	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedGroupId = 0;
	private int _selectedAccountTypeId = 0;

	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private readonly List<AccountingCartModel> _cart = [];

	private SfGrid<AccountingCartModel> _sfGrid;

	// Accounting model for transaction details
	private AccountingModel _accounting = new();

	// Validation dialog
	private SfDialog _validationDialog;
	private bool _validationDialogVisible = false;
	private List<string> _validationErrors = [];

	// Confirmation dialog
	private SfDialog _confirmationDialog;
	private bool _confirmationDialogVisible = false;

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
		await LoadGroups();
		await LoadAccountTypes();
		await LoadLedgers();
		await LoadExistingCart();
		await LoadExistingAccounting();

		StateHasChanged();
	}

	private async Task LoadGroups()
	{
		_groups = await CommonData.LoadTableDataByStatus<GroupModel>(TableNames.Group);
		_groups.Add(new GroupModel { Id = 0, Name = "All Groups" });
		_groups.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedGroupId = 0;
	}

	private async Task LoadAccountTypes()
	{
		_accountTypes = await CommonData.LoadTableDataByStatus<AccountTypeModel>(TableNames.AccountType);
		_accountTypes.Add(new AccountTypeModel { Id = 0, Name = "All Account Types" });
		_accountTypes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedAccountTypeId = 0;
	}

	private async Task LoadLedgers()
	{
		_cart.Clear();

		var allLedgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);

		foreach (var ledger in allLedgers)
			_cart.Add(new()
			{
				AccountTypeId = ledger.AccountTypeId,
				GroupId = ledger.GroupId,
				Serial = 0,
				Id = ledger.Id,
				Name = ledger.Name,
				Debit = 0,
				Credit = 0,
				Remarks = string.Empty
			});

		_cart.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
	}

	private async Task LoadExistingCart()
	{
		if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<AccountingCartModel>>(
				await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingCartDataFileName)) ?? [];
			foreach (var item in items)
			{
				_cart.Where(x => x.Id == item.Id).FirstOrDefault().Debit = item.Debit;
				_cart.Where(x => x.Id == item.Id).FirstOrDefault().Credit = item.Credit;
				_cart.Where(x => x.Id == item.Id).FirstOrDefault().Remarks = item.Remarks;
			}
		}
	}

	private async Task LoadExistingAccounting()
	{
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
				UserId = _user.Id,
				AccountingDate = DateOnly.FromDateTime(DateTime.Now),
				VoucherId = 0,
				Remarks = string.Empty
			};
			await SaveAccountingModel();
		}
	}
	#endregion

	#region Saving
	private async Task SaveAccountingFile()
	{
		if (!_cart.Any(x => x.Debit > 0 || x.Credit > 0) && await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Debit > 0 || _.Credit > 0)));

		// Also save accounting model
		await SaveAccountingModel();

		VibrationService.VibrateHapticClick();

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task SaveAccountingModel()
	{
		await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName,
			System.Text.Json.JsonSerializer.Serialize(_accounting));
	}

	private async Task UpdateDebit(AccountingCartModel item, decimal? value)
	{
		item.Debit = value ?? 0;
		// Clear credit if debit is entered (accounting principle)
		if (item.Debit > 0)
			item.Credit = 0;

		await SaveAccountingFile();
		VibrationService.VibrateHapticClick();
	}

	private async Task UpdateCredit(AccountingCartModel item, decimal? value)
	{
		item.Credit = value ?? 0;
		// Clear debit if credit is entered (accounting principle)
		if (item.Credit > 0)
			item.Debit = 0;

		await SaveAccountingFile();
		VibrationService.VibrateHapticClick();
	}

	private async Task UpdateRemarks(AccountingCartModel item, string value)
	{
		item.Remarks = value ?? string.Empty;
		await SaveAccountingFile();
	}
	#endregion

	#region Filtering
	private async Task OnAccountTypeChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, AccountTypeModel> args)
	{
		_selectedAccountTypeId = args.Value;
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task OnGroupChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, GroupModel> args)
	{
		_selectedGroupId = args.Value;
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private List<AccountingCartModel> GetFilteredLedgers()
	{
		var filtered = _cart.AsEnumerable();

		if (_selectedAccountTypeId > 0)
			filtered = filtered.Where(x => x.AccountTypeId == _selectedAccountTypeId);

		if (_selectedGroupId > 0)
			filtered = filtered.Where(x => x.GroupId == _selectedGroupId);

		return filtered.ToList();
	}
	#endregion

	#region Cart
	private async Task GoToCart()
	{
		await SaveAccountingFile();

		if (!ValidateForm())
		{
			_validationDialogVisible = true;
			VibrationService.VibrateWithTime(300);
			return;
		}

		// Show confirmation dialog before proceeding to cart
		_confirmationDialogVisible = true;
		VibrationService.VibrateHapticClick();
	}

	// Confirmation dialog methods
	private void CloseConfirmationDialog()
	{
		_confirmationDialogVisible = false;
		StateHasChanged();
	}

	private void ConfirmGoToCart()
	{
		_confirmationDialogVisible = false;
		VibrationService.VibrateWithTime(500);
		NavigationManager.NavigateTo("/Accounting/Cart");
	}

	// Form validation method similar to other pages
	private bool ValidateForm()
	{
		_validationErrors.Clear();

		// Check if cart has entries
		if (!_cart.Any(x => (x.Debit ?? 0) > 0 || (x.Credit ?? 0) > 0))
			_validationErrors.Add("Please add at least one ledger entry with debit or credit amount.");

		// Check balance
		if (BalanceDifference != 0)
			_validationErrors.Add($"Transaction is not balanced. Difference: ₹{Math.Abs(BalanceDifference):N2} {(BalanceDifference > 0 ? "(Debit heavy)" : "(Credit heavy)")}");

		// Check for very small amounts (potential data entry errors)
		var smallAmounts = _cart.Where(x => ((x.Debit ?? 0) > 0 && (x.Debit ?? 0) < 0.01m) || ((x.Credit ?? 0) > 0 && (x.Credit ?? 0) < 0.01m)).Count();
		if (smallAmounts > 0)
			_validationErrors.Add($"{smallAmounts} entries have very small amounts (less than ₹0.01). Please verify these amounts.");

		return _validationErrors.Count == 0;
	}

	// Validation dialog methods
	private void CloseValidationDialog()
	{
		_validationDialogVisible = false;
		StateHasChanged();
	}

	private void GoToCartFromValidation()
	{
		CloseValidationDialog();
		// Show confirmation dialog after validation passes
		_confirmationDialogVisible = true;
		VibrationService.VibrateHapticClick();
	}
	#endregion
}