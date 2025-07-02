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

		return true;
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

		await _sfGrid?.Refresh();
		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<RawMaterialModel> args)
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

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await RawMaterialData.InsertRawMaterial(_rawMaterialModel);
		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}