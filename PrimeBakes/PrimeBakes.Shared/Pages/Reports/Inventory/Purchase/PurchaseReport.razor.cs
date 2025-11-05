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
	private bool _isDeleteDialogVisible = false;

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

	private SfToast _sfErrorToast;
	private SfToast _sfSuccessToast;
	private SfDialog _deleteConfirmationDialog;

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
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

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
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

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
			OtherChargesPercent = pr.OtherChargesPercent
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
					_purchaseOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				)
			);

			// Generate file name with date range
			string fileName = $"PURCHASE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			{
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			}
			fileName += ".xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", stream);

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
					_purchaseOverviews,
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				)
			);

			// Generate file name with date range
			string fileName = $"PURCHASE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			{
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			}
			fileName += ".pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, "application/pdf", stream);

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

	private async Task ExportPowerBI(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		await ShowToast("Info", "Power BI export is not implemented yet.", "error");
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
					await JSRuntime.InvokeVoidAsync("open", $"/inventory/purchasereturn/{actualId}", "_blank");
				else
					NavigationManager.NavigateTo($"/inventory/purchasereturn/{actualId}");
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
				await DownloadPurchaseReturnInvoice(actualId);
			else
				await DownloadPurchaseInvoice(actualId);
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

	private async Task DownloadPurchaseInvoice(int purchaseId)
	{
		// Load purchase header
		var purchaseHeader = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, purchaseId);
		if (purchaseHeader is null)
		{
			await ShowToast("Error", "Purchase record not found.", "error");
			return;
		}

		// Load purchase details
		var purchaseDetails = await PurchaseData.LoadPurchaseDetailByPurchase(purchaseId);
		if (purchaseDetails is null || purchaseDetails.Count == 0)
		{
			await ShowToast("Error", "No purchase details found for this transaction.", "error");
			return;
		}

		// Load company information
		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, purchaseHeader.CompanyId);
		if (company is null)
		{
			await ShowToast("Error", "Company information not found.", "error");
			return;
		}

		// Load party/supplier information
		var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, purchaseHeader.PartyId);
		if (party is null)
		{
			await ShowToast("Error", "Party information not found.", "error");
			return;
		}

		// Generate invoice PDF
		var stream = await Task.Run(() =>
			PurchaseInvoicePDFExport.ExportPurchaseInvoice(
				purchaseHeader,
				purchaseDetails,
				company,
				party,
				logoPath: null, // Uses default logo from wwwroot
				invoiceType: "PURCHASE INVOICE"
			)
		);

		// Generate file name
		string fileName = $"PURCHASE_INVOICE_{purchaseHeader.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		fileName = fileName.Replace("/", "_").Replace("\\", "_"); // Clean up transaction number

		// Save and view the invoice PDF
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", stream);

		await ShowToast("Success", $"Invoice generated successfully for {purchaseHeader.TransactionNo}", "success");
	}

	private async Task DownloadPurchaseReturnInvoice(int purchaseReturnId)
	{
		// Load purchase return header
		var purchaseReturnHeader = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturnId);
		if (purchaseReturnHeader == null)
		{
			await ShowToast("Error", "Purchase return record not found.", "error");
			return;
		}

		// Load purchase return details
		var purchaseReturnDetails = await PurchaseReturnData.LoadPurchaseReturnDetailByPurchaseReturn(purchaseReturnId);
		if (purchaseReturnDetails is null || purchaseReturnDetails.Count == 0)
		{
			await ShowToast("Error", "No purchase return details found for this transaction.", "error");
			return;
		}

		// Load company information
		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, purchaseReturnHeader.CompanyId);
		if (company is null)
		{
			await ShowToast("Error", "Company information not found.", "error");
			return;
		}

		// Load party/supplier information
		var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, purchaseReturnHeader.PartyId);
		if (party is null)
		{
			await ShowToast("Error", "Party information not found.", "error");
			return;
		}

		// Generate invoice PDF
		var stream = await Task.Run(() =>
			PurchaseReturnInvoicePDFExport.ExportPurchaseReturnInvoice(
				purchaseReturnHeader,
				purchaseReturnDetails,
				company,
				party,
				logoPath: null, // Uses default logo from wwwroot
				invoiceType: "PURCHASE RETURN INVOICE"
			)
		);

		// Generate file name
		string fileName = $"PURCHASE_RETURN_INVOICE_{purchaseReturnHeader.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		fileName = fileName.Replace("/", "_").Replace("\\", "_"); // Clean up transaction number

		// Save and view the invoice PDF
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", stream);

		await ShowToast("Success", $"Purchase return invoice generated successfully for {purchaseReturnHeader.TransactionNo}", "success");
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
			await SaveAndViewService.SaveAndView(fileName, contentType, fileStream);
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

			var purchase = await CommonData.LoadTableDataById<PurchaseModel>(TableNames.Purchase, _deleteTransactionId);
			var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, purchase.FinancialYearId);
			if (financialYear is null || financialYear.Locked || financialYear.Status == false)
				throw new InvalidOperationException("Cannot delete purchase transaction as the financial year is locked.");

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to delete this purchase transaction.");

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
			await JSRuntime.InvokeVoidAsync("open", "/report/purchaseitem", "_blank");
		else
			NavigationManager.NavigateTo("/report/purchaseitem");
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
	#endregion
}