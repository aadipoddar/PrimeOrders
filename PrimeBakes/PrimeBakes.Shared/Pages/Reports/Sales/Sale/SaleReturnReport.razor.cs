using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleReturnReport
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showDeleted = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private LocationModel _selectedLocation = new();
    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedParty = new();

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<SaleReturnOverviewModel> _saleReturnOverviews = [];

    private SfGrid<SaleReturnOverviewModel> _sfSaleReturnGrid;

    private string _errorTitle = string.Empty;
    private string _errorMessage = string.Empty;

    private string _successTitle = string.Empty;
    private string _successMessage = string.Empty;

    private string _deleteTransactionNo = string.Empty;
    private int _deleteTransactionId = 0;
    private bool _isDeleteDialogVisible = false;

    private string _recoverTransactionNo = string.Empty;
    private int _recoverTransactionId = 0;
    private bool _isRecoverDialogVisible = false;

    private SfToast _sfErrorToast;
    private SfToast _sfSuccessToast;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadLocations();
        await LoadCompanies();
        await LoadParties();
        await LoadSaleReturnOverviews();
    }

    private async Task LoadDates()
    {
        _fromDate = await CommonData.LoadCurrentDateTime();
        _toDate = _fromDate;
    }

    private async Task LoadLocations()
    {
        _locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
        _locations.Add(new()
        {
            Id = 0,
            Name = "All Locations"
        });
        _locations = [.. _locations.OrderBy(s => s.Name)];
        _selectedLocation = _locations.FirstOrDefault(_ => _.Id == _user.LocationId);
    }

    private async Task LoadCompanies()
    {
        _companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
        _companies.Add(new()
        {
            Id = 0,
            Name = "All Companies"
        });
        _companies = [.. _companies.OrderBy(s => s.Name)];
        _selectedCompany = _companies.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadParties()
    {
        _parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
        _parties.Add(new()
        {
            Id = 0,
            Name = "All Parties"
        });
        _parties = [.. _parties.OrderBy(s => s.Name)];
        _selectedParty = _parties.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadSaleReturnOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _saleReturnOverviews = await SaleReturnData.LoadSaleReturnOverviewByDate(
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
            !_showDeleted);

            if (_selectedLocation?.Id > 0)
                _saleReturnOverviews = [.. _saleReturnOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _saleReturnOverviews = [.. _saleReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _saleReturnOverviews = [.. _saleReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _saleReturnOverviews = [.. _saleReturnOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading sale return overviews: {ex.Message}", "error");
        }
        finally
        {
            if (_sfSaleReturnGrid is not null)
                await _sfSaleReturnGrid.Refresh();
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Changed Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadSaleReturnOverviews();
    }

    private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedLocation = args.Value;
        await LoadSaleReturnOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedCompany = args.Value;
        await LoadSaleReturnOverviews();
    }

    private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedParty = args.Value;
        await LoadSaleReturnOverviews();
    }

    private async Task SetDateRange(DateRangeType rangeType)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            var today = DateTime.Now.Date;
            var currentYear = today.Year;
            var currentMonth = today.Month;

            switch (rangeType)
            {
                case DateRangeType.Today:
                    _fromDate = today;
                    _toDate = today;
                    break;

                case DateRangeType.Yesterday:
                    _fromDate = today.AddDays(-1);
                    _toDate = today.AddDays(-1);
                    break;

                case DateRangeType.CurrentMonth:
                    _fromDate = new DateTime(currentYear, currentMonth, 1);
                    _toDate = _fromDate.AddMonths(1).AddDays(-1);
                    break;

                case DateRangeType.PreviousMonth:
                    _fromDate = new DateTime(_fromDate.Year, _fromDate.Month, 1).AddMonths(-1);
                    _toDate = _fromDate.AddMonths(1).AddDays(-1);
                    break;

                case DateRangeType.CurrentFinancialYear:
                    var currentFY = await FinancialYearData.LoadFinancialYearByDateTime(today);
                    _fromDate = currentFY.StartDate.ToDateTime(TimeOnly.MinValue);
                    _toDate = currentFY.EndDate.ToDateTime(TimeOnly.MaxValue);
                    break;

                case DateRangeType.PreviousFinancialYear:
                    currentFY = await FinancialYearData.LoadFinancialYearByDateTime(_fromDate);
                    var financialYears = await CommonData.LoadTableDataByStatus<FinancialYearModel>(TableNames.FinancialYear);
                    var previousFY = financialYears
                        .Where(fy => fy.Id != currentFY.Id)
                        .OrderByDescending(fy => fy.StartDate)
                        .FirstOrDefault();

                    if (previousFY is null)
                    {
                        await ShowToast("Warning", "No previous financial year found.", "error");
                        return;
                    }

                    _fromDate = previousFY.StartDate.ToDateTime(TimeOnly.MinValue);
                    _toDate = previousFY.EndDate.ToDateTime(TimeOnly.MaxValue);
                    break;

                case DateRangeType.AllTime:
                    _fromDate = new DateTime(2000, 1, 1);
                    _toDate = today;
                    break;
            }
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while setting date range: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            await LoadSaleReturnOverviews();
            StateHasChanged();
        }
    }
    #endregion

    #region Exporting
    private async Task ExportExcel(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            // Convert DateTime to DateOnly for Excel export
            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            // Determine if location should be shown (only for location ID 1 users)
            bool showLocation = _user.LocationId == 1;
            string locationName = _selectedLocation?.Id > 0 ? _selectedLocation.Name : null;

            // Call the Excel export utility
            var stream = await Task.Run(() =>
                SaleReturnReportExcelExport.ExportSaleReturnReport(
                    _saleReturnOverviews.Where(_ => _.Status),
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    showLocation,
                    locationName
                )
            );

            // Generate file name with date range
            string fileName = $"SALE_RETURN_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Sale return report exported to Excel successfully.", "success");
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

    private async Task ExportPdf(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            // Convert DateTime to DateOnly for PDF export
            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            // Determine if location should be shown (only for location ID 1 users)
            bool showLocation = _user.LocationId == 1;
            string locationName = _selectedLocation?.Id > 0 ? _selectedLocation.Name : null;

            // Call the PDF export utility
            var stream = await Task.Run(() =>
                SaleReturnReportPdfExport.ExportSaleReturnReport(
                    _saleReturnOverviews.Where(_ => _.Status),
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    showLocation,
                    locationName
                )
            );

            // Generate file name with date range
            string fileName = $"SALE_RETURN_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Sale return report exported to PDF successfully.", "success");
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

    #region Actions
    private async Task ViewSaleReturn(int saleId)
    {
        try
        {
            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{saleId}", "_blank");
            else
                NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{saleId}");
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while opening sale return: {ex.Message}", "error");
        }
    }

    private async Task DownloadInvoice(int saleReturnId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(saleReturnId);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while generating invoice: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ConfirmDelete()
    {
        if (_isProcessing)
            return;

        try
        {
            _isDeleteDialogVisible = false;
            _isProcessing = true;
            StateHasChanged();

            if (!_user.Admin || _user.LocationId > 1)
                throw new UnauthorizedAccessException("You do not have permission to delete this sale return transaction.");

            var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _deleteTransactionId);
            var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
            if (financialYear is null || financialYear.Locked || financialYear.Status == false)
                throw new InvalidOperationException("Cannot delete sale return transaction as the financial year is locked.");

            await SaleReturnData.DeleteSaleReturn(_deleteTransactionId);
            await ShowToast("Success", $"Sale return '{_deleteTransactionNo}' has been successfully deleted.", "success");

            _deleteTransactionId = 0;
            _deleteTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to delete purchase return: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadSaleReturnOverviews();
        }
    }

    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfSaleReturnGrid is not null)
            await _sfSaleReturnGrid.Refresh();
    }

    private async Task ToggleDeleted()
    {
        if (_user.LocationId > 1)
            return;

        _showDeleted = !_showDeleted;
        await LoadSaleReturnOverviews();
    }

    private async Task ConfirmRecover()
    {
        if (_isProcessing)
            return;

        try
        {
            _isRecoverDialogVisible = false;
            _isProcessing = true;
            StateHasChanged();

            if (!_user.Admin || _user.LocationId > 1)
                throw new UnauthorizedAccessException("You do not have permission to recover this sale return transaction.");

            var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _recoverTransactionId);

            // Recover the sale return transaction
            saleReturn.Status = true;
            saleReturn.LastModifiedBy = _user.Id;
            saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
            saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

            await SaleReturnData.RecoverSaleReturnTransaction(saleReturn);
            await ShowToast("Success", $"Transaction '{_recoverTransactionNo}' has been successfully recovered.", "success");

            _recoverTransactionId = 0;
            _recoverTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"Failed to recover sale return: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadSaleReturnOverviews();
        }
    }
    #endregion

    #region Utilities
    private async Task NavigateToSaleReturnPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SaleReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.SaleReturn);
    }

    private async Task NavigateToItemReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturnItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturnItem);
    }

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

    private void ShowDeleteConfirmation(int id, string transactionNo)
    {
        if (_user.LocationId > 1)
            return;

        _deleteTransactionId = id;
        _deleteTransactionNo = transactionNo;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _isDeleteDialogVisible = false;
        _deleteTransactionId = 0;
        _deleteTransactionNo = string.Empty;
        StateHasChanged();
    }

    private void ShowRecoverConfirmation(int id, string transactionNo)
    {
        if (_user.LocationId > 1)
            return;

        _recoverTransactionId = id;
        _recoverTransactionNo = transactionNo;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _isRecoverDialogVisible = false;
        _recoverTransactionId = 0;
        _recoverTransactionNo = string.Empty;
        StateHasChanged();
    }
    #endregion
}