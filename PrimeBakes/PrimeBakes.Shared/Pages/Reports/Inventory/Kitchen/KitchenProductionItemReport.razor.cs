using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Kitchen;

public partial class KitchenProductionItemReport : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private PeriodicTimer _autoRefreshTimer;
	private CancellationTokenSource _autoRefreshCts;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSummary = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();
	private LocationModel _selectedKitchen = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _kitchens = [];
	private List<KitchenProductionItemOverviewModel> _transactionOverviews = [];

	private SfGrid<KitchenProductionItemOverviewModel> _sfGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
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
		await LoadCompanies();
		await LoadKitchens();
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

	private async Task LoadKitchens()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_kitchens.Add(new()
		{
			Id = 0,
			Name = "All Kitchens"
		});
		_kitchens = [.. _kitchens.OrderBy(s => s.Name)];
		_selectedKitchen = _kitchens.FirstOrDefault(_ => _.Id == 0);
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

			_transactionOverviews = await CommonData.LoadTableDataByDate<KitchenProductionItemOverviewModel>(
				ViewNames.KitchenProductionItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedKitchen?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.KitchenId == _selectedKitchen.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showSummary)
				_transactionOverviews = [.. _transactionOverviews
					.GroupBy(t => t.ItemName)
					.Select(g => new KitchenProductionItemOverviewModel
					{
						ItemName = g.Key,
						ItemCode = g.First().ItemCode,
						ItemCategoryName = g.First().ItemCategoryName,
						Quantity = g.Sum(t => t.Quantity),
						Total = g.Sum(t => t.Total)
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
	#endregion

	#region Change Events
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

	private async Task OnKitchenChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
	{
		_selectedKitchen = args.Value;
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

			var stream = await KitchenProductionItemReportExcelExport.ExportKitchenProductionItemReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_showSummary
				);

			string fileName = $"KITCHEN_PRODUCTION_ITEM_REPORT";
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

			var stream = await KitchenProductionItemReportPDFExport.ExportKitchenProductionItemReport(
					_transactionOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_showSummary
				);

			string fileName = $"KITCHEN_PRODUCTION_ITEM_REPORT";
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
				await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.KitchenProduction}/{transactionId}", "_blank");
			else
				NavigationManager.NavigateTo($"{PageRouteNames.KitchenProduction}/{transactionId}");
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
		await DownloadPdfInvoice(selectedCartItem.MasterId);
	}

	private async Task DownloadSelectedCartItemExcelInvoice()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await DownloadExcelInvoice(selectedCartItem.MasterId);
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

			var (fileName, pdfStream) = await KitchenProductionData.GenerateAndDownloadInvoice(transactionId);
			await SaveAndViewService.SaveAndView(pdfStream, fileName);
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

			var (fileName, excelStream) = await KitchenProductionData.GenerateAndDownloadExcelInvoice(transactionId);
			await SaveAndViewService.SaveAndView(excelStream, fileName);
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
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.KitchenProduction, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.KitchenProduction);
	}

	private async Task NavigateToTransactionHistory()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenProduction, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportKitchenProduction);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

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