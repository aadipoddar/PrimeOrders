using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Items;

public partial class ProductPage
{
	private UserModel _userModel;
	private bool _isLoading = true;
	private bool _isSubmitting = false;
	private string _successMessage = "";
	private string _errorMessage = "";

	private ProductModel _productModel = new()
	{
		Name = "",
		Code = "",
		Rate = 0,
		TaxId = 0,
		ProductCategoryId = 0,
		LocationId = 1,
		Status = true
	};

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<TaxModel> _taxTypes = [];

	private List<ProductRateModel> _productRates = [];
	private int _selectedProductId = 0;
	private string _selectedProductName = "";
	private int _newLocationRateId = 0;
	private decimal _newLocationRate = 0;
	private List<LocationModel> _availableLocationsForRates = [];

	private SfGrid<ProductModel> _sfGrid;
	private SfGrid<ProductRateModel> _sfRatesGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		_userModel = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
		_productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		_taxTypes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

		// Auto-generate product code
		_productModel.Code = GenerateCodes.GenerateProductCode(_products.OrderBy(r => r.Code).LastOrDefault()?.Code);
		_productModel.LocationId = _userModel.LocationId;

		// Location-based filtering - non-location-1 users can only see their own location's products
		if (_userModel.LocationId != 1)
		{
			_products = [.. _products.Where(p => p.LocationId == _userModel.LocationId)];
			_productCategories = [.. _productCategories.Where(c => c.LocationId == _userModel.LocationId || c.LocationId == 1)];
		}

		// Set default values
		if (_productCategories.Count > 0)
			_productModel.ProductCategoryId = _productCategories[0].Id;

		if (_taxTypes.Count > 0)
			_productModel.TaxId = _taxTypes[0].Id;

		StateHasChanged();
	}

	private async Task OnProductFormSubmit()
	{
		// Ensure non-location-1 users can only assign their own location
		if (_userModel.LocationId != 1)
		{
			_productModel.LocationId = _userModel.LocationId;
		}

		if (!await ValidateProductForm())
			return;

		_isSubmitting = true;
		StateHasChanged();

		try
		{
			await ProductData.InsertProduct(_productModel);

			if (_productModel.Id == 0)
				_successMessage = "Product added successfully!";
			else
				_successMessage = "Product updated successfully!";

			await _sfToast.ShowAsync();
			await LoadData();
			ResetProductForm();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error saving product: {ex.Message}";
			await _sfErrorToast.ShowAsync();
			Console.WriteLine(ex.Message);
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateProductForm()
	{
		if (string.IsNullOrWhiteSpace(_productModel.Code))
		{
			await ShowErrorToast("Product code is required.");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_productModel.Name))
		{
			await ShowErrorToast("Product name is required.");
			return false;
		}

		if (_productModel.Rate <= 0)
		{
			await ShowErrorToast("Product rate must be greater than zero.");
			return false;
		}

		if (_productModel.ProductCategoryId <= 0)
		{
			await ShowErrorToast("Please select a product category.");
			return false;
		}

		if (_productModel.TaxId <= 0)
		{
			await ShowErrorToast("Please select a tax type.");
			return false;
		}

		if (_productModel.LocationId <= 0)
		{
			await ShowErrorToast("Please select a location.");
			return false;
		}

		// Non-location-1 users can only manage their own location's products
		if (_userModel.LocationId != 1 && _productModel.LocationId != _userModel.LocationId)
		{
			await ShowErrorToast("You can only manage products for your own location.");
			return false;
		}

		// Check for duplicate code
		if (_products.Any(p => p.Code.Equals(_productModel.Code, StringComparison.OrdinalIgnoreCase) && p.Id != _productModel.Id))
		{
			await ShowErrorToast("Product code already exists.");
			return false;
		}

		return true;
	}

	private void ResetProductForm()
	{
		_productModel = new()
		{
			Name = "",
			Code = GenerateCodes.GenerateProductCode(_products.OrderBy(r => r.Code).LastOrDefault()?.Code),
			Rate = 0,
			TaxId = _taxTypes.Count > 0 ? _taxTypes[0].Id : 0,
			ProductCategoryId = _productCategories.Count > 0 ? _productCategories[0].Id : 0,
			LocationId = _userModel.LocationId,
			Status = true
		};

		// Clear location rates data
		_selectedProductId = 0;
		_selectedProductName = "";
		_productRates.Clear();
		_availableLocationsForRates.Clear();
		_newLocationRateId = 0;
		_newLocationRate = 0;
	}

	// Product Rate Management
	private async Task LoadProductRates(int productId, string productName)
	{
		try
		{
			_selectedProductId = productId;
			_selectedProductName = productName;
			_productRates = await ProductData.LoadProductRateByProduct(productId);

			// Filter rates for non-location-1 users
			if (_userModel.LocationId != 1)
			{
				_productRates = [.. _productRates.Where(r => r.LocationId == _userModel.LocationId)];
			}

			// Update available locations for rates dropdown
			UpdateAvailableLocationsForRates();

			StateHasChanged();
		}
		catch (Exception ex)
		{
			await _sfErrorToast.ShowAsync();
			Console.WriteLine(ex.Message);
		}
	}

	private void UpdateAvailableLocationsForRates()
	{
		// Get locations that don't already have active rates for the selected product
		var existingLocationIds = _productRates.Where(r => r.Status).Select(r => r.LocationId).ToHashSet();

		_availableLocationsForRates = [.. _locations.Where(location => !existingLocationIds.Contains(location.Id))];

		// Reset the selected location if it's no longer available
		if (_newLocationRateId > 0 && !_availableLocationsForRates.Any(l => l.Id == _newLocationRateId))
		{
			_newLocationRateId = 0;
		}
	}

	private async Task AddLocationRate()
	{
		if (_newLocationRateId <= 0)
		{
			await ShowErrorToast("Please select a location.");
			return;
		}

		if (_newLocationRate <= 0)
		{
			await ShowErrorToast("Rate must be greater than zero.");
			return;
		}

		// Check if rate already exists for this location (safety check)
		if (_productRates.Any(r => r.LocationId == _newLocationRateId && r.Status))
		{
			await ShowErrorToast("Rate already exists for this location.");
			return;
		}

		try
		{
			var productRate = new ProductRateModel
			{
				ProductId = _selectedProductId,
				LocationId = _newLocationRateId,
				Rate = _newLocationRate,
				Status = true
			};

			await ProductData.InsertProductRate(productRate);
			_successMessage = "Location rate added successfully!";
			await _sfToast.ShowAsync();

			// Reload rates
			await LoadProductRates(_selectedProductId, _selectedProductName);

			// Reset form
			_newLocationRateId = 0;
			_newLocationRate = 0;
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error adding location rate: {ex.Message}";
			await _sfErrorToast.ShowAsync();
			Console.WriteLine(ex.Message);
		}
	}

	// Grid Event Handlers
	private async Task OnProductRowSelected(RowSelectEventArgs<ProductModel> args)
	{
		var selectedProduct = args.Data;
		_productModel = new ProductModel
		{
			Id = selectedProduct.Id,
			Code = selectedProduct.Code,
			Name = selectedProduct.Name,
			Rate = selectedProduct.Rate,
			ProductCategoryId = selectedProduct.ProductCategoryId,
			TaxId = selectedProduct.TaxId,
			LocationId = selectedProduct.LocationId,
			Status = selectedProduct.Status
		};

		// Load product rates only for primary location products
		if (selectedProduct.LocationId == 1)
		{
			await LoadProductRates(selectedProduct.Id, selectedProduct.Name);
		}
		else
		{
			// Clear rates data for non-primary location products
			_selectedProductId = selectedProduct.Id;
			_selectedProductName = selectedProduct.Name;
			_productRates.Clear();
			_availableLocationsForRates.Clear();
			_newLocationRateId = 0;
			_newLocationRate = 0;
		}
		StateHasChanged();
	}

	private void OnGridActionBegin(ActionEventArgs<ProductModel> args)
	{
		if (args.RequestType == Syncfusion.Blazor.Grids.Action.Save)
		{
			// Validate the data
			if (string.IsNullOrWhiteSpace(args.Data.Code))
			{
				args.Cancel = true;
				_ = ShowErrorToast("Product code is required.");
				return;
			}

			if (string.IsNullOrWhiteSpace(args.Data.Name))
			{
				args.Cancel = true;
				_ = ShowErrorToast("Product name is required.");
				return;
			}

			if (args.Data.Rate <= 0)
			{
				args.Cancel = true;
				_ = ShowErrorToast("Product rate must be greater than zero.");
				return;
			}

			// Check for duplicate code
			if (_products.Any(p => p.Code.Equals(args.Data.Code, StringComparison.OrdinalIgnoreCase) && p.Id != args.Data.Id))
			{
				args.Cancel = true;
				_ = ShowErrorToast("Product code already exists.");
				return;
			}

			// Location-based permission check for editing
			if (args.Action == "Edit" && _userModel.LocationId != 1 && args.Data.LocationId != _userModel.LocationId)
			{
				args.Cancel = true;
				_ = ShowErrorToast("You can only edit products from your location.");
				return;
			}
		}

		if (args.RequestType == Syncfusion.Blazor.Grids.Action.Delete)
		{
			// Location-based permission check for deleting
			if (_userModel.LocationId != 1 && args.Data.LocationId != _userModel.LocationId)
			{
				args.Cancel = true;
				_ = ShowErrorToast("You can only delete products from your location.");
				return;
			}
		}
	}

	private void OnGridActionComplete(ActionEventArgs<ProductModel> args)
	{
		if (args.RequestType == Syncfusion.Blazor.Grids.Action.Save ||
			args.RequestType == Syncfusion.Blazor.Grids.Action.Delete)
		{
			_successMessage = args.RequestType == Syncfusion.Blazor.Grids.Action.Save ? "Product saved successfully!" : "Product deleted successfully!";
			_ = _sfToast.ShowAsync();
		}
	}

	private async Task RemoveLocationRate(int rateId)
	{
		try
		{
			var productRate = _productRates.FirstOrDefault(r => r.Id == rateId);
			productRate.Status = false;
			await ProductData.InsertProductRate(productRate);
			_successMessage = "Location rate removed successfully!";
			await _sfToast.ShowAsync();

			// Reload rates (this will also update available locations)
			await LoadProductRates(_selectedProductId, _selectedProductName);
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error removing rate: {ex.Message}";
			await _sfErrorToast.ShowAsync();
			Console.WriteLine(ex.Message);
		}
	}

	// Helper Methods
	private async Task ShowErrorToast(string message)
	{
		_errorMessage = message;
		await _sfErrorToast.ShowAsync();
	}

	private string GetLocationName(int locationId)
	{
		var location = _locations.FirstOrDefault(l => l.Id == locationId);
		return location?.Name ?? "Unknown";
	}

	private string GetCategoryName(int categoryId)
	{
		var category = _productCategories.FirstOrDefault(c => c.Id == categoryId);
		return category?.Name ?? "Unknown";
	}

	private string GetTaxCode(int taxId)
	{
		var tax = _taxTypes.FirstOrDefault(t => t.Id == taxId);
		return tax?.Code ?? "Unknown";
	}

	// Permission Checks
	private bool CanManageRates()
	{
		return _userModel.LocationId == 1; // Only location 1 users can manage rates for all locations
	}

	private bool ShouldShowLocationRates()
	{
		// Only show location rates for products from primary location (LocationId == 1)
		var selectedProduct = _products.FirstOrDefault(p => p.Id == _selectedProductId);
		return selectedProduct != null && selectedProduct.LocationId == 1;
	}

	private bool CanEditProduct(ProductModel product)
	{
		return _userModel.LocationId == 1 || product.LocationId == _userModel.LocationId;
	}

	private bool CanDeleteProduct(ProductModel product)
	{
		return _userModel.LocationId == 1 || product.LocationId == _userModel.LocationId;
	}
}