using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Inventory;

public partial class KitchenPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private KitchenModel _kitchen = new();

    private List<KitchenModel> _kitchens = [];

    private SfGrid<KitchenModel> _sfGrid;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    private int _deleteKitchenId = 0;
    private string _deleteKitchenName = string.Empty;
    private bool _isDeleteDialogVisible = false;

    private int _recoverKitchenId = 0;
    private string _recoverKitchenName = string.Empty;
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
            .Add(ModCode.Ctrl, Code.S, SaveKitchen, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true), "New", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _kitchens = await CommonData.LoadTableData<KitchenModel>(TableNames.Kitchen);

        if (!_showDeleted)
            _kitchens = [.. _kitchens.Where(k => k.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnEditKitchen(KitchenModel kitchen)
    {
        _kitchen = new()
        {
            Id = kitchen.Id,
            Name = kitchen.Name,
            Remarks = kitchen.Remarks,
            Status = kitchen.Status
        };

        StateHasChanged();
    }

    private void ShowDeleteConfirmation(int id, string name)
    {
        _deleteKitchenId = id;
        _deleteKitchenName = name;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _deleteKitchenId = 0;
        _deleteKitchenName = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            _isDeleteDialogVisible = false;

            var kitchen = _kitchens.FirstOrDefault(k => k.Id == _deleteKitchenId);
            if (kitchen == null)
            {
                await ShowToast("Error", "Kitchen not found.", "error");
                return;
            }

            kitchen.Status = false;
            await KitchenData.InsertKitchen(kitchen);

            await ShowToast("Success", $"Kitchen '{kitchen.Name}' has been deleted successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to delete kitchen: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            _deleteKitchenId = 0;
            _deleteKitchenName = string.Empty;
        }
    }

    private void ShowRecoverConfirmation(int id, string name)
    {
        _recoverKitchenId = id;
        _recoverKitchenName = name;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _recoverKitchenId = 0;
        _recoverKitchenName = string.Empty;
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

            var kitchen = _kitchens.FirstOrDefault(k => k.Id == _recoverKitchenId);
            if (kitchen == null)
            {
                await ShowToast("Error", "Kitchen not found.", "error");
                return;
            }

            kitchen.Status = true;
            await KitchenData.InsertKitchen(kitchen);

            await ShowToast("Success", $"Kitchen '{kitchen.Name}' has been recovered successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to recover kitchen: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            _recoverKitchenId = 0;
            _recoverKitchenName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _kitchen.Name = _kitchen.Name?.Trim() ?? "";
        _kitchen.Remarks = _kitchen.Remarks?.Trim() ?? "";

        _kitchen.Name = _kitchen.Name?.ToUpper() ?? "";
        _kitchen.Status = true;

        if (string.IsNullOrWhiteSpace(_kitchen.Name))
        {
            await ShowToast("Error", "Kitchen name is required. Please enter a valid kitchen name.", "error");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_kitchen.Remarks))
            _kitchen.Remarks = null;

        if (_kitchen.Id > 0)
        {
            var existingKitchen = _kitchens.FirstOrDefault(_ => _.Id != _kitchen.Id && _.Name.Equals(_kitchen.Name, StringComparison.OrdinalIgnoreCase));
            if (existingKitchen is not null)
            {
                await ShowToast("Error", $"Kitchen name '{_kitchen.Name}' already exists. Please choose a different name.", "error");
                return false;
            }
        }
        else
        {
            var existingKitchen = _kitchens.FirstOrDefault(_ => _.Name.Equals(_kitchen.Name, StringComparison.OrdinalIgnoreCase));
            if (existingKitchen is not null)
            {
                await ShowToast("Error", $"Kitchen name '{_kitchen.Name}' already exists. Please choose a different name.", "error");
                return false;
            }
        }

        return true;
    }

    private async Task SaveKitchen()
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

            await KitchenData.InsertKitchen(_kitchen);

            await ShowToast("Success", $"Kitchen '{_kitchen.Name}' has been saved successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to save kitchen: {ex.Message}", "error");
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
            var stream = await KitchenExcelExport.ExportKitchen(_kitchens);

            // Generate file name
            string fileName = _showDeleted ? "KITCHEN_MASTER_DELETED.xlsx" : "KITCHEN_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Kitchen data exported to Excel successfully.", "success");
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
            var stream = await KitchenPDFExport.ExportKitchen(_kitchens);

            // Generate file name
            string fileName = _showDeleted ? "KITCHEN_MASTER_DELETED.pdf" : "KITCHEN_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Kitchen data exported to PDF successfully.", "success");
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
            OnEditKitchen(selectedRecords[0]);
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