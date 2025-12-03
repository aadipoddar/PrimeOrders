using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Products;

public partial class ProductCategoryPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private ProductCategoryModel _productCategory = new();

	private List<ProductCategoryModel> _productCategories = [];

	private SfGrid<ProductCategoryModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteProductCategoryId = 0;
	private string _deleteProductCategoryName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverProductCategoryId = 0;
	private string _recoverProductCategoryName = string.Empty;
	private bool _isRecoverDialogVisible = false;

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
		_productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);

		if (!_showDeleted)
			_productCategories = [.. _productCategories.Where(pc => pc.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditProductCategory(ProductCategoryModel productCategory)
	{
		_productCategory = new()
		{
			Id = productCategory.Id,
			Name = productCategory.Name,
			Remarks = productCategory.Remarks,
			Status = productCategory.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteProductCategoryId = id;
		_deleteProductCategoryName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteProductCategoryId = 0;
		_deleteProductCategoryName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var productCategory = _productCategories.FirstOrDefault(pc => pc.Id == _deleteProductCategoryId);
			if (productCategory == null)
			{
				await ShowToast("Error", "Product Category not found.", "error");
				return;
			}

			productCategory.Status = false;
			await ProductData.InsertProductCategory(productCategory);

			await ShowToast("Success", $"Product Category '{productCategory.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminProductCategory, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete Product Category: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteProductCategoryId = 0;
			_deleteProductCategoryName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverProductCategoryId = id;
		_recoverProductCategoryName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverProductCategoryId = 0;
		_recoverProductCategoryName = string.Empty;
		_isRecoverDialogVisible = false;
		StateHasChanged();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			_isRecoverDialogVisible = false;

			var productCategory = _productCategories.FirstOrDefault(pc => pc.Id == _recoverProductCategoryId);
			if (productCategory == null)
			{
				await ShowToast("Error", "Product Category not found.", "error");
				return;
			}

			productCategory.Status = true;
			await ProductData.InsertProductCategory(productCategory);

			await ShowToast("Success", $"Product Category '{productCategory.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminProductCategory, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover Product Category: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_recoverProductCategoryId = 0;
			_recoverProductCategoryName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_productCategory.Name = _productCategory.Name?.Trim() ?? "";
		_productCategory.Name = _productCategory.Name?.ToUpper() ?? "";

		_productCategory.Remarks = _productCategory.Remarks?.Trim() ?? "";
		_productCategory.Status = true;

		if (string.IsNullOrWhiteSpace(_productCategory.Name))
		{
			await ShowToast("Error", "Product Category name is required. Please enter a valid product category name.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_productCategory.Remarks))
			_productCategory.Remarks = null;

		if (_productCategory.Id > 0)
		{
			var existingProductCategory = _productCategories.FirstOrDefault(_ => _.Id != _productCategory.Id && _.Name.Equals(_productCategory.Name, StringComparison.OrdinalIgnoreCase));
			if (existingProductCategory is not null)
			{
				await ShowToast("Error", $"Product Category name '{_productCategory.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}
		else
		{
			var existingProductCategory = _productCategories.FirstOrDefault(_ => _.Name.Equals(_productCategory.Name, StringComparison.OrdinalIgnoreCase));
			if (existingProductCategory is not null)
			{
				await ShowToast("Error", $"Product Category name '{_productCategory.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}

		return true;
	}

	private async Task SaveProductCategory()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await ProductData.InsertProductCategory(_productCategory);

			await ShowToast("Success", $"Product Category '{_productCategory.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminProductCategory, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save Product Category: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			// Call the Excel export utility
			var stream = await ProductCategoryExcelExport.ExportProductCategory(_productCategories);

			// Generate file name
			string fileName = "PRODUCT_CATEGORY_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Product Category data exported to Excel successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while exporting to Excel: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			// Call the PDF export utility
			var stream = await ProductCategoryPDFExport.ExportProductCategory(_productCategories);

			// Generate file name
			string fileName = "PRODUCT_CATEGORY_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Product Category data exported to PDF successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while exporting to PDF: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}

		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}
	#endregion
}