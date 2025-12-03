using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Products;

public partial class ProductLocationPage
{
    private bool _isLoading = true;
    private bool _isProcessing = false;

    private ProductLocationModel _productLocation = new();

    private List<ProductLocationModel> _productLocations = [];
    private List<ProductLocationOverviewModel> _productLocationOverviews = [];
    private List<LocationModel> _locations = [];
    private List<ProductModel> _products = [];

    private string _selectedLocationName = string.Empty;
    private string _selectedProductName = string.Empty;

    private SfGrid<ProductLocationOverviewModel> _sfGrid;
    private SfDialog _deleteConfirmationDialog;

    private int _deleteProductLocationId = 0;
    private string _deleteProductLocationName = string.Empty;
    private bool _isDeleteDialogVisible = false;


    private string _errorTitle = string.Empty;
    private string _errorMessage = string.Empty;

    private string _successTitle = string.Empty;
    private string _successMessage = string.Empty;

    private SfToast _sfSuccessToast;
    private SfToast _sfErrorToast;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        try
        {
            _productLocations = await CommonData.LoadTableData<ProductLocationModel>(TableNames.ProductLocation);
            _locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
            _products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

            // Filter active locations and products only
            _locations = [.. _locations.Where(l => l.Status)];
            _products = [.. _products.Where(p => p.Status)];

            // If a location is selected, load products for that location
            if (_productLocation.LocationId > 0)
                _productLocationOverviews = await ProductData.LoadProductByLocation(_productLocation.LocationId);
            else
                _productLocationOverviews = [];

            if (_sfGrid is not null)
                await _sfGrid.Refresh();
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to load data: {ex.Message}", false);
        }
    }
    #endregion

    #region Autocomplete Events
    private async Task OnLocationChange(ChangeEventArgs<string, LocationModel> args)
    {
        if (args.ItemData != null)
        {
            _productLocation.LocationId = args.ItemData.Id;
            _selectedLocationName = args.ItemData.Name;

            // Load products for the selected location
            await LoadData();
            StateHasChanged();
        }
        else
        {
            _productLocation.LocationId = 0;
            _selectedLocationName = string.Empty;
            _productLocationOverviews = [];

            if (_sfGrid is not null)
                await _sfGrid.Refresh();

            StateHasChanged();
        }
    }

    private void OnProductChange(ChangeEventArgs<string, ProductModel> args)
    {
        if (args.ItemData != null)
        {
            _productLocation.ProductId = args.ItemData.Id;
            _selectedProductName = args.ItemData.Name;
            // If a product-location already exists for this location & product, use its stored rate
            if (_productLocation.LocationId > 0)
            {
                var existing = _productLocations.FirstOrDefault(pl => pl.LocationId == _productLocation.LocationId && pl.ProductId == _productLocation.ProductId);
                if (existing != null)
                {
                    _productLocation.Id = existing.Id; // prepare for update instead of insert
                    _productLocation.Rate = existing.Rate; // take current persisted rate
                }
                else
                {
                    // No existing row: keep any user-entered rate; if none (0) fall back to product's base rate
                    if (_productLocation.Rate == 0)
                        _productLocation.Rate = args.ItemData.Rate;
                }
            }
        }
        else
        {
            _productLocation.ProductId = 0;
            _selectedProductName = string.Empty;
        }
    }
    #endregion

    #region Actions
    private void OnEditProductLocation(ProductLocationModel productLocation)
    {
        _productLocation = new()
        {
            Id = productLocation.Id,
            ProductId = productLocation.ProductId,
            Rate = productLocation.Rate,
            LocationId = productLocation.LocationId,
            Status = productLocation.Status
        };

        // Set autocomplete values
        var location = _locations.FirstOrDefault(l => l.Id == productLocation.LocationId);
        _selectedLocationName = location?.Name ?? string.Empty;

        var product = _products.FirstOrDefault(p => p.Id == productLocation.ProductId);
        _selectedProductName = product?.Name ?? string.Empty;

        StateHasChanged();
    }

    private void ShowDeleteConfirmation(int id, string name)
    {
        _deleteProductLocationId = id;
        _deleteProductLocationName = name;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private async Task ConfirmDelete()
    {
        DeleteProductLocation();
    }

    private void CancelDelete()
    {
        _deleteProductLocationId = 0;
        _deleteProductLocationName = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private async void DeleteProductLocation()
    {
        _isProcessing = true;
        _isDeleteDialogVisible = false;
        StateHasChanged();

        try
        {
            var productLocation = _productLocations.FirstOrDefault(pl => pl.Id == _deleteProductLocationId);
            if (productLocation != null)
            {
                productLocation.Status = false;
                await ProductData.InsertProductLocation(productLocation);

                await LoadData();
                await ShowToast("Success", "Product location deleted successfully", true);
            }
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to delete product location: {ex.Message}", false);
        }
        finally
        {
            _deleteProductLocationId = 0;
            _deleteProductLocationName = string.Empty;
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Save
    private async Task<bool> ValidateForm()
    {
        if (_productLocation.LocationId == 0)
        {
            await ShowToast("Validation Error", "Please select a location", false);
            return false;
        }

        if (_productLocation.ProductId == 0)
        {
            await ShowToast("Validation Error", "Please select a product", false);
            return false;
        }

        if (_productLocation.Rate < 0)
        {
            await ShowToast("Validation Error", "Rate must be greater than or equal to 0", false);
            return false;
        }

        return true;
    }

    private async void SaveProductLocation()
    {
        if (!await ValidateForm())
            return;

        _isProcessing = true;
        StateHasChanged();

        try
        {
            // If an existing product-location row already exists for this location & product, update it instead of creating a duplicate
            var existing = _productLocations.FirstOrDefault(pl => pl.LocationId == _productLocation.LocationId && pl.ProductId == _productLocation.ProductId);
            if (existing != null)
                _productLocation.Id = existing.Id; // upsert semantics

            _productLocation.Status = true;
            await ProductData.InsertProductLocation(_productLocation);

            // Reset form
            _productLocation = new() { LocationId = _productLocation.LocationId };
            _selectedProductName = string.Empty;

            await LoadData();
            await ShowToast("Success", "Product location saved successfully", true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to save product location: {ex.Message}", false);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ShowToast(string title, string message, bool isSuccess)
    {
        if (isSuccess)
        {
            _successTitle = title;
            _successMessage = message;
            await _sfSuccessToast.ShowAsync(new()
            {
                Title = _successTitle,
                Content = _successMessage
            });
        }
        else
        {
            _errorTitle = title;
            _errorMessage = message;
            await _sfErrorToast.ShowAsync(new()
            {
                Title = _errorTitle,
                Content = _errorMessage
            });
        }

        VibrationService.VibrateWithTime(200);
    }
    #endregion

    #region Export
    private async void ExportExcel()
    {
        _isProcessing = true;
        StateHasChanged();

        try
        {
            // Enrich data with location and product names
            var exportData = _productLocationOverviews.Select(pl => new
            {
                pl.Id,
                Location = _locations.FirstOrDefault(l => l.Id == pl.LocationId)?.Name ?? "",
                ProductCode = pl.Code,
                ProductName = pl.Name,
                pl.Rate,
                Status = _productLocations.FirstOrDefault(p => p.Id == pl.Id)?.Status ?? false
            }).ToList();

            var stream = await ProductLocationExcelExport.ExportProductLocation(exportData);

            await SaveAndViewService.SaveAndView("Product_Location.xlsx", stream);
            await ShowToast("Success", "Excel export completed successfully", true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to export to Excel: {ex.Message}", false);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async void ExportPdf()
    {
        _isProcessing = true;
        StateHasChanged();

        try
        {
            // Enrich data with location and product names
            var exportData = _productLocationOverviews.Select(pl => new
            {
                pl.Id,
                Location = _locations.FirstOrDefault(l => l.Id == pl.LocationId)?.Name ?? "",
                ProductCode = pl.Code,
                ProductName = pl.Name,
                pl.Rate,
                Status = _productLocations.FirstOrDefault(p => p.Id == pl.Id)?.Status ?? false
            }).ToList();

            var stream = await ProductLocationPDFExport.ExportProductLocation(exportData);

            await SaveAndViewService.SaveAndView("Product_Location.pdf", stream);

            await ShowToast("Success", "PDF export completed successfully", true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to export to PDF: {ex.Message}", false);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Gets the display name for a product location combining location and product name
    /// </summary>
    private string GetDisplayName(ProductLocationOverviewModel productLocation)
    {
        var locationName = _locations.FirstOrDefault(l => l.Id == productLocation.LocationId)?.Name ?? "Unknown";
        return $"{locationName} - {productLocation.Name}";
    }
    #endregion
}