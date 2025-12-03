using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Order;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

using Toolbelt.Blazor.HotKeys2;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Order;

public partial class OrderItemReport : IAsyncDisposable
{
	[Inject] private HotKeys HotKeys { get; set; }
	private HotKeysContext _hotKeysContext;

	private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private LocationModel _selectedLocation = new();
    private CompanyModel _selectedCompany = new();
    private string _selectedOrderStatus = "All";

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<OrderItemOverviewModel> _transactionOverviews = [];

    private SfGrid<OrderItemOverviewModel> _sfGrid;

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

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
			.Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
            .Add(ModCode.Ctrl | ModCode.Shift, Code.E, ExportConsolidatedExcel, "Export Consolidated to Excel", Exclude.None)
            .Add(ModCode.Ctrl | ModCode.Shift, Code.P, ExportConsolidatedPdf, "Export Consolidated to PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistory, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadSelectedCartItemInvoice, "Download Selected Transaction Invoice", Exclude.None);

		await LoadDates();
        await LoadLocations();
        await LoadCompanies();
        await LoadTransactionOverviews();
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

    private async Task LoadTransactionOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

			_transactionOverviews = await CommonData.LoadTableDataByDate<OrderItemOverviewModel>(
				ViewNames.OrderItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (_selectedLocation?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            // Filter by order status
            if (_selectedOrderStatus == "Pending")
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.SaleId == null)];
            else if (_selectedOrderStatus == "Completed")
                _transactionOverviews = [.. _transactionOverviews.Where(_ => _.SaleId != null)];

            _transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
			await ShowToast("Error", $"An error occurred while loading transaction overviews: {ex.Message}", "error");
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
        await LoadTransactionOverviews();
    }

    private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedLocation = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedCompany = args.Value;
        await LoadTransactionOverviews();
    }

    private async Task OnOrderStatusChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, string> args)
    {
        _selectedOrderStatus = args.Value;
        await LoadTransactionOverviews();
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
            await LoadTransactionOverviews();
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

            var stream = await OrderItemReportExcelExport.ExportOrderItemReport(
                    _transactionOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _user.LocationId == 1,
                    _selectedLocation?.Name
                );

            string fileName = $"ORDER_ITEM_REPORT";
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

            var stream = await OrderItemReportPdfExport.ExportOrderItemReport(
                    _transactionOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _user.LocationId == 1,
                    _selectedLocation?.Name
                );

            string fileName = $"ORDER_ITEM_REPORT";
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

    private async Task ExportConsolidatedExcel()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var stream = await OrderItemConsolidatedExcelExport.ExportConsolidatedOrderItemReport(
                    _transactionOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _selectedLocation?.Name
                );

            string fileName = $"ORDER_ITEM_CONSOLIDATED_REPORT";
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

    private async Task ExportConsolidatedPdf()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
            DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

            var stream = await OrderItemConsolidatedPdfExport.ExportConsolidatedOrderItemReport(
                    _transactionOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _selectedLocation?.Name
                );

            string fileName = $"ORDER_ITEM_CONSOLIDATED_REPORT";
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

	#region Actions
	private async Task ViewSelectedCartItem()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await ViewTransaction(selectedCartItem.MasterId);
	}

	private async Task ViewTransaction(int transactionId)
    {
        try
        {
            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Order}/{transactionId}", "_blank");
            else
                NavigationManager.NavigateTo($"{PageRouteNames.Order}/{transactionId}");
        }
        catch (Exception ex)
        {
			await ShowToast("Error", $"An error occurred while opening transaction: {ex.Message}", "error");
        }
    }

	private async Task DownloadSelectedCartItemInvoice()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await DownloadInvoice(selectedCartItem.MasterId);
	}

	private async Task DownloadInvoice(int transactionId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            var (pdfStream, fileName) = await OrderData.GenerateAndDownloadInvoice(transactionId);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await ShowToast("Success", "Invoice downloaded successfully.", "success");
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

        if (_sfGrid is not null)
            await _sfGrid.Refresh();
    }
    #endregion

    #region Utilities
    private async Task NavigateToTransactionPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Order, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Order);
    }

    private async Task NavigateToTransactionHistory()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrder, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportOrder);
    }

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

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