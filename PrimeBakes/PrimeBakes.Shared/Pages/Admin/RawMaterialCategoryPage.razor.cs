using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class RawMaterialCategoryPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private RawMaterialCategoryModel _rawMaterialCategoryModel = new()
	{
		Id = 0,
		Name = "",
		Status = true
	};

	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];

	private SfGrid<RawMaterialCategoryModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	// Toast message properties
	private string _successMessage = "Operation completed successfully!";
	private string _errorMessage = "An error occurred. Please try again.";

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadRawMaterialCategories();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadRawMaterialCategories()
	{
		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}

	private void OnAddCategory()
	{
		_rawMaterialCategoryModel = new()
		{
			Id = 0,
			Name = "",
			Status = true
		};
		StateHasChanged();
	}

	private void OnEditCategory(RawMaterialCategoryModel category)
	{
		_rawMaterialCategoryModel = new()
		{
			Id = category.Id,
			Name = category.Name,
			Status = category.Status
		};
		StateHasChanged();
	}

	private async Task ToggleCategoryStatus(RawMaterialCategoryModel category)
	{
		try
		{
			category.Status = !category.Status;
			await RawMaterialData.InsertRawMaterialCategory(category);
			await LoadRawMaterialCategories();

			_successMessage = $"Category '{category.Name}' has been {(category.Status ? "activated" : "deactivated")} successfully.";
			await ShowSuccessToast();

			OnAddCategory();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to update category status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_rawMaterialCategoryModel.Name))
		{
			_errorMessage = "Category name is required. Please enter a valid category name.";
			await ShowErrorToast();
			return false;
		}

		// Check for duplicate names
		if (_rawMaterialCategoryModel.Id > 0)
		{
			var existingCategory = _rawMaterialCategories.FirstOrDefault(_ => _.Id != _rawMaterialCategoryModel.Id && _.Name.Equals(_rawMaterialCategoryModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingCategory is not null)
			{
				_errorMessage = $"Category name '{_rawMaterialCategoryModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}
		}
		else
		{
			var existingCategory = _rawMaterialCategories.FirstOrDefault(_ => _.Name.Equals(_rawMaterialCategoryModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingCategory is not null)
			{
				_errorMessage = $"Category name '{_rawMaterialCategoryModel.Name}' already exists. Please choose a different name.";
				await ShowErrorToast();
				return false;
			}
		}

		return true;
	}

	private async Task SaveRawMaterialCategory()
	{
		try
		{
			if (_isSubmitting || !await ValidateForm())
				return;

			_isSubmitting = true;
			StateHasChanged();

			var isNewCategory = _rawMaterialCategoryModel.Id == 0;
			var categoryName = _rawMaterialCategoryModel.Name;

			await RawMaterialData.InsertRawMaterialCategory(_rawMaterialCategoryModel);
			await LoadRawMaterialCategories();

			// Reset form
			OnAddCategory();

			_successMessage = isNewCategory
				? $"Category '{categoryName}' has been created successfully!"
				: $"Category '{categoryName}' has been updated successfully!";
			await ShowSuccessToast();
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

	public void RowSelectHandler(RowSelectEventArgs<RawMaterialCategoryModel> args) =>
		OnEditCategory(args.Data);

	// Helper methods for showing toasts with dynamic content
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