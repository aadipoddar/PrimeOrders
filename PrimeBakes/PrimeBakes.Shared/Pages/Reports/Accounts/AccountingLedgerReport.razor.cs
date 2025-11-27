using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Reports.Accounts;

public partial class AccountingLedgerReport
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedLedger = new();
    private TrialBalanceModel _selectedTrialBalance = new();

    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _ledgers = [];
    private List<AccountingLedgerOverviewModel> _accountingLedgerOverviews = [];

    private SfGrid<AccountingLedgerOverviewModel> _sfAccountingLedgerGrid;

    private string _errorTitle = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successTitle = string.Empty;
    private string _successMessage = string.Empty;

    private SfToast _sfErrorToast;
    private SfToast _sfSuccessToast;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadCompanies();
        await LoadLedgers();
        await LoadAccountingLedgerOverviews();
    }

    private async Task LoadDates()
    {
        _fromDate = await CommonData.LoadCurrentDateTime();
        _toDate = _fromDate;
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

    private async Task LoadLedgers()
    {
        _ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
        _ledgers.Add(new()
        {
            Id = 0,
            Name = "All Ledgers"
        });

        _ledgers = [.. _ledgers.OrderBy(s => s.Name)];
        _selectedLedger = _ledgers.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadAccountingLedgerOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _accountingLedgerOverviews = await AccountingData.LoadAccountingLedgerOverviewByDate(
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (_selectedCompany?.Id > 0)
                _accountingLedgerOverviews = [.. _accountingLedgerOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            // Filter by ledger with contra ledger details
            if (_selectedLedger?.Id > 0)
            {
                List<AccountingLedgerOverviewModel> filteredOverviews = [];
                var partyLedgers = _accountingLedgerOverviews.Where(l => l.Id == _selectedLedger.Id).ToList();

                foreach (var item in partyLedgers)
                {
                    var referenceLedgers = _accountingLedgerOverviews
                        .Where(l => l.AccountingId == item.AccountingId && l.Id != _selectedLedger.Id)
                        .ToList();

                    var referenceLedgerNamesWithAmount = string.Join("\n",
                        referenceLedgers.Select(l =>
                        $"{l.LedgerName}\t({(l.Debit.HasValue && l.Debit.Value > 0 ? "Dr " + l.Debit.Value.FormatIndianCurrency() : l.Credit.HasValue && l.Credit.Value > 0 ? "Cr " + l.Credit.Value.FormatIndianCurrency() : "0.00")})")); item.LedgerName = referenceLedgerNamesWithAmount;
                    filteredOverviews.Add(item);
                }

                _accountingLedgerOverviews = filteredOverviews;

                var trialBalances = await AccountingData.LoadTrialBalanceByDate(
                    DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                    DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

                _selectedTrialBalance = trialBalances.FirstOrDefault(tb => tb.LedgerId == _selectedLedger.Id);
            }

            _accountingLedgerOverviews = [.. _accountingLedgerOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading accounting ledger overviews: {ex.Message}", "error");
        }
        finally
        {
            if (_sfAccountingLedgerGrid is not null)
                await _sfAccountingLedgerGrid.Refresh();
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Change Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadAccountingLedgerOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        _selectedCompany = args.Value;
        await LoadAccountingLedgerOverviews();
    }

    private async Task OnLedgerChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        _selectedLedger = args.Value;
        await LoadAccountingLedgerOverviews();
    }

    private async Task SetDateRange(DateRangeType rangeType)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            var today = await CommonData.LoadCurrentDateTime();
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

                    if (previousFY == null)
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
            await LoadAccountingLedgerOverviews();
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var stream = await Task.Run(() =>
                AccountingLedgerReportExcelExport.ExportAccountingLedgerReport(
                    _accountingLedgerOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany?.Name : null,
                    _selectedLedger?.Id > 0 ? _selectedLedger?.Name : null,
                    _selectedLedger?.Id > 0 ? _selectedTrialBalance : null
                )
            );

            string fileName = $"LEDGER_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Ledger report exported to Excel successfully.", "success");
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var stream = await Task.Run(() =>
                AccountingLedgerReportPdfExport.ExportAccountingLedgerReport(
                    _accountingLedgerOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedCompany?.Id > 0 ? _selectedCompany?.Name : null,
                    _selectedLedger?.Id > 0 ? _selectedLedger?.Name : null,
                    _selectedLedger?.Id > 0 ? _selectedTrialBalance : null
                )
            );

            string fileName = $"LEDGER_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Ledger report exported to PDF successfully.", "success");
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
    private async Task ViewAccounting(int accountingId)
    {
        try
        {
            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.FinancialAccounting}/{accountingId}", "_blank");
            else
                NavigationManager.NavigateTo($"{PageRouteNames.FinancialAccounting}/{accountingId}");
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while opening accounting: {ex.Message}", "error");
        }
    }

    private async Task DownloadInvoice(int accountingId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            var (pdfStream, fileName) = await AccountingData.GenerateAndDownloadInvoice(accountingId);
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

    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfAccountingLedgerGrid is not null)
            await _sfAccountingLedgerGrid.Refresh();
    }
    #endregion

    #region Utilities
    private async Task NavigateToAccountingPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.FinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting);
    }

    private async Task NavigateToAccountingReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportFinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportFinancialAccounting);
    }

    private void NavigateToTrialBalance()
    {
        NavigationManager.NavigateTo(PageRouteNames.ReportTrialBalance);
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
    #endregion
}