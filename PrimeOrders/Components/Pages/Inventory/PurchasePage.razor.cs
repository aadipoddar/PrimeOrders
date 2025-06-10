using DocumentFormat.OpenXml.Spreadsheet;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class PurchasePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	[Parameter] public int? PurchaseId { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _dialogVisible = false;

	private string _errorMessage = "";

	private decimal _baseTotal = 0;
	private decimal _afterDiscounts = 0;
	private decimal _subTotal = 0;
	private decimal _total = 0;

	private int _selectedRawMaterialCategoryId = 0;
	private int _selectedRawMaterialId = 0;

	private PurchaseRawMaterialCartModel _selectedRawMaterialCart = new();

	private SupplierModel _supplier = new();
	private PurchaseModel _purchase = new() { BillDate = DateOnly.FromDateTime(DateTime.Now), Status = true, Remarks = "" };

	private List<SupplierModel> _suppliers;
	private List<RawMaterialCategoryModel> _rawMaterialCategories;
	private List<RawMaterialModel> _rawMaterials;
	private readonly List<PurchaseRawMaterialCartModel> _purchaseRawMaterialCarts = [];

	private SfGrid<RawMaterialModel> _sfRawMaterialGrid;
	private SfGrid<PurchaseRawMaterialCartModel> _sfRawMaterialCartGrid;

	private SfDialog _sfRawMaterialManageDialog;
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
			await LoadComboBox();
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
		_suppliers = await CommonData.LoadTableDataByStatus<SupplierModel>(TableNames.Supplier);
		_supplier = _suppliers.FirstOrDefault();
		_purchase.SupplierId = _supplier?.Id ?? 0;

		_rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_selectedRawMaterialCategoryId = _rawMaterialCategories.FirstOrDefault()?.Id ?? 0;

		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.FirstOrDefault()?.Id ?? 0;

		if (PurchaseId.HasValue && PurchaseId.Value > 0)
			await LoadPurchase();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task LoadPurchase()
	{
		_purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, PurchaseId.Value);

		if (_purchase is null)
			NavManager.NavigateTo("/Inventory-Dashboard");

		_purchaseRawMaterialCarts.Clear();

		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(_purchase.Id);
		foreach (var item in purchaseDetails)
		{
			var product = await CommonData.LoadTableDataById<RawMaterialCategoryModel>(TableNames.RawMaterial, item.RawMaterialId);

			_purchaseRawMaterialCarts.Add(new()
			{
				RawMaterialId = item.RawMaterialId,
				RawMaterialName = product.Name,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				SGSTPercent = item.SGSTPercent,
				IGSTPercent = item.IGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTAmount = item.SGSTAmount,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total,
			});
		}

		await _sfRawMaterialCartGrid?.Refresh();
		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Purchase Details Events
	private async void OnSupplierChanged(ChangeEventArgs<int, SupplierModel> args)
	{
		_supplier = await CommonData.LoadTableDataById<SupplierModel>(TableNames.Supplier, args.Value);
		_purchase.SupplierId = _supplier?.Id ?? 0;
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void CashDiscountPercentValueChanged(decimal args)
	{
		_purchase.CDPercent = args;
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void UpdateFinancialDetails()
	{
		_purchase.SupplierId = _supplier?.Id ?? 0;
		_purchase.UserId = _user?.Id ?? 0;

		foreach (var item in _purchaseRawMaterialCarts)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
		}

		_baseTotal = _purchaseRawMaterialCarts.Sum(c => c.BaseTotal);
		_afterDiscounts = _purchaseRawMaterialCarts.Sum(c => c.AfterDiscount);
		_subTotal = _purchaseRawMaterialCarts.Sum(c => c.Total);
		_purchase.CDAmount = _subTotal * (_purchase.CDPercent / 100);
		_total = _subTotal - _purchase.CDAmount;

		_sfRawMaterialCartGrid?.Refresh();
		_sfRawMaterialGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Raw Materials
	private async void RawMatrialCategoryChanged(ListBoxChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		_selectedRawMaterialCategoryId = args.Value;
		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);

		await _sfRawMaterialGrid.Refresh();
		UpdateFinancialDetails();
		StateHasChanged();
	}

	public async void RawMaterialRowSelectHandler(RowSelectEventArgs<RawMaterialModel> args)
	{
		_selectedRawMaterialId = args.Data.Id;
		var rawMaterial = args.Data;

		var existingProduct = _purchaseRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == rawMaterial.Id);

		if (existingProduct is not null)
			existingProduct.Quantity++;

		else
		{
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, rawMaterial.TaxId);

			_purchaseRawMaterialCarts.Add(new()
			{
				RawMaterialId = rawMaterial.Id,
				RawMaterialName = rawMaterial.Name,
				Quantity = 1,
				Rate = rawMaterial.MRP,
				BaseTotal = rawMaterial.MRP,
				DiscPercent = 0,
				CGSTPercent = productTax.CGST,
				SGSTPercent = productTax.SGST,
				IGSTPercent = productTax.IGST,
				// Rest handled in the UpdateFinancialDetails()
			});
		}

		_selectedRawMaterialId = 0;
		await _sfRawMaterialCartGrid?.Refresh();
		await _sfRawMaterialGrid?.Refresh();
		UpdateFinancialDetails();
		StateHasChanged();
	}

	public void RawMaterialCartRowSelectHandler(RowSelectEventArgs<PurchaseRawMaterialCartModel> args)
	{
		_selectedRawMaterialCart = args.Data;
		_dialogVisible = true;
		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Dialog Events
	private void DialogRateValueChanged(decimal args)
	{
		_selectedRawMaterialCart.Rate = args;
		UpdateModalFinancialDetails();
	}

	private void DialogQuantityValueChanged(decimal args)
	{
		_selectedRawMaterialCart.Quantity = args;
		UpdateModalFinancialDetails();
	}

	private void DialogDiscPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.DiscPercent = args;
		UpdateModalFinancialDetails();
	}

	private void DialogCGSTPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.CGSTPercent = args;
		UpdateModalFinancialDetails();
	}

	private void DialogSGSTPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.SGSTPercent = args;
		UpdateModalFinancialDetails();
	}

	private void DialogIGSTPercentValueChanged(decimal args)
	{
		_selectedRawMaterialCart.IGSTPercent = args;
		UpdateModalFinancialDetails();
	}

	private void UpdateModalFinancialDetails()
	{
		_selectedRawMaterialCart.BaseTotal = _selectedRawMaterialCart.Rate * _selectedRawMaterialCart.Quantity;
		_selectedRawMaterialCart.DiscAmount = _selectedRawMaterialCart.BaseTotal * (_selectedRawMaterialCart.DiscPercent / 100);
		_selectedRawMaterialCart.AfterDiscount = _selectedRawMaterialCart.BaseTotal - _selectedRawMaterialCart.DiscAmount;
		_selectedRawMaterialCart.CGSTAmount = _selectedRawMaterialCart.AfterDiscount * (_selectedRawMaterialCart.CGSTPercent / 100);
		_selectedRawMaterialCart.SGSTAmount = _selectedRawMaterialCart.AfterDiscount * (_selectedRawMaterialCart.SGSTPercent / 100);
		_selectedRawMaterialCart.IGSTAmount = _selectedRawMaterialCart.AfterDiscount * (_selectedRawMaterialCart.IGSTPercent / 100);
		_selectedRawMaterialCart.Total = _selectedRawMaterialCart.AfterDiscount + _selectedRawMaterialCart.CGSTAmount + _selectedRawMaterialCart.SGSTAmount + _selectedRawMaterialCart.IGSTAmount;

		StateHasChanged();
	}

	private async void OnSaveRawMaterialManageClick()
	{
		_purchaseRawMaterialCarts.Remove(_purchaseRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		if (_selectedRawMaterialCart.Quantity > 0)
			_purchaseRawMaterialCarts.Add(_selectedRawMaterialCart);

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async void OnRemoveFromCartRawMaterialManageClick()
	{
		_selectedRawMaterialCart.Quantity = 0;
		_purchaseRawMaterialCarts.Remove(_purchaseRawMaterialCarts.FirstOrDefault(c => c.RawMaterialId == _selectedRawMaterialCart.RawMaterialId));

		_dialogVisible = false;
		await _sfRawMaterialCartGrid?.Refresh();

		UpdateFinancialDetails();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		if (_purchaseRawMaterialCarts.Count == 0 || _purchaseRawMaterialCarts is null)
		{
			_errorMessage = "Please add at least one raw material to the purchase.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_purchase.SupplierId == 0)
		{
			_errorMessage = "Please select a supplier.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_purchase.BillNo))
		{
			_errorMessage = "Bill No is required.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return false;
		}
		if (_purchase.UserId == 0)
		{
			_errorMessage = "User is required.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return false;
		}
		return true;
	}

	private async void OnSavePurchaseClick()
	{
		UpdateFinancialDetails();

		if (!await ValidateForm())
			return;

		_purchase.Id = await PurchaseData.InsertPurchase(_purchase);
		if (_purchase.Id <= 0)
		{
			_errorMessage = "Failed to save purchase.";
			StateHasChanged();
			await _sfErrorToast.ShowAsync();
			return;
		}

		await InsertStock();
		await InsertPurchaseDetail();

		await _sfSuccessToast.ShowAsync();
	}

	private async Task InsertStock()
	{
		if (PurchaseId.HasValue && PurchaseId.Value > 0)
		{
			var existingPurchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(PurchaseId.Value);

			foreach (var item in existingPurchaseDetails)
				await StockData.InsertStock(new()
				{
					Id = 0,
					RawMaterialId = item.RawMaterialId,
					Quantity = -item.Quantity, // Deducting stock for existing purchase
					Type = StockType.PurchaseUpdate.ToString(),
					BillId = _purchase.Id,
					TransactionDate = _purchase.BillDate,
					LocationId = _user.LocationId
				});
		}

		foreach (var item in _purchaseRawMaterialCarts)
			await StockData.InsertStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				Type = StockType.Purchase.ToString(),
				BillId = _purchase.Id,
				TransactionDate = _purchase.BillDate,
				LocationId = _user.LocationId
			});
	}

	private async Task InsertPurchaseDetail()
	{
		if (PurchaseId.HasValue && PurchaseId.Value > 0)
		{
			var existingPurchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(PurchaseId.Value);

			if (existingPurchaseDetails is not null)
				foreach (var item in existingPurchaseDetails)
				{
					item.Status = false;
					await PurchaseData.InsertPurchaseDetail(item);
				}
		}

		foreach (var item in _purchaseRawMaterialCarts)
			await PurchaseData.InsertPurchaseDetail(new()
			{
				Id = 0,
				PurchaseId = _purchase.Id,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				Total = item.Total,
				Status = true
			});
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo("/Inventory/Purchase", forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
	#endregion
}