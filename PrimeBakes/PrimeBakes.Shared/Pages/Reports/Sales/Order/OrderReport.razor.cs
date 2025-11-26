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
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Order;

public partial class OrderReport
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showDeleted = false;
    private bool _isDeleteDialogVisible = false;
    private bool _isRecoverDialogVisible = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private LocationModel _selectedLocation = new();
    private CompanyModel _selectedCompany = new();
    private string _selectedOrderStatus = "All";

    private List<LocationModel> _locations = [];
    private List<CompanyModel> _companies = [];
    private List<OrderOverviewModel> _orderOverviews = [];

    private SfGrid<OrderOverviewModel> _sfOrderGrid;

    private string _errorTitle = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successTitle = string.Empty;
    private string _successMessage = string.Empty;
    private string _deleteTransactionNo = string.Empty;
    private int _deleteTransactionId = 0;
    private string _recoverTransactionNo = string.Empty;
    private int _recoverTransactionId = 0;

    private SfToast _sfErrorToast;
    private SfToast _sfSuccessToast;
    private SfDialog _deleteConfirmationDialog;
    private SfDialog _recoverConfirmationDialog;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
        _user = authResult.User;
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadLocations();
        await LoadCompanies();
        await LoadOrderOverviews();
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

    private async Task LoadOrderOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _orderOverviews = await OrderData.LoadOrderOverviewByDate(
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
            !_showDeleted);

            if (_selectedLocation?.Id > 0)
                _orderOverviews = [.. _orderOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _orderOverviews = [.. _orderOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            // Filter by order status
            if (_selectedOrderStatus == "Pending")
                _orderOverviews = [.. _orderOverviews.Where(_ => _.SaleId == null)];
            else if (_selectedOrderStatus == "Completed")
                _orderOverviews = [.. _orderOverviews.Where(_ => _.SaleId != null)];

            _orderOverviews = [.. _orderOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading sale overviews: {ex.Message}", "error");
        }
        finally
        {
            if (_sfOrderGrid is not null)
                await _sfOrderGrid.Refresh();
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
        await LoadOrderOverviews();
    }

    private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedLocation = args.Value;
        await LoadOrderOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedCompany = args.Value;
        await LoadOrderOverviews();
    }

    private async Task OnOrderStatusChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, string> args)
    {
        _selectedOrderStatus = args.Value;
        await LoadOrderOverviews();
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
            await LoadOrderOverviews();
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

            // Call the Excel export utility
            var stream = await Task.Run(() =>
                OrderReportExcelExport.ExportOrderReport(
                    _orderOverviews.Where(_ => _.Status),
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _user.LocationId == 1,
                    _selectedLocation?.Name
                )
            );

            // Generate file name with date range
            string fileName = $"ORDER_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            // Save and view the Excel file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Order report exported to Excel successfully.", "success");
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

            // Call the PDF export utility
            var stream = await Task.Run(() =>
                OrderReportPdfExport.ExportOrderReport(
                    _orderOverviews.Where(_ => _.Status),
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns,
                    _user.LocationId == 1,
                    _selectedLocation?.Name
                )
            );

            // Generate file name with date range
            string fileName = $"ORDER_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            // Save and view the PDF file
            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Order report exported to PDF successfully.", "success");
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
    private async Task ViewOrder(int orderId)
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Order}/{orderId}", "_blank");
        else
            NavigationManager.NavigateTo($"{PageRouteNames.Order}/{orderId}");
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
                throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

            var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _deleteTransactionId);
            if (order.SaleId is not null && order.SaleId > 0)
                throw new InvalidOperationException("Cannot delete order linked to a sale transaction. Please unlink the sale first.");

            await OrderData.DeleteOrder(_deleteTransactionId);

            await ShowToast("Success", $"Order transaction {_deleteTransactionNo} has been deleted successfully.", "success");

            _deleteTransactionId = 0;
            _deleteTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while deleting order transaction: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadOrderOverviews();
        }
    }

    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfOrderGrid is not null)
            await _sfOrderGrid.Refresh();
    }

    private async Task ToggleDeleted()
    {
        if (_user.LocationId > 1)
            return;

        _showDeleted = !_showDeleted;
        await LoadOrderOverviews();
        StateHasChanged();
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
                throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

            if (_recoverTransactionId == 0)
            {
                await ShowToast("Error", "Invalid sale transaction selected for recovery.", "error");
                return;
            }

            var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, _recoverTransactionId);
            if (order is null)
            {
                await ShowToast("Error", "Order transaction not found.", "error");
                return;
            }

            // Update the Status to true (active)
            order.Status = true;
            order.LastModifiedBy = _user.Id;
            order.LastModifiedAt = await CommonData.LoadCurrentDateTime();
            order.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

            await OrderData.RecoverOrderTransaction(order);

            await ShowToast("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", "success");

            _recoverTransactionId = 0;
            _recoverTransactionNo = string.Empty;
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while recovering sale transaction: {ex.Message}", "error");
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
            await LoadOrderOverviews();
        }
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

    private async Task NavigateToItemReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrderItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportOrderItem);
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
        _deleteTransactionId = id;
        _deleteTransactionNo = transactionNo;
        _isDeleteDialogVisible = true;
        StateHasChanged();
    }

    private void CancelDelete()
    {
        _deleteTransactionId = 0;
        _deleteTransactionNo = string.Empty;
        _isDeleteDialogVisible = false;
        StateHasChanged();
    }

    private void ShowRecoverConfirmation(int id, string transactionNo)
    {
        _recoverTransactionId = id;
        _recoverTransactionNo = transactionNo;
        _isRecoverDialogVisible = true;
        StateHasChanged();
    }

    private void CancelRecover()
    {
        _recoverTransactionId = 0;
        _recoverTransactionNo = string.Empty;
        _isRecoverDialogVisible = false;
        StateHasChanged();
    }
    #endregion
}