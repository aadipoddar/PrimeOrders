using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class ProductStockAdjustment
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private int _selectedLocationId = 1;
	private int _selectedCategoryId = 0;
	private bool _adjustmentConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<LocationModel> _locations = [];
	private List<ProductCategoryModel> _productCategories = [];
	private readonly List<ProductStockAdjustmentCartModel> _cart = [];
	private readonly List<ValidationError> _validationErrors = [];

	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<ProductStockAdjustmentCartModel> _sfGrid;

	private SfDialog _sfAdjustmentConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;

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
		await LoadLocations();
		await LoadCategories();
		await LoadProducts();
		await LoadStock();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		if (_user.LocationId == 1)
			_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);

		_selectedLocationId = _user.LocationId;
	}

	private async Task LoadCategories()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.RemoveAll(c => c.LocationId != _selectedLocationId && c.LocationId != 1);
		_productCategories.Add(new()
		{
			Id = 0,
			Name = "All Categories"
		});
		_productCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;
	}

	private async Task LoadProducts()
	{
		var allProducts = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		allProducts.RemoveAll(c => c.LocationId != _selectedLocationId && c.LocationId != 1);

		foreach (var product in allProducts)
			_cart.Add(new()
			{
				ProductCategoryId = product.ProductCategoryId,
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 0,
				Rate = product.Rate,
				Total = 0
			});

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));
	}

	private async Task LoadStock()
	{
		var stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(
					DateTime.Now.AddDays(-1),
					DateTime.Now.AddDays(1),
					_selectedLocationId);

		foreach (var item in stockSummary)
			if (_cart.Any(c => c.ProductId == item.ProductId))
				_cart.FirstOrDefault(c => c.ProductId == item.ProductId).Quantity = item.ClosingStock;
	}
	#endregion

	#region Dialog Methods
	private void CloseConfirmationDialog()
	{
		_adjustmentConfirmationDialogVisible = false;
		StateHasChanged();
	}
	#endregion

	#region Location and Products
	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		if (args is null || args.Value <= 0)
			return;

		_selectedLocationId = args.Value;
		_cart.Clear();

		// Reload everything based on new location
		await LoadCategories();
		await LoadProducts();
		await LoadStock();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task OnProductCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;
		else
			_selectedCategoryId = args.Value;

		await SaveAdjustmentFile();
	}

	private async Task UpdateQuantity(ProductStockAdjustmentCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = newQuantity;
		await SaveAdjustmentFile();
	}
	#endregion

	#region Saving
	private async Task SaveAdjustmentFile()
	{
		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private bool ValidateForm()
	{
		_validationErrors.Clear();

		// Check for extremely negative quantities that might be unintentional
		var extremeNegativeItems = _cart.Where(x => x.Quantity < -1000).ToList();
		if (extremeNegativeItems.Count != 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Large Negative Quantities",
				Message = $"Some items have very large negative quantities. Please verify: {string.Join(", ", extremeNegativeItems.Select(x => $"{x.ProductName} ({x.Quantity})"))}"
			});
		}

		return _validationErrors.Count == 0;
	}

	private async Task ConfirmAdjustment()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveAdjustmentFile();

			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_adjustmentConfirmationDialogVisible = false;
				return;
			}

			await ProductStockData.SaveProductStockAdjustment(_cart, _selectedLocationId);
			_cart.Clear();
			await SendLocalNotification();
			NavigationManager.NavigateTo("/Inventory/ProductStockAdjustment/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "System Error",
				Message = $"An error occurred while saving the adjustment: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_adjustmentConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task SendLocalNotification()
	{
		await NotificationService.ShowLocalNotification(
			100,
			 "Product Stock Adjustment Saved",
			 "Stock Adjusted.",
			   $"Product Stock Adjustment has been successfully saved on {DateTime.Now:dd/MM/yy hh:mm tt}. Please check the Stock Adjustment report for details.");
	}
	#endregion
}