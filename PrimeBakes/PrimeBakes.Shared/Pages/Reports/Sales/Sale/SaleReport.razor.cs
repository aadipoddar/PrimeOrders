using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

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
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

using Toolbelt.Blazor.HotKeys2;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleReport : IAsyncDisposable
{
	[Inject] private HotKeys HotKeys { get; set; }
	private HotKeysContext _hotKeysContext;

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSaleReturns = false;
	private bool _showStockTransfers = false;
	private bool _showDeleted = false;
	private bool _isDeleteDialogVisible = false;
	private bool _isRecoverDialogVisible = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private LocationModel _selectedLocation = new();
	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<LocationModel> _locations = [];
	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<SaleOverviewModel> _transactionOverviews = [];
	private List<SaleReturnOverviewModel> _transactionReturnOverviews = [];
	private List<StockTransferOverviewModel> _transactionTransferOverviews = [];

	private SfGrid<SaleOverviewModel> _sfGrid;

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
			.Add(ModCode.Alt, Code.P, DownloadSelectedCartItemInvoice, "Download Selected Transaction Invoice", Exclude.None)
			.Add(Code.Delete, DeleteSelectedCartItem, "Delete Selected Transaction", Exclude.None);

		await LoadDates();
		await LoadLocations();
		await LoadCompanies();
		await LoadParties();
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

			_transactionOverviews = await CommonData.LoadTableDataByDate<SaleOverviewModel>(
				ViewNames.SaleOverview,
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

			if (_showSaleReturns)
				await LoadTransactionReturnOverviews();

			if (_showStockTransfers)
				await LoadTransactionTransferOverviews();
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
		_transactionReturnOverviews = await CommonData.LoadTableDataByDate<SaleReturnOverviewModel>(
			ViewNames.SaleReturnItemOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		if (!_showDeleted)
			_transactionReturnOverviews = [.. _transactionReturnOverviews.Where(_ => _.Status)];

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
		_transactionOverviews.AddRange(_transactionReturnOverviews.Select(pr => new SaleOverviewModel
		{
			Id = pr.Id * -1, // Negative ID to differentiate returns
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = pr.PartyId,
			PartyName = pr.PartyName,
			TransactionDateTime = pr.TransactionDateTime,
			OtherChargesAmount = -pr.OtherChargesAmount,
			RoundOffAmount = -pr.RoundOffAmount,
			TotalAmount = -pr.TotalAmount,
			TotalAfterItemDiscount = -pr.TotalAfterItemDiscount,
			TotalExtraTaxAmount = -pr.TotalExtraTaxAmount,
			TotalInclusiveTaxAmount = -pr.TotalInclusiveTaxAmount,
			BaseTotal = -pr.BaseTotal,
			CreatedAt = pr.CreatedAt,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedFromPlatform = pr.CreatedFromPlatform,
			DiscountAmount = -pr.DiscountAmount,
			DiscountPercent = pr.DiscountPercent,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			Remarks = pr.Remarks,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
			Card = -pr.Card,
			Credit = -pr.Credit,
			Cash = -pr.Cash,
			UPI = -pr.UPI,
			CustomerId = pr.CustomerId,
			CustomerName = pr.CustomerName,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			ItemDiscountAmount = -pr.ItemDiscountAmount,
			OrderDateTime = null,
			OrderId = null,
			OrderTransactionNo = null,
			TotalAfterTax = -pr.TotalAfterTax,
			TotalItems = pr.TotalItems,
			TotalQuantity = -pr.TotalQuantity,
			TransactionNo = pr.TransactionNo,
			PaymentModes = pr.PaymentModes,
			OtherChargesPercent = pr.OtherChargesPercent,
			Status = pr.Status
		}));

		_transactionOverviews = [.. _transactionOverviews.OrderBy(_ => _.TransactionDateTime)];
	}

	private async Task LoadTransactionTransferOverviews()
	{
		_transactionTransferOverviews = await CommonData.LoadTableDataByDate<StockTransferOverviewModel>(
			ViewNames.StockTransferOverview,
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

		if (!_showDeleted)
			_transactionTransferOverviews = [.. _transactionTransferOverviews.Where(_ => _.Status)];

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
		_transactionOverviews.AddRange(_transactionTransferOverviews.Select(pr => new SaleOverviewModel
		{
			Id = 0, // Stock transfers do not have a sale ID
			CompanyId = pr.CompanyId,
			CompanyName = pr.CompanyName,
			PartyId = _parties.FirstOrDefault(p => p.LocationId == pr.ToLocationId)?.Id,
			PartyName = _parties.FirstOrDefault(p => p.LocationId == pr.ToLocationId)?.Name,
			TransactionDateTime = pr.TransactionDateTime,
			OtherChargesAmount = pr.OtherChargesAmount,
			RoundOffAmount = pr.RoundOffAmount,
			TotalAmount = pr.TotalAmount,
			TotalAfterItemDiscount = pr.TotalAfterItemDiscount,
			TotalExtraTaxAmount = pr.TotalExtraTaxAmount,
			TotalInclusiveTaxAmount = pr.TotalInclusiveTaxAmount,
			BaseTotal = pr.BaseTotal,
			CreatedAt = pr.CreatedAt,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedFromPlatform = pr.CreatedFromPlatform,
			DiscountAmount = pr.DiscountAmount,
			DiscountPercent = pr.DiscountPercent,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			Remarks = pr.Remarks,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
			Card = pr.Card,
			Credit = pr.Credit,
			Cash = pr.Cash,
			UPI = pr.UPI,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			ItemDiscountAmount = pr.ItemDiscountAmount,
			OrderDateTime = null,
			OrderId = null,
			OrderTransactionNo = null,
			CustomerId = null,
			CustomerName = null,
			TotalAfterTax = pr.TotalAfterTax,
			TotalItems = pr.TotalItems,
			TotalQuantity = pr.TotalQuantity,
			TransactionNo = pr.TransactionNo,
			PaymentModes = pr.PaymentModes,
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
				SaleReportExcelExport.ExportSaleReport(
					_transactionOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_user.LocationId == 1,
					_selectedLocation?.Name,
					_selectedParty?.Id > 0 ? _selectedParty?.Name : null
				)
			);

			string fileName = $"SALE_REPORT";
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
				SaleReportPdfExport.ExportSaleReport(
					_transactionOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_user.LocationId == 1,
					_selectedLocation?.Name,
					_selectedParty?.Id > 0 ? _selectedParty?.Name : null
				)
			);

			string fileName = $"SALE_REPORT";
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
		await ViewTransaction(selectedCartItem.Id, selectedCartItem.TransactionNo);
	}

	private async Task ViewTransaction(int transactionId, string transactionNo)
	{
		try
		{
			if (transactionId == 0 && !string.IsNullOrEmpty(transactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.StockTransfer}/{stockTransfer.Id}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.StockTransfer}/{stockTransfer.Id}");
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
			await ShowToast("Error", $"An error occurred while opening transaction: {ex.Message}", "error");
		}
	}

	private async Task DownloadSelectedCartItemInvoice()
	{
		if (_sfGrid is null || _sfGrid.SelectedRecords is null || _sfGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfGrid.SelectedRecords.First();
		await DownloadInvoice(selectedCartItem.Id, selectedCartItem.TransactionNo);
	}

	private async Task DownloadInvoice(int transactionId, string transactionNo)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (transactionId == 0 && !string.IsNullOrWhiteSpace(transactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == transactionNo);
				var (pdfStream, fileName) = await StockTransferData.GenerateAndDownloadInvoice(stockTransfer.Id);
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
			_isDeleteDialogVisible = false;
			_isProcessing = true;
			StateHasChanged();

			if (!_user.Admin || _user.LocationId > 1)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			if (_deleteTransactionId == 0 && !string.IsNullOrWhiteSpace(_deleteTransactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == _deleteTransactionNo);
				await StockTransferData.DeleteStockTransfer(stockTransfer.Id);
			}
			else if (_deleteTransactionId < 0)
				await SaleReturnData.DeleteSaleReturn(Math.Abs(_deleteTransactionId));
			else
				await SaleData.DeleteSale(_deleteTransactionId);

			await ShowToast("Success", $"Transaction {_deleteTransactionNo} has been deleted successfully.", "success");

			_deleteTransactionId = 0;
			_deleteTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while deleting transaction: {ex.Message}", "error");
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

	private async Task ToggleDeleted()
	{
		if (_user.LocationId > 1)
			return;

		_showDeleted = !_showDeleted;
		await LoadTransactionOverviews();
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

			if (_recoverTransactionId == 0 && !string.IsNullOrWhiteSpace(_recoverTransactionNo))
			{
				var stockTransfer = _transactionTransferOverviews.FirstOrDefault(st => st.TransactionNo == _recoverTransactionNo);
				await RecoverStockTransferTransaction(stockTransfer.Id);
			}
			else if (_recoverTransactionId < 0)
				await RecoverSaleReturnTransaction(Math.Abs(_recoverTransactionId));
			else
				await RecoverSaleTransaction(_recoverTransactionId);

			await ShowToast("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", "success");

			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while recovering transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadTransactionOverviews();
		}
	}

	private async Task RecoverSaleTransaction(int recoverTransactionId)
	{
		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, recoverTransactionId);
		if (sale is null)
		{
			await ShowToast("Error", "Sale transaction not found.", "error");
			return;
		}

		// Update the Status to true (active)
		sale.Status = true;
		sale.LastModifiedBy = _user.Id;
		sale.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		sale.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await SaleData.RecoverSaleTransaction(sale);
	}

	private async Task RecoverSaleReturnTransaction(int recoverTransactionId)
	{
		var saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, recoverTransactionId);
		if (saleReturn is null)
		{
			await ShowToast("Error", "Transaction not found.", "error");
			return;
		}

		// Update the Status to true (active)
		saleReturn.Status = true;
		saleReturn.LastModifiedBy = _user.Id;
		saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await SaleReturnData.RecoverSaleReturnTransaction(saleReturn);
	}

	private async Task RecoverStockTransferTransaction(int recoverTransactionId)
	{
		var stockTransfer = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, recoverTransactionId);
		if (stockTransfer is null)
		{
			await ShowToast("Error", "Stock transfer transaction not found.", "error");
			return;
		}

		// Update the Status to true (active)
		stockTransfer.Status = true;
		stockTransfer.LastModifiedBy = _user.Id;
		stockTransfer.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		stockTransfer.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await StockTransferData.RecoverStockTransferTransaction(stockTransfer);
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

	private async Task NavigateToItemReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportSaleItem);
	}

	private async Task NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	private async Task NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.SalesDashboard);

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

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();
	}
	#endregion
}