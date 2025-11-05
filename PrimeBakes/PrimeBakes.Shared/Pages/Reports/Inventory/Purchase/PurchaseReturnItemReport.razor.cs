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

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Purchase;

public partial class PurchaseReturnItemReport
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<PurchaseReturnItemOverviewModel> _purchaseReturnItemOverviews = [];
	private List<PurchaseItemOverviewModel> _purchaseItemOverviews = [];

	private SfGrid<PurchaseReturnItemOverviewModel> _sfPurchaseReturnItemGrid;

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
		await LoadPurchaseReturnItemOverviews();
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

	private async Task LoadPurchaseReturnItemOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			_purchaseReturnItemOverviews = await PurchaseReturnData.LoadPurchaseReturnItemOverviewByDate(
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue));

			if (_selectedCompany?.Id > 0)
				_purchaseReturnItemOverviews = [.. _purchaseReturnItemOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedParty?.Id > 0)
				_purchaseReturnItemOverviews = [.. _purchaseReturnItemOverviews.Where(_ => _.PartyId == _selectedParty.Id)];

			_purchaseReturnItemOverviews = [.. _purchaseReturnItemOverviews.OrderBy(_ => _.TransactionDateTime)];
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while loading purchase return item overviews: {ex.Message}", "error");
		}
		finally
		{
			if (_sfPurchaseReturnItemGrid is not null)
				await _sfPurchaseReturnItemGrid.Refresh();
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
		await LoadPurchaseReturnItemOverviews();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		_selectedCompany = args.Value;
		await LoadPurchaseReturnItemOverviews();
	}

	private async Task OnPartyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LedgerModel, LedgerModel> args)
	{
		_selectedParty = args.Value;
		await LoadPurchaseReturnItemOverviews();
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
			await LoadPurchaseReturnItemOverviews();
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

			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var stream = await Task.Run(() =>
			PurchaseReturnItemReportExcelExport.ExportPurchaseReturnItemReport(
			_purchaseReturnItemOverviews,
			dateRangeStart,
			dateRangeEnd,
			_showAllColumns
			)
			);

			string fileName = $"PURCHASE_RETURN_ITEM_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			{
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			}
			fileName += ".xlsx";

			await SaveAndViewService.SaveAndView(fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", stream);

			await ShowToast("Success", "Purchase return item report exported to Excel successfully.", "success");
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

			DateOnly? dateRangeStart = _fromDate != default ? DateOnly.FromDateTime(_fromDate) : null;
			DateOnly? dateRangeEnd = _toDate != default ? DateOnly.FromDateTime(_toDate) : null;

			var stream = await Task.Run(() =>
			PurchaseReturnItemReportPDFExport.ExportPurchaseReturnItemReport(
			_purchaseReturnItemOverviews,
			dateRangeStart,
			dateRangeEnd,
			_showAllColumns
			)
			);

			string fileName = $"PURCHASE_RETURN_ITEM_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			{
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			}
			fileName += ".pdf";

			await SaveAndViewService.SaveAndView(fileName, "application/pdf", stream);

			await ShowToast("Success", "Purchase return item report exported to PDF successfully.", "success");
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
	private async Task ViewPurchaseReturn(int purchaseReturnId)
	{
		try
		{
			if (purchaseReturnId < 0)
			{
				int actualId = Math.Abs(purchaseReturnId);
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"/inventory/purchase/{actualId}", "_blank");
				else
					NavigationManager.NavigateTo($"/inventory/purchase/{actualId}");
			}
			else
			{
				if (FormFactor.GetFormFactor() == "Web")
					await JSRuntime.InvokeVoidAsync("open", $"/inventory/purchasereturn/{purchaseReturnId}", "_blank");
				else
					NavigationManager.NavigateTo($"/inventory/purchasereturn/{purchaseReturnId}");
			}
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while opening purchase return: {ex.Message}", "error");
		}
	}

	private async Task DownloadInvoice(int purchaseReturnId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			var purchaseReturnHeader = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, purchaseReturnId);
			if (purchaseReturnHeader == null)
			{
				await ShowToast("Error", "Purchase return record not found.", "error");
				return;
			}

			var purchaseReturnDetails = await PurchaseReturnData.LoadPurchaseReturnDetailByPurchaseReturn(purchaseReturnId);
			if (purchaseReturnDetails is null || purchaseReturnDetails.Count == 0)
			{
				await ShowToast("Error", "No purchase return details found for this transaction.", "error");
				return;
			}

			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, purchaseReturnHeader.CompanyId);
			if (company is null)
			{
				await ShowToast("Error", "Company information not found.", "error");
				return;
			}

			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, purchaseReturnHeader.PartyId);
			if (party is null)
			{
				await ShowToast("Error", "Party information not found.", "error");
				return;
			}

			var stream = await Task.Run(() =>
			PurchaseReturnInvoicePDFExport.ExportPurchaseReturnInvoice(
			purchaseReturnHeader,
			purchaseReturnDetails,
			company,
			party,
			logoPath: null,
			invoiceType: "PURCHASE RETURN INVOICE"
			)
			);

			string fileName = $"PURCHASE_RETURN_INVOICE_{purchaseReturnHeader.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
			fileName = fileName.Replace("/", "_").Replace("\\", "_");

			await SaveAndViewService.SaveAndView(fileName, "application/pdf", stream);

			await ShowToast("Success", $"Purchase return invoice generated successfully for {purchaseReturnHeader.TransactionNo}", "success");
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

		if (_sfPurchaseReturnItemGrid is not null)
			await _sfPurchaseReturnItemGrid.Refresh();
	}
	#endregion

	#region Utilities
	private async Task NavigateToPurchaseReturnPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/inventory/purchasereturn", "_blank");
		else
			NavigationManager.NavigateTo("/inventory/purchasereturn");
	}

	private async Task NavigateToPurchaseReturnReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/report/purchasereturn", "_blank");
		else
			NavigationManager.NavigateTo("/report/purchasereturn");
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
