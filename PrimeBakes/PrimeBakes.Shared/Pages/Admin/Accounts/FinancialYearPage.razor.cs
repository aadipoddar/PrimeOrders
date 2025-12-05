using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class FinancialYearPage : IAsyncDisposable
{
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showDeleted = false;

    private FinancialYearModel _financialYear = new();

    private List<FinancialYearModel> _financialYears = [];

    private SfGrid<FinancialYearModel> _sfGrid;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    private int _deleteFinancialYearId = 0;
    private string _deleteFinancialYearName = string.Empty;
    private bool _isDeleteDialogVisible = false;

    private int _recoverFinancialYearId = 0;
    private string _recoverFinancialYearName = string.Empty;
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
            .Add(ModCode.Ctrl, Code.S, SaveFinancialYear, "Save", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true), "New", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
            .Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
            .Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

        _financialYears = await CommonData.LoadTableData<FinancialYearModel>(TableNames.FinancialYear);

        if (!_showDeleted)
            _financialYears = [.. _financialYears.Where(g => g.Status)];

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Actions
    private void OnEditFinancialYear(FinancialYearModel financialYear)
    {
        _financialYear = new()
        {
            Id = financialYear.Id,
            StartDate = financialYear.StartDate,
            EndDate = financialYear.EndDate,
            YearNo = financialYear.YearNo,
            Remarks = financialYear.Remarks,
            Locked = financialYear.Locked,
            Status = financialYear.Status
        };

        StateHasChanged();
    }

    private string GetFinancialYearName(FinancialYearModel fy)
    {
        return $"{fy.StartDate:dd-MMM-yyyy} to {fy.EndDate:dd-MMM-yyyy}";
    }

    private void AutoGenerateNextYear()
    {
        if (_financialYears.Count == 0)
        {
            // No existing financial years, start with a default
            _financialYear.StartDate = new DateOnly(DateTime.Now.Year, 4, 1);
            _financialYear.EndDate = new DateOnly(DateTime.Now.Year + 1, 3, 31);
            _financialYear.YearNo = 1;
        }
        else
        {
            // Find the latest financial year by end date
            var latestYear = _financialYears
                .Where(fy => fy.Status)
                .OrderByDescending(fy => fy.EndDate)
                .FirstOrDefault();

            if (latestYear != null)
            {
                // Generate next year based on latest
                _financialYear.StartDate = latestYear.EndDate.AddDays(1);
                _financialYear.EndDate = latestYear.EndDate.AddYears(1);
                _financialYear.YearNo = latestYear.YearNo + 1;
            }
            else
            {
                // Fallback if no active years exist
                _financialYear.StartDate = new DateOnly(DateTime.Now.Year, 4, 1);
                _financialYear.EndDate = new DateOnly(DateTime.Now.Year + 1, 3, 31);
                _financialYear.YearNo = 1;
            }
        }

        _financialYear.Locked = false;
        _financialYear.Remarks = string.Empty;
        _financialYear.Id = 0;
        _financialYear.Status = true;

        StateHasChanged();
    }

    private void ShowDeleteConfirmation(int id, string name)
    {
        _deleteFinancialYearId = id;
        _deleteFinancialYearName = name;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _deleteFinancialYearId = 0;
        _deleteFinancialYearName = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private async Task ConfirmDelete()
    {
        try
        {
            _isProcessing = true;
            _isDeleteDialogVisible = false;

            var financialYear = _financialYears.FirstOrDefault(g => g.Id == _deleteFinancialYearId);
            if (financialYear == null)
            {
                await _toastNotification.ShowAsync("Error", "Financial Year not found.", ToastType.Error);
                return;
            }

            financialYear.Status = false;
            await FinancialYearData.InsertFinancialYear(financialYear);

            await _toastNotification.ShowAsync("Success", $"Financial Year '{_deleteFinancialYearName}' has been deleted successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to delete Financial Year: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _deleteFinancialYearId = 0;
            _deleteFinancialYearName = string.Empty;
        }
    }

    private void ShowRecoverConfirmation(int id, string name)
    {
        _recoverFinancialYearId = id;
        _recoverFinancialYearName = name;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _recoverFinancialYearId = 0;
        _recoverFinancialYearName = string.Empty;
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

            var financialYear = _financialYears.FirstOrDefault(g => g.Id == _recoverFinancialYearId);
            if (financialYear == null)
            {
                await _toastNotification.ShowAsync("Error", "Financial Year not found.", ToastType.Error);
                return;
            }

            financialYear.Status = true;
            await FinancialYearData.InsertFinancialYear(financialYear);

            await _toastNotification.ShowAsync("Success", $"Financial Year '{_recoverFinancialYearName}' has been recovered successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to recover Financial Year: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
            _recoverFinancialYearId = 0;
            _recoverFinancialYearName = string.Empty;
        }
    }
    #endregion

    #region Saving
    private async Task<bool> ValidateForm()
    {
        _financialYear.Remarks = _financialYear.Remarks?.Trim() ?? "";
        _financialYear.Status = true;

        if (_financialYear.StartDate == default)
        {
            await _toastNotification.ShowAsync("Error", "Start date is required. Please select a valid start date.", ToastType.Error);
            return false;
        }

        if (_financialYear.EndDate == default)
        {
            await _toastNotification.ShowAsync("Error", "End date is required. Please select a valid end date.", ToastType.Error);
            return false;
        }

        if (_financialYear.EndDate <= _financialYear.StartDate)
        {
            await _toastNotification.ShowAsync("Error", "End date must be after start date.", ToastType.Error);
            return false;
        }

        if (_financialYear.YearNo <= 0)
        {
            await _toastNotification.ShowAsync("Error", "Year number must be greater than 0.", ToastType.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_financialYear.Remarks))
            _financialYear.Remarks = null;

        // Check for overlapping date ranges
        var overlapping = _financialYears.FirstOrDefault(fy =>
            fy.Id != _financialYear.Id &&
            fy.Status &&
            ((fy.StartDate <= _financialYear.StartDate && fy.EndDate >= _financialYear.StartDate) ||
             (fy.StartDate <= _financialYear.EndDate && fy.EndDate >= _financialYear.EndDate) ||
             (_financialYear.StartDate <= fy.StartDate && _financialYear.EndDate >= fy.EndDate)));

        if (overlapping is not null)
        {
            await _toastNotification.ShowAsync("Error", $"Date range overlaps with existing financial year ({overlapping.StartDate:dd-MMM-yyyy} to {overlapping.EndDate:dd-MMM-yyyy}).", ToastType.Error);
            return false;
        }

        return true;
    }

    private async Task SaveFinancialYear()
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

            await FinancialYearData.InsertFinancialYear(_financialYear);

            await _toastNotification.ShowAsync("Success", $"Financial Year '{_financialYear.StartDate:dd-MMM-yyyy} to {_financialYear.EndDate:dd-MMM-yyyy}' has been saved successfully.", ToastType.Success);
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Failed to save Financial Year: {ex.Message}", ToastType.Error);
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
            var stream = await FinancialYearExcelExport.ExportFinancialYear(_financialYears);

            // Generate file name
            string fileName = "FINANCIAL_YEAR_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Financial Year data exported to Excel successfully.", ToastType.Success);
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
            var stream = await FinancialYearPDFExport.ExportFinancialYear(_financialYears);

            // Generate file name
            string fileName = "FINANCIAL_YEAR_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await _toastNotification.ShowAsync("Success", "Financial Year data exported to PDF successfully.", ToastType.Success);
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
            OnEditFinancialYear(selectedRecords[0]);
    }

    private async Task DeleteSelectedItem()
    {
        var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
        if (selectedRecords.Count > 0)
        {
            if (selectedRecords[0].Status)
                ShowDeleteConfirmation(selectedRecords[0].Id, GetFinancialYearName(selectedRecords[0]));
            else
                ShowRecoverConfirmation(selectedRecords[0].Id, GetFinancialYearName(selectedRecords[0]));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hotKeysContext.DisposeAsync();
    }
}