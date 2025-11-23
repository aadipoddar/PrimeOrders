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
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Sales.Sale;

public partial class SaleReport
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showSaleReturns = false;
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
	private List<SaleOverviewModel> _saleOverviews = [];
	private List<SaleReturnOverviewModel> _saleReturnOverviews = [];

	private SfGrid<SaleOverviewModel> _sfSaleGrid;

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

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;
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
		await LoadSaleOverviews();
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

	private async Task LoadSaleOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			_saleOverviews = await SaleData.LoadSaleOverviewByDate(
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
			!_showDeleted);

			if (_selectedLocation?.Id > 0)
				_saleOverviews = [.. _saleOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

			if (_selectedCompany?.Id > 0)
				_saleOverviews = [.. _saleOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_saleOverviews = [.. _saleOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_saleOverviews = [.. _saleOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showSaleReturns)
				await LoadSaleReturnOverviews();
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while loading sale overviews: {ex.Message}", "error");
		}
		finally
		{
			if (_sfSaleGrid is not null)
				await _sfSaleGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task LoadSaleReturnOverviews()
	{
		_saleReturnOverviews = await SaleReturnData.LoadSaleReturnOverviewByDate(
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
			 !_showDeleted);

		if (_selectedLocation?.Id > 0)
			_saleReturnOverviews = [.. _saleReturnOverviews.Where(_ => _.LocationId == _selectedLocation.Id)];

		if (_selectedCompany?.Id > 0)
			_saleReturnOverviews = [.. _saleReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		if (_selectedParty?.Id > 0)
			_saleReturnOverviews = [.. _saleReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

		_saleReturnOverviews = [.. _saleReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergeSaleAndReturns();
	}

	private void MergeSaleAndReturns()
	{
		_saleOverviews.AddRange(_saleReturnOverviews.Select(pr => new SaleOverviewModel
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
			AfterDiscount = -pr.AfterDiscount,
			BaseTotal = -pr.BaseTotal,
			CGSTAmount = -pr.CGSTAmount,
			CGSTPercent = pr.CGSTPercent,
			CreatedAt = pr.CreatedAt,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedFromPlatform = pr.CreatedFromPlatform,
			DiscountAmount = -pr.DiscountAmount,
			DiscountPercent = pr.DiscountPercent,
			FinancialYear = pr.FinancialYear,
			FinancialYearId = pr.FinancialYearId,
			IGSTAmount = -pr.IGSTAmount,
			IGSTPercent = pr.IGSTPercent,
			Remarks = pr.Remarks,
			LastModifiedAt = pr.LastModifiedAt,
			LastModifiedBy = pr.LastModifiedBy,
			LastModifiedByUserName = pr.LastModifiedByUserName,
			LastModifiedFromPlatform = pr.LastModifiedFromPlatform,
			SGSTAmount = -pr.SGSTAmount,
			SGSTPercent = pr.SGSTPercent,
			Card = -pr.Card,
			Credit = -pr.Credit,
			Cash = -pr.Cash,
			UPI = -pr.UPI,
			CustomerId = pr.CustomerId,
			CustomerName = pr.CustomerName,
			LocationId = pr.LocationId,
			LocationName = pr.LocationName,
			ItemDiscountAmount = -pr.ItemDiscountAmount,
			ItemDiscountPercent = pr.ItemDiscountPercent,
			OrderDateTime = null,
			OrderId = null,
			OrderTransactionNo = null,
			TotalAfterDiscount = -pr.TotalAfterDiscount,
			TotalAfterOtherCharges = -pr.TotalAfterOtherCharges,
			TotalAfterTax = -pr.TotalAfterTax,
			TotalItems = pr.TotalItems,
			TotalQuantity = -pr.TotalQuantity,
			TotalTaxAmount = -pr.TotalTaxAmount,
			TransactionNo = pr.TransactionNo,
			PaymentModes = pr.PaymentModes,
			OtherChargesPercent = pr.OtherChargesPercent,
			Status = pr.Status
		}));

		_saleOverviews = [.. _saleOverviews.OrderBy(_ => _.TransactionDateTime)];
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadSaleOverviews();
	}

	private async Task OnLocationChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
	{
		if (_user.LocationId > 1)
			return;

		_selectedLocation = args.Value;
		await LoadSaleOverviews();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		if (_user.LocationId > 1)
			return;

		_selectedCompany = args.Value;
		await LoadSaleOverviews();
	}

	private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		if (_user.LocationId > 1)
			return;

		_selectedParty = args.Value;
		await LoadSaleOverviews();
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
			await LoadSaleOverviews();
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

			// Convert DateTime to DateOnly for Excel export
			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			// Call the Excel export utility
			var stream = await Task.Run(() =>
				SaleReportExcelExport.ExportSaleReport(
					_saleOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_user.LocationId == 1,
					_selectedLocation?.Name
				)
			);

			// Generate file name with date range
			string fileName = $"SALE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			fileName += ".xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Sale report exported to Excel successfully.", "success");
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

			// Convert DateTime to DateOnly for PDF export
			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			// Call the PDF export utility
			var stream = await Task.Run(() =>
				SaleReportPdfExport.ExportSaleReport(
					_saleOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns,
					_user.LocationId == 1,
					_selectedLocation?.Name
				)
			);

			// Generate file name with date range
			string fileName = $"SALE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			fileName += ".pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Sale report exported to PDF successfully.", "success");
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
	private async Task ViewSale(int saleId)
	{
		try
		{
			if (saleId < 0)
			{
				// Navigate to Sale Return Page
				int actualId = Math.Abs(saleId);
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.SaleReturn}/{actualId}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.SaleReturn}/{actualId}");
			}
			else
			{
				// Navigate to Sale Page
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"{PageRouteNames.Sale}/{saleId}", "_blank");
				else
					NavigationManager.NavigateTo($"{PageRouteNames.Sale}/{saleId}");
			}
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while opening sale: {ex.Message}", "error");
		}
	}

	private async Task DownloadInvoice(int saleId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			// Check if saleId is negative (indicates a sale return)
			bool isPurchaseReturn = saleId < 0;
			int actualId = Math.Abs(saleId);

			if (isPurchaseReturn)
			{
				var (pdfStream, fileName) = await SaleReturnData.GenerateAndDownloadInvoice(actualId);
				await SaveAndViewService.SaveAndView(fileName, pdfStream);
			}
			else
			{
				var (pdfStream, fileName) = await SaleData.GenerateAndDownloadInvoice(actualId);
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

			if (_deleteTransactionId < 0)
				await SaleReturnData.DeleteSaleReturn(Math.Abs(_deleteTransactionId));
			else
				await SaleData.DeleteSale(_deleteTransactionId);

			await ShowToast("Success", $"Sale transaction {_deleteTransactionNo} has been deleted successfully.", "success");

			_deleteTransactionId = 0;
			_deleteTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while deleting sale transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadSaleOverviews();
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfSaleGrid is not null)
			await _sfSaleGrid.Refresh();
	}

	private async Task ToggleSaleReturns()
	{
		_showSaleReturns = !_showSaleReturns;
		await LoadSaleOverviews();
		StateHasChanged();
	}

	private async Task ToggleDeleted()
	{
		if (_user.LocationId > 1)
			return;

		_showDeleted = !_showDeleted;
		await LoadSaleOverviews();
		StateHasChanged();
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

			if (_recoverTransactionId == 0)
			{
				await ShowToast("Error", "Invalid sale transaction selected for recovery.", "error");
				return;
			}

			if (_recoverTransactionId < 0)
				await RecoverSaleReturnTransaction(Math.Abs(_recoverTransactionId));
			else
				await RecoverSaleTransaction(_recoverTransactionId);

			await ShowToast("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", "success");

			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while recovering sale transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadSaleOverviews();
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
			await ShowToast("Error", "Sale Return transaction not found.", "error");
			return;
		}

		// Update the Status to true (active)
		saleReturn.Status = true;
		saleReturn.LastModifiedBy = _user.Id;
		saleReturn.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		saleReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await SaleReturnData.RecoverSaleReturnTransaction(saleReturn);
	}
	#endregion

	#region Utilities
	private async Task NavigateToSalePage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Sale, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.Sale);
	}

	private async Task NavigateToItemReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleItem, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportSaleItem);
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
	#endregion
}