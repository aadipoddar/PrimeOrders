using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class KitchenIssuePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedRawMaterialCategoryId = 0;
	private int _selectedRawMaterialId = 0;

	private double _selectedRawMaterialQuantity = 1;

	private List<KitchenModel> _kitchens = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<RawMaterialModel> _rawMaterials = [];

	private readonly List<RawMaterialRecipeModel> _rawMaterialCart = [];

	private KitchenIssueModel _kitchenIssue = new();

	private SfGrid<RawMaterialRecipeModel> _sfGrid;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

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
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		_kitchenIssue.KitchenId = _kitchens.Count > 0 ? _kitchens[0].Id : 0;

		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_selectedRawMaterialCategoryId = _rawMaterialCategories.Count > 0 ? _rawMaterialCategories[0].Id : 0;

		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;
	}

	private async void RawMaterialCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		_selectedRawMaterialCategoryId = args.Value;
		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;
	}

	private async Task OnAddButtonClick()
	{
		var existingRecipe = _rawMaterialCart.FirstOrDefault(r => r.RawMaterialId == _selectedRawMaterialId && r.RawMaterialCategoryId == _selectedRawMaterialCategoryId);
		if (existingRecipe is not null)
			existingRecipe.Quantity += (decimal)_selectedRawMaterialQuantity;

		else
			_rawMaterialCart.Add(new()
			{
				RawMaterialCategoryId = _selectedRawMaterialCategoryId,
				RawMaterialId = _selectedRawMaterialId,
				RawMaterialName = (await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, _selectedRawMaterialId)).Name,
				Quantity = (decimal)_selectedRawMaterialQuantity,
			});

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	public void RowSelectHandler(RowSelectEventArgs<RawMaterialRecipeModel> args)
	{
		if (args.Data is not null)
			_rawMaterialCart.Remove(args.Data);

		_sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		_kitchenIssue.UserId = _user.Id;
		_kitchenIssue.IssueDate = DateTime.Now;
		_kitchenIssue.Status = true;
		_kitchenIssue.LocationId = _user.LocationId;
		await _sfGrid.Refresh();

		if (_kitchenIssue.KitchenId <= 0)
		{
			_sfErrorToast.Content = "Please select a kitchen.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return false;
		}
		if (_rawMaterialCart.Count == 0)
		{
			_sfErrorToast.Content = "Please add at least one raw material.";
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

		_kitchenIssue.Id = await KitchenIssueData.InsertKitchenIssue(_kitchenIssue);
		if (_kitchenIssue.Id <= 0)
		{
			_sfErrorToast.Content = "Failed to save kitchen issue. Please try again.";
			await _sfErrorToast.ShowAsync();
			StateHasChanged();
			return;
		}

		await InsertKitchenIssueDetail();
		await InsertStock();

		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertKitchenIssueDetail()
	{
		foreach (var item in _rawMaterialCart)
			await KitchenIssueData.InsertKitchenIssueDetail(new()
			{
				Id = 0,
				KitchenIssueId = _kitchenIssue.Id,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				Status = true
			});
	}

	private async Task InsertStock()
	{
		foreach (var item in _rawMaterialCart)
			await StockData.InsertStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = -item.Quantity,
				BillId = _kitchenIssue.Id,
				TransactionDate = DateOnly.FromDateTime(DateTime.Now),
				Type = StockType.KitchenIssue.ToString(),
				LocationId = _user.LocationId,
			});
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo("/Inventory/Purchase", forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}