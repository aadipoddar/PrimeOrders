using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.RawMaterial;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Inventory;

public partial class RawMaterialPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private RawMaterialModel _rawMaterial = new();

    private List<RawMaterialModel> _rawMaterials = [];
    private List<RawMaterialCategoryModel> _categories = [];
    private List<TaxModel> _taxes = [];

    private string _selectedCategoryName = string.Empty;
    private string _selectedTaxCode = string.Empty;

    private SfGrid<RawMaterialModel> _sfGrid;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    private int _deleteRawMaterialId = 0;
    private string _deleteRawMaterialName = string.Empty;
    private bool _isDeleteDialogVisible = false;

    private int _recoverRawMaterialId = 0;
    private string _recoverRawMaterialName = string.Empty;
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
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveRawMaterial, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial, true), "New", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
        _categories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
        _taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

        if (!_showDeleted)
            _rawMaterials = [.. _rawMaterials.Where(rm => rm.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Autocomplete Events
    private void OnCategoryChange(ChangeEventArgs<string, RawMaterialCategoryModel> args)
    {
        if (args.ItemData != null)
        {
            _rawMaterial.RawMaterialCategoryId = args.ItemData.Id;
            _selectedCategoryName = args.ItemData.Name;
        }
        else
        {
            _rawMaterial.RawMaterialCategoryId = 0;
            _selectedCategoryName = string.Empty;
        }
    }

    private void OnTaxChange(ChangeEventArgs<string, TaxModel> args)
    {
        if (args.ItemData != null)
        {
            _rawMaterial.TaxId = args.ItemData.Id;
            _selectedTaxCode = args.ItemData.Code;
        }
        else
        {
            _rawMaterial.TaxId = 0;
            _selectedTaxCode = string.Empty;
        }
    }
    #endregion

    #region Actions
    private void OnEditRawMaterial(RawMaterialModel rawMaterial)
    {
        _rawMaterial = new()
        {
            Id = rawMaterial.Id,
            Name = rawMaterial.Name,
            Code = rawMaterial.Code,
            RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
            Rate = rawMaterial.Rate,
            UnitOfMeasurement = rawMaterial.UnitOfMeasurement,
            TaxId = rawMaterial.TaxId,
            Remarks = rawMaterial.Remarks,
            Status = rawMaterial.Status
        };

        // Set autocomplete values
        var category = _categories.FirstOrDefault(c => c.Id == rawMaterial.RawMaterialCategoryId);
        _selectedCategoryName = category?.Name ?? string.Empty;

        var tax = _taxes.FirstOrDefault(t => t.Id == rawMaterial.TaxId);
        _selectedTaxCode = tax?.Code ?? string.Empty;

        StateHasChanged();
    }

    private void ShowDeleteConfirmation(int id, string name)
    {
        _deleteRawMaterialId = id;
        _deleteRawMaterialName = name;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _deleteRawMaterialId = 0;
        _deleteRawMaterialName = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            _isDeleteDialogVisible = false;

            var rawMaterial = _rawMaterials.FirstOrDefault(rm => rm.Id == _deleteRawMaterialId);
            if (rawMaterial == null)
            {
                await ShowToast("Error", "Raw material not found.", "error");
                return;
            }

            rawMaterial.Status = false;
            await RawMaterialData.InsertRawMaterial(rawMaterial);

            await ShowToast("Success", $"Raw material '{rawMaterial.Name}' has been deleted successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to delete raw material: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            _deleteRawMaterialId = 0;
            _deleteRawMaterialName = string.Empty;
        }
    }

    private void ShowRecoverConfirmation(int id, string name)
    {
        _recoverRawMaterialId = id;
        _recoverRawMaterialName = name;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _recoverRawMaterialId = 0;
        _recoverRawMaterialName = string.Empty;
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

            var rawMaterial = _rawMaterials.FirstOrDefault(rm => rm.Id == _recoverRawMaterialId);
            if (rawMaterial == null)
            {
                await ShowToast("Error", "Raw material not found.", "error");
                return;
            }

            rawMaterial.Status = true;
            await RawMaterialData.InsertRawMaterial(rawMaterial);

            await ShowToast("Success", $"Raw material '{rawMaterial.Name}' has been recovered successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to recover raw material: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            _recoverRawMaterialId = 0;
            _recoverRawMaterialName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _rawMaterial.Name = _rawMaterial.Name?.Trim() ?? "";
        _rawMaterial.Code = _rawMaterial.Code?.Trim() ?? "";
        _rawMaterial.UnitOfMeasurement = _rawMaterial.UnitOfMeasurement?.Trim() ?? "";
        _rawMaterial.Remarks = _rawMaterial.Remarks?.Trim() ?? "";

        _rawMaterial.Code = _rawMaterial.Code?.ToUpper() ?? "";
        _rawMaterial.UnitOfMeasurement = _rawMaterial.UnitOfMeasurement?.ToUpper() ?? "";

        _rawMaterial.Status = true;

        if (string.IsNullOrWhiteSpace(_rawMaterial.Name))
        {
            await ShowToast("Error", "Raw material name is required. Please enter a valid name.", "error");
            return false;
        }

        // Code is auto-generated, no need to validate

        if (_rawMaterial.RawMaterialCategoryId <= 0)
        {
            await ShowToast("Error", "Category is required. Please select a category.", "error");
            return false;
        }

        if (_rawMaterial.Rate < 0)
        {
            await ShowToast("Error", "Rate must be greater than or equal to 0.", "error");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_rawMaterial.UnitOfMeasurement))
        {
            await ShowToast("Error", "Unit of measurement is required. Please enter a valid unit.", "error");
            return false;
        }

        if (_rawMaterial.TaxId <= 0)
        {
            await ShowToast("Error", "Tax is required. Please select a tax.", "error");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_rawMaterial.Remarks))
            _rawMaterial.Remarks = null;

        if (_rawMaterial.Id > 0)
        {
            var existingByName = _rawMaterials.FirstOrDefault(rm => rm.Id != _rawMaterial.Id && rm.Name.Equals(_rawMaterial.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await ShowToast("Error", $"Raw material name '{_rawMaterial.Name}' already exists. Please choose a different name.", "error");
                return false;
            }

            // Code is preserved when editing, no need to check for duplicates
        }
        else
        {
            var existingByName = _rawMaterials.FirstOrDefault(rm => rm.Name.Equals(_rawMaterial.Name, StringComparison.OrdinalIgnoreCase));
            if (existingByName is not null)
            {
                await ShowToast("Error", $"Raw material name '{_rawMaterial.Name}' already exists. Please choose a different name.", "error");
                return false;
            }

            // Code is auto-generated and unique, no need to check for duplicates
        }

        return true;
    }

    private async Task SaveRawMaterial()
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

            await ShowToast("Processing Transaction", "Please wait while the transaction is being saved...", "success");

            if (_rawMaterial.Id == 0)
                _rawMaterial.Code = await GenerateCodes.GenerateRawMaterialCode();

            await RawMaterialData.InsertRawMaterial(_rawMaterial);

            await ShowToast("Success", $"Raw material '{_rawMaterial.Name}' has been saved successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to save raw material: {ex.Message}", "error");
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
            await ShowToast("Processing", "Exporting to Excel...", "success");

            // Enrich data with category and tax names
            var enrichedData = _rawMaterials.Select(rm => new
            {
                rm.Id,
                rm.Name,
                rm.Code,
                Category = _categories.FirstOrDefault(c => c.Id == rm.RawMaterialCategoryId)?.Name ?? "N/A",
                rm.Rate,
                rm.UnitOfMeasurement,
                Tax = _taxes.FirstOrDefault(t => t.Id == rm.TaxId)?.Code ?? "N/A",
                rm.Remarks,
                rm.Status
            }).ToList();

            // Call the Excel export utility
            var stream = await RawMaterialExcelExport.ExportRawMaterial(enrichedData);

            // Generate file name
            string fileName = "RAW_MATERIAL_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Raw material data exported to Excel successfully.", "success");
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
            await ShowToast("Processing", "Exporting to PDF...", "success");

            // Enrich data with category and tax names
            var enrichedData = _rawMaterials.Select(rm => new
            {
                rm.Id,
                rm.Name,
                rm.Code,
                Category = _categories.FirstOrDefault(c => c.Id == rm.RawMaterialCategoryId)?.Name ?? "N/A",
                rm.Rate,
                rm.UnitOfMeasurement,
                Tax = _taxes.FirstOrDefault(t => t.Id == rm.TaxId)?.Code ?? "N/A",
                rm.Remarks,
                rm.Status
            }).ToList();

            // Call the PDF export utility
            var stream = await RawMaterialPDFExport.ExportRawMaterial(enrichedData);

            // Generate file name
            string fileName = "RAW_MATERIAL_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Raw material data exported to PDF successfully.", "success");
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

    private async Task EditSelectedItem()
    {
        var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
        if (selectedRecords.Count > 0)
            OnEditRawMaterial(selectedRecords[0]);
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