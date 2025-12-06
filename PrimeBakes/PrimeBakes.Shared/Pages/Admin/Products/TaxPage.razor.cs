using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Product;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Admin.Products;

public partial class TaxPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private TaxModel _tax = new();

    private List<TaxModel> _taxes = [];

    private SfGrid<TaxModel> _sfGrid;
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    private int _deleteTaxId = 0;
    private string _deleteTaxCode = string.Empty;

    private int _recoverTaxId = 0;
    private string _recoverTaxCode = string.Empty;

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
            .Add(ModCode.Ctrl, Code.S, SaveTax, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminTax, true), "New", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

        if (!_showDeleted)
            _taxes = [.. _taxes.Where(t => t.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnEditTax(TaxModel tax)
    {
        _tax = new()
        {
            Id = tax.Id,
            Code = tax.Code,
            CGST = tax.CGST,
            SGST = tax.SGST,
            IGST = tax.IGST,
            Inclusive = tax.Inclusive,
            Extra = tax.Extra,
            Remarks = tax.Remarks,
            Status = tax.Status
        };

        StateHasChanged();
    }

    private async Task ShowDeleteConfirmation(int id, string code)
    {
        _deleteTaxId = id;
        _deleteTaxCode = code;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteTaxId = 0;
        _deleteTaxCode = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();

            var tax = _taxes.FirstOrDefault(t => t.Id == _deleteTaxId);
            if (tax == null)
            {
                await _toastNotification.ShowAsync("Error", "Tax not found.", ToastType.Error);
                return;
            }

            tax.Status = false;
            await TaxData.InsertTax(tax);

            await _toastNotification.ShowAsync("Deleted", $"Tax '{tax.Code}' has been deleted successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminTax, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete tax: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteTaxId = 0;
            _deleteTaxCode = string.Empty;
        }
    }

    private async Task ShowRecoverConfirmation(int id, string code)
    {
        _recoverTaxId = id;
        _recoverTaxCode = code;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverTaxId = 0;
        _recoverTaxCode = string.Empty;
        await _recoverConfirmationDialog.HideAsync();
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
            await _recoverConfirmationDialog.HideAsync();

            var tax = _taxes.FirstOrDefault(t => t.Id == _recoverTaxId);
            if (tax == null)
            {
                await _toastNotification.ShowAsync("Error", "Tax not found.", ToastType.Error);
                return;
            }

            tax.Status = true;
            await TaxData.InsertTax(tax);

            await _toastNotification.ShowAsync("Recovered", $"Tax '{tax.Code}' has been recovered successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminTax, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover tax: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverTaxId = 0;
            _recoverTaxCode = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _tax.Code = _tax.Code?.Trim() ?? "";
        _tax.Code = _tax.Code?.ToUpper() ?? "";

        _tax.Remarks = _tax.Remarks?.Trim() ?? "";
        _tax.Status = true;

        if (string.IsNullOrWhiteSpace(_tax.Code))
        {
            await _toastNotification.ShowAsync("Validation", "Tax code is required. Please enter a valid tax code.", ToastType.Warning);
            return false;
        }

        if (_tax.CGST < 0 || _tax.CGST > 100)
        {
            await _toastNotification.ShowAsync("Validation", "CGST must be between 0 and 100.", ToastType.Warning);
            return false;
        }

        if (_tax.SGST < 0 || _tax.SGST > 100)
        {
            await _toastNotification.ShowAsync("Validation", "SGST must be between 0 and 100.", ToastType.Warning);
            return false;
        }

        if (_tax.IGST < 0 || _tax.IGST > 100)
        {
            await _toastNotification.ShowAsync("Validation", "IGST must be between 0 and 100.", ToastType.Warning);
            return false;
        }

        // Tax must be either Inclusive or Extra, not both, and not both false
        if (_tax.Inclusive == _tax.Extra)
        {
            await _toastNotification.ShowAsync("Validation", "Tax must be either Inclusive or Extra, but not both and not neither.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_tax.Remarks))
            _tax.Remarks = null;

        if (_tax.Id > 0)
        {
            var existingTax = _taxes.FirstOrDefault(t => t.Id != _tax.Id && t.Code.Equals(_tax.Code, StringComparison.OrdinalIgnoreCase));
            if (existingTax is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Tax code '{_tax.Code}' already exists. Please choose a different code.", ToastType.Warning);
                return false;
            }
        }
        else
        {
            var existingTax = _taxes.FirstOrDefault(t => t.Code.Equals(_tax.Code, StringComparison.OrdinalIgnoreCase));
            if (existingTax is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Tax code '{_tax.Code}' already exists. Please choose a different code.", ToastType.Warning);
                return false;
            }
        }

        return true;
    }

    private async Task SaveTax()
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

            await _toastNotification.ShowAsync("Processing", "Please wait while the tax is being saved...", ToastType.Info);

            await TaxData.InsertTax(_tax);

            await _toastNotification.ShowAsync("Saved", $"Tax '{_tax.Code}' has been saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminTax, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save tax: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Exporting", "Exporting to Excel...", ToastType.Info);

            // Call the Excel export utility
            var stream = await TaxExcelExport.ExportTax(_taxes);

            // Generate file name
            string fileName = "TAX_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Exported", "Tax data exported to Excel successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to Excel: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Exporting", "Exporting to PDF...", ToastType.Info);

            // Call the PDF export utility
            var stream = await TaxPDFExport.ExportTax(_taxes);

            // Generate file name
            string fileName = "TAX_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Exported", "Tax data exported to PDF successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to PDF: {ex.Message}", ToastType.Error);
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
            OnEditTax(selectedRecords[0]);
    }

    private async Task DeleteSelectedItem()
    {
        var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
        if (selectedRecords.Count > 0)
        {
            if (selectedRecords[0].Status)
                ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Code);
            else
                ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Code);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hotKeysContext.DisposeAsync();
    }
}