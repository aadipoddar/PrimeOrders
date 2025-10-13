using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase;

public partial class PurchasePage
{
	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedSupplierId = 0;
	private int _selectedCategoryId = 0;

	private bool _supplierDetailsDialogVisible = false;
	private bool _rawMaterialDetailsDialogVisible = false;
	private bool _basicInfoDialogVisible = false;
	private bool _discountDetailsDialogVisible = false;
	private bool _taxDetailsDialogVisible = false;

	private LedgerModel? _selectedSupplier;
	private PurchaseRawMaterialCartModel? _selectedRawMaterialForEdit;

	private PurchaseModel _purchase = new()
	{
		Id = 0,
		BillNo = "",
		BillDateTime = DateTime.Now,
		CDPercent = 0,
		RoundOff = 0,
		Remarks = "",
		CreatedAt = DateTime.Now,
		Status = true,
	};

	private List<LedgerModel> _suppliers = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private readonly List<PurchaseRawMaterialCartModel> _cart = [];
	private readonly List<PurchaseRawMaterialCartModel> _allCart = [];

	private SfGrid<PurchaseRawMaterialCartModel> _sfGrid;

	private SfDialog _sfSupplierDetailsDialog;
	private SfDialog _sfRawMaterialDetailsDialog;
	private SfDialog _sfBasicInfoDialog;
	private SfDialog _sfDiscountDetailsDialog;
	private SfDialog _sfTaxDetailsDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_purchase.UserId = _user.Id;

		_suppliers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);

		await LoadPurchase();
		await LoadRawMaterialCategories();
		await LoadRawMaterials();
		await LoadExistingCart();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task LoadPurchase()
	{
		if (await DataStorageService.LocalExists(StorageFileNames.PurchaseDataFileName))
			_purchase = System.Text.Json.JsonSerializer.Deserialize<PurchaseModel>(
				await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseDataFileName)) ??
				new()
				{
					Id = 0,
					BillDateTime = DateTime.Now,
					CDPercent = 0,
					RoundOff = 0,
					Remarks = "",
					CreatedAt = DateTime.Now,
					Status = true,
				};

		if (_purchase.SupplierId > 0)
		{
			_selectedSupplier = _suppliers.FirstOrDefault(s => s.Id == _purchase.SupplierId);
			_selectedSupplierId = _selectedSupplier.Id;
		}
		else
		{
			_selectedSupplier = _suppliers.FirstOrDefault();
			_purchase.SupplierId = _selectedSupplier.Id;
			_selectedSupplierId = _selectedSupplier.Id;
		}

		_purchase.UserId = _user.Id;
	}

	private async Task LoadRawMaterialCategories()
	{
		_rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_rawMaterialCategories.Add(new() { Id = 0, Name = "All Categories" });
		_rawMaterialCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;
	}

	private async Task LoadRawMaterials()
	{
		_allCart.Clear();

		if (_selectedSupplier?.Id > 0)
			_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(_selectedSupplier.Id, _purchase.BillDateTime);
		else
			_rawMaterials = await CommonData.LoadTableDataByStatus<RawMaterialModel>(TableNames.RawMaterial);

		var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

		foreach (var rawMaterial in _rawMaterials)
		{
			var rawMaterialTax = taxes.FirstOrDefault(t => t.Id == rawMaterial.TaxId);

			_allCart.Add(new()
			{
				RawMaterialId = rawMaterial.Id,
				RawMaterialName = rawMaterial.Name,
				RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
				Rate = rawMaterial.MRP,
				MeasurementUnit = rawMaterial.MeasurementUnit,
				Quantity = 0,
				BaseTotal = 0,
				DiscPercent = _purchase.CDPercent,
				DiscAmount = 0,
				AfterDiscount = 0,
				CGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.CGST : 0,
				CGSTAmount = 0,
				SGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.SGST : 0,
				SGSTAmount = 0,
				IGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.IGST : 0,
				IGSTAmount = 0,
				Total = 0,
				NetRate = 0
			});
		}

		ResetCart();
	}

	private async Task LoadExistingCart()
	{
		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.PurchaseCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<PurchaseRawMaterialCartModel>>(
				await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseCartDataFileName)) ?? [];
			foreach (var item in items)
			{
				var cartItem = _cart.FirstOrDefault(p => p.RawMaterialId == item.RawMaterialId);
				if (cartItem is not null)
				{
					cartItem.Rate = item.Rate;
					cartItem.Quantity = item.Quantity;
					cartItem.MeasurementUnit = item.MeasurementUnit;
					cartItem.DiscPercent = item.DiscPercent;
					cartItem.CGSTPercent = item.CGSTPercent;
					cartItem.SGSTPercent = item.SGSTPercent;
					cartItem.IGSTPercent = item.IGSTPercent;
				}
			}
		}

		UpdateFinancialDetails();
	}
	#endregion

	#region Supplier
	private async Task OnSupplierChanged(ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		_selectedSupplier = args.Value;

		if (args.ItemData is not null && args.ItemData.Id > 0)
			_purchase.SupplierId = args.ItemData.Id;
		else
		{
			_purchase.SupplierId = 0;
			_selectedSupplier = null;
		}

		await LoadRawMaterials();
		await SavePurchaseFile();
		StateHasChanged();
	}
	#endregion

	#region Category
	private async Task OnRawMaterialCategoryChanged(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;
		else
			_selectedCategoryId = args.Value;

		await _sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Cart
	private void ResetCart()
	{
		_cart.Clear();

		foreach (var item in _allCart)
			_cart.Add(new()
			{
				RawMaterialId = item.RawMaterialId,
				RawMaterialName = item.RawMaterialName,
				RawMaterialCategoryId = item.RawMaterialCategoryId,
				Rate = item.Rate,
				MeasurementUnit = item.MeasurementUnit,
				Quantity = item.Quantity,
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
				NetRate = item.NetRate
			});

		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));
	}

	private async Task AddToCart(PurchaseRawMaterialCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SavePurchaseFile();
	}

	private async Task UpdateQuantity(PurchaseRawMaterialCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SavePurchaseFile();
	}
	#endregion

	#region Raw Material Details Dialog
	private void OpenRawMaterialDetailsDialog(PurchaseRawMaterialCartModel item)
	{
		if (item is null || item.Quantity <= 0)
			return;

		_selectedRawMaterialForEdit = item;
		_rawMaterialDetailsDialogVisible = true;
		StateHasChanged();
	}

	private void OpenBasicDetails()
	{
		_rawMaterialDetailsDialogVisible = false;
		_basicInfoDialogVisible = true;
		VibrationService.VibrateHapticClick();
		StateHasChanged();
	}

	private void OpenDiscountDetails()
	{
		_rawMaterialDetailsDialogVisible = false;
		_discountDetailsDialogVisible = true;
		VibrationService.VibrateHapticClick();
		StateHasChanged();
	}

	private void OpenTaxDetails()
	{
		_rawMaterialDetailsDialogVisible = false;
		_taxDetailsDialogVisible = true;
		VibrationService.VibrateHapticClick();
		StateHasChanged();
	}
	#endregion

	#region Basic Info Dialog
	private async Task OnBasicRateChanged(decimal newRate)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.Rate = Math.Max(0, newRate);
		await SavePurchaseFile();
	}

	private async Task OnBasicQuantityChanged(decimal newQuantity)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.Quantity = Math.Max(0, newQuantity);
		await SavePurchaseFile();
	}

	private async Task OnMeasurementUnitChanged(string newUnit)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.MeasurementUnit = newUnit;
		await SavePurchaseFile();
	}

	private async Task OnSaveBasicInfoClick()
	{
		_basicInfoDialogVisible = false;
		await SavePurchaseFile();
	}
	#endregion

	#region Discount Details Dialog
	private async Task OnDiscountPercentChanged(decimal newDiscountPercent)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.DiscPercent = Math.Max(0, Math.Min(100, newDiscountPercent));
		await SavePurchaseFile();
	}

	private async Task OnSaveDiscountClick()
	{
		_discountDetailsDialogVisible = false;
		await SavePurchaseFile();
	}
	#endregion

	#region Tax Details Dialog
	private async Task OnCGSTPercentChanged(decimal newCGSTPercent)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.CGSTPercent = Math.Max(0, Math.Min(50, newCGSTPercent));
		await SavePurchaseFile();
	}

	private async Task OnSGSTPercentChanged(decimal newSGSTPercent)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.SGSTPercent = Math.Max(0, Math.Min(50, newSGSTPercent));
		await SavePurchaseFile();
	}

	private async Task OnIGSTPercentChanged(decimal newIGSTPercent)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.IGSTPercent = Math.Max(0, Math.Min(50, newIGSTPercent));
		await SavePurchaseFile();
	}

	private async Task OnSaveTaxClick()
	{
		_taxDetailsDialogVisible = false;
		await SavePurchaseFile();
	}
	#endregion

	#region Saving
	private void UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = item.Quantity == 0 ? 0 : (item.AfterDiscount - (item.AfterDiscount * (_purchase.CDPercent / 100))
				+ item.CGSTAmount + item.SGSTAmount + item.IGSTAmount) / item.Quantity;
		}

		var totalBeforeCashDiscount = _cart.Sum(x => x.Total);
		var cashDiscountAmount = totalBeforeCashDiscount * (_purchase.CDPercent / 100);
		_purchase.RoundOff = Math.Round(totalBeforeCashDiscount - cashDiscountAmount) - (totalBeforeCashDiscount - cashDiscountAmount);

		_sfGrid?.Refresh();
		StateHasChanged();
	}

	private async Task SavePurchaseFile()
	{
		UpdateFinancialDetails();

		await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseDataFileName, System.Text.Json.JsonSerializer.Serialize(_purchase));

		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.PurchaseCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.PurchaseCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();

		if (_sfGrid is not null)
			await _sfGrid?.Refresh();
		StateHasChanged();
	}

	private async Task GoToCart()
	{
		await SavePurchaseFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || await DataStorageService.LocalExists(StorageFileNames.PurchaseCartDataFileName) == false)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();
		NavigationManager.NavigateTo("/Inventory/Purchase/Cart");
	}
	#endregion
}