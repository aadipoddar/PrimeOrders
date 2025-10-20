using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Exporting.Purchase;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase;

public partial class PurchaseCartPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isSaving = false;
	private bool _customRoundOff = false;

	private decimal _baseTotal = 0;
	private decimal _discountAmount = 0;
	private decimal _subTotal = 0;
	private decimal _afterTax = 0;
	private decimal _cashDiscountAmount = 0;
	private decimal _total = 0;

	private bool _purchaseDetailsDialogVisible = false;
	private bool _purchaseConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;
	private bool _discountDialogVisible = false;
	private bool _rawMaterialDetailsDialogVisible = false;

	private readonly List<PurchaseRawMaterialCartModel> _cart = [];
	private readonly List<ValidationError> _validationErrors = [];
	private PurchaseRawMaterialCartModel _selectedRawMaterialForEdit;

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

	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<PurchaseRawMaterialCartModel> _sfGrid;
	private SfDialog _sfPurchaseDetailsDialog;
	private SfDialog _sfPurchaseConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;
	private SfDialog _sfDiscountDialog;
	private SfDialog _sfRawMaterialDetailsDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadPurchase();
		await LoadCart();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task LoadPurchase()
	{
		_purchase = System.Text.Json.JsonSerializer.Deserialize<PurchaseModel>(
			await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseDataFileName));

		_purchase.UserId = _user.Id;
	}

	private async Task LoadCart()
	{
		_cart.Clear();
		if (await DataStorageService.LocalExists(StorageFileNames.PurchaseCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<PurchaseRawMaterialCartModel>>(
				await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		UpdateFinancialDetails();
	}
	#endregion

	#region Raw Materials
	private async Task UpdateQuantity(PurchaseRawMaterialCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SavePurchaseFile();
	}

	private async Task UpdateRate(PurchaseRawMaterialCartModel item, decimal newRate)
	{
		if (item is null || _isSaving)
			return;

		item.Rate = Math.Max(0, newRate);
		await SavePurchaseFile();
	}

	private async Task UpdateUnit(PurchaseRawMaterialCartModel item, string newUnit)
	{
		if (item is null || _isSaving || string.IsNullOrWhiteSpace(newUnit))
			return;

		item.MeasurementUnit = newUnit.Trim();
		await SavePurchaseFile();
	}

	private async Task RemoveFromCart(PurchaseRawMaterialCartModel item)
	{
		if (item is null || _isSaving)
			return;

		_cart.Remove(item);
		await SavePurchaseFile();
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();
		VibrationService.VibrateWithTime(500);
		await SavePurchaseFile();
	}

	private async Task ApplyDiscount()
	{
		_discountDialogVisible = false;
		await SavePurchaseFile();
	}

	private async Task OnRoundOffChanged(decimal newRoundOff)
	{
		if (_isSaving)
			return;

		_purchase.RoundOff = newRoundOff;
		_customRoundOff = true;
		await SavePurchaseFile();
	}

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
			item.NetRate = item.Quantity == 0 ? 0 : item.Total / item.Quantity * (1 - (_purchase.CDPercent / 100));
		}

		_baseTotal = _cart.Sum(c => c.BaseTotal);
		_subTotal = _cart.Sum(c => c.AfterDiscount);
		_discountAmount = _baseTotal - _subTotal;
		_afterTax = _cart.Sum(c => c.Total);
		_cashDiscountAmount = _afterTax * (_purchase.CDPercent / 100);

		if (!_customRoundOff)
			_purchase.RoundOff = Math.Round(_afterTax - _cashDiscountAmount) - (_afterTax - _cashDiscountAmount);

		_total = _afterTax - _cashDiscountAmount + _purchase.RoundOff;

		_sfGrid?.Refresh();
		StateHasChanged();
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

	private async Task OnRateChanged(decimal newRate)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.Rate = Math.Max(0, newRate);
		await SavePurchaseFile();
	}

	private async Task OnQuantityChanged(decimal newQuantity)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.Quantity = Math.Max(0, newQuantity);
		await SavePurchaseFile();
	}

	private async Task OnDiscountPercentChanged(decimal newDiscountPercent)
	{
		if (_selectedRawMaterialForEdit is null)
			return;

		_selectedRawMaterialForEdit.DiscPercent = Math.Max(0, Math.Min(100, newDiscountPercent));
		await SavePurchaseFile();
	}

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

	private async Task OnSaveRawMaterialDetailsClick()
	{
		_rawMaterialDetailsDialogVisible = false;
		await SavePurchaseFile();
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

	#endregion

	#region Saving
	private async Task CloseConfirmPurchaseDialog()
	{
		if (_isSaving)
			return;

		_purchaseConfirmationDialogVisible = false;
		_customRoundOff = false;
		await SavePurchaseFile();
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
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		await SavePurchaseFile();

		_validationErrors.Clear();

		if (!_cart.Any(x => x.Quantity > 0))
		{
			_validationErrors.Add(new()
			{
				Field = "Cart",
				Message = "Cart is empty. Please add raw materials to the cart."
			});
			return false;
		}

		if (_purchase.SupplierId <= 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Supplier",
				Message = "Please select a supplier for the purchase."
			});
			return false;
		}

		if (string.IsNullOrWhiteSpace(_purchase.BillNo))
		{
			_validationErrors.Add(new()
			{
				Field = "Bill Number",
				Message = "Bill number is required."
			});
			return false;
		}

		if (_purchase.CDPercent < 0 || _purchase.CDPercent > 100)
		{
			_validationErrors.Add(new()
			{
				Field = "Cash Discount",
				Message = "Cash discount percent must be between 0 and 100."
			});
			return false;
		}

		return true;
	}

	private async Task SavePurchase()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			if (!await ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_purchaseConfirmationDialogVisible = false;
				return;
			}

			_purchase.Id = await PurchaseData.SavePurchase(_purchase, _cart);
			await PrintPurchaseBill();
			await DeleteCartPurchase();
			await SendLocalNotification();
			NavigationManager.NavigateTo("/Inventory/Purchase/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "Exception",
				Message = $"An error occurred while saving the Purchase: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_purchaseConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task PrintPurchaseBill()
	{
		var memoryStream = await PurchaseA4Print.GenerateA4PurchaseBill(_purchase.Id);
		var fileName = $"Purchase_Bill_{_purchase.BillNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);
	}

	private async Task DeleteCartPurchase()
	{
		_cart.Clear();
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseCartDataFileName);
	}

	private async Task SendLocalNotification()
	{
		var purchase = await PurchaseData.LoadPurchaseOverviewByPurchaseId(_purchase.Id);
		await NotificationService.ShowLocalNotification(
			_purchase.Id,
			"Purchase Placed",
			$"{purchase.BillNo}",
			$"Your purchase #{purchase.BillNo} has been successfully placed | Total Items: {purchase.TotalItems} | Total Qty: {purchase.TotalQuantity} | Supplier: {purchase.SupplierName} | User: {purchase.UserName} | Date: {purchase.BillDateTime:dd/MM/yy hh:mm tt} | Remarks: {purchase.Remarks}");
	}
	#endregion
}