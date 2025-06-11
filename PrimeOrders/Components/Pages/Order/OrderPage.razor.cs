using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Order;

public partial class OrderPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;

	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;

	private OrderProductCartModel _selectedProductCart = new();

	private readonly OrderModel _order = new()
	{
		Id = 0,
		OrderDate = DateOnly.FromDateTime(DateTime.Now),
		Remarks = "",
		Completed = false,
		Status = true
	};

	private List<LocationModel> _locations;
	private List<ProductCategoryModel> _productCategories;
	private List<ProductModel> _products;
	private readonly List<OrderProductCartModel> _orderProductCarts = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<OrderProductCartModel> _sfProductCartGrid;

	private SfDialog _sfProductManageDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region LoadData
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_isLoading = true;

		if (firstRender && !await ValidatePassword()) NavManager.NavigateTo("/Login");

		_isLoading = false;

		StateHasChanged();

		if (firstRender)
			await LoadComboBox();
	}

	private async Task<bool> ValidatePassword()
	{
		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var password = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
			return false;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
		if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), password))
			return false;

		_user = user;
		return true;
	}

	private async void ProductCategoryChanged(ListBoxChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedProductCategoryId = args.Value;
		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);

		await _sfProductGrid.Refresh();
		StateHasChanged();
	}

	private async Task LoadComboBox()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_order.LocationId = _user.LocationId;

		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_selectedProductCategoryId = _productCategories.FirstOrDefault()?.Id ?? 0;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_order.OrderNo = await GenerateBillNo.GenerateOrderBillNo(_order.LocationId);

		StateHasChanged();
	}
	#endregion

	#region Products
	public async void ProductRowSelectHandler(RowSelectEventArgs<ProductModel> args)
	{
		_selectedProductId = args.Data.Id;
		var product = args.Data;

		var existingProduct = _orderProductCarts.FirstOrDefault(p => p.ProductId == _selectedProductId);

		if (existingProduct is not null)
			existingProduct.Quantity += 1;

		else
			_orderProductCarts.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 1
			});

		_selectedProductId = 0;
		await _sfProductCartGrid?.Refresh();
		await _sfProductGrid?.Refresh();
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
	private async void OnSaveProductManageClick()
	{
		_orderProductCarts.Remove(_orderProductCarts.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		if (_selectedProductCart.Quantity > 0)
			_orderProductCarts.Add(_selectedProductCart);

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		StateHasChanged();
	}

	private async void OnRemoveFromCartProductManageClick()
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

		if (!_user.Admin)
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

	private async void OnSaveOrderClick()
	{
		if (!await ValidateForm())
			return;

		_order.OrderNo = await GenerateBillNo.GenerateOrderBillNo(_order.LocationId);

		_order.Id = await OrderData.InsertOrder(_order);
		if (_order.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save the order. Please try again.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return;
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

		await _sfSuccessToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
	#endregion
}