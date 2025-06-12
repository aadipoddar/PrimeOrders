using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class ProductStockAdjustmentPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedLocationId = 1;
	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;
	private double _selectedProductQuantity = 1;

	private List<LocationModel> _locations = [];
	private List<ProductStockDetailModel> _stockDetails = [];
	private readonly List<ItemRecipeModel> _newStockProducts = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];

	private SfGrid<ProductStockDetailModel> _sfStockGrid;
	private SfGrid<ItemRecipeModel> _sfNewStockGrid;
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
			await LoadInitialData();
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

	private async Task LoadInitialData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location, true);
		if (_user.LocationId != 1)
			_selectedLocationId = _user.LocationId;

		_productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.RawMaterialCategory);
		_selectedProductCategoryId = _productCategories.Count > 0 ? _productCategories[0].Id : 0;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;

		StateHasChanged();
		await LoadStockDetails();
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

		await _sfStockGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Product
	private async void ProductCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedProductCategoryId = args.Value;
		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;
		StateHasChanged();
	}

	private async void OnAddButtonClick()
	{
		var existingProduct = _newStockProducts.FirstOrDefault(r => r.ItemId == _selectedProductId && r.ItemCategoryId == _selectedProductCategoryId);
		if (existingProduct is not null)
			existingProduct.Quantity += (decimal)_selectedProductQuantity;
		else
			_newStockProducts.Add(new ItemRecipeModel
			{
				ItemId = _selectedProductId,
				ItemName = _products.FirstOrDefault(r => r.Id == _selectedProductId)?.Name ?? "Unknown",
				ItemCategoryId = _selectedProductCategoryId,
				Quantity = (decimal)_selectedProductQuantity
			});

		await _sfNewStockGrid.Refresh();
		StateHasChanged();
	}

	public void RowSelectHandler(RowSelectEventArgs<ItemRecipeModel> args)
	{
		if (args.Data is not null)
			_newStockProducts.Remove(args.Data);

		_sfNewStockGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async void OnSaveButtonClick()
	{
		await _sfNewStockGrid.Refresh();

		if (_newStockProducts.Count == 0)
		{
			await _sfErrorToast.ShowAsync();
			return;
		}

		foreach (var item in _newStockProducts)
		{
			decimal quantity = 0;

			var existingStock = _stockDetails.FirstOrDefault(s => s.ProductId == item.ItemId);

			if (existingStock is null)
				quantity = item.Quantity;

			else
				quantity = item.Quantity - existingStock.ClosingStock;

			await StockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ItemId,
				Quantity = quantity,
				Type = StockType.Adjustment.ToString(),
				TransactionNo = "",
				TransactionDate = DateOnly.FromDateTime(DateTime.Now),
				LocationId = _selectedLocationId,
			});
		}

		await _sfSuccessToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
	#endregion
}