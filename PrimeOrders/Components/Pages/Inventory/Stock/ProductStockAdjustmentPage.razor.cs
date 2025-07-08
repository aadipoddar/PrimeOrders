using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Inventory.Stock;

public partial class ProductStockAdjustmentPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _stockDetailsDialogVisible = false;
	private bool _adjustmentSummaryDialogVisible = false;

	private decimal _baseTotal = 0;
	private decimal _total = 0;
	private decimal _selectedQuantity = 1;

	private int _selectedLocationId = 1;
	private int _selectedProductId = 0;

	private string _productSearchText = "";
	private int _selectedProductIndex = -1;
	private List<ProductModel> _filteredProducts = [];
	private bool _isProductSearchActive = false;
	private bool _hasAddedProductViaSearch = true;

	private ProductStockAdjustmentCartModel _selectedProductCart = new();
	private ProductModel _selectedProduct = new();

	private List<LocationModel> _locations = [];
	private List<ProductStockDetailModel> _stockDetails = [];
	private List<ProductModel> _products = [];
	private readonly List<ProductStockAdjustmentCartModel> _productStockAdjustmentCarts = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<ProductStockAdjustmentCartModel> _sfProductCartGrid;
	private SfGrid<ProductStockDetailModel> _sfStockGrid;

	private SfDialog _sfStockDetailsDialog;
	private SfDialog _sfAdjustmentSummaryDialog;
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

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory)).User) is not null))
			return;

		await LoadData();
		await JS.InvokeVoidAsync("setupProductStockAdjustmentPageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);
		if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_filteredProducts = [.. _products];

		await LoadStockDetails();
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_selectedLocationId = args.Value;
		await LoadStockDetails();
	}

	private async Task LoadStockDetails()
	{
		int locationId = _user?.LocationId == 1 ? _selectedLocationId : _user.LocationId;

		_stockDetails = await StockData.LoadProductStockDetailsByDateLocationId(
			DateTime.Now.AddDays(-1),
			DateTime.Now.AddDays(1),
			locationId);

		if (_sfStockGrid is not null)
			await _sfStockGrid?.Refresh();

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
				p.Name.Contains(_productSearchText, StringComparison.OrdinalIgnoreCase) ||
				p.Code != null && p.Code.Contains(_productSearchText, StringComparison.OrdinalIgnoreCase)
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

	#region Stock Adjustment Details Events
	private void UpdateFinancialDetails()
	{
		foreach (var item in _productStockAdjustmentCarts)
		{
			item.Total = item.Rate * item.Quantity;
		}

		_baseTotal = _productStockAdjustmentCarts.Sum(c => c.Total);
		_total = _baseTotal;

		_sfProductCartGrid?.Refresh();
		_sfProductGrid?.Refresh();
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

	public void ProductCartRowSelectHandler(RowSelectEventArgs<ProductStockAdjustmentCartModel> args)
	{
		_selectedProductCart = args.Data;
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

		var existingProduct = _productStockAdjustmentCarts.FirstOrDefault(c => c.ProductId == _selectedProduct.Id);

		if (existingProduct is not null)
			existingProduct.Quantity = _selectedQuantity; // For stock adjustment, we set the target quantity, not add to it
		else
		{
			_productStockAdjustmentCarts.Add(new()
			{
				ProductId = _selectedProduct.Id,
				ProductName = _selectedProduct.Name,
				Quantity = _selectedQuantity,
				Rate = _selectedProduct.Rate,
				Total = _selectedQuantity * _selectedProduct.Rate
			});
		}

		_quantityDialogVisible = false;
		_selectedProduct = new();
		await _sfProductCartGrid?.Refresh();
		await _sfProductGrid?.Refresh();
		UpdateFinancialDetails();

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

	private void DialogQuantityValueChanged(decimal args)
	{
		_selectedProductCart.Quantity = args;
		UpdateModalFinancialDetails();
	}

	private void UpdateModalFinancialDetails()
	{
		_selectedProductCart.Total = _selectedProductCart.Rate * _selectedProductCart.Quantity;
		StateHasChanged();
	}

	private async Task OnSaveProductManageClick()
	{
		_productStockAdjustmentCarts.Remove(_productStockAdjustmentCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		if (_selectedProductCart.Quantity > 0)
			_productStockAdjustmentCarts.Add(_selectedProductCart);

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnRemoveFromCartProductManageClick()
	{
		_selectedProductCart.Quantity = 0;
		_productStockAdjustmentCarts.Remove(_productStockAdjustmentCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		if (_productStockAdjustmentCarts.Count == 0 || _productStockAdjustmentCarts is null)
		{
			_sfErrorToast.Content = "Please add at least one product for stock adjustment.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_selectedLocationId <= 0)
		{
			_sfErrorToast.Content = "Please select a location.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task OnSaveStockAdjustmentClick()
	{
		UpdateFinancialDetails();

		if (!await ValidateForm())
			return;

		await InsertStockAdjustment();

		_adjustmentSummaryDialogVisible = false;
		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertStockAdjustment()
	{
		foreach (var item in _productStockAdjustmentCarts)
		{
			decimal adjustmentQuantity = 0;
			var existingStock = _stockDetails.FirstOrDefault(s => s.ProductId == item.ProductId);

			if (existingStock is null)
				adjustmentQuantity = item.Quantity;
			else
				adjustmentQuantity = item.Quantity - existingStock.ClosingStock;

			if (adjustmentQuantity != 0) // Only create stock entry if there's an actual adjustment
			{
				await StockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = item.ProductId,
					Quantity = adjustmentQuantity,
					Type = StockType.Adjustment.ToString(),
					TransactionNo = $"PADJ-{DateTime.Now:yyyyMMddHHmmss}",
					TransactionDate = DateOnly.FromDateTime(DateTime.Now),
					LocationId = _selectedLocationId
				});
			}
		}

		// Refresh stock details after adjustment
		await LoadStockDetails();
	}
	#endregion
}