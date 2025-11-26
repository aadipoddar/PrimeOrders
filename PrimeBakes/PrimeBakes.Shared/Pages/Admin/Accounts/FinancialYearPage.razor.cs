using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class FinancialYearPage
{
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

        await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
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
                await ShowToast("Error", "Financial Year not found.", "error");
                return;
            }

            financialYear.Status = false;
            await FinancialYearData.InsertFinancialYear(financialYear);

            await ShowToast("Success", $"Financial Year '{_deleteFinancialYearName}' has been deleted successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to delete Financial Year: {ex.Message}", "error");
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
                await ShowToast("Error", "Financial Year not found.", "error");
                return;
            }

            financialYear.Status = true;
            await FinancialYearData.InsertFinancialYear(financialYear);

            await ShowToast("Success", $"Financial Year '{_recoverFinancialYearName}' has been recovered successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to recover Financial Year: {ex.Message}", "error");
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
            await ShowToast("Error", "Start date is required. Please select a valid start date.", "error");
            return false;
        }

        if (_financialYear.EndDate == default)
        {
            await ShowToast("Error", "End date is required. Please select a valid end date.", "error");
            return false;
        }

        if (_financialYear.EndDate <= _financialYear.StartDate)
        {
            await ShowToast("Error", "End date must be after start date.", "error");
            return false;
        }

        if (_financialYear.YearNo <= 0)
        {
            await ShowToast("Error", "Year number must be greater than 0.", "error");
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
            await ShowToast("Error", $"Date range overlaps with existing financial year ({overlapping.StartDate:dd-MMM-yyyy} to {overlapping.EndDate:dd-MMM-yyyy}).", "error");
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

            if (!await ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            await FinancialYearData.InsertFinancialYear(_financialYear);

            await ShowToast("Success", $"Financial Year '{_financialYear.StartDate:dd-MMM-yyyy} to {_financialYear.EndDate:dd-MMM-yyyy}' has been saved successfully.", "success");
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to save Financial Year: {ex.Message}", "error");
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
            var stream = await Task.Run(() => FinancialYearExcelExport.ExportFinancialYear(_financialYears));

            // Generate file name
            string fileName = "FINANCIAL_YEAR_MASTER.xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Financial Year data exported to Excel successfully.", "success");
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
            var stream = await Task.Run(() => FinancialYearPDFExport.ExportFinancialYear(_financialYears));

            // Generate file name
            string fileName = "FINANCIAL_YEAR_MASTER.pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Financial Year data exported to PDF successfully.", "success");
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