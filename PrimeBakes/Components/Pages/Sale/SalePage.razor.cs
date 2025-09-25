using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Order;
using PrimeOrdersLibrary.Data.Product;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Accounts.Masters;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Order;
using PrimeOrdersLibrary.Models.Product;
using PrimeOrdersLibrary.Models.Sale;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Components.Pages.Sale;

public partial class SalePage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedCategoryId = 0;

	private bool _partyDetailsDialogVisible = false;
	private bool _productDetailsDialogVisible = false;
	private bool _basicInfoDialogVisible = false;
	private bool _discountDetailsDialogVisible = false;
	private bool _taxDetailsDialogVisible = false;

	private LedgerModel? _selectedParty;
	private SaleProductCartModel? _selectedProductForEdit;

	private SaleModel _sale = new()
	{
		Id = 0,
		SaleDateTime = DateTime.Now,
		OrderId = null,
		Remarks = "",
		Cash = 0,
		Card = 0,
		UPI = 0,
		Credit = 0,
		PartyId = null,
		CustomerId = null,
		DiscPercent = 0,
		DiscReason = "",
		CreatedAt = DateTime.Now,
		Status = true,
	};

	private List<LedgerModel> _parties = [];
	private List<OrderModel> _orders = [];
	private List<ProductCategoryModel> _productCategories = [];
	private readonly List<SaleProductCartModel> _cart = [];
	private readonly List<SaleProductCartModel> _allCart = [];

	private SfGrid<SaleProductCartModel> _sfGrid;

	private SfDialog _sfPartyDetailsDialog;
	private SfDialog _sfProductDetailsDialog;
	private SfDialog _sfBasicInfoDialog;
	private SfDialog _sfDiscountDetailsDialog;
	private SfDialog _sfTaxDetailsDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_sale.LocationId = _user.LocationId;
		_sale.UserId = _user.Id;

		_parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
		_parties.RemoveAll(p => p.LocationId == _user.LocationId);

		await LoadSale();
		await LoadProductCategories();
		await LoadProducts();
		await LoadExistingCart();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task LoadSale()
	{
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.Sale);
		if (File.Exists(fullPath))
			_sale = System.Text.Json.JsonSerializer.Deserialize<SaleModel>(await File.ReadAllTextAsync(fullPath));

		if (_sale.PartyId is not null && _sale.PartyId > 0)
		{
			_selectedParty = _parties.FirstOrDefault(p => p.Id == _sale.PartyId);

			if (_selectedParty is not null && _selectedParty.LocationId is not null)
			{
				var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _selectedParty.LocationId.Value);
				_sale.DiscPercent = location.Discount;
				_orders = await OrderData.LoadOrderByLocation(_selectedParty.LocationId.Value);
			}
		}

		_sale.LocationId = _user.LocationId;
		_sale.UserId = _user.Id;
		_sale.BillNo = await GenerateCodes.GenerateSaleBillNo(_sale);
	}

	private async Task LoadProductCategories()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.RemoveAll(r => r.LocationId != 1 && r.LocationId != _user.LocationId);
		_productCategories.Add(new() { Id = 0, Name = "All Categories" });
		_productCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;
	}

	private async Task LoadProducts()
	{
		_allCart.Clear();

		var allProducts = await ProductData.LoadProductByLocationRate(_user.LocationId);
		allProducts.RemoveAll(r => r.LocationId != 1 && r.LocationId != _user.LocationId);

		var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

		foreach (var product in allProducts)
		{
			var productTax = taxes.FirstOrDefault(t => t.Id == product.TaxId) ?? new TaxModel();

			_allCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				ProductCategoryId = product.ProductCategoryId,
				Rate = product.Rate,
				Quantity = 0,
				BaseTotal = 0,
				DiscPercent = _sale.DiscPercent,
				DiscAmount = 0,
				AfterDiscount = 0,
				CGSTPercent = productTax.Extra ? productTax.CGST : 0,
				CGSTAmount = 0,
				SGSTPercent = productTax.Extra ? productTax.SGST : 0,
				SGSTAmount = 0,
				IGSTPercent = productTax.Extra ? productTax.IGST : 0,
				IGSTAmount = 0,
				Total = 0,
				NetRate = 0
			});
		}

		ResetCart();
		UpdateFinancialDetails();
	}

	private async Task LoadExistingCart()
	{
		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleProductCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
			{
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Rate = item.Rate;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Quantity = item.Quantity;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().DiscPercent = item.DiscPercent;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().CGSTPercent = item.CGSTPercent;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().SGSTPercent = item.SGSTPercent;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().IGSTPercent = item.IGSTPercent;
			}
		}

		UpdateFinancialDetails();
	}
	#endregion

	#region Party Order
	private async Task OnPartyChanged(ChangeEventArgs<LedgerModel?, LedgerModel> args)
	{
		_orders = [];
		_selectedParty = args.Value;

		if (args.ItemData is not null && args.ItemData.Id > 0)
		{
			_sale.PartyId = args.ItemData.Id;

			if (args.ItemData.LocationId is not null)
			{
				var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, args.ItemData.LocationId.Value);
				_sale.DiscPercent = location.Discount;
				_orders = await OrderData.LoadOrderByLocation(args.ItemData.LocationId.Value);
			}
		}

		else
		{
			_sale.PartyId = null;
			_sale.DiscPercent = 0;
		}

		_sale.OrderId = null;

		foreach (var item in _cart)
			item.DiscPercent = _sale.DiscPercent;

		await SaveSaleFile();
		StateHasChanged();
	}

	private async Task OnOrderChanged(ChangeEventArgs<int?, OrderModel> args)
	{
		ResetCart();

		if (args.Value.HasValue && args.Value.Value > 0)
		{
			_sale.OrderId = args.Value;

			var orderDetails = await OrderData.LoadOrderDetailByOrder(_sale.OrderId.Value);

			foreach (var item in orderDetails)
			{
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Quantity += item.Quantity;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().DiscPercent = _sale.DiscPercent;
			}
		}

		else _sale.OrderId = null;

		await SaveSaleFile();
		StateHasChanged();
	}
	#endregion

	#region Cart
	private void ResetCart()
	{
		_cart.Clear();

		foreach (var item in _allCart)
			_cart.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = item.ProductName,
				ProductCategoryId = item.ProductCategoryId,
				Rate = item.Rate,
				Quantity = item.Quantity,
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
				NetRate = item.NetRate
			});

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));
	}

	private async Task OnProductCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;

		else
			_selectedCategoryId = args.Value;

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task AddToCart(SaleProductCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveSaleFile();
	}

	private async Task UpdateQuantity(SaleProductCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveSaleFile();
	}
	#endregion

	#region Saving
	private void UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = item.Quantity > 0 ? item.Total / item.Quantity : 0;
		}

		_sfGrid?.Refresh();
		StateHasChanged();
	}

	private async Task SaveSaleFile()
	{
		UpdateFinancialDetails();

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.Sale), System.Text.Json.JsonSerializer.Serialize(_sale));

		if (!_cart.Any(x => x.Quantity > 0) && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task GoToCart()
	{
		await SaveSaleFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart)) == false)
			return;

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		_cart.Clear();

		NavManager.NavigateTo("/Sale/Cart");
	}
	#endregion

	#region Product Details Dialog
	private void OpenProductDetailsDialog(SaleProductCartModel item)
	{
		if (item is null || item.Quantity <= 0)
			return;

		_selectedProductForEdit = item;
		_productDetailsDialogVisible = true;
		StateHasChanged();
	}

	private void OpenBasicDetails()
	{
		_productDetailsDialogVisible = false;
		_basicInfoDialogVisible = true;
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		StateHasChanged();
	}

	private void OpenDiscountDetails()
	{
		_productDetailsDialogVisible = false;
		_discountDetailsDialogVisible = true;
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		StateHasChanged();
	}

	private void OpenTaxDetails()
	{
		_productDetailsDialogVisible = false;
		_taxDetailsDialogVisible = true;
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		StateHasChanged();
	}
	#endregion

	#region Basic Info Dialog
	private async Task OnBasicRateChanged(decimal newRate)
	{
		if (_selectedProductForEdit is null)
			return;

		_selectedProductForEdit.Rate = Math.Max(0, newRate);
		await SaveSaleFile();
	}

	private async Task OnBasicQuantityChanged(decimal newQuantity)
	{
		if (_selectedProductForEdit is null)
			return;

		_selectedProductForEdit.Quantity = Math.Max(0, newQuantity);
		await SaveSaleFile();
	}

	private async Task OnSaveBasicInfoClick()
	{
		_basicInfoDialogVisible = false;
		await SaveSaleFile();
	}
	#endregion

	#region Discount Details Dialog
	private async Task OnDiscountPercentChanged(decimal newDiscountPercent)
	{
		if (_selectedProductForEdit is null)
			return;

		_selectedProductForEdit.DiscPercent = Math.Max(0, Math.Min(100, newDiscountPercent));
		await SaveSaleFile();
	}

	private async Task OnSaveDiscountClick()
	{
		_discountDetailsDialogVisible = false;
		await SaveSaleFile();
	}
	#endregion

	#region Tax Details Dialog
	private async Task OnCGSTPercentChanged(decimal newCGSTPercent)
	{
		if (_selectedProductForEdit is null)
			return;

		_selectedProductForEdit.CGSTPercent = Math.Max(0, Math.Min(50, newCGSTPercent));
		await SaveSaleFile();
	}

	private async Task OnSGSTPercentChanged(decimal newSGSTPercent)
	{
		if (_selectedProductForEdit is null)
			return;

		_selectedProductForEdit.SGSTPercent = Math.Max(0, Math.Min(50, newSGSTPercent));
		await SaveSaleFile();
	}

	private async Task OnIGSTPercentChanged(decimal newIGSTPercent)
	{
		if (_selectedProductForEdit is null)
			return;

		_selectedProductForEdit.IGSTPercent = Math.Max(0, Math.Min(50, newIGSTPercent));
		await SaveSaleFile();
	}

	private async Task OnSaveTaxClick()
	{
		_taxDetailsDialogVisible = false;
		await SaveSaleFile();
	}
	#endregion
}