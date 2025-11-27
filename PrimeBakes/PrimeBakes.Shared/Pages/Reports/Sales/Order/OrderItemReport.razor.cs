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

namespace PrimeBakes.Shared.Pages.Reports.Sales.Order;

public partial class OrderItemReport
{
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
    private List<OrderItemOverviewModel> _orderItemOverviews = [];

    private SfGrid<OrderItemOverviewModel> _sfOrderItemGrid;

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
        await LoadDates();
        await LoadLocations();
        await LoadCompanies();
        await LoadOrderItemOverviews();
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

    private async Task LoadOrderItemOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _orderItemOverviews = await OrderData.LoadOrderItemOverviewByDate(
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (_selectedLocation?.Id > 0)
                _orderItemOverviews = [.. _orderItemOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _orderItemOverviews = [.. _orderItemOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            // Filter by order status
            if (_selectedOrderStatus == "Pending")
                _orderItemOverviews = [.. _orderItemOverviews.Where(_ => _.SaleId == null)];
            else if (_selectedOrderStatus == "Completed")
                _orderItemOverviews = [.. _orderItemOverviews.Where(_ => _.SaleId != null)];

            _orderItemOverviews = [.. _orderItemOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading purchase item overviews: {ex.Message}", "error");
        }
        finally
        {
            if (_sfOrderItemGrid is not null)
                await _sfOrderItemGrid.Refresh();
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
        await LoadOrderItemOverviews();
    }

    private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedLocation = args.Value;
        await LoadOrderItemOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedCompany = args.Value;
        await LoadOrderItemOverviews();
    }

    private async Task OnOrderStatusChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, string> args)
    {
        _selectedOrderStatus = args.Value;
        await LoadOrderItemOverviews();
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
            await LoadOrderItemOverviews();
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
            OrderItemReportExcelExport.ExportOrderItemReport(
                    _orderItemOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _user.LocationId == 1,
                    _selectedLocation?.Name
                )
            );

            string fileName = $"ORDER_ITEM_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Order item report exported to Excel successfully.", "success");
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
            OrderItemReportPdfExport.ExportOrderItemReport(
            _orderItemOverviews,
            dateRangeStart,
            dateRangeEnd,
            _showAllColumns,
            _user.LocationId == 1,
            _selectedLocation?.Name
            )
            );

            string fileName = $"ORDER_ITEM_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Order item report exported to PDF successfully.", "success");
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

    private async Task ExportConsolidatedExcel(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
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
            OrderItemConsolidatedExcelExport.ExportConsolidatedOrderItemReport(
                    _orderItemOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _selectedLocation?.Name
                )
            );

            string fileName = $"ORDER_ITEM_CONSOLIDATED_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Consolidated order item report exported to Excel successfully.", "success");
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while exporting consolidated report to Excel: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task ExportConsolidatedPdf(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
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
            OrderItemConsolidatedPdfExport.ExportConsolidatedOrderItemReport(
                _orderItemOverviews,
                dateRangeStart,
                dateRangeEnd,
                _selectedLocation?.Name
                )
            );

            string fileName = $"ORDER_ITEM_CONSOLIDATED_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Consolidated order item report exported to PDF successfully.", "success");
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while exporting consolidated report to PDF: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Actions
    private async Task ViewOrder(int orderId)
    {
        try
        {
            if (FormFactor.GetFormFactor() == "Web")
                await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Order}/{orderId}", "_blank");
            else
                NavigationManager.NavigateTo($"{PageRouteNames.Order}/{orderId}");
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while opening order: {ex.Message}", "error");
        }
    }

    private async Task DownloadInvoice(int orderId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            var (pdfStream, fileName) = await OrderData.GenerateAndDownloadInvoice(orderId);
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

        if (_sfOrderItemGrid is not null)
            await _sfOrderItemGrid.Refresh();
    }
    #endregion

    #region Utilities
    private async Task NavigateToOrderPage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Order, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Order);
    }

    private async Task NavigateToOrderReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrder, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportOrder);
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