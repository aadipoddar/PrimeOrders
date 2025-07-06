using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class RawMaterialPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;

	private RawMaterialModel _rawMaterialModel = new()
	{
		Name = "",
		Code = "",
		RawMaterialCategoryId = 0,
		MRP = 0,
		TaxId = 0,
		Status = true
	};

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<TaxModel> _taxTypes = [];

	private SfGrid<RawMaterialModel> _sfGrid;
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
		_rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_taxTypes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

		if (_rawMaterialCategories.Count > 0)
			_rawMaterialModel.RawMaterialCategoryId = _rawMaterialCategories[0].Id;

		if (_taxTypes.Count > 0)
			_rawMaterialModel.TaxId = _taxTypes[0].Id;

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<RawMaterialModel> args)
	{
		_rawMaterialModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_rawMaterialModel.Name))
		{
			_sfErrorToast.Content = "Raw material name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_rawMaterialModel.Code))
		{
			_sfErrorToast.Content = "Raw material code is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_rawMaterialModel.RawMaterialCategoryId <= 0)
		{
			_sfErrorToast.Content = "Please select a raw material category.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_rawMaterialModel.TaxId <= 0)
		{
			_sfErrorToast.Content = "Please select a tax type.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await RawMaterialData.InsertRawMaterial(_rawMaterialModel);
		await _sfToast.ShowAsync();
	}
}