using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Sale;

public partial class SaleReturnPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	[Parameter] public int? SaleReturnId { get; set; }

	private UserModel _user;
	private LocationModel _userLocation;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _isSaving = false;
	private bool _returnDetailsDialogVisible = false;
	private bool _returnSummaryDialogVisible = false;
	private bool _billSelectionDialogVisible = false;

	private decimal _selectedQuantity = 1;
	private int _selectedSaleId = 0;

	private string _productSearchText = "";
	private int _selectedProductIndex = -1;
	private List<SaleReturnProductCartModel> _filteredProducts = [];
	private bool _isProductSearchActive = false;
	private bool _hasAddedProductViaSearch = true;

	private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
	private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);

	private SaleReturnProductCartModel _selectedProductCart = new();
	private SaleReturnProductCartModel _selectedProduct = new();
	private SaleModel _selectedSale = new();
	private SaleReturnModel _saleReturn = new()
	{
		ReturnDateTime = DateTime.Now,
		Remarks = "",
		Status = true
	};

	private List<SaleReturnProductCartModel> _products;
	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _availableSales = [];
	private readonly List<SaleReturnProductCartModel> _availableSaleProducts = [];
	private readonly List<SaleReturnProductCartModel> _saleReturnProductCart = [];

	private SfGrid<SaleReturnProductCartModel> _sfProductGrid;
	private SfGrid<SaleReturnProductCartModel> _sfProductCartGrid;

	private SfDialog _sfReturnDetailsDialog;
	private SfDialog _sfReturnSummaryDialog;
	private SfDialog _sfBillSelectionDialog;
	private SfDialog _sfProductManageDialog;
	private SfDialog _sfQuantityDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region LoadData
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Sales)).User) is not null))
			return;

		_userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

		await LoadData();
		await JS.InvokeVoidAsync("setupSaleReturnPageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		await LoadLocations();

		if (_user.LocationId == 1)
			_saleReturn.LocationId = _locations.FirstOrDefault()?.Id ?? _user.LocationId;
		else
			_saleReturn.LocationId = _user.LocationId;

		if (SaleReturnId.HasValue && SaleReturnId.Value > 0)
			await LoadSaleReturn();

		await LoadAvailableSales();

		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			_locations.RemoveAll(l => l.Id == 1);
		}
	}

	private async Task LoadAvailableSales()
	{
		_availableSales = await SaleData.LoadSaleDetailsByDateLocationId(
			_startDate.ToDateTime(new TimeOnly(0, 0)),
			_endDate.ToDateTime(new TimeOnly(23, 59)),
			1);

		var filteredSales = new List<SaleOverviewModel>();

		foreach (var sale in _availableSales)
			if (sale.PartyId.HasValue && sale.PartyId > 0)
			{
				var supplier = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
				if (supplier?.LocationId == _saleReturn.LocationId)
					filteredSales.Add(sale);
			}

		_availableSales = filteredSales;

		StateHasChanged();
	}

	private async Task LoadSaleReturn()
	{
		_saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId.Value);

		if (_saleReturn is null)
			NavManager.NavigateTo("/SaleReturn");

		if (_saleReturn.SaleId > 0)
			await LoadSaleForReturn(_saleReturn.SaleId);

		_saleReturnProductCart.Clear();

		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(_saleReturn.Id);
		foreach (var item in saleReturnDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
			var cartItem = _availableSaleProducts.FirstOrDefault(p => p.ProductId == item.ProductId);

			_saleReturnProductCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = item.Quantity,
				MaxQuantity = cartItem?.SoldQuantity ?? 0,
				SoldQuantity = cartItem?.SoldQuantity ?? 0,
				AlreadyReturnedQuantity = cartItem?.AlreadyReturnedQuantity ?? 0,
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
				NetRate = item.NetRate,
				Total = item.Total,
			});
		}

		StateHasChanged();
	}

	private async Task LoadSaleForReturn(int saleId)
	{
		_selectedSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId);
		_selectedSaleId = saleId;
		_saleReturn.SaleId = saleId;

		if (_selectedSale is not null)
		{
			_saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(_saleReturn);

			var saleDetails = await SaleData.LoadSaleDetailBySale(saleId);
			var existingReturns = await SaleReturnData.LoadSaleReturnBySale(saleId);

			_availableSaleProducts.Clear();

			foreach (var detail in saleDetails)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);

				decimal alreadyReturnedQty = 0;
				foreach (var returnRecord in existingReturns.Where(r => r.Status))
				{
					var returnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(returnRecord.Id);
					alreadyReturnedQty += returnDetails.Where(rd => rd.ProductId == detail.ProductId && rd.Status).Sum(rd => rd.Quantity);
				}

				decimal maxReturnableQty = detail.Quantity - alreadyReturnedQty;

				if (maxReturnableQty > 0)
					_availableSaleProducts.Add(new()
					{
						ProductId = product.Id,
						ProductName = product.Name,
						Quantity = 0,
						MaxQuantity = maxReturnableQty,
						SoldQuantity = detail.Quantity,
						AlreadyReturnedQuantity = alreadyReturnedQty,
						Rate = detail.Rate,
						BaseTotal = detail.BaseTotal,
						DiscPercent = detail.DiscPercent,
						DiscAmount = detail.DiscAmount,
						AfterDiscount = detail.AfterDiscount,
						CGSTPercent = detail.CGSTPercent,
						CGSTAmount = detail.CGSTAmount,
						SGSTPercent = detail.SGSTPercent,
						SGSTAmount = detail.SGSTAmount,
						IGSTPercent = detail.IGSTPercent,
						IGSTAmount = detail.IGSTAmount,
						NetRate = detail.NetRate,
						Total = detail.Total
					});
			}

			_products = [];
			foreach (var availableProduct in _availableSaleProducts)
				_products.Add(availableProduct);

			_filteredProducts = [.. _products];
		}

		StateHasChanged();
	}
	#endregion

	#region Fields Selection
	private async Task DateRangeChanged(RangePickerEventArgs<DateOnly> args)
	{
		_startDate = args.StartDate;
		_endDate = args.EndDate;
		await LoadAvailableSales();
	}

	private async Task OnSaleSelected(ChangeEventArgs<int, SaleOverviewModel> args)
	{
		if (args.Value > 0)
		{
			await LoadSaleForReturn(args.Value);
			_billSelectionDialogVisible = false;
		}
	}

	private void OnBillSelectionClick()
	{
		_billSelectionDialogVisible = true;
		StateHasChanged();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_saleReturn.LocationId = args.Value;

		if (_selectedSaleId > 0)
			_saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(_saleReturn);

		await LoadAvailableSales();
		StateHasChanged();
	}
	#endregion

	#region Keyboard Navigation Methods
	[JSInvokable]
	public async Task HandleKeyboardShortcut(string key)
	{
		if (_isProductSearchActive)
		{
			await HandleProductSearchKeyboard(key);
			return;
		}

		switch (key.ToLower())
		{
			case "f2":
				await StartProductSearch();
				break;

			case "escape":
				await HandleEscape();
				break;
		}
	}

	private async Task HandleProductSearchKeyboard(string key)
	{
		switch (key.ToLower())
		{
			case "escape":
				await ExitProductSearch();
				break;

			case "enter":
				await SelectCurrentProduct();
				break;

			case "arrowdown":
				NavigateProductSelection(1);
				break;

			case "arrowup":
				NavigateProductSelection(-1);
				break;

			case "backspace":
				if (_productSearchText.Length > 0)
				{
					_productSearchText = _productSearchText[..^1];
					await FilterProducts();
				}
				break;

			default:
				// Add character to search if it's alphanumeric or space
				if (key.Length == 1 && (char.IsLetterOrDigit(key[0]) || key == " "))
				{
					_productSearchText += key.ToUpper();
					await FilterProducts();
				}
				break;
		}

		StateHasChanged();
	}

	private async Task StartProductSearch()
	{
		if (_selectedSaleId == 0)
		{
			_sfErrorToast.Content = "Please select a sale first to search products.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		_hasAddedProductViaSearch = true;
		_isProductSearchActive = true;
		_productSearchText = "";
		_selectedProductIndex = 0;
		_filteredProducts = [.. _products];

		if (_filteredProducts.Count > 0)
			_selectedProduct = _filteredProducts[0];

		StateHasChanged();
		await JS.InvokeVoidAsync("showProductSearchIndicator", _productSearchText);
	}

	private async Task ExitProductSearch()
	{
		_isProductSearchActive = false;
		_productSearchText = "";
		_selectedProductIndex = -1;
		StateHasChanged();
		await JS.InvokeVoidAsync("hideProductSearchIndicator");
	}

	private async Task FilterProducts()
	{
		if (string.IsNullOrEmpty(_productSearchText))
			_filteredProducts = [.. _products];
		else
			_filteredProducts = [.. _products.Where(p =>
				p.ProductName.Contains(_productSearchText, StringComparison.OrdinalIgnoreCase)
			)];

		_selectedProductIndex = 0;
		if (_filteredProducts.Count > 0)
			_selectedProduct = _filteredProducts[0];

		await JS.InvokeVoidAsync("updateProductSearchIndicator", _productSearchText, _filteredProducts.Count);
		StateHasChanged();
	}

	private void NavigateProductSelection(int direction)
	{
		if (_filteredProducts.Count == 0) return;

		_selectedProductIndex += direction;

		if (_selectedProductIndex < 0)
			_selectedProductIndex = _filteredProducts.Count - 1;
		else if (_selectedProductIndex >= _filteredProducts.Count)
			_selectedProductIndex = 0;

		_selectedProduct = _filteredProducts[_selectedProductIndex];
		StateHasChanged();
	}

	private async Task SelectCurrentProduct()
	{
		if (_selectedProduct.ProductId > 0)
		{
			await OnAddToCartButtonClick(_selectedProduct);
			await ExitProductSearch();
		}
	}

	private async Task HandleEscape()
	{
		if (_isProductSearchActive)
			await ExitProductSearch();

		StateHasChanged();
	}
	#endregion

	#region Products
	private async Task OnAddToCartButtonClick(SaleReturnProductCartModel product)
	{
		if (product is null || product.ProductId <= 0)
			return;

		if (_selectedSaleId == 0)
		{
			_sfErrorToast.Content = "Please select a sale first.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		var availableProduct = _availableSaleProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
		if (availableProduct is null)
		{
			_sfErrorToast.Content = "This product is not available for return from the selected sale.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if (availableProduct.MaxQuantity <= 0)
		{
			_sfErrorToast.Content = "No quantity available for return for this product.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		_selectedProduct = product;
		_selectedQuantity = 1;
		_quantityDialogVisible = true;
		_hasAddedProductViaSearch = false;
		StateHasChanged();
	}

	public void ProductCartRowSelectHandler(RowSelectEventArgs<SaleReturnProductCartModel> args)
	{
		_selectedProductCart = args.Data;
		_dialogVisible = true;
		StateHasChanged();
	}

	private void UpdateFinancialDetails()
	{
		foreach (var item in _saleReturnProductCart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = item.Total / item.Quantity;
		}

		_sfProductGrid?.Refresh();
		_sfProductCartGrid?.Refresh();
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

		var availableProduct = _availableSaleProducts.FirstOrDefault(p => p.ProductId == _selectedProduct.ProductId);
		if (availableProduct is null)
		{
			_sfErrorToast.Content = "Product not found in the selected sale.";
			await _sfErrorToast.ShowAsync();
			OnCancelQuantityDialogClick();
			return;
		}

		var existingCartItem = _saleReturnProductCart.FirstOrDefault(c => c.ProductId == _selectedProduct.ProductId);
		decimal currentCartQuantity = existingCartItem?.Quantity ?? 0;

		if (currentCartQuantity + _selectedQuantity > availableProduct.MaxQuantity)
		{
			_sfErrorToast.Content = $"Cannot return more than {availableProduct.MaxQuantity} units. Currently in cart: {currentCartQuantity}";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if (existingCartItem is not null)
			existingCartItem.Quantity += _selectedQuantity;
		else
			_saleReturnProductCart.Add(new()
			{
				ProductId = _selectedProduct.ProductId,
				ProductName = _selectedProduct.ProductName,
				Quantity = _selectedQuantity,
				MaxQuantity = availableProduct.MaxQuantity,
				SoldQuantity = availableProduct.SoldQuantity,
				AlreadyReturnedQuantity = availableProduct.AlreadyReturnedQuantity,
				Rate = _selectedProduct.Rate,
				DiscPercent = _selectedProduct.DiscPercent,
				CGSTPercent = _selectedProduct.CGSTPercent,
				SGSTPercent = _selectedProduct.SGSTPercent,
				IGSTPercent = _selectedProduct.IGSTPercent,
				// Handle Calculation in UpdateFinancialDetails
			});

		_quantityDialogVisible = false;
		_selectedProduct = new();
		await _sfProductCartGrid?.Refresh();
		await _sfProductGrid?.Refresh();

		if (_hasAddedProductViaSearch)
			await StartProductSearch();

		StateHasChanged();
	}

	private void OnCancelQuantityDialogClick()
	{
		_quantityDialogVisible = false;
		_selectedProduct = new();
		StateHasChanged();
	}

	private async Task OnSaveProductManageClick()
	{
		var availableProduct = _availableSaleProducts.FirstOrDefault(p => p.ProductId == _selectedProductCart.ProductId);
		if (availableProduct is not null && _selectedProductCart.Quantity > availableProduct.MaxQuantity)
		{
			_sfErrorToast.Content = $"Cannot return more than {availableProduct.MaxQuantity} units.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		_saleReturnProductCart.Remove(_saleReturnProductCart.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		if (_selectedProductCart.Quantity > 0)
			_saleReturnProductCart.Add(_selectedProductCart);

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		StateHasChanged();
	}

	private async Task OnRemoveFromCartProductManageClick()
	{
		_selectedProductCart.Quantity = 0;
		_saleReturnProductCart.Remove(_saleReturnProductCart.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_saleReturn.UserId = _user.Id;

		if (_user.LocationId != 1)
			_saleReturn.LocationId = _user.LocationId;

		if (_selectedSaleId == 0)
		{
			_sfErrorToast.Content = "Please select a sale to return products from.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		_saleReturn.SaleId = _selectedSaleId;

		if (SaleReturnId is null)
			_saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(_saleReturn);

		if (_saleReturnProductCart.Count == 0)
		{
			_sfErrorToast.Content = "Please add at least one product to the return.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_saleReturn.UserId == 0 || _saleReturn.LocationId == 0)
		{
			_sfErrorToast.Content = "User and Location is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task ConfirmReturnSubmission()
	{
		if (!await ValidateForm())
			return;

		_isSaving = true;
		StateHasChanged();

		_saleReturn.Id = await SaleReturnData.InsertSaleReturn(_saleReturn);
		if (_saleReturn.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save the return. Please try again.";
			await _sfErrorToast.ShowAsync();
			_isSaving = false;
			StateHasChanged();
			return;
		}

		await InsertSaleReturnDetails();
		await InsertStock();
		int accountingId = await InsertAccounting();
		await InsertAccountingDetails(accountingId);

		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertSaleReturnDetails()
	{
		if (SaleReturnId.HasValue && SaleReturnId > 0)
		{
			var existingSaleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(_saleReturn.Id);
			foreach (var existingDetail in existingSaleReturnDetails)
			{
				existingDetail.Status = false;
				await SaleReturnData.InsertSaleReturnDetail(existingDetail);
			}
		}

		foreach (var cartItem in _saleReturnProductCart)
			await SaleReturnData.InsertSaleReturnDetail(new()
			{
				Id = 0,
				SaleReturnId = _saleReturn.Id,
				ProductId = cartItem.ProductId,
				Quantity = cartItem.Quantity,
				Rate = cartItem.Rate,
				BaseTotal = cartItem.BaseTotal,
				DiscPercent = cartItem.DiscPercent,
				DiscAmount = cartItem.DiscAmount,
				AfterDiscount = cartItem.AfterDiscount,
				CGSTPercent = cartItem.CGSTPercent,
				CGSTAmount = cartItem.CGSTAmount,
				SGSTPercent = cartItem.SGSTPercent,
				SGSTAmount = cartItem.SGSTAmount,
				IGSTPercent = cartItem.IGSTPercent,
				IGSTAmount = cartItem.IGSTAmount,
				NetRate = cartItem.NetRate,
				Total = cartItem.Total,
				Status = true
			});
	}

	private async Task InsertStock()
	{
		if (SaleReturnId.HasValue && SaleReturnId.Value > 0)
			await RawMaterialStockData.DeleteProductStockByTransactionNo(_saleReturn.TransactionNo);

		foreach (var product in _saleReturnProductCart)
		{
			var item = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, product.ProductId);
			if (item.LocationId != 1)
				continue;

			// Remove stock from the return location (negative quantity)
			await RawMaterialStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = -product.Quantity,
				TransactionNo = _saleReturn.TransactionNo,
				Type = StockType.SaleReturn.ToString(),
				TransactionDate = DateOnly.FromDateTime(_saleReturn.ReturnDateTime),
				LocationId = _saleReturn.LocationId
			});

			// Add stock back to main location (positive quantity)
			await RawMaterialStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = product.Quantity,
				TransactionNo = _saleReturn.TransactionNo,
				Type = StockType.SaleReturn.ToString(),
				TransactionDate = DateOnly.FromDateTime(_saleReturn.ReturnDateTime),
				LocationId = 1
			});
		}
	}

	private async Task<int> InsertAccounting()
	{
		if (SaleReturnId.HasValue && SaleReturnId.Value > 0)
		{
			var existingAccounting = await AccountingData.LoadAccountingByReferenceNo(_saleReturn.TransactionNo);
			if (existingAccounting is not null && existingAccounting.Id > 0)
			{
				existingAccounting.Status = false;
				await AccountingData.InsertAccounting(existingAccounting);
			}
		}

		return await AccountingData.InsertAccounting(new()
		{
			Id = 0,
			ReferenceNo = _saleReturn.TransactionNo,
			AccountingDate = DateOnly.FromDateTime(_saleReturn.ReturnDateTime),
			FinancialYearId = (await FinancialYearData.LoadFinancialYearByDate(DateOnly.FromDateTime(_saleReturn.ReturnDateTime))).Id,
			VoucherId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId)).Value),
			Remarks = _saleReturn.Remarks,
			UserId = _saleReturn.UserId,
			GeneratedModule = GeneratedModules.SaleReturn.ToString(),
			Status = true
		});
	}

	private async Task InsertAccountingDetails(int accountingId)
	{
		var saleReturnOverview = await SaleReturnData.LoadSaleReturnOverviewBySaleReturnId(_saleReturn.Id);

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = (await LedgerData.LoadLedgerByLocation(saleReturnOverview.LocationId)).Id,
			Credit = saleReturnOverview.Total,
			Debit = null,
			Remarks = $"Cash / Party Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
			Status = true
		});

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.SaleReturnVoucherId)).Value),
			Debit = saleReturnOverview.Total - saleReturnOverview.TotalTaxAmount,
			Credit = null,
			Remarks = $"Sales Return Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
			Status = true
		});

		await AccountingData.InsertAccountingDetails(new()
		{
			Id = 0,
			AccountingId = accountingId,
			LedgerId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.GSTLedgerId)).Value),
			Debit = saleReturnOverview.TotalTaxAmount,
			Credit = null,
			Remarks = $"GST Account Posting For Sale Return Bill {saleReturnOverview.TransactionNo}",
			Status = true
		});
	}
	#endregion
}