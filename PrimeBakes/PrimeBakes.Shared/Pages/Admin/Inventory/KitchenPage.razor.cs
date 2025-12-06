using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;

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
    private DeleteConfirmationDialog _deleteConfirmationDialog;
    private RecoverConfirmationDialog _recoverConfirmationDialog;

    private int _deleteKitchenId = 0;
    private string _deleteKitchenName = string.Empty;

    private int _recoverKitchenId = 0;
    private string _recoverKitchenName = string.Empty;

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

    private async Task ShowDeleteConfirmation(int id, string name)
    {
        _deleteKitchenId = id;
        _deleteKitchenName = name;
        await _deleteConfirmationDialog.ShowAsync();
    }

    private async Task CancelDelete()
    {
        _deleteKitchenId = 0;
        _deleteKitchenName = string.Empty;
        await _deleteConfirmationDialog.HideAsync();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            await _deleteConfirmationDialog.HideAsync();

            var kitchen = _kitchens.FirstOrDefault(k => k.Id == _deleteKitchenId);
            if (kitchen == null)
            {
                await _toastNotification.ShowAsync("Error", "Kitchen not found.", ToastType.Error);
                return;
            }

            kitchen.Status = false;
            await KitchenData.InsertKitchen(kitchen);

            await _toastNotification.ShowAsync("Deleted", $"Kitchen '{kitchen.Name}' has been deleted successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete kitchen: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteKitchenId = 0;
            _deleteKitchenName = string.Empty;
        }
    }

    private async Task ShowRecoverConfirmation(int id, string name)
    {
        _recoverKitchenId = id;
        _recoverKitchenName = name;
        await _recoverConfirmationDialog.ShowAsync();
    }

    private async Task CancelRecover()
    {
        _recoverKitchenId = 0;
        _recoverKitchenName = string.Empty;
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

            var kitchen = _kitchens.FirstOrDefault(k => k.Id == _recoverKitchenId);
            if (kitchen == null)
            {
                await _toastNotification.ShowAsync("Error", "Kitchen not found.", ToastType.Error);
                return;
            }

            kitchen.Status = true;
            await KitchenData.InsertKitchen(kitchen);

            await _toastNotification.ShowAsync("Recovered", $"Kitchen '{kitchen.Name}' has been recovered successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover kitchen: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Validation", "Kitchen name is required. Please enter a valid kitchen name.", ToastType.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_kitchen.Remarks))
            _kitchen.Remarks = null;

        if (_kitchen.Id > 0)
        {
            var existingKitchen = _kitchens.FirstOrDefault(_ => _.Id != _kitchen.Id && _.Name.Equals(_kitchen.Name, StringComparison.OrdinalIgnoreCase));
            if (existingKitchen is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Kitchen name '{_kitchen.Name}' already exists. Please choose a different name.", ToastType.Warning);
                return false;
            }
        }
        else
        {
            var existingKitchen = _kitchens.FirstOrDefault(_ => _.Name.Equals(_kitchen.Name, StringComparison.OrdinalIgnoreCase));
            if (existingKitchen is not null)
            {
                await _toastNotification.ShowAsync("Duplicate", $"Kitchen name '{_kitchen.Name}' already exists. Please choose a different name.", ToastType.Warning);
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
            StateHasChanged();

            if (!await ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            await _toastNotification.ShowAsync("Processing", "Please wait while the kitchen is being saved...", ToastType.Info);

            await KitchenData.InsertKitchen(_kitchen);

            await _toastNotification.ShowAsync("Saved", $"Kitchen '{_kitchen.Name}' has been saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save kitchen: {ex.Message}", ToastType.Error);
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
            var stream = await KitchenExcelExport.ExportKitchen(_kitchens);

            // Generate file name
            string fileName = _showDeleted ? "KITCHEN_MASTER_DELETED.xlsx" : "KITCHEN_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Exported", "Kitchen data exported to Excel successfully.", ToastType.Success);
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
            var stream = await KitchenPDFExport.ExportKitchen(_kitchens);

            // Generate file name
            string fileName = _showDeleted ? "KITCHEN_MASTER_DELETED.pdf" : "KITCHEN_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Exported", "Kitchen data exported to PDF successfully.", ToastType.Success);
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