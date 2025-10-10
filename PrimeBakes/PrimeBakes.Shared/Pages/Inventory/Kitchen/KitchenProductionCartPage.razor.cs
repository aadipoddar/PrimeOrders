using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenProductionCartPage
{
	private bool _isLoading = true;
	private bool _isSaving = false;

	private UserModel _user;

	private List<KitchenProductionProductCartModel> _cart = [];
	private List<KitchenModel> _kitchens = [];

	private KitchenProductionModel _kitchenProduction = new()
	{
		Id = 0,
		TransactionNo = "",
		KitchenId = 0,
		UserId = 0,
		ProductionDate = DateTime.Now,
		CreatedAt = DateTime.Now,
		LocationId = 1,
		Status = true,
		Remarks = ""
	};
	private KitchenProductionProductCartModel _selectedProductForEdit;

	// Grid Reference
	private SfGrid<KitchenProductionProductCartModel> _sfGrid;

	// Dialog References and Visibility
	private SfDialog _sfProductionDetailsDialog;
	private SfDialog _sfProductionConfirmationDialog;
	private SfDialog _sfProductDetailsDialog;

	private bool _productionDetailsDialogVisible = false;
	private bool _productionConfirmationDialogVisible = false;
	private bool _productDetailsDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private readonly List<ValidationError> _validationErrors = [];
	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

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
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);

		_kitchenProduction.UserId = _user.Id;
		_kitchenProduction.KitchenId = _kitchens.FirstOrDefault()?.Id ?? 0;
		_kitchenProduction.ProductionDate = DateTime.Now;
		_kitchenProduction.LocationId = 1;
		_kitchenProduction.TransactionNo = await GenerateCodes.GenerateKitchenProductionTransactionNo(_kitchenProduction);
		_kitchenProduction.CreatedAt = DateTime.Now;

		_cart.Clear();
		if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<KitchenProductionProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionDataFileName))
			_kitchenProduction = System.Text.Json.JsonSerializer.Deserialize<KitchenProductionModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionDataFileName)) ?? new()
			{
				Id = 0,
				TransactionNo = "",
				KitchenId = 0,
				UserId = 0,
				ProductionDate = DateTime.Now,
				CreatedAt = DateTime.Now,
				LocationId = 1,
				Status = true,
				Remarks = ""
			};

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Raw Material
	private async Task UpdateQuantity(KitchenProductionProductCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveKitchenProductionFile();
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();
		VibrationService.VibrateWithTime(500);
		await SaveKitchenProductionFile();
	}

	private void OpenProductDetailsDialog(KitchenProductionProductCartModel item)
	{
		_selectedProductForEdit = item;
		_productDetailsDialogVisible = true;
	}

	private async Task OnBasicQuantityChanged(decimal quantity)
	{
		if (_selectedProductForEdit is not null)
			_selectedProductForEdit.Quantity = Math.Max(0, quantity);

		await SaveKitchenProductionFile();
	}

	private async Task OnRateChanged(decimal rate)
	{
		if (_selectedProductForEdit is not null)
			_selectedProductForEdit.Rate = Math.Max(0, rate);

		await SaveKitchenProductionFile();
	}

	private async Task OnSaveBasicInfoClick()
	{
		_productDetailsDialogVisible = false;
		await SaveKitchenProductionFile();
	}
	#endregion

	#region Saving
	private async Task SaveKitchenProductionFile()
	{
		foreach (var item in _cart)
			item.Total = item.Quantity * item.Rate;

		await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionDataFileName, System.Text.Json.JsonSerializer.Serialize(_kitchenProduction));

		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private bool ValidateForm()
	{
		_validationErrors.Clear();

		_kitchenProduction.LocationId = 1;
		_kitchenProduction.UserId = _user.Id;

		if (_kitchenProduction.KitchenId <= 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Kitchen",
				Message = "Please select a valid Kitchen for the Production."
			});
			return false;
		}

		if (_cart.Count == 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Cart Items",
				Message = "Please add at least one product to the order."
			});
			return false;
		}

		StateHasChanged();
		return true;
	}

	private async Task SaveKitchenIssue()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveKitchenProductionFile();

			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_productionConfirmationDialogVisible = false;
				return;
			}

			_kitchenProduction.Id = await KitchenProductionData.SaveKitchenProduction(_kitchenProduction, _cart);
			await DeleteCart();
			await PrintInvoice();
			await SendLocalNotification();
			NavigationManager.NavigateTo("/Inventory/Kitchen-Production/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "Exception",
				Message = $"An error occurred while saving the Production: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_productionConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task DeleteCart()
	{
		_cart.Clear();
		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);
	}

	private async Task PrintInvoice()
	{
		var ms = await KitchenProductionA4Print.GenerateA4KitchenProductionBill(_kitchenProduction.Id);
		var fileName = $"Kitchen_Production_{_kitchenProduction.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", ms);
	}

	private async Task SendLocalNotification()
	{
		var kitchenProduction = await KitchenProductionData.LoadKitchenProductionOverviewByKitchenProductionId(_kitchenProduction.Id);
		await NotificationService.ShowLocalNotification(
			kitchenProduction.KitchenId,
			"Kitchen Production Placed",
			$"{kitchenProduction.TransactionNo}",
			$"Your kitchen production #{kitchenProduction.TransactionNo} has been successfully placed | Total Items: {kitchenProduction.TotalProducts} | Total Qty: {kitchenProduction.TotalQuantity} | User: {kitchenProduction.UserName} | Date: {kitchenProduction.ProductionDate:dd/MM/yy hh:mm tt}");
	}
	#endregion
}