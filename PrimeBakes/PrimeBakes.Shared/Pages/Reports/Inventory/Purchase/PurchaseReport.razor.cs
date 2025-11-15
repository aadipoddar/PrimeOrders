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
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseReport
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showPurchaseReturns = false;
	private bool _showDeleted = false;
	private bool _isDeleteDialogVisible = false;
	private bool _isRecoverDialogVisible = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<PurchaseOverviewModel> _purchaseOverviews = [];
	private List<PurchaseReturnOverviewModel> _purchaseReturnOverviews = [];

	private SfGrid<PurchaseOverviewModel> _sfPurchaseGrid;

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

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory);
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
		await LoadPurchaseOverviews();
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

	private async Task LoadPurchaseOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			_purchaseOverviews = await PurchaseData.LoadPurchaseOverviewByDate(
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
			!_showDeleted);

			if (_selectedCompany?.Id > 0)
				_purchaseOverviews = [.. _purchaseOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_purchaseOverviews = [.. _purchaseOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_purchaseOverviews = [.. _purchaseOverviews.OrderBy(_ => _.TransactionDateTime)];

			if (_showPurchaseReturns)
				await LoadPurchaseReturnOverviews();
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while loading purchase overviews: {ex.Message}", "error");
		}
		finally
		{
			if (_sfPurchaseGrid is not null)
				await _sfPurchaseGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task LoadPurchaseReturnOverviews()
	{
		_purchaseReturnOverviews = await PurchaseReturnData.LoadPurchaseReturnOverviewByDate(
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
			 !_showDeleted);

		if (_selectedCompany?.Id > 0)
			_purchaseReturnOverviews = [.. _purchaseReturnOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

		if (_selectedParty?.Id > 0)
			_purchaseReturnOverviews = [.. _purchaseReturnOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

		_purchaseReturnOverviews = [.. _purchaseReturnOverviews.OrderBy(_ => _.TransactionDateTime)];

		MergePurchaseAndReturns();
	}

	private void MergePurchaseAndReturns()
	{
		_purchaseOverviews.AddRange(_purchaseReturnOverviews.Select(pr => new PurchaseOverviewModel
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
			AfterDiscount = -pr.AfterDiscount,
			BaseTotal = -pr.BaseTotal,
			CashDiscountPercent = pr.CashDiscountPercent,
			CGSTAmount = -pr.CGSTAmount,
			CGSTPercent = pr.CGSTPercent,
			CreatedAt = pr.CreatedAt,
			CreatedBy = pr.CreatedBy,
			CreatedByName = pr.CreatedByName,
			CreatedFromPlatform = pr.CreatedFromPlatform,
			DiscountAmount = -pr.DiscountAmount,
			DiscountPercent = pr.DiscountPercent,
			DocumentUrl = pr.DocumentUrl,
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
			TotalAfterCashDiscount = -pr.TotalAfterCashDiscount,
			TotalAfterOtherCharges = -pr.TotalAfterOtherCharges,
			TotalAfterTax = -pr.TotalAfterTax,
			TotalItems = pr.TotalItems,
			TotalQuantity = -pr.TotalQuantity,
			TotalTaxAmount = -pr.TotalTaxAmount,
			TransactionNo = pr.TransactionNo,
			OtherChargesPercent = pr.OtherChargesPercent,
			Status = pr.Status
		}));

		_purchaseOverviews = [.. _purchaseOverviews.OrderBy(_ => _.TransactionDateTime)];
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadPurchaseOverviews();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		_selectedCompany = args.Value;
		await LoadPurchaseOverviews();
	}

	private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		_selectedParty = args.Value;
		await LoadPurchaseOverviews();
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
			await LoadPurchaseOverviews();
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
				PurchaseReportExcelExport.ExportPurchaseReport(
					_purchaseOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				)
			);

			// Generate file name with date range
			string fileName = $"PURCHASE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			fileName += ".xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Purchase report exported to Excel successfully.", "success");
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
				PurchaseReportPDFExport.ExportPurchaseReport(
					_purchaseOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				)
			);

			// Generate file name with date range
			string fileName = $"PURCHASE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			fileName += ".pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Purchase report exported to PDF successfully.", "success");
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
				// Navigate to Purchase Return Page
				int actualId = Math.Abs(purchaseId);
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"/inventory/purchase-return/{actualId}", "_blank");
				else
					NavigationManager.NavigateTo($"/inventory/purchase-return/{actualId}");
			}
			else
			{
				// Navigate to Purchase Page
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

			// Check if purchaseId is negative (indicates a purchase return)
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

	private async Task DownloadOriginalInvoice(string documentUrl)
	{
		if (_isProcessing)
			return;

		try
		{
			if (string.IsNullOrEmpty(documentUrl))
			{
				await ShowToast("Warning", "No original document available for this purchase.", "error");
				return;
			}

			_isProcessing = true;

			var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(documentUrl, BlobStorageContainers.purchase);
			var fileName = documentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, fileStream);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while downloading original invoice: {ex.Message}", "error");
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

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			if (_deleteTransactionId < 0)
				await PurchaseReturnData.DeletePurchaseReturn(Math.Abs(_deleteTransactionId));
			else
				await PurchaseData.DeletePurchase(_deleteTransactionId);

			await ShowToast("Success", $"Purchase transaction {_deleteTransactionNo} has been deleted successfully.", "success");

			_deleteTransactionId = 0;
			_deleteTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while deleting purchase transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadPurchaseOverviews();
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfPurchaseGrid is not null)
			await _sfPurchaseGrid.Refresh();
	}

	private async Task TogglePurchaseReturns()
	{
		_showPurchaseReturns = !_showPurchaseReturns;
		await LoadPurchaseOverviews();
		StateHasChanged();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadPurchaseOverviews();
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

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to recover this purchase transaction.");

			if (_recoverTransactionId == 0)
			{
				await ShowToast("Error", "Invalid purchase transaction selected for recovery.", "error");
				return;
			}

			if (_recoverTransactionId < 0)
				await RecoverPurchaseReturnTransaction(Math.Abs(_recoverTransactionId));
			else
				await RecoverPurchaseTransaction(_recoverTransactionId);

			await ShowToast("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", "success");

			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while recovering purchase transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadPurchaseOverviews();
		}
	}

	private async Task RecoverPurchaseTransaction(int recoverTransactionId)
	{
		var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, recoverTransactionId);
		if (purchase is null)
		{
			await ShowToast("Error", "Purchase transaction not found.", "error");
			return;
		}

		// Update the Status to true (active)
		purchase.Status = true;
		purchase.LastModifiedBy = _user.Id;
		purchase.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		purchase.LastModifiedFromPlatform = FormFactor.GetFormFactor();

		await PurchaseData.RecoverPurchaseTransaction(purchase);
	}

	private async Task RecoverPurchaseReturnTransaction(int recoverTransactionId)
	{
		var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, recoverTransactionId);
		if (purchaseReturn is null)
		{
			await ShowToast("Error", "Purchase Return transaction not found.", "error");
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
	private async Task NavigateToPurchasePage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/inventory/purchase", "_blank");
		else
			NavigationManager.NavigateTo("/inventory/purchase");
	}

	private async Task NavigateToItemReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/report/purchase-item", "_blank");
		else
			NavigationManager.NavigateTo("/report/purchase-item");
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