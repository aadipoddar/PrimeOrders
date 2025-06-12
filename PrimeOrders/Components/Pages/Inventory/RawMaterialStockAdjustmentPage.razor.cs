using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class RawMaterialStockAdjustmentPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] private IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedLocationId = 1;
	private int _selectedRawMaterialCategoryId = 0;
	private int _selectedRawMaterialId = 0;
	private double _selectedRawMaterialQuantity = 1;

	private List<LocationModel> _locations = [];
	private List<RawMaterialStockDetailModel> _stockDetails = [];
	private readonly List<ItemRecipeModel> _newStockRawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<RawMaterialModel> _rawMaterials = [];

	private SfGrid<RawMaterialStockDetailModel> _sfStockGrid;
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

		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_selectedRawMaterialCategoryId = _rawMaterialCategories.Count > 0 ? _rawMaterialCategories[0].Id : 0;

		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;

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

		_stockDetails = await StockData.LoadRawMaterialStockDetailsByDateLocationId(
			DateTime.Now.AddDays(-1),
			DateTime.Now.AddDays(1),
			locationId);

		await _sfStockGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region RawMaterial
	private async void RawMaterialCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		_selectedRawMaterialCategoryId = args.Value;
		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;
		StateHasChanged();
	}

	private async void OnAddButtonClick()
	{
		var existingRawMaterial = _newStockRawMaterials.FirstOrDefault(r => r.ItemId == _selectedRawMaterialId && r.ItemCategoryId == _selectedRawMaterialCategoryId);
		if (existingRawMaterial is not null)
			existingRawMaterial.Quantity += (decimal)_selectedRawMaterialQuantity;
		else
			_newStockRawMaterials.Add(new ItemRecipeModel
			{
				ItemId = _selectedRawMaterialId,
				ItemName = _rawMaterials.FirstOrDefault(r => r.Id == _selectedRawMaterialId)?.Name ?? "Unknown",
				ItemCategoryId = _selectedRawMaterialCategoryId,
				Quantity = (decimal)_selectedRawMaterialQuantity
			});

		await _sfNewStockGrid.Refresh();
		StateHasChanged();
	}

	public void RowSelectHandler(RowSelectEventArgs<ItemRecipeModel> args)
	{
		if (args.Data is not null)
			_newStockRawMaterials.Remove(args.Data);

		_sfNewStockGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async void OnSaveButtonClick()
	{
		await _sfNewStockGrid.Refresh();

		if (_newStockRawMaterials.Count == 0)
		{
			await _sfErrorToast.ShowAsync();
			return;
		}

		foreach (var item in _newStockRawMaterials)
		{
			decimal quantity = 0;

			var existingStock = _stockDetails.FirstOrDefault(s => s.RawMaterialId == item.ItemId);

			if (existingStock is null)
				quantity = item.Quantity;

			else
				quantity = item.Quantity - existingStock.ClosingStock;

			await StockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.ItemId,
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