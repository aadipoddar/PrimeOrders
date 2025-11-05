using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase;

public partial class PurchaseReturnPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _autoGenerateTransactionNo = false;
	private bool _isUploadDialogVisible = false;

	private decimal _itemBaseTotal = 0;
	private decimal _itemDiscountTotal = 0;
	private decimal _itemAfterDiscountTotal = 0;
	private decimal _itemTaxTotal = 0;
	private decimal _itemAfterTaxTotal = 0;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private RawMaterialModel? _selectedRawMaterial = new();
	private PurchaseReturnItemCartModel _selectedCart = new();
	private PurchaseReturnModel _purchaseReturn = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<TaxModel> _taxes = [];
	private List<PurchaseReturnItemCartModel> _cart = [];

	private SfAutoComplete<RawMaterialModel?, RawMaterialModel> _sfItemAutoComplete;
	private SfGrid<PurchaseReturnItemCartModel> _sfCartGrid;
	private SfDialog _uploadDocumentDialog;
	private SfUploader _sfDocumentUploader;

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

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		await LoadCompanies();
		await LoadLedgers();
		await LoadExistingPurchase();
		await LoadItems();
		await LoadExistingCart();
		await SavePurchaseReturnFile();
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

	private async Task LoadLedgers()
	{
		try
		{
			_parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
			_parties = [.. _parties.OrderBy(s => s.Name)];
			_parties.Add(new()
			{
				Id = 0,
				Name = "Create New Party Ledger..."
			});

			_selectedParty = _parties.FirstOrDefault();
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Ledgers", ex.Message, "error");
		}
	}

	private async Task LoadExistingPurchase()
	{
		try
		{
			if (Id.HasValue)
			{
				_purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, Id.Value);
				if (_purchaseReturn is null)
				{
					await ShowToast("Purchase Return Not Found", "The requested purchase return could not be found.", "error");
					NavigationManager.NavigateTo("/inventory/purchasereturn", true);
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.PurchaseReturnDataFileName))
				_purchaseReturn = System.Text.Json.JsonSerializer.Deserialize<PurchaseReturnModel>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseReturnDataFileName));

			else
			{
				_purchaseReturn = new()
				{
					Id = 0,
					TransactionNo = string.Empty,
					CompanyId = _selectedCompany.Id,
					PartyId = _selectedParty.Id,
					TransactionDateTime = await CommonData.LoadCurrentDateTime(),
					FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
					CreatedBy = _user.Id,
					ItemsTotalAmount = 0,
					CashDiscountPercent = 0,
					CashDiscountAmount = 0,
					OtherChargesPercent = 0,
					OtherChargesAmount = 0,
					RoundOffAmount = 0,
					TotalAmount = 0,
					Remarks = "",
					CreatedAt = DateTime.Now,
					CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
					Status = true,
					LastModifiedAt = null,
					LastModifiedBy = null,
					LastModifiedFromPlatform = null
				};
				await DeleteLocalFiles();
			}

			if (_purchaseReturn.CompanyId > 0)
				_selectedCompany = _companies.FirstOrDefault(s => s.Id == _purchaseReturn.CompanyId);
			else
			{
				_selectedCompany = _companies.FirstOrDefault();
				_purchaseReturn.CompanyId = _selectedCompany.Id;
			}

			if (_purchaseReturn.PartyId > 0)
				_selectedParty = _parties.FirstOrDefault(s => s.Id == _purchaseReturn.PartyId);
			else
			{
				_selectedParty = _parties.FirstOrDefault();
				_purchaseReturn.PartyId = _selectedParty.Id;
			}

			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _purchaseReturn.FinancialYearId);
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Purchase Data", ex.Message, "error");
			await DeleteLocalFiles();
		}
		finally
		{
			await SavePurchaseReturnFile();
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(_purchaseReturn.PartyId, _purchaseReturn.TransactionDateTime);
			_taxes = await CommonData.LoadTableDataByStatus<TaxModel>(TableNames.Tax);

			_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
			_rawMaterials.Add(new()
			{
				Id = 0,
				Name = "Create New Item ..."
			});
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Items", ex.Message, "error");
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (_purchaseReturn.Id > 0)
			{
				var existingCart = await PurchaseReturnData.LoadPurchaseReturnDetailByPurchaseReturn(_purchaseReturn.Id);

				foreach (var item in existingCart)
				{
					if (_rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId) is null)
					{
						await ShowToast("Item Not Found", $"The item with ID {item.RawMaterialId} in the existing purchase return cart was not found in the available items list. It may have been deleted or is inaccessible.", "error");
						continue;
					}

					_cart.Add(new()
					{
						ItemId = item.RawMaterialId,
						ItemName = _rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId)?.Name ?? "",
						Quantity = item.Quantity,
						UnitOfMeasurement = item.UnitOfMeasurement,
						Rate = item.Rate,
						BaseTotal = item.BaseTotal,
						DiscountPercent = item.DiscountPercent,
						DiscountAmount = item.DiscountAmount,
						AfterDiscount = item.AfterDiscount,
						CGSTPercent = item.CGSTPercent,
						CGSTAmount = item.CGSTAmount,
						SGSTPercent = item.SGSTPercent,
						SGSTAmount = item.SGSTAmount,
						IGSTPercent = item.IGSTPercent,
						IGSTAmount = item.IGSTAmount,
						TotalTaxAmount = item.TotalTaxAmount,
						Total = item.Total,
						InclusiveTax = item.InclusiveTax,
						NetRate = item.NetRate,
						Remarks = item.Remarks
					});
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.PurchaseReturnCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<PurchaseReturnItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseReturnCartDataFileName));
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Existing Cart", ex.Message, "error");
			await DeleteLocalFiles();
		}
		finally
		{
			await SavePurchaseReturnFile();
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
				await JSRuntime.InvokeVoidAsync("open", "/Admin/Company", "_blank");
			else
				NavigationManager.NavigateTo("/Admin/Company");

			return;
		}

		_selectedCompany = args.Value;
		_purchaseReturn.CompanyId = _selectedCompany.Id;

		await SavePurchaseReturnFile();
	}

	private async Task OnPartyChanged(ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", "/Admin/Ledger", "_blank");
			else
				NavigationManager.NavigateTo("/Admin/Ledger");

			return;
		}

		_selectedParty = args.Value;
		_purchaseReturn.PartyId = _selectedParty.Id;

		await LoadItems();
		await SavePurchaseReturnFile();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_purchaseReturn.TransactionDateTime = args.Value;
		await LoadItems();
		await SavePurchaseReturnFile();
	}

	private async Task OnAutoGenerateTransactionNoChecked(Syncfusion.Blazor.Buttons.ChangeEventArgs<bool> args)
	{
		_autoGenerateTransactionNo = args.Checked;
		await SavePurchaseReturnFile();
	}

	private async Task OnCashDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_purchaseReturn.CashDiscountPercent = args.Value;
		await SavePurchaseReturnFile();
	}

	private async Task OnOtherDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_purchaseReturn.OtherChargesPercent = args.Value;
		await SavePurchaseReturnFile();
	}

	private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
	{
		_purchaseReturn.RoundOffAmount = args.Value;
		await SavePurchaseReturnFile(true);
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<RawMaterialModel?, RawMaterialModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", "/Admin/RawMaterial", "_blank");
			else
				NavigationManager.NavigateTo("/Admin/RawMaterial");

			return;
		}

		_selectedRawMaterial = args.Value;

		if (_selectedRawMaterial is null)
			_selectedCart = new()
			{
				ItemId = 0,
				ItemName = "",
				Quantity = 1,
				UnitOfMeasurement = "",
				Rate = 0,
				DiscountPercent = 0,
				CGSTPercent = 0,
				SGSTPercent = 0,
				IGSTPercent = 0
			};

		else
		{
			var isSameState = _selectedParty.StateUTId == _selectedCompany.StateUTId;

			_selectedCart.ItemId = _selectedRawMaterial.Id;
			_selectedCart.ItemName = _selectedRawMaterial.Name;
			_selectedCart.Quantity = 1;
			_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;
			_selectedCart.Rate = _selectedRawMaterial.Rate;
			_selectedCart.DiscountPercent = 0;
			_selectedCart.CGSTPercent = _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).CGST;
			_selectedCart.SGSTPercent = isSameState ? _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).SGST : 0;
			_selectedCart.IGSTPercent = isSameState ? 0 : _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId).IGST;
		}

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Quantity = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemRateChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Rate = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.DiscountPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemCGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.CGSTPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemSGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.SGSTPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemIGSTPercentChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.IGSTPercent = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemInclusiveTaxChanged(Syncfusion.Blazor.Buttons.ChangeEventArgs<bool> args)
	{
		_selectedCart.InclusiveTax = args.Checked;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedRawMaterial is null)
			return;

		if (_selectedCart.Quantity <= 0)
			_selectedCart.Quantity = 1;

		if (string.IsNullOrWhiteSpace(_selectedCart.UnitOfMeasurement))
			_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;

		_selectedCart.ItemId = _selectedRawMaterial.Id;
		_selectedCart.ItemName = _selectedRawMaterial.Name;
		_selectedCart.BaseTotal = _selectedCart.Rate * _selectedCart.Quantity;
		_selectedCart.DiscountAmount = _selectedCart.BaseTotal * (_selectedCart.DiscountPercent / 100);
		_selectedCart.AfterDiscount = _selectedCart.BaseTotal - _selectedCart.DiscountAmount;

		if (_selectedCart.InclusiveTax)
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / (100 + _selectedCart.CGSTPercent));
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / (100 + _selectedCart.SGSTPercent));
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / (100 + _selectedCart.IGSTPercent));
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount;
		}
		else
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / 100);
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / 100);
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / 100);
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount + _selectedCart.TotalTaxAmount;
		}

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.DiscountPercent < 0 || _selectedCart.CGSTPercent < 0 || _selectedCart.SGSTPercent < 0 || _selectedCart.IGSTPercent < 0 || _selectedCart.Total < 0 || string.IsNullOrEmpty(_selectedCart.UnitOfMeasurement))
		{
			await ShowToast("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", "error");
			return;
		}

		// Validate that all three taxes cannot be applied together
		int taxCount = 0;
		if (_selectedCart.CGSTPercent > 0) taxCount++;
		if (_selectedCart.SGSTPercent > 0) taxCount++;
		if (_selectedCart.IGSTPercent > 0) taxCount++;

		if (taxCount == 3)
		{
			await ShowToast("Invalid Tax Configuration", "All three taxes (CGST, SGST, IGST) cannot be applied together. Use either CGST+SGST or IGST only.", "error");
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.DiscountPercent = _selectedCart.DiscountPercent;
			existingItem.CGSTPercent = _selectedCart.CGSTPercent;
			existingItem.SGSTPercent = _selectedCart.SGSTPercent;
			existingItem.IGSTPercent = _selectedCart.IGSTPercent;
		}
		else
			_cart.Add(new()
			{
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
				UnitOfMeasurement = _selectedCart.UnitOfMeasurement,
				Rate = _selectedCart.Rate,
				DiscountPercent = _selectedCart.DiscountPercent,
				CGSTPercent = _selectedCart.CGSTPercent,
				SGSTPercent = _selectedCart.SGSTPercent,
				IGSTPercent = _selectedCart.IGSTPercent,
				InclusiveTax = _selectedCart.InclusiveTax,
				Remarks = _selectedCart.Remarks
			});

		_selectedRawMaterial = null;
		_selectedCart = new();

		await _sfItemAutoComplete.FocusAsync();
		await SavePurchaseReturnFile();
	}

	private async Task EditCartItem(PurchaseReturnItemCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == cartItem.ItemId);

		if (_selectedRawMaterial is null)
			return;

		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			UnitOfMeasurement = cartItem.UnitOfMeasurement,
			Rate = cartItem.Rate,
			DiscountPercent = cartItem.DiscountPercent,
			CGSTPercent = cartItem.CGSTPercent,
			SGSTPercent = cartItem.SGSTPercent,
			IGSTPercent = cartItem.IGSTPercent,
			InclusiveTax = cartItem.InclusiveTax,
			Remarks = cartItem.Remarks
		};

		await _sfItemAutoComplete.FocusAsync();
		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveItemFromCart(PurchaseReturnItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SavePurchaseReturnFile();
	}

	private async Task ClearCart()
	{
		if (_cart.Count == 0)
		{
			await ShowToast("Cart Empty", "The cart is already empty.", "error");
			return;
		}

		_cart.Clear();
		_selectedRawMaterial = null;
		_selectedCart = new();
		await SavePurchaseReturnFile();
		await ShowToast("Cart Cleared", "All items have been removed from the cart.", "success");
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails(bool customRoundOff = false)
	{
		foreach (var item in _cart)
		{
			if (item.Quantity == 0)
				_cart.Remove(item);

			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscountAmount = item.BaseTotal * (item.DiscountPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

			if (item.InclusiveTax)
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / (100 + item.CGSTPercent));
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / (100 + item.SGSTPercent));
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / (100 + item.IGSTPercent));
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount;
			}
			else
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount + item.TotalTaxAmount;
			}

			var perUnitCost = item.Total / item.Quantity;
			var withOtherCharges = perUnitCost * (1 + _purchaseReturn.OtherChargesPercent / 100);
			item.NetRate = withOtherCharges * (1 - _purchaseReturn.CashDiscountPercent / 100);
		}

		_purchaseReturn.ItemsTotalAmount = _cart.Sum(x => x.Total);

		_itemBaseTotal = _cart.Sum(x => x.BaseTotal);
		_itemDiscountTotal = _cart.Sum(x => x.DiscountAmount);
		_itemAfterDiscountTotal = _cart.Sum(x => x.AfterDiscount);
		_itemTaxTotal = _cart.Sum(x => x.TotalTaxAmount);
		_itemAfterTaxTotal = _cart.Sum(x => x.Total);

		_purchaseReturn.OtherChargesAmount = _itemAfterTaxTotal * _purchaseReturn.OtherChargesPercent / 100;
		var totalAfterOtherCharges = _itemAfterTaxTotal + _purchaseReturn.OtherChargesAmount;

		_purchaseReturn.CashDiscountAmount = totalAfterOtherCharges * _purchaseReturn.CashDiscountPercent / 100;
		var totalAfterCashDiscount = totalAfterOtherCharges - _purchaseReturn.CashDiscountAmount;

		if (!customRoundOff)
			_purchaseReturn.RoundOffAmount = Math.Round(totalAfterCashDiscount) - totalAfterCashDiscount;

		_purchaseReturn.TotalAmount = totalAfterCashDiscount + _purchaseReturn.RoundOffAmount;

		_purchaseReturn.CompanyId = _selectedCompany.Id;
		_purchaseReturn.PartyId = _selectedParty.Id;
		_purchaseReturn.CreatedBy = _user.Id;

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_purchaseReturn.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_purchaseReturn.FinancialYearId = _selectedFinancialYear.Id;
		else
		{
			await ShowToast("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", "error");
			_purchaseReturn.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_purchaseReturn.TransactionDateTime);
			_purchaseReturn.FinancialYearId = _selectedFinancialYear.Id;
		}
		#endregion

		if (_autoGenerateTransactionNo)
			_purchaseReturn.TransactionNo = await GenerateCodes.GeneratePurchaseReturnTransactionNo(_purchaseReturn);
	}

	private async Task SavePurchaseReturnFile(bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails(customRoundOff);

			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseReturnDataFileName, System.Text.Json.JsonSerializer.Serialize(_purchaseReturn));
			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseReturnCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Purchase Data", ex.Message, "error");
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
		if (_cart.Count == 0)
		{
			await ShowToast("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", "error");
			return false;
		}

		if (_selectedCompany is null || _purchaseReturn.CompanyId <= 0)
		{
			await ShowToast("Company Not Selected", "Please select a company for the purchase return transaction.", "error");
			return false;
		}

		if (_selectedParty is null || _purchaseReturn.PartyId <= 0)
		{
			await ShowToast("Party Not Selected", "Please select a party ledger for the purchase return transaction.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_purchaseReturn.TransactionNo))
		{
			await ShowToast("Transaction Number Missing", "Please enter a transaction number for the purchase return.", "error");
			return false;
		}

		if (_purchaseReturn.TransactionDateTime == default)
		{
			await ShowToast("Transaction Date Missing", "Please select a valid transaction date for the purchase return.", "error");
			return false;
		}

		if (_selectedFinancialYear is null || _purchaseReturn.FinancialYearId <= 0)
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

		if (_purchaseReturn.TotalAmount <= 0)
		{
			await ShowToast("Invalid Total Amount", "The total amount of the purchase return transaction must be greater than zero.", "error");
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
		{
			await ShowToast("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", "error");
			return false;
		}

		if (_purchaseReturn.Id > 0)
		{
			var existingPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, _purchaseReturn.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingPurchaseReturn.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			{
				await ShowToast("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", "error");
				return false;
			}

			if (!_user.Admin)
			{
				await ShowToast("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", "error");
				return false;
			}
		}

		if (string.IsNullOrWhiteSpace(_purchaseReturn.DocumentUrl))
			_purchaseReturn.DocumentUrl = null;

		if (string.IsNullOrWhiteSpace(_purchaseReturn.Remarks))
			_purchaseReturn.Remarks = null;

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await SavePurchaseReturnFile(true);

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			_purchaseReturn.Status = true;
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_purchaseReturn.TransactionDateTime = DateOnly.FromDateTime(_purchaseReturn.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_purchaseReturn.LastModifiedAt = currentDateTime;
			_purchaseReturn.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_purchaseReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_purchaseReturn.CreatedBy = _user.Id;
			_purchaseReturn.LastModifiedBy = _user.Id;

			_purchaseReturn.Id = await PurchaseReturnData.SavePurchaseReturnTransaction(_purchaseReturn, _cart);
			await GenerateAndDownloadInvoice();
			await DeleteLocalFiles();
			NavigationManager.NavigateTo("/inventory/purchasereturn", true);

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

	private async Task GenerateAndDownloadInvoice()
	{
		try
		{
			// Load saved purchase return details (since _purchaseReturn now has the Id)
			var savedPurchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, _purchaseReturn.Id);
			if (savedPurchaseReturn is null)
			{
				await ShowToast("Warning", "Invoice generation skipped - purchase return data not found.", "error");
				return;
			}

			// Load purchase return details from database
			var purchaseReturnDetails = await PurchaseReturnData.LoadPurchaseReturnDetailByPurchaseReturn(_purchaseReturn.Id);
			if (purchaseReturnDetails is null || purchaseReturnDetails.Count == 0)
			{
				await ShowToast("Warning", "Invoice generation skipped - no line items found.", "error");
				return;
			}

			// Load company and party
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, savedPurchaseReturn.CompanyId);
			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, savedPurchaseReturn.PartyId);

			if (company is null || party is null)
			{
				await ShowToast("Warning", "Invoice generation skipped - company or party not found.", "error");
				return;
			}

			// Generate invoice PDF
			var pdfStream = await Task.Run(() =>
				PurchaseReturnInvoicePDFExport.ExportPurchaseReturnInvoice(
					savedPurchaseReturn,
					purchaseReturnDetails,
					company,
					party,
					null, // logo path - uses default
					"PURCHASE RETURN INVOICE"
				)
			);

			// Generate file name
			string fileName = $"PURCHASE_RETURN_INVOICE_{savedPurchaseReturn.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

			// Save and view the PDF
			await SaveAndViewService.SaveAndView(fileName, "application/pdf", pdfStream);
		}
		catch (Exception ex)
		{
			await ShowToast("Invoice Generation Failed", $"Transaction saved but invoice generation failed: {ex.Message}", "error");
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseReturnDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseReturnCartDataFileName);
	}
	#endregion

	#region Uploading Document
	private void UploadDocument()
	{
		_isUploadDialogVisible = true;
		StateHasChanged();
	}

	private async Task OnRemoveFile(RemovingEventArgs args) =>
		await RemoveExistingDocument();

	private async Task RemoveExistingDocument()
	{
		try
		{
			if (string.IsNullOrEmpty(_purchaseReturn.DocumentUrl))
				return;

			var fileName = _purchaseReturn.DocumentUrl.Split('/').Last();
			await BlobStorageAccess.DeleteFileFromBlobStorage(fileName, BlobStorageContainers.purchasereturn);
			_purchaseReturn.DocumentUrl = null;

			await SavePurchaseReturnFile();
			await ShowToast("Document Removed", "The uploaded document has been removed successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Removing Document", ex.Message, "error");
		}
	}

	private async Task DownloadExistingDocument()
	{
		try
		{
			if (string.IsNullOrEmpty(_purchaseReturn.DocumentUrl))
				return;

			var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(_purchaseReturn.DocumentUrl, BlobStorageContainers.purchasereturn);
			var fileName = _purchaseReturn.DocumentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, contentType, fileStream);
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Downloading Document", ex.Message, "error");
		}
	}

	private void CloseUploadDialog()
	{
		_isUploadDialogVisible = false;
		StateHasChanged();
	}

	private async Task OnUploaderFileChange(UploadChangeEventArgs args)
	{
		try
		{
			var uploadedFiles = args.Files;

			if (uploadedFiles is not null && uploadedFiles.Count == 1)
			{
				if (!string.IsNullOrEmpty(_purchaseReturn.DocumentUrl))
					await RemoveExistingDocument();

				await using var file = uploadedFiles[0].File.OpenReadStream(maxAllowedSize: 52428800); // 50 MB
				var fileName = $"{Guid.NewGuid()}_{uploadedFiles[0].File.Name}";
				var fileUrl = await BlobStorageAccess.UploadFileToBlobStorage(file, fileName, BlobStorageContainers.purchasereturn);
				_purchaseReturn.DocumentUrl = fileUrl; await SavePurchaseReturnFile();
				await ShowToast("Document Uploaded Successfully", "The document has been uploaded and linked to the purchase return transaction.", "success");
			}
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Uploading Document", ex.Message, "error");
		}
	}

	private async Task InterpretFiles()
	{
		try
		{
			await ShowToast("Feature Not Implemented", "The interpret files feature is not yet implemented.", "error");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Interpreting Files", ex.Message, "error");
		}
	}
	#endregion

	#region Utilities
	private async Task ResetPage(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo("/inventory/purchasereturn", true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/report/purchasereturn", "_blank");
		else
			NavigationManager.NavigateTo("/report/purchasereturn");
	}

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
	#endregion
}