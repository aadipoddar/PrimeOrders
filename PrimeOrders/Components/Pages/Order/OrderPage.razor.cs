using Syncfusion.Blazor.DropDowns;
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

	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;

	private OrderProductCartModel _selectedProductCart = new();

	private OrderModel _order = new()
	{
		Id = 0,
		OrderDate = DateOnly.FromDateTime(DateTime.Now),
		Remarks = "",
		SaleId = null,
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

	private SfDialog _sfOrderConfirmDialog;
	private bool _confirmDialogVisible = false;

	#region LoadData
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_isLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

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

	private async Task LoadComboBox()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_locations.Remove(_locations.FirstOrDefault(c => c.Id == 1));

		if (_user.LocationId == 1)
			_order.LocationId = _locations.FirstOrDefault()?.Id ?? 0;
		else
			_order.LocationId = _user.LocationId;

		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.RemoveAll(r => r.LocationId != 1);
		_selectedProductCategoryId = _productCategories.FirstOrDefault()?.Id ?? 0;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_products.RemoveAll(r => r.LocationId != 1);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

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
		{
			await JS.InvokeVoidAsync("showToast", "Order Already been Executed. Please Update Sale First");
			NavManager.NavigateTo("/");
		}

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

		await _sfProductCartGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Products
	private async void ProductCategoryChanged(ListBoxChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedProductCategoryId = args.Value;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_products.RemoveAll(r => r.LocationId != 1);

		await _sfProductGrid.Refresh();
		StateHasChanged();
	}

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

	private void CloseConfirmationDialog()
	{
		_confirmDialogVisible = false;
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

	private void OnSaveOrderClick()
	{
		_confirmDialogVisible = true;
		StateHasChanged();
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

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo("/Order", forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
	#endregion
}