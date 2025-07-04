using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class RawMaterialCategoryPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;

	private RawMaterialCategoryModel _categoryModel = new()
	{
		Name = "",
		Status = true
	};

	private List<RawMaterialCategoryModel> _categories = [];

	private SfGrid<RawMaterialCategoryModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((await AuthService.ValidateUser(JS, NavManager, UserRoles.Admin, true)).User is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_categories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<RawMaterialCategoryModel> args)
	{
		_categoryModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_categoryModel.Name))
		{
			_sfErrorToast.Content = "Category name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await RawMaterialData.InsertRawMaterialCategory(_categoryModel);
		await _sfToast.ShowAsync();
	}
}