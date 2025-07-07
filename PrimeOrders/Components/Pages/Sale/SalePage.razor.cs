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
	private bool _quantityDialogVisible = false;
	private bool _isSaving = false;
	private bool _billDetailsDialogVisible = false;
	private bool _discountDialogVisible = false;
	private bool _orderPartyDialogVisible = false;
	private bool _saleSummaryDialogVisible = false;

	private decimal _selectedQuantity = 1;
	private decimal _baseTotal = 0;
	private decimal _discountAmount = 0;
	private decimal _subTotal = 0;
	private decimal _total = 0;

	private int _selectedProductId = 0;
	private int _selectedPaymentModeId = 0;

	private string _productSearchText = "";
	private int _selectedProductIndex = -1;
	private List<ProductModel> _filteredProducts = [];
	private bool _isProductSearchActive = false;
	private bool _hasAddedProductViaSearch = true;

	private SaleProductCartModel _selectedProductCart = new();
	private ProductModel _selectedProduct = new();
	private SaleModel _sale = new()
	{
		SaleDateTime = DateTime.Now,
		DiscReason = "",
		Remarks = "",
		Status = true
	};

	private List<PaymentModeModel> _paymentModes;
	private List<OrderModel> _orders = [];
	private List<SupplierModel> _parties = [];
	private List<ProductModel> _products;
	private readonly List<SaleProductCartModel> _saleProductCart = [];

	private SfGrid<ProductModel> _sfProductGrid;
	private SfGrid<SaleProductCartModel> _sfProductCartGrid;

	private SfDialog _sfBillDetailsDialog;
	private SfDialog _sfDiscountDialog;
	private SfDialog _sfOrderPartyDialog;
	private SfDialog _sfSaleSummaryDialog;
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
		await JS.InvokeVoidAsync("setupSalePageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_parties = await CommonData.LoadTableDataByStatus<SupplierModel>(TableNames.Supplier);
		_parties.RemoveAll(p => p.LocationId == _user.LocationId);

		_paymentModes = PaymentModeData.GetPaymentModes();
		_selectedPaymentModeId = _paymentModes.FirstOrDefault()?.Id ?? 0;

		_products = await ProductData.LoadProductByLocationRate(_user.LocationId);
		_products.RemoveAll(r => r.LocationId != 1 && r.LocationId != _user.LocationId);
		_selectedProductId = _products.FirstOrDefault()?.Id ?? 0;

		_filteredProducts = [.. _products];

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

			_saleProductCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = item.Quantity,
				Rate = item.Rate,
				DiscPercent = item.DiscPercent,
				CGSTPercent = item.CGSTPercent,
				SGSTPercent = item.SGSTPercent,
				IGSTPercent = item.IGSTPercent,
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

	#region Sale Details Events
	private async Task OnPartyChanged(ChangeEventArgs<int?, SupplierModel> args)
	{
		if (args.Value.HasValue && args.Value.Value > 0)
		{
			_sale.PartyId = args.Value.Value;

			if (args.ItemData.LocationId is not null)
			{
				var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, args.ItemData.LocationId.Value);
				_sale.DiscPercent = location.Discount;
				_orders = await OrderData.LoadOrderByLocation(args.ItemData.LocationId.Value);
			}
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
				product.Rate = (await ProductData.LoadProductByLocationRate(_user.LocationId)).FirstOrDefault(p => p.Id == product.Id)?.Rate ?? 0;

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
						CGSTPercent = productTax.Extra ? productTax.CGST : 0,
						SGSTPercent = productTax.Extra ? productTax.SGST : 0,
						IGSTPercent = productTax.Extra ? productTax.IGST : 0
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
	public async Task ProductCartRowSelectHandler(RowSelectEventArgs<SaleProductCartModel> args)
	{
		_selectedProductCart = args.Data;
		_dialogVisible = true;
		await UpdateFinancialDetails();
		StateHasChanged();
	}

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
	#endregion

	#region Dialog Events
	private async Task OnAddToCartClick()
	{
		if (_selectedQuantity <= 0)
		{
			OnCancelQuantityDialogClick();
			return;
		}

		var existingProduct = _saleProductCart.FirstOrDefault(c => c.ProductId == _selectedProduct.Id);

		if (existingProduct is not null)
			existingProduct.Quantity += _selectedQuantity;

		else
		{
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, _selectedProduct.TaxId);

			_saleProductCart.Add(new()
			{
				ProductId = _selectedProduct.Id,
				ProductName = _selectedProduct.Name,
				Quantity = _selectedQuantity,
				Rate = _selectedProduct.Rate,
				DiscPercent = _sale.DiscPercent,
				CGSTPercent = productTax.Extra ? productTax.CGST : 0,
				SGSTPercent = productTax.Extra ? productTax.SGST : 0,
				IGSTPercent = productTax.Extra ? productTax.IGST : 0
				// Rest handled in the UpdateFinancialDetails()
			});
		}

		_quantityDialogVisible = false;
		_selectedProduct = new();
		await _sfProductCartGrid?.Refresh();
		await _sfProductGrid?.Refresh();
		await UpdateFinancialDetails();

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

	#region Saving Methods
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

	private async Task<bool> SaveSale()
	{
		_sale.Id = await SaleData.InsertSale(_sale);
		if (_sale.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save Sale.";
			await _sfErrorToast.ShowAsync();
			_isSaving = false;
			StateHasChanged();
			return false;
		}

		await InsertSaleDetail();
		await InsertStock();
		await UpdateOrder();
		return true;
	}

	// Button 1: Save Only
	private async Task OnSaveOnlyClick()
	{
		if (!await ValidateForm())
			return;

		_isSaving = true;
		StateHasChanged();

		if (await SaveSale())
		{
			_sfSuccessToast.Content = "Sale saved successfully.";
			await _sfSuccessToast.ShowAsync();
		}

		_isSaving = false;
		StateHasChanged();
	}

	// Button 2: Save and Thermal Print
	private async Task OnSaveAndThermalPrintClick()
	{
		if (!await ValidateForm())
			return;

		_isSaving = true;
		StateHasChanged();

		if (await SaveSale())
		{
			await PrintThermalBill();
			_sfSuccessToast.Content = "Sale saved and thermal bill printed successfully.";
			await _sfSuccessToast.ShowAsync();
		}

		_isSaving = false;
		StateHasChanged();
	}

	// Button 3: Save and A4 Print
	private async Task OnSaveAndA4PrintClick()
	{
		if (!await ValidateForm())
			return;

		_isSaving = true;
		StateHasChanged();

		if (await SaveSale())
		{
			await PrintA4Bill();
			_sfSuccessToast.Content = "Sale saved and A4 bill generated successfully.";
			await _sfSuccessToast.ShowAsync();
		}

		_isSaving = false;
		StateHasChanged();
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

		var supplier = await CommonData.LoadTableDataById<SupplierModel>(TableNames.Supplier, _sale.PartyId.Value);
		if (supplier.LocationId.HasValue && supplier.LocationId.Value > 0)
			foreach (var product in _saleProductCart)
				await StockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = product.ProductId,
					Quantity = product.Quantity,
					Type = StockType.Purchase.ToString(),
					TransactionNo = _sale.BillNo,
					TransactionDate = DateOnly.FromDateTime(_sale.SaleDateTime),
					LocationId = supplier.LocationId.Value
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
		var content = await SaleThermalPrint.GenerateThermalBill(_sale);
		await JS.InvokeVoidAsync("printToPrinter", content.ToString());
	}

	private async Task PrintA4Bill()
	{
		var pdfBytes = await SaleA4Print.GenerateA4SaleBill(_sale, _saleProductCart);
		var fileName = $"Sale_Bill_{_sale.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

		await JS.InvokeVoidAsync("downloadPdf", Convert.ToBase64String(pdfBytes), fileName);
	}
	#endregion
}