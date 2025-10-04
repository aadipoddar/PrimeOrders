using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrimeBakes.Shared.Services;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class ProductCategoryPage
{
    [Inject] public NavigationManager NavManager { get; set; }
    [Inject] public IJSRuntime JS { get; set; }

    private bool _isLoading = true;
    private bool _isSubmitting = false;
    private string _successMessage = "";
    private string _errorMessage = "";

    private UserModel _currentUser;
    private ProductCategoryModel _categoryModel = new()
    {
        Id = 0,
        Name = "",
        LocationId = 1,
        Status = true
    };

    private List<ProductCategoryModel> _categories = [];
    private List<LocationModel> _locations = [];

    private SfGrid<ProductCategoryModel> _sfGrid;
    private SfToast _sfToast;
    private SfToast _sfUpdateToast;
    private SfToast _sfErrorToast;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            // Validate user and check admin permissions
            var userResult = await AuthService.ValidateUser(DataStorageService, NavManager, NotificationService, VibrationService, UserRoles.Admin);
            _currentUser = userResult.User;
            await LoadData();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load page: {ex.Message}";
            await ShowErrorToast();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadData()
    {
        try
        {
            // Load all categories and locations
            _categories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
            _locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

            // Set default location for new categories
            _categoryModel.LocationId = _currentUser.LocationId;

            // Apply location-based filtering
            if (_currentUser.LocationId != 1)
            {
                // Non-location-1 users can only see their own categories
                _categories = _categories.Where(c => c.LocationId == _currentUser.LocationId).ToList();

                // Non-location-1 users can only create categories for their location
                _locations = _locations.Where(l => l.Id == _currentUser.LocationId).ToList();
            }

            // Refresh grid if it exists
            if (_sfGrid != null)
            {
                await _sfGrid.Refresh();
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load data: {ex.Message}";
            await ShowErrorToast();
        }
    }

    public async Task RowSelectHandler(RowSelectEventArgs<ProductCategoryModel> args)
    {
        try
        {
            // Check if user can edit this category
            if (_currentUser.LocationId != 1 && args.Data.LocationId != _currentUser.LocationId)
            {
                _errorMessage = "You can only edit categories from your location.";
                await ShowErrorToast();
                return;
            }

            _categoryModel = new ProductCategoryModel
            {
                Id = args.Data.Id,
                Name = args.Data.Name,
                LocationId = args.Data.LocationId,
                Status = args.Data.Status
            };

            _successMessage = "Category selected for editing.";
            await ShowSuccessToast();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error selecting category: {ex.Message}";
            await ShowErrorToast();
        }
    }

    private async Task<bool> ValidateForm()
    {
        try
        {
            // Ensure non-location-1 users can only create/edit for their location
            if (_currentUser.LocationId != 1)
            {
                _categoryModel.LocationId = _currentUser.LocationId;
            }

            if (_categoryModel.LocationId <= 0)
            {
                _errorMessage = "Please select a valid location.";
                await ShowErrorToast();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_categoryModel.Name))
            {
                _errorMessage = "Category name is required.";
                await ShowErrorToast();
                return false;
            }

            if (_categoryModel.Name.Length > 50)
            {
                _errorMessage = "Category name cannot exceed 50 characters.";
                await ShowErrorToast();
                return false;
            }

            // Check for duplicate category names within the same location
            var existingCategory = _categories.FirstOrDefault(c =>
                c.Name.Equals(_categoryModel.Name, StringComparison.OrdinalIgnoreCase) &&
                c.LocationId == _categoryModel.LocationId &&
                c.Id != _categoryModel.Id);

            if (existingCategory != null)
            {
                _errorMessage = "A category with this name already exists in this location.";
                await ShowErrorToast();
                return false;
            }

            // Additional validation for editing existing categories
            if (_categoryModel.Id > 0)
            {
                var originalCategory = _categories.FirstOrDefault(c => c.Id == _categoryModel.Id);
                if (originalCategory != null)
                {
                    // Non-location-1 users can only edit their own location's categories
                    if (_currentUser.LocationId != 1 && originalCategory.LocationId != _currentUser.LocationId)
                    {
                        _errorMessage = "You can only edit categories from your location.";
                        await ShowErrorToast();
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Validation error: {ex.Message}";
            await ShowErrorToast();
            return false;
        }
    }

    private async Task OnSaveClick()
    {
        if (_isSubmitting) return;

        try
        {
            _isSubmitting = true;
            StateHasChanged();

            if (!await ValidateForm()) return;

            await ProductData.InsertProductCategory(_categoryModel);

            // Show success message
            _successMessage = _categoryModel.Id == 0 ?
                "Product category created successfully!" :
                "Product category updated successfully!";
            await ShowSuccessToast();

            // Reset form for new entry
            ResetForm();

            // Reload data to reflect changes
            await LoadData();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save category: {ex.Message}";
            await ShowErrorToast();
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private void ResetForm()
    {
        _categoryModel = new ProductCategoryModel
        {
            Id = 0,
            Name = "",
            LocationId = _currentUser.LocationId,
            Status = true
        };
        StateHasChanged();
    }

    private async Task OnDeleteClick(ProductCategoryModel category)
    {
        try
        {
            // Check if user can delete this category
            if (_currentUser.LocationId != 1 && category.LocationId != _currentUser.LocationId)
            {
                _errorMessage = "You can only delete categories from your location.";
                await ShowErrorToast();
                return;
            }

            // Note: In a real application, you might want to check if the category is being used by products
            // before allowing deletion. For now, we'll just deactivate it.
            category.Status = false;
            await ProductData.InsertProductCategory(category);

            _successMessage = "Product category deactivated successfully!";
            await ShowSuccessToast();

            await LoadData();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to delete category: {ex.Message}";
            await ShowErrorToast();
        }
    }

    private string GetLocationName(int locationId)
    {
        return _locations.FirstOrDefault(l => l.Id == locationId)?.Name ?? "Unknown";
    }

    private string GetStatusText(bool status)
    {
        return status ? "Active" : "Inactive";
    }

    private string GetStatusClass(bool status)
    {
        return status ? "status-active" : "status-inactive";
    }

    public void SelectCategoryForEdit(ProductCategoryModel category)
    {
        // Check if user can edit this category
        if (_currentUser.LocationId != 1 && category.LocationId != _currentUser.LocationId)
        {
            _errorMessage = "You can only edit categories from your location.";
            _ = ShowErrorToast();
            return;
        }

        _categoryModel = new ProductCategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            LocationId = category.LocationId,
            Status = category.Status
        };

        StateHasChanged();
    }

    private async Task ShowSuccessToast()
    {
        if (_sfToast != null)
        {
            var toastModel = new ToastModel
            {
                Title = "Success",
                Content = _successMessage,
                CssClass = "e-toast-success",
                Icon = "e-success toast-icons"
            };
            await _sfToast.ShowAsync(toastModel);
        }
    }

    private async Task ShowErrorToast()
    {
        if (_sfErrorToast != null)
        {
            var toastModel = new ToastModel
            {
                Title = "Error",
                Content = _errorMessage,
                CssClass = "e-toast-danger",
                Icon = "e-error toast-icons"
            };
            await _sfErrorToast.ShowAsync(toastModel);
        }
    }
}