using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.StockTransfer;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

using PrimeBakes.Shared.Components;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sales.StockTransfer;

public partial class StockTransferItemReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel _selectedLocation = new();
	private LocationModel _selectedToLocation = new();
	private CompanyModel _selectedCompany = new();

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<StockTransferItemOverviewModel> _transactionOverviews = [];

	private SfGrid<StockTransferItemOverviewModel> _sfGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales, true);
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
		_selectedToLocation = _locations.FirstOrDefault(_ => _.Id == 0);
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
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			_transactionOverviews = await CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(
				ViewNames.StockTransferItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (_selectedLocation?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

			if (_selectedToLocation?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.ToLocationId == _selectedToLocation.Id)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
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
		await LoadTransactionOverviews();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
	{
		_selectedLocation = args.Value;
		await LoadTransactionOverviews();
	}

	private async Task OnToLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
	{
		_selectedToLocation = args.Value;
		await LoadTransactionOverviews();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		_selectedCompany = args.Value;
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
						await _toastNotification.ShowAsync("Warning", "No previous financial year found.", ToastType.Warning);
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
			await _toastNotification.ShowAsync("Error", $"Failed to set date range: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);

			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var stream = await StockTransferItemReportExcelExport.ExportStockTransferItemReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedLocation?.Id > 0,
					_selectedLocation?.Name,
					_selectedToLocation?.Id > 0,
					_selectedToLocation?.Name
				);

			string fileName = $"STOCK_TRANSFER_ITEM_REPORT";
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

			var stream = await StockTransferItemReportPdfExport.ExportStockTransferItemReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedLocation?.Id > 0,
					_selectedLocation?.Name,
					_selectedToLocation?.Id > 0,
					_selectedToLocation?.Name
				);

			string fileName = $"STOCK_TRANSFER_ITEM_REPORT";
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
				await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{transactionId}", "_blank");
			else
				NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{transactionId}");
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Processing", "Generating invoice...", ToastType.Info);

			var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(transactionId);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await _toastNotification.ShowAsync("Success", "Invoice downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while generating invoice: {ex.Message}", ToastType.Error);
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
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.StockTransfer, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.StockTransfer);
	}

	private async Task NavigateToTransactionHistory()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportStockTransfer, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportStockTransfer);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}