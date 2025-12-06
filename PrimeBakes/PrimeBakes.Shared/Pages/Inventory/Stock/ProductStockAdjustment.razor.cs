using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class ProductStockAdjustment : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DateTime _transactionDateTime = DateTime.Now;
	private string _transactionNo = string.Empty;

	private FinancialYearModel _selectedFinancialYear = new();
	private LocationModel _selectedLocation;
	private ProductLocationOverviewModel? _selectedProduct = new();
	private ProductStockAdjustmentCartModel _selectedCart = new();

	private List<LocationModel> _locations = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<ProductStockAdjustmentCartModel> _cart = [];
	private List<ProductStockSummaryModel> _stockSummary = [];

	private SfAutoComplete<ProductLocationOverviewModel?, ProductLocationOverviewModel> _sfItemAutoComplete;
	private SfGrid<ProductStockAdjustmentCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.Enter, AddItemToCart, "Add item to cart", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, () => _sfItemAutoComplete.FocusAsync(), "Focus on item input", Exclude.None)
			.Add(ModCode.Ctrl, Code.S, SaveTransaction, "Save the transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistoryPage, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);

		_transactionDateTime = await CommonData.LoadCurrentDateTime();
		await LoadLocations();
		await LoadStock();
		await LoadItems();
		await LoadExistingCart();
	}

	private async Task LoadLocations()
	{
		try
		{
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);

			_locations = [.. _locations.OrderBy(s => s.Name)];
			_locations.Insert(0, new()
			{
				Id = 0,
				Name = "Create New Location ..."
			});

			_selectedLocation = _locations.FirstOrDefault(_ => _.Id == 1);
			_transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(_transactionDateTime, _selectedLocation.Id);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Locations", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadStock()
	{
		try
		{
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_transactionDateTime);
			_stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(_transactionDateTime, _transactionDateTime, _selectedLocation.Id);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Stock Data", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_products = await ProductData.LoadProductByLocation(_selectedLocation.Id);

			_products = [.. _products.OrderBy(s => s.Name)];
			_products.Add(new()
			{
				Id = 0,
				Name = "Create New Item ..."
			});
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Items", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (await DataStorageService.LocalExists(StorageFileNames.ProductStockAdjustmentCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<ProductStockAdjustmentCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.ProductStockAdjustmentCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}
	#endregion

	#region Change Events
	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_transactionDateTime = args.Value;
		await LoadStock();
		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnLocationChanged(ChangeEventArgs<LocationModel, LocationModel> args)
	{
		args.Value ??= _locations.FirstOrDefault(_ => _.Id == 1);

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLocation, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminLocation);
			return;
		}

		_selectedLocation = args.Value;
		await LoadStock();
		await LoadItems();
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<ProductLocationOverviewModel?, ProductLocationOverviewModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminProduct, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminProduct);

			return;
		}

		_selectedProduct = args.Value;

		if (_selectedProduct is null)
			_selectedCart = new()
			{
				ProductId = 0,
				ProductName = "",
				Stock = 0,
				Quantity = 1,
				Total = 0,
				Rate = 0,
			};

		else
		{
			_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.ProductId == _selectedProduct.ProductId)?.ClosingStock ?? 0;
			_selectedCart.Quantity = _stockSummary.FirstOrDefault(s => s.ProductId == _selectedProduct.ProductId)?.ClosingStock ?? 0;
			_selectedCart.Rate = _selectedProduct.Rate;
			_selectedCart.Total = _selectedCart.Rate * _selectedCart.Quantity;
		}

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Quantity = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedProduct is null)
			return;

		_selectedCart.ProductId = _selectedProduct.ProductId;
		_selectedCart.ProductName = _selectedProduct.Name;
		_selectedCart.Rate = _selectedProduct.Rate;
		_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.ProductId == _selectedProduct.ProductId)?.ClosingStock ?? 0;
		_selectedCart.Total = _selectedCart.Quantity * _selectedCart.Rate;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ProductId == _selectedCart.ProductId);
		if (existingItem is not null)
		{
			existingItem.Quantity = _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
		}
		else
			_cart.Add(new()
			{
				ProductId = _selectedCart.ProductId,
				ProductName = _selectedCart.ProductName,
				Stock = _selectedCart.Stock,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				Total = _selectedCart.Total
			});

		_selectedProduct = null;
		_selectedCart = new();

		await _sfItemAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(ProductStockAdjustmentCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ProductId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ProductId = cartItem.ProductId,
			ProductName = cartItem.ProductName,
			Stock = _stockSummary.FirstOrDefault(s => s.ProductId == cartItem.ProductId)?.ClosingStock ?? 0,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Total = cartItem.Total
		};

		await _sfItemAutoComplete.FocusAsync();
		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(ProductStockAdjustmentCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.Stock = _stockSummary.FirstOrDefault(s => s.ProductId == item.ProductId)?.ClosingStock ?? 0;
			item.Quantity = item.Quantity;
			item.Total = item.Rate * item.Quantity;
		}

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_transactionDateTime);
		if (_selectedFinancialYear is null || _selectedFinancialYear.Locked || _selectedFinancialYear.Status == false)
		{
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
			_transactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_transactionDateTime);
			_stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(_transactionDateTime, _transactionDateTime, _selectedLocation.Id);
		}
		#endregion

		_transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(_transactionDateTime, _selectedLocation.Id);
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			await DataStorageService.LocalSaveAsync(StorageFileNames.ProductStockAdjustmentCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (_cart.Count == 0)
		{
			await _toastNotification.ShowAsync("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_transactionNo))
		{
			await _toastNotification.ShowAsync("Transaction Number Missing", "Transaction number is missing for the adjustment.", ToastType.Warning);
			return false;
		}

		if (_transactionDateTime == default)
		{
			await _toastNotification.ShowAsync("Transaction Date Missing", "Please select a valid transaction date for the adjustment.", ToastType.Warning);
			return false;
		}

		if (_selectedFinancialYear is null || _selectedFinancialYear.Id <= 0)
		{
			await _toastNotification.ShowAsync("Financial Year Not Found", "The transaction date does not fall within any financial year. Please check the date and try again.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await _toastNotification.ShowAsync("Financial Year Locked", "The financial year for the selected transaction date is locked. Please select a different date.", ToastType.Error);
			return false;
		}

		if (_selectedFinancialYear.Status == false)
		{
			await _toastNotification.ShowAsync("Financial Year Inactive", "The financial year for the selected transaction date is inactive. Please select a different date.", ToastType.Error);
			return false;
		}

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			await SaveTransactionFile();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await ProductStockData.SaveProductStockAdjustment(_transactionDateTime, _selectedLocation.Id, _cart);
			await DeleteLocalFiles();
			NavigationManager.NavigateTo(PageRouteNames.ProductStockAdjustment, true);

			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully!", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DeleteLocalFiles() =>
		await DataStorageService.LocalRemove(StorageFileNames.ProductStockAdjustmentCartDataFileName);
	#endregion

	#region Utilities
	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(PageRouteNames.ProductStockAdjustment, true);
	}

	private async Task NavigateToTransactionHistoryPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportProductStock, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportProductStock);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}