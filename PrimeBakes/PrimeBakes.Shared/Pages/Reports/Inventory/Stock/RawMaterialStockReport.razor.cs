using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Data.Sales.Sale;
using PrimeBakesLibrary.Data.Sales.StockTransfer;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Stock;

public partial class RawMaterialStockReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDetails = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now;
	private DateTime _toDate = DateTime.Now;

	private List<RawMaterialStockSummaryModel> _stockSummary = [];
	private List<RawMaterialStockDetailsModel> _stockDetails = [];

	private SfGrid<RawMaterialStockSummaryModel> _sfStockGrid;
	private SfGrid<RawMaterialStockDetailsModel> _sfStockDetailsGrid;

	private bool _isDeleteDialogVisible = false;
	private int _deleteAdjustmentId = 0;
	private string _deleteTransactionNo = string.Empty;
	private SfDialog _deleteConfirmationDialog;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.R, LoadStockData, "Refresh Data", Exclude.None)
			.Add(Code.F5, LoadStockData, "Refresh Data", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export to Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export to PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadSelectedCartItemPdfInvoice, "Download Selected Transaction PDF Invoice", Exclude.None)
			.Add(ModCode.Alt, Code.E, DownloadSelectedCartItemExcelInvoice, "Download Selected Transaction Excel Invoice", Exclude.None)
			.Add(Code.Delete, DeleteSelectedCartItem, "Delete Selected Transaction", Exclude.None);


		await LoadDates();
		await LoadStockData();
		await StartAutoRefresh();
	}

	private async Task LoadDates()
	{
		_fromDate = await CommonData.LoadCurrentDateTime();
		_toDate = _fromDate;
	}

	private async Task LoadStockData()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Loading", "Fetching stock data...", ToastType.Info);

			_stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_fromDate, _toDate);

			_stockSummary = [.. _stockSummary.Where(_ => _.OpeningStock != 0 ||
												  _.PurchaseStock != 0 ||
												  _.SaleStock != 0 ||
												  _.ClosingStock != 0)];
			_stockSummary = [.. _stockSummary.OrderBy(_ => _.RawMaterialName)];

			if (_showDetails)
				await LoadStockDetails();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load stock: {ex.Message}", ToastType.Error);
		}
		finally
		{
			if (_sfStockGrid is not null)
				await _sfStockGrid.Refresh();

			if (_sfStockDetailsGrid is not null)
				await _sfStockDetailsGrid.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task LoadStockDetails()
	{
		_stockDetails = await CommonData.LoadTableDataByDate<RawMaterialStockDetailsModel>(
				ViewNames.RawMaterialStockDetails,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		_stockDetails = [.. _stockDetails.OrderBy(_ => _.TransactionDateTime).ThenBy(_ => _.RawMaterialName)];
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadStockData();
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
			await LoadStockData();
			StateHasChanged();
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing || _stockSummary is null || _stockSummary.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);

			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var stream = await RawMaterialStockReportExcelExport.ExportRawMaterialStockReport(
					_stockSummary,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_showDetails ? _stockDetails : null
				);

			string fileName = $"RAW_MATERIAL_STOCK_REPORT";
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
		if (_isProcessing || _stockSummary is null || _stockSummary.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Exporting", "Generating PDF file...", ToastType.Info);

			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var summaryStream = await RawMaterialStockSummaryReportPDFExport.ExportRawMaterialStockReport(
					_stockSummary,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				);

			string summaryFileName = $"RAW_MATERIAL_STOCK_SUMMARY";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				summaryFileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			summaryFileName += ".pdf";
			await SaveAndViewService.SaveAndView(summaryFileName, summaryStream);

			if (_showDetails && _stockDetails is not null && _stockDetails.Count > 0)
			{
				var detailsStream = await RawMaterialStockDetailsReportPDFExport.ExportRawMaterialStockDetailsReport(
						_stockDetails,
						dateRangeStart,
						dateRangeEnd
					);

				string detailsFileName = $"RAW_MATERIAL_STOCK_DETAILS";
				if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
					detailsFileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
				detailsFileName += ".pdf";
				await SaveAndViewService.SaveAndView(detailsFileName, detailsStream);
			}

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
	private async Task ToggleDetailsView()
	{
		_showDetails = !_showDetails;
		await LoadStockData();
	}

	private void ToggleColumnsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();
	}

	private async Task ViewSelectedCartItem()
	{
		if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();
		await ViewTransaction(selectedCartItem.Type, selectedCartItem.TransactionId.Value);
	}

	private async Task ViewTransaction(string type, int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			var url = type?.ToLower() switch
			{
				"purchase" => $"{PageRouteNames.Purchase}/{transactionId}",
				"purchasereturn" => $"{PageRouteNames.PurchaseReturn}/{transactionId}",
				"sale" => $"{PageRouteNames.Sale}/{transactionId}",
				"salereturn" => $"{PageRouteNames.SaleReturn}/{transactionId}",
				"kitchenissue" => $"{PageRouteNames.KitchenIssue}/{transactionId}",
				"kitchenproduction" => $"{PageRouteNames.KitchenProduction}/{transactionId}",
				"stocktransfer" => $"{PageRouteNames.StockTransfer}/{transactionId}",
				_ => null
			};

			if (string.IsNullOrEmpty(url))
			{
				await _toastNotification.ShowAsync("Error", "Unknown transaction type.", ToastType.Error);
				return;
			}

			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", url, "_blank");
			else
				NavigationManager.NavigateTo(url);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while opening transaction: {ex.Message}", ToastType.Error);
		}
	}

	private async Task DownloadSelectedCartItemPdfInvoice()
	{
		if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();
		await DownloadPdfInvoice(selectedCartItem.Type, selectedCartItem.TransactionId.Value);
	}

	private async Task DownloadSelectedCartItemExcelInvoice()
	{
		if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();
		await DownloadExcelInvoice(selectedCartItem.Type, selectedCartItem.TransactionId.Value);
	}

	private async Task DownloadPdfInvoice(string type, int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating PDF invoice...", ToastType.Info);

			if (type.Equals("purchase", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await PurchaseData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (type.Equals("purchasereturn", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await PurchaseReturnData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (type.Equals("sale", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (type.Equals("salereturn", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (type.Equals("kitchenissue", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await KitchenIssueData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (type.Equals("kitchenproduction", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await KitchenProductionData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else if (type.Equals("stocktransfer", StringComparison.CurrentCultureIgnoreCase))
			{
				var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}

			await _toastNotification.ShowAsync("Success", "PDF invoice downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while downloading PDF invoice: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DownloadExcelInvoice(string type, int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating Excel invoice...", ToastType.Info);

			if (type.Equals("purchase", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await PurchaseData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (type.Equals("purchasereturn", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await PurchaseReturnData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (type.Equals("sale", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await SaleData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (type.Equals("salereturn", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await SaleReturnData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (type.Equals("kitchenissue", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await KitchenIssueData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (type.Equals("kitchenproduction", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await KitchenProductionData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else if (type.Equals("stocktransfer", StringComparison.CurrentCultureIgnoreCase))
			{
				var (excelStream, fileName) = await StockTransferData.GenerateAndDownloadExcelInvoice(transactionId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}

			await _toastNotification.ShowAsync("Success", "Excel invoice downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while downloading Excel invoice: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DeleteSelectedCartItem()
	{
		if (_sfStockDetailsGrid is null || _sfStockDetailsGrid.SelectedRecords is null || _sfStockDetailsGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfStockDetailsGrid.SelectedRecords.First();

		if (selectedCartItem.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase))
			ShowDeleteConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
	}

	private async Task ConfirmDelete()
	{
		if (_isProcessing || _deleteAdjustmentId == 0)
			return;

		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;
			StateHasChanged();

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			var adjustment = _stockDetails.FirstOrDefault(x => x.Id == _deleteAdjustmentId);
			if (adjustment is null && !adjustment.Type.Equals("adjustment", StringComparison.CurrentCultureIgnoreCase))
				return;

			await RawMaterialStockData.DeleteRawMaterialStockById(_deleteAdjustmentId);
			await _toastNotification.ShowAsync("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while deleting transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_deleteAdjustmentId = 0;
			_deleteTransactionNo = string.Empty;
			_isProcessing = false;
			StateHasChanged();
			await LoadStockData();
		}
	}
	#endregion

	#region Utilities
	private async Task NavigateToTransactionPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.RawMaterialStockAdjustment, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.RawMaterialStockAdjustment);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

	private void ShowDeleteConfirmation(int id, string transactionNo)
	{
		_deleteAdjustmentId = id;
		_deleteTransactionNo = transactionNo ?? "N/A";
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteAdjustmentId = 0;
		_deleteTransactionNo = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
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
					await LoadStockData();
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