using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenProductionPage
{
	private bool _isLoading = true;

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private readonly List<KitchenProductionProductCartModel> _cart = [];

	private int _selectedCategoryId = 0;
	private KitchenProductionModel _kitchenProduction = new();
	private KitchenProductionProductCartModel _selectedProductForEdit;

	// Grid Reference
	private SfGrid<KitchenProductionProductCartModel> _sfGrid;

	// Dialog References and Visibility
	private SfDialog _sfProductDetailsDialog;

	private bool _productDetailsDialogVisible = false;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.Product);
		_productCategories.Add(new() { Id = 0, Name = "All Categories" });
		_productCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;

		_cart.Clear();

		_products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		foreach (var item in _products)
			_cart.Add(new()
			{
				ProductCategoryId = item.ProductCategoryId,
				ProductId = item.Id,
				ProductName = item.Name,
				Quantity = 0,
				Rate = item.Rate,
				Total = 0
			});

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<KitchenProductionProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionCartDataFileName)) ?? [];
			foreach (var item in items)
			{
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Quantity = item.Quantity;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Rate = item.Rate;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Total = item.Total;
			}
		}
	}

	private async Task OnProductCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;
		else
			_selectedCategoryId = args.Value;

		await _sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Product
	private async Task AddToCart(KitchenProductionProductCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveKitchenProductionFile();
	}

	private async Task UpdateQuantity(KitchenProductionProductCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveKitchenProductionFile();
	}

	private async Task OpenProductDetailsDialog(KitchenProductionProductCartModel item)
	{
		_selectedProductForEdit = item;
		_productDetailsDialogVisible = true;
		await Task.CompletedTask;
	}

	private async Task OnBasicQuantityChanged(decimal quantity)
	{
		if (_selectedProductForEdit != null)
			_selectedProductForEdit.Quantity = quantity;

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

		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task GoToCart()
	{
		await SaveKitchenProductionFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName) == false)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo("/Inventory/Kitchen-Production/Cart");
	}
	#endregion
}