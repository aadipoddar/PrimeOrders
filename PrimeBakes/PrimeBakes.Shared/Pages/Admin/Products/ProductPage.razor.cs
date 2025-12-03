using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
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

public partial class ProductPage
{
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private ProductModel _product = new();

    private List<ProductModel> _products = [];
    private List<ProductCategoryModel> _categories = [];
    private List<TaxModel> _taxes = [];

    private string _selectedCategoryName = string.Empty;
    private string _selectedTaxCode = string.Empty;

    private SfGrid<ProductModel> _sfGrid;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    private int _deleteProductId = 0;
    private string _deleteProductName = string.Empty;
    private bool _isDeleteDialogVisible = false;

    private int _recoverProductId = 0;
    private string _recoverProductName = string.Empty;
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
        _products = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
        _categories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
        _taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

        if (!_showDeleted)
            _products = [.. _products.Where(p => p.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Autocomplete Events
    private void OnCategoryChange(ChangeEventArgs<string, ProductCategoryModel> args)
    {
        if (args.ItemData != null)
        {
            _product.ProductCategoryId = args.ItemData.Id;
            _selectedCategoryName = args.ItemData.Name;
        }
        else
        {
            _product.ProductCategoryId = 0;
            _selectedCategoryName = string.Empty;
        }
    }

    private void OnTaxChange(ChangeEventArgs<string, TaxModel> args)
    {
        if (args.ItemData != null)
        {
            _product.TaxId = args.ItemData.Id;
            _selectedTaxCode = args.ItemData.Code;
        }
        else
        {
            _product.TaxId = 0;
            _selectedTaxCode = string.Empty;
        }
    }
    #endregion

    #region Actions
    private void OnEditProduct(ProductModel product)
    {
        _product = new()
        {
            Id = product.Id,
            Name = product.Name,
            Code = product.Code,
            ProductCategoryId = product.ProductCategoryId,
            Rate = product.Rate,
            TaxId = product.TaxId,
            Remarks = product.Remarks,
            Status = product.Status
        };

        // Set autocomplete values
        var category = _categories.FirstOrDefault(c => c.Id == product.ProductCategoryId);
        _selectedCategoryName = category?.Name ?? string.Empty;

        var tax = _taxes.FirstOrDefault(t => t.Id == product.TaxId);
        _selectedTaxCode = tax?.Code ?? string.Empty;

        StateHasChanged();
    }

    private void ShowDeleteConfirmation(int id, string name)
    {
        _deleteProductId = id;
        _deleteProductName = name;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _deleteProductId = 0;
        _deleteProductName = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            _isDeleteDialogVisible = false;

            var product = _products.FirstOrDefault(p => p.Id == _deleteProductId);
            if (product == null)
            {
                await ShowToast("Error", "Product not found.", "error");
                return;
            }

            product.Status = false;
            await ProductData.InsertProduct(product);

            await ShowToast("Success", $"Product '{product.Name}' has been deleted successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to delete product: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            _deleteProductId = 0;
            _deleteProductName = string.Empty;
        }
    }

    private void ShowRecoverConfirmation(int id, string name)
    {
        _recoverProductId = id;
        _recoverProductName = name;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _recoverProductId = 0;
        _recoverProductName = string.Empty;
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

            var product = _products.FirstOrDefault(p => p.Id == _recoverProductId);
            if (product == null)
            {
                await ShowToast("Error", "Product not found.", "error");
                return;
            }

            product.Status = true;
            await ProductData.InsertProduct(product);

            await ShowToast("Success", $"Product '{product.Name}' has been recovered successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to recover product: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            _recoverProductId = 0;
            _recoverProductName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _product.Name = _product.Name?.Trim() ?? "";
        _product.Code = _product.Code?.Trim() ?? "";
        _product.Remarks = _product.Remarks?.Trim() ?? "";

        _product.Code = _product.Code?.ToUpper() ?? "";

        _product.Status = true;

        if (string.IsNullOrWhiteSpace(_product.Name))
        {
            await ShowToast("Error", "Product name is required. Please enter a valid name.", "error");
            return false;
        }

        // Code is auto-generated, no need to validate

        if (_product.ProductCategoryId <= 0)
        {
            await ShowToast("Error", "Category is required. Please select a category.", "error");
            return false;
        }

        if (_product.Rate < 0)
        {
            await ShowToast("Error", "Rate must be greater than or equal to 0.", "error");
            return false;
        }

        if (_product.TaxId <= 0)
        {
            await ShowToast("Error", "Tax is required. Please select a tax.", "error");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_product.Remarks))
            _product.Remarks = null;

        if (_product.Id > 0)
        {
            var existingByName = _products.FirstOrDefault(p => p.Id != _product.Id && p.Name.Equals(_product.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await ShowToast("Error", $"Product name '{_product.Name}' already exists. Please choose a different name.", "error");
                return false;
            }

            // Code is preserved when editing, no need to check for duplicates
        }
        else
        {
            var existingByName = _products.FirstOrDefault(p => p.Name.Equals(_product.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await ShowToast("Error", $"Product name '{_product.Name}' already exists. Please choose a different name.", "error");
                return false;
            }

            // Code is auto-generated and unique, no need to check for duplicates
        }

        return true;
    }

    private async Task SaveProduct()
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

            if (_product.Id == 0)
                _product.Code = await GenerateCodes.GenerateProductCode();

            var isNewProduct = _product.Id == 0;

			_product.Id =  await ProductData.InsertProduct(_product);
            if (isNewProduct)
				await InsertProductLocations();

            await ShowToast("Success", $"Product '{_product.Name}' has been saved successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to save product: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task InsertProductLocations()
    {
        var locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
        foreach (var location in locations)
            await ProductData.InsertProductLocation(new ()
			{
                Id = 0,
                Rate = _product.Rate,
				ProductId = _product.Id,
				LocationId = location.Id,
                Status = true,
			});
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

            // Enrich data with category and tax names
            var enrichedData = _products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                Category = _categories.FirstOrDefault(c => c.Id == p.ProductCategoryId)?.Name ?? "N/A",
                p.Rate,
                Tax = _taxes.FirstOrDefault(t => t.Id == p.TaxId)?.Code ?? "N/A",
                p.Remarks,
                p.Status
            }).ToList();

            // Call the Excel export utility
            var stream = await ProductExcelExport.ExportProduct(enrichedData);

            // Generate file name
            string fileName = "PRODUCT_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Product data exported to Excel successfully.", "success");
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

            // Enrich data with category and tax names
            var enrichedData = _products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                Category = _categories.FirstOrDefault(c => c.Id == p.ProductCategoryId)?.Name ?? "N/A",
                p.Rate,
                Tax = _taxes.FirstOrDefault(t => t.Id == p.TaxId)?.Code ?? "N/A",
                p.Remarks,
                p.Status
            }).ToList();

            // Call the PDF export utility
            var stream = await ProductPDFExport.ExportProduct(enrichedData);

            // Generate file name
            string fileName = "PRODUCT_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Product data exported to PDF successfully.", "success");
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