using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.Data.Sales.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sales.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleItemReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSaleReturns = false;
	private bool _showStockTransfers = false;
	private bool _showSummary = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel _selectedLocation = new();
	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<SaleItemOverviewModel> _transactionOverviews = [];
	private List<SaleReturnItemOverviewModel> _transactionReturnOverviews = [];
	private List<StockTransferItemOverviewModel> _transactionTransferOverviews = [];

	private SfGrid<SaleItemOverviewModel> _sfGrid;

	private ToastNotification _toastNotification;

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
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistory, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadSelectedCartItemPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
			.Add(ModCode.Alt, Code.E, DownloadSelectedCartItemExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None);

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

			_transactionOverviews = await CommonData.LoadTableDataByDate<SaleItemOverviewModel>(
				ViewNames.SaleItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (_selectedLocation?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showSaleReturns)
				await LoadTransactionReturnOverviews();

			if (_showStockTransfers)
				await LoadTransactionTransferOverviews();

			if (_showSummary)
				_transactionOverviews = [.. _transactionOverviews
					.GroupBy(t => t.ItemName)
					.Select(g => new SaleItemOverviewModel
					{
						ItemName = g.Key,
						ItemCode = g.First().ItemCode,
						ItemCategoryName = g.First().ItemCategoryName,
						Quantity = g.Sum(t => t.Quantity),
						BaseTotal = g.Sum(t => t.BaseTotal),
						DiscountAmount = g.Sum(t => t.DiscountAmount),
						AfterDiscount = g.Sum(t => t.AfterDiscount),
						SGSTAmount = g.Sum(t => t.SGSTAmount),
						CGSTAmount = g.Sum(t => t.CGSTAmount),
						IGSTAmount = g.Sum(t => t.IGSTAmount),
						TotalTaxAmount = g.Sum(t => t.TotalTaxAmount),
						Total = g.Sum(t => t.Total),
						NetTotal = g.Sum(t => t.NetTotal)
					})
					.OrderBy(t => t.ItemName)];
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

	private async Task LoadTransactionReturnOverviews()
	{
		_transactionReturnOverviews = await CommonData.LoadTableDataByDate<SaleReturnItemOverviewModel>(
			ViewNames.SaleReturnItemOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		if (_selectedLocation?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

		if (_selectedCompany?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		if (_selectedParty?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

		_transactionReturnOverviews = [.. _transactionReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeTransactionAndReturns();
	}

	private void MergeTransactionAndReturns()
	{
		_transactionOverviews.AddRange(_transactionReturnOverviews.Select(pr => new SaleItemOverviewModel
		{
			Id = -pr.Id,
			MasterId = -pr.MasterId,
			OrderTransactionNo = null,
			CustomerId = pr.CustomerId,
			CustomerName = pr.CustomerName,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			OrderId = null,
			SaleRemarks = pr.SaleReturnRemarks,
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
			NetTotal = -pr.NetTotal,
			Remarks = pr.Remarks
		}));

		_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
	}

	private async Task LoadTransactionTransferOverviews()
	{
		_transactionTransferOverviews = await CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(
			ViewNames.StockTransferItemOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		if (_selectedLocation?.Id > 0)
			_transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

		if (_selectedCompany?.Id > 0)
			_transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		if (_selectedParty?.Id > 0 && _selectedParty?.LocationId > 0)
			_transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.ToLocationId == _selectedParty.LocationId)];

		_transactionReturnOverviews = [.. _transactionReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeTransactionAndTransfers();
	}

	private void MergeTransactionAndTransfers()
	{
		_transactionOverviews.AddRange(_transactionTransferOverviews.Select(pr => new SaleItemOverviewModel
		{
			Id = 0,
			MasterId = 0,
			OrderTransactionNo = null,
			CustomerId = null,
			CustomerName = null,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			OrderId = null,
			SaleRemarks = pr.StockTransferRemarks,
			ItemName = pr.ItemName,
			ItemCode = pr.ItemCode,
			ItemCategoryId = pr.ItemCategoryId,
			ItemCategoryName = pr.ItemCategoryName,
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = _parties.FirstOrDefault(p => p.LocationId == pr.ToLocationId)?.Id,
			PartyName = _parties.FirstOrDefault(p => p.LocationId == pr.ToLocationId)?.Name,
			TransactionNo = pr.TransactionNo,
			TransactionDateTime = pr.TransactionDateTime,
			Quantity = pr.Quantity,
			Rate = pr.Rate,
			BaseTotal = pr.BaseTotal,
			DiscountPercent = pr.DiscountPercent,
			DiscountAmount = pr.DiscountAmount,
			AfterDiscount = pr.AfterDiscount,
			CGSTPercent = pr.CGSTPercent,
			CGSTAmount = pr.CGSTAmount,
			SGSTPercent = pr.SGSTPercent,
			SGSTAmount = pr.SGSTAmount,
			IGSTPercent = pr.IGSTPercent,
			IGSTAmount = pr.IGSTAmount,
			TotalTaxAmount = pr.TotalTaxAmount,
			InclusiveTax = pr.InclusiveTax,
			Total = pr.Total,
			NetRate = pr.NetRate,
			NetTotal = pr.NetTotal,
			Remarks = pr.Remarks
		}));

		_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
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

			var stream = await SaleItemReportExcelExport.ExportSaleItemReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedLocation?.Id > 0,
					_selectedLocation?.Name,
					_showSummary
				);

			string fileName = $"SALE_ITEM_REPORT";
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

			var stream = await SaleItemReportPDFExport.ExportSaleItemReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedLocation?.Id > 0,
					_selectedLocation?.Name,
					_showSummary
				);

			string fileName = $"SALE_ITEM_REPORT";
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
		await ViewTransaction(selectedCartItem.MasterId, selectedCartItem.TransactionNo);
	}

	private async Task ViewTransaction(int transactionId, string transactionNo)
	{
		try
		{
			if (transactionId == 0 && !string.IsNullOrEmpty(transactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{stockTransfer.MasterId}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{stockTransfer.MasterId}");
			}
			else if (transactionId < 0)
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{Math.Abs(transactionId)}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{Math.Abs(transactionId)}");
			}
			else
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Sale}/{transactionId}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Sale}/{transactionId}");
			}
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
		await DownloadPdfInvoice(selectedCartItem.MasterId, selectedCartItem.TransactionNo);
	}

	private async Task DownloadSelectedCartItemExcelInvoice()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await DownloadExcelInvoice(selectedCartItem.MasterId, selectedCartItem.TransactionNo);
	}

	private async Task DownloadPdfInvoice(int transactionId, string transactionNo)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			if (transactionId == 0 && !string.IsNullOrWhiteSpace(transactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
				var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(stockTransfer.MasterId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (transactionId < 0)
			{
				var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(Math.Abs(transactionId));
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else
			{
				var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

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

	private async Task DownloadExcelInvoice(int transactionId, string transactionNo)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			if (transactionId == 0 && !string.IsNullOrWhiteSpace(transactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
				var (excelStream, fileName) = await StockTransferData.GenerateAndDownloadExcelInvoice(stockTransfer.MasterId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (transactionId < 0)
			{
				var (excelStream, fileName) = await SaleReturnData.GenerateAndDownloadExcelInvoice(Math.Abs(transactionId));
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else
			{
				var (excelStream, fileName) = await SaleData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}

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

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}

	private async Task ToggleSaleReturns()
	{
		_showSaleReturns = !_showSaleReturns;
		await LoadTransactionOverviews();
	}

	private async Task ToggleStockTransfers()
	{
		_showStockTransfers = !_showStockTransfers;
		await LoadTransactionOverviews();
	}

	private async Task ToggleSummary()
	{
		_showSummary = !_showSummary;
		await LoadTransactionOverviews();
	}
	#endregion

	#region Utilities
	private async Task NavigateToTransactionPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Sale, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.Sale);
	}

	private async Task NavigateToTransactionHistory()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSale, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportSale);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

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