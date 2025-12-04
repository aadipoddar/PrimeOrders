using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Operations;

public partial class SettingsPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	#region Fields

	// UI State
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _isResetDialogVisible = false;

	// Toast and Dialog References
	private SfToast _sfSuccessToast = default!;
	private SfToast _sfErrorToast = default!;
	private SfDialog _resetConfirmationDialog = default!;

	// Primary Configuration
	private string _primaryCompanyLinkingId = string.Empty;
	private string _selectedCompanyName = string.Empty;
	private List<CompanyModel> _companies = [];

	// Code Prefixes
	private string _rawMaterialCodePrefix = string.Empty;
	private string _finishedProductCodePrefix = string.Empty;
	private string _ledgerCodePrefix = string.Empty;

	// Transaction Prefixes
	private string _purchaseTransactionPrefix = string.Empty;
	private string _purchaseReturnTransactionPrefix = string.Empty;
	private string _kitchenIssueTransactionPrefix = string.Empty;
	private string _kitchenProductionTransactionPrefix = string.Empty;
	private string _rawMaterialStockAdjustmentTransactionPrefix = string.Empty;
	private string _productStockAdjustmentTransactionPrefix = string.Empty;
	private string _saleTransactionPrefix = string.Empty;
	private string _saleReturnTransactionPrefix = string.Empty;
	private string _stockTransferTransactionPrefix = string.Empty;
	private string _orderTransactionPrefix = string.Empty;
	private string _accountingTransactionPrefix = string.Empty;

	// Vouchers
	private string _saleVoucherId = string.Empty;
	private string _selectedSaleVoucherName = string.Empty;
	private string _saleReturnVoucherId = string.Empty;
	private string _selectedSaleReturnVoucherName = string.Empty;
	private string _stockTransferVoucherId = string.Empty;
	private string _selectedStockTransferVoucherName = string.Empty;
	private string _purchaseVoucherId = string.Empty;
	private string _selectedPurchaseVoucherName = string.Empty;
	private string _purchaseReturnVoucherId = string.Empty;
	private string _selectedPurchaseReturnVoucherName = string.Empty;
	private List<VoucherModel> _vouchers = [];

	// Ledgers
	private string _saleLedgerId = string.Empty;
	private string _selectedSaleLedgerName = string.Empty;
	private string _stockTransferLedgerId = string.Empty;
	private string _selectedStockTransferLedgerName = string.Empty;
	private string _purchaseLedgerId = string.Empty;
	private string _selectedPurchaseLedgerName = string.Empty;
	private string _cashLedgerId = string.Empty;
	private string _selectedCashLedgerName = string.Empty;
	private string _cashSalesLedgerId = string.Empty;
	private string _selectedCashSalesLedgerName = string.Empty;
	private string _gstLedgerId = string.Empty;
	private string _selectedGSTLedgerName = string.Empty;
	private List<LedgerModel> _ledgers = [];

	// Purchase Behavior
	private bool _updateItemMasterRateOnPurchase = false;
	private bool _updateItemMasterUOMOnPurchase = false;

	#endregion

	#region Load Data

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}


	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.S, SaveSettings, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None);

		try
		{
			// Load all settings
			await LoadAllSettings();

			// Load reference data for autocompletes
			await LoadCompanies();
			await LoadVouchers();
			await LoadLedgers();

			// Map IDs to selected names for autocompletes
			MapSelections();
		}
		catch (Exception ex)
		{
			await ShowToast("Load Error", $"Failed to load settings: {ex.Message}", "error");
		}
	}

	private async Task LoadAllSettings()
	{
		// Primary Configuration
		var primaryCompanySetting = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_primaryCompanyLinkingId = primaryCompanySetting?.Value ?? string.Empty;

		// Code Prefixes
		var rmCodePrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialCodePrefix);
		_rawMaterialCodePrefix = rmCodePrefixSetting?.Value ?? string.Empty;

		var fpCodePrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.FinishedProductCodePrefix);
		_finishedProductCodePrefix = fpCodePrefixSetting?.Value ?? string.Empty;

		var ledgerCodePrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.LedgerCodePrefix);
		_ledgerCodePrefix = ledgerCodePrefixSetting?.Value ?? string.Empty;

		// Transaction Prefixes
		var purchasePrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseTransactionPrefix);
		_purchaseTransactionPrefix = purchasePrefixSetting?.Value ?? string.Empty;

		var purchaseReturnPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnTransactionPrefix);
		_purchaseReturnTransactionPrefix = purchaseReturnPrefixSetting?.Value ?? string.Empty;

		var kitchenIssuePrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenIssueTransactionPrefix);
		_kitchenIssueTransactionPrefix = kitchenIssuePrefixSetting?.Value ?? string.Empty;

		var kitchenProductionPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenProductionTransactionPrefix);
		_kitchenProductionTransactionPrefix = kitchenProductionPrefixSetting?.Value ?? string.Empty;

		var rmStockAdjPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix);
		_rawMaterialStockAdjustmentTransactionPrefix = rmStockAdjPrefixSetting?.Value ?? string.Empty;

		var productStockAdjPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.ProductStockAdjustmentTransactionPrefix);
		_productStockAdjustmentTransactionPrefix = productStockAdjPrefixSetting?.Value ?? string.Empty;

		var salePrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleTransactionPrefix);
		_saleTransactionPrefix = salePrefixSetting?.Value ?? string.Empty;

		var saleReturnPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnTransactionPrefix);
		_saleReturnTransactionPrefix = saleReturnPrefixSetting?.Value ?? string.Empty;

		var stockTransferPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferTransactionPrefix);
		_stockTransferTransactionPrefix = stockTransferPrefixSetting?.Value ?? string.Empty;

		var orderPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.OrderTransactionPrefix);
		_orderTransactionPrefix = orderPrefixSetting?.Value ?? string.Empty;

		var accountingPrefixSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AccountingTransactionPrefix);
		_accountingTransactionPrefix = accountingPrefixSetting?.Value ?? string.Empty;

		// Vouchers
		var saleVoucherSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleVoucherId);
		_saleVoucherId = saleVoucherSetting?.Value ?? string.Empty;

		var saleReturnVoucherSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId);
		_saleReturnVoucherId = saleReturnVoucherSetting?.Value ?? string.Empty;

		var stockTransferVoucherSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferVoucherId);
		_stockTransferVoucherId = stockTransferVoucherSetting?.Value ?? string.Empty;

		var purchaseVoucherSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId);
		_purchaseVoucherId = purchaseVoucherSetting?.Value ?? string.Empty;

		var purchaseReturnVoucherSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseReturnVoucherId);
		_purchaseReturnVoucherId = purchaseReturnVoucherSetting?.Value ?? string.Empty;

		// Ledgers
		var saleLedgerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.SaleLedgerId);
		_saleLedgerId = saleLedgerSetting?.Value ?? string.Empty;

		var stockTransferLedgerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.StockTransferLedgerId);
		_stockTransferLedgerId = stockTransferLedgerSetting?.Value ?? string.Empty;

		var purchaseLedgerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId);
		_purchaseLedgerId = purchaseLedgerSetting?.Value ?? string.Empty;

		var cashLedgerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.CashLedgerId);
		_cashLedgerId = cashLedgerSetting?.Value ?? string.Empty;

		var cashSalesLedgerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.CashSalesLedgerId);
		_cashSalesLedgerId = cashSalesLedgerSetting?.Value ?? string.Empty;

		var gstLedgerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId);
		_gstLedgerId = gstLedgerSetting?.Value ?? string.Empty;

		// Purchase Behavior
		var updateRateSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterRateOnPurchase);
		_updateItemMasterRateOnPurchase = bool.TryParse(updateRateSetting?.Value, out var updateRate) && updateRate;

		var updateUOMSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.UpdateItemMasterUOMOnPurchase);
		_updateItemMasterUOMOnPurchase = bool.TryParse(updateUOMSetting?.Value, out var updateUOM) && updateUOM;
	}

	private async Task LoadCompanies()
	{
		var result = await CommonData.LoadTableData<CompanyModel>(TableNames.Company);
		_companies = result ?? [];
	}

	private async Task LoadVouchers()
	{
		var result = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);
		_vouchers = result ?? [];
	}

	private async Task LoadLedgers()
	{
		var result = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
		_ledgers = result ?? [];
	}

	private void MapSelections()
	{
		// Map Company
		if (!string.IsNullOrEmpty(_primaryCompanyLinkingId) && long.TryParse(_primaryCompanyLinkingId, out var companyId))
		{
			var company = _companies.FirstOrDefault(c => c.Id == companyId);
			_selectedCompanyName = company?.Name ?? string.Empty;
		}

		// Map Vouchers
		if (!string.IsNullOrEmpty(_saleVoucherId) && long.TryParse(_saleVoucherId, out var saleVoucherId))
		{
			var voucher = _vouchers.FirstOrDefault(v => v.Id == saleVoucherId);
			_selectedSaleVoucherName = voucher?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_saleReturnVoucherId) && long.TryParse(_saleReturnVoucherId, out var saleReturnVoucherId))
		{
			var voucher = _vouchers.FirstOrDefault(v => v.Id == saleReturnVoucherId);
			_selectedSaleReturnVoucherName = voucher?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_stockTransferVoucherId) && long.TryParse(_stockTransferVoucherId, out var stockTransferVoucherId))
		{
			var voucher = _vouchers.FirstOrDefault(v => v.Id == stockTransferVoucherId);
			_selectedStockTransferVoucherName = voucher?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_purchaseVoucherId) && long.TryParse(_purchaseVoucherId, out var purchaseVoucherId))
		{
			var voucher = _vouchers.FirstOrDefault(v => v.Id == purchaseVoucherId);
			_selectedPurchaseVoucherName = voucher?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_purchaseReturnVoucherId) && long.TryParse(_purchaseReturnVoucherId, out var purchaseReturnVoucherId))
		{
			var voucher = _vouchers.FirstOrDefault(v => v.Id == purchaseReturnVoucherId);
			_selectedPurchaseReturnVoucherName = voucher?.Name ?? string.Empty;
		}

		// Map Ledgers
		if (!string.IsNullOrEmpty(_saleLedgerId) && long.TryParse(_saleLedgerId, out var saleLedgerId))
		{
			var ledger = _ledgers.FirstOrDefault(l => l.Id == saleLedgerId);
			_selectedSaleLedgerName = ledger?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_stockTransferLedgerId) && long.TryParse(_stockTransferLedgerId, out var stockTransferLedgerId))
		{
			var ledger = _ledgers.FirstOrDefault(l => l.Id == stockTransferLedgerId);
			_selectedStockTransferLedgerName = ledger?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_purchaseLedgerId) && long.TryParse(_purchaseLedgerId, out var purchaseLedgerId))
		{
			var ledger = _ledgers.FirstOrDefault(l => l.Id == purchaseLedgerId);
			_selectedPurchaseLedgerName = ledger?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_cashLedgerId) && long.TryParse(_cashLedgerId, out var cashLedgerId))
		{
			var ledger = _ledgers.FirstOrDefault(l => l.Id == cashLedgerId);
			_selectedCashLedgerName = ledger?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_cashSalesLedgerId) && long.TryParse(_cashSalesLedgerId, out var cashSalesLedgerId))
		{
			var ledger = _ledgers.FirstOrDefault(l => l.Id == cashSalesLedgerId);
			_selectedCashSalesLedgerName = ledger?.Name ?? string.Empty;
		}

		if (!string.IsNullOrEmpty(_gstLedgerId) && long.TryParse(_gstLedgerId, out var gstLedgerId))
		{
			var ledger = _ledgers.FirstOrDefault(l => l.Id == gstLedgerId);
			_selectedGSTLedgerName = ledger?.Name ?? string.Empty;
		}
	}

	#endregion

	#region Autocomplete Change Handlers

	private void OnCompanyChange(ChangeEventArgs<string, CompanyModel> args)
	{
		if (args.ItemData != null)
		{
			_primaryCompanyLinkingId = args.ItemData.Id.ToString();
		}
	}

	private void OnSaleVoucherChange(ChangeEventArgs<string, VoucherModel> args)
	{
		if (args.ItemData != null)
		{
			_saleVoucherId = args.ItemData.Id.ToString();
		}
	}

	private void OnSaleReturnVoucherChange(ChangeEventArgs<string, VoucherModel> args)
	{
		if (args.ItemData != null)
		{
			_saleReturnVoucherId = args.ItemData.Id.ToString();
		}
	}

	private void OnStockTransferVoucherChange(ChangeEventArgs<string, VoucherModel> args)
	{
		if (args.ItemData != null)
		{
			_stockTransferVoucherId = args.ItemData.Id.ToString();
		}
	}

	private void OnPurchaseVoucherChange(ChangeEventArgs<string, VoucherModel> args)
	{
		if (args.ItemData != null)
		{
			_purchaseVoucherId = args.ItemData.Id.ToString();
		}
	}

	private void OnPurchaseReturnVoucherChange(ChangeEventArgs<string, VoucherModel> args)
	{
		if (args.ItemData != null)
		{
			_purchaseReturnVoucherId = args.ItemData.Id.ToString();
		}
	}

	private void OnSaleLedgerChange(ChangeEventArgs<string, LedgerModel> args)
	{
		if (args.ItemData != null)
		{
			_saleLedgerId = args.ItemData.Id.ToString();
		}
	}

	private void OnStockTransferLedgerChange(ChangeEventArgs<string, LedgerModel> args)
	{
		if (args.ItemData != null)
		{
			_stockTransferLedgerId = args.ItemData.Id.ToString();
		}
	}

	private void OnPurchaseLedgerChange(ChangeEventArgs<string, LedgerModel> args)
	{
		if (args.ItemData != null)
		{
			_purchaseLedgerId = args.ItemData.Id.ToString();
		}
	}

	private void OnCashLedgerChange(ChangeEventArgs<string, LedgerModel> args)
	{
		if (args.ItemData != null)
		{
			_cashLedgerId = args.ItemData.Id.ToString();
		}
	}

	private void OnCashSalesLedgerChange(ChangeEventArgs<string, LedgerModel> args)
	{
		if (args.ItemData != null)
		{
			_cashSalesLedgerId = args.ItemData.Id.ToString();
		}
	}

	private void OnGSTLedgerChange(ChangeEventArgs<string, LedgerModel> args)
	{
		if (args.ItemData != null)
		{
			_gstLedgerId = args.ItemData.Id.ToString();
		}
	}

	#endregion

	#region Save Settings

	private async Task SaveSettings()
	{
		if (_isProcessing) return;

		try
		{
			_isProcessing = true;

			// Validate required fields
			if (string.IsNullOrWhiteSpace(_primaryCompanyLinkingId))
			{
				await ShowToast("Validation Error", "Primary Company is required.", "error");
				return;
			}

			await ShowToast("Processing Transaction", "Please wait while the transaction is being saved...", "success");

			// Save all settings
			var settings = await CommonData.LoadTableData<SettingsModel>(TableNames.Settings);

			await UpdateSetting(SettingsKeys.PrimaryCompanyLinkingId, _primaryCompanyLinkingId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.PrimaryCompanyLinkingId).Description);

			await UpdateSetting(SettingsKeys.RawMaterialCodePrefix, _rawMaterialCodePrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.RawMaterialCodePrefix).Description);
			await UpdateSetting(SettingsKeys.FinishedProductCodePrefix, _finishedProductCodePrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.FinishedProductCodePrefix).Description);
			await UpdateSetting(SettingsKeys.LedgerCodePrefix, _ledgerCodePrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.LedgerCodePrefix).Description);
			await UpdateSetting(SettingsKeys.PurchaseTransactionPrefix, _purchaseTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.PurchaseTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.PurchaseReturnTransactionPrefix, _purchaseReturnTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.PurchaseReturnTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.KitchenIssueTransactionPrefix, _kitchenIssueTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.KitchenIssueTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.KitchenProductionTransactionPrefix, _kitchenProductionTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.KitchenProductionTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix, _rawMaterialStockAdjustmentTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.ProductStockAdjustmentTransactionPrefix, _productStockAdjustmentTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.ProductStockAdjustmentTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.SaleTransactionPrefix, _saleTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.SaleTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.SaleReturnTransactionPrefix, _saleReturnTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.SaleReturnTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.StockTransferTransactionPrefix, _stockTransferTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.StockTransferTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.OrderTransactionPrefix, _orderTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.OrderTransactionPrefix).Description);
			await UpdateSetting(SettingsKeys.AccountingTransactionPrefix, _accountingTransactionPrefix, settings.FirstOrDefault(_ => _.Key == SettingsKeys.AccountingTransactionPrefix).Description);

			await UpdateSetting(SettingsKeys.SaleVoucherId, _saleVoucherId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.SaleVoucherId).Description);
			await UpdateSetting(SettingsKeys.SaleReturnVoucherId, _saleReturnVoucherId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.SaleReturnVoucherId).Description);
			await UpdateSetting(SettingsKeys.StockTransferVoucherId, _stockTransferVoucherId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.StockTransferVoucherId).Description);
			await UpdateSetting(SettingsKeys.PurchaseVoucherId, _purchaseVoucherId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.PurchaseVoucherId).Description);
			await UpdateSetting(SettingsKeys.PurchaseReturnVoucherId, _purchaseReturnVoucherId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.PurchaseReturnVoucherId).Description);
			await UpdateSetting(SettingsKeys.SaleLedgerId, _saleLedgerId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.SaleLedgerId).Description);
			await UpdateSetting(SettingsKeys.StockTransferLedgerId, _stockTransferLedgerId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.StockTransferLedgerId).Description);
			await UpdateSetting(SettingsKeys.PurchaseLedgerId, _purchaseLedgerId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.PurchaseLedgerId).Description);
			await UpdateSetting(SettingsKeys.CashLedgerId, _cashLedgerId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.CashLedgerId).Description);
			await UpdateSetting(SettingsKeys.CashSalesLedgerId, _cashSalesLedgerId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.CashSalesLedgerId).Description);
			await UpdateSetting(SettingsKeys.GSTLedgerId, _gstLedgerId, settings.FirstOrDefault(_ => _.Key == SettingsKeys.GSTLedgerId).Description);
			await UpdateSetting(SettingsKeys.UpdateItemMasterRateOnPurchase, _updateItemMasterRateOnPurchase.ToString(), settings.FirstOrDefault(_ => _.Key == SettingsKeys.UpdateItemMasterRateOnPurchase).Description);
			await UpdateSetting(SettingsKeys.UpdateItemMasterUOMOnPurchase, _updateItemMasterUOMOnPurchase.ToString(), settings.FirstOrDefault(_ => _.Key == SettingsKeys.UpdateItemMasterUOMOnPurchase).Description);

			await ShowToast("Success", "Settings saved successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Save Error", $"Failed to save settings: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private static async Task UpdateSetting(string key, string value, string description)
	{
		var setting = new SettingsModel
		{
			Key = key,
			Value = value ?? string.Empty,
			Description = description
		};

		await SettingsData.UpdateSettings(setting);
	}

	#endregion

	#region Reset Settings

	private void ShowResetConfirmation()
	{
		_isResetDialogVisible = true;
	}

	private void CancelReset()
	{
		_isResetDialogVisible = false;
	}

	private async Task ConfirmReset()
	{
		try
		{
			_isResetDialogVisible = false;
			_isProcessing = true;

			await ShowToast("Processing Transaction", "Please wait while the transaction is being saved...", "success");

			await SettingsData.ResetSettings();

			// Reload data
			await LoadData();

			await ShowToast("Success", "Settings have been reset to default values.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Reset Error", $"Failed to reset settings: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	#endregion

	#region Toast Notifications

	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "success")
		{
			await _sfSuccessToast?.ShowAsync(new()
			{
				Title = title,
				Content = message
			});
		}
		else
		{
			await _sfErrorToast?.ShowAsync(new()
			{
				Title = title,
				Content = message
			});
		}
	}

	#endregion

	public async ValueTask DisposeAsync()
	{
		await _hotKeysContext.DisposeAsync();
	}
}