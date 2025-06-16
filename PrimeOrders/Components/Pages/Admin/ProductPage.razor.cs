using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class ProductPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;
	private UserModel _user;

	private ProductModel _productModel = new()
	{
		Name = "",
		Code = "",
		Rate = 0,
		TaxId = 0,
		ProductCategoryId = 0,
		LocationId = 1,
		Status = true
	};

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<TaxModel> _taxTypes = [];

	private SfGrid<ProductModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_isLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

		_isLoading = false;
		StateHasChanged();

		if (firstRender)
			await LoadData();
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

	private async Task LoadData()
	{
		_products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
		_productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		_taxTypes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

		_productModel.LocationId = _user.LocationId;

		if (_user.LocationId != 1)
		{
			_products = [.. _products.Where(p => p.LocationId == _user.LocationId)];
			_productCategories = [.. _productCategories.Where(c => c.LocationId == _user.LocationId || c.LocationId == 1)];
		}

		if (_productCategories.Count > 0)
			_productModel.ProductCategoryId = _productCategories[0].Id;

		if (_taxTypes.Count > 0)
			_productModel.TaxId = _taxTypes[0].Id;

		await _sfGrid?.Refresh();
		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<ProductModel> args)
	{
		_productModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (_user.LocationId != 1)
			_productModel.LocationId = _user.LocationId;

		if (string.IsNullOrWhiteSpace(_productModel.Name))
		{
			_sfErrorToast.Content = "Product name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_productModel.Code))
		{
			_sfErrorToast.Content = "Product code is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_productModel.ProductCategoryId <= 0)
		{
			_sfErrorToast.Content = "Please select a product category.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_productModel.LocationId <= 0)
		{
			_sfErrorToast.Content = "Please select a location.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_productModel.TaxId <= 0)
		{
			_sfErrorToast.Content = "Please select a tax type.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await ProductData.InsertProduct(_productModel);
		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}