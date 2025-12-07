using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleReturnReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel _selectedLocation = new();
	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<SaleReturnOverviewModel> _transactionOverviews = [];

	private SfGrid<SaleReturnOverviewModel> _sfGrid;

	private string _deleteTransactionNo = string.Empty;
	private int _deleteTransactionId = 0;

	private string _recoverTransactionNo = string.Empty;
	private int _recoverTransactionId = 0;

	private ToastNotification _toastNotification;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

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
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.R, LoadTransactionOverviews, "Refresh Data", Exclude.None)
			.Add(Code.F5, LoadTransactionOverviews, "Refresh Data", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.I, NavigateToItemReport, "Open item report", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadSelectedCartItemPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
			.Add(ModCode.Alt, Code.E, DownloadSelectedCartItemExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
			.Add(Code.Delete, DeleteSelectedCartItem, "Delete Selected Transaction", Exclude.None);

		await LoadDates();
		await LoadLocations();
		await LoadCompanies();
		await LoadParties();
		await LoadTransactionOverviews();
		await StartAutoRefresh();
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

	private async Task LoadTransactionOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching transactions...", ToastType.Info);

			_transactionOverviews = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(
				ViewNames.SaleReturnOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (!_showDeleted)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

			if (_selectedLocation?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showSummary)
				_transactionOverviews = [.. _transactionOverviews
					.GroupBy(t => t.PartyName)
					.Select(g => new SaleReturnOverviewModel
					{
						PartyName = g.Key,
						TotalItems = g.Sum(t => t.TotalItems),
						TotalQuantity = g.Sum(t => t.TotalQuantity),
						BaseTotal = g.Sum(t => t.BaseTotal),
						ItemDiscountAmount = g.Sum(t => t.ItemDiscountAmount),
						TotalAfterItemDiscount = g.Sum(t => t.TotalAfterItemDiscount),
						TotalInclusiveTaxAmount = g.Sum(t => t.TotalInclusiveTaxAmount),
						TotalExtraTaxAmount = g.Sum(t => t.TotalExtraTaxAmount),
						TotalAfterTax = g.Sum(t => t.TotalAfterTax),
						OtherChargesAmount = g.Sum(t => t.OtherChargesAmount),
						DiscountAmount = g.Sum(t => t.DiscountAmount),
						RoundOffAmount = g.Sum(t => t.RoundOffAmount),
						TotalAmount = g.Sum(t => t.TotalAmount),
						Cash = g.Sum(t => t.Cash),
						Card = g.Sum(t => t.Card),
						UPI = g.Sum(t => t.UPI),
						Credit = g.Sum(t => t.Credit)
					})];
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

	#region Changed Events
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

	private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		if (_user.LocationId > 1)
			return;

		_selectedParty = args.Value;
		await LoadTransactionOverviews();
	}

	private async Task HandleDatesChanged((DateTime FromDate, DateTime ToDate) dates)
	{
		_fromDate = dates.FromDate;
		_toDate = dates.ToDate;
		await LoadTransactionOverviews();
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

			var stream = await SaleReturnReportExcelExport.ExportSaleReturnReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedLocation?.Id > 0 ? _selectedLocation?.Name : null,
					_selectedParty?.Id > 0 ? _selectedParty?.Name : null,
					_showSummary
				);

			string fileName = $"SALE_RETURN_REPORT";
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

			// Convert DateTime to DateOnly for PDF export
			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var stream = await SaleReturnReportPdfExport.ExportSaleReturnReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedLocation?.Id > 0 ? _selectedLocation?.Name : null,
					_selectedParty?.Id > 0 ? _selectedParty?.Name : null,
					_showSummary
				);

			string fileName = $"SALE_RETURN_REPORT";
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
		await ViewTransaction(selectedCartItem.Id);
	}

	private async Task ViewTransaction(int transactionId)
	{
		try
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{transactionId}", "_blank");
			else
				NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{transactionId}");
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
		}
	}

	private async Task DownloadSelectedCartItemPdfInvoice()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await DownloadPdfInvoice(selectedCartItem.Id);
	}

	private async Task DownloadSelectedCartItemExcelInvoice()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await DownloadExcelInvoice(selectedCartItem.Id);
	}

	private async Task DownloadPdfInvoice(int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(transactionId);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
			await _toastNotification.ShowAsync("Success", "PDF invoice downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while generating PDF invoice: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DownloadExcelInvoice(int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			var (excelStream, fileName) = await SaleReturnData.GenerateAndDownloadExcelInvoice(transactionId);
			await SaveAndViewService.SaveAndView(fileName, excelStream);
			await _toastNotification.ShowAsync("Success", "Excel invoice downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while generating Excel invoice: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteSelectedCartItem()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();

		if (!selectedCartItem.Status)
			await ShowRecoverConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
		else
			await ShowDeleteConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
	}

	private async Task ConfirmDelete()
	{
		if (_isProcessing)
			return;

		try
		{
			await _deleteConfirmationDialog.HideAsync();
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission to delete this sale return transaction.");

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _deleteTransactionId);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, saleReturn.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
				throw new InvalidOperationException("Cannot delete sale return transaction as the financial year is locked.");

			await SaleReturnData.DeleteSaleReturn(_deleteTransactionId);
			await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);

			_deleteTransactionId = 0;
			_deleteTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}

	private async Task ToggleDeleted()
	{
		if (_user.LocationId > 1)
			return;

		_showDeleted = !_showDeleted;
		await LoadTransactionOverviews();
	}

	private async Task ToggleSummary()
	{
		if (_user.LocationId > 1)
			return;

		_showSummary = !_showSummary;
		await LoadTransactionOverviews();
	}

	private async Task ConfirmRecover()
	{
		if (_isProcessing)
			return;

		try
		{
			await _recoverConfirmationDialog.HideAsync();
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

			await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

			var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, _recoverTransactionId);

			// Recover the sale return transaction
			saleReturn.Status = true;
			saleReturn.LastModifiedBy = _user.Id;
			saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
			saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

			await SaleReturnData.RecoverSaleReturnTransaction(saleReturn);
			await _toastNotification.ShowAsync("Success", $"Transaction '{_recoverTransactionNo}' has been successfully recovered.", ToastType.Success);

			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover sale return: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}
	#endregion

	#region Utilities
	private async Task NavigateToTransactionPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SaleReturn, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.SaleReturn);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturnItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturnItem);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

	private async Task ShowDeleteConfirmation(int id, string transactionNo)
	{
		if (_user.LocationId > 1)
			return;

		_deleteTransactionId = id;
		_deleteTransactionNo = transactionNo;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		await _deleteConfirmationDialog.HideAsync();
		_deleteTransactionId = 0;
		_deleteTransactionNo = string.Empty;
	}

	private async Task ShowRecoverConfirmation(int id, string transactionNo)
	{
		if (_user.LocationId > 1)
			return;

		_recoverTransactionId = id;
		_recoverTransactionNo = transactionNo;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		await _recoverConfirmationDialog.HideAsync();
		_recoverTransactionId = 0;
		_recoverTransactionNo = string.Empty;
	}

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
					await LoadTransactionOverviews();
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