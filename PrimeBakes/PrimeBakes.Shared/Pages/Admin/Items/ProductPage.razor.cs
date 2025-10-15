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
		Status = true
	};

	private List<ProductModel> _products = [];
	private List<ProductCategoryModel> _productCategories = [];
	private List<LocationModel> _locations = [];
	private List<TaxModel> _taxTypes = [];

	private List<ProductLocationOverviewModel> _productLocations = [];
	private int _selectedProductId = 0;
	private string _selectedProductName = "";
	private int _newLocationRateId = 0;
	private decimal _newLocationRate = 0;
	private List<LocationModel> _availableLocationsForRates = [];

	private SfGrid<ProductModel> _sfGrid;
	private SfGrid<ProductLocationOverviewModel> _sfRatesGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnInitializedAsync()
	{
		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
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

		// Set default values
		if (_productCategories.Count > 0)
			_productModel.ProductCategoryId = _productCategories[0].Id;

		if (_taxTypes.Count > 0)
			_productModel.TaxId = _taxTypes[0].Id;

		StateHasChanged();
	}

	private async Task OnProductFormSubmit()
	{
		if (!await ValidateProductForm())
			return;

		_isSubmitting = true;
		StateHasChanged();

		try
		{
			bool update = _productModel.Id != 0;

			_productModel.Id = await ProductData.InsertProduct(_productModel);

			if (!update)
			{
				if (_productModel.Status)
				{
					var locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
					foreach (var location in locations)
						await ProductData.InsertProductLocation(new()
						{
							Id = 0,
							ProductId = _productModel.Id,
							LocationId = location.Id,
							Rate = _productModel.Rate,
							Status = true
						});
				}

				_successMessage = "Product added successfully!";
			}
			else
			{
				if (!_productModel.Status)
				{
					var productLocations = await ProductData.LoadProductRateByProduct(_productModel.Id);
					foreach (var productLocation in productLocations)
						await ProductData.InsertProductLocation(new()
						{
							Id = productLocation.Id,
							ProductId = productLocation.ProductId,
							LocationId = productLocation.LocationId,
							Rate = productLocation.Rate,
							Status = false
						});
				}

				_successMessage = "Product updated successfully!";
			}

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
			Id = 0,
			Name = "",
			Code = GenerateCodes.GenerateProductCode(_products.OrderBy(r => r.Code).LastOrDefault()?.Code),
			Rate = 0,
			TaxId = _taxTypes.Count > 0 ? _taxTypes[0].Id : 0,
			ProductCategoryId = _productCategories.Count > 0 ? _productCategories[0].Id : 0,
			Status = true
		};

		// Clear location rates data
		_selectedProductId = 0;
		_selectedProductName = "";
		_productLocations.Clear();
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
			_productLocations = await ProductData.LoadProductRateByProduct(productId);

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
		// Get locations that don't already have rates for the selected product
		var existingLocationIds = _productLocations.Select(r => r.LocationId).ToHashSet();

		_availableLocationsForRates = [.. _locations.Where(location => !existingLocationIds.Contains(location.Id))];

		// Reset the selected location if it's no longer available
		if (_newLocationRateId > 0 && !_availableLocationsForRates.Any(l => l.Id == _newLocationRateId))
			_newLocationRateId = 0;
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
		if (_productLocations.Any(r => r.LocationId == _newLocationRateId))
		{
			await ShowErrorToast("Rate already exists for this location.");
			return;
		}

		try
		{
			await ProductData.InsertProductLocation(new()
			{
				Id = 0,
				ProductId = _selectedProductId,
				LocationId = _newLocationRateId,
				Rate = _newLocationRate,
				Status = true
			});
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
		_productModel = new()
		{
			Id = selectedProduct.Id,
			Code = selectedProduct.Code,
			Name = selectedProduct.Name,
			Rate = selectedProduct.Rate,
			ProductCategoryId = selectedProduct.ProductCategoryId,
			TaxId = selectedProduct.TaxId,
			Status = selectedProduct.Status
		};

		// Load product rates only for primary location products
		await LoadProductRates(selectedProduct.Id, selectedProduct.Name);

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
			var productRate = await CommonData.LoadTableDataById<ProductLocationModel>(TableNames.ProductLocation, rateId);
			productRate.Status = false;
			await ProductData.InsertProductLocation(productRate);
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
}