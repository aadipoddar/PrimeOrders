using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class KitchenProductionPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;

	private double _selectedProductQuantity = 1;

	private List<KitchenModel> _kitchens = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];

	private readonly List<ItemRecipeModel> _productCart = [];

	private readonly KitchenProductionModel _kitchenProduction = new() { ProductionDate = DateTime.Now };

	private SfGrid<ItemRecipeModel> _sfGrid;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_isLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

		if (firstRender)
			await LoadComboBox();

		_isLoading = false;
		StateHasChanged();
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
		_kitchenProduction.UserId = _user.Id;
		_kitchenProduction.LocationId = _user.LocationId;
		_kitchenProduction.TransactionNo = await GenerateBillNo.GenerateKitchenProductionTransactionNo(_kitchenProduction);

		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		_kitchenProduction.KitchenId = _kitchens.Count > 0 ? _kitchens[0].Id : 0;

		_productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.RemoveAll(r => r.LocationId != 1);
		_selectedProductCategoryId = _productCategories.Count > 0 ? _productCategories[0].Id : 0;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_products.RemoveAll(r => r.LocationId != 1);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;
	}
	#endregion

	#region Product
	private async Task ProductCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedProductCategoryId = args.Value;
		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_products.RemoveAll(r => r.LocationId != 1);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;
	}

	private async Task OnAddButtonClick()
	{
		var existingItem = _productCart.FirstOrDefault(r => r.ItemId == _selectedProductId && r.ItemCategoryId == _selectedProductCategoryId);
		if (existingItem is not null)
			existingItem.Quantity += (decimal)_selectedProductQuantity;

		else
			_productCart.Add(new()
			{
				ItemCategoryId = _selectedProductCategoryId,
				ItemId = _selectedProductId,
				ItemName = (await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, _selectedProductId)).Name,
				Quantity = (decimal)_selectedProductQuantity,
			});

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	public void RowSelectHandler(RowSelectEventArgs<ItemRecipeModel> args)
	{
		if (args.Data is not null)
			_productCart.Remove(args.Data);

		_sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_kitchenProduction.UserId = _user.Id;
		_kitchenProduction.LocationId = _user.LocationId;
		_kitchenProduction.Status = true;
		_kitchenProduction.TransactionNo = await GenerateBillNo.GenerateKitchenProductionTransactionNo(_kitchenProduction);
		await _sfGrid.Refresh();

		if (_kitchenProduction.KitchenId <= 0)
		{
			_sfErrorToast.Content = "Please select a kitchen.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return false;
		}
		if (_productCart.Count == 0)
		{
			_sfErrorToast.Content = "Please add at least one Product.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return false;
		}
		return true;
	}

	private async Task OnSaveButtonClick()
	{
		if (!await ValidateForm())
			return;

		_kitchenProduction.Id = await KitchenProductionData.InsertKitchenProduction(_kitchenProduction);
		if (_kitchenProduction.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save kitchen production. Please try again.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return;
		}

		await InsertKitchenProductionDetail();
		await InsertStock();

		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertKitchenProductionDetail()
	{
		foreach (var item in _productCart)
			await KitchenProductionData.InsertKitchenProductionDetail(new()
			{
				Id = 0,
				KitchenProductionId = _kitchenProduction.Id,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				Status = true
			});
	}

	private async Task InsertStock()
	{
		foreach (var item in _productCart)
			await StockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				BillId = _kitchenProduction.Id,
				TransactionDate = DateOnly.FromDateTime(DateTime.Now),
				Type = StockType.KitchenProduction.ToString(),
				LocationId = _user.LocationId,
			});
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo("/Inventory-Dashboard", forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
	#endregion
}