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

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleReturnItemReport
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();
    private LocationModel _selectedLocation = new();
    private LedgerModel _selectedParty = new();

    private List<CompanyModel> _companies = [];
    private List<LocationModel> _locations = [];
    private List<LedgerModel> _parties = [];
    private List<SaleReturnItemOverviewModel> _saleReturnItemOverviews = [];

    private SfGrid<SaleReturnItemOverviewModel> _sfSaleReturnItemGrid;

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
        await LoadSaleReturnItemOverviews();
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

    private async Task LoadSaleReturnItemOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _saleReturnItemOverviews = await SaleReturnData.LoadSaleReturnItemOverviewByDate(
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (_selectedLocation?.Id > 0)
                _saleReturnItemOverviews = [.. _saleReturnItemOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

            if (_selectedCompany?.Id > 0)
                _saleReturnItemOverviews = [.. _saleReturnItemOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _saleReturnItemOverviews = [.. _saleReturnItemOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _saleReturnItemOverviews = [.. _saleReturnItemOverviews.OrderBy(_ => _.TransactionDateTime)];
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading sale return item overviews: {ex.Message}", "error");
        }
        finally
        {
            if (_sfSaleReturnItemGrid is not null)
                await _sfSaleReturnItemGrid.Refresh();
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
        await LoadSaleReturnItemOverviews();
    }

    private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedLocation = args.Value;
        await LoadSaleReturnItemOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedCompany = args.Value;
        await LoadSaleReturnItemOverviews();
    }

    private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        if (_user.LocationId > 1)
            return;

        _selectedParty = args.Value;
        await LoadSaleReturnItemOverviews();
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
            await LoadSaleReturnItemOverviews();
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
            SaleReturnItemReportExcelExport.ExportSaleReturnItemReport(
            _saleReturnItemOverviews,
            dateRangeStart,
            dateRangeEnd,
            _showAllColumns,
            _user.LocationId == 1,
            _selectedLocation?.Id > 0 ? _selectedLocation.Name : null
            )
            );

            string fileName = $"SALE_RETURN_ITEM_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Sale return item report exported to Excel successfully.", "success");
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
            SaleReturnItemReportPDFExport.ExportSaleReturnItemReport(
            _saleReturnItemOverviews,
            dateRangeStart,
            dateRangeEnd,
            _showAllColumns,
            _user.LocationId == 1,
            _selectedLocation?.Id > 0 ? _selectedLocation.Name : null
            )
            );

            string fileName = $"SALE_RETURN_ITEM_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Sale return item report exported to PDF successfully.", "success");
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
    private async Task ViewSaleReturn(int saleReturnId)
    {
        try
        {
            if (saleReturnId < 0)
            {
                int actualId = Math.Abs(saleReturnId);
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{actualId}", "_blank");
                else
                    NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{actualId}");
            }
            else
            {
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{saleReturnId}", "_blank");
                else
                    NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{saleReturnId}");
            }
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

    private async Task ToggleDetailsView()
    {
        _showAllColumns = !_showAllColumns;
        StateHasChanged();

        if (_sfSaleReturnItemGrid is not null)
            await _sfSaleReturnItemGrid.Refresh();
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

    private async Task NavigateToSaleReturnReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturn);
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