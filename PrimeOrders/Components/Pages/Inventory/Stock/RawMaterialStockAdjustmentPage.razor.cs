using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Inventory.Stock;

public partial class RawMaterialStockAdjustmentPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _stockDetailsDialogVisible = false;
	private bool _adjustmentSummaryDialogVisible = false;

	private decimal _baseTotal = 0;
	private decimal _total = 0;
	private decimal _selectedQuantity = 1;

	private int _selectedLocationId = 1;
	private int _selectedRawMaterialId = 0;

	private string _materialSearchText = "";
	private int _selectedMaterialIndex = -1;
	private List<RawMaterialModel> _filteredRawMaterials = [];
	private bool _isMaterialSearchActive = false;
	private bool _hasAddedMaterialViaSearch = true;

	private StockAdjustmentRawMaterialCartModel _selectedRawMaterialCart = new();
	private RawMaterialModel _selectedRawMaterial = new();

	private List<RawMaterialStockDetailModel> _stockDetails = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private readonly List<StockAdjustmentRawMaterialCartModel> _stockAdjustmentRawMaterialCarts = [];

	private SfGrid<RawMaterialModel> _sfRawMaterialGrid;
	private SfGrid<StockAdjustmentRawMaterialCartModel> _sfRawMaterialCartGrid;
	private SfGrid<RawMaterialStockDetailModel> _sfStockGrid;

	private SfDialog _sfStockDetailsDialog;
	private SfDialog _sfAdjustmentSummaryDialog;
	private SfDialog _sfRawMaterialManageDialog;
	private SfDialog _sfQuantityDialog;
	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region LoadData
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory, true)).User) is not null))
			return;

		await LoadData();
		await JS.InvokeVoidAsync("setupStockAdjustmentPageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_selectedLocationId = 1;

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDate(0, DateOnly.FromDateTime(DateTime.Now));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;

		_filteredRawMaterials = [.. _rawMaterials];

		await LoadStockDetails();
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task LoadStockDetails()
	{
		_stockDetails = await StockData.LoadRawMaterialStockDetailsByDateLocationId(
			DateTime.Now.AddDays(-1),
			DateTime.Now.AddDays(1),
			1);

		if (_sfStockGrid is not null)
			await _sfStockGrid.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Keyboard Navigation Methods
	[JSInvokable]
	public async Task HandleKeyboardShortcut(string key)
	{
		if (_isMaterialSearchActive)
		{
			await HandleMaterialSearchKeyboard(key);
			return;
		}

		switch (key.ToLower())
		{
			case "f2":
				await StartMaterialSearch();
				break;

			case "escape":
				await HandleEscape();
				break;
		}
	}

	private async Task HandleMaterialSearchKeyboard(string key)
	{
		switch (key.ToLower())
		{
			case "escape":
				await ExitMaterialSearch();
				break;

			case "enter":
				await SelectCurrentMaterial();
				break;

			case "arrowdown":
				NavigateMaterialSelection(1);
				break;

			case "arrowup":
				NavigateMaterialSelection(-1);
				break;

			case "backspace":
				if (_materialSearchText.Length > 0)
				{
					_materialSearchText = _materialSearchText[..^1];
					await FilterMaterials();
				}
				break;

			default:
				// Add character to search if it's alphanumeric or space
				if (key.Length == 1 && (char.IsLetterOrDigit(key[0]) || key == " "))
				{
					_materialSearchText += key.ToUpper();
					await FilterMaterials();
				}
				break;
		}

		StateHasChanged();
	}

	private async Task StartMaterialSearch()
	{
		_hasAddedMaterialViaSearch = true;
		_isMaterialSearchActive = true;
		_materialSearchText = "";
		_selectedMaterialIndex = 0;
		_filteredRawMaterials = [.. _rawMaterials];

		if (_filteredRawMaterials.Count > 0)
			_selectedRawMaterial = _filteredRawMaterials[0];

		StateHasChanged();
		await JS.InvokeVoidAsync("showMaterialSearchIndicator", _materialSearchText);
	}

	private async Task ExitMaterialSearch()
	{
		_isMaterialSearchActive = false;
		_materialSearchText = "";
		_selectedMaterialIndex = -1;
		StateHasChanged();
		await JS.InvokeVoidAsync("hideMaterialSearchIndicator");
	}

	private async Task FilterMaterials()
	{
		if (string.IsNullOrEmpty(_materialSearchText))
			_filteredRawMaterials = [.. _rawMaterials];
		else
			_filteredRawMaterials = [.. _rawMaterials.Where(m =>
				m.Name.Contains(_materialSearchText, StringComparison.OrdinalIgnoreCase) ||
				m.Code != null && m.Code.Contains(_materialSearchText, StringComparison.OrdinalIgnoreCase)
			)];

		_selectedMaterialIndex = 0;
		if (_filteredRawMaterials.Count > 0)
			_selectedRawMaterial = _filteredRawMaterials[0];

		await JS.InvokeVoidAsync("updateMaterialSearchIndicator", _materialSearchText, _filteredRawMaterials.Count);
		StateHasChanged();
	}

	private void NavigateMaterialSelection(int direction)
	{
		if (_filteredRawMaterials.Count == 0) return;

		_selectedMaterialIndex += direction;

		if (_selectedMaterialIndex < 0)
			_selectedMaterialIndex = _filteredRawMaterials.Count - 1;
		else if (_selectedMaterialIndex >= _filteredRawMaterials.Count)
			_selectedMaterialIndex = 0;

		_selectedRawMaterial = _filteredRawMaterials[_selectedMaterialIndex];
		StateHasChanged();
	}

	private async Task SelectCurrentMaterial()
	{
		if (_selectedRawMaterial.Id > 0)
		{
			_selectedQuantity = 1;
			_quantityDialogVisible = true;
			await ExitMaterialSearch();
			StateHasChanged();
		}
	}

	private async Task HandleEscape()
	{
		if (_isMaterialSearchActive)
			await ExitMaterialSearch();

		StateHasChanged();
	}
	#endregion

	#region Stock Adjustment Details Events
	private void UpdateFinancialDetails()
	{
		foreach (var item in _stockAdjustmentRawMaterialCarts)
		{
			item.Total = item.Rate * item.Quantity;
		}

		_baseTotal = _stockAdjustmentRawMaterialCarts.Sum(c => c.Total);
		_total = _baseTotal;

		_sfRawMaterialCartGrid?.Refresh();
		_sfRawMaterialGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Raw Materials
	private void OnAddToCartButtonClick(RawMaterialModel material)
	{
		if (material is null || material.Id <= 0)
			return;

		_selectedRawMaterial = material;
		_selectedQuantity = 1;
		_quantityDialogVisible = true;
		_hasAddedMaterialViaSearch = false;
		StateHasChanged();
	}

	public void RawMaterialCartRowSelectHandler(RowSelectEventArgs<StockAdjustmentRawMaterialCartModel> args)
	{
		_selectedRawMaterialCart = args.Data;
		_dialogVisible = true;
		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Dialog Events
	private async Task OnAddToCartClick()
	{
		if (_selectedQuantity <= 0)
		{
			OnCancelQuantityDialogClick();
			return;
		}

		var existingMaterial = _stockAdjustmentRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterial.Id);

		if (existingMaterial is not null)
			existingMaterial.Quantity = _selectedQuantity; // For stock adjustment, we set the target quantity, not add to it
		else
		{
			_stockAdjustmentRawMaterialCarts.Add(new()
			{
				RawMaterialId = _selectedRawMaterial.Id,
				RawMaterialName = _selectedRawMaterial.Name,
				Quantity = _selectedQuantity,
				Rate = _selectedRawMaterial.MRP,
				Total = _selectedQuantity * _selectedRawMaterial.MRP
			});
		}

		_quantityDialogVisible = false;
		_selectedRawMaterial = new();
		await _sfRawMaterialCartGrid?.Refresh();
		await _sfRawMaterialGrid?.Refresh();
		UpdateFinancialDetails();

		if (_hasAddedMaterialViaSearch)
			await StartMaterialSearch();

		StateHasChanged();
	}

	private void OnCancelQuantityDialogClick()
	{
		_quantityDialogVisible = false;
		_selectedRawMaterial = new();
		StateHasChanged();
	}

	private void DialogQuantityValueChanged(decimal args)
	{
		_selectedRawMaterialCart.Quantity = args;
		UpdateModalFinancialDetails();
	}

	private void UpdateModalFinancialDetails()
	{
		_selectedRawMaterialCart.Total = _selectedRawMaterialCart.Rate * _selectedRawMaterialCart.Quantity;
		StateHasChanged();
	}

	private async Task OnSaveRawMaterialManageClick()
	{
		_stockAdjustmentRawMaterialCarts.Remove(_stockAdjustmentRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		if (_selectedRawMaterialCart.Quantity > 0)
			_stockAdjustmentRawMaterialCarts.Add(_selectedRawMaterialCart);

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnRemoveFromCartRawMaterialManageClick()
	{
		_selectedRawMaterialCart.Quantity = 0;
		_stockAdjustmentRawMaterialCarts.Remove(_stockAdjustmentRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		if (_stockAdjustmentRawMaterialCarts.Count == 0 || _stockAdjustmentRawMaterialCarts is null)
		{
			_sfErrorToast.Content = "Please add at least one raw material for stock adjustment.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_selectedLocationId <= 0)
		{
			_sfErrorToast.Content = "Please select a location.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task OnSaveStockAdjustmentClick()
	{
		UpdateFinancialDetails();

		if (!await ValidateForm())
			return;

		await InsertStockAdjustment();

		_adjustmentSummaryDialogVisible = false;
		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertStockAdjustment()
	{
		foreach (var item in _stockAdjustmentRawMaterialCarts)
		{
			decimal adjustmentQuantity = 0;
			var existingStock = _stockDetails.FirstOrDefault(s => s.RawMaterialId == item.RawMaterialId);

			if (existingStock is null)
				adjustmentQuantity = item.Quantity;
			else
				adjustmentQuantity = item.Quantity - existingStock.ClosingStock;

			if (adjustmentQuantity != 0)
				await StockData.InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = item.RawMaterialId,
					Quantity = adjustmentQuantity,
					NetRate = null,
					Type = StockType.Adjustment.ToString(),
					TransactionNo = $"ADJ-{DateTime.Now:yyyyMMddHHmmss}",
					TransactionDate = DateOnly.FromDateTime(DateTime.Now),
					LocationId = _selectedLocationId
				});
		}

		await LoadStockDetails();
	}
	#endregion
}