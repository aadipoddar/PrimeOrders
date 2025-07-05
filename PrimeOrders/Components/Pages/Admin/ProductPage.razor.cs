using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class ProductPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

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

	private List<ProductRateModel> _productRates = [];
	private int _newLocationRateId = 0;
	private decimal _newLocationRate = 0;

	private SfGrid<ProductModel> _sfGrid;
	private SfGrid<ProductRateModel> _sfRatesGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Admin)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
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

		if (_locations.Count > 0 && _newLocationRateId == 0)
			_newLocationRateId = _locations[0].Id;

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<ProductModel> args)
	{
		_productModel = args.Data;
		await LoadProductRates();
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task LoadProductRates()
	{
		if (_productModel.Id > 0)
		{
			_productRates = await ProductData.LoadProductRateByProduct(_productModel.Id);

			if (_user.LocationId != 1)
				_productRates = [.. _productRates.Where(r => r.LocationId == _user.LocationId)];

			if (_sfRatesGrid is not null)
				await _sfRatesGrid.Refresh();
		}

		else
			_productRates.Clear();
	}

	public List<LocationModel> GetAvailableLocations()
	{
		var locationsWithRates = _productRates.Select(r => r.LocationId).ToList();
		return [.. _locations.Where(l => l.Status && !locationsWithRates.Contains(l.Id))];
	}

	private async Task DeleteProductRate(int productRateId)
	{
		if (productRateId > 0)
		{
			var productRate = _productRates.FirstOrDefault(r => r.Id == productRateId);
			await ProductData.InsertProductRate(new ProductRateModel
			{
				Id = productRateId,
				ProductId = _productModel.Id,
				LocationId = productRate.LocationId,
				Rate = productRate.Rate,
				Status = false
			});
		}

		await LoadProductRates();
		GetAvailableLocations();
		StateHasChanged();
	}

	public async Task AddLocationRate()
	{
		if (_productModel.Id <= 0)
		{
			_sfErrorToast.Content = "Please save the product first before adding location rates.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		if (_newLocationRateId <= 0)
		{
			_sfErrorToast.Content = "Please select a location.";
			await _sfErrorToast.ShowAsync();
			return;
		}

		await ProductData.InsertProductRate(new()
		{
			Id = 0,
			ProductId = _productModel.Id,
			LocationId = _newLocationRateId,
			Rate = _newLocationRate > 0 ? _newLocationRate : _productModel.Rate,
			Status = true
		});

		_newLocationRateId = _locations.FirstOrDefault()?.Id ?? 0;
		_newLocationRate = 0;

		await LoadProductRates();
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
}