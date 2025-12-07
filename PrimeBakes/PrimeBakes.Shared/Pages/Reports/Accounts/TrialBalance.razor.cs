using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Accounts;

public partial class TrialBalance : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

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
    private ToastNotification _toastNotification;

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
        await StartAutoRefresh();
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
            StateHasChanged();
            await _toastNotification.ShowAsync("Loading", "Fetching trial balance...", ToastType.Info);

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
            await _toastNotification.ShowAsync("Error", $"Failed to load transactions: {ex.Message}", ToastType.Error);
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

    private async Task HandleDatesChanged((DateTime FromDate, DateTime ToDate) dates)
    {
        _fromDate = dates.FromDate;
        _toDate = dates.ToDate;
        await LoadTrialBalance();
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
            await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);

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
			await _toastNotification.ShowAsync("Exported", "Excel file downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"Excel export failed: {ex.Message}", ToastType.Error);
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
            await _toastNotification.ShowAsync("Exporting", "Generating PDF file...", ToastType.Info);

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
			await _toastNotification.ShowAsync("Exported", "PDF file downloaded successfully.", ToastType.Success);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("Error", $"PDF export failed: {ex.Message}", ToastType.Error);
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

	private async Task StartAutoRefresh()
	{
		var timerSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.AutoRefreshReportTimer);
		var refreshMinutes = int.TryParse(timerSetting?.Value, out var minutes) ? minutes : 5;

		_autoRefreshCts = new CancellationTokenSource();
		_autoRefreshTimer = new PeriodicTimer(TimeSpan.FromMinutes(refreshMinutes));
		_ = AutoRefreshLoop(_autoRefreshCts.Token);
	}

	private async Task AutoRefreshLoop(CancellationToken cancellationToken)
	{
		try
		{
			while (await _autoRefreshTimer.WaitForNextTickAsync(cancellationToken))
				await InvokeAsync(async () =>
				{
					await LoadTrialBalance();
				});
		}
		catch (OperationCanceledException)
		{
			// Timer was cancelled, expected on dispose
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_autoRefreshCts is not null)
		{
			await _autoRefreshCts.CancelAsync();
			_autoRefreshCts.Dispose();
		}

		_autoRefreshTimer?.Dispose();

		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}