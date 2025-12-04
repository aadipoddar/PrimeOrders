using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.Data.Sales.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages.Sales.StockTransfer;

public partial class StockTransferPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private LocationModel _selectedLocation = new();
	private LocationModel _selectedToLocation = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductLocationOverviewModel? _selectedProduct = new();
	private StockTransferItemCartModel _selectedCart = new();
	private StockTransferModel _stockTransfer = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _locations = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<TaxModel> _taxes = [];
	private List<StockTransferItemCartModel> _cart = [];

	private SfAutoComplete<ProductLocationOverviewModel?, ProductLocationOverviewModel> _sfItemAutoComplete;
	private SfGrid<StockTransferItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.Enter, AddItemToCart, "Add item to cart", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, () => _sfItemAutoComplete.FocusAsync(), "Focus on item input", Exclude.None)
			.Add(ModCode.Ctrl, Code.S, SaveTransaction, "Save the transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, DownloadInvoice, "Download invoice", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistoryPage, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);

		await LoadLocations();
		await LoadCompanies();
		await LoadExistingTransaction();
		await LoadItems();
		await LoadExistingCart();
		await SaveTransactionFile();
	}

	private async Task LoadLocations()
	{
		try
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations = [.. _locations.OrderBy(s => s.Name)];
			_locations.Add(new()
			{
				Id = 0,
				Name = "Create New Location ..."
			});

			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
			_selectedToLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Locations", ex.Message, ToastType.Error);
		}
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
			await _toastNotification.ShowAsync("An Error Occurred While Loading Companies", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadExistingTransaction()
	{
		try
		{
			if (Id.HasValue)
			{
				_stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, Id.Value);
				if (_stockTransfer is null || _stockTransfer.Id == 0)
				{
					await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
					NavigationManager.NavigateTo(PageRouteNames.StockTransfer, true);
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.StockTransferDataFileName))
				_stockTransfer = System.Text.Json.JsonSerializer.Deserialize<StockTransferModel>(await DataStorageService.LocalGetAsync(StorageFileNames.StockTransferDataFileName));

			else
			{
				_stockTransfer = new()
				{
					Id = 0,
					TransactionNo = string.Empty,
					CompanyId = _selectedCompany.Id,
					LocationId = _user.LocationId,
					ToLocationId = _user.LocationId,
					BaseTotal = 0,
					TotalQuantity = 0,
					TransactionDateTime = await CommonData.LoadCurrentDateTime(),
					FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
					CreatedBy = _user.Id,
					TotalItems = 0,
					ItemDiscountAmount = 0,
					TotalAfterItemDiscount = 0,
					TotalInclusiveTaxAmount = 0,
					TotalExtraTaxAmount = 0,
					TotalAfterTax = 0,
					DiscountPercent = 0,
					DiscountAmount = 0,
					OtherChargesPercent = 0,
					OtherChargesAmount = 0,
					RoundOffAmount = 0,
					TotalAmount = 0,
					Card = 0,
					Cash = 0,
					Credit = 0,
					UPI = 0,
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

			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _stockTransfer.LocationId);
			_selectedToLocation = _locations.FirstOrDefault(s => s.Id == _stockTransfer.ToLocationId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _stockTransfer.CompanyId);

			if (Id is null)
				_stockTransfer.DiscountPercent = _selectedToLocation.Discount;

			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _stockTransfer.FinancialYearId);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_products = await ProductData.LoadProductByLocation(_stockTransfer.LocationId);
			_taxes = await CommonData.LoadTableDataByStatus<TaxModel>(TableNames.Tax);

			_products = [.. _products.OrderBy(s => s.Name)];

			_products.Add(new()
			{
				Id = 0,
				Name = "Create New Item ..."
			});
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Items", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (_stockTransfer.Id > 0)
			{
				var existingCart = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, _stockTransfer.Id);

				foreach (var item in existingCart)
				{
					if (_products.FirstOrDefault(s => s.ProductId == item.ProductId) is null)
					{
						var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
						await _toastNotification.ShowAsync("Item Not Found", $"The item {product?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
						continue;
					}

					_cart.Add(new()
					{
						ItemId = item.ProductId,
						ItemName = _products.FirstOrDefault(s => s.ProductId == item.ProductId)?.Name ?? "",
						Quantity = item.Quantity,
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

			else if (await DataStorageService.LocalExists(StorageFileNames.StockTransferCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<StockTransferItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.StockTransferCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}
	#endregion

	#region Change Events
	private async Task OnFromLocationChanged(ChangeEventArgs<LocationModel, LocationModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLocation, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminLocation);

			return;
		}

		_selectedLocation = args.Value;
		_stockTransfer.LocationId = _selectedLocation.Id;

		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnToLocationChanged(ChangeEventArgs<LocationModel, LocationModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLocation, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminLocation);

			return;
		}

		_selectedToLocation = args.Value;
		_stockTransfer.ToLocationId = _selectedToLocation.Id;

		await SaveTransactionFile();
	}

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
		_stockTransfer.CompanyId = _selectedCompany.Id;

		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_stockTransfer.TransactionDateTime = args.Value;
		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_stockTransfer.DiscountPercent = args.Value;
		await SaveTransactionFile();
	}

	private async Task OnOtherDiscountPercentChanged(ChangeEventArgs<decimal> args)
	{
		_stockTransfer.OtherChargesPercent = args.Value;
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(ChangeEventArgs<decimal> args)
	{
		_stockTransfer.RoundOffAmount = args.Value;
		await SaveTransactionFile(true);
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<ProductLocationOverviewModel?, ProductLocationOverviewModel?> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminProduct, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminProduct);

			return;
		}

		_selectedProduct = args.Value;

		if (_selectedProduct is null)
			_selectedCart = new()
			{
				ItemId = 0,
				ItemName = "",
				Quantity = 1,
				Rate = 0,
				DiscountPercent = 0,
				CGSTPercent = 0,
				SGSTPercent = 0,
				IGSTPercent = 0
			};

		else
		{
			var fromLedger = await LedgerData.LoadLedgerByLocation(_stockTransfer.LocationId);
			var toLedger = await LedgerData.LoadLedgerByLocation(_stockTransfer.ToLocationId);

			var isSameState = fromLedger.StateUTId == toLedger.StateUTId;

			_selectedCart.ItemId = _selectedProduct.ProductId;
			_selectedCart.ItemName = _selectedProduct.Name;
			_selectedCart.Quantity = 1;
			_selectedCart.Rate = _selectedProduct.Rate;
			_selectedCart.DiscountPercent = 0;
			_selectedCart.CGSTPercent = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).CGST;
			_selectedCart.SGSTPercent = isSameState ? _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).SGST : 0;
			_selectedCart.IGSTPercent = isSameState ? 0 : _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).IGST;
			_selectedCart.InclusiveTax = _taxes.FirstOrDefault(s => s.Id == _selectedProduct.TaxId).Inclusive;
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
		if (_selectedProduct is null)
			return;

		if (_selectedCart.Quantity <= 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;
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
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.DiscountPercent < 0 || _selectedCart.CGSTPercent < 0 || _selectedCart.SGSTPercent < 0 || _selectedCart.IGSTPercent < 0 || _selectedCart.Total < 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		// Validate that all three taxes cannot be applied together
		int taxCount = 0;
		if (_selectedCart.CGSTPercent > 0) taxCount++;
		if (_selectedCart.SGSTPercent > 0) taxCount++;
		if (_selectedCart.IGSTPercent > 0) taxCount++;

		if (taxCount == 3)
		{
			await _toastNotification.ShowAsync("Invalid Tax Configuration", "All three taxes (CGST, SGST, IGST) cannot be applied together. Use either CGST+SGST or IGST only.", ToastType.Error);
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
				Rate = _selectedCart.Rate,
				DiscountPercent = _selectedCart.DiscountPercent,
				CGSTPercent = _selectedCart.CGSTPercent,
				SGSTPercent = _selectedCart.SGSTPercent,
				IGSTPercent = _selectedCart.IGSTPercent,
				InclusiveTax = _selectedCart.InclusiveTax,
				Remarks = _selectedCart.Remarks
			});

		_selectedProduct = null;
		_selectedCart = new();

		await _sfItemAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(StockTransferItemCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ItemId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
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

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(StockTransferItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
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
			var withOtherCharges = perUnitCost * (1 + _stockTransfer.OtherChargesPercent / 100);
			item.NetRate = withOtherCharges * (1 - _stockTransfer.DiscountPercent / 100);

			item.Remarks = item.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(item.Remarks))
				item.Remarks = null;
		}

		_stockTransfer.TotalItems = _cart.Count;
		_stockTransfer.TotalQuantity = _cart.Sum(x => x.Quantity);
		_stockTransfer.BaseTotal = _cart.Sum(x => x.BaseTotal);
		_stockTransfer.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
		_stockTransfer.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
		_stockTransfer.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_stockTransfer.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_stockTransfer.TotalAfterTax = _cart.Sum(x => x.Total);

		_stockTransfer.OtherChargesAmount = _stockTransfer.TotalAfterTax * _stockTransfer.OtherChargesPercent / 100;
		var totalAfterOtherCharges = _stockTransfer.TotalAfterTax + _stockTransfer.OtherChargesAmount;

		_stockTransfer.DiscountAmount = totalAfterOtherCharges * _stockTransfer.DiscountPercent / 100;
		var totalAfterDiscount = totalAfterOtherCharges - _stockTransfer.DiscountAmount;

		if (!customRoundOff)
			_stockTransfer.RoundOffAmount = Math.Round(totalAfterDiscount) - totalAfterDiscount;

		_stockTransfer.TotalAmount = totalAfterDiscount + _stockTransfer.RoundOffAmount;

		_stockTransfer.CompanyId = _selectedCompany.Id;
		_stockTransfer.LocationId = _selectedLocation.Id;
		_stockTransfer.ToLocationId = _selectedToLocation.Id;
		_stockTransfer.CreatedBy = _user.Id;

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_stockTransfer.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_stockTransfer.FinancialYearId = _selectedFinancialYear.Id;
		else
		{
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
			_stockTransfer.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_stockTransfer.TransactionDateTime);
			_stockTransfer.FinancialYearId = _selectedFinancialYear.Id;
		}
		#endregion

		if (Id is null)
			_stockTransfer.TransactionNo = await GenerateCodes.GenerateStockTransferTransactionNo(_stockTransfer);
	}

	private async Task SaveTransactionFile(bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails(customRoundOff);

			await DataStorageService.LocalSaveAsync(StorageFileNames.StockTransferDataFileName, System.Text.Json.JsonSerializer.Serialize(_stockTransfer));
			await DataStorageService.LocalSaveAsync(StorageFileNames.StockTransferCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
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
			await _toastNotification.ShowAsync("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", ToastType.Error);
			return false;
		}

		if (_selectedCompany is null || _stockTransfer.CompanyId <= 0)
		{
			await _toastNotification.ShowAsync("Company Not Selected", "Please select a company for the transaction.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_stockTransfer.TransactionNo))
		{
			await _toastNotification.ShowAsync("Transaction Number Missing", "Please enter a transaction number for the transaction.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.TransactionDateTime == default)
		{
			await _toastNotification.ShowAsync("Transaction Date Missing", "Please select a valid transaction date for the transaction.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear is null || _stockTransfer.FinancialYearId <= 0)
		{
			await _toastNotification.ShowAsync("Financial Year Not Found", "The transaction date does not fall within any financial year. Please check the date and try again.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await _toastNotification.ShowAsync("Financial Year Locked", "The financial year for the selected transaction date is locked. Please select a different date.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Status == false)
		{
			await _toastNotification.ShowAsync("Financial Year Inactive", "The financial year for the selected transaction date is inactive. Please select a different date.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.TotalItems <= 0)
		{
			await _toastNotification.ShowAsync("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.TotalQuantity <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Quantity", "The total quantity of items in the cart must be greater than zero.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.TotalAmount < 0)
		{
			await _toastNotification.ShowAsync("Invalid Total Amount", "The total amount of the transaction must be greater than zero.", ToastType.Error);
			return false;
		}

		if (_cart.Any(item => item.Quantity <= 0))
		{
			await _toastNotification.ShowAsync("Invalid Item Quantity", "One or more items in the cart have a quantity less than or equal to zero. Please correct the quantities before saving.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.LocationId == _stockTransfer.ToLocationId)
		{
			await _toastNotification.ShowAsync("Invalid Locations", "The 'From' and 'To' locations cannot be the same. Please select different locations.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.Id > 0)
		{
			var existingTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, _stockTransfer.Id);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingTransfer.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
			{
				await _toastNotification.ShowAsync("Financial Year Locked or Inactive", "The financial year for the selected transaction date is either locked or inactive. Please select a different date.", ToastType.Error);
				return false;
			}

			if (!_user.Admin)
			{
				await _toastNotification.ShowAsync("Insufficient Permissions", "You do not have the necessary permissions to modify this transaction.", ToastType.Error);
				await DeleteLocalFiles();
				NavigationManager.NavigateTo(PageRouteNames.StockTransfer, true);
				return false;
			}
		}

		_stockTransfer.Remarks = _stockTransfer.Remarks?.Trim();
		if (string.IsNullOrWhiteSpace(_stockTransfer.Remarks))
			_stockTransfer.Remarks = null;

		if (_stockTransfer.Cash < 0 || _stockTransfer.Card < 0 || _stockTransfer.Credit < 0 || _stockTransfer.UPI < 0)
		{
			await _toastNotification.ShowAsync("Invalid Payment Amounts", "Payment amounts (Cash, Card, Credit, UPI) cannot be negative. Please correct the amounts before saving.", ToastType.Error);
			return false;
		}

		if (_stockTransfer.Cash + _stockTransfer.Card + _stockTransfer.Credit + _stockTransfer.UPI != _stockTransfer.TotalAmount)
		{
			await _toastNotification.ShowAsync("Payment Amount Mismatch", "The sum of payment amounts (Cash, Card, Credit, UPI) must equal the total amount of the transaction. Please correct the amounts before saving.", ToastType.Error);
			return false;
		}

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await SaveTransactionFile(true);

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			_stockTransfer.Status = true;
			var currentDateTime = await CommonData.LoadCurrentDateTime();
			_stockTransfer.TransactionDateTime = DateOnly.FromDateTime(_stockTransfer.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
			_stockTransfer.LastModifiedAt = currentDateTime;
			_stockTransfer.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_stockTransfer.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
			_stockTransfer.CreatedBy = _user.Id;
			_stockTransfer.LastModifiedBy = _user.Id;

			_stockTransfer.Id = await StockTransferData.SaveStockTransferTransaction(_stockTransfer, _cart);
			var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(_stockTransfer.Id);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await DeleteLocalFiles();
			NavigationManager.NavigateTo(PageRouteNames.StockTransfer, true);

			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully! Invoice has been generated.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.StockTransferDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.StockTransferCartDataFileName);
	}
	#endregion

	#region Utilities
	private async Task DownloadInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("No Transaction Selected", "Please save the transaction first before downloading the invoice.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating invoice...", ToastType.Info);
			var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(Id.Value);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await _toastNotification.ShowAsync("Invoice Downloaded", "The invoice has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(PageRouteNames.StockTransfer, true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportStockTransfer, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportStockTransfer);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportStockTransferItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportStockTransferItem);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}