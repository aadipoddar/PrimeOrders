using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenIssuePage
{
	private bool _isLoading = true;

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private readonly List<KitchenIssueRawMaterialCartModel> _cart = [];

	private int _selectedCategoryId = 0;
	private KitchenIssueModel _kitchenIssue = new();
	private KitchenIssueRawMaterialCartModel _selectedRawMaterialForEdit;

	// Grid Reference
	private SfGrid<KitchenIssueRawMaterialCartModel> _sfGrid;

	// Dialog References and Visibility
	private SfDialog _sfRawMaterialDetailsDialog;

	private bool _rawMaterialDetailsDialogVisible = false;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory);
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_rawMaterialCategories.Add(new() { Id = 0, Name = "All Categories" });
		_rawMaterialCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;

		_cart.Clear();

		_rawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(0, DateTime.Now);
		foreach (var item in _rawMaterials)
			_cart.Add(new()
			{
				RawMaterialId = item.Id,
				RawMaterialName = item.Name,
				RawMaterialCategoryId = item.RawMaterialCategoryId,
				MeasurementUnit = item.MeasurementUnit,
				Quantity = 0,
				Rate = item.MRP,
				Total = 0
			});

		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<KitchenIssueRawMaterialCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueCartDataFileName)) ?? [];
			foreach (var item in items)
			{
				_cart.Where(p => p.RawMaterialId == item.RawMaterialId).FirstOrDefault().Quantity = item.Quantity;
				_cart.Where(p => p.RawMaterialId == item.RawMaterialId).FirstOrDefault().Rate = item.Rate;
				_cart.Where(p => p.RawMaterialId == item.RawMaterialId).FirstOrDefault().Total = item.Total;
				_cart.Where(p => p.RawMaterialId == item.RawMaterialId).FirstOrDefault().MeasurementUnit = item.MeasurementUnit;
			}
		}
	}

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

	#region Raw Material
	private async Task AddToCart(KitchenIssueRawMaterialCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveKitchenIssueFile();
	}

	private async Task UpdateQuantity(KitchenIssueRawMaterialCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveKitchenIssueFile();
	}

	private async Task OpenRawMaterialDetailsDialog(KitchenIssueRawMaterialCartModel item)
	{
		_selectedRawMaterialForEdit = item;
		_rawMaterialDetailsDialogVisible = true;
		await Task.CompletedTask;
	}

	private async Task OnBasicQuantityChanged(decimal quantity)
	{
		if (_selectedRawMaterialForEdit != null)
			_selectedRawMaterialForEdit.Quantity = quantity;

		await SaveKitchenIssueFile();
	}

	private async Task OnMeasurementUnitChanged(string unit)
	{
		if (_selectedRawMaterialForEdit is not null)
			_selectedRawMaterialForEdit.MeasurementUnit = unit;

		await SaveKitchenIssueFile();
	}

	private async Task OnRateChanged(decimal rate)
	{
		if (_selectedRawMaterialForEdit is not null)
			_selectedRawMaterialForEdit.Rate = Math.Max(0, rate);

		await SaveKitchenIssueFile();
	}

	private async Task OnSaveBasicInfoClick()
	{
		_rawMaterialDetailsDialogVisible = false;
		await SaveKitchenIssueFile();
	}
	#endregion

	#region Saving
	private async Task SaveKitchenIssueFile()
	{
		foreach (var item in _cart)
			item.Total = item.Quantity * item.Rate;

		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task GoToCart()
	{
		await SaveKitchenIssueFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName) == false)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo("/Inventory/Kitchen-Issue/Cart");
	}
	#endregion
}