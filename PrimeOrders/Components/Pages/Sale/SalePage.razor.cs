using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Sale;

public partial class SalePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	[Parameter] public int? SaleId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _isSaving = false;

	private decimal _baseTotal = 0;
	private decimal _discountAmount = 0;
	private decimal _subTotal = 0;
	private decimal _total = 0;

	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;
	private int _selectedPaymentModeId = 0;

	private SaleProductCartModel _selectedProductCart = new();

	private SaleModel _sale = new()
	{
		SaleDateTime = DateTime.Now,
		DiscReason = "",
		Remarks = "",
		Status = true
	};

	private List<PaymentModeModel> _paymentModes;
	private List<OrderModel> _orders = [];
	private List<LocationModel> _parties;
	private List<ProductCategoryModel> _productCategories;
	private List<ProductModel> _products;
	private readonly List<SaleProductCartModel> _saleProductCart = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<SaleProductCartModel> _sfProductCartGrid;

	private SfDialog _sfProductManageDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

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
		_parties = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_parties.Remove(_parties.FirstOrDefault(c => c.Id == _user.LocationId));

		_paymentModes = PaymentModeData.GetPaymentModes();
		_selectedPaymentModeId = _paymentModes.FirstOrDefault()?.Id ?? 0;

		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.RemoveAll(r => r.LocationId != 1 && r.LocationId != _user.LocationId);
		_selectedProductCategoryId = _productCategories.FirstOrDefault()?.Id ?? 0;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_products.RemoveAll(r => r.LocationId != 1 && r.LocationId != _user.LocationId);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_sale.LocationId = _user?.LocationId ?? 0;
		_sale.BillNo = await GenerateBillNo.GenerateSaleBillNo(_sale);

		await UpdateFinancialDetails();

		if (SaleId.HasValue && SaleId.Value > 0)
			await LoadSale();

		StateHasChanged();
	}

	private async Task LoadSale()
	{
		_sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, SaleId.Value);

		if (_sale is null)
			NavManager.NavigateTo("/Sale");

		_saleProductCart.Clear();

		var saleDetails = await SaleData.LoadSaleDetailBySale(_sale.Id);
		foreach (var item in saleDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, product.TaxId);
			_saleProductCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = item.Quantity,
				Rate = item.Rate,
				DiscPercent = item.DiscPercent,
				CGSTPercent = productTax.CGST,
				SGSTPercent = productTax.SGST,
				IGSTPercent = productTax.IGST,
				BaseTotal = item.BaseTotal,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTAmount = item.CGSTAmount,
				SGSTAmount = item.SGSTAmount,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total
			});
		}

		_selectedPaymentModeId = _sale.Cash > 0 ? 1 : _sale.Card > 0 ? 2 : _sale.UPI > 0 ? 3 : _sale.Credit > 0 ? 4 : 0;

		await UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Purchase Details Events
	private async Task OnPartyChanged(ChangeEventArgs<int?, LocationModel> args)
	{
		if (args.Value.HasValue && args.Value.Value > 0)
		{
			_sale.PartyId = args.Value.Value;
			_sale.DiscPercent = args.ItemData.Discount;
			_orders = await OrderData.LoadOrderByLocation(args.Value.Value);
		}

		else
		{
			_sale.OrderId = null;
			_sale.PartyId = null;
			_sale.DiscPercent = 0;
		}

		foreach (var item in _saleProductCart)
			item.DiscPercent = _sale.DiscPercent;

		await UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnOrderChanged(ChangeEventArgs<int?, OrderModel> args)
	{
		if (args.Value.HasValue && args.Value.Value > 0)
		{
			_sale.OrderId = args.Value;

			var orderDetails = await OrderData.LoadOrderDetailByOrder(_sale.OrderId.Value);

			_saleProductCart.Clear();

			foreach (var item in orderDetails)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
				if (product is not null)
				{
					var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, product.TaxId);

					_saleProductCart.Add(new()
					{
						ProductId = product.Id,
						ProductName = product.Name,
						Quantity = item.Quantity,
						Rate = product.Rate,
						DiscPercent = _sale.DiscPercent,
						CGSTPercent = productTax.CGST,
						SGSTPercent = productTax.SGST,
						IGSTPercent = productTax.IGST
						// Rest handled in the UpdateFinancialDetails()
					});
				}
			}
		}
		else
		{
			_sale.OrderId = null;
			_saleProductCart.Clear();
		}

		await UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task SaleDiscountPercentValueChanged(decimal args)
	{
		_sale.DiscPercent = args;

		foreach (var item in _saleProductCart)
			item.DiscPercent = args;

		await UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task UpdateFinancialDetails()
	{
		_sale.UserId = _user?.Id ?? 0;
		_sale.LocationId = _user?.LocationId ?? 0;

		if (SaleId is null)
			_sale.BillNo = await GenerateBillNo.GenerateSaleBillNo(_sale);

		foreach (var item in _saleProductCart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
		}

		_baseTotal = _saleProductCart.Sum(c => c.BaseTotal);
		_subTotal = _saleProductCart.Sum(c => c.AfterDiscount);
		_discountAmount = _baseTotal - _subTotal;
		_total = _saleProductCart.Sum(c => c.Total);

		switch (_selectedPaymentModeId)
		{
			case 1:
				_sale.Cash = _total;
				_sale.Card = 0;
				_sale.UPI = 0;
				_sale.Credit = 0;
				break;
			case 2:
				_sale.Cash = 0;
				_sale.Card = _total;
				_sale.UPI = 0;
				_sale.Credit = 0;
				break;
			case 3:
				_sale.Cash = 0;
				_sale.Card = 0;
				_sale.UPI = _total;
				_sale.Credit = 0;
				break;
			case 4:
				_sale.Cash = 0;
				_sale.Card = 0;
				_sale.UPI = 0;
				_sale.Credit = _total;
				break;
			default:
				break;
		}

		_sfProductGrid?.Refresh();
		_sfProductCartGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Products
	private async Task ProductCategoryChanged(ListBoxChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedProductId = args.Value;
		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_products.RemoveAll(c => c.LocationId != 1 && c.LocationId != _user.LocationId);

		await _sfProductGrid?.Refresh();
		await UpdateFinancialDetails();
		StateHasChanged();
	}

	public async Task ProductRowSelectHandler(RowSelectEventArgs<ProductModel> args)
	{
		_selectedProductId = args.Data.Id;
		var product = args.Data;

		var existingProduct = _saleProductCart.FirstOrDefault(c => c.ProductId == product.Id);

		if (existingProduct is not null)
			existingProduct.Quantity++;

		else
		{
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, product.TaxId);

			_saleProductCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 1,
				Rate = product.Rate,
				DiscPercent = _sale.DiscPercent,
				CGSTPercent = productTax.CGST,
				SGSTPercent = productTax.SGST,
				IGSTPercent = productTax.IGST,
				// Rest handled in the UpdateFinancialDetails()
			});
		}

		_selectedProductId = 0;
		await _sfProductCartGrid?.Refresh();
		await _sfProductGrid?.Refresh();
		await UpdateFinancialDetails();
		StateHasChanged();
	}

	public async Task ProductCartRowSelectHandler(RowSelectEventArgs<SaleProductCartModel> args)
	{
		_selectedProductCart = args.Data;
		_dialogVisible = true;
		await UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Dialog Events
	private async Task DialogRateValueChanged(decimal args)
	{
		_selectedProductCart.Rate = args;
		await UpdateModalFinancialDetails();
	}

	private async Task DialogQuantityValueChanged(decimal args)
	{
		_selectedProductCart.Quantity = args;
		await UpdateModalFinancialDetails();
	}

	private async Task DialogDiscPercentValueChanged(decimal args)
	{
		_selectedProductCart.DiscPercent = args;
		await UpdateModalFinancialDetails();
	}

	private async Task DialogCGSTPercentValueChanged(decimal args)
	{
		_selectedProductCart.CGSTPercent = args;
		await UpdateModalFinancialDetails();
	}

	private async Task DialogSGSTPercentValueChanged(decimal args)
	{
		_selectedProductCart.SGSTPercent = args;
		await UpdateModalFinancialDetails();
	}

	private async Task DialogIGSTPercentValueChanged(decimal args)
	{
		_selectedProductCart.IGSTPercent = args;
		await UpdateModalFinancialDetails();
	}

	private async Task UpdateModalFinancialDetails()
	{
		_selectedProductCart.BaseTotal = _selectedProductCart.Rate * _selectedProductCart.Quantity;
		_selectedProductCart.DiscAmount = _selectedProductCart.BaseTotal * (_selectedProductCart.DiscPercent / 100);
		_selectedProductCart.AfterDiscount = _selectedProductCart.BaseTotal - _selectedProductCart.DiscAmount;
		_selectedProductCart.CGSTAmount = _selectedProductCart.AfterDiscount * (_selectedProductCart.CGSTPercent / 100);
		_selectedProductCart.SGSTAmount = _selectedProductCart.AfterDiscount * (_selectedProductCart.SGSTPercent / 100);
		_selectedProductCart.IGSTAmount = _selectedProductCart.AfterDiscount * (_selectedProductCart.IGSTPercent / 100);
		_selectedProductCart.Total = _selectedProductCart.AfterDiscount + _selectedProductCart.CGSTAmount + _selectedProductCart.SGSTAmount + _selectedProductCart.IGSTAmount;

		await UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnSaveProductManageClick()
	{
		await UpdateModalFinancialDetails();

		_saleProductCart.Remove(_saleProductCart.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		if (_selectedProductCart.Quantity > 0)
			_saleProductCart.Add(_selectedProductCart);

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		await UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnRemoveFromCartProductManageClick()
	{
		_selectedProductCart.Quantity = 0;
		_saleProductCart.Remove(_saleProductCart.FirstOrDefault(c => c.ProductId == _selectedProductCart.ProductId));

		_dialogVisible = false;
		await _sfProductCartGrid?.Refresh();

		await UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		await UpdateFinancialDetails();

		_sale.SaleDateTime = DateOnly.FromDateTime(_sale.SaleDateTime).ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));

		if (SaleId is null)
			_sale.BillNo = await GenerateBillNo.GenerateSaleBillNo(_sale);

		if (_saleProductCart?.Count == 0)
		{
			_sfErrorToast.Content = "Please add at least one Product to the Sale.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return false;
		}

		if (_sale.UserId == 0 || _sale.LocationId == 0)
		{
			_sfErrorToast.Content = "User and Location is required.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return false;
		}
		return true;
	}

	private async Task OnSaveSaleClick()
	{
		if (!await ValidateForm())
			return;

		_isSaving = true;
		StateHasChanged();

		_sale.Id = await SaleData.InsertSale(_sale);
		if (_sale.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save Sale.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		await InsertSaleDetail();
		await InsertStock();
		await UpdateOrder();
		await PrintThermalBill();

		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertSaleDetail()
	{
		if (SaleId.HasValue && SaleId.Value > 0)
		{
			var existingSaleDetails = await SaleData.LoadSaleDetailBySale(_sale.Id);
			foreach (var item in existingSaleDetails)
			{
				item.Status = false;
				await SaleData.InsertSaleDetail(item);
			}
		}

		foreach (var item in _saleProductCart)
			await SaleData.InsertSaleDetail(new()
			{
				Id = 0,
				SaleId = _sale.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
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
				Status = true
			});
	}

	private async Task InsertStock()
	{
		if (SaleId.HasValue && SaleId.Value > 0)
			await StockData.DeleteProductStockByTransactionNo(_sale.BillNo);

		foreach (var product in _saleProductCart)
		{
			var item = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, product.ProductId);
			if (item.LocationId != 1)
				continue;

			await StockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = -product.Quantity,
				TransactionNo = _sale.BillNo,
				Type = StockType.Sale.ToString(),
				TransactionDate = DateOnly.FromDateTime(_sale.SaleDateTime),
				LocationId = _sale.LocationId
			});
		}

		if (_sale.PartyId is null || _sale.PartyId <= 0)
			return;

		foreach (var product in _saleProductCart)
			await StockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = product.ProductId,
				Quantity = product.Quantity,
				Type = StockType.Purchase.ToString(),
				TransactionNo = _sale.BillNo,
				TransactionDate = DateOnly.FromDateTime(_sale.SaleDateTime),
				LocationId = _sale.PartyId.Value
			});
	}

	private async Task UpdateOrder()
	{
		if (SaleId.HasValue && SaleId.Value > 0)
		{
			var existingOrder = await OrderData.LoadOrderBySale(_sale.Id);
			if (existingOrder is not null && existingOrder.Id > 0)
			{
				existingOrder.SaleId = null;
				await OrderData.InsertOrder(existingOrder);
			}
		}

		if (_sale.OrderId is null)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _sale.OrderId.Value);
		if (order is not null && order.Status)
		{
			order.SaleId = _sale.Id;
			await OrderData.InsertOrder(order);
		}
	}

	private async Task PrintThermalBill()
	{
		var content = await PrintBill.PrintThermalBill(_sale);
		await JS.InvokeVoidAsync("printToPrinter", content.ToString());
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo("/Sale", forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
	#endregion
}