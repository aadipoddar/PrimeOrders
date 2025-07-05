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
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _isSaving = false;
	private bool _returnDetailsDialogVisible = false;
	private bool _returnSummaryDialogVisible = false;
	private bool _locationDialogVisible = false;

	private decimal _selectedQuantity = 1;
	private int _selectedProductId = 0;

	private string _productSearchText = "";
	private int _selectedProductIndex = -1;
	private List<ProductModel> _filteredProducts = [];
	private bool _isProductSearchActive = false;
	private bool _hasAddedProductViaSearch = true;

	private SaleReturnProductCartModel _selectedProductCart = new();
	private ProductModel _selectedProduct = new();
	private SaleReturnModel _saleReturn = new()
	{
		ReturnDateTime = DateTime.Now,
		Remarks = "",
		Status = true
	};

	private List<ProductModel> _products;
	private List<LocationModel> _locations = [];
	private readonly List<SaleReturnProductCartModel> _saleReturnProductCart = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<SaleReturnProductCartModel> _sfProductCartGrid;

	private SfDialog _sfReturnDetailsDialog;
	private SfDialog _sfReturnSummaryDialog;
	private SfDialog _sfLocationDialog;
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

		await LoadData();
		await JS.InvokeVoidAsync("setupSaleReturnPageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		// Load locations for main location users
		await LoadLocations();

		_products = await ProductData.LoadProductByLocationRate(_user.LocationId);
		_products.RemoveAll(r => r.LocationId != 1 && r.LocationId != _user.LocationId);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_filteredProducts = [.. _products];

		// Set default location based on user's role
		if (_user.LocationId == 1)
			_saleReturn.LocationId = _locations.FirstOrDefault()?.Id ?? _user.LocationId;
		else
			_saleReturn.LocationId = _user.LocationId;

		_saleReturn.TransactionNo = await GenerateBillNo.GenerateSaleReturnTransactionNo(_saleReturn);

		if (SaleReturnId.HasValue && SaleReturnId.Value > 0)
			await LoadSaleReturn();

		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
			// Remove main location from the list as we want to select other locations
			_locations.RemoveAll(l => l.Id == 1);
		}
	}

	private async Task LoadSaleReturn()
	{
		_saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId.Value);

		if (_saleReturn is null)
			NavManager.NavigateTo("/SaleReturn");

		_saleReturnProductCart.Clear();

		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(_saleReturn.Id);
		foreach (var item in saleReturnDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);

			_saleReturnProductCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = item.Quantity
			});
		}

		StateHasChanged();
	}
	#endregion

	#region Location Selection
	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<int, LocationModel> args)
	{
		_saleReturn.LocationId = args.Value;
		// Regenerate transaction number when location changes
		_saleReturn.TransactionNo = await GenerateBillNo.GenerateSaleReturnTransactionNo(_saleReturn);
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
		_hasAddedProductViaSearch = true;
		_isProductSearchActive = true;
		_productSearchText = "";
		_selectedProductIndex = 0;
		_filteredProducts = _products.ToList();

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
				p.Name.Contains(_productSearchText, StringComparison.OrdinalIgnoreCase) ||
				p.Code.Contains(_productSearchText, StringComparison.OrdinalIgnoreCase)
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
		if (_selectedProduct.Id > 0)
		{
			_selectedQuantity = 1;
			_quantityDialogVisible = true;
			await ExitProductSearch();
			StateHasChanged();
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
	private void OnAddToCartButtonClick(ProductModel product)
	{
		if (product is null || product.Id <= 0)
			return;

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
	#endregion

	#region Dialog Events
	private async Task OnAddToCartClick()
	{
		if (_selectedQuantity <= 0)
		{
			OnCancelQuantityDialogClick();
			return;
		}

		var existingProduct = _saleReturnProductCart.FirstOrDefault(c => c.ProductId == _selectedProduct.Id);

		if (existingProduct is not null)
			existingProduct.Quantity += _selectedQuantity;
		else
		{
			_saleReturnProductCart.Add(new()
			{
				ProductId = _selectedProduct.Id,
				ProductName = _selectedProduct.Name,
				Quantity = _selectedQuantity
			});
		}

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

		// Don't override LocationId if user is from main location and has selected a specific location
		if (_user.LocationId != 1)
			_saleReturn.LocationId = _user.LocationId;

		if (SaleReturnId is null)
			_saleReturn.TransactionNo = await GenerateBillNo.GenerateSaleReturnTransactionNo(_saleReturn);

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

		_returnSummaryDialogVisible = false;
		_isSaving = false;
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
				Status = true
			});
	}

	private async Task InsertStock()
	{
		if (SaleReturnId.HasValue && SaleReturnId.Value > 0)
			await StockData.DeleteProductStockByTransactionNo(_saleReturn.TransactionNo);

		foreach (var product in _saleReturnProductCart)
		{
			var item = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, product.ProductId);
			if (item.LocationId != 1)
				continue;

			// Remove stock from the return location (negative quantity)
			await StockData.InsertProductStock(new()
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
			await StockData.InsertProductStock(new()
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
	#endregion
}