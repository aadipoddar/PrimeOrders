using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Purchase;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseItemReport
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showAllColumns = false;
    private bool _showPurchaseReturns = false;

    private DateTime _fromDate = DateTime.Now.Date;
    private DateTime _toDate = DateTime.Now.Date;

    private CompanyModel _selectedCompany = new();
    private LedgerModel _selectedParty = new();

    private List<CompanyModel> _companies = [];
    private List<LedgerModel> _parties = [];
    private List<PurchaseItemOverviewModel> _purchaseItemOverviews = [];
    private List<PurchaseReturnItemOverviewModel> _purchaseReturnItemOverviews = [];

    private SfGrid<PurchaseItemOverviewModel> _sfPurchaseItemGrid;

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

        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        _user = authResult.User;
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        await LoadDates();
        await LoadCompanies();
        await LoadParties();
        await LoadPurchaseItemOverviews();
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

    private async Task LoadPurchaseItemOverviews()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;

            _purchaseItemOverviews = await PurchaseData.LoadPurchaseItemOverviewByDate(
            DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
            DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

            if (_selectedCompany?.Id > 0)
                _purchaseItemOverviews = [.. _purchaseItemOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

            if (_selectedParty?.Id > 0)
                _purchaseItemOverviews = [.. _purchaseItemOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

            _purchaseItemOverviews = [.. _purchaseItemOverviews.OrderBy(_ => _.TransactionDateTime)];

            if (_showPurchaseReturns)
                await LoadPurchaseReturnItemOverviews();
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while loading purchase item overviews: {ex.Message}", "error");
        }
        finally
        {
            if (_sfPurchaseItemGrid is not null)
                await _sfPurchaseItemGrid.Refresh();
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task LoadPurchaseReturnItemOverviews()
    {
        _purchaseReturnItemOverviews = await PurchaseReturnData.LoadPurchaseReturnItemOverviewByDate(
        DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
        DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

        if (_selectedCompany?.Id > 0)
            _purchaseReturnItemOverviews = [.. _purchaseReturnItemOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

        if (_selectedParty?.Id > 0)
            _purchaseReturnItemOverviews = [.. _purchaseReturnItemOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

        _purchaseReturnItemOverviews = [.. _purchaseReturnItemOverviews.OrderBy(_ => _.TransactionDateTime)];

        MergePurchaseAndReturns();
    }

    private void MergePurchaseAndReturns()
    {
        _purchaseItemOverviews.AddRange(_purchaseReturnItemOverviews.Select(pr => new PurchaseItemOverviewModel
        {
            Id = pr.Id,
            PurchaseId = -pr.PurchaseReturnId,
            ItemName = pr.ItemName,
            ItemCode = pr.ItemCode,
            ItemCategoryId = pr.ItemCategoryId,
            ItemCategoryName = pr.ItemCategoryName,
            CompanyId = pr.CompanyId,
            CompanyName = pr.CompanyName,
            PartyId = pr.PartyId,
            PartyName = pr.PartyName,
            TransactionNo = pr.TransactionNo,
            TransactionDateTime = pr.TransactionDateTime,
            PurchaseRemarks = pr.PurchaseReturnRemarks,
            Quantity = -pr.Quantity,
            Rate = pr.Rate,
            BaseTotal = -pr.BaseTotal,
            DiscountPercent = pr.DiscountPercent,
            DiscountAmount = -pr.DiscountAmount,
            AfterDiscount = -pr.AfterDiscount,
            CGSTPercent = pr.CGSTPercent,
            CGSTAmount = -pr.CGSTAmount,
            SGSTPercent = pr.SGSTPercent,
            SGSTAmount = -pr.SGSTAmount,
            IGSTPercent = pr.IGSTPercent,
            IGSTAmount = -pr.IGSTAmount,
            TotalTaxAmount = -pr.TotalTaxAmount,
            InclusiveTax = pr.InclusiveTax,
            Total = -pr.Total,
            NetRate = pr.NetRate,
            Remarks = pr.Remarks
        }));

        _purchaseItemOverviews = [.. _purchaseItemOverviews.OrderBy(_ => _.TransactionDateTime)];
    }
    #endregion

    #region Change Events
    private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
    {
        _fromDate = args.StartDate;
        _toDate = args.EndDate;
        await LoadPurchaseItemOverviews();
    }

    private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
    {
        _selectedCompany = args.Value;
        await LoadPurchaseItemOverviews();
    }

    private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
    {
        _selectedParty = args.Value;
        await LoadPurchaseItemOverviews();
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
            await LoadPurchaseItemOverviews();
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
            PurchaseItemReportExcelExport.ExportPurchaseItemReport(
                    _purchaseItemOverviews,
                    dateRangeStart,
                    dateRangeEnd,
                    _showAllColumns
                )
            );

            string fileName = $"PURCHASE_ITEM_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".xlsx";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Purchase item report exported to Excel successfully.", "success");
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
            PurchaseItemReportPDFExport.ExportPurchaseItemReport(
            _purchaseItemOverviews,
            dateRangeStart,
            dateRangeEnd,
            _showAllColumns
            )
            );

            string fileName = $"PURCHASE_ITEM_REPORT";
            if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
                fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
            fileName += ".pdf";

            await SaveAndViewService.SaveAndView(fileName, stream);

            await ShowToast("Success", "Purchase item report exported to PDF successfully.", "success");
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
    private async Task ViewPurchase(int purchaseId)
    {
        try
        {
            if (purchaseId < 0)
            {
                int actualId = Math.Abs(purchaseId);
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"/inventory/purchase-return/{actualId}", "_blank");
                else
                    NavigationManager.NavigateTo($"/inventory/purchase-return/{actualId}");
            }
            else
            {
                if (FormFactor.GetFormFactor() == "Web")
                    await JSRuntime.InvokeVoidAsync("open", $"/inventory/purchase/{purchaseId}", "_blank");
                else
                    NavigationManager.NavigateTo($"/inventory/purchase/{purchaseId}");
            }
        }
        catch (Exception ex)
        {
            await ShowToast("Error", $"An error occurred while opening purchase: {ex.Message}", "error");
        }
    }

    private async Task DownloadInvoice(int purchaseId)
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            bool isPurchaseReturn = purchaseId < 0;
            int actualId = Math.Abs(purchaseId);

            if (isPurchaseReturn)
            {
                var (pdfStream, fileName) = await PurchaseReturnData.GenerateAndDownloadInvoice(actualId);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
            else
            {
                var (pdfStream, fileName) = await PurchaseData.GenerateAndDownloadInvoice(actualId);
                await SaveAndViewService.SaveAndView(fileName, pdfStream);
            }
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

        if (_sfPurchaseItemGrid is not null)
            await _sfPurchaseItemGrid.Refresh();
    }

    private async Task TogglePurchaseReturns()
    {
        _showPurchaseReturns = !_showPurchaseReturns;
        await LoadPurchaseItemOverviews();
        StateHasChanged();
    }
    #endregion

    #region Utilities
    private async Task NavigateToPurchasePage()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", "/inventory/purchase", "_blank");
        else
            NavigationManager.NavigateTo("/inventory/purchase");
    }

    private async Task NavigateToPurchaseReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", "/report/purchase", "_blank");
        else
            NavigationManager.NavigateTo("/report/purchase");
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
