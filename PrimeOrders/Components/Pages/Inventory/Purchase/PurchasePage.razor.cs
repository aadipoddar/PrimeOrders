using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Exporting.Purchase;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Inventory.Purchase;

public partial class PurchasePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	[Parameter] public int? PurchaseId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _billDetailsDialogVisible = false;
	private bool _supplierDialogVisible = false;
	private bool _adjustmentsDialogVisible = false;
	private bool _purchaseSummaryDialogVisible = false;

	private decimal _baseTotal = 0;
	private decimal _afterDiscounts = 0;
	private decimal _subTotal = 0;
	private decimal _total = 0;
	private decimal _selectedQuantity = 1;
	private decimal _selectedRate = 0;
	private string _selectedMeasurementUnit;

	private int _selectedRawMaterialId = 0;

	private string _materialSearchText = "";
	private int _selectedMaterialIndex = -1;
	private List<RawMaterialModel> _filteredRawMaterials = [];
	private bool _isMaterialSearchActive = false;
	private bool _hasAddedMaterialViaSearch = true;

	private PurchaseRawMaterialCartModel _selectedRawMaterialCart = new();
	private RawMaterialModel _selectedRawMaterial = new();

	private LedgerModel _supplier = new();
	private PurchaseModel _purchase = new() { BillDateTime = DateTime.Now, Status = true, Remarks = "" };

	private List<LedgerModel> _suppliers;
	private List<RawMaterialModel> _rawMaterials;
	private readonly List<PurchaseRawMaterialCartModel> _purchaseRawMaterialCarts = [];

	private SfGrid<RawMaterialModel> _sfRawMaterialGrid;
	private SfGrid<PurchaseRawMaterialCartModel> _sfRawMaterialCartGrid;

	private SfDialog _sfBillDetailsDialog;
	private SfDialog _sfSupplierDialog;
	private SfDialog _sfAdjustmentsDialog;
	private SfDialog _sfPurchaseSummaryDialog;
	private SfDialog _sfRawMaterialManageDialog;
	private SfDialog _sfQuantityDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region LoadData
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory, true)).User) is not null))
			return;

		await LoadData();
		await JS.InvokeVoidAsync("setupPurchasePageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_suppliers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
		_supplier = _suppliers.FirstOrDefault();
		_purchase.SupplierId = _supplier?.Id ?? 0;

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(_purchase.SupplierId, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_purchase.BillDateTime, "India Standard Time"));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;
		_filteredRawMaterials = [.. _rawMaterials];

		if (PurchaseId.HasValue && PurchaseId.Value > 0)
			await LoadPurchase();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task LoadPurchase()
	{
		_purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, PurchaseId.Value);

		if (_purchase is null)
			NavManager.NavigateTo("/Inventory-Dashboard");

		_supplier = _suppliers.FirstOrDefault(s => s.Id == _purchase.SupplierId);
		_purchaseRawMaterialCarts.Clear();

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(_purchase.SupplierId, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_purchase.BillDateTime, "India Standard Time"));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;
		_filteredRawMaterials = [.. _rawMaterials];

		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(_purchase.Id);
		foreach (var item in purchaseDetails)
		{
			var product = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, item.RawMaterialId);

			_purchaseRawMaterialCarts.Add(new()
			{
				RawMaterialId = item.RawMaterialId,
				RawMaterialName = product.Name,
				Quantity = item.Quantity,
				MeasurementUnit = item.MeasurementUnit,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				SGSTPercent = item.SGSTPercent,
				IGSTPercent = item.IGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTAmount = item.SGSTAmount,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total,
				NetRate = item.NetRate,
			});
		}

		if (_sfRawMaterialCartGrid is not null)
			await _sfRawMaterialCartGrid.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Keyboard Navigation Methods
	[JSInvokable]
	public async Task HandleKeyboardShortcut(string key)
	{
		if (_isMaterialSearchActive)
		{
			await HandleMaterialSearchKeyboard(key);
			return;
		}

		switch (key.ToLower())
		{
			case "f2":
				await StartMaterialSearch();
				break;

			case "escape":
				await HandleEscape();
				break;
		}
	}

	private async Task HandleMaterialSearchKeyboard(string key)
	{
		switch (key.ToLower())
		{
			case "escape":
				await ExitMaterialSearch();
				break;

			case "enter":
				await SelectCurrentMaterial();
				break;

			case "arrowdown":
				NavigateMaterialSelection(1);
				break;

			case "arrowup":
				NavigateMaterialSelection(-1);
				break;

			case "backspace":
				if (_materialSearchText.Length > 0)
				{
					_materialSearchText = _materialSearchText[..^1];
					await FilterMaterials();
				}
				break;

			default:
				// Add character to search if it's alphanumeric or space
				if (key.Length == 1 && (char.IsLetterOrDigit(key[0]) || key == " "))
				{
					_materialSearchText += key.ToUpper();
					await FilterMaterials();
				}
				break;
		}

		StateHasChanged();
	}

	private async Task StartMaterialSearch()
	{
		_hasAddedMaterialViaSearch = true;
		_isMaterialSearchActive = true;
		_materialSearchText = "";
		_selectedMaterialIndex = 0;
		_filteredRawMaterials = [.. _rawMaterials];

		if (_filteredRawMaterials.Count > 0)
			_selectedRawMaterial = _filteredRawMaterials[0];

		StateHasChanged();
		await JS.InvokeVoidAsync("showMaterialSearchIndicator", _materialSearchText);
	}

	private async Task ExitMaterialSearch()
	{
		_isMaterialSearchActive = false;
		_materialSearchText = "";
		_selectedMaterialIndex = -1;
		StateHasChanged();
		await JS.InvokeVoidAsync("hideMaterialSearchIndicator");
	}

	private async Task FilterMaterials()
	{
		if (string.IsNullOrEmpty(_materialSearchText))
			_filteredRawMaterials = [.. _rawMaterials];
		else
			_filteredRawMaterials = [.. _rawMaterials.Where(m =>
				m.Name.Contains(_materialSearchText, StringComparison.OrdinalIgnoreCase) ||
				m.Code != null && m.Code.Contains(_materialSearchText, StringComparison.OrdinalIgnoreCase)
			)];

		_selectedMaterialIndex = 0;
		if (_filteredRawMaterials.Count > 0)
			_selectedRawMaterial = _filteredRawMaterials[0];

		await JS.InvokeVoidAsync("updateMaterialSearchIndicator", _materialSearchText, _filteredRawMaterials.Count);
		StateHasChanged();
	}

	private void NavigateMaterialSelection(int direction)
	{
		if (_filteredRawMaterials.Count == 0) return;

		_selectedMaterialIndex += direction;

		if (_selectedMaterialIndex < 0)
			_selectedMaterialIndex = _filteredRawMaterials.Count - 1;
		else if (_selectedMaterialIndex >= _filteredRawMaterials.Count)
			_selectedMaterialIndex = 0;

		_selectedRawMaterial = _filteredRawMaterials[_selectedMaterialIndex];
		StateHasChanged();
	}

	private async Task SelectCurrentMaterial()
	{
		if (_selectedRawMaterial.Id > 0)
		{
			SetQuantityDialogDefaults();
			_quantityDialogVisible = true;
			await ExitMaterialSearch();
			StateHasChanged();
		}
	}

	private async Task HandleEscape()
	{
		if (_isMaterialSearchActive)
			await ExitMaterialSearch();

		StateHasChanged();
	}
	#endregion

	#region Purchase Details Events
	private async Task OnSupplierChanged(ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		if (args.ItemData is null || args.ItemData.Id <= 0)
			_supplier = _suppliers.FirstOrDefault();

		else
			_supplier = args.ItemData ?? new();

		_purchase.SupplierId = _supplier.Id;

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(_purchase.SupplierId, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_purchase.BillDateTime, "India Standard Time"));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;
		_filteredRawMaterials = [.. _rawMaterials];

		if (_sfRawMaterialGrid is not null)
			await _sfRawMaterialGrid.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void CashDiscountPercentValueChanged(decimal args)
	{
		_purchase.CDPercent = args;
		UpdateFinancialDetails();
		StateHasChanged();
	}

	public async Task PurchaseDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_purchase.BillDateTime = args.Value;

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(_purchase.SupplierId, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_purchase.BillDateTime, "India Standard Time"));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;
		_filteredRawMaterials = [.. _rawMaterials];

		if (_sfRawMaterialGrid is not null)
			await _sfRawMaterialGrid.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void UpdateFinancialDetails()
	{
		_purchase.SupplierId = _supplier?.Id ?? 0;
		_purchase.UserId = _user?.Id ?? 0;

		foreach (var item in _purchaseRawMaterialCarts)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = (item.AfterDiscount - (item.AfterDiscount * (_purchase.CDPercent / 100))
				+ (item.CGSTAmount + item.SGSTAmount + item.IGSTAmount)) / item.Quantity;
		}

		_baseTotal = _purchaseRawMaterialCarts.Sum(c => c.BaseTotal);
		_afterDiscounts = _purchaseRawMaterialCarts.Sum(c => c.AfterDiscount);
		_subTotal = _purchaseRawMaterialCarts.Sum(c => c.Total);
		_purchase.CDAmount = _subTotal * (_purchase.CDPercent / 100);
		_total = _subTotal - _purchase.CDAmount;

		_sfRawMaterialCartGrid?.Refresh();
		_sfRawMaterialGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Raw Materials
	private void OnAddToCartButtonClick(RawMaterialModel material)
	{
		if (material is null || material.Id <= 0)
			return;

		_selectedRawMaterial = material;
		SetQuantityDialogDefaults();
		_quantityDialogVisible = true;
		_hasAddedMaterialViaSearch = false;
		StateHasChanged();
	}

	private void SetQuantityDialogDefaults()
	{
		_selectedQuantity = 1;
		_selectedRate = _selectedRawMaterial.MRP;
		_selectedMeasurementUnit = _selectedRawMaterial.MeasurementUnit;
	}

	public void RawMaterialCartRowSelectHandler(RowSelectEventArgs<PurchaseRawMaterialCartModel> args)
	{
		_selectedRawMaterialCart = args.Data;
		_dialogVisible = true;
		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Dialog Events
	private async Task OnAddToCartClick()
	{
		if (_selectedQuantity <= 0)
		{
			OnCancelQuantityDialogClick();
			return;
		}

		var existingProduct = _purchaseRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterial.Id);

		if (existingProduct is not null)
		{
			existingProduct.Quantity += _selectedQuantity;
			existingProduct.Rate = _selectedRate;
			existingProduct.MeasurementUnit = _selectedMeasurementUnit.ToString();
		}
		else
		{
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, _selectedRawMaterial.TaxId);

			_purchaseRawMaterialCarts.Add(new()
			{
				RawMaterialId = _selectedRawMaterial.Id,
				RawMaterialName = _selectedRawMaterial.Name,
				Quantity = _selectedQuantity,
				Rate = _selectedRate,
				MeasurementUnit = _selectedMeasurementUnit,
				BaseTotal = _selectedRate * _selectedQuantity,
				DiscPercent = 0,
				CGSTPercent = productTax.Extra ? productTax.CGST : 0,
				SGSTPercent = productTax.Extra ? productTax.SGST : 0,
				IGSTPercent = productTax.Extra ? productTax.IGST : 0
				// Rest handled in the UpdateFinancialDetails()
			});
		}

		_quantityDialogVisible = false;
		_selectedRawMaterial = new();
		await _sfRawMaterialCartGrid?.Refresh();
		await _sfRawMaterialGrid?.Refresh();
		UpdateFinancialDetails();

		if (_hasAddedMaterialViaSearch)
			await StartMaterialSearch();

		StateHasChanged();
	}

	private void OnCancelQuantityDialogClick()
	{
		_quantityDialogVisible = false;
		_selectedRawMaterial = new();
		StateHasChanged();
	}

	private void DialogRateValueChanged(decimal args)
	{
		_selectedRawMaterialCart.Rate = args;
		UpdateModalFinancialDetails();
	}

	private void DialogQuantityValueChanged(decimal args)
	{
		_selectedRawMaterialCart.Quantity = args;
		UpdateModalFinancialDetails();
	}

	private void DialogMeasuringUnitValueChanged(string args)
	{
		_selectedRawMaterialCart.MeasurementUnit = args;
		UpdateModalFinancialDetails();
	}

	private void DialogDiscPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.DiscPercent = args;
		UpdateModalFinancialDetails();
	}

	private void DialogCGSTPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.CGSTPercent = args;
		UpdateModalFinancialDetails();
	}

	private void DialogSGSTPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.SGSTPercent = args;
		UpdateModalFinancialDetails();
	}

	private void DialogIGSTPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.IGSTPercent = args;
		UpdateModalFinancialDetails();
	}

	private void UpdateModalFinancialDetails()
	{
		_selectedRawMaterialCart.BaseTotal = _selectedRawMaterialCart.Rate * _selectedRawMaterialCart.Quantity;
		_selectedRawMaterialCart.DiscAmount = _selectedRawMaterialCart.BaseTotal * (_selectedRawMaterialCart.DiscPercent / 100);
		_selectedRawMaterialCart.AfterDiscount = _selectedRawMaterialCart.BaseTotal - _selectedRawMaterialCart.DiscAmount;
		_selectedRawMaterialCart.CGSTAmount = _selectedRawMaterialCart.AfterDiscount * (_selectedRawMaterialCart.CGSTPercent / 100);
		_selectedRawMaterialCart.SGSTAmount = _selectedRawMaterialCart.AfterDiscount * (_selectedRawMaterialCart.SGSTPercent / 100);
		_selectedRawMaterialCart.IGSTAmount = _selectedRawMaterialCart.AfterDiscount * (_selectedRawMaterialCart.IGSTPercent / 100);
		_selectedRawMaterialCart.Total = _selectedRawMaterialCart.AfterDiscount + _selectedRawMaterialCart.CGSTAmount + _selectedRawMaterialCart.SGSTAmount + _selectedRawMaterialCart.IGSTAmount;

		StateHasChanged();
	}

	private async Task OnSaveRawMaterialManageClick()
	{
		_purchaseRawMaterialCarts.Remove(_purchaseRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		if (_selectedRawMaterialCart.Quantity > 0)
			_purchaseRawMaterialCarts.Add(_selectedRawMaterialCart);

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnRemoveFromCartRawMaterialManageClick()
	{
		_selectedRawMaterialCart.Quantity = 0;
		_purchaseRawMaterialCarts.Remove(_purchaseRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		UpdateFinancialDetails();

		_purchase.BillDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(_purchase.BillDateTime)
			.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)),
			"India Standard Time");

		StateHasChanged();

		if (_purchaseRawMaterialCarts.Count == 0 || _purchaseRawMaterialCarts is null)
		{
			_sfErrorToast.Content = "Please add at least one raw material to the purchase.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_purchase.SupplierId <= 0)
		{
			_sfErrorToast.Content = "Please select a supplier.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_purchase.BillNo))
		{
			_sfErrorToast.Content = "Bill No is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_purchase.UserId == 0)
		{
			_sfErrorToast.Content = "User is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task<bool> SavePurchase()
	{
		if (!await ValidateForm())
			return false;

		StateHasChanged();

		_purchase.Id = await PurchaseData.InsertPurchase(_purchase);
		if (_purchase.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save Purchase.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return false;
		}

		await InsertPurchaseDetail();
		await InsertStock();
		int accountingId = await InsertAccounting();
		await InsertAccountingDetails(accountingId);

		return true;
	}

	private async Task InsertPurchaseDetail()
	{
		if (PurchaseId.HasValue && PurchaseId.Value > 0)
		{
			var existingPurchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(PurchaseId.Value);
			foreach (var item in existingPurchaseDetails)
			{
				item.Status = false;
				await PurchaseData.InsertPurchaseDetail(item);
			}
		}

		foreach (var item in _purchaseRawMaterialCarts)
			await PurchaseData.InsertPurchaseDetail(new()
			{
				Id = 0,
				PurchaseId = _purchase.Id,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				MeasurementUnit = item.MeasurementUnit.ToUpper(),
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total,
				NetRate = item.NetRate,
				Status = true
			});
	}

	private async Task InsertStock()
	{
		if (PurchaseId.HasValue && PurchaseId.Value > 0)
			await StockData.DeleteRawMaterialStockByTransactionNo(_purchase.BillNo);

		foreach (var item in _purchaseRawMaterialCarts)
			await StockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				NetRate = item.NetRate,
				Type = StockType.Purchase.ToString(),
				TransactionNo = _purchase.BillNo,
				TransactionDate = DateOnly.FromDateTime(_purchase.BillDateTime),
				LocationId = _user.LocationId
			});
	}

	private async Task<int> InsertAccounting()
	{
		if (PurchaseId.HasValue && PurchaseId.Value > 0)
		{
			var existingAccounting = await AccountingData.LoadAccountingByReferenceNo(_purchase.BillNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		return await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			ReferenceNo = _purchase.BillNo,
			AccountingDate = DateOnly.FromDateTime(_purchase.BillDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(DateOnly.FromDateTime(_purchase.BillDateTime))).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseVoucherId)).Value),
			Remarks = _purchase.Remarks,
			UserId = _purchase.UserId,
			GeneratedModule = GeneratedModules.Purchase.ToString(),
			Status = true
		});
	}

	private async Task InsertAccountingDetails(int accountingId)
	{
		var purchaseOverview = await PurchaseData.LoadPurchaseOverviewByPurchaseId(_purchase.Id);

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = purchaseOverview.SupplierId,
			Debit = null,
			Credit = purchaseOverview.Total,
			Remarks = $"Cash / Party Account Posting For Purchase Bill {purchaseOverview.BillNo}",
			Status = true
		});

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PurchaseLedgerId)).Value),
			Debit = purchaseOverview.Total - purchaseOverview.TotalTaxAmount,
			Credit = null,
			Remarks = $"Purchase Account Posting For Purchase Bill {purchaseOverview.BillNo}",
			Status = true
		});

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
			Debit = purchaseOverview.TotalTaxAmount,
			Credit = null,
			Remarks = $"GST Account Posting For Purchase Bill {purchaseOverview.BillNo}",
			Status = true
		});
	}

	// Button 1: Save Only
	private async Task OnSavePurchaseClick()
	{
		if (await SavePurchase())
		{
			_sfSuccessToast.Content = "Purchase saved successfully.";
			await _sfSuccessToast.ShowAsync();
		}

		_purchaseSummaryDialogVisible = false;
		StateHasChanged();
	}

	// Button 2: Save and A4 Prints
	private async Task OnSaveAndPrintClick()
	{


		if (await SavePurchase())
		{
			await PrintInvoice();
			_sfSuccessToast.Content = "Purchase saved and invoice generated successfully.";
			await _sfSuccessToast.ShowAsync();
		}

		_purchaseSummaryDialogVisible = false;
		StateHasChanged();
	}

	private async Task PrintInvoice()
	{
		var memoryStream = await PurchaseA4Print.GenerateA4PurchaseBill(_purchase.Id);
		var fileName = $"Purchase_Invoice_{_purchase.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await JS.InvokeVoidAsync("savePDF", Convert.ToBase64String(memoryStream.ToArray()), fileName);
	}
	#endregion
}