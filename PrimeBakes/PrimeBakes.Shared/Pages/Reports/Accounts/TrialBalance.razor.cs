using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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

public partial class TrialBalance : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private GroupModel _selectedGroup = new();
    private AccountTypeModel _selectedAccountType = new();

    private List<GroupModel> _groups = [];
    private List<AccountTypeModel> _accountTypes = [];
    private List<TrialBalanceModel> _trialBalance = [];

    private SfGrid<TrialBalanceModel> _sfGrid;

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
        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.R, LoadTrialBalance, "Refresh Data", Exclude.None)
            .Add(Code.F5, LoadTrialBalance, "Refresh Data", Exclude.None)
            .Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
            .Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
            .Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
            .Add(ModCode.Ctrl, Code.I, NavigateToLedgerReport, "Ledger Report", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None);

		await LoadDates();
        await LoadGroups();
        await LoadAccountTypes();
        await LoadTrialBalance();
    }

    private async Task LoadDates()
    {
        _fromDate = await CommonData.LoadCurrentDateTime();
        _toDate = _fromDate;
    }

    private async Task LoadGroups()
    {
        _groups = await CommonData.LoadTableDataByStatus<GroupModel>(TableNames.Group);
        _groups.Add(new()
        {
            Id = 0,
            Name = "All Groups"
        });

        _groups = [.. _groups.OrderBy(s => s.Name)];
        _selectedGroup = _groups.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadAccountTypes()
    {
        _accountTypes = await CommonData.LoadTableDataByStatus<AccountTypeModel>(TableNames.AccountType);
        _accountTypes.Add(new()
        {
            Id = 0,
            Name = "All Account Types"
        });
        _accountTypes = [.. _accountTypes.OrderBy(s => s.Name)];
        _selectedAccountType = _accountTypes.FirstOrDefault(_ => _.Id == 0);
    }

    private async Task LoadTrialBalance()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _trialBalance = await AccountingData.LoadTrialBalanceByDate(
                DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
                DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (_selectedGroup?.Id > 0)
                _trialBalance = [.. _trialBalance.Where(_ => _.GroupId == _selectedGroup.Id)];

            if (_selectedAccountType?.Id > 0)
                _trialBalance = [.. _trialBalance.Where(_ => _.AccountTypeId == _selectedAccountType.Id)];

            _trialBalance = [.. _trialBalance.OrderBy(_ => _.LedgerName)];
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading trial balance: {ex.Message}", "error");
        }
        finally
        {
            if (_sfGrid is not null)
                await _sfGrid.Refresh();
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
        await LoadTrialBalance();
    }

    private async Task OnAccountTypeChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<AccountTypeModel, AccountTypeModel> args)
    {
        _selectedAccountType = args.Value;
        await LoadTrialBalance();
    }

    private async Task OnGroupChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<GroupModel, GroupModel> args)
    {
        _selectedGroup = args.Value;
        await LoadTrialBalance();
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
            await LoadTrialBalance();
            StateHasChanged();
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var stream = await TrialBalanceExcelExport.ExportTrialBalance(
                    _trialBalance,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedGroup?.Id > 0 ? _selectedGroup?.Name : null,
                    _selectedAccountType?.Id > 0 ? _selectedAccountType?.Name : null
                );

            string fileName = $"TRIAL_BALANCE";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            await SaveAndViewService.SaveAndView(fileName, stream);
			await ShowToast("Success", "Transaction report exported to Excel successfully.", "success");
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

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var stream = await TrialBalancePdfExport.ExportTrialBalance(
                    _trialBalance,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _selectedGroup?.Id > 0 ? _selectedGroup?.Name : null,
                    _selectedAccountType?.Id > 0 ? _selectedAccountType?.Name : null
                );

            string fileName = $"TRIAL_BALANCE";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            await SaveAndViewService.SaveAndView(fileName, stream);
			await ShowToast("Success", "Transaction report exported to PDF successfully.", "success");
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
    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }

    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.FinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting);
    }

    private async Task NavigateToLedgerReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportAccountingLedger, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportAccountingLedger);
    }

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

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

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}