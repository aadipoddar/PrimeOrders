using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Accounts;

public partial class SettingsPage
{
	[Inject] private NavigationManager NavManager { get; set; } = default!;
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private SfToast _sfSuccessToast;
	private SfToast _sfResetToast;

	private List<SettingsModel> _settings = [];
	private List<VoucherModel> _vouchers = [];
	private List<LedgerModel> _ledgers = [];

	// Setting values
	private int _salesVoucherId = 1;
	private int _purchaseVoucherId = 1;
	private int _saleReturnVoucherId = 1;
	private int _saleLedgerId = 1;
	private int _purchaseLedgerId = 1;
	private int _cashLedgerId = 1;
	private int _gstLedgerId = 1;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Accounts, true)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		// Load all data in parallel
		var settingsTask = CommonData.LoadTableData<SettingsModel>(TableNames.Settings);
		var vouchersTask = CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);
		var ledgersTask = CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);

		await Task.WhenAll(settingsTask, vouchersTask, ledgersTask);

		_settings = [.. await settingsTask];
		_vouchers = [.. await vouchersTask];
		_ledgers = [.. await ledgersTask];

		await LoadSettingValues();
	}

	private async Task LoadSettingValues()
	{
		_salesVoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SalesVoucherId)).Value);
		_purchaseVoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId)).Value);
		_saleReturnVoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId)).Value);
		_saleLedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId)).Value);
		_purchaseLedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId)).Value);
		_cashLedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.CashLedgerId)).Value);
		_gstLedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value);
	}

	private async Task OnSaveClick()
	{
		_isSaving = true;
		StateHasChanged();

		var settingsToSave = new List<SettingsModel>
			{
				new() { Key = SettingsKeys.SalesVoucherId, Value = _salesVoucherId.ToString() },
				new() { Key = SettingsKeys.PurchaseVoucherId, Value = _purchaseVoucherId.ToString() },
				new() { Key = SettingsKeys.SaleReturnVoucherId, Value = _saleReturnVoucherId.ToString() },
				new() { Key = SettingsKeys.SaleLedgerId, Value = _saleLedgerId.ToString() },
				new() { Key = SettingsKeys.PurchaseLedgerId, Value = _purchaseLedgerId.ToString() },
				new() { Key = SettingsKeys.CashLedgerId, Value = _cashLedgerId.ToString() },
				new() { Key = SettingsKeys.GSTLedgerId, Value = _gstLedgerId.ToString() }
			};

		foreach (var setting in settingsToSave)
			await SettingsData.UpdateSettings(setting);

		await _sfSuccessToast!.ShowAsync();
	}

	private async Task OnResetClick()
	{
		_isSaving = true;
		StateHasChanged();

		await SettingsData.ResetSettings();
		await _sfResetToast!.ShowAsync();
	}
}