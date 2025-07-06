using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class KitchenProductionPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	[Parameter] public int? KitchenProductionId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _billDetailsDialogVisible = false;
	private bool _kitchenDialogVisible = false;
	private bool _kitchenProductionSummaryDialogVisible = false;

	private decimal _baseTotal = 0;
	private decimal _total = 0;
	private decimal _selectedQuantity = 1;

	private int _selectedProductId = 0;

	private string _productSearchText = "";
	private int _selectedProductIndex = -1;
	private List<ProductModel> _filteredProducts = [];
	private bool _isProductSearchActive = false;
	private bool _hasAddedProductViaSearch = true;

	private KitchenProductionProductCartModel _selectedProductCart = new();
	private ProductModel _selectedProduct = new();

	private KitchenModel _kitchen = new();
	private KitchenProductionModel _kitchenProduction = new()
	{
		ProductionDate = DateTime.Now,
		Status = true
	};

	private List<KitchenModel> _kitchens;
	private List<ProductModel> _products;
	private readonly List<KitchenProductionProductCartModel> _kitchenProductionProductCarts = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<KitchenProductionProductCartModel> _sfProductCartGrid;

	private SfDialog _sfBillDetailsDialog;
	private SfDialog _sfKitchenDialog;
	private SfDialog _sfKitchenProductionSummaryDialog;
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

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory, true)).User) is not null))
			return;

		await LoadData();
		await JS.InvokeVoidAsync("setupKitchenProductionPageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		_kitchen = _kitchens.FirstOrDefault();
		_kitchenProduction.KitchenId = _kitchen?.Id ?? 0;

		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		_products.RemoveAll(p => p.LocationId != 1); // Only products from main location
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_filteredProducts = [.. _products];

		_kitchenProduction.LocationId = _user?.LocationId ?? 0;
		_kitchenProduction.UserId = _user?.Id ?? 0;
		_kitchenProduction.TransactionNo = await GenerateBillNo.GenerateKitchenProductionTransactionNo(_kitchenProduction);

		if (KitchenProductionId.HasValue && KitchenProductionId.Value > 0)
			await LoadKitchenProduction();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task LoadKitchenProduction()
	{
		_kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, KitchenProductionId.Value);

		if (_kitchenProduction is null)
			NavManager.NavigateTo("/Inventory-Dashboard");

		_kitchenProductionProductCarts.Clear();

		var kitchenProductionDetails = await KitchenProductionData.LoadKitchenProductionDetailByKitchenProduction(_kitchenProduction.Id);
		foreach (var item in kitchenProductionDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);

			_kitchenProductionProductCarts.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = product.Name,
				Quantity = item.Quantity,
				Rate = product.Rate,
				Total = item.Quantity * product.Rate
			});
		}

		if (_sfProductCartGrid is not null)
			await _sfProductCartGrid.Refresh();

		UpdateFinancialDetails();
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
				(p.Code != null && p.Code.Contains(_productSearchText, StringComparison.OrdinalIgnoreCase))
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

	#region Kitchen Production Details Events
	private async Task OnKitchenChanged(ChangeEventArgs<int, KitchenModel> args)
	{
		_kitchen = await CommonData.LoadTableDataById<KitchenModel>(TableNames.Kitchen, args.Value);
		_kitchen ??= new KitchenModel();

		_kitchenProduction.KitchenId = _kitchen?.Id ?? 0;
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void UpdateFinancialDetails()
	{
		_kitchenProduction.KitchenId = _kitchen?.Id ?? 0;
		_kitchenProduction.UserId = _user?.Id ?? 0;
		_kitchenProduction.LocationId = _user?.LocationId ?? 0;

		foreach (var item in _kitchenProductionProductCarts)
		{
			item.Total = item.Rate * item.Quantity;
		}

		_baseTotal = _kitchenProductionProductCarts.Sum(c => c.Total);
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

	public void ProductCartRowSelectHandler(RowSelectEventArgs<KitchenProductionProductCartModel> args)
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

		var existingProduct = _kitchenProductionProductCarts.FirstOrDefault(c => c.ProductId == _selectedProduct.Id);

		if (existingProduct is not null)
			existingProduct.Quantity += _selectedQuantity;
		else
		{
			_kitchenProductionProductCarts.Add(new()
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
		_kitchenProductionProductCarts.Remove(_kitchenProductionProductCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		if (_selectedProductCart.Quantity > 0)
			_kitchenProductionProductCarts.Add(_selectedProductCart);

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnRemoveFromCartProductManageClick()
	{
		_selectedProductCart.Quantity = 0;
		_kitchenProductionProductCarts.Remove(_kitchenProductionProductCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		if (_kitchenProductionProductCarts.Count == 0 || _kitchenProductionProductCarts is null)
		{
			_sfErrorToast.Content = "Please add at least one product to the kitchen production.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_kitchenProduction.KitchenId <= 0)
		{
			_sfErrorToast.Content = "Please select a kitchen.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_kitchenProduction.TransactionNo))
		{
			_sfErrorToast.Content = "Transaction No is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_kitchenProduction.UserId == 0)
		{
			_sfErrorToast.Content = "User is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}
		return true;
	}

	private async Task OnSaveKitchenProductionClick()
	{
		UpdateFinancialDetails();

		if (!await ValidateForm())
			return;

		_kitchenProduction.Id = await KitchenProductionData.InsertKitchenProduction(_kitchenProduction);
		if (_kitchenProduction.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save kitchen production.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return;
		}

		await InsertKitchenProductionDetail();
		await InsertStock();

		_kitchenProductionSummaryDialogVisible = false;
		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertKitchenProductionDetail()
	{
		if (KitchenProductionId.HasValue && KitchenProductionId.Value > 0)
		{
			var existingKitchenProductionDetails = await KitchenProductionData.LoadKitchenProductionDetailByKitchenProduction(KitchenProductionId.Value);
			foreach (var item in existingKitchenProductionDetails)
			{
				item.Status = false;
				await KitchenProductionData.InsertKitchenProductionDetail(item);
			}
		}

		foreach (var item in _kitchenProductionProductCarts)
			await KitchenProductionData.InsertKitchenProductionDetail(new()
			{
				Id = 0,
				KitchenProductionId = _kitchenProduction.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Status = true
			});
	}

	private async Task InsertStock()
	{
		if (KitchenProductionId.HasValue && KitchenProductionId.Value > 0)
			await StockData.DeleteProductStockByTransactionNo(_kitchenProduction.TransactionNo);

		if (!_kitchenProduction.Status)
			return;

		foreach (var item in _kitchenProductionProductCarts)
			await StockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Type = StockType.KitchenProduction.ToString(),
				TransactionNo = _kitchenProduction.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(_kitchenProduction.ProductionDate),
				LocationId = _user.LocationId
			});
	}
	#endregion
}