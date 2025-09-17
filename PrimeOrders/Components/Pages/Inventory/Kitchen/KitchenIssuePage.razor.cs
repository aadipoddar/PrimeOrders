using PrimeOrdersLibrary.Data.Inventory.Kitchen;
using PrimeOrdersLibrary.Data.Notification;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Inventory.Kitchen;

public partial class KitchenIssuePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }
	[Inject] public IBrowserNotificationService BrowserNotificationService { get; set; }

	[Parameter] public int? KitchenIssueId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;
	private bool _dialogVisible = false;
	private bool _quantityDialogVisible = false;
	private bool _billDetailsDialogVisible = false;
	private bool _kitchenIssueSummaryDialogVisible = false;

	private decimal _baseTotal = 0;
	private decimal _total = 0;
	private decimal _selectedQuantity = 1;

	private int _selectedRawMaterialId = 0;

	private string _materialSearchText = "";
	private int _selectedMaterialIndex = -1;
	private List<RawMaterialModel> _filteredRawMaterials = [];
	private bool _isMaterialSearchActive = false;
	private bool _hasAddedMaterialViaSearch = true;

	private KitchenIssueRawMaterialCartModel _selectedRawMaterialCart = new();
	private RawMaterialModel _selectedRawMaterial = new();

	private KitchenModel _kitchen = new();
	private KitchenIssueModel _kitchenIssue = new()
	{
		IssueDate = DateTime.Now,
		Status = true,
		Remarks = ""
	};

	private List<KitchenModel> _kitchens;
	private List<RawMaterialModel> _rawMaterials;
	private readonly List<KitchenIssueRawMaterialCartModel> _kitchenIssueRawMaterialCarts = [];

	private SfGrid<RawMaterialModel> _sfRawMaterialGrid;
	private SfGrid<KitchenIssueRawMaterialCartModel> _sfRawMaterialCartGrid;

	private SfDialog _sfBillDetailsDialog;
	private SfDialog _sfKitchenIssueSummaryDialog;
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
		await JS.InvokeVoidAsync("setupKitchenIssuePageKeyboardHandlers", DotNetObjectReference.Create(this));

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		_kitchen = _kitchens.FirstOrDefault();
		_kitchenIssue.KitchenId = _kitchen?.Id ?? 0;

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(0, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_kitchenIssue.IssueDate, "India Standard Time"));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;

		_filteredRawMaterials = [.. _rawMaterials];

		_kitchenIssue.LocationId = _user?.LocationId ?? 0;
		_kitchenIssue.UserId = _user?.Id ?? 0;
		_kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(_kitchenIssue);

		if (KitchenIssueId.HasValue && KitchenIssueId.Value > 0)
			await LoadKitchenIssue();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task LoadKitchenIssue()
	{
		_kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, KitchenIssueId.Value);

		if (_kitchenIssue is null)
			NavManager.NavigateTo("/Inventory-Dashboard");

		_kitchenIssueRawMaterialCarts.Clear();

		var kitchenIssueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(_kitchenIssue.Id);
		foreach (var item in kitchenIssueDetails)
		{
			var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, item.RawMaterialId);

			_kitchenIssueRawMaterialCarts.Add(new()
			{
				RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
				RawMaterialId = item.RawMaterialId,
				RawMaterialName = rawMaterial.Name,
				Quantity = item.Quantity,
				Rate = rawMaterial.MRP,
				Total = item.Quantity * rawMaterial.MRP
			});
		}

		if (_sfRawMaterialCartGrid is not null)
			await _sfRawMaterialCartGrid.Refresh();

		UpdateFinancialDetails();
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

	#region Kitchen Issue Details Events
	private async Task OnKitchenChanged(ChangeEventArgs<int, KitchenModel> args)
	{
		_kitchen = await CommonData.LoadTableDataById<KitchenModel>(TableNames.Kitchen, args.Value);
		_kitchen ??= new KitchenModel();

		_kitchenIssue.KitchenId = _kitchen?.Id ?? 0;
		UpdateFinancialDetails();
		StateHasChanged();
	}

	public async Task KitchenIssueDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_kitchenIssue.IssueDate = args.Value;

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(0, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(_kitchenIssue.IssueDate, "India Standard Time"));
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;
		_filteredRawMaterials = [.. _rawMaterials];

		if (_sfRawMaterialGrid is not null)
			await _sfRawMaterialGrid.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void UpdateFinancialDetails()
	{
		_kitchenIssue.KitchenId = _kitchen?.Id ?? 0;
		_kitchenIssue.UserId = _user?.Id ?? 0;
		_kitchenIssue.LocationId = _user?.LocationId ?? 0;

		foreach (var item in _kitchenIssueRawMaterialCarts)
		{
			item.Total = item.Rate * item.Quantity;
		}

		_baseTotal = _kitchenIssueRawMaterialCarts.Sum(c => c.Total);
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

	public void RawMaterialCartRowSelectHandler(RowSelectEventArgs<KitchenIssueRawMaterialCartModel> args)
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

		var existingMaterial = _kitchenIssueRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterial.Id);

		if (existingMaterial is not null)
			existingMaterial.Quantity += _selectedQuantity;
		else
		{
			_kitchenIssueRawMaterialCarts.Add(new()
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
		_kitchenIssueRawMaterialCarts.Remove(_kitchenIssueRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		if (_selectedRawMaterialCart.Quantity > 0)
			_kitchenIssueRawMaterialCarts.Add(_selectedRawMaterialCart);

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task OnRemoveFromCartRawMaterialManageClick()
	{
		_selectedRawMaterialCart.Quantity = 0;
		_kitchenIssueRawMaterialCarts.Remove(_kitchenIssueRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_kitchenIssue.IssueDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(_kitchenIssue.IssueDate)
			.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)),
			"India Standard Time");

		if (_kitchenIssueRawMaterialCarts.Count == 0 || _kitchenIssueRawMaterialCarts is null)
		{
			_sfErrorToast.Content = "Please add at least one raw material to the kitchen issue.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_kitchenIssue.KitchenId <= 0)
		{
			_sfErrorToast.Content = "Please select a kitchen.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_kitchenIssue.TransactionNo))
		{
			_sfErrorToast.Content = "Transaction No is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_kitchenIssue.UserId == 0)
		{
			_sfErrorToast.Content = "User is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}
		return true;
	}

	private async Task OnSaveKitchenIssueClick()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			UpdateFinancialDetails();

			if (!await ValidateForm())
				return;

			_kitchenIssue.Id = await KitchenIssueData.InsertKitchenIssue(_kitchenIssue);
			if (_kitchenIssue.Id <= 0)
			{
				_sfErrorToast.Content = "Failed to save kitchen issue.";
				await _sfErrorToast.ShowAsync();
				return;
			}

			await InsertKitchenIssueDetail();
			await InsertStock();
			await SendNotification.SendKitchenIssueNotificationMainLocationAdminInventory(_kitchenIssue.Id);

			_kitchenIssueSummaryDialogVisible = false;
			await _sfSuccessToast.ShowAsync();
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task InsertKitchenIssueDetail()
	{
		if (KitchenIssueId.HasValue && KitchenIssueId.Value > 0)
		{
			var existingKitchenIssueDetails = await KitchenIssueData.LoadKitchenIssueDetailByKitchenIssue(KitchenIssueId.Value);
			foreach (var item in existingKitchenIssueDetails)
			{
				item.Status = false;
				await KitchenIssueData.InsertKitchenIssueDetail(item);
			}
		}

		foreach (var item in _kitchenIssueRawMaterialCarts)
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
		if (KitchenIssueId.HasValue && KitchenIssueId.Value > 0)
			await StockData.DeleteRawMaterialStockByTransactionNo(_kitchenIssue.TransactionNo);

		if (!_kitchenIssue.Status)
			return;

		foreach (var item in _kitchenIssueRawMaterialCarts)
			await StockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = -item.Quantity,
				Type = StockType.KitchenIssue.ToString(),
				TransactionNo = _kitchenIssue.TransactionNo,
				TransactionDate = DateOnly.FromDateTime(_kitchenIssue.IssueDate),
				LocationId = _kitchenIssue.LocationId
			});
	}
	#endregion
}