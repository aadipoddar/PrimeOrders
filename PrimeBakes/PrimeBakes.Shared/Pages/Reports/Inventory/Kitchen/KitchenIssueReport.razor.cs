using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Reports.Inventory.Kitchen;

public partial class KitchenIssueReport
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showAllColumns = false;
	private bool _showDeleted = false;
	private bool _isDeleteDialogVisible = false;
	private bool _isRecoverDialogVisible = false;

	private DateTime _fromDate = DateTime.Now.Date;
	private DateTime _toDate = DateTime.Now.Date;

	private CompanyModel _selectedCompany = new();
	private LocationModel _selectedKitchen = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _kitchens = [];
	private List<KitchenIssueOverviewModel> _kitchenIssueOverviews = [];

	private SfGrid<KitchenIssueOverviewModel> _sfKitchenIssueGrid;

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
		await LoadKitchens();
		await LoadKitchenIssueOverviews();
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

	private async Task LoadKitchenIssueOverviews()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			_kitchenIssueOverviews = await KitchenIssueData.LoadKitchenIssueOverviewByDate(
			DateOnly.FromDateTime(_fromDate).ToDateTime(TimeOnly.MinValue),
			DateOnly.FromDateTime(_toDate).ToDateTime(TimeOnly.MaxValue),
			!_showDeleted);

			if (_selectedCompany?.Id > 0)
				_kitchenIssueOverviews = [.. _kitchenIssueOverviews.Where(_ => _.CompanyId == _selectedCompany.Id)];

			if (_selectedKitchen?.Id > 0)
				_kitchenIssueOverviews = [.. _kitchenIssueOverviews.Where(_ => _.KitchenId == _selectedKitchen.Id)];

			_kitchenIssueOverviews = [.. _kitchenIssueOverviews.OrderBy(_ => _.TransactionDateTime)];
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while loading kitchen issue overviews: {ex.Message}", "error");
		}
		finally
		{
			if (_sfKitchenIssueGrid is not null)
				await _sfKitchenIssueGrid.Refresh();
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnDateRangeChanged(Syncfusion.Blazor.Calendars.RangePickerEventArgs<DateTime> args)
	{
		_fromDate = args.StartDate;
		_toDate = args.EndDate;
		await LoadKitchenIssueOverviews();
	}

	private async Task OnCompanyChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		_selectedCompany = args.Value;
		await LoadKitchenIssueOverviews();
	}

	private async Task OnKitchenChanged(Syncfusion.Blazor.DropDowns.ChangeEventArgs<LocationModel, LocationModel> args)
	{
		_selectedKitchen = args.Value;
		await LoadKitchenIssueOverviews();
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
			await LoadKitchenIssueOverviews();
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
				KitchenIssueReportExcelExport.ExportKitchenIssueReport(
					_kitchenIssueOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				)
			);

			// Generate file name with date range
			string fileName = $"KITCHEN_ISSUE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			fileName += ".xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Kitchen issue report exported to Excel successfully.", "success");
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
				KitchenIssueReportPDFExport.ExportKitchenIssueReport(
					_kitchenIssueOverviews.Where(_ => _.Status),
					dateRangeStart,
					dateRangeEnd,
					_showAllColumns
				)
			);

			// Generate file name with date range
			string fileName = $"KITCHEN_ISSUE_REPORT";
			if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
				fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
			fileName += ".pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Kitchen issue report exported to PDF successfully.", "success");
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
	private async Task ViewKitchenIssue(int kitchenIssueId)
	{
		try
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", $"/inventory/kitchen-issue/{kitchenIssueId}", "_blank");
			else
				NavigationManager.NavigateTo($"/inventory/kitchen-issue/{kitchenIssueId}");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while opening kitchen issue: {ex.Message}", "error");
		}
	}

	private async Task DownloadInvoice(int kitchenIssueId)
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			var (pdfStream, fileName) = await KitchenIssueData.GenerateAndDownloadInvoice(kitchenIssueId);
			await SaveAndViewService.SaveAndView(fileName, pdfStream);
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

			if (!_user.Admin)
				throw new UnauthorizedAccessException("You do not have permission to delete this transaction.");

			await KitchenIssueData.DeleteKitchenIssue(_deleteTransactionId);

			await ShowToast("Success", $"Kitchen issue transaction {_deleteTransactionNo} has been deleted successfully.", "success");

			_deleteTransactionId = 0;
			_deleteTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while deleting kitchen issue transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadKitchenIssueOverviews();
		}
	}

	private async Task ToggleDetailsView()
	{
		_showAllColumns = !_showAllColumns;
		StateHasChanged();

		if (_sfKitchenIssueGrid is not null)
			await _sfKitchenIssueGrid.Refresh();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadKitchenIssueOverviews();
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
				throw new UnauthorizedAccessException("You do not have permission to recover this kitchen issue transaction.");

			if (_recoverTransactionId == 0)
			{
				await ShowToast("Error", "Invalid kitchen issue transaction selected for recovery.", "error");
				return;
			}

			await RecoverKitchenIssueTransaction(_recoverTransactionId);

			await ShowToast("Success", $"Transaction {_recoverTransactionNo} has been recovered successfully.", "success");

			_recoverTransactionId = 0;
			_recoverTransactionNo = string.Empty;
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while recovering kitchen issue transaction: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
			await LoadKitchenIssueOverviews();
		}
	}

	private async Task RecoverKitchenIssueTransaction(int recoverTransactionId)
	{
		var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, recoverTransactionId);
		if (kitchenIssue is null)
		{
			await ShowToast("Error", "Kitchen issue transaction not found.", "error");
			return;
		}

		// Update the Status to true (active)
		kitchenIssue.Status = true;
		kitchenIssue.LastModifiedBy = _user.Id;
		kitchenIssue.LastModifiedAt = await CommonData.LoadCurrentDateTime();
		kitchenIssue.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();

		await KitchenIssueData.RecoverKitchenIssueTransaction(kitchenIssue);
	}
	#endregion

	#region Utilities
	private async Task NavigateToKitchenIssuePage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/inventory/kitchen-issue", "_blank");
		else
			NavigationManager.NavigateTo("/inventory/kitchen-issue");
	}

	private async Task NavigateToItemReport(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", "/report/kitchen-issue-item", "_blank");
		else
			NavigationManager.NavigateTo("/report/kitchen-issue-item");
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