using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Purchase;
using PrimeBakesLibrary.Exporting.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Purchase;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Stock;

public partial class RawMaterialStockReport
{
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

		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		await LoadDates();
		await LoadStockData();
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
			await ShowToast("Error", $"An error occurred while loading stock data : {ex.Message}", "error");
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
		_stockDetails = await RawMaterialStockData.LoadRawMaterialStockDetailsByDate(_fromDate, _toDate);
		_stockDetails = [.. _stockDetails.OrderBy(_ => _.TransactionDate).ThenBy(_ => _.RawMaterialName)];
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
			await LoadStockData();
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

	private async Task ViewTransaction(string type, int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			var url = type?.ToLower() switch
			{
				"purchase" => $"/inventory/purchase/{transactionId}",
				"purchasereturn" => $"/inventory/purchase-return/{transactionId}",
				"sale" => $"/sale/sale/{transactionId}",
				"salereturn" => $"/sale/sale-return/{transactionId}",
				"kitchenissue" => $"/production/kitchen-issue/{transactionId}",
				"kitchenproduction" => $"/production/kitchen-production/{transactionId}",
				_ => null
			};

			if (string.IsNullOrEmpty(url))
			{
				await ShowToast("Error", "Unknown transaction type.", "error");
				return;
			}

			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", url, "_blank");
			else
				NavigationManager.NavigateTo(url);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while opening transaction: {ex.Message}", "error");
		}
	}

	private async Task DownloadInvoice(string type, int transactionId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (type?.ToLower() == "purchase")
				await DownloadPurchaseInvoice(transactionId);
			else if (type?.ToLower() == "purchasereturn")
				await DownloadPurchaseReturnInvoice(transactionId);

			await ShowToast("Info", "Invoice download functionality to be implemented.", "error");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while downloading invoice: {ex.Message}", "error");
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
		await SaveAndViewService.SaveAndView(fileName, stream);

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
		await SaveAndViewService.SaveAndView(fileName, stream);

		await ShowToast("Success", $"Purchase return invoice generated successfully for {purchaseReturnHeader.TransactionNo}", "success");
	}

	private void DeleteAdjustment(int adjustmentId)
	{
		if (_isProcessing)
			return;

		// Find the adjustment in the details list to get the transaction number
		var adjustment = _stockDetails.FirstOrDefault(x => x.Id == adjustmentId);
		if (adjustment is null)
			return;

		_deleteAdjustmentId = adjustmentId;
		_deleteTransactionNo = adjustment.TransactionNo ?? "N/A";
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

	private async Task ConfirmDelete()
	{
		if (_isProcessing || _deleteAdjustmentId == 0)
			return;

		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;
			StateHasChanged();

			await RawMaterialStockData.DeleteRawMaterialStockById(_deleteAdjustmentId);
			await ShowToast("Success", "Stock adjustment deleted successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while deleting adjustment: {ex.Message}", "error");
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

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing || _stockSummary is null || _stockSummary.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			// Load details data if _showDetails is true and not already loaded
			if (_showDetails && (_stockDetails is null || _stockDetails.Count == 0))
				await LoadStockDetails();

			// Generate the Excel file using the custom export utility
			var stream = await Task.Run(() =>
				RawMaterialStockReportExcelExport.ExportRawMaterialStockReport(
					_stockSummary,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns,
					_showDetails ? _stockDetails : null
				)
			);

			// Generate file name
			string fileName = $"RAW_MATERIAL_STOCK_REPORT_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
			await SaveAndViewService.SaveAndView(fileName, stream);
			await ShowToast("Success", "Stock report exported to Excel successfully.", "success");
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
		if (_isProcessing || _stockSummary is null || _stockSummary.Count == 0)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			// Generate timestamp for file naming
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

			// Generate the summary PDF file using the custom export utility
			var summaryStream = await Task.Run(() =>
				RawMaterialStockSummaryReportPDFExport.ExportRawMaterialStockReport(
					_stockSummary,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate),
					_showAllColumns
				)
			);

			// Generate summary file name
			string summaryFileName = $"RAW_MATERIAL_STOCK_SUMMARY_{timestamp}.pdf";
			await SaveAndViewService.SaveAndView(summaryFileName, summaryStream);
			await ShowToast("Success", "Stock summary exported to PDF successfully.", "success");

			// If details are shown, generate and export the details PDF as well
			if (!_showDetails)
				return;

			// Generate the details PDF file
			if (_stockDetails is null || _stockDetails.Count == 0)
				return;

			var detailsStream = await Task.Run(() =>
				RawMaterialStockDetailsReportPDFExport.ExportRawMaterialStockDetailsReport(
					_stockDetails,
					DateOnly.FromDateTime(_fromDate),
					DateOnly.FromDateTime(_toDate)
				)
			);

			// Generate details file name
			string detailsFileName = $"RAW_MATERIAL_STOCK_DETAILS_{timestamp}.pdf";
			await SaveAndViewService.SaveAndView(detailsFileName, detailsStream);
			await ShowToast("Success", "Stock details exported to PDF successfully.", "success");
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

	#region Utilities
	private async Task NavigateToStockAdjustmentPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/inventory/raw-material-stock-adjustment", "_blank");
		else
			NavigationManager.NavigateTo("/inventory/raw-material-stock-adjustment");
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
	#endregion
}