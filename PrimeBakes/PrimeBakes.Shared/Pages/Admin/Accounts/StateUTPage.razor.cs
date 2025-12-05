using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class StateUTPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private StateUTModel _stateUT = new();

    private List<StateUTModel> _stateUTs = [];

    private SfGrid<StateUTModel> _sfGrid;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    private int _deleteStateUTId = 0;
    private string _deleteStateUTName = string.Empty;
    private bool _isDeleteDialogVisible = false;

    private int _recoverStateUTId = 0;
    private string _recoverStateUTName = string.Empty;
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
            .Add(ModCode.Ctrl, Code.S, SaveStateUT, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminStateUT, true), "New", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

        if (!_showDeleted)
            _stateUTs = [.. _stateUTs.Where(g => g.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnEditStateUT(StateUTModel stateUT)
    {
        _stateUT = new()
        {
            Id = stateUT.Id,
            Name = stateUT.Name,
            Remarks = stateUT.Remarks,
            UnionTerritory = stateUT.UnionTerritory,
            Status = stateUT.Status
        };

        StateHasChanged();
    }
    private void ShowDeleteConfirmation(int id, string name)
    {
        _deleteStateUTId = id;
        _deleteStateUTName = name;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _deleteStateUTId = 0;
        _deleteStateUTName = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            _isDeleteDialogVisible = false;

            var stateUT = _stateUTs.FirstOrDefault(g => g.Id == _deleteStateUTId);
            if (stateUT == null)
            {
                await _toastNotification.ShowAsync("Error", "State/UT not found.", ToastType.Error);
                return;
            }

            stateUT.Status = false;
            await StateUTData.InsertStateUT(stateUT);

            await _toastNotification.ShowAsync("Success", $"State/UT '{stateUT.Name}' has been deleted successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminStateUT, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete State/UT: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteStateUTId = 0;
            _deleteStateUTName = string.Empty;
        }
    }

    private void ShowRecoverConfirmation(int id, string name)
    {
        _recoverStateUTId = id;
        _recoverStateUTName = name;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _recoverStateUTId = 0;
        _recoverStateUTName = string.Empty;
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

            var stateUT = _stateUTs.FirstOrDefault(g => g.Id == _recoverStateUTId);
            if (stateUT == null)
            {
                await _toastNotification.ShowAsync("Error", "State/UT not found.", ToastType.Error);
                return;
            }

            stateUT.Status = true;
            await StateUTData.InsertStateUT(stateUT);

            await _toastNotification.ShowAsync("Success", $"State/UT '{stateUT.Name}' has been recovered successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminStateUT, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover State/UT: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverStateUTId = 0;
            _recoverStateUTName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _stateUT.Name = _stateUT.Name?.Trim() ?? "";
        _stateUT.Name = _stateUT.Name?.ToUpper() ?? "";

        _stateUT.Remarks = _stateUT.Remarks?.Trim() ?? "";
        _stateUT.Status = true;

        if (string.IsNullOrWhiteSpace(_stateUT.Name))
        {
            await _toastNotification.ShowAsync("Error", "State/UT name is required. Please enter a valid state/UT name.", ToastType.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_stateUT.Remarks))
            _stateUT.Remarks = null;

        if (_stateUT.Id > 0)
        {
            var existingStateUT = _stateUTs.FirstOrDefault(_ => _.Id != _stateUT.Id && _.Name.Equals(_stateUT.Name, StringComparison.OrdinalIgnoreCase));
            if (existingStateUT is not null)
            {
                await _toastNotification.ShowAsync("Error", $"State/UT name '{_stateUT.Name}' already exists. Please choose a different name.", ToastType.Error);
                return false;
            }
        }
        else
        {
            var existingStateUT = _stateUTs.FirstOrDefault(_ => _.Name.Equals(_stateUT.Name, StringComparison.OrdinalIgnoreCase));
            if (existingStateUT is not null)
            {
                await _toastNotification.ShowAsync("Error", $"State/UT name '{_stateUT.Name}' already exists. Please choose a different name.", ToastType.Error);
                return false;
            }
        }

        return true;
    }

    private async Task SaveStateUT()
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

            await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

            await StateUTData.InsertStateUT(_stateUT);

            await _toastNotification.ShowAsync("Success", $"State/UT '{_stateUT.Name}' has been saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminStateUT, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save State/UT: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Processing", "Exporting to Excel...", ToastType.Info);

            // Call the Excel export utility
            var stream = await StateUTExcelExport.ExportStateUT(_stateUTs);

            // Generate file name
            string fileName = "STATE_UT_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "State/UT data exported to Excel successfully.", ToastType.Success);
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
            await _toastNotification.ShowAsync("Processing", "Exporting to PDF...", ToastType.Info);

            // Call the PDF export utility
            var stream = await StateUTPDFExport.ExportStateUT(_stateUTs);

            // Generate file name
            string fileName = "STATE_UT_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "State/UT data exported to PDF successfully.", ToastType.Success);
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
            OnEditStateUT(selectedRecords[0]);
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