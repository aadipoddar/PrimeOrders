using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class CompanyPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private CompanyModel _company = new();

	private List<CompanyModel> _companies = [];
	private List<StateUTModel> _stateUTs = [];

	private SfGrid<CompanyModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteCompanyId = 0;
	private string _deleteCompanyName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverCompanyId = 0;
	private string _recoverCompanyName = string.Empty;
	private bool _isRecoverDialogVisible = false;

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;

	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableData<CompanyModel>(TableNames.Company);
		_stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);

		if (!_showDeleted)
			_companies = [.. _companies.Where(c => c.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditCompany(CompanyModel company)
	{
		_company = new()
		{
			Id = company.Id,
			Name = company.Name,
			Code = company.Code,
			StateUTId = company.StateUTId,
			GSTNo = company.GSTNo,
			PANNo = company.PANNo,
			CINNo = company.CINNo,
			Alias = company.Alias,
			Phone = company.Phone,
			Email = company.Email,
			Address = company.Address,
			Remarks = company.Remarks,
			Status = company.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteCompanyId = id;
		_deleteCompanyName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteCompanyId = 0;
		_deleteCompanyName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var company = _companies.FirstOrDefault(c => c.Id == _deleteCompanyId);
			if (company == null)
			{
				await ShowToast("Error", "Company not found.", "error");
				return;
			}

			company.Status = false;
			await CompanyData.InsertCompany(company);

			await ShowToast("Success", $"Company '{company.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete Company: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteCompanyId = 0;
			_deleteCompanyName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverCompanyId = id;
		_recoverCompanyName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverCompanyId = 0;
		_recoverCompanyName = string.Empty;
		_isRecoverDialogVisible = false;
		StateHasChanged();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			_isRecoverDialogVisible = false;

			var company = _companies.FirstOrDefault(c => c.Id == _recoverCompanyId);
			if (company == null)
			{
				await ShowToast("Error", "Company not found.", "error");
				return;
			}

			company.Status = true;
			await CompanyData.InsertCompany(company);

			await ShowToast("Success", $"Company '{company.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover Company: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_recoverCompanyId = 0;
			_recoverCompanyName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_company.Name = _company.Name?.Trim() ?? "";
		_company.Name = _company.Name?.ToUpper() ?? "";

		_company.Code = _company.Code?.Trim() ?? "";
		_company.Code = _company.Code?.ToUpper() ?? "";

		_company.GSTNo = _company.GSTNo?.Trim() ?? "";
		_company.GSTNo = _company.GSTNo?.ToUpper() ?? "";

		_company.PANNo = _company.PANNo?.Trim() ?? "";
		_company.PANNo = _company.PANNo?.ToUpper() ?? "";

		_company.CINNo = _company.CINNo?.Trim() ?? "";
		_company.CINNo = _company.CINNo?.ToUpper() ?? "";

		_company.Alias = _company.Alias?.Trim() ?? "";
		_company.Alias = _company.Alias?.ToUpper() ?? "";

		_company.Phone = _company.Phone?.Trim() ?? "";
		_company.Email = _company.Email?.Trim() ?? "";
		_company.Address = _company.Address?.Trim() ?? "";

		_company.Remarks = _company.Remarks?.Trim() ?? "";
		_company.Status = true;

		if (string.IsNullOrWhiteSpace(_company.Name))
		{
			await ShowToast("Error", "Company name is required. Please enter a valid company name.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_company.Code))
		{
			await ShowToast("Error", "Code is required. Please enter a valid code.", "error");
			return false;
		}

		if (_company.StateUTId <= 0)
		{
			await ShowToast("Error", "State/UT is required. Please select a valid State/UT.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_company.GSTNo)) _company.GSTNo = null;
		if (string.IsNullOrWhiteSpace(_company.PANNo)) _company.PANNo = null;
		if (string.IsNullOrWhiteSpace(_company.CINNo)) _company.CINNo = null;
		if (string.IsNullOrWhiteSpace(_company.Alias)) _company.Alias = null;
		if (string.IsNullOrWhiteSpace(_company.Phone)) _company.Phone = null;
		if (string.IsNullOrWhiteSpace(_company.Email)) _company.Email = null;
		if (string.IsNullOrWhiteSpace(_company.Address)) _company.Address = null;
		if (string.IsNullOrWhiteSpace(_company.Remarks)) _company.Remarks = null;

		if (_company.Id > 0)
		{
			var existingCompany = _companies.FirstOrDefault(_ => _.Id != _company.Id && _.Name.Equals(_company.Name, StringComparison.OrdinalIgnoreCase));
			if (existingCompany is not null)
			{
				await ShowToast("Error", $"Company name '{_company.Name}' already exists. Please choose a different name.", "error");
				return false;
			}

			var existingCode = _companies.FirstOrDefault(_ => _.Id != _company.Id && _.Code.Equals(_company.Code, StringComparison.OrdinalIgnoreCase));
			if (existingCode is not null)
			{
				await ShowToast("Error", $"Company code '{_company.Code}' already exists. Please choose a different code.", "error");
				return false;
			}
		}
		else
		{
			var existingCompany = _companies.FirstOrDefault(_ => _.Name.Equals(_company.Name, StringComparison.OrdinalIgnoreCase));
			if (existingCompany is not null)
			{
				await ShowToast("Error", $"Company name '{_company.Name}' already exists. Please choose a different name.", "error");
				return false;
			}

			var existingCode = _companies.FirstOrDefault(_ => _.Code.Equals(_company.Code, StringComparison.OrdinalIgnoreCase));
			if (existingCode is not null)
			{
				await ShowToast("Error", $"Company code '{_company.Code}' already exists. Please choose a different code.", "error");
				return false;
			}
		}

		return true;
	}

	private async Task SaveCompany()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await CompanyData.InsertCompany(_company);

			await ShowToast("Success", $"Company '{_company.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save Company: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
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

			// Call the Excel export utility
			var stream = await Task.Run(() => CompanyExcelExport.ExportCompany(_companies));

			// Generate file name
			string fileName = "COMPANY_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Company data exported to Excel successfully.", "success");
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

			// Call the PDF export utility
			var stream = await Task.Run(() => CompanyPDFExport.ExportCompany(_companies));

			// Generate file name
			string fileName = "COMPANY_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Company data exported to PDF successfully.", "success");
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