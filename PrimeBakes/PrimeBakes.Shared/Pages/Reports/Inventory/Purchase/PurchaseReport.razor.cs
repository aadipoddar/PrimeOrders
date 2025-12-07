using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Purchase;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;
	private bool _showTransactionReturns = false;
	private bool _showDeleted = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<PurchaseOverviewModel> _transactionOverviews = [];
	private List<PurchaseReturnOverviewModel> _transactionReturnOverviews = [];

	private SfGrid<PurchaseOverviewModel> _sfGrid;

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

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
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

			_transactionOverviews = await CommonData.LoadTableDataByDate<PurchaseOverviewModel>(
				ViewNames.PurchaseOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (!_showDeleted)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.Status)];

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showTransactionReturns)
				await LoadTransactionReturnOverviews();

			if (_showSummary)
				_transactionOverviews = [.. _transactionOverviews
					.GroupBy(t => t.PartyName)
					.Select(g => new PurchaseOverviewModel
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
						CashDiscountAmount = g.Sum(t => t.CashDiscountAmount),
						OtherChargesAmount = g.Sum(t => t.OtherChargesAmount),
						RoundOffAmount = g.Sum(t => t.RoundOffAmount),
						TotalAmount = g.Sum(t => t.TotalAmount)
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

	private async Task LoadTransactionReturnOverviews()
	{
		_transactionReturnOverviews = await CommonData.LoadTableDataByDate<PurchaseReturnOverviewModel>(
			ViewNames.PurchaseReturnOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		if (!_showDeleted)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.Status)];

		if (_selectedCompany?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		if (_selectedParty?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

		_transactionReturnOverviews = [.. _transactionReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeTransactionAndReturns();
	}

	private void MergeTransactionAndReturns()
	{
		_transactionOverviews.AddRange(_transactionReturnOverviews.Select(pr => new PurchaseOverviewModel
		{
			Id = pr.Id * -1, // Negative ID to differentiate returns
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = pr.PartyId,
			PartyName = pr.PartyName,
			TransactionDateTime = pr.TransactionDateTime,
			CashDiscountAmount = -pr.CashDiscountAmount,
			OtherChargesAmount = -pr.OtherChargesAmount,
			RoundOffAmount = -pr.RoundOffAmount,
			TotalAmount = -pr.TotalAmount,
			BaseTotal = -pr.BaseTotal,
			CashDiscountPercent = pr.CashDiscountPercent,
			CreatedAt = pr.CreatedAt,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedFromPlatform = pr.CreatedFromPlatform,
			DocumentUrl = pr.DocumentUrl,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			Remarks = pr.Remarks,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
			ItemDiscountAmount = -pr.ItemDiscountAmount,
			TotalAfterItemDiscount = -pr.TotalAfterItemDiscount,
			TotalExtraTaxAmount = -pr.TotalExtraTaxAmount,
			TotalInclusiveTaxAmount = -pr.TotalInclusiveTaxAmount,
			TotalAfterTax = -pr.TotalAfterTax,
			TotalItems = pr.TotalItems,
			TotalQuantity = -pr.TotalQuantity,
			TransactionNo = pr.TransactionNo,
			OtherChargesPercent = pr.OtherChargesPercent,
			Status = pr.Status
		}));

		_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadTransactionOverviews();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		_selectedCompany = args.Value;
		await LoadTransactionOverviews();
	}

	private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
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

			var stream = await PurchaseReportExcelExport.ExportPurchaseReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedParty?.Id > 0 ? _selectedParty?.Name : null,
					_showSummary
				);

			string fileName = $"PURCHASE_REPORT";
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

			var stream = await PurchaseReportPDFExport.ExportPurchaseReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_selectedParty?.Id > 0 ? _selectedParty?.Name : null,
					_showSummary
				);

			string fileName = $"PURCHASE_REPORT";
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
			if (transactionId < 0)
			{
				int actualId = Math.Abs(transactionId);
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.PurchaseReturn}/{actualId}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.PurchaseReturn}/{actualId}");
			}
			else
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Purchase}/{transactionId}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Purchase}/{transactionId}");
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

			bool isPurchaseReturn = transactionId < 0;
			int actualId = Math.Abs(transactionId);

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

			await _toastNotification.ShowAsync("Success", "PDF invoice generated successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"PDF invoice generation failed: {ex.Message}", ToastType.Error);
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

			bool isPurchaseReturn = transactionId < 0;
			int actualId = Math.Abs(transactionId);

			if (isPurchaseReturn)
			{
				var (excelStream, fileName) = await PurchaseReturnData.GenerateAndDownloadExcelInvoice(actualId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}
			else
			{
				var (excelStream, fileName) = await PurchaseData.GenerateAndDownloadExcelInvoice(actualId);
				await SaveAndViewService.SaveAndView(fileName, excelStream);
			}

			await _toastNotification.ShowAsync("Success", "Excel invoice generated successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Excel invoice generation failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task DownloadOriginalInvoice(string documentUrl)
	{
		if (_isProcessing)
			return;

		try
		{
			if (string.IsNullOrEmpty(documentUrl))
			{
				await _toastNotification.ShowAsync("Warning", "No original document available for this transaction.", ToastType.Warning);
				return;
			}

			_isProcessing = true;

			var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(documentUrl, BlobStorageContainers.purchase);
			var fileName = documentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, fileStream);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while downloading original invoice: {ex.Message}", ToastType.Error);
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
			ShowRecoverConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
		else
			ShowDeleteConfirmation(selectedCartItem.Id, selectedCartItem.TransactionNo);
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

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			await _toastNotification.ShowAsync("Processing", "Deleting transaction...", ToastType.Info);

			if (_deleteTransactionId < 0)
				await PurchaseReturnData.DeletePurchaseReturn(Math.Abs(_deleteTransactionId));
			else
				await PurchaseData.DeletePurchase(_deleteTransactionId);

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

	private async Task ToggleTransactionReturns()
	{
		_showTransactionReturns = !_showTransactionReturns;
		await LoadTransactionOverviews();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadTransactionOverviews();
	}

	private async Task ToggleSummary()
	{
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

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to recover this transaction.");

			await _toastNotification.ShowAsync("Processing", "Recovering transaction...", ToastType.Info);

			if (_recoverTransactionId == 0)
			{
				await _toastNotification.ShowAsync("Error", "Invalid transaction selected for recovery.", ToastType.Error);
				return;
			}

			if (_recoverTransactionId < 0)
				await RecoverReturnTransaction(Math.Abs(_recoverTransactionId));
			else
				await RecoverTransaction(_recoverTransactionId);

			await _toastNotification.ShowAsync("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", ToastType.Success);

			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while recovering transaction: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task RecoverTransaction(int recoverTransactionId)
	{
		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, recoverTransactionId);
		if (purchase is null)
		{
			await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
			return;
		}

		// Update the Status to true (active)
		purchase.Status = true;
		purchase.LastModifiedBy = _user.Id;
		purchase.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		purchase.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await PurchaseData.RecoverPurchaseTransaction(purchase);
	}

	private async Task RecoverReturnTransaction(int recoverTransactionId)
	{
		var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, recoverTransactionId);
		if (purchaseReturn is null)
		{
			await _toastNotification.ShowAsync("Error", "Transaction not found.", ToastType.Error);
			return;
		}

		// Update the Status to true (active)
		purchaseReturn.Status = true;
		purchaseReturn.LastModifiedBy = _user.Id;
		purchaseReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		purchaseReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await PurchaseReturnData.RecoverPurchaseReturnTransaction(purchaseReturn);
	}
	#endregion

	#region Utilities
	private async Task NavigateToTransactionPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Purchase, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.Purchase);
	}

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchaseItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportPurchaseItem);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

	private async Task ShowDeleteConfirmation(int id, string transactionNo)
	{
		_deleteTransactionId = id;
		_deleteTransactionNo = transactionNo;
		StateHasChanged();
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteTransactionId = 0;
		_deleteTransactionNo = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ShowRecoverConfirmation(int id, string transactionNo)
	{
		_recoverTransactionId = id;
		_recoverTransactionNo = transactionNo;
		StateHasChanged();
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverTransactionId = 0;
		_recoverTransactionNo = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
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