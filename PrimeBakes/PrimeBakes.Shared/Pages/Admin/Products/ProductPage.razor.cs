using PrimeBakes.Shared.Components;
using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Products;

public partial class ProductPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
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

    private ToastNotification _toastNotification;

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
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveProduct, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true), "New", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

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
                await _toastNotification.ShowAsync("Error", "Product not found.", ToastType.Error);
                return;
            }

            product.Status = false;
            await ProductData.InsertProduct(product);

            await _toastNotification.ShowAsync("Deleted", $"Product '{product.Name}' removed successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete product: {ex.Message}", ToastType.Error);
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
                await _toastNotification.ShowAsync("Error", "Product not found.", ToastType.Error);
                return;
            }

            product.Status = true;
            await ProductData.InsertProduct(product);

            await _toastNotification.ShowAsync("Recovered", $"Product '{product.Name}' restored successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover product: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Validation", "Product name is required.", ToastType.Warning);
            return false;
        }

        // Code is auto-generated, no need to validate

        if (_product.ProductCategoryId <= 0)
        {
            await _toastNotification.ShowAsync("Validation", "Please select a category.", ToastType.Warning);
            return false;
        }

        if (_product.Rate < 0)
        {
            await _toastNotification.ShowAsync("Validation", "Rate must be 0 or greater.", ToastType.Warning);
            return false;
        }

        if (_product.TaxId <= 0)
        {
            await _toastNotification.ShowAsync("Validation", "Please select a tax.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_product.Remarks))
            _product.Remarks = null;

        if (_product.Id > 0)
        {
            var existingByName = _products.FirstOrDefault(p => p.Id != _product.Id && p.Name.Equals(_product.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Product name '{_product.Name}' already exists.", ToastType.Warning);
                return false;
            }

            // Code is preserved when editing, no need to check for duplicates
        }
        else
        {
            var existingByName = _products.FirstOrDefault(p => p.Name.Equals(_product.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await _toastNotification.ShowAsync("Validation", $"Product name '{_product.Name}' already exists.", ToastType.Warning);
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
            StateHasChanged();

            if (!await ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            await _toastNotification.ShowAsync("Saving", "Processing product...", ToastType.Info);

            if (_product.Id == 0)
                _product.Code = await GenerateCodes.GenerateProductCode();

            var isNewProduct = _product.Id == 0;

			_product.Id =  await ProductData.InsertProduct(_product);
            if (isNewProduct)
				await InsertProductLocations();

            await _toastNotification.ShowAsync("Saved", $"Product '{_product.Name}' saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save product: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);

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

            await _toastNotification.ShowAsync("Exported", "Excel file downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Excel export failed: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Exporting", "Generating PDF file...", ToastType.Info);

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

            await _toastNotification.ShowAsync("Exported", "PDF file downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"PDF export failed: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    private async Task EditSelectedItem()
    {
        var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
        if (selectedRecords.Count > 0)
            OnEditProduct(selectedRecords[0]);
    }

    private async Task DeleteSelectedItem()
    {
        var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
        if (selectedRecords.Count > 0)
        {
            if (selectedRecords[0].Status)
                ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
            else
                ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hotKeysContext.DisposeAsync();
    }
}