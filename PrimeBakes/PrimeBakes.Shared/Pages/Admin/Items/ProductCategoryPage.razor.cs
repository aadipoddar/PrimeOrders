using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Items;

public partial class ProductCategoryPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;
	private string _successMessage = "";
	private string _errorMessage = "";

	private ProductCategoryModel _categoryModel = new()
	{
		Id = 0,
		Name = "",
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
			await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
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

			// Refresh grid if it exists
			if (_sfGrid is not null)
				await _sfGrid.Refresh();

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
			_categoryModel = new()
			{
				Id = args.Data.Id,
				Name = args.Data.Name,
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

			// Check for duplicate category names
			var existingCategory = _categories.FirstOrDefault(c =>
				c.Name.Equals(_categoryModel.Name, StringComparison.OrdinalIgnoreCase) &&
				c.Id != _categoryModel.Id);

			if (existingCategory != null)
			{
				_errorMessage = "A category with this name already exists.";
				await ShowErrorToast();
				return false;
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
			Status = true
		};
		StateHasChanged();
	}

	private async Task OnDeleteClick(ProductCategoryModel category)
	{
		try
		{
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
		_categoryModel = new()
		{
			Id = category.Id,
			Name = category.Name,
			Status = category.Status
		};

		StateHasChanged();
	}

	private async Task ShowSuccessToast()
	{
		if (_sfToast is not null)
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
		if (_sfErrorToast is not null)
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