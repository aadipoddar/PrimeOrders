using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class ProductCategoryPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private LocationModel _userLocation;
	private bool _isLoading = true;

	private ProductCategoryModel _categoryModel = new()
	{
		Name = "",
		LocationId = 1,
		Status = true
	};

	private List<ProductCategoryModel> _categories = [];
	private List<LocationModel> _locations = [];

	private SfGrid<ProductCategoryModel> _sfGrid;
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

		_userLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_categories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		_categoryModel.LocationId = _user.LocationId;

		if (!_userLocation.MainLocation)
			_categories = [.. _categories.Where(c => c.LocationId == _user.LocationId)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<ProductCategoryModel> args)
	{
		_categoryModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (!_userLocation.MainLocation)
			_categoryModel.LocationId = _user.LocationId;

		if (_categoryModel.LocationId <= 0)
		{
			_sfErrorToast.Content = "Please select a location.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_categoryModel.Name))
		{
			_sfErrorToast.Content = "Category name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await ProductData.InsertProductCategory(_categoryModel);
		await _sfToast.ShowAsync();
	}
}