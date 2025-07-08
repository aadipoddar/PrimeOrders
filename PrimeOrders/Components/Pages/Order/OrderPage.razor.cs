using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Order;

public partial class OrderPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	[Parameter] public int? OrderId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _orderDetailsDialogVisible = false;
	private bool _orderSummaryDialogVisible = false;

	private decimal _selectedQuantity = 1;
	private int _selectedProductId = 0;

	private string _productSearchText = "";
	private int _selectedProductIndex = -1;
	private List<ProductModel> _filteredProducts = [];
	private bool _isProductSearchActive = false;
	private bool _hasAddedProductViaSearch = true;

	private OrderProductCartModel _selectedProductCart = new();
	private ProductModel _selectedProduct = new();

	private OrderModel _order = new()
	{
		Id = 0,
		OrderDate = DateOnly.FromDateTime(DateTime.Now),
		Remarks = "",
		SaleId = null,
		Status = true
	};

	private List<LocationModel> _locations;
	private List<ProductModel> _products;
	private readonly List<OrderProductCartModel> _orderProductCarts = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<OrderProductCartModel> _sfProductCartGrid;

	private SfDialog _sfOrderDetailsDialog;
	private SfDialog _sfOrderSummaryDialog;
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

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Order)).User) is not null))
			return;

		await LoadData();
		await JS.InvokeVoidAsync("setupOrderPageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_locations.Remove(_locations.FirstOrDefault(c => c.Id == 1));

		if (_user.LocationId == 1)
			_order.LocationId = _locations.FirstOrDefault()?.Id ?? 0;
		else
			_order.LocationId = _user.LocationId;

		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		_products.RemoveAll(r => r.LocationId != 1);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_filteredProducts = [.. _products];

		_order.OrderNo = await GenerateBillNo.GenerateOrderBillNo(_order);

		if (OrderId.HasValue && OrderId > 0)
			await LoadOrder();

		StateHasChanged();
	}

	private async Task LoadOrder()
	{
		_order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, OrderId.Value);

		if (_order is null)
			NavManager.NavigateTo("/");

		if (_order.SaleId != null && _order.SaleId > 0)
			NavManager.NavigateTo("/");

		_orderProductCarts.Clear();

		var orderDetails = await OrderData.LoadOrderDetailByOrder(_order.Id);
		foreach (var detail in orderDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);

			_orderProductCarts.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = detail.Quantity
			});
		}

		if (_sfProductCartGrid is not null)
			await _sfProductCartGrid.Refresh();
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

	public void ProductCartRowSelectHandler(RowSelectEventArgs<OrderProductCartModel> args)
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

		var existingProduct = _orderProductCarts.FirstOrDefault(c => c.ProductId == _selectedProduct.Id);

		if (existingProduct is not null)
			existingProduct.Quantity += _selectedQuantity;
		else
		{
			_orderProductCarts.Add(new()
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
		_orderProductCarts.Remove(_orderProductCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		if (_selectedProductCart.Quantity > 0)
			_orderProductCarts.Add(_selectedProductCart);

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		StateHasChanged();
	}

	private async Task OnRemoveFromCartProductManageClick()
	{
		_selectedProductCart.Quantity = 0;
		_orderProductCarts.Remove(_orderProductCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_order.UserId = _user.Id;

		if (OrderId is null)
			_order.OrderNo = await GenerateBillNo.GenerateOrderBillNo(_order);

		if (!_user.Admin || _user.LocationId != 1)
			_order.LocationId = _user.LocationId;

		if (_order.LocationId <= 0)
		{
			_sfErrorToast.Content = "Please Select a Location for the Order";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_orderProductCarts.Count == 0)
		{
			_sfErrorToast.Content = "Please add at least one product to the order.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task ConfirmOrderSubmission()
	{
		if (!await ValidateForm())
			return;

		_order.Id = await OrderData.InsertOrder(_order);
		if (_order.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save the order. Please try again.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return;
		}

		await InsertOrderDetails();

		_orderSummaryDialogVisible = false;
		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertOrderDetails()
	{
		if (OrderId.HasValue && OrderId > 0)
		{
			var existingOrderDetails = await OrderData.LoadOrderDetailByOrder(_order.Id);
			foreach (var existingDetail in existingOrderDetails)
			{
				existingDetail.Status = false;
				await OrderData.InsertOrderDetail(existingDetail);
			}
		}

		foreach (var cartItem in _orderProductCarts)
			await OrderData.InsertOrderDetail(new OrderDetailModel()
			{
				Id = 0,
				OrderId = _order.Id,
				ProductId = cartItem.ProductId,
				Quantity = cartItem.Quantity,
				Status = true
			});
	}
	#endregion
}