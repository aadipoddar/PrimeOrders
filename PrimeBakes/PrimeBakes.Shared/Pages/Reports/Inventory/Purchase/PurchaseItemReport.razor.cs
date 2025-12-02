using Microsoft.AspNetCore.Components;
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

using Toolbelt.Blazor.HotKeys2;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseItemReport : IAsyncDisposable
{
	[Inject] private HotKeys HotKeys { get; set; }
	private HotKeysContext _hotKeysContext;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showTransactionReturns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<PurchaseItemOverviewModel> _transactionOverviews = [];
	private List<PurchaseReturnItemOverviewModel> _transactionReturnOverviews = [];

	private SfGrid<PurchaseItemOverviewModel> _sfGrid;

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
			.Add(ModCode.Ctrl, Code.H, NavigateToTransactionHistory, "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, NavigateToTransactionPage, "New Transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, NavigateToDashboard, "Go to dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(ModCode.Ctrl, Code.O, ViewSelectedCartItem, "Open Selected Transaction", Exclude.None)
			.Add(ModCode.Alt, Code.P, DownloadSelectedCartItemInvoice, "Download Selected Transaction Invoice", Exclude.None);

		await LoadDates();
		await LoadCompanies();
		await LoadParties();
		await LoadTransactionOverviews();
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

			_transactionOverviews = await CommonData.LoadTableDataByDate<PurchaseItemOverviewModel>(
				ViewNames.PurchaseItemOverview,
				DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
				DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (_selectedCompany?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_transactionOverviews = [.. _transactionOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showTransactionReturns)
				await LoadTransactionReturnOverviews();
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while loading transaction overviews: {ex.Message}", "error");
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
		_transactionReturnOverviews = await CommonData.LoadTableDataByDate<PurchaseReturnItemOverviewModel>(
			ViewNames.PurchaseReturnItemOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		if (_selectedCompany?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		if (_selectedParty?.Id > 0)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

		_transactionReturnOverviews = [.. _transactionReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeTransactionAndReturns();
	}

	private void MergeTransactionAndReturns()
	{
		_transactionOverviews.AddRange(_transactionReturnOverviews.Select(pr => new PurchaseItemOverviewModel
		{
			Id = -pr.Id,
			MasterId = -pr.MasterId,
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
			NetTotal = -pr.NetTotal,
			NetRate = pr.NetRate,
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

			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var stream = await Task.Run(() =>
				PurchaseItemReportExcelExport.ExportPurchaseItemReport(
					_transactionOverviews,
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
			await ShowToast("Success", "Transaction report exported to Excel successfully.", "success");
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

	private async Task ExportPdf()
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
					_transactionOverviews,
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
			await ShowToast("Success", "Transaction report exported to PDF successfully.", "success");
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
			await ShowToast("Error", $"An error occurred while opening transaction: {ex.Message}", "error");
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

			await ShowToast("Success", "Invoice downloaded successfully.", "success");
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

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}

	private async Task ToggleTransactionReturns()
	{
		_showTransactionReturns = !_showTransactionReturns;
		await LoadTransactionOverviews();
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

	private async Task NavigateToTransactionHistory()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchase, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportPurchase);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard);

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

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}
